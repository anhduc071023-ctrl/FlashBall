using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class CloudAuthConfig
{
    public string firebaseDatabaseUrl = "";
    public string databaseAuthSecret = "";
    public string googleOAuthClientId = "";
    public string googleOAuthClientSecret = "";
}

[Serializable]
public class AccountRecord
{
    public string accountId;
    public string username;
    public string passwordHash;
    public string googleEmail;
    public string authProvider; // "local" or "google"
    public string displayName;

    public int elo = 100;
    public int gold = 0;
    public int soloWins = 0;
    public int soloLosses = 0;
    public int matchesPlayed = 0;

    public int equippedWeapon = 0;
    public List<int> ownedWeapons = new List<int> { 0 };

    public int equippedSkill = 1; // SkillType.CloneBall
    public List<int> ownedSkills = new List<int> { 1 };

    public int equippedSkin = 0; // SkinType.DefaultStriker
    public List<int> ownedSkins = new List<int> { 0 };

    public bool tutorialCompleted = false;
    public bool newbieMailUnlocked = false;
    public bool newbieMailClaimed = false;

    public long updatedAtUnix;
}

[Serializable]
public class CloudLeaderboardRecord
{
    public string accountId;
    public string displayName;
    public int elo;
    public int soloWins;
    public int soloLosses;
    public int equippedSkin;
    public long updatedAtUnix;
}

[Serializable]
class LocalAccountDatabase
{
    public List<AccountRecord> accounts = new List<AccountRecord>();
}

public static class AccountDatabaseService
{
    const string KEY_ACTIVE_ACCOUNT_ID = "FlashBall_ActiveAccountId";
    const string DB_FILENAME = "flashball_accounts_db.json";

    static CloudAuthConfig cachedConfig;
    static LocalAccountDatabase cachedDb;
    static AccountRecord activeAccount;
    static bool initialized;
    static bool localMigrationStarted;

    public static bool IsLoggedIn => activeAccount != null && !string.IsNullOrEmpty(activeAccount.accountId);
    public static AccountRecord CurrentAccount => activeAccount;
    public static bool HasCloudDatabase => !string.IsNullOrWhiteSpace(GetConfig().firebaseDatabaseUrl);
    public static string LastCloudError { get; private set; } = "";

    static string DbFilePath => Path.Combine(Application.persistentDataPath, DB_FILENAME);

    public static void ReloadConfig()
    {
        cachedConfig = null;
        GetConfig();
    }

    public static CloudAuthConfig GetConfig()
    {
        if (cachedConfig != null) return cachedConfig;
        TextAsset ta = Resources.Load<TextAsset>("CloudAuthConfig");
        if (ta != null && !string.IsNullOrEmpty(ta.text))
        {
            try
            {
                cachedConfig = JsonUtility.FromJson<CloudAuthConfig>(ta.text);
            }
            catch
            {
                cachedConfig = new CloudAuthConfig();
            }
        }
        if (cachedConfig == null) cachedConfig = new CloudAuthConfig();
        return cachedConfig;
    }

    public static void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        LoadLocalDb();

        string savedId = PlayerPrefs.GetString(KEY_ACTIVE_ACCOUNT_ID, "");
        if (!string.IsNullOrEmpty(savedId))
        {
            AccountRecord found = FindLocalById(savedId);
            if (found != null)
            {
                activeAccount = found;
                PlayerStatsManager.ImportFromAccount(found);
                SyncAllAccountsToLeaderboard();
                _ = PullLatestFromCloudIfConfigured(found.accountId);
            }
            else if (HasCloudDatabase)
            {
                _ = PullSavedAccountByIdFromCloudAsync(savedId);
            }
        }

