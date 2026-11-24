using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public struct RankLeaderboardEntry
{
    public int position;
    public string playerName;
    public int elo;
    public bool isLocalPlayer;
    public bool isRealPlayer;
}

public struct SoloLeaderboardEntry
{
    public int position;
    public string playerName;
    public int wins;
    public int losses;
    public bool isLocalPlayer;
    public bool isRealPlayer;
}

[Serializable]
class RealPlayerRecord
{
    public string accountId;
    public string name;
    public int elo;
    public int wins;
    public int losses;
    public long updatedAtUnix;
}

[Serializable]
class RealPlayerRecordList
{
    public List<RealPlayerRecord> items = new List<RealPlayerRecord>();
}

public static class LeaderboardManager
{
    const string KEY_REAL_PLAYERS = "FlashBall_RealPlayersLeaderboard_V1";
    const int MaxSavedRealPlayers = 100;

    static readonly (string name, int elo)[] SeedRankRivals = new (string, int)[]
    {
        ("KuroBlade", 1320),
        ("ZephX",     1210),
        ("Valkyrie",  1160),
        ("AegisVN",   1040),
        ("NovaSlash",  920),
        ("Ryujin",     815),
        ("FrostKira",  710),
        ("SoraFlash",  590),
        ("Blaze99",    480),
        ("KaitoPro",   390),
        ("ShadowMin",  305),
        ("TornadoKid", 230),
        ("HuyParry",   170),
        ("NamBlade",   115),
        ("RookieZero",  60)
    };

    static readonly (string name, int wins, int losses)[] SeedSoloRivals = new (string, int, int)[]
    {
        ("KuroBlade", 42, 7),
        ("ZephX",     35, 9),
        ("Ryujin",    29, 10),
        ("Valkyrie",  24, 8),
        ("AegisVN",   20, 11),
        ("NovaSlash", 17, 9),
        ("FrostKira", 14, 8),
        ("SoraFlash", 12, 10),
        ("Blaze99",   10, 9),
        ("KaitoPro",   8, 7),
        ("ShadowMin",  6, 6),
        ("TornadoKid", 4, 5),
        ("HuyParry",   3, 4),
        ("NamBlade",   2, 4),
        ("RookieZero", 1, 3)
    };

