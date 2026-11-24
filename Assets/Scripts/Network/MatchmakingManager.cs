using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchmakingManager :
    MonoBehaviour,
    INetworkRunnerCallbacks
{
    public static MatchmakingManager Instance;

    [Header("Network")]
    public NetworkRunner runnerPrefab;

    [Header("Player")]
    public NetworkObject playerPrefab;

    [System.NonSerialized]
    [HideInInspector]
    public NetworkRunner runner;

    [Header("Online Mode")]
    public bool useDedicatedServerForPublicMatches = true;

    public bool allowHostFallbackInEditor = true;

    public bool allowAutoHostFallbackInBuilds = true;

    public bool matchRankByEloBucket = false;

    public bool autoStartDedicatedServerInBatchMode = true;

    public string dedicatedServerSessionName =
        "RankRoom";

    public int dedicatedServerPlayerCount = 4;

    public float fallbackStartDelay = 12f;

    public float editorFallbackStartDelay = 5f;

    public int minimumPlayersToStart = 2;

    public bool fillEmptySlotsWithBots = true;

    public static int TargetMatchPlayers { get; private set; } = 2;

    public static bool AutoPlayLocalClient { get; private set; }

    public static GameMatchMode CurrentMatchMode { get; private set; } = GameMatchMode.Rank;

    public static int CustomBotCount { get; set; } = 0;

    public const int MaxCustomPlayers = 8;

    public static string CurrentCustomRoomCode { get; private set; } = "";

    public static string CustomStatusError { get; private set; } = "";

    bool isDedicatedServerInstance;
    bool restartingDedicatedServer;
    bool customJoinOnly;

    // ================= RANK =================

    [Header("Rank")]
    public int playerElo = 1500;

    // ================= SCENES =================

    const int MENU_SCENE = 0;
    const int LOADING_SCENE = 1;
    const int GAMEPLAY_SCENE = 2;

    // ================= STATES =================

    public static bool SceneLoaded;

    bool isStarting;
    bool shuttingDown;

    bool gameplayLoading;
    bool gameplayStarted;

    int targetPlayers;

    string currentRoomName;

    bool allowHostFallback;

    readonly Dictionary<PlayerRef, int> spawnIndexes =
        new Dictionary<PlayerRef, int>();

    // ================= INIT =================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            runner = null;
            playerElo = PlayerStatsManager.PlayerElo;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (
            Application.isBatchMode &&
            autoStartDedicatedServerInBatchMode
        )
        {
            ApplyServerCommandLineArgs();

            StartDedicatedServer();
            return;
        }

        CheckAutoClientArgs();
    }

    void CheckAutoClientArgs()
    {
        string[] args =
            System.Environment.GetCommandLineArgs();

        string autoMode = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-autoBot")
            {
                AutoPlayLocalClient = true;
            }

            if (
                args[i] == "-autoJoin" &&
                i + 1 < args.Length
            )
            {
                autoMode = args[i + 1].ToLowerInvariant();
            }
        }

        if (!string.IsNullOrEmpty(autoMode))
        {
            StartCoroutine(AutoJoinAfterDelay(autoMode));
        }
    }

    IEnumerator AutoJoinAfterDelay(string mode)
    {
        yield return new WaitForSeconds(0.5f);

        MenuUI menu = FindFirstObjectByType<MenuUI>();

        switch (mode)
        {
            case "rank":
                if (menu != null) menu.PlayRank();
                else StartRankMatch();
                break;

            case "solo":
                if (menu != null) menu.PlaySolo();
                else StartSoloMatch();
                break;

            case "custom":
                if (menu != null) menu.PlayCustom();
                else StartCustomMatch();
                break;

            case "training":
                if (menu != null) menu.PlayTraining();
                else StartTraining();
                break;
        }
    }

    void ApplyServerCommandLineArgs()
    {
        string[] args =
            System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            if (
                (args[i] == "-sessionName" || args[i] == "-room") &&
                i + 1 < args.Length
            )
            {
                dedicatedServerSessionName =
                    args[i + 1];
            }

            if (
                args[i] == "-playerCount" &&
                i + 1 < args.Length &&
                int.TryParse(
                    args[i + 1],
                    out int parsedCount
                )
            )
            {
                dedicatedServerPlayerCount =
                    Mathf.Max(
                        1,
                        parsedCount
                    );
            }

            if (
                args[i] == "-fallbackStartDelay" &&
                i + 1 < args.Length &&
                float.TryParse(
                    args[i + 1],
                    out float parsedDelay
                )
            )
            {
                fallbackStartDelay =
                    Mathf.Max(
                        0f,
                        parsedDelay
                    );
            }
        }
    }

    public void StartDedicatedServer()
    {
        if (runner != null || isStarting)
            return;

        isDedicatedServerInstance = true;
        restartingDedicatedServer = false;
        isStarting = true;
        SceneLoaded = false;
        gameplayLoading = false;
        gameplayStarted = false;
        targetPlayers = dedicatedServerPlayerCount;
        TargetMatchPlayers = Mathf.Max(2, dedicatedServerPlayerCount);
        spawnIndexes.Clear();

        StartCoroutine(
            DelayedStartServer()
        );
    }

    IEnumerator DelayedStartServer()
    {
        yield return null;
        yield return null;

        StartServerGame();
    }

    static void ParseServerModeAndBucket(
        string rawSessionName,
        out string modeKey,
        out string bucket
    )
    {
        string lower = string.IsNullOrEmpty(rawSessionName)
            ? "rankroom_silver"
            : rawSessionName.ToLowerInvariant();

        if (lower.Contains("solo"))
            modeKey = "solo";
        else if (lower.Contains("custom"))
            modeKey = "custom";
        else if (lower.Contains("train"))
            modeKey = "training";
        else
            modeKey = "rank";

        if (lower.Contains("bronze"))
            bucket = "bronze";
        else if (lower.Contains("gold"))
            bucket = "gold";
        else
            bucket = "silver";
    }

    async void StartServerGame()
    {
        runner =
            Instantiate(
                runnerPrefab
            );

        runner.name =
            "DedicatedNetworkRunner";

        runner.ProvideInput = false;

        DontDestroyOnLoad(
            runner.gameObject
        );

        runner.AddCallbacks(this);

        var sceneManager =
            runner.gameObject
            .AddComponent
            <NetworkSceneManagerDefault>();

        ParseServerModeAndBucket(
            dedicatedServerSessionName,
            out string modeKey,
            out string bucket
        );

        string suffix = System.Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
        string uniqueSessionName = dedicatedServerSessionName + "_" + suffix;

        var props = new Dictionary<string, SessionProperty>
        {
            { "mode", modeKey }
        };

        if (matchRankByEloBucket && modeKey == "rank")
        {
            props["rank"] = bucket;
        }

        var result =
            await runner.StartGame(
                new StartGameArgs()
                {
                    GameMode =
                        GameMode.Server,

                    SessionName =
                        uniqueSessionName,

                    PlayerCount =
                        dedicatedServerPlayerCount,

                    IsVisible = true,
                    IsOpen = true,

                    SessionProperties = props,

                    Scene =
                        SceneRef.FromIndex(
                            MENU_SCENE
                        ),

                    SceneManager =
                        sceneManager
                });

        isStarting = false;

        if (!result.Ok)
        {
            Debug.LogError(
                "Dedicated Server Start Failed: " +
                result.ShutdownReason
            );

            if (runner != null)
            {
                await runner.Shutdown();
            }

            runner = null;
        }
        else
        {
            string rankTag = (matchRankByEloBucket && modeKey == "rank") ? bucket : "ALL";
            Debug.Log(
                $"Dedicated Server READY: session={uniqueSessionName} mode={modeKey} rank={rankTag} maxPlayers={dedicatedServerPlayerCount}"
            );
        }
    }

    public void NotifyDedicatedServerMatchEnded()
    {
        if (!isDedicatedServerInstance || restartingDedicatedServer)
            return;

        StartCoroutine(RestartDedicatedServerDelayed(6f));
    }

    IEnumerator RestartDedicatedServerDelayed(float delay)
    {
        if (restartingDedicatedServer)
            yield break;

        restartingDedicatedServer = true;

        float elapsed = 0f;
        while (elapsed < delay)
        {
            if (runner == null || !runner.IsRunning || runner.ActivePlayers.Count() == 0)
                break;

            elapsed += 0.25f;
            yield return new WaitForSeconds(0.25f);
        }

        RestartDedicatedServerNow();
    }

    async void RestartDedicatedServerNow()
    {
        Debug.Log("Dedicated Server resetting session for a brand-new match...");

        if (runner != null)
        {
            await runner.Shutdown();
            runner = null;
        }

        StopAllCoroutines();
        spawnIndexes.Clear();
        SceneLoaded = false;
        gameplayLoading = false;
        gameplayStarted = false;
        isStarting = false;
        shuttingDown = false;
        restartingDedicatedServer = false;

        SceneManager.LoadScene(MENU_SCENE);

        StartDedicatedServer();
    }

    // ================= RANK =================

    string GetRankBucket()
    {
        return PlayerStatsManager.GetRankBucket(playerElo);
    }

    // ================= START =================

    public static string GenerateRoomCode()
    {
        return Random.Range(1000, 10000).ToString();
    }

    public static string NormalizeRoomCode(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (char c in raw.Trim())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToUpperInvariant(c));
                if (sb.Length >= 8)
                    break;
            }
        }
        return sb.ToString();
    }

    public void StartRankMatch()
    {
        CurrentMatchMode = GameMatchMode.Rank;
        playerElo = PlayerStatsManager.PlayerElo;
        CustomBotCount = 0;
        customJoinOnly = false;
        CustomStatusError = "";
        StartFlow("RankRoom", 4, false);
    }

    public void StartSoloMatch()
    {
        CurrentMatchMode = GameMatchMode.Solo;
        CustomBotCount = 0;
        customJoinOnly = false;
        CustomStatusError = "";
        StartFlow("SoloRoom", 2, false);
    }

    public void StartTraining()
    {
        CurrentMatchMode = GameMatchMode.Training;
        CustomBotCount = 0;
        customJoinOnly = false;
        CustomStatusError = "";
        StartFlow("TrainingRoom", 1, true);
    }

    public void StartCustomMatch()
    {
        CreateCustomRoom(null);
    }

    public async void CreateCustomRoom(string desiredCode = null)
    {
        if (shuttingDown)
            return;

        if (runner != null)
        {
            shuttingDown = true;
            await runner.Shutdown();
            runner = null;
            shuttingDown = false;
        }

        StopAllCoroutines();
        isStarting = false;
        gameplayLoading = false;
        gameplayStarted = false;
        SceneLoaded = false;
        spawnIndexes.Clear();

        string normalized = NormalizeRoomCode(desiredCode);
        if (string.IsNullOrEmpty(normalized))
        {
            normalized = GenerateRoomCode();
        }

        CurrentMatchMode = GameMatchMode.Custom;
        CurrentCustomRoomCode = normalized;
        CustomStatusError = "";
        CustomBotCount = 0;
        customJoinOnly = false;

        CustomLobbyUI.Instance?.RefreshLobby();

        StartFlow("CUSTOM_" + normalized, MaxCustomPlayers, true);
    }

    public async void JoinCustomRoomByCode(string inputCode)
    {
        if (shuttingDown)
            return;

        string normalized = NormalizeRoomCode(inputCode);
        if (string.IsNullOrEmpty(normalized))
        {
            CustomStatusError = "Hay nhap so phong (VD: 4821)!";
            CustomLobbyUI.Instance?.RefreshLobby();
            return;
        }

        if (runner != null)
        {
            shuttingDown = true;
            await runner.Shutdown();
            runner = null;
            shuttingDown = false;
        }

        StopAllCoroutines();
        isStarting = false;
        gameplayLoading = false;
        gameplayStarted = false;
        SceneLoaded = false;
        spawnIndexes.Clear();

        CurrentMatchMode = GameMatchMode.Custom;
        CurrentCustomRoomCode = normalized;
        CustomStatusError = "";
        CustomBotCount = 0;
        customJoinOnly = true;

        CustomLobbyUI.Instance?.RefreshLobby();

        StartFlow("CUSTOM_" + normalized, MaxCustomPlayers, false);
    }

    // ================= FLOW =================

    void StartFlow(
        string roomName,
        int maxPlayers,
        bool allowHost
    )
    {
        if (runner != null)
            return;

        if (isStarting)
            return;

        isStarting = true;

        SceneLoaded = false;

        gameplayLoading = false;

        gameplayStarted = false;

        targetPlayers =
            maxPlayers;

        TargetMatchPlayers =
            Mathf.Max(2, maxPlayers);

        currentRoomName =
            roomName;

        allowHostFallback =
            allowHost;

        spawnIndexes.Clear();

        StartCoroutine(
            DelayedStart(
                roomName,
                maxPlayers,
                allowHost
            )
        );
    }

    IEnumerator DelayedStart(
        string roomName,
        int maxPlayers,
        bool allowHost
    )
    {
        yield return null;
        yield return null;

        StartGame(
            roomName,
            maxPlayers,
            allowHost
        );
    }

    // ================= START GAME =================

    async void StartGame(
        string roomName,
        int maxPlayers,
        bool allowHost
    )
    {
        SceneLoaded = false;

        runner =
            Instantiate(
                runnerPrefab
            );

        runner.name =
            "NetworkRunner";

        runner.ProvideInput =
            true;

        DontDestroyOnLoad(
            runner.gameObject
        );

        runner.AddCallbacks(this);

        var sceneManager =
            runner.gameObject
            .AddComponent
            <NetworkSceneManagerDefault>();

        string bucket =
            GetRankBucket();

        string modeKey = CurrentMatchMode.ToString().ToLowerInvariant();

        // Rank & Solo dùng SessionName = null + SessionProperties (mode + rank)
        // để Photon CHỈ ghép vào phòng đang IsOpen=true (trận mới), KHÔNG bao giờ vào lại phòng cũ!
        // Custom dùng đúng mã phòng CUSTOM_<CODE>.
        string finalRoomName = null;
        if (CurrentMatchMode == GameMatchMode.Custom)
        {
            finalRoomName = roomName;
        }
        else if (CurrentMatchMode == GameMatchMode.Training)
        {
            finalRoomName = "Training_" + System.Guid.NewGuid().ToString("N").Substring(0, 6);
        }

        var props =
            new Dictionary<string, SessionProperty>();

        props.Add("mode", modeKey);
        if (matchRankByEloBucket && CurrentMatchMode == GameMatchMode.Rank)
        {
            props.Add("rank", bucket);
        }

        GameMode mode;
        bool canCreateSession;

        if (CurrentMatchMode == GameMatchMode.Custom && customJoinOnly)
        {
            mode = GameMode.Client;
            canCreateSession = false;
        }
        else
        {
            mode = ResolveGameMode(allowHost);
            canCreateSession = CanCreateSession(allowHost);
        }

        var result =
            await runner.StartGame(
                new StartGameArgs()
                {
                    GameMode =
                        mode,

                    SessionName =
                        finalRoomName,

                    PlayerCount =
                        maxPlayers,

                    EnableClientSessionCreation =
                        canCreateSession,

                    IsVisible =
                        true,

                    IsOpen =
                        true,

                    SessionProperties =
                        props,

                    // WAIT ROOM
                    Scene =
                        SceneRef.FromIndex(
                            MENU_SCENE
                        ),

                    SceneManager =
                        sceneManager
                });

        isStarting = false;

        if (
            !result.Ok &&
            !allowHost &&
            !(CurrentMatchMode == GameMatchMode.Custom && customJoinOnly) &&
            ShouldRetryAsHostForDevelopment()
        )
        {
            Debug.LogWarning(
                "Dedicated server not found. Retrying as AutoHostOrClient for development test."
            );

            if (runner != null)
            {
                await runner.Shutdown();
            }

            runner = null;
            isStarting = false;

            StartFlow(
                roomName,
                maxPlayers,
                true
            );

            return;
        }

        if (!result.Ok)
        {
            Debug.LogError(
                "Start Failed: " +
                result.ShutdownReason +
                ". Public matches require a running dedicated server when Use Dedicated Server For Public Matches is enabled."
            );

            if (runner != null)
            {
                await runner.Shutdown();
            }

            runner = null;

            SceneLoaded = false;

            if (CurrentMatchMode == GameMatchMode.Custom)
            {
                CustomStatusError = customJoinOnly
                    ? $"Khong tim thay phong #{CurrentCustomRoomCode}!\nKiem tra ma hoac bam TAO MOI."
                    : $"Khong the tao phong #{CurrentCustomRoomCode}: {result.ShutdownReason}";
                CustomLobbyUI.Instance?.RefreshLobby();
                return;
            }

            MenuUI menuUI =
                FindFirstObjectByType<MenuUI>();

            if (menuUI != null)
            {
                menuUI.StopSearching();
            }

            CustomLobbyUI.Instance?.HideLobby();
        }
        else if (CurrentMatchMode == GameMatchMode.Custom)
        {
            CustomStatusError = "";
            CustomLobbyUI.Instance?.RefreshLobby();
        }
    }

    GameMode ResolveGameMode(
        bool allowHost
    )
    {
        if (allowHost)
            return GameMode.AutoHostOrClient;

        if (useDedicatedServerForPublicMatches)
            return GameMode.Client;

        return GameMode.AutoHostOrClient;
    }

    bool CanCreateSession(
        bool allowHost
    )
    {
        if (allowHost)
            return true;

        return !useDedicatedServerForPublicMatches;
    }

    bool ShouldUseHostForDevelopment()
    {
#if UNITY_EDITOR
        return allowHostFallbackInEditor;
#else
        return allowAutoHostFallbackInBuilds;
#endif
    }

    bool ShouldRetryAsHostForDevelopment()
    {
#if UNITY_EDITOR
        return allowHostFallbackInEditor;
#else
        return allowAutoHostFallbackInBuilds;
#endif
    }

    // ================= PLAYER JOIN =================

    public void OnPlayerJoined(
        NetworkRunner runner,
        PlayerRef player
    )
    {
        Debug.Log(
            "Player Joined: " +
            player.PlayerId
        );

        int count =
            runner.ActivePlayers.Count();

        Debug.Log(
            "Players: " +
            count +
            "/" +
            targetPlayers
        );

        // =========================
        // WAIT ROOM
        // =========================

        if (
            CurrentMatchMode == GameMatchMode.Custom &&
            !gameplayLoading &&
            !gameplayStarted
        )
        {
            CustomLobbyUI.Instance?.RefreshLobby();
            return;
        }

        if (
            !gameplayLoading &&
            !gameplayStarted &&
            runner.IsServer &&
            count >= targetPlayers
        )
        {
            gameplayLoading = true;
            LockSessionForMatch();

            Debug.Log(
                "Enough Players -> Loading Scene"
            );

            runner.LoadScene(
                SceneRef.FromIndex(
                    LOADING_SCENE
                )
            );

            return;
        }

        bool canStartWithBots =
            fillEmptySlotsWithBots &&
            count >= 1 &&
            targetPlayers > 1;

        if (
            !gameplayLoading &&
            !gameplayStarted &&
            runner.IsServer &&
            (
                (count >= minimumPlayersToStart && targetPlayers > minimumPlayersToStart) ||
                canStartWithBots
            )
        )
        {
            StopCoroutine(
                nameof(StartWithDelay)
            );

            StartCoroutine(
                StartWithDelay()
            );
        }

        // =========================
        // KHONG CHO JOIN MUON HOI SINH GIUA TRAN
        // =========================

        if (gameplayStarted || gameplayLoading)
        {
            return;
        }
    }

    void LockSessionForMatch()
    {
        if (runner != null && runner.IsServer && runner.SessionInfo != null)
        {
            runner.SessionInfo.IsOpen = false;
            runner.SessionInfo.IsVisible = false;
        }
    }

    // ================= SPAWN =================

    void SpawnPlayer(
        NetworkRunner runner,
        PlayerRef player
    )
    {
        Transform spawnPoint =
            GetSpawnPoint(
                runner,
                player
            );

        Vector3 spawnPos =
            spawnPoint != null
            ? spawnPoint.position
            : GetFallbackSpawnPosition(
                runner,
                player
            );

        spawnPos = SnapSpawnToGround(spawnPos);

        Quaternion spawnRot =
            spawnPoint != null
            ? spawnPoint.rotation
            : Quaternion.identity;

        NetworkObject obj =
            runner.Spawn(
                playerPrefab,
                spawnPos,
                spawnRot,
                player
            );

        runner.SetPlayerObject(
            player,
            obj
        );

        Debug.Log(
            "Spawned Player at " +
            spawnPos
        );
    }

    Vector3 SnapSpawnToGround(Vector3 pos)
    {
        RaycastHit[] hits =
            Physics.RaycastAll(
                pos + Vector3.up * 5f,
                Vector3.down,
                25f
            );

        bool found = false;
        float bestY = float.MinValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger)
                continue;

            if (hit.collider.GetComponentInParent<PlayerHealth>() != null)
                continue;

            if (hit.collider.GetComponentInParent<BallController>() != null)
                continue;

            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                found = true;
            }
        }

        pos.y = found ? bestY + 0.02f : 0.1f;
        return pos;
    }

    Transform GetSpawnPoint(
        NetworkRunner runner,
        PlayerRef player
    )
    {
        Transform[] points =
            GetGameplaySpawnPoints();

        if (
            points == null ||
            points.Length == 0
        )
            return null;

        int index =
            GetEvenSpawnIndex(
                runner,
                player,
                points.Length
            );

        return points[index];
    }

    Transform[] GetGameplaySpawnPoints()
    {
        if (
            GameManager.Instance != null &&
            GameManager.Instance.spawnPoints != null &&
            GameManager.Instance.spawnPoints.Length > 0
        )
        {
            return GameManager.Instance.spawnPoints;
        }

        GameObject root =
            GameObject.Find("Spawnpoint");

        if (root == null)
            return null;

        Transform[] children =
            root.GetComponentsInChildren
            <Transform>();

        List<Transform> points =
            new List<Transform>();

        foreach (Transform child in children)
        {
            if (
                child != null &&
                child != root.transform
            )
            {
                points.Add(child);
            }
        }

        return points.ToArray();
    }

    int GetEvenSpawnIndex(
        NetworkRunner runner,
        PlayerRef player,
        int pointCount
    )
    {
        if (
            spawnIndexes.TryGetValue(
                player,
                out int cachedIndex
            )
        )
            return cachedIndex;

        int playerOrder =
            runner.ActivePlayers
            .OrderBy(p => p.PlayerId)
            .ToList()
            .IndexOf(player);

        if (playerOrder < 0)
            playerOrder = spawnIndexes.Count;

        int step =
            Mathf.Max(
                1,
                pointCount /
                Mathf.Max(
                    1,
                    runner.ActivePlayers.Count()
                )
            );

        int index =
            (playerOrder * step) %
            pointCount;

        while (
            spawnIndexes.ContainsValue(index) &&
            spawnIndexes.Count < pointCount
        )
        {
            index =
                (index + 1) %
                pointCount;
        }

        spawnIndexes[player] =
            index;

        return index;
    }

    Vector3 GetFallbackSpawnPosition(
        NetworkRunner runner,
        PlayerRef player
    )
    {
        int playerOrder =
            runner.ActivePlayers
            .OrderBy(p => p.PlayerId)
            .ToList()
            .IndexOf(player);

        int playerCount =
            Mathf.Max(
                1,
                runner.ActivePlayers.Count()
            );

        float angle =
            Mathf.PI *
            2f *
            playerOrder /
            playerCount;

        float radius = 8f;

        return new Vector3(
            Mathf.Cos(angle) * radius,
            1f,
            Mathf.Sin(angle) * radius
        );
    }

    IEnumerator StartWithDelay()
    {
        float delay =
            Application.isEditor
            ? Mathf.Min(fallbackStartDelay, editorFallbackStartDelay)
            : fallbackStartDelay;

        yield return new WaitForSeconds(
            delay
        );

        if (
            runner == null ||
            gameplayLoading ||
            gameplayStarted ||
            !runner.IsServer
        )
            yield break;

        int activeCount = runner.ActivePlayers.Count();

        if (
            activeCount < minimumPlayersToStart &&
            !(fillEmptySlotsWithBots && activeCount >= 1)
        )
            yield break;

        gameplayLoading = true;
        LockSessionForMatch();

        Debug.Log(
            "Match starting (players: " +
            activeCount +
            "/" +
            targetPlayers +
            ") -> Loading Scene"
        );

        runner.LoadScene(
            SceneRef.FromIndex(
                LOADING_SCENE
            )
        );
    }

    // ================= PLAYER LEFT =================

    public void OnPlayerLeft(
        NetworkRunner runner,
        PlayerRef player
    )
    {
        Debug.Log(
            "Player Left"
        );

        spawnIndexes.Remove(player);

        NetworkObject obj =
            runner.GetPlayerObject(
                player
            );

        if (obj != null)
        {
            if (BallController.instance != null && BallController.instance.GetTarget() == obj.transform)
            {
                BallController.instance.SwitchTarget(obj.transform);
            }

            runner.Despawn(obj);

            runner.SetPlayerObject(
                player,
                null
            );
        }

        if (CurrentMatchMode == GameMatchMode.Custom)
        {
            int humans = runner != null ? runner.ActivePlayers.Count() : 0;
            CustomBotCount = Mathf.Clamp(
                CustomBotCount,
                0,
                Mathf.Max(0, MaxCustomPlayers - humans)
            );
            CustomLobbyUI.Instance?.RefreshLobby();
        }

        // Khi tat ca nguoi choi that da roi khoi phong sau khi tran bat dau ->
        // Dedicated Server tu dong dong phong cu va mo phong MOI TINH cho tran tiep theo!
        if (
            isDedicatedServerInstance &&
            !restartingDedicatedServer &&
            (gameplayLoading || gameplayStarted) &&
            runner != null &&
            runner.IsServer &&
            runner.ActivePlayers.Count() == 0
        )
        {
            StartCoroutine(RestartDedicatedServerDelayed(1.0f));
        }
    }

    // ================= CUSTOM LOBBY =================

    public void StartCustomMatchFromLobby()
    {
        if (runner == null || !runner.IsRunning)
            return;

        if (!runner.IsServer)
            return;

        if (gameplayLoading || gameplayStarted)
            return;

        int humans = runner.ActivePlayers.Count();
        int bots = Mathf.Clamp(
            CustomBotCount,
            0,
            Mathf.Max(0, MaxCustomPlayers - humans)
        );

        TargetMatchPlayers = Mathf.Clamp(
            humans + bots,
            2,
            MaxCustomPlayers
        );

        gameplayLoading = true;
        LockSessionForMatch();

        Debug.Log(
            $"Host starting Custom Match ({humans} humans + {bots} bots -> target {TargetMatchPlayers})"
        );

        runner.LoadScene(
            SceneRef.FromIndex(
                LOADING_SCENE
            )
        );
    }

    public void AddCustomBot()
    {
        if (runner == null || !runner.IsRunning || !runner.IsServer)
            return;

        int humans = runner.ActivePlayers.Count();
        if (humans + CustomBotCount < MaxCustomPlayers)
        {
            CustomBotCount++;
        }
    }

    public void RemoveCustomBot()
    {
        if (runner == null || !runner.IsRunning || !runner.IsServer)
            return;

        if (CustomBotCount > 0)
        {
            CustomBotCount--;
        }
    }

    public bool IsHostWithOtherLiveHumans()
    {
        if (runner == null || !runner.IsRunning)
            return false;

        if (isDedicatedServerInstance || !runner.IsServer)
            return false;

        int otherHumans = 0;
        foreach (PlayerRef p in runner.ActivePlayers)
        {
            if (p == runner.LocalPlayer)
                continue;

            NetworkObject pObj = runner.GetPlayerObject(p);
            if (pObj != null)
            {
                PlayerHealth hp = pObj.GetComponent<PlayerHealth>();
                if (hp != null && !hp.IsDead)
                {
                    otherHumans++;
                }
            }
        }
        return otherHumans > 0;
    }

    public void ReturnToMenu()
    {
        CancelMatchmaking();
    }

    // ================= SCENE =================

    public void OnSceneLoadStart(
        NetworkRunner runner
    )
    {
        SceneLoaded = false;

        Debug.Log(
            "Scene Loading..."
        );
    }

    public void OnSceneLoadDone(
        NetworkRunner runner
    )
    {
        SceneLoaded = true;

        int currentScene =
            runner.SceneManager
            .MainRunnerScene
            .buildIndex;

        Debug.Log(
            "Scene Loaded: " +
            currentScene
        );

        // =========================
        // LOADING SCENE
        // =========================

        if (
            currentScene ==
            LOADING_SCENE
        )
        {
            if (runner.IsServer)
            {
                StartCoroutine(
                    LoadGameplayDelayed()
                );
            }

            return;
        }

        // =========================
        // GAMEPLAY
        // =========================

        if (
            currentScene ==
            GAMEPLAY_SCENE
        )
        {
            gameplayStarted = true;

            Debug.Log(
                "Gameplay Loaded"
            );

            if (runner.IsServer)
            {
                foreach (
                    PlayerRef player
                    in runner.ActivePlayers
                )
                {
                    if (
                        runner.GetPlayerObject(
                            player
                        ) != null
                    )
                    {
                        continue;
                    }

                    SpawnPlayer(
                        runner,
                        player
                    );
                }
            }
        }
    }

    // ================= LOAD GAMEPLAY =================

    IEnumerator LoadGameplayDelayed()
    {
        // cho loading scene chạy UI
        yield return new WaitForSeconds(3f);

        if (
            runner == null
        )
            yield break;

        Debug.Log(
            "Loading -> Gameplay"
        );

        runner.LoadScene(
            SceneRef.FromIndex(
                GAMEPLAY_SCENE
            )
        );
    }

    // ================= CANCEL =================

    public async void CancelMatchmaking()
    {
        if (shuttingDown)
            return;

        shuttingDown = true;

        if (runner != null)
        {
            await runner.Shutdown();

            runner = null;
        }

        StopAllCoroutines();

        CustomBotCount = 0;

        CustomStatusError = "";

        customJoinOnly = false;

        spawnIndexes.Clear();

        SceneLoaded = false;

        gameplayLoading = false;

        gameplayStarted = false;

        isStarting = false;

        shuttingDown = false;

        SceneManager.LoadScene(
            MENU_SCENE
        );

        Debug.Log(
            "Matchmaking Cancelled"
        );
    }

    // ================= SHUTDOWN =================

    public void OnShutdown(
        NetworkRunner runner,
        ShutdownReason shutdownReason
    )
    {
        Debug.Log(
            "Shutdown: " +
            shutdownReason
        );

        bool wasIntentionalShutdown = shuttingDown;
        bool wasInGameplay = SceneManager.GetActiveScene().buildIndex == GAMEPLAY_SCENE;

        StopAllCoroutines();

        CustomBotCount = 0;

        CustomStatusError = "";

        customJoinOnly = false;

        spawnIndexes.Clear();

        SceneLoaded = false;

        gameplayLoading = false;

        gameplayStarted = false;

        isStarting = false;

        shuttingDown = false;

        this.runner = null;

        // Bảo vệ tuyệt đối không bao giờ bị ĐƠ GAME nếu Host hoặc kết nối đột ngột ngắt giữa trận!
        if (!wasIntentionalShutdown && !isDedicatedServerInstance && wasInGameplay)
        {
            HandleUnexpectedGameplayDisconnect();
        }
    }

    void HandleUnexpectedGameplayDisconnect()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (MatchResultUI.Instance != null && !MatchResultUI.Instance.IsShown)
        {
            bool localAlive = true;
            if (NetworkPlayer.Local != null)
            {
                PlayerHealth hp = NetworkPlayer.Local.GetComponent<PlayerHealth>();
                if (hp != null && hp.IsDead)
                {
                    localAlive = false;
                }
            }

            int total = GameManager.Instance != null
                ? Mathf.Max(2, GameManager.Instance.TotalMatchPlayers)
                : Mathf.Max(2, TargetMatchPlayers);

            MatchResultUI.Instance.ShowResult(
                localAlive,
                localAlive ? 1 : 2,
                total
            );
            return;
        }

        if (MatchResultUI.Instance == null)
        {
            SceneManager.LoadScene(MENU_SCENE);
        }
    }

    // ================= CONNECTION =================

    public void OnConnectedToServer(
        NetworkRunner runner
    )
    {
        Debug.Log(
            "Connected To Server"
        );
    }

    public void OnDisconnectedFromServer(
        NetworkRunner runner,
        NetDisconnectReason reason
    )
    {
        Debug.Log(
            "Disconnected: " +
            reason
        );
    }

    // ================= INPUT =================

    public void OnInput(
        NetworkRunner runner,
        NetworkInput input
    )
    {
        NetworkInputData data =
            new NetworkInputData();

        if (PlayerMovement.Local != null)
        {
            data.moveDirection =
                PlayerMovement.Local
                .GetDesiredWorldMoveDirection();

            data.jumpPressed =
                PlayerMovement.Local
                .ConsumeJumpInput();
        }

        input.Set(data);
    }

    public void OnInputMissing(
        NetworkRunner runner,
        PlayerRef player,
        NetworkInput input
    )
    {
    }

    // ================= UNUSED =================

    public void OnConnectRequest(
        NetworkRunner runner,
        NetworkRunnerCallbackArgs.ConnectRequest request,
        byte[] token
    )
    {
    }

    public void OnConnectFailed(
        NetworkRunner runner,
        NetAddress remoteAddress,
        NetConnectFailedReason reason
    )
    {
        Debug.LogError(
            "Connect Failed: " +
            reason
        );

        isStarting = false;
    }

    public void OnSessionListUpdated(
        NetworkRunner runner,
        List<SessionInfo> sessionList
    )
    {
    }

    public void OnCustomAuthenticationResponse(
        NetworkRunner runner,
        Dictionary<string, object> data
    )
    {
    }

    public void OnHostMigration(
        NetworkRunner runner,
        HostMigrationToken hostMigrationToken
    )
    {
    }

    public void OnObjectEnterAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player
    )
    {
    }

    public void OnObjectExitAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player
    )
    {
    }

    public void OnReliableDataReceived(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        System.ArraySegment<byte> data
    )
    {
    }

    public void OnReliableDataProgress(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        float progress
    )
    {
    }

    public void OnUserSimulationMessage(
        NetworkRunner runner,
        SimulationMessagePtr message
    )
    {
    }
}
