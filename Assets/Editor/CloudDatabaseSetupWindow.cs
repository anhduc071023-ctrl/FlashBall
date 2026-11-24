#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Cửa sổ cấu hình 1-click Firebase Realtime Database URL giúp đồng bộ Tài Khoản + Tiến Trình + Bảng Xếp Hạng
/// giữa máy tính (PC) và tất cả điện thoại Android/iOS!
/// </summary>
public class CloudDatabaseSetupWindow : EditorWindow
{
    const string ConfigAssetPath = "Assets/Resources/CloudAuthConfig.json";

    CloudAuthConfig cfg;
    string testStatus = "";
    MessageType testMsgType = MessageType.Info;

    [MenuItem("Tools/FlashBall/Cau Hinh Cloud Database (PC & Dien Thoai)")]
    public static void ShowWindow()
    {
        var w = GetWindow<CloudDatabaseSetupWindow>("FlashBall Cloud DB");
        w.minSize = new Vector2(520f, 390f);
        w.LoadConfig();
    }

    void OnEnable()
    {
        LoadConfig();
    }

    void LoadConfig()
    {
        if (File.Exists(ConfigAssetPath))
        {
            try
            {
                cfg = JsonUtility.FromJson<CloudAuthConfig>(File.ReadAllText(ConfigAssetPath, Encoding.UTF8));
            }
            catch
            {
                cfg = new CloudAuthConfig();
            }
        }
        if (cfg == null) cfg = new CloudAuthConfig();
    }

    void OnGUI()
    {
        GUILayout.Space(8f);
        EditorGUILayout.LabelField("DONG BO TAI KHOAN & BXH LIEN THIET BI (FIREBASE REALTIME DB)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "De nick tao tren PC co the dang nhap duoc tren dien thoai ban be va dong bo BXH toan cau:\n" +
            "1. Vao https://console.firebase.google.com -> Tao Project (mien phi) -> Build -> Realtime Database -> Create Database.\n" +
            "2. Trong tab Rules, dat .read = true, .write = true (hoac chi cho phep /accounts va /leaderboard).\n" +
            "3. Copy link dang: https://ten-project-default-rtdb.asia-southeast1.firebasedatabase.app\n" +
            "4. Dan vao o ben duoi va bam LUU & KIEM TRA KET NOI, sau do Build lai APK!",
            MessageType.Info
        );

        GUILayout.Space(8f);
        cfg.firebaseDatabaseUrl = EditorGUILayout.TextField("Firebase Database URL", cfg.firebaseDatabaseUrl);
        cfg.databaseAuthSecret = EditorGUILayout.TextField("Database Secret (tuy chon)", cfg.databaseAuthSecret);
        GUILayout.Space(6f);
        cfg.googleOAuthClientId = EditorGUILayout.TextField("Google OAuth Client ID (tuy chon)", cfg.googleOAuthClientId);
        cfg.googleOAuthClientSecret = EditorGUILayout.TextField("Google OAuth Secret (tuy chon)", cfg.googleOAuthClientSecret);

        GUILayout.Space(12f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Mo Firebase Console", GUILayout.Height(32f)))
        {
            Application.OpenURL("https://console.firebase.google.com/");
        }
        if (GUILayout.Button("LUU & KIEM TRA KET NOI", GUILayout.Height(32f)))
        {
            SaveAndTestConnection();
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(testStatus))
        {
            GUILayout.Space(10f);
            EditorGUILayout.HelpBox(testStatus, testMsgType);
        }
    }

    void SaveAndTestConnection()
    {
        cfg.firebaseDatabaseUrl = (cfg.firebaseDatabaseUrl ?? "").Trim().TrimEnd('/');
        cfg.databaseAuthSecret = (cfg.databaseAuthSecret ?? "").Trim();

        Directory.CreateDirectory(Path.GetDirectoryName(ConfigAssetPath));
        File.WriteAllText(ConfigAssetPath, JsonUtility.ToJson(cfg, true), Encoding.UTF8);
        AssetDatabase.Refresh();
        AccountDatabaseService.ReloadConfig();

        if (string.IsNullOrWhiteSpace(cfg.firebaseDatabaseUrl))
        {
            testStatus = "Da luu (URL dang trong -> che do cuc bo). Hay dan Firebase Realtime Database URL de bat dong bo lien thiet bi!";
            testMsgType = MessageType.Warning;
            return;
        }

        string pingUrl = $"{cfg.firebaseDatabaseUrl}/_flashball_ping.json";
        if (!string.IsNullOrEmpty(cfg.databaseAuthSecret))
        {
            pingUrl += "?auth=" + Uri.EscapeDataString(cfg.databaseAuthSecret);
        }

        try
        {
            string payload = "{\"ok\":true,\"ts\":" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "}";
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            using (UnityWebRequest req = new UnityWebRequest(pingUrl, UnityWebRequest.kHttpVerbPUT))
            {
                req.uploadHandler = new UploadHandlerRaw(bytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 8;
                var op = req.SendWebRequest();
                while (!op.isDone) { }

                if (req.result == UnityWebRequest.Result.Success)
                {
                    testStatus = "KET NOI CLOUD DATABASE THANH CONG! Tu gio moi tai khoan & BXH tren PC va Dien Thoai se tu dong dong bo voi nhau!";
                    testMsgType = MessageType.Info;
                }
                else
                {
                    testStatus = $"Ket noi that bai ({req.responseCode}): {req.error}\nHay kiem tra lai URL va tab Rules (.read/.write = true) trong Firebase Realtime Database!";
                    testMsgType = MessageType.Error;
                }
            }
        }
        catch (Exception ex)
        {
            testStatus = "Loi kiem tra ket noi: " + ex.Message;
            testMsgType = MessageType.Error;
        }
    }
}
#endif
