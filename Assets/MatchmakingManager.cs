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

    [HideInInspector]
    public NetworkRunner runner;

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

    // ================= INIT =================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ================= RANK =================

    string GetRankBucket()
    {
        if (playerElo < 1000)
            return "bronze";

        if (playerElo < 2000)
            return "silver";

        return "gold";
    }

    // ================= START =================

    public void StartRankMatch()
    {
        StartFlow("RankRoom", 4);
    }

    public void StartSoloMatch()
    {
        StartFlow("SoloRoom", 2);
    }

    public void StartTraining()
    {
        StartFlow("TrainingRoom", 1);
    }

    // ================= FLOW =================

    void StartFlow(
        string roomName,
        int maxPlayers
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

        StartCoroutine(
            DelayedStart(
                roomName,
                maxPlayers
            )
        );
    }

    IEnumerator DelayedStart(
        string roomName,
        int maxPlayers
    )
    {
        yield return null;
        yield return null;

        StartGame(
            roomName,
            maxPlayers
        );
    }

    // ================= START GAME =================

    async void StartGame(
        string roomName,
        int maxPlayers
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

        string finalRoomName =
            roomName + "_" + bucket;

        var props =
            new Dictionary<string, SessionProperty>();

        props.Add(
            "rank",
            bucket
        );

        var result =
            await runner.StartGame(
                new StartGameArgs()
                {
                    GameMode =
                        GameMode.AutoHostOrClient,

                    SessionName =
                        finalRoomName,

                    PlayerCount =
                        maxPlayers,

                    EnableClientSessionCreation =
                        true,

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

        if (!result.Ok)
        {
            Debug.LogError(
                "Start Failed: " +
                result.ShutdownReason
            );

            if (runner != null)
            {
                await runner.Shutdown();
            }

            runner = null;

            SceneLoaded = false;
        }
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
            !gameplayLoading &&
            !gameplayStarted &&
            runner.IsServer &&
            count >= targetPlayers
        )
        {
            gameplayLoading = true;

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

        // =========================
        // ONLY SPAWN IN GAMEPLAY
        // =========================

        if (
            runner.SceneManager
            .MainRunnerScene
            .buildIndex !=
            GAMEPLAY_SCENE
        )
        {
            return;
        }

        if (!runner.IsServer)
            return;

        if (
            runner.GetPlayerObject(player)
            != null
        )
        {
            return;
        }

        SpawnPlayer(
            runner,
            player
        );
    }

    // ================= SPAWN =================

    void SpawnPlayer(
        NetworkRunner runner,
        PlayerRef player
    )
    {
        Vector3 spawnPos =
            new Vector3(
                Random.Range(-3f, 3f),
                1f,
                Random.Range(-3f, 3f)
            );

        NetworkObject obj =
            runner.Spawn(
                playerPrefab,
                spawnPos,
                Quaternion.identity,
                player
            );

        runner.SetPlayerObject(
            player,
            obj
        );

        Debug.Log(
            "Spawned Player"
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

        NetworkObject obj =
            runner.GetPlayerObject(
                player
            );

        if (obj != null)
        {
            runner.Despawn(obj);

            runner.SetPlayerObject(
                player,
                null
            );
        }
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

        StopAllCoroutines();

        SceneLoaded = false;

        gameplayLoading = false;

        gameplayStarted = false;

        isStarting = false;

        shuttingDown = false;

        this.runner = null;
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