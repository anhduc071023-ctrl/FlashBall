using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public BallController ball;

    // ================= MATCH =================

    [Header("Match")]
    public float prepareTime = 30f;

    [Networked]
    public bool started { get; set; }

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

        // chỉ host start match
        if (Object.HasStateAuthority)
        {
            StartCoroutine(
                StartMatchRoutine()
            );
        }
    }

    // ================= MATCH FLOW =================

    IEnumerator StartMatchRoutine()
    {
        started = false;

        // check ball
        if (ball == null)
        {
            Debug.LogError(
                "Ball Missing!"
            );

            yield break;
        }

        // tắt ball lúc đầu
        ball.gameObject.SetActive(false);

        Debug.Log("PREPARE PHASE");

        float timer = prepareTime;

        while (timer > 0)
        {
            Debug.Log(
                "Match starts in: " +
                Mathf.CeilToInt(timer)
            );

            timer -= 1f;

            yield return new WaitForSeconds(1f);
        }

        Debug.Log("MATCH START");

        started = true;

        // bật ball
        ball.gameObject.SetActive(true);

        // start ball
        ball.StartMatch();
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