        if (HasCloudDatabase && !localMigrationStarted)
        {
            localMigrationStarted = true;
            _ = MigrateLocalAccountsToCloudAsync();
            _ = SyncGlobalLeaderboardFromCloudAsync();
        }
    }

    // ================= REGISTER =================

    public static async Task<(bool ok, string message)> RegisterAsync(string username, string password, string displayName)
    {
        EnsureInitialized();

        string cleanUser = NormalizeKey(username);
        string cleanName = PlayerStatsManager.SanitizeName(displayName);
        if (cleanUser.Length < 3)
            return (false, "Tai khoan can it nhat 3 ky tu (chu/so)!");
        if (string.IsNullOrEmpty(password) || password.Length < 4)
            return (false, "Mat khau can it nhat 4 ky tu!");
        if (cleanName.Length < 3)
            return (false, "Ten nhan vat can it nhat 3 ky tu!");

        string accountId = "acc_" + cleanUser;

        if (HasCloudDatabase)
        {
            var lookup = await CloudGetAccountResultAsync(accountId);
            if (!lookup.networkOk)
                return (false, "Khong ket noi duoc Cloud Database! Kiem tra mang internet.");
            if (lookup.record != null && !string.IsNullOrEmpty(lookup.record.accountId))
                return (false, "Tai khoan nay da ton tai tren he thong!");
        }
        else if (FindLocalById(accountId) != null)
        {
            return (false, "Tai khoan nay da ton tai!");
        }

        AccountRecord rec = new AccountRecord
        {
            accountId = accountId,
            username = cleanUser,
            passwordHash = ComputeSha256(password),
            googleEmail = "",
            authProvider = "local",
            displayName = cleanName,
            elo = 100,
            gold = 0,
            soloWins = 0,
            soloLosses = 0,
            matchesPlayed = 0,
            equippedWeapon = (int)WeaponType.WindBlade,
            ownedWeapons = new List<int> { (int)WeaponType.WindBlade },
            equippedSkill = (int)SkillType.CloneBall,
            ownedSkills = new List<int> { (int)SkillType.CloneBall },
            equippedSkin = (int)SkinType.DefaultStriker,
            ownedSkins = new List<int> { (int)SkinType.DefaultStriker },
            tutorialCompleted = false,
            newbieMailUnlocked = false,
            newbieMailClaimed = false,
            updatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        if (HasCloudDatabase)
        {
            bool savedCloud = await CloudPutAccountAsync(rec);
            if (!savedCloud)
                return (false, "Loi luu tai khoan len Cloud Database! Hay thu lai.");
            await CloudPutLeaderboardAsync(BuildLeaderboardRecord(rec));
        }

        UpsertLocal(rec);
        ActivateAccount(rec);
        return (true, HasCloudDatabase ? "Dang ky & dong bo Cloud thanh cong!" : "Dang ky thanh cong (Cuc bo)!");
    }

    // ================= LOGIN (USERNAME + PASSWORD) =================

    public static async Task<(bool ok, string message)> LoginAsync(string username, string password)
    {
        EnsureInitialized();

        string cleanUser = NormalizeKey(username);
        if (cleanUser.Length < 3 || string.IsNullOrEmpty(password))
            return (false, "Vui long nhap day du tai khoan & mat khau!");

        string accountId = "acc_" + cleanUser;
        string hash = ComputeSha256(password);

        AccountRecord found = null;
        bool cloudChecked = false;
        bool cloudReachable = false;

        if (HasCloudDatabase)
        {
            cloudChecked = true;
            var res = await CloudGetAccountResultAsync(accountId);
            cloudReachable = res.networkOk;
            found = res.record;
        }

        AccountRecord localFound = FindLocalById(accountId);
        if (found == null || string.IsNullOrEmpty(found.accountId))
        {
            if (localFound != null)
            {
                found = localFound;
                // Nếu nick có sẵn trên máy PC này mà chưa lên Cloud -> đẩy ngay lên Cloud để điện thoại khác đăng nhập được!
                if (HasCloudDatabase && cloudReachable)
                {
                    await CloudPutAccountAsync(localFound);
                    await CloudPutLeaderboardAsync(BuildLeaderboardRecord(localFound));
                }
            }
            else if (cloudChecked && !cloudReachable)
            {
                return (false, "Khong ket noi duoc Cloud Database! Kiem tra mang internet.");
            }
        }
        else if (localFound != null && localFound.updatedAtUnix > found.updatedAtUnix)
        {
            // Nếu bản local trên máy này mới hơn bản trên Cloud -> cập nhật Cloud cho đồng nhất!
            found = localFound;
            if (HasCloudDatabase && cloudReachable)
            {
                await CloudPutAccountAsync(found);
                await CloudPutLeaderboardAsync(BuildLeaderboardRecord(found));
            }
        }

        if (found == null || string.IsNullOrEmpty(found.accountId))
            return (false, "Khong tim thay tai khoan! Hay kiem tra lai hoac bam DANG KY.");

        if (!string.Equals(found.passwordHash, hash, StringComparison.Ordinal))
            return (false, "Sai mat khau! Vui long kiem tra lai.");

        UpsertLocal(found);
        ActivateAccount(found);
        if (HasCloudDatabase)
        {
            _ = CloudPutLeaderboardAsync(BuildLeaderboardRecord(found));
            _ = SyncGlobalLeaderboardFromCloudAsync();
        }
        return (true, "Dang nhap thanh cong!");
    }

    // ================= GOOGLE SIGN-IN =================

    public static async Task<(bool ok, string message)> SignInWithGoogleEmailAsync(string email, string optionalDisplayName = "")
    {
        EnsureInitialized();

        string cleanEmail = string.IsNullOrWhiteSpace(email) ? "" : email.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(cleanEmail) || !cleanEmail.Contains("@") || !cleanEmail.Contains("."))
            return (false, "Vui long nhap dia chi Gmail hop le!");

        string accountId = "google_" + ComputeSha256(cleanEmail).Substring(0, 16);

        AccountRecord found = null;
        bool cloudReachable = false;
        if (HasCloudDatabase)
        {
            var res = await CloudGetAccountResultAsync(accountId);
            cloudReachable = res.networkOk;
            found = res.record;
            if (!cloudReachable && FindLocalById(accountId) == null)
                return (false, "Khong ket noi duoc Cloud Database! Kiem tra mang internet.");
        }

        AccountRecord localFound = FindLocalById(accountId);
        if (found == null || string.IsNullOrEmpty(found.accountId))
        {
            found = localFound;
            if (found != null && HasCloudDatabase && cloudReachable)
            {
                await CloudPutAccountAsync(found);
                await CloudPutLeaderboardAsync(BuildLeaderboardRecord(found));
            }
        }
        else if (localFound != null && localFound.updatedAtUnix > found.updatedAtUnix)
        {
            found = localFound;
            if (HasCloudDatabase && cloudReachable)
            {
                await CloudPutAccountAsync(found);
            }
        }

        if (found != null && !string.IsNullOrEmpty(found.accountId))
        {
            UpsertLocal(found);
            ActivateAccount(found);
            if (HasCloudDatabase)
            {
                _ = CloudPutLeaderboardAsync(BuildLeaderboardRecord(found));
                _ = SyncGlobalLeaderboardFromCloudAsync();
            }
            return (true, $"Chao mung tro lai, {found.displayName}!");
        }

        string defaultNick = PlayerStatsManager.SanitizeName(optionalDisplayName);
        if (defaultNick.Length < 3)
        {
            int at = cleanEmail.IndexOf('@');
            defaultNick = PlayerStatsManager.SanitizeName(at > 0 ? cleanEmail.Substring(0, at) : "Player");
            if (defaultNick.Length < 3) defaultNick = "Player" + UnityEngine.Random.Range(100, 999);
        }

        AccountRecord created = new AccountRecord
        {
            accountId = accountId,
            username = cleanEmail,
            passwordHash = "",
            googleEmail = cleanEmail,
            authProvider = "google",
            displayName = defaultNick,
            elo = 100,
            gold = 0,
            soloWins = 0,
            soloLosses = 0,
            matchesPlayed = 0,
            equippedWeapon = (int)WeaponType.WindBlade,
            ownedWeapons = new List<int> { (int)WeaponType.WindBlade },
            equippedSkill = (int)SkillType.CloneBall,
            ownedSkills = new List<int> { (int)SkillType.CloneBall },
            equippedSkin = (int)SkinType.DefaultStriker,
            ownedSkins = new List<int> { (int)SkinType.DefaultStriker },
            tutorialCompleted = false,
            newbieMailUnlocked = false,
            newbieMailClaimed = false,
            updatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        if (HasCloudDatabase)
        {
            await CloudPutAccountAsync(created);
            await CloudPutLeaderboardAsync(BuildLeaderboardRecord(created));
        }

        UpsertLocal(created);
        ActivateAccount(created);
        return (true, $"Dang nhap Google thanh cong ({cleanEmail})!");
    }

    public static bool HasRealGoogleOAuthConfigured()
    {
        var cfg = GetConfig();
        return !string.IsNullOrWhiteSpace(cfg.googleOAuthClientId) && !string.IsNullOrWhiteSpace(cfg.googleOAuthClientSecret);
    }

    public static async Task<(bool ok, string message)> SignInWithGoogleBrowserOAuthAsync()
    {
        var cfg = GetConfig();
        if (!HasRealGoogleOAuthConfigured())
            return (false, "USE_EMAIL_PROMPT");

        try
        {
            int port = 49152 + UnityEngine.Random.Range(0, 10000);
            string redirectUri = $"http://127.0.0.1:{port}/";
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(redirectUri);
                listener.Start();

                string authUrl =
                    "https://accounts.google.com/o/oauth2/v2/auth?" +
                    "client_id=" + Uri.EscapeDataString(cfg.googleOAuthClientId) +
                    "&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
                    "&response_type=code" +
                    "&scope=" + Uri.EscapeDataString("openid email profile") +
                    "&prompt=select_account";

                Application.OpenURL(authUrl);

                Task<HttpListenerContext> ctxTask = listener.GetContextAsync();
                Task timeoutTask = Task.Delay(60000);
                Task completed = await Task.WhenAny(ctxTask, timeoutTask);
                if (completed != ctxTask)
                {
                    listener.Stop();
                    return (false, "Het thoi gian dang nhap Google (60s).");
                }

                HttpListenerContext ctx = ctxTask.Result;
                string code = ctx.Request.QueryString["code"];
                byte[] respBytes = Encoding.UTF8.GetBytes("<html><body style='background:#0b1220;color:#fff;font-family:sans-serif;text-align:center;padding:60px'><h2>Dang nhap FlashBall thanh cong!</h2><p>Ban co the dong tab nay va quay lai game.</p></body></html>");
                ctx.Response.ContentLength64 = respBytes.Length;
                ctx.Response.OutputStream.Write(respBytes, 0, respBytes.Length);
                ctx.Response.OutputStream.Close();
                listener.Stop();

                if (string.IsNullOrEmpty(code))
                    return (false, "Ban da huy dang nhap Google.");

                WWWForm form = new WWWForm();
                form.AddField("code", code);
                form.AddField("client_id", cfg.googleOAuthClientId);
                form.AddField("client_secret", cfg.googleOAuthClientSecret);
                form.AddField("redirect_uri", redirectUri);
                form.AddField("grant_type", "authorization_code");

                using (UnityWebRequest tokReq = UnityWebRequest.Post("https://oauth2.googleapis.com/token", form))
                {
                    await SendWebRequestAsync(tokReq);
                    if (tokReq.result != UnityWebRequest.Result.Success)
                        return (false, "Loi xac thuc token Google.");

                    GoogleTokenResp tok = JsonUtility.FromJson<GoogleTokenResp>(tokReq.downloadHandler.text);
                    if (tok == null || string.IsNullOrEmpty(tok.access_token))
                        return (false, "Khong nhan duoc access_token Google.");

                    using (UnityWebRequest infoReq = UnityWebRequest.Get("https://www.googleapis.com/oauth2/v2/userinfo"))
                    {
                        infoReq.SetRequestHeader("Authorization", "Bearer " + tok.access_token);
                        await SendWebRequestAsync(infoReq);
                        if (infoReq.result != UnityWebRequest.Result.Success)
                            return (false, "Khong lay duoc thong tin Google.");

                        GoogleUserInfo info = JsonUtility.FromJson<GoogleUserInfo>(infoReq.downloadHandler.text);
                        return await SignInWithGoogleEmailAsync(info.email, info.name);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Google OAuth fallback: " + ex.Message);
            return (false, "USE_EMAIL_PROMPT");
        }
    }

    [Serializable]
    class GoogleTokenResp { public string access_token; }

    [Serializable]
    class GoogleUserInfo { public string email; public string name; }

    // ================= LOGOUT & SAVE =================

    public static void SaveCurrentProfile()
    {
        _ = SaveCurrentProfileAsync();
    }

    public static async Task<bool> SaveCurrentProfileAsync()
    {
        if (!IsLoggedIn)
        {
            // Ngay cả khi chơi bằng tài khoản Khách, vẫn cập nhật điểm BXH nếu có Cloud DB
            if (HasCloudDatabase && PlayerStatsManager.HasPlayerName)
            {
                return await CloudPutLeaderboardAsync(BuildGuestLeaderboardRecord());
            }
            return false;
        }

        PlayerStatsManager.ExportToAccount(activeAccount);
        activeAccount.updatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        UpsertLocal(activeAccount);
        SyncAllAccountsToLeaderboard();

        if (!HasCloudDatabase)
            return true;

        bool okAcc = await CloudPutAccountAsync(activeAccount);
        bool okLb = await CloudPutLeaderboardAsync(BuildLeaderboardRecord(activeAccount));
        return okAcc && okLb;
    }

    public static void Logout()
    {
        if (IsLoggedIn)
        {
            SaveCurrentProfile();
        }

        activeAccount = null;
        PlayerPrefs.DeleteKey(KEY_ACTIVE_ACCOUNT_ID);
        PlayerPrefs.Save();
    }

    static void ActivateAccount(AccountRecord rec)
    {
        activeAccount = rec;
        PlayerPrefs.SetString(KEY_ACTIVE_ACCOUNT_ID, rec.accountId);
        PlayerPrefs.Save();
        PlayerStatsManager.ImportFromAccount(rec);
        SyncAllAccountsToLeaderboard();
    }

    static void SyncAllAccountsToLeaderboard()
    {
        if (cachedDb == null || cachedDb.accounts == null) return;
        for (int i = 0; i < cachedDb.accounts.Count; i++)
        {
            var a = cachedDb.accounts[i];
            if (a != null && !string.IsNullOrWhiteSpace(a.displayName))
            {
                LeaderboardManager.RecordRealPlayer(a.displayName, a.elo, a.soloWins, a.soloLosses);
            }
        }
    }

    static CloudLeaderboardRecord BuildLeaderboardRecord(AccountRecord rec)
    {
        return new CloudLeaderboardRecord
        {
            accountId = rec.accountId,
            displayName = PlayerStatsManager.SanitizeName(rec.displayName),
            elo = Mathf.Max(0, rec.elo),
            soloWins = Mathf.Max(0, rec.soloWins),
            soloLosses = Mathf.Max(0, rec.soloLosses),
            equippedSkin = rec.equippedSkin,
            updatedAtUnix = rec.updatedAtUnix > 0 ? rec.updatedAtUnix : DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    static CloudLeaderboardRecord BuildGuestLeaderboardRecord()
    {
        string rawId = string.IsNullOrEmpty(SystemInfo.deviceUniqueIdentifier)
            ? PlayerStatsManager.PlayerName
            : SystemInfo.deviceUniqueIdentifier;
        string guestId = "guest_" + ComputeSha256(rawId).Substring(0, 12);
        return new CloudLeaderboardRecord
        {
            accountId = guestId,
            displayName = PlayerStatsManager.PlayerName,
            elo = Mathf.Max(0, PlayerStatsManager.PlayerElo),
            soloWins = Mathf.Max(0, PlayerStatsManager.SoloWins),
            soloLosses = Mathf.Max(0, PlayerStatsManager.SoloLosses),
            equippedSkin = (int)PlayerStatsManager.EquippedSkin,
            updatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    // ================= GLOBAL CLOUD LEADERBOARD & MIGRATION =================

    /// <summary>
    /// Đẩy chỉ số mới nhất của người chơi hiện tại lên Cloud rồi tải toàn bộ BXH toàn cầu về đồng bộ!
    /// </summary>
    public static async Task<(bool ok, int count, string message)> SyncGlobalLeaderboardFromCloudAsync()
    {
        EnsureInitialized();

        if (!HasCloudDatabase)
        {
            SyncAllAccountsToLeaderboard();
            return (false, 0, "Chua cau hinh Firebase URL trong CloudAuthConfig.json");
        }

        // 1. Đẩy điểm mới nhất của chính mình lên Cloud trước
        if (IsLoggedIn)
        {
            await SaveCurrentProfileAsync();
            await PullLatestFromCloudIfConfigured(activeAccount.accountId);
        }
        else if (PlayerStatsManager.HasPlayerName)
        {
            await CloudPutLeaderboardAsync(BuildGuestLeaderboardRecord());
        }

        // 2. Tải toàn bộ danh sách /leaderboard.json từ Cloud về
        string url = BuildCloudUrl("leaderboard");
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = 10;
            await SendWebRequestAsync(req);
            if (req.result != UnityWebRequest.Result.Success)
            {
                LastCloudError = req.error;
                return (false, 0, "Khong ket noi duoc Cloud BXH! Kiem tra mang.");
            }

            string body = req.downloadHandler.text;
            List<CloudLeaderboardRecord> list = ParseFirebaseObjectMap<CloudLeaderboardRecord>(body);
            LeaderboardManager.ApplyCloudPlayers(list);
            return (true, list.Count, $"Da dong bo BXH toan cau ({list.Count} nguoi choi)!");
        }
    }

    /// <summary>
    /// Tự động đồng bộ các tài khoản cũ đã tạo trên máy PC lên Cloud ngay khi bật Cloud Database
    /// để điện thoại/thiết bị khác đăng nhập được ngay bằng nick có sẵn!
    /// </summary>
    static async Task MigrateLocalAccountsToCloudAsync()
    {
        if (!HasCloudDatabase || cachedDb == null || cachedDb.accounts == null || cachedDb.accounts.Count == 0)
            return;

        List<AccountRecord> snapshot = new List<AccountRecord>(cachedDb.accounts);
        for (int i = 0; i < snapshot.Count; i++)
        {
            AccountRecord localRec = snapshot[i];
            if (localRec == null || string.IsNullOrEmpty(localRec.accountId))
                continue;

            var remoteRes = await CloudGetAccountResultAsync(localRec.accountId);
            if (!remoteRes.networkOk)
                return; // mạng lỗi -> dừng sớm để tránh spam

            if (remoteRes.record == null || string.IsNullOrEmpty(remoteRes.record.accountId) || localRec.updatedAtUnix > remoteRes.record.updatedAtUnix)
            {
                await CloudPutAccountAsync(localRec);
                await CloudPutLeaderboardAsync(BuildLeaderboardRecord(localRec));
            }
            else if (remoteRes.record.updatedAtUnix > localRec.updatedAtUnix)
            {
                UpsertLocal(remoteRes.record);
                if (activeAccount != null && activeAccount.accountId == remoteRes.record.accountId)
                {
                    activeAccount = remoteRes.record;
                    PlayerStatsManager.ImportFromAccount(remoteRes.record);
                }
            }
        }
    }

    // ================= LOCAL JSON DB =================

    static void LoadLocalDb()
    {
        if (File.Exists(DbFilePath))
        {
            try
            {
                string json = File.ReadAllText(DbFilePath, Encoding.UTF8);
                cachedDb = JsonUtility.FromJson<LocalAccountDatabase>(json);
            }
            catch
            {
                cachedDb = new LocalAccountDatabase();
            }
        }
        if (cachedDb == null) cachedDb = new LocalAccountDatabase();
        if (cachedDb.accounts == null) cachedDb.accounts = new List<AccountRecord>();
    }

    static void SaveLocalDb()
    {
        if (cachedDb == null) return;
        try
        {
            File.WriteAllText(DbFilePath, JsonUtility.ToJson(cachedDb, true), Encoding.UTF8);
        }
        catch (Exception e)
        {
            Debug.LogWarning("SaveLocalDb warning: " + e.Message);
        }
    }

    static AccountRecord FindLocalById(string id)
    {
        if (cachedDb == null) LoadLocalDb();
        for (int i = 0; i < cachedDb.accounts.Count; i++)
        {
            if (cachedDb.accounts[i] != null && cachedDb.accounts[i].accountId == id)
                return cachedDb.accounts[i];
        }
        return null;
    }

    static void UpsertLocal(AccountRecord rec)
    {
        if (cachedDb == null) LoadLocalDb();
        for (int i = 0; i < cachedDb.accounts.Count; i++)
        {
            if (cachedDb.accounts[i] != null && cachedDb.accounts[i].accountId == rec.accountId)
            {
                cachedDb.accounts[i] = rec;
                SaveLocalDb();
                return;
            }
        }
        cachedDb.accounts.Add(rec);
        SaveLocalDb();
    }

    // ================= CLOUD FIREBASE REST SYNC =================

    static string BuildCloudUrl(string relativePath)
    {
        var cfg = GetConfig();
        string baseUrl = (cfg.firebaseDatabaseUrl ?? "").Trim().TrimEnd('/');
        string url = $"{baseUrl}/{relativePath.TrimStart('/')}.json";
        if (!string.IsNullOrWhiteSpace(cfg.databaseAuthSecret))
        {
            url += "?auth=" + Uri.EscapeDataString(cfg.databaseAuthSecret.Trim());
        }
        return url;
    }

    static async Task PullSavedAccountByIdFromCloudAsync(string accountId)
    {
        var res = await CloudGetAccountResultAsync(accountId);
        if (res.networkOk && res.record != null && !string.IsNullOrEmpty(res.record.accountId))
        {
            UpsertLocal(res.record);
            ActivateAccount(res.record);
            MenuUI menu = UnityEngine.Object.FindFirstObjectByType<MenuUI>();
            if (menu != null) menu.RefreshPlayerStats();
        }
    }

    static async Task PullLatestFromCloudIfConfigured(string accountId)
    {
        if (!HasCloudDatabase) return;
        var res = await CloudGetAccountResultAsync(accountId);
        if (res.networkOk && res.record != null && !string.IsNullOrEmpty(res.record.accountId))
        {
            if (activeAccount != null && activeAccount.accountId == accountId && res.record.updatedAtUnix > activeAccount.updatedAtUnix)
            {
                activeAccount = res.record;
                UpsertLocal(res.record);
                PlayerStatsManager.ImportFromAccount(res.record);
                MenuUI menu = UnityEngine.Object.FindFirstObjectByType<MenuUI>();
                if (menu != null) menu.RefreshPlayerStats();
            }
        }
    }

    static async Task<(bool networkOk, AccountRecord record)> CloudGetAccountResultAsync(string accountId)
    {
        string url = BuildCloudUrl("accounts/" + accountId);
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = 8;
            await SendWebRequestAsync(req);
            if (req.result != UnityWebRequest.Result.Success)
            {
                LastCloudError = req.error;
                return (false, null);
            }

            string body = req.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(body) || body.Trim() == "null")
                return (true, null);

            try
            {
                return (true, JsonUtility.FromJson<AccountRecord>(body));
            }
            catch
            {
                return (true, null);
            }
        }
    }

    static async Task<bool> CloudPutAccountAsync(AccountRecord rec)
    {
        string url = BuildCloudUrl("accounts/" + rec.accountId);
        byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(rec));

        using (UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT))
        {
            req.uploadHandler = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 8;
            await SendWebRequestAsync(req);
            if (req.result != UnityWebRequest.Result.Success)
            {
                LastCloudError = req.error;
                return false;
            }
            return true;
        }
    }

    static async Task<bool> CloudPutLeaderboardAsync(CloudLeaderboardRecord rec)
    {
        if (rec == null || string.IsNullOrEmpty(rec.accountId))
            return false;

        string url = BuildCloudUrl("leaderboard/" + rec.accountId);
        byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(rec));

        using (UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT))
        {
            req.uploadHandler = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 8;
            await SendWebRequestAsync(req);
            return req.result == UnityWebRequest.Result.Success;
        }
    }

    /// <summary>
    /// Phân tách JSON dạng dictionary của Firebase ({"id1":{...},"id2":{...}}) thành List&lt;T&gt; thuần C# (tương thích 100% IL2CPP Android/iOS).
    /// </summary>
    public static List<T> ParseFirebaseObjectMap<T>(string json) where T : class
    {
        var results = new List<T>();
        if (string.IsNullOrWhiteSpace(json))
            return results;

        string trimmed = json.Trim();
        if (trimmed == "null" || trimmed.Length < 2 || trimmed[0] != '{')
            return results;

        int depth = 0;
        bool inString = false;
        bool escape = false;
        int objStart = -1;

        for (int i = 0; i < trimmed.Length; i++)
        {
            char c = trimmed[i];
            if (escape)
            {
                escape = false;
                continue;
            }
            if (c == '\\' && inString)
            {
                escape = true;
                continue;
            }
            if (c == '"')
            {
                inString = !inString;
                continue;
            }
            if (inString)
                continue;

            if (c == '{')
            {
                depth++;
                if (depth == 2)
                {
                    objStart = i;
                }
            }
            else if (c == '}')
            {
                if (depth == 2 && objStart >= 0)
                {
                    string itemJson = trimmed.Substring(objStart, i - objStart + 1);
                    objStart = -1;
                    try
                    {
                        T parsed = JsonUtility.FromJson<T>(itemJson);
                        if (parsed != null)
                            results.Add(parsed);
                    }
                    catch
                    {
                        // Bỏ qua bản ghi lỗi định dạng
                    }
                }
                depth--;
            }
        }

        return results;
    }

    static Task SendWebRequestAsync(UnityWebRequest req)
    {
        var tcs = new TaskCompletionSource<bool>();
        var op = req.SendWebRequest();
        op.completed += _ => tcs.TrySetResult(true);
        return tcs.Task;
    }

    // ================= HELPERS =================

    static string NormalizeKey(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        StringBuilder sb = new StringBuilder();
        foreach (char c in raw.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c == '_')
                sb.Append(c);
        }
        return sb.ToString();
    }

    static string ComputeSha256(string input)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? ""));
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                sb.Append(bytes[i].ToString("x2"));
            return sb.ToString();
        }
    }
}
