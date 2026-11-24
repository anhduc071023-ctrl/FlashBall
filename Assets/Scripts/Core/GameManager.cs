using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MatchPhase
{
    Waiting,
    SkillSelect,
    Playing
}

public class GameManager :
    NetworkBehaviour,
    INetworkRunnerCallbacks
{
    public static GameManager Instance;

    // ================= PLAYER =================

    [Header("Player")]
    public NetworkPrefabRef playerPrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    // ================= BALL =================

    [Header("Ball")]
    public NetworkObject ballPrefab;

    public BallController ball;

    public Transform mapCenter;

    // ================= MATCH =================

    [Header("Match")]
    public float prepareTime = 10f;

    [Networked]
    public bool started { get; set; }

    [Networked]
    public MatchPhase phase { get; set; }

    [Networked]
    public int prepareSecondsRemaining { get; set; }

    [Networked]
    public int TotalMatchPlayers { get; set; }

    [Networked]
    public int SelectedMapId { get; set; }

    static int lastRandomMapId = -1;
    int lastAppliedMapId = -1;
    bool serverMatchFinishedNotified;

    // ================= AWAKE =================

    void Awake()
    {
        Instance = this;
    }

    // ================= SPAWNED =================

    public override void Spawned()
    {
        if (Runner != null)
        {
            Runner.AddCallbacks(this);
        }

        // chỉ host start match & chọn random map đồng bộ cho tất cả client!
        if (Object.HasStateAuthority)
        {
            int nextMap = UnityEngine.Random.Range(0, 3);
            if (nextMap == lastRandomMapId)
            {
                nextMap = (nextMap + UnityEngine.Random.Range(1, 3)) % 3;
            }
            lastRandomMapId = nextMap;
            SelectedMapId = nextMap;

            StartCoroutine(
                StartMatchRoutine()
            );
        }

        ApplySyncedArenaMap();
    }

    void ApplySyncedArenaMap()
    {
        if (Object == null || !Object.IsValid)
            return;

        if (SelectedMapId != lastAppliedMapId)
        {
            lastAppliedMapId = SelectedMapId;
            ArenaMapManager.EnsureInstance().ApplyMap((ArenaMapType)Mathf.Clamp(SelectedMapId, 0, 2));
        }
    }

    public override void Render()
    {
        ApplySyncedArenaMap();

        MatchFlowUI ui =
            MatchFlowUI.Instance;

        if (ui != null)
        {
            ui.ApplyState(
                phase,
                prepareSecondsRemaining
            );
        }

        CheckLocalMatchOutcome();
        CheckServerMatchCompleted();
    }

    void CheckServerMatchCompleted()
    {
        if (serverMatchFinishedNotified)
            return;

        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return;

        if (phase != MatchPhase.Playing || TotalMatchPlayers < 2)
            return;

        int aliveCount = PlayerFinder.CountAlivePlayers();
        int aliveHumans = PlayerFinder.CountAliveHumanPlayers();

        if (aliveCount <= 1 || aliveHumans <= 0)
        {
            serverMatchFinishedNotified = true;

            if (ball != null && ball.Object != null && ball.Object.IsValid)
            {
                ball.SetBallActive(false);
            }

            MatchmakingManager.Instance?.NotifyDedicatedServerMatchEnded();
        }
    }

    void CheckLocalMatchOutcome()
    {
        if (Object == null || !Object.IsValid || Runner == null || !Runner.IsRunning)
            return;

        if (phase != MatchPhase.Playing)
            return;

        MatchResultUI resultUI = MatchResultUI.Instance;
        if (resultUI == null || resultUI.IsShown)
            return;

        NetworkPlayer localPlayer = NetworkPlayer.Local;
        if (localPlayer == null)
            return;

        PlayerHealth localHp = localPlayer.GetComponent<PlayerHealth>();
        if (localHp == null || localHp.Object == null || !localHp.Object.IsValid)
            return;

        int total = Mathf.Max(2, TotalMatchPlayers);
        int aliveCount = PlayerFinder.CountAlivePlayers();

        if (localHp.IsDead)
        {
            int placement = Mathf.Clamp(aliveCount + 1, 2, total);
            resultUI.ShowResult(false, placement, total);
            return;
        }

        if (aliveCount <= 1 && total >= 2)
        {
            if (Object != null && Object.HasStateAuthority && ball != null)
            {
                ball.SetBallActive(false);
            }

            resultUI.ShowResult(true, 1, total);
        }
    }

    // ================= MATCH FLOW =================

    IEnumerator StartMatchRoutine()
    {
        started = false;
        phase = MatchPhase.Waiting;
        prepareSecondsRemaining = 0;

        if (!EnsureBall())
        {
            Debug.LogError(
                "Ball Missing!"
            );

            yield break;
        }

        while (
            PlayerFinder.CountAliveHumanPlayers()
            <= 0
        )
        {
            yield return null;
        }

        yield return null;

        SetupBotOpponents();

        TotalMatchPlayers = Mathf.Max(2, PlayerFinder.CountAlivePlayers());

        ball.SetBallActive(false);

        phase = MatchPhase.SkillSelect;

        Debug.Log("SKILL SELECT PHASE");

        float timer = prepareTime;

        while (timer > 0)
        {
            prepareSecondsRemaining =
                Mathf.CeilToInt(timer);

            Debug.Log(
                "Match starts in: " +
                prepareSecondsRemaining
            );

            timer -= 1f;

            yield return new WaitForSeconds(1f);
        }

        Debug.Log("MATCH START");

        started = true;
        phase = MatchPhase.Playing;
        prepareSecondsRemaining = 0;

        // start ball
        ball.StartMatch();
    }

    void SetupBotOpponents()
    {
        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        AutoParryBot[] existingBots =
            FindObjectsByType<AutoParryBot>(
                FindObjectsSortMode.None
            );

        foreach (AutoParryBot bot in existingBots)
        {
            if (bot == null)
                continue;

            NetworkObject botNet =
                bot.GetComponent<NetworkObject>();

            if (
                Runner != null &&
                botNet != null &&
                botNet.IsValid
            )
            {
                Runner.Despawn(botNet);
            }
            else
            {
                bot.gameObject.SetActive(false);
            }
        }

        if (
            MatchmakingManager.Instance != null &&
            !MatchmakingManager.Instance.fillEmptySlotsWithBots
        )
            return;

        int humanCount =
            PlayerFinder.CountAliveHumanPlayers();

        int desiredPlayers =
            MatchmakingManager.Instance != null
            ? Mathf.Max(2, MatchmakingManager.TargetMatchPlayers)
            : 2;

        int botsNeeded =
            desiredPlayers - humanCount;

        if (botsNeeded <= 0)
            return;

        List<Vector3> occupiedPositions =
            new List<Vector3>();

        NetworkPlayer[] existingPlayers =
            FindObjectsByType<NetworkPlayer>(
                FindObjectsSortMode.None
            );

        foreach (NetworkPlayer p in existingPlayers)
        {
            if (p == null || !p.gameObject.activeInHierarchy)
                continue;

            if (p.GetComponent<AutoParryBot>() != null)
                continue;

            occupiedPositions.Add(p.transform.position);
        }

        Transform[] points =
            GetAvailableSpawnPoints();

        for (int i = 0; i < botsNeeded; i++)
        {
            Vector3 spawnPos;
            Quaternion spawnRot;

            GetNextBotSpawnPose(
                points,
                occupiedPositions,
                i,
                botsNeeded,
                out spawnPos,
                out spawnRot
            );

            spawnPos = SnapSpawnToGround(spawnPos);

            occupiedPositions.Add(spawnPos);

            NetworkObject botObj = null;

            if (playerPrefab.IsValid)
            {
                botObj =
                    Runner.Spawn(
                        playerPrefab,
                        spawnPos,
                        spawnRot
                    );
            }
            else if (
                MatchmakingManager.Instance != null &&
                MatchmakingManager.Instance.playerPrefab != null
            )
            {
                botObj =
                    Runner.Spawn(
                        MatchmakingManager.Instance.playerPrefab,
                        spawnPos,
                        spawnRot
                    );
            }

            if (botObj == null)
            {
                Debug.LogWarning("Failed to spawn Bot player");
                continue;
            }

            string botName =
                "Bot_" + UnityEngine.Random.Range(100, 999);

            botObj.name = botName;

            SkillType botSkill =
                (SkillType)UnityEngine.Random.Range(1, 4);

            NetworkPlayer netPlayer =
                botObj.GetComponent<NetworkPlayer>();

            if (netPlayer != null)
            {
                netPlayer.PlayerName = botName;
                netPlayer.SkillID = (int)botSkill;
                netPlayer.WeaponID = UnityEngine.Random.Range(0, WeaponCatalog.All.Length);
                netPlayer.SkinID = UnityEngine.Random.Range(0, SkinCatalog.All.Length);
            }

            PlayerAbilityController ability =
                botObj.GetComponent<PlayerAbilityController>();

            if (ability != null)
            {
                ability.currentSkill = botSkill;
            }

            if (botObj.GetComponent<AutoParryBot>() == null)
            {
                botObj.gameObject.AddComponent<AutoParryBot>();
            }

            Debug.Log(
                "Spawned Bot Player: " +
                botName +
                " at " +
                spawnPos
            );
        }
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

    Transform[] GetAvailableSpawnPoints()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints;

        GameObject root = GameObject.Find("Spawnpoint");
        if (root == null)
            return null;

        List<Transform> list = new List<Transform>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>())
        {
            if (child != null && child != root.transform)
                list.Add(child);
        }

        return list.ToArray();
    }

    void GetNextBotSpawnPose(
        Transform[] points,
        List<Vector3> occupiedPositions,
        int botIndex,
        int totalBots,
        out Vector3 pos,
        out Quaternion rot
    )
    {
        if (points != null && points.Length > 0)
        {
            Transform bestPoint = null;
            float bestMinDist = -1f;

            foreach (Transform pt in points)
            {
                if (pt == null)
                    continue;

                float minDist = float.MaxValue;
                foreach (Vector3 occ in occupiedPositions)
                {
                    float d = Vector3.Distance(pt.position, occ);
                    if (d < minDist)
                        minDist = d;
                }

                if (occupiedPositions.Count == 0)
                    minDist = 100f;

                if (minDist > bestMinDist)
                {
                    bestMinDist = minDist;
                    bestPoint = pt;
                }
            }

            if (bestPoint != null)
            {
                pos = bestPoint.position;
                rot = bestPoint.rotation;
                return;
            }
        }

        FindMapCenter();
        Vector3 center =
            mapCenter != null
            ? mapCenter.position
            : Vector3.zero;

        float angle =
            Mathf.PI * 2f * (botIndex + 1) / Mathf.Max(1, totalBots + 1);

        pos = center + new Vector3(Mathf.Cos(angle) * 10f, 1f, Mathf.Sin(angle) * 10f);
        rot = Quaternion.LookRotation((center - pos).normalized);
    }

    // ================= BALL SPAWN =================

    bool EnsureBall()
    {
        BallController runtimeBall =
            FindRuntimeBall();

        if (runtimeBall != null)
        {
            ball = runtimeBall;
            CleanupDuplicateBalls(ball);
            return true;
        }

        if (IsRuntimeBall(ball))
        {
            CleanupDuplicateBalls(ball);
            return true;
        }

        ball = null;

        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return false;

        if (Runner == null)
        {
            Debug.LogError(
                "Ball spawn failed: Runner missing"
            );

            return false;
        }

        if (ballPrefab == null)
        {
            Debug.LogError(
                "Ball spawn failed: Ball Prefab not assigned"
            );

            return false;
        }

        FindMapCenter();

        Vector3 spawnPosition =
            mapCenter != null
            ? mapCenter.position
            : Vector3.zero;

        NetworkObject ballObject =
            Runner.Spawn(
                ballPrefab,
                spawnPosition,
                Quaternion.identity
            );

        if (ballObject == null)
        {
            Debug.LogError(
                "Ball spawn failed: Runner.Spawn returned null"
            );

            return false;
        }

        ball =
            ballObject.GetComponent
            <BallController>();

        if (ball == null)
        {
            Debug.LogError(
                "Ball spawn failed: prefab has no BallController"
            );

            return false;
        }

        ball.Init(
            null,
            mapCenter
        );

        Debug.Log(
            "Ball spawned by GameManager"
        );

        CleanupDuplicateBalls(ball);

        return true;
    }

    BallController FindRuntimeBall()
    {
        BallController[] balls =
            FindObjectsByType<BallController>(
                FindObjectsSortMode.None
            );

        foreach (BallController candidate in balls)
        {
            if (IsRuntimeBall(candidate))
                return candidate;
        }

        return null;
    }

    void CleanupDuplicateBalls(
        BallController keep
    )
    {
        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        BallController[] balls =
            FindObjectsByType<BallController>(
                FindObjectsSortMode.None
            );

        foreach (BallController candidate in balls)
        {
            if (
                candidate == null ||
                candidate == keep ||
                candidate.isFakeBall
            )
                continue;

            NetworkObject netObj =
                candidate.GetComponent<NetworkObject>();

            if (
                Runner != null &&
                netObj != null
            )
            {
                Runner.Despawn(netObj);
            }
            else
            {
                candidate.gameObject.SetActive(false);
            }
        }
    }

    bool IsRuntimeBall(
        BallController candidate
    )
    {
        if (candidate == null)
            return false;

        if (
            candidate.gameObject == null ||
            !candidate.gameObject.scene.IsValid()
        )
            return false;

        if (candidate.Object == null)
            return false;

        return true;
    }

    void FindMapCenter()
    {
        if (mapCenter != null)
            return;

        GameObject centerObject =
            GameObject.Find("MapCenter");

        if (centerObject != null)
        {
            mapCenter =
                centerObject.transform;
        }
    }

    // ================= PLAYER JOIN =================

    public void OnPlayerJoined(
       NetworkRunner runner,
       PlayerRef player)
    {
    }

    // ================= PLAYER LEFT =================

    public void OnPlayerLeft(
        NetworkRunner runner,
        PlayerRef player)
    {
        Debug.Log(
            "Player Left"
        );
    }

    // ================= INPUT =================

    public void OnInput(
        NetworkRunner runner,
        NetworkInput input)
    {
    }

    public void OnInputMissing(
        NetworkRunner runner,
        PlayerRef player,
        NetworkInput input)
    {
    }

    // ================= CONNECTION =================

    void INetworkRunnerCallbacks
        .OnConnectedToServer(
        NetworkRunner runner)
    {
        Debug.Log(
            "Connected To Server"
        );
    }

    void INetworkRunnerCallbacks
        .OnDisconnectedFromServer(
        NetworkRunner runner,
        NetDisconnectReason reason)
    {
        Debug.Log(
            "Disconnected"
        );
    }

    // ================= EMPTY CALLBACKS =================

    public void OnShutdown(
        NetworkRunner runner,
        ShutdownReason shutdownReason)
    {
    }

    public void OnConnectRequest(
        NetworkRunner runner,
        NetworkRunnerCallbackArgs
        .ConnectRequest request,
        byte[] token)
    {
    }

    public void OnConnectFailed(
        NetworkRunner runner,
        NetAddress remoteAddress,
        NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(
        NetworkRunner runner,
        SimulationMessagePtr message)
    {
    }

    public void OnSessionListUpdated(
        NetworkRunner runner,
        List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(
        NetworkRunner runner,
        Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(
        NetworkRunner runner,
        HostMigrationToken hostMigrationToken)
    {
    }

    public void OnSceneLoadDone(
        NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(
        NetworkRunner runner)
    {
    }

    public void OnObjectEnterAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player)
    {
    }

    public void OnObjectExitAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player)
    {
    }

    public void OnReliableDataReceived(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        float progress)
    {
    }
}