    static RealPlayerRecordList LoadRealPlayers()
    {
        string json = PlayerPrefs.GetString(KEY_REAL_PLAYERS, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                RealPlayerRecordList parsed = JsonUtility.FromJson<RealPlayerRecordList>(json);
                if (parsed != null && parsed.items != null)
                    return parsed;
            }
            catch
            {
                // fallback to empty list
            }
        }
        return new RealPlayerRecordList();
    }

    static void SaveRealPlayers(RealPlayerRecordList data)
    {
        if (data == null) return;
        data.items.Sort((a, b) => b.elo.CompareTo(a.elo));
        if (data.items.Count > MaxSavedRealPlayers)
        {
            data.items.RemoveRange(MaxSavedRealPlayers, data.items.Count - MaxSavedRealPlayers);
        }
        PlayerPrefs.SetString(KEY_REAL_PLAYERS, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Nhận toàn bộ danh sách người chơi thật từ Cloud Database và đồng bộ vào BXH local.
    /// </summary>
    public static void ApplyCloudPlayers(List<CloudLeaderboardRecord> cloudRecords)
    {
        if (cloudRecords == null) return;

        RealPlayerRecordList data = LoadRealPlayers();
        for (int i = 0; i < cloudRecords.Count; i++)
        {
            var cr = cloudRecords[i];
            if (cr == null) continue;
            string cleanName = PlayerStatsManager.SanitizeName(cr.displayName);
            if (string.IsNullOrEmpty(cleanName) || cleanName.StartsWith("Bot_", StringComparison.OrdinalIgnoreCase))
                continue;

            RealPlayerRecord existing = null;
            for (int j = 0; j < data.items.Count; j++)
            {
                bool sameId = !string.IsNullOrEmpty(cr.accountId) &&
                              string.Equals(data.items[j].accountId, cr.accountId, StringComparison.OrdinalIgnoreCase);
                bool sameName = string.Equals(data.items[j].name, cleanName, StringComparison.OrdinalIgnoreCase);
                if (sameId || sameName)
                {
                    existing = data.items[j];
                    break;
                }
            }

            if (existing != null)
            {
                if (!string.IsNullOrEmpty(cr.accountId)) existing.accountId = cr.accountId;
                existing.name = cleanName;
                existing.elo = Mathf.Max(0, cr.elo);
                existing.wins = Mathf.Max(0, cr.soloWins);
                existing.losses = Mathf.Max(0, cr.soloLosses);
                existing.updatedAtUnix = Math.Max(existing.updatedAtUnix, cr.updatedAtUnix);
            }
            else
            {
                data.items.Add(new RealPlayerRecord
                {
                    accountId = cr.accountId ?? "",
                    name = cleanName,
                    elo = Mathf.Max(0, cr.elo),
                    wins = Mathf.Max(0, cr.soloWins),
                    losses = Mathf.Max(0, cr.soloLosses),
                    updatedAtUnix = cr.updatedAtUnix
                });
            }
        }

        SaveRealPlayers(data);
    }

    /// <summary>
    /// Nút CẬP NHẬT trên BXH gọi hàm này: đẩy điểm mới nhất của mình + tải BXH toàn cầu từ Cloud Database!
    /// </summary>
    public static async Task<(bool cloudSynced, int realPlayerCount, string statusMessage)> RefreshFromCloudAsync()
    {
        RecordLocalPlayerSnapshot();

        if (!AccountDatabaseService.HasCloudDatabase)
        {
            var localList = LoadRealPlayers();
            return (false, localList.items.Count, "Da cap nhat cuc bo (Chua dien Firebase URL trong CloudAuthConfig.json)");
        }

        var res = await AccountDatabaseService.SyncGlobalLeaderboardFromCloudAsync();
        RecordLocalPlayerSnapshot();
        return (res.ok, res.count, res.message);
    }

    public static void RecordRealPlayer(string rawName, int elo, int soloWins, int soloLosses)
    {
        string clean = PlayerStatsManager.SanitizeName(rawName);
        if (string.IsNullOrEmpty(clean) || clean.StartsWith("Bot_", StringComparison.OrdinalIgnoreCase))
            return;

        RealPlayerRecordList data = LoadRealPlayers();
        RealPlayerRecord existing = null;
        for (int i = 0; i < data.items.Count; i++)
        {
            if (string.Equals(data.items[i].name, clean, StringComparison.OrdinalIgnoreCase))
            {
                existing = data.items[i];
                break;
            }
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (existing != null)
        {
            existing.name = clean;
            existing.elo = Mathf.Max(0, elo);
            existing.wins = Mathf.Max(0, soloWins);
            existing.losses = Mathf.Max(0, soloLosses);
            existing.updatedAtUnix = now;
        }
        else
        {
            data.items.Add(new RealPlayerRecord
            {
                accountId = "",
                name = clean,
                elo = Mathf.Max(0, elo),
                wins = Mathf.Max(0, soloWins),
                losses = Mathf.Max(0, soloLosses),
                updatedAtUnix = now
            });
        }

        SaveRealPlayers(data);
    }

    public static void RecordLocalPlayerSnapshot()
    {
        if (!PlayerStatsManager.HasPlayerName)
            return;

        RecordRealPlayer(
            PlayerStatsManager.PlayerName,
            PlayerStatsManager.PlayerElo,
            PlayerStatsManager.SoloWins,
            PlayerStatsManager.SoloLosses
        );
    }

    public static List<RankLeaderboardEntry> GetRankLeaderboard(out RankLeaderboardEntry localEntry)
    {
        RecordLocalPlayerSnapshot();

        string myName = PlayerStatsManager.PlayerName;
        int myElo = PlayerStatsManager.PlayerElo;

        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<RankLeaderboardEntry>();

        list.Add(new RankLeaderboardEntry
        {
            playerName = myName,
            elo = myElo,
            isLocalPlayer = true,
            isRealPlayer = true
        });
        usedNames.Add(myName);

        RealPlayerRecordList saved = LoadRealPlayers();
        for (int i = 0; i < saved.items.Count; i++)
        {
            var r = saved.items[i];
            if (string.IsNullOrEmpty(r.name) || usedNames.Contains(r.name))
                continue;

            list.Add(new RankLeaderboardEntry
            {
                playerName = r.name,
                elo = r.elo,
                isLocalPlayer = false,
                isRealPlayer = true
            });
            usedNames.Add(r.name);
        }

        for (int i = 0; i < SeedRankRivals.Length; i++)
        {
            if (usedNames.Contains(SeedRankRivals[i].name))
                continue;

            list.Add(new RankLeaderboardEntry
            {
                playerName = SeedRankRivals[i].name,
                elo = SeedRankRivals[i].elo,
                isLocalPlayer = false,
                isRealPlayer = false
            });
            usedNames.Add(SeedRankRivals[i].name);
        }

        list.Sort((a, b) =>
        {
            int cmp = b.elo.CompareTo(a.elo);
            if (cmp != 0) return cmp;
            return b.isLocalPlayer.CompareTo(a.isLocalPlayer);
        });

        localEntry = default;
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            e.position = i + 1;
            list[i] = e;
            if (e.isLocalPlayer) localEntry = e;
        }

        return list;
    }

    public static List<SoloLeaderboardEntry> GetSoloLeaderboard(out SoloLeaderboardEntry localEntry)
    {
        RecordLocalPlayerSnapshot();

        string myName = PlayerStatsManager.PlayerName;
        int myWins = PlayerStatsManager.SoloWins;
        int myLosses = PlayerStatsManager.SoloLosses;

        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<SoloLeaderboardEntry>();

        list.Add(new SoloLeaderboardEntry
        {
            playerName = myName,
            wins = myWins,
            losses = myLosses,
            isLocalPlayer = true,
            isRealPlayer = true
        });
        usedNames.Add(myName);

        RealPlayerRecordList saved = LoadRealPlayers();
        for (int i = 0; i < saved.items.Count; i++)
        {
            var r = saved.items[i];
            if (string.IsNullOrEmpty(r.name) || usedNames.Contains(r.name))
                continue;

            list.Add(new SoloLeaderboardEntry
            {
                playerName = r.name,
                wins = r.wins,
                losses = r.losses,
                isLocalPlayer = false,
                isRealPlayer = true
            });
            usedNames.Add(r.name);
        }

        for (int i = 0; i < SeedSoloRivals.Length; i++)
        {
            if (usedNames.Contains(SeedSoloRivals[i].name))
                continue;

            list.Add(new SoloLeaderboardEntry
            {
                playerName = SeedSoloRivals[i].name,
                wins = SeedSoloRivals[i].wins,
                losses = SeedSoloRivals[i].losses,
                isLocalPlayer = false,
                isRealPlayer = false
            });
            usedNames.Add(SeedSoloRivals[i].name);
        }

        list.Sort((a, b) =>
        {
            int cmp = b.wins.CompareTo(a.wins);
            if (cmp != 0) return cmp;
            int cmpLoss = a.losses.CompareTo(b.losses);
            if (cmpLoss != 0) return cmpLoss;
            return b.isLocalPlayer.CompareTo(a.isLocalPlayer);
        });

        localEntry = default;
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            e.position = i + 1;
            list[i] = e;
            if (e.isLocalPlayer) localEntry = e;
        }

        return list;
    }
}
