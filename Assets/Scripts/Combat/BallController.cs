using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(
    typeof(Rigidbody),
    typeof(NetworkObject)
)]
public class BallController : NetworkBehaviour
{
    public static BallController instance;

    // ================= NETWORK =================

    [Networked]
    public float speed { get; set; }

    [Networked]
    public bool isInSkill { get; set; }

    [Networked]
    public bool isResetting { get; set; }

    [Networked]
    public Vector3 netPosition { get; set; }

    [Networked]
    public Vector3 netVelocity { get; set; }

    [Networked]
    public NetworkBool ballActive { get; set; }

    [Networked]
    public NetworkId CurrentTargetId { get; set; }

    [Networked]
    public NetworkBool isFreezeSlowing { get; set; }

    [Networked]
    public int LightningChargeStacks { get; set; }

    // ================= REFERENCES =================

    [Header("References")]
    public Transform mapCenter;

    Rigidbody rb;

    Transform player;

    Transform lastParryPlayer;

    [Header("Network Smoothing")]
    public float remoteSmooth = 18f;

    public float remotePrediction = 0.04f;

    [Header("Arena Bounds")]
    public float maxArenaRadius = 120f;

    public float minHeight = -10f;

    public float maxHeight = 85f;

    [Header("Height")]
    public float targetHeightOffset = 1.0f;

    // ================= SPEED =================

    [Header("Speed")]
    public float startSpeed = 10f;

    public float speedMultiplier = 1.1f;

    public float maxSpeed = 35f;

    // ================= PARRY =================

    [Header("Parry")]
    public float parryForce = 70f;

    public float serverParryRange = 9.0f;

    public float liftForce = 2f;

    public float sideForce = 2f;

    public float extraGravity = 40f;

    // ================= HOMING =================

    [Header("Homing")]
    public float homingStrength = 5f;

    // ================= POWER =================

    [Header("Power")]
    public float powerForceMultiplier = 1.8f;

    public float powerSpeedMultiplier = 1.5f;

    // ================= STATE =================

    [Header("State")]
    public bool isFakeBall = false;

    public float preSkillSpeed;

    bool canMove = true;

    bool waitingNextLaunch;

    float parryArcTimer;

    public bool isTornadoDiving { get; private set; }

    float tornadoDiveSpeed;

    float freezeSlowTimer;
    Transform freezeOwner;

    float nextLightningStrikeTimer = 8f;
    Coroutine lightningStrikeRoutine;

    Vector3 baseBallScale = Vector3.one * 1.1f;
    ArenaMapType appliedBallMapType = (ArenaMapType)(-1);
    GameObject defaultFireEffectGo;
    MeshRenderer defaultBallMeshRenderer;
    Material defaultBallOriginalMat;
    GameObject mapBallVisualRoot;
    Transform techGyroOuter;
    Transform techGyroInner;
    LineRenderer[] stormBallArcs;
    float stormArcRefreshTimer;
    readonly List<Material> ballThemeRuntimeMats = new List<Material>();

    Coroutine skillRoutine;

    Coroutine resumeRoutine;

    HashSet<Transform> ignoreTargets =
        new HashSet<Transform>();

    LineRenderer targetLine;
    Material targetLineMat;
    Transform cachedVisualTarget;
    NetworkId cachedVisualTargetId;

    // ================= SPAWN =================

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        baseBallScale = transform.localScale.sqrMagnitude > 0.01f ? transform.localScale : (Vector3.one * 1.1f);
        CacheDefaultBallVisuals();

        FindMapCenter();

        if (!isFakeBall)
        {
            instance = this;
            EnsureTargetLine();
        }

        rb.useGravity = false;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.constraints =
            RigidbodyConstraints.FreezeRotation;

        rb.sleepThreshold = 0f;

        if (
            Object != null &&
            !Object.HasStateAuthority
        )
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
        else
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
        }

        speed = startSpeed;

        ResetVelocity();

        if (
            Object != null &&
            Object.HasStateAuthority
        )
        {
            ballActive = false;
        }

        if (
            Object != null &&
            Object.HasStateAuthority &&
            mapCenter != null
        )
        {
            transform.position =
                mapCenter.position;
        }

        PublishNetworkState();
    }

    // ================= INIT =================

    public void Init(
        Transform firstPlayer,
        Transform center
    )
    {
        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        player = firstPlayer;

        lastParryPlayer = null;

        mapCenter = center;

        FindMapCenter();

        if (mapCenter != null)
        {
            transform.position =
                mapCenter.position;
        }

        speed = startSpeed;

        ResetVelocity();

        ballActive = true;

        PublishNetworkState();
    }

    // ================= UPDATE =================

    public override void FixedUpdateNetwork()
    {
        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        PublishNetworkState();

        // fake ball chỉ bay theo force
        if (isFakeBall)
            return;

        if (!ballActive)
        {
            ResetVelocity();
            PublishNetworkState();
            return;
        }

        if (!canMove)
            return;

        if (isResetting)
            return;

        if (waitingNextLaunch)
            return;

        if (isInSkill)
            return;

        if (rb == null)
            return;

        // ================= TARGET =================

        if (
            player == null ||
            !player.gameObject.activeInHierarchy ||
            PlayerFinder.IsPlayerInvisible(player)
        )
        {
            player =
                PlayerFinder
                .FindClosestAlivePlayer(
                    transform.position
                );

            if (player == null)
            {
                ResetVelocity();
                PublishNetworkState();
                return;
            }
        }

        PlayerHealth hp =
            player.GetComponent<PlayerHealth>();

        if (hp == null || hp.IsDead || PlayerFinder.IsPlayerInvisible(player))
        {
            Transform oldPlayer = player;
            SwitchTarget(player);

            if (player == oldPlayer || PlayerFinder.IsPlayerInvisible(player))
            {
                player =
                    PlayerFinder
                    .FindClosestAlivePlayer(
                        transform.position,
                        oldPlayer
                    );
            }

            if (player == null || PlayerFinder.IsPlayerInvisible(player))
            {
                player = null;
                ResetVelocity();
                PublishNetworkState();
                return;
            }
        }

        // ================= THUNDERSTORM MAP: SET DANH XUONG BONG LAM BONG TO HON =================

        if (ArenaMapManager.CurrentMapType == ArenaMapType.ThunderStorm)
        {
            nextLightningStrikeTimer -= Runner.DeltaTime;
            if (nextLightningStrikeTimer <= 0f)
            {
                nextLightningStrikeTimer = Random.Range(5.5f, 9.0f);
                if (lightningStrikeRoutine != null) StopCoroutine(lightningStrikeRoutine);
                lightningStrikeRoutine = StartCoroutine(ServerLightningStrikeBallRoutine());
            }
        }

        // ================= FREEZE SLOW HOMING (BAY CHAM VE PHIA MINH DE DE PARRY!) =================

        if (isFreezeSlowing)
        {
            freezeSlowTimer -= Runner.DeltaTime;
            if (
                freezeOwner != null &&
                freezeOwner.gameObject.activeInHierarchy &&
                !PlayerFinder.IsPlayerInvisible(freezeOwner)
            )
            {
                PlayerHealth ownerHp = freezeOwner.GetComponent<PlayerHealth>();
                if (ownerHp != null && !ownerHp.IsDead)
                {
                    player = freezeOwner;
                }
            }

            if (freezeSlowTimer <= 0f || player == null)
            {
                isFreezeSlowing = false;
                freezeSlowTimer = 0f;
                freezeOwner = null;
                speed = Mathf.Clamp(Mathf.Max(preSkillSpeed, startSpeed), startSpeed, maxSpeed);
            }
            else
            {
                Vector3 slowTargetPos = new Vector3(
                    player.position.x,
                    player.position.y + targetHeightOffset,
                    player.position.z
                );
                Vector3 toOwner = slowTargetPos - transform.position;
                float slowSpd = Mathf.Clamp(Mathf.Max(preSkillSpeed, startSpeed) * 0.28f, 5.2f, 7.2f);
                if (toOwner.sqrMagnitude > 0.001f)
                {
                    rb.linearVelocity = Vector3.Lerp(
                        rb.linearVelocity,
                        toOwner.normalized * slowSpd,
                        Runner.DeltaTime * 9.5f
                    );
                }

                PublishNetworkState();
                return;
            }
        }

        // ================= TORNADO DIVE =================

        if (isTornadoDiving)
        {
            Vector3 targetPos3D =
                new Vector3(
                    player.position.x,
                    player.position.y + targetHeightOffset,
                    player.position.z
                );

            Vector3 toTarget = targetPos3D - transform.position;
            float dist3D = toTarget.magnitude;

            if (transform.position.y <= targetPos3D.y + 0.35f || dist3D <= 1.8f)
            {
                isTornadoDiving = false;
            }
            else if (toTarget.sqrMagnitude > 0.001f)
            {
                float diveSpd = Mathf.Max(tornadoDiveSpeed, speed);
                rb.linearVelocity =
                    Vector3.Lerp(
                        rb.linearVelocity,
                        toTarget.normalized * diveSpd,
                        Runner.DeltaTime * 14f
                    );

                PublishNetworkState();
                return;
            }
        }

        // ================= HOMING =================

        float targetY = player.position.y + targetHeightOffset;
        float minFlightY = player.position.y + 0.65f;

        Vector3 targetPos =
            new Vector3(player.position.x, targetY, player.position.z);

        Vector3 dir =
            targetPos - transform.position;

        dir.y = 0f;

        float horizDist = dir.magnitude;

        // tránh đứng im
        if (dir.sqrMagnitude < 0.001f)
        {
            dir = transform.forward;
        }

        dir.Normalize();

        Vector3 horizVel =
            new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float effectiveHoming =
            horizDist < 9f
            ? Mathf.Max(homingStrength, 6.8f)
            : homingStrength;

        horizVel =
            Vector3.Lerp(
                horizVel,
                dir * speed,
                Runner.DeltaTime * effectiveHoming
            );

        float velY = rb.linearVelocity.y;
        float parryGravity = Mathf.Max(extraGravity, 38f);

        if (parryArcTimer > 0f)
        {
            parryArcTimer -= Runner.DeltaTime;
            velY -= parryGravity * Runner.DeltaTime;

            // Khi tiến gần mục tiêu thì kết thúc arc sớm để bóng chúi thẳng vào thân player, không bay qua đầu
            if (horizDist < 7.5f && velY < 1.5f)
            {
                parryArcTimer = 0f;
            }
        }

        if (parryArcTimer <= 0f)
        {
            if (transform.position.y > targetY + 0.12f)
            {
                float timeToTarget =
                    Mathf.Max(0.08f, horizDist / Mathf.Max(speed, 12f));

                float idealDiveVelY =
                    Mathf.Clamp((targetY - transform.position.y) / timeToTarget, -34f, 1.5f);

                velY =
                    Mathf.Lerp(
                        velY - parryGravity * 0.85f * Runner.DeltaTime,
                        idealDiveVelY,
                        Runner.DeltaTime * 15f
                    );

                if (horizDist < 5.0f)
                {
                    float forcedDrop = (targetY - transform.position.y) * 14f;
                    velY = Mathf.Min(velY, forcedDrop);
                }
            }
            else
            {
                velY =
                    Mathf.Lerp(
                        velY,
                        (targetY - transform.position.y) * 12f,
                        Runner.DeltaTime * 18f
                    );
            }
        }

        if (transform.position.y < minFlightY)
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.MoveTowards(pos.y, targetY, Runner.DeltaTime * 12f);
            transform.position = pos;

            velY = Mathf.Max(velY, Mathf.Max(0f, (targetY - transform.position.y) * 9f));
        }

        float maxAllowedVel = Mathf.Max(maxSpeed * 1.5f, speed);
        if (horizVel.magnitude > maxAllowedVel)
        {
            horizVel = horizVel.normalized * maxAllowedVel;
        }

        rb.linearVelocity = new Vector3(horizVel.x, velY, horizVel.z);

        // ================= ARENA BOUNDS (CHO PHÉP BAY TOÀN MAP KỂ CẢ 4 GÓC, KHÔNG KHỰNG!) =================

        Vector3 centerPos =
            mapCenter != null
            ? mapCenter.position
            : Vector3.zero;

        Vector3 flatOffset = transform.position - centerPos;
        flatOffset.y = 0f;

        // Luôn mở rộng vùng an toàn vượt xa vị trí player mục tiêu (+28m) để bóng bay thoải mái tới mọi góc map!
        float targetFlatDist = 0f;
        if (player != null)
        {
            Vector3 tOff = player.position - centerPos;
            tOff.y = 0f;
            targetFlatDist = tOff.magnitude;
        }

        float safeRadius = Mathf.Max(maxArenaRadius, 120f, targetFlatDist + 28f);
        float safeMinY = Mathf.Min(minHeight, -10f);
        float safeMaxY = Mathf.Max(maxHeight, 85f);

        if (
            transform.position.y < safeMinY ||
            transform.position.y > safeMaxY ||
            flatOffset.magnitude > safeRadius
        )
        {
            Vector3 clampedPos = transform.position;

            if (flatOffset.magnitude > safeRadius)
            {
                Vector3 edge = centerPos + flatOffset.normalized * (safeRadius - 2f);
                clampedPos.x = edge.x;
                clampedPos.z = edge.z;
            }

            clampedPos.y = Mathf.Clamp(clampedPos.y, Mathf.Max(0.8f, targetY - 1.5f), safeMaxY - 2f);
            transform.position = clampedPos;

            Vector3 backDir = targetPos - transform.position;
            if (backDir.sqrMagnitude > 0.001f)
            {
                rb.linearVelocity = backDir.normalized * speed;
            }
            else
            {
                ResetVelocity();
            }
        }

        PublishNetworkState();
    }

    public override void Render()
    {
        if (
            Object == null ||
            Object.HasStateAuthority
        )
            return;

        if (isFakeBall)
            return;

        if (!ballActive)
        {
            transform.position = netPosition;
            return;
        }

        Vector3 predictedPosition =
            netPosition +
            netVelocity *
            remotePrediction;

        transform.position =
            Vector3.Lerp(
                transform.position,
                predictedPosition,
                Time.deltaTime * remoteSmooth
            );
    }

    void LateUpdate()
    {
        UpdateTargetIndicatorLine();
        UpdateMapThemedBallVisuals();
    }

    void OnDestroy()
    {
        if (targetLineMat != null)
        {
            Destroy(targetLineMat);
        }
        CleanupMapBallVisuals();
    }

    void EnsureTargetLine()
    {
        if (targetLine != null || isFakeBall)
            return;

        GameObject lineObj = new GameObject("TargetLockLine");
        lineObj.transform.SetParent(transform, false);

        targetLine = lineObj.AddComponent<LineRenderer>();
        targetLine.useWorldSpace = true;
        targetLine.positionCount = 2;
        targetLine.startWidth = 0.075f;
        targetLine.endWidth = 0.05f;
        targetLine.numCapVertices = 4;
        targetLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        targetLine.receiveShadows = false;
        targetLine.enabled = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        targetLineMat = new Material(shader);
        if (targetLineMat.HasProperty("_Surface"))
            targetLineMat.SetFloat("_Surface", 1f);
        if (targetLineMat.HasProperty("_SrcBlend"))
            targetLineMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (targetLineMat.HasProperty("_DstBlend"))
            targetLineMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (targetLineMat.HasProperty("_ZWrite"))
            targetLineMat.SetFloat("_ZWrite", 0f);
        if (targetLineMat.HasProperty("_BaseColor"))
            targetLineMat.SetColor("_BaseColor", Color.white);
        if (targetLineMat.HasProperty("_Color"))
            targetLineMat.SetColor("_Color", Color.white);
        targetLineMat.renderQueue = 3100;

        targetLine.sharedMaterial = targetLineMat;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1.00f, 0.12f, 0.12f), 0f),
                new GradientColorKey(new Color(1.00f, 0.25f, 0.10f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.88f, 1f)
            }
        );
        targetLine.colorGradient = g;
    }

    Transform ResolveVisualTarget()
    {
        if (Object != null && Object.IsValid && Object.HasStateAuthority && player != null)
        {
            return player;
        }

        if (!CurrentTargetId.IsValid)
        {
            cachedVisualTarget = null;
            cachedVisualTargetId = default;
            return null;
        }

        if (cachedVisualTarget != null && cachedVisualTargetId == CurrentTargetId)
        {
            return cachedVisualTarget;
        }

        if (Runner != null && Runner.TryFindObject(CurrentTargetId, out NetworkObject foundObj) && foundObj != null)
        {
            cachedVisualTarget = foundObj.transform;
            cachedVisualTargetId = CurrentTargetId;
            return cachedVisualTarget;
        }

        NetworkObject fallback = FindNetworkObject(CurrentTargetId);
        if (fallback != null)
        {
            cachedVisualTarget = fallback.transform;
            cachedVisualTargetId = CurrentTargetId;
            return cachedVisualTarget;
        }

        return null;
    }

    void UpdateTargetIndicatorLine()
    {
        if (isFakeBall)
            return;

        if (targetLine == null)
        {
            EnsureTargetLine();
        }

        if (targetLine == null)
            return;

        if (
            Object == null ||
            !Object.IsValid ||
            Runner == null ||
            !Runner.IsRunning ||
            !ballActive ||
            isResetting
        )
        {
            targetLine.enabled = false;
            return;
        }

        Transform visualTarget = ResolveVisualTarget();
        if (
            visualTarget == null ||
            !visualTarget.gameObject.activeInHierarchy ||
            PlayerFinder.IsPlayerInvisible(visualTarget)
        )
        {
            targetLine.enabled = false;
            return;
        }

        PlayerHealth targetHp = visualTarget.GetComponent<PlayerHealth>();
        if (targetHp != null && targetHp.Object != null && targetHp.Object.IsValid && targetHp.IsDead)
        {
            targetLine.enabled = false;
            return;
        }

        NetworkObject targetNet = visualTarget.GetComponent<NetworkObject>();
        bool targetingLocalPlayer = targetNet != null && targetNet.HasInputAuthority;

        float pulse = targetingLocalPlayer ? (0.02f * Mathf.Sin(Time.time * 16f)) : 0f;
        targetLine.startWidth = (targetingLocalPlayer ? 0.105f : 0.068f) + pulse;
        targetLine.endWidth = (targetingLocalPlayer ? 0.070f : 0.045f) + pulse * 0.6f;

        Vector3 startPos = transform.position;
        Vector3 endPos = visualTarget.position + Vector3.up * targetHeightOffset;

        targetLine.SetPosition(0, startPos);
        targetLine.SetPosition(1, endPos);
        targetLine.enabled = true;
    }

    // ================= PARRY =================

    public void RequestParry(
        Transform parryPlayer,
        bool isPower
    )
    {
        if (parryPlayer == null)
            return;

        NetworkObject playerObject =
            parryPlayer.GetComponent
            <NetworkObject>();

        if (playerObject == null)
            return;

        if (
            Object != null &&
            Object.HasStateAuthority
        )
        {
            Parry(
                parryPlayer,
                isPower
            );

            return;
        }

        RPC_RequestParry(
            playerObject.Id,
            isPower
        );
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    void RPC_RequestParry(
        NetworkId playerId,
        bool isPower
    )
    {
        if (Runner == null)
            return;

        NetworkObject playerObject =
            FindNetworkObject(playerId);

        if (playerObject == null)
            return;

        Parry(
            playerObject.transform,
            isPower
        );
    }

    public void Parry(
        Transform parryPlayer,
        bool isPower
    )
    {
        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        if (isResetting)
            return;

        // Không cho phép Parry cắt ngang khi bóng đang trong Tornado (tránh văng khỏi map)
        if (isInSkill)
            return;

        if (!ballActive)
            return;

        if (rb == null)
            return;

        if (parryPlayer == null)
            return;

        float dist =
            Vector3.Distance(
                parryPlayer.position,
                transform.position
            );

        float effectiveParryRange = Mathf.Max(serverParryRange, 8.8f);

        if (dist > effectiveParryRange)
        {
            Debug.Log("SERVER PARRY MISS");
            return;
        }

        StopActiveSkillAndResumeRoutines();

        if (isFreezeSlowing)
        {
            isFreezeSlowing = false;
            freezeSlowTimer = 0f;
            freezeOwner = null;
            speed = Mathf.Clamp(Mathf.Max(speed, preSkillSpeed), startSpeed, maxSpeed);
        }

        isTornadoDiving = false;
        rb.isKinematic = false;

        // ignore người vừa parry & ghi nhận để kỹ năng Niệm Lực có thể phản ngược đúng kẻ vừa parry!
        lastParryPlayer = parryPlayer;
        IgnorePlayer(parryPlayer);

        // 1. Xác định mục tiêu gần nhất TRƯỚC khi tính hướng bay
        Transform nextTarget = null;

        if (!isFakeBall)
        {
            nextTarget =
                PlayerFinder.FindClosestAlivePlayer(
                    parryPlayer.position,
                    parryPlayer
                );

            if (
                nextTarget == null ||
                nextTarget == parryPlayer ||
                PlayerFinder.IsPlayerInvisible(nextTarget)
            )
            {
                nextTarget =
                    PlayerFinder.FindClosestAlivePlayer(
                        transform.position,
                        parryPlayer
                    );
            }

            player = nextTarget;
        }

        // speed
        float speedMul =
            isPower
            ? powerSpeedMultiplier
            : speedMultiplier;

        speed =
            Mathf.Clamp(
                speed * speedMul,
                startSpeed,
                maxSpeed
            );

        // 2. Hướng cơ sở thẳng từ bóng tới mục tiêu gần nhất
        Vector3 toTargetDir;

        if (nextTarget != null)
        {
            toTargetDir =
                nextTarget.position -
                transform.position;
            toTargetDir.y = 0f;
        }
        else
        {
            toTargetDir = parryPlayer.forward;
            toTargetDir.y = 0f;
        }

        if (toTargetDir.sqrMagnitude < 0.001f)
        {
            toTargetDir = parryPlayer.forward;
            toTargetDir.y = 0f;
        }

        if (toTargetDir.sqrMagnitude < 0.001f)
        {
            toTargetDir = Vector3.forward;
        }

        toTargetDir.Normalize();

        // 3. Lệch ngang random 45-60 độ sang trái hoặc phải quanh trục Y
        //    để bóng luôn tiến cong về phía mục tiêu, không bao giờ bật ngược ra sau
        float curveSign = Random.value < 0.5f ? -1f : 1f;
        float curveAngleDeg = curveSign * Random.Range(45f, 60f);

        Vector3 horizLaunchDir =
            (
                Quaternion.AngleAxis(curveAngleDeg, Vector3.up) *
                toTargetDir
            ).normalized;

        parryArcTimer =
            isPower ? 0.32f : 0.25f;

        float rawForce =
            isPower
            ? parryForce * powerForceMultiplier
            : parryForce;

        float horizSpeed =
            Mathf.Clamp(
                rawForce,
                speed * 1.15f,
                Mathf.Max(maxSpeed * 1.45f, speed * 1.25f)
            );

        Vector3 launchVel = horizLaunchDir * horizSpeed;

        float refY = parryPlayer.position.y + targetHeightOffset;
        float bounceY =
            transform.position.y > refY + 2.0f
            ? (isPower ? 3.2f : 2.0f)
            : (isPower ? 9.0f : 6.8f);

        launchVel.y = bounceY;

        rb.linearVelocity = launchVel;

        PublishNetworkState();

        float normSpeed = Mathf.InverseLerp(startSpeed, Mathf.Max(startSpeed + 1f, maxSpeed), speed);
        int weaponId = ResolveWeaponId(parryPlayer);
        RPC_PlayParrySFX(weaponId, isPower, normSpeed);
    }

    static int ResolveWeaponId(Transform parryPlayer)
    {
        if (parryPlayer == null)
            return (int)WeaponType.WindBlade;

        WeaponElementController wep = parryPlayer.GetComponent<WeaponElementController>();
        if (wep != null)
            return (int)wep.CurrentWeapon;

        NetworkPlayer netPlayer = parryPlayer.GetComponent<NetworkPlayer>();
        if (netPlayer != null && netPlayer.Object != null && netPlayer.Object.IsValid)
            return Mathf.Clamp(netPlayer.WeaponID, 0, WeaponCatalog.All.Length - 1);

        return (int)WeaponType.WindBlade;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_PlayParrySFX(int weaponId, bool isPower, float normalizedSpeed)
    {
        WeaponType wep = (WeaponType)Mathf.Clamp(weaponId, 0, WeaponCatalog.All.Length - 1);
        GameAudioManager.PlayElementalParry(wep, isPower, normalizedSpeed);
    }

    public void BeginTornado(
        Transform owner,
        float duration,
        float radius,
        float angularSpeed,
        float riseHeight,
        float riseDuration,
        float launchSpeed
    )
    {
        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        if (!ballActive)
            return;

        if (owner == null)
            return;

        if (skillRoutine != null)
        {
            StopCoroutine(skillRoutine);
        }

        skillRoutine =
            StartCoroutine(
                TornadoRoutine(
                    owner,
                    duration,
                    radius,
                    angularSpeed,
                    riseHeight,
                    riseDuration,
                    launchSpeed
                )
            );
    }

    IEnumerator TornadoRoutine(
        Transform owner,
        float duration,
        float radius,
        float angularSpeed,
        float riseHeight,
        float riseDuration,
        float launchSpeed
    )
    {
        if (rb == null)
            yield break;

        isInSkill = true;
        isTornadoDiving = false;
        parryArcTimer = 0f;
        canMove = false;
        waitingNextLaunch = true;
        preSkillSpeed = speed;

        ResetVelocity();
        rb.isKinematic = true;

        float totalDuration = Mathf.Clamp(duration, 1.8f, 2.4f);
        float actualRise = Mathf.Clamp(riseHeight, 13f, 16f);
        float diveSpeed = Mathf.Clamp(Mathf.Max(launchSpeed, preSkillSpeed * 1.55f), 42f, 64f);

        float baseRadius = Mathf.Clamp(radius, 2.0f, 3.2f);
        float startY = owner.position.y + targetHeightOffset;
        float angle = 0f;
        float elapsed = 0f;

        // Phát hiệu ứng cột gió lốc xoáy hình phễu cho tất cả người chơi
        RPC_SpawnTornadoFunnelVFX(owner.position, actualRise, totalDuration);

        // Bóng bay XOÁY VÒNG quanh player VÀ BAY DẦN LÊN TRÊN liên tục theo hình phễu lốc xoáy thật!
        while (
            elapsed < totalDuration &&
            owner != null &&
            ballActive
        )
        {
            float t = Mathf.Clamp01(elapsed / totalDuration);

            // Tốc độ xoáy tăng tốc dần khi cuốn lên cao
            float currentAngSpeed = Mathf.Lerp(Mathf.Max(angularSpeed, 560f), 1180f, t);
            angle += currentAngSpeed * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;

            // Bán kính hình phễu: hẹp ở chân gần player -> mở rộng dần khi xoáy lên cao -> thu nhẹ ở đỉnh
            float funnelProfile = Mathf.Lerp(0.70f, 1.48f, Mathf.Sin(t * Mathf.PI * 0.78f));
            float currentRadius = baseRadius * funnelProfile;

            // Độ cao bay DẦN LÊN TRÊN liên tục từ giây đầu tiên (hình xoắn ốc lốc xoáy)
            float currentHeight = Mathf.Pow(t, 1.25f) * actualRise;

            Vector3 spiralPos =
                owner.position +
                new Vector3(
                    Mathf.Cos(rad) * currentRadius,
                    targetHeightOffset + currentHeight,
                    Mathf.Sin(rad) * currentRadius
                );

            transform.position = spiralPos;
            PublishNetworkState();

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!ballActive)
        {
            rb.isKinematic = false;
            isInSkill = false;
            skillRoutine = null;
            yield break;
        }

        Vector3 riseEnd = transform.position;
        transform.position = riseEnd;
        rb.position = riseEnd;
        Physics.SyncTransforms();
        rb.isKinematic = false;
        rb.position = riseEnd;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        isInSkill = false;
        canMove = true;
        waitingNextLaunch = false;
        speed = Mathf.Max(preSkillSpeed, startSpeed);

        Transform target =
            PlayerFinder.FindNextPlayer(owner);

        if (target != null)
        {
            player = target;
            tornadoDiveSpeed = diveSpeed;
            isTornadoDiving = true;

            Vector3 targetPos3D =
                target.position +
                Vector3.up * targetHeightOffset;

            Vector3 dir =
                targetPos3D -
                riseEnd;

            if (dir.sqrMagnitude > 0.001f)
            {
                rb.linearVelocity =
                    dir.normalized *
                    tornadoDiveSpeed;
            }
        }

        skillRoutine = null;

        PublishNetworkState();
    }

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    void RPC_SpawnTornadoFunnelVFX(Vector3 basePos, float height, float duration)
    {
        GameAudioManager.PlaySFX(GameSfxType.SkillTornado, 0.95f);
        StartCoroutine(TornadoFunnelVisualRoutine(basePos, height, duration));
    }

    static Texture2D sharedWindStreakTex;

    static Texture2D GetSoftWindStreakTexture()
    {
        if (sharedWindStreakTex != null)
            return sharedWindStreakTex;

        const int w = 128;
        const int h = 32;
        sharedWindStreakTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        sharedWindStreakTex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);
            // Mép trên/dưới mờ mịn như luồng không khí thật, lõi hơi sáng
            float across = Mathf.Pow(Mathf.Sin(v * Mathf.PI), 1.85f);

            for (int x = 0; x < w; x++)
            {
                float u = (float)x / (w - 1);
                float along = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 0.72f);
                // Sọc gió mỏng tự nhiên chạy dọc theo vệt
                float wisp = 0.74f + 0.26f * Mathf.Sin(u * 26f + v * 7f) * Mathf.Cos(v * 11f - u * 9f);
                float alpha = Mathf.Clamp01(across * along * wisp);
                float tint = Mathf.Lerp(0.82f, 1.0f, across);
                sharedWindStreakTex.SetPixel(x, y, new Color(tint, tint, tint, alpha));
            }
        }

        sharedWindStreakTex.Apply();
        return sharedWindStreakTex;
    }

    IEnumerator TornadoFunnelVisualRoutine(Vector3 basePos, float height, float duration)
    {
        GameObject vfxRoot = new GameObject("TornadoFunnelVFX");
        vfxRoot.transform.position = basePos;

        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");

        Texture2D windTex = GetSoftWindStreakTexture();
        Texture2D softDot = ParrySlashVFX.GetSoftGlowTexture();

        // 1. Vật liệu dải gió trong suốt mềm mại (Alpha-Blended nhẹ pha chút sáng trắng-xanh khí lưu)
        Material ribbonMat = new Material(sh);
        if (ribbonMat.HasProperty("_Surface")) ribbonMat.SetFloat("_Surface", 1f);
        if (ribbonMat.HasProperty("_Blend")) ribbonMat.SetFloat("_Blend", 0f);
        if (ribbonMat.HasProperty("_SrcBlend")) ribbonMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (ribbonMat.HasProperty("_DstBlend")) ribbonMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (ribbonMat.HasProperty("_ZWrite")) ribbonMat.SetFloat("_ZWrite", 0f);
        if (ribbonMat.HasProperty("_BaseColor")) ribbonMat.SetColor("_BaseColor", Color.white);
        if (ribbonMat.HasProperty("_Color")) ribbonMat.SetColor("_Color", Color.white);
        ribbonMat.mainTexture = windTex;
        if (ribbonMat.HasProperty("_BaseMap")) ribbonMat.SetTexture("_BaseMap", windTex);
        ribbonMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        ribbonMat.renderQueue = 3052;

        // 5 luồng vệt gió mảnh, vuốt nhọn hai đầu, uốn lượn tự nhiên quanh phễu lốc xoáy
        const int ribbonCount = 5;
        const int pts = 32;
        LineRenderer[] ribbons = new LineRenderer[ribbonCount];
        float[] phaseOffsets = new float[ribbonCount];
        float[] heightStarts = new float[ribbonCount];
        float[] heightSpans = new float[ribbonCount];
        float[] radiusScales = new float[ribbonCount];
        float[] speedMuls = new float[ribbonCount];

        AnimationCurve windWidthCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.00f),
            new Keyframe(0.18f, 0.24f),
            new Keyframe(0.55f, 0.34f),
            new Keyframe(0.84f, 0.18f),
            new Keyframe(1.00f, 0.00f)
        );

        Gradient windGrad = new Gradient();
        windGrad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.82f, 0.98f, 1.00f), 0.00f),
                new GradientColorKey(new Color(0.58f, 0.96f, 0.95f), 0.45f),
                new GradientColorKey(new Color(0.92f, 1.00f, 1.00f), 1.00f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.00f, 0.00f),
                new GradientAlphaKey(0.52f, 0.22f),
                new GradientAlphaKey(0.46f, 0.72f),
                new GradientAlphaKey(0.00f, 1.00f)
            }
        );

        for (int s = 0; s < ribbonCount; s++)
        {
            GameObject spGo = new GameObject("WindStreak_" + s);
            spGo.transform.SetParent(vfxRoot.transform, false);
            LineRenderer lr = spGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = pts;
            lr.widthCurve = windWidthCurve;
            lr.widthMultiplier = (s % 2 == 0) ? 0.85f : 0.58f;
            lr.colorGradient = windGrad;
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCornerVertices = 4;
            lr.numCapVertices = 2;
            lr.sharedMaterial = ribbonMat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            ribbons[s] = lr;
            phaseOffsets[s] = s * (Mathf.PI * 2f / ribbonCount);
            heightStarts[s] = (s * 0.14f) % 0.42f;
            heightSpans[s] = 0.46f + (s % 3) * 0.14f;
            radiusScales[s] = 0.84f + (s % 3) * 0.16f;
            speedMuls[s] = 0.88f + (s % 2) * 0.26f;
        }

        // 2. Lớp sương gió cuốn xoáy lên thành cột phễu mờ ảo
        GameObject mistGo = new GameObject("TornadoWindMist");
        mistGo.transform.SetParent(vfxRoot.transform, false);
        mistGo.transform.localPosition = Vector3.up * 0.35f;
        mistGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem mistPs = mistGo.AddComponent<ParticleSystem>();
        mistPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var mMain = mistPs.main;
        mMain.loop = true;
        mMain.playOnAwake = false;
        mMain.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.35f);
        mMain.startSpeed = new ParticleSystem.MinMaxCurve(3.8f, 6.4f);
        mMain.startSize = new ParticleSystem.MinMaxCurve(1.15f, 2.35f);
        mMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        mMain.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.99f, 1.00f, 0.24f),
            new Color(0.45f, 0.93f, 0.94f, 0.18f)
        );

        var mEmit = mistPs.emission;
        mEmit.rateOverTime = 34f;

        var mShape = mistPs.shape;
        mShape.enabled = true;
        mShape.shapeType = ParticleSystemShapeType.Cone;
        mShape.angle = 13f;
        mShape.radius = 1.25f;

        var mVel = mistPs.velocityOverLifetime;
        mVel.enabled = true;
        mVel.space = ParticleSystemSimulationSpace.Local;
        mVel.orbitalZ = 5.4f;

        var mCol = mistPs.colorOverLifetime;
        mCol.enabled = true;
        Gradient mistGrad = new Gradient();
        mistGrad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.88f, 0.99f, 1.00f), 0f),
                new GradientColorKey(new Color(0.55f, 0.95f, 0.96f), 0.55f),
                new GradientColorKey(new Color(0.85f, 1.00f, 1.00f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.00f, 0.00f),
                new GradientAlphaKey(0.32f, 0.22f),
                new GradientAlphaKey(0.20f, 0.72f),
                new GradientAlphaKey(0.00f, 1.00f)
            }
        );
        mCol.color = mistGrad;

        var mSize = mistPs.sizeOverLifetime;
        mSize.enabled = true;
        mSize.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(0.5f, 1.05f), new Keyframe(1f, 1.55f))
        );

        Material mistMat = new Material(sh);
        if (mistMat.HasProperty("_Surface")) mistMat.SetFloat("_Surface", 1f);
        if (mistMat.HasProperty("_Blend")) mistMat.SetFloat("_Blend", 0f);
        if (mistMat.HasProperty("_SrcBlend")) mistMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mistMat.HasProperty("_DstBlend")) mistMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mistMat.HasProperty("_ZWrite")) mistMat.SetFloat("_ZWrite", 0f);
        if (mistMat.HasProperty("_BaseColor")) mistMat.SetColor("_BaseColor", Color.white);
        if (mistMat.HasProperty("_Color")) mistMat.SetColor("_Color", Color.white);
        mistMat.mainTexture = softDot;
        if (mistMat.HasProperty("_BaseMap")) mistMat.SetTexture("_BaseMap", softDot);
        mistMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mistMat.renderQueue = 3048;

        ParticleSystemRenderer mistPsr = mistGo.GetComponent<ParticleSystemRenderer>();
        if (mistPsr != null)
        {
            mistPsr.sharedMaterial = mistMat;
            mistPsr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mistPsr.receiveShadows = false;
        }
        mistPs.Play();

        // 3. Các vệt bụi gió mảnh kéo dài bay xoáy nhanh dọc thân lốc
        GameObject gustGo = new GameObject("TornadoWindGusts");
        gustGo.transform.SetParent(vfxRoot.transform, false);
        gustGo.transform.localPosition = Vector3.up * 0.45f;
        gustGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem gustPs = gustGo.AddComponent<ParticleSystem>();
        gustPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var gMain = gustPs.main;
        gMain.loop = true;
        gMain.playOnAwake = false;
        gMain.startLifetime = new ParticleSystem.MinMaxCurve(0.50f, 0.90f);
        gMain.startSpeed = new ParticleSystem.MinMaxCurve(7.5f, 12.5f);
        gMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        gMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        gMain.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.92f, 1.00f, 1.00f, 0.72f),
            new Color(0.45f, 0.96f, 0.95f, 0.50f)
        );

        var gEmit = gustPs.emission;
        gEmit.rateOverTime = 42f;

        var gShape = gustPs.shape;
        gShape.enabled = true;
        gShape.shapeType = ParticleSystemShapeType.Cone;
        gShape.angle = 14f;
        gShape.radius = 1.35f;

        var gVel = gustPs.velocityOverLifetime;
        gVel.enabled = true;
        gVel.space = ParticleSystemSimulationSpace.Local;
        gVel.orbitalZ = 9.2f;

        Material gustMat = new Material(sh);
        if (gustMat.HasProperty("_Surface")) gustMat.SetFloat("_Surface", 1f);
        if (gustMat.HasProperty("_Blend")) gustMat.SetFloat("_Blend", 2f);
        if (gustMat.HasProperty("_SrcBlend")) gustMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (gustMat.HasProperty("_DstBlend")) gustMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (gustMat.HasProperty("_ZWrite")) gustMat.SetFloat("_ZWrite", 0f);
        if (gustMat.HasProperty("_BaseColor")) gustMat.SetColor("_BaseColor", Color.white);
        if (gustMat.HasProperty("_Color")) gustMat.SetColor("_Color", Color.white);
        gustMat.mainTexture = softDot;
        if (gustMat.HasProperty("_BaseMap")) gustMat.SetTexture("_BaseMap", softDot);
        gustMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        gustMat.renderQueue = 3058;

        ParticleSystemRenderer gustPsr = gustGo.GetComponent<ParticleSystemRenderer>();
        if (gustPsr != null)
        {
            gustPsr.renderMode = ParticleSystemRenderMode.Stretch;
            gustPsr.lengthScale = 3.6f;
            gustPsr.velocityScale = 0.08f;
            gustPsr.sharedMaterial = gustMat;
            gustPsr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            gustPsr.receiveShadows = false;
        }
        gustPs.Play();

        float elapsed = 0f;
        while (elapsed < duration && vfxRoot != null)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float curHeight = Mathf.Lerp(2.2f, height, Mathf.Pow(t, 0.85f));
            float fade = t < 0.12f
                ? Mathf.Lerp(0f, 1f, t / 0.12f)
                : (t > 0.78f ? Mathf.Lerp(1f, 0f, (t - 0.78f) / 0.22f) : 1f);

            for (int s = 0; s < ribbonCount; s++)
            {
                LineRenderer lr = ribbons[s];
                float climbShift = (elapsed * 0.42f * speedMuls[s] + heightStarts[s]) % 0.62f;
                float spinOffset = elapsed * (9.5f * speedMuls[s]) + phaseOffsets[s];

                for (int i = 0; i < pts; i++)
                {
                    float f = (float)i / (pts - 1);
                    float normH = Mathf.Clamp01(climbShift + f * heightSpans[s]);
                    float y = normH * curHeight;
                    float wobble = Mathf.Sin(f * 10f - elapsed * 11f + s) * 0.18f;
                    float r = Mathf.Lerp(1.15f, 4.10f, Mathf.Pow(normH, 0.92f)) * radiusScales[s] + wobble;
                    float a = f * Mathf.PI * 3.4f + spinOffset;
                    lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                }
            }

            Color col = new Color(0.86f, 0.99f, 1.00f, fade * 0.76f);
            if (ribbonMat.HasProperty("_BaseColor")) ribbonMat.SetColor("_BaseColor", col);
            if (ribbonMat.HasProperty("_Color")) ribbonMat.SetColor("_Color", col);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (mistPs != null) mistPs.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (gustPs != null) gustPs.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        for (int s = 0; s < ribbonCount; s++)
        {
            if (ribbons[s] != null) ribbons[s].enabled = false;
        }

        yield return new WaitForSeconds(0.65f);

        if (ribbonMat != null) Destroy(ribbonMat);
        if (mistMat != null) Destroy(mistMat);
        if (gustMat != null) Destroy(gustMat);
        if (vfxRoot != null) Destroy(vfxRoot);
    }

    // ================= BLADE BALL SKILL 1: FREEZE (DONG BANG LAM CHAM BAY VE PHIA MINH) =================

    public void BeginFreeze(Transform owner, float duration)
    {
        if (Object == null || !Object.HasStateAuthority)
            return;
        if (!ballActive || isResetting || isInSkill || owner == null)
            return;

        StopActiveSkillAndResumeRoutines();

        isInSkill = false;
        isTornadoDiving = false;
        parryArcTimer = 0f;
        canMove = true;
        waitingNextLaunch = false;
        if (rb != null) rb.isKinematic = false;

        preSkillSpeed = Mathf.Max(speed, startSpeed);
        freezeOwner = owner;
        player = owner;
        ignoreTargets.Remove(owner);

        freezeSlowTimer = Mathf.Clamp(duration, 2.2f, 3.6f);
        isFreezeSlowing = true;

        // Giảm tốc tức thì để bóng trôi êm chậm rãi về phía mình, cực dễ canh Parry!
        if (rb != null)
        {
            Vector3 toOwner = (owner.position + Vector3.up * targetHeightOffset) - transform.position;
            float slowSpd = Mathf.Clamp(preSkillSpeed * 0.28f, 5.2f, 7.2f);
            if (toOwner.sqrMagnitude > 0.001f)
            {
                rb.linearVelocity = toOwner.normalized * slowSpd;
            }
        }

        PublishNetworkState();
        RPC_SpawnFreezeVFX(transform.position, freezeSlowTimer);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_SpawnFreezeVFX(Vector3 pos, float duration)
    {
        GameAudioManager.PlaySFX(GameSfxType.SkillFreeze, 0.92f);
        StartCoroutine(FreezeVisualRoutine(pos, duration));
    }

    IEnumerator FreezeVisualRoutine(Vector3 pos, float duration)
    {
        GameObject root = new GameObject("FreezeBallVFX");
        root.transform.position = transform != null ? transform.position : pos;

        Shader litSh = Shader.Find("Universal Render Pipeline/Lit");
        if (litSh == null) litSh = Shader.Find("Standard");

        Material iceMat = new Material(litSh);
        if (iceMat.HasProperty("_Surface")) iceMat.SetFloat("_Surface", 1f);
        if (iceMat.HasProperty("_Blend")) iceMat.SetFloat("_Blend", 0f);
        if (iceMat.HasProperty("_SrcBlend")) iceMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (iceMat.HasProperty("_DstBlend")) iceMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (iceMat.HasProperty("_ZWrite")) iceMat.SetFloat("_ZWrite", 0f);
        if (iceMat.HasProperty("_BaseColor")) iceMat.SetColor("_BaseColor", new Color(0.55f, 0.92f, 1.00f, 0.48f));
        if (iceMat.HasProperty("_Smoothness")) iceMat.SetFloat("_Smoothness", 0.96f);
        if (iceMat.HasProperty("_EmissionColor"))
        {
            iceMat.EnableKeyword("_EMISSION");
            iceMat.SetColor("_EmissionColor", new Color(0.25f, 0.85f, 1.00f, 1f) * 0.92f);
        }
        iceMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        iceMat.renderQueue = 3030;

        // Khối tinh thể băng bao bọc quanh quả bóng (xoay mượt & bay theo bóng!)
        GameObject crystal1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crystal1.transform.SetParent(root.transform, false);
        crystal1.transform.localScale = Vector3.one * 1.78f;
        crystal1.transform.localRotation = Quaternion.Euler(45f, 35f, 20f);
        Destroy(crystal1.GetComponent<Collider>());
        crystal1.GetComponent<Renderer>().sharedMaterial = iceMat;

        GameObject crystal2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crystal2.transform.SetParent(root.transform, false);
        crystal2.transform.localScale = new Vector3(1.42f, 2.12f, 1.42f);
        crystal2.transform.localRotation = Quaternion.Euler(-30f, 65f, 45f);
        Destroy(crystal2.GetComponent<Collider>());
        crystal2.GetComponent<Renderer>().sharedMaterial = iceMat;

        // Hạt tuyết băng rơi vương vãi dọc đường bóng trôi chậm
        GameObject psGo = new GameObject("FreezeSnowPS");
        psGo.transform.SetParent(root.transform, false);
        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.95f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.36f);
        main.gravityModifier = 0.25f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.92f, 0.99f, 1f, 0.95f), new Color(0.28f, 0.86f, 1f, 0.85f));
        var emit = ps.emission;
        emit.rateOverTime = 42f;
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.25f;

        Shader pSh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (pSh == null) pSh = Shader.Find("Sprites/Default");
        Material pMat = new Material(pSh);
        if (pMat.HasProperty("_Surface")) pMat.SetFloat("_Surface", 1f);
        if (pMat.HasProperty("_Blend")) pMat.SetFloat("_Blend", 2f);
        if (pMat.HasProperty("_SrcBlend")) pMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (pMat.HasProperty("_DstBlend")) pMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (pMat.HasProperty("_ZWrite")) pMat.SetFloat("_ZWrite", 0f);
        Texture2D iceTex = ParrySlashVFX.GetIceCrystalTexture();
        pMat.mainTexture = iceTex;
        if (pMat.HasProperty("_BaseMap")) pMat.SetTexture("_BaseMap", iceTex);
        pMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        pMat.renderQueue = 3060;

        ParticleSystemRenderer psr = psGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.sharedMaterial = pMat;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        ps.Play();

        float elapsed = 0f;
        while (elapsed < duration && root != null && this != null)
        {
            // Nếu bóng đã bị người chơi Parry hoặc hết đóng băng -> vỡ băng lập tức!
            if (elapsed > 0.12f && Object != null && Object.IsValid && !isFreezeSlowing)
                break;

            float t = Mathf.Clamp01(elapsed / duration);
            float pop = t < 0.08f ? Mathf.Lerp(0.1f, 1.05f, t / 0.08f) : 1.0f;
            float sizeMatch = Mathf.Max(1f, transform.localScale.x / Mathf.Max(0.1f, baseBallScale.x));
            root.transform.position = transform.position;
            root.transform.localScale = Vector3.one * (pop * sizeMatch);
            crystal1.transform.Rotate(0f, 38f * Time.deltaTime, 0f, Space.World);
            crystal2.transform.Rotate(0f, -28f * Time.deltaTime, 0f, Space.World);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Vỡ tung mảnh băng khi bị đánh Parry hoặc hết thời gian đóng băng!
        if (crystal1 != null) crystal1.SetActive(false);
        if (crystal2 != null) crystal2.SetActive(false);
        if (ps != null)
        {
            main.startSpeed = new ParticleSystem.MinMaxCurve(4.5f, 11f);
            ps.Emit(40);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        yield return new WaitForSeconds(0.65f);
        if (iceMat != null) Destroy(iceMat);
        if (pMat != null) Destroy(pMat);
        if (root != null) Destroy(root);
    }

    // ================= BLADE BALL SKILL 2: TELEKINESIS (NIEM LUC PHAN CONG) =================

    public void BeginTelekinesis(
        Transform owner,
        float holdDuration,
        float speedBoost,
        float minCounterSpeed = 36f,
        float maxCounterSpeed = 56f
    )
    {
        if (Object == null || !Object.HasStateAuthority)
            return;
        if (!ballActive || isResetting || isInSkill || owner == null)
            return;

        if (skillRoutine != null)
            StopCoroutine(skillRoutine);

        skillRoutine = StartCoroutine(
            TelekinesisRoutine(
                owner,
                Mathf.Clamp(holdDuration, 0.30f, 1.20f),
                Mathf.Clamp(speedBoost, 1.50f, 2.40f),
                Mathf.Max(28f, minCounterSpeed),
                Mathf.Max(44f, maxCounterSpeed)
            )
        );
    }

    IEnumerator TelekinesisRoutine(
        Transform owner,
        float holdDuration,
        float speedBoost,
        float minCounterSpeed,
        float maxCounterSpeed
    )
    {
        if (rb == null) yield break;

        isInSkill = true;
        isTornadoDiving = false;
        parryArcTimer = 0f;
        canMove = false;
        waitingNextLaunch = true;
        preSkillSpeed = speed;

        // Xác định kẻ địch VỪA PARRY bóng tới để phản ngược thẳng vào mặt đối phương!
        Transform counterTarget = null;
        if (
            lastParryPlayer != null &&
            lastParryPlayer != owner &&
            lastParryPlayer.gameObject.activeInHierarchy &&
            !PlayerFinder.IsPlayerInvisible(lastParryPlayer)
        )
        {
            PlayerHealth lhp = lastParryPlayer.GetComponent<PlayerHealth>();
            if (lhp != null && !lhp.IsDead)
            {
                counterTarget = lastParryPlayer;
            }
        }

        Vector3 basePos = transform.position;
        ResetVelocity();
        rb.isKinematic = true;
        PublishNetworkState();

        RPC_SpawnTelekinesisVFX(basePos, owner.position, holdDuration);

        // Giữ bóng ngắn gọn (0.42s) nạp lực niệm siêu nhanh khiến đối phương khó kịp phản xạ!
        float elapsed = 0f;
        while (elapsed < holdDuration && ballActive && owner != null)
        {
            float t = Mathf.Clamp01(elapsed / holdDuration);
            float lift = Mathf.Lerp(0f, 0.65f, Mathf.Sin(t * Mathf.PI * 0.5f));
            float shake = Mathf.Sin(elapsed * 42f) * 0.08f;
            transform.position = basePos + new Vector3(shake, lift, -shake);
            PublishNetworkState();

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!ballActive)
        {
            rb.isKinematic = false;
            isInSkill = false;
            skillRoutine = null;
            yield break;
        }

        Vector3 releasePos = transform.position;
        rb.position = releasePos;
        Physics.SyncTransforms();
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        isInSkill = false;
        canMove = true;
        waitingNextLaunch = false;

        // Ưu tiên tuyệt đối phản ngược về kẻ địch VỪA PARRY tới; nếu không còn hợp lệ mới chọn mục tiêu gần nhất
        if (
            counterTarget == null ||
            !counterTarget.gameObject.activeInHierarchy ||
            PlayerFinder.IsPlayerInvisible(counterTarget)
        )
        {
            counterTarget = PlayerFinder.FindClosestAlivePlayer(owner != null ? owner.position : releasePos, owner);
            if (counterTarget == null) counterTarget = PlayerFinder.FindClosestAlivePlayer(releasePos);
        }

        float counterSpeed = Mathf.Clamp(
            Mathf.Max(preSkillSpeed, startSpeed) * speedBoost,
            minCounterSpeed,
            maxCounterSpeed
        );

        speed = Mathf.Clamp(counterSpeed, startSpeed, maxSpeed * 1.45f);
        lastParryPlayer = owner;
        IgnorePlayer(owner);

        if (counterTarget != null)
        {
            player = counterTarget;
            tornadoDiveSpeed = counterSpeed;
            isTornadoDiving = true;

            Vector3 dir = (counterTarget.position + Vector3.up * targetHeightOffset) - releasePos;
            if (dir.sqrMagnitude > 0.001f)
            {
                rb.linearVelocity = dir.normalized * counterSpeed;
            }
        }

        skillRoutine = null;
        PublishNetworkState();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_SpawnTelekinesisVFX(Vector3 ballPos, Vector3 casterPos, float duration)
    {
        GameAudioManager.PlaySFX(GameSfxType.SkillTelekinesis, 0.95f);
        StartCoroutine(TelekinesisVisualRoutine(ballPos, casterPos, duration));
    }

    IEnumerator TelekinesisVisualRoutine(Vector3 ballPos, Vector3 casterPos, float duration)
    {
        GameObject root = new GameObject("TelekinesisVFX");
        root.transform.position = ballPos;

        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");

        Material ringMat = new Material(sh);
        if (ringMat.HasProperty("_Surface")) ringMat.SetFloat("_Surface", 1f);
        if (ringMat.HasProperty("_Blend")) ringMat.SetFloat("_Blend", 2f);
        if (ringMat.HasProperty("_SrcBlend")) ringMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (ringMat.HasProperty("_DstBlend")) ringMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (ringMat.HasProperty("_ZWrite")) ringMat.SetFloat("_ZWrite", 0f);
        if (ringMat.HasProperty("_BaseColor")) ringMat.SetColor("_BaseColor", new Color(0.92f, 0.30f, 1.00f, 0.92f));
        ringMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        ringMat.renderQueue = 3060;

        // 2 vòng niệm lực tím-hồng quay chéo quanh bóng
        Transform[] rings = new Transform[2];
        for (int rIdx = 0; rIdx < 2; rIdx++)
        {
            GameObject rGo = new GameObject("PsychicRing_" + rIdx);
            rGo.transform.SetParent(root.transform, false);
            rGo.transform.localRotation = Quaternion.Euler(rIdx == 0 ? 55f : -55f, rIdx * 45f, 0f);
            LineRenderer lr = rGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            int segs = 36;
            lr.positionCount = segs;
            lr.startWidth = 0.14f;
            lr.endWidth = 0.14f;
            lr.sharedMaterial = ringMat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float rad = rIdx == 0 ? 1.65f : 2.05f;
            for (int i = 0; i < segs; i++)
            {
                float a = (float)i / segs * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad, 0f));
            }
            rings[rIdx] = rGo.transform;
        }

        // Tia liên kết niệm lực giữa người chơi và quả bóng
        GameObject beamGo = new GameObject("PsychicBeam");
        beamGo.transform.SetParent(root.transform, false);
        LineRenderer beamLr = beamGo.AddComponent<LineRenderer>();
        beamLr.useWorldSpace = true;
        beamLr.positionCount = 12;
        beamLr.startWidth = 0.10f;
        beamLr.endWidth = 0.22f;
        beamLr.sharedMaterial = ringMat;
        beamLr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        float elapsed = 0f;
        while (elapsed < duration && root != null)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 curBall = transform.position;
            root.transform.position = curBall;

            rings[0].Rotate(0f, 0f, 420f * Time.deltaTime, Space.Self);
            rings[1].Rotate(0f, 0f, -340f * Time.deltaTime, Space.Self);
            float pulse = 1.0f + 0.12f * Mathf.Sin(elapsed * 26f);
            root.transform.localScale = Vector3.one * pulse;

            Vector3 startP = casterPos + Vector3.up * 1.25f;
            for (int i = 0; i < 12; i++)
            {
                float f = i / 11f;
                Vector3 p = Vector3.Lerp(startP, curBall, f);
                float wave = Mathf.Sin(f * Mathf.PI) * Mathf.Sin(f * 16f - elapsed * 24f) * 0.22f;
                p += Vector3.up * wave;
                beamLr.SetPosition(i, p);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (ringMat != null) Destroy(ringMat);
        if (root != null) Destroy(root);
    }

    public Vector3 GetCurrentVelocity()
    {
        if (rb == null)
            return netVelocity;

        return rb.linearVelocity;
    }

    // ================= COLLISION =================

    void OnCollisionEnter(
        Collision collision
    )
    {
        // ================= FAKE BALL =================

        if (isFakeBall)
        {
            if (
                Runner != null &&
                Object != null
            )
            {
                Runner.Despawn(Object);
            }

            return;
        }

        // ================= SERVER =================

        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        if (isResetting || isInSkill)
            return;

        if (!ballActive)
            return;

        if (collision == null)
            return;

        if (collision.transform == null)
            return;

        Transform hit =
            collision.transform;

        if (hit == null)
            return;

        // ================= PLAYER =================

        PlayerHealth hp =
            hit.GetComponentInParent
            <PlayerHealth>();

        if (hp == null)
            return;

        if (hp.IsDead)
            return;

        if (hp.transform == null)
            return;

        StopActiveSkillAndResumeRoutines();

        isResetting = true;

        canMove = false;

        isInSkill = false;
        isFreezeSlowing = false;
        freezeSlowTimer = 0f;
        freezeOwner = null;
        isTornadoDiving = false;
        parryArcTimer = 0f;
        LightningChargeStacks = 0;
        nextLightningStrikeTimer = Random.Range(7.0f, 11.0f);

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        waitingNextLaunch = true;

        ResetVelocity();

        Vector3 dir =
            (
                hp.transform.position -
                transform.position
            ).normalized;

        hp.TakeDamage(1, dir);

        // reset ball
        if (mapCenter != null)
        {
            transform.position =
                mapCenter.position;
        }

        speed = startSpeed;

        player = null;

        lastParryPlayer = null;

        ignoreTargets.Clear();

        resumeRoutine = StartCoroutine(
            ResumeRoutine()
        );

        PublishNetworkState();
    }

    // ================= RESUME =================

    IEnumerator ResumeRoutine()
    {
        yield return new WaitForSeconds(
            0.5f
        );

        canMove = true;

        isResetting = false;

        speed = startSpeed;

        ResetVelocity();

        PublishNetworkState();

        yield return new WaitForSeconds(
            0.2f
        );

        player =
            PlayerFinder
            .FindClosestAlivePlayer(
                transform.position
            );

        waitingNextLaunch = false;
        resumeRoutine = null;
    }

    void StopActiveSkillAndResumeRoutines()
    {
        if (skillRoutine != null)
        {
            StopCoroutine(skillRoutine);
            skillRoutine = null;
        }

        if (resumeRoutine != null)
        {
            StopCoroutine(resumeRoutine);
            resumeRoutine = null;
        }

        if (lightningStrikeRoutine != null)
        {
            StopCoroutine(lightningStrikeRoutine);
            lightningStrikeRoutine = null;
        }
    }

    // ================= MAP-THEMED BALL VISUALS & THUNDERSTORM LIGHTNING GROWTH =================

    void CacheDefaultBallVisuals()
    {
        if (defaultBallMeshRenderer == null)
        {
            defaultBallMeshRenderer = GetComponent<MeshRenderer>();
            if (defaultBallMeshRenderer != null)
            {
                defaultBallOriginalMat = defaultBallMeshRenderer.sharedMaterial;
            }
        }

        if (defaultFireEffectGo == null)
        {
            Transform fe = transform.Find("FireEffect");
            if (fe != null)
            {
                defaultFireEffectGo = fe.gameObject;
            }
            else
            {
                FireEffect comp = GetComponentInChildren<FireEffect>(true);
                if (comp != null) defaultFireEffectGo = comp.gameObject;
            }
        }
    }

    IEnumerator ServerLightningStrikeBallRoutine()
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            yield break;

        int nextStacks = Mathf.Min(LightningChargeStacks + 1, 4);
        RPC_TriggerBallLightningStrike(nextStacks);

        yield return new WaitForSeconds(0.40f);

        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            yield break;

        if (ballActive && !isResetting && ArenaMapManager.CurrentMapType == ArenaMapType.ThunderStorm)
        {
            LightningChargeStacks = nextStacks;
            speed = Mathf.Clamp(speed * 1.07f, startSpeed, maxSpeed * 1.15f);
            PublishNetworkState();
        }

        lightningStrikeRoutine = null;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_TriggerBallLightningStrike(int newStackLevel)
    {
        GameAudioManager.PlaySFX(GameSfxType.LightningStrike, 0.90f);
        ArenaMapManager.EnsureInstance().SpawnBallLightningStrikeVFX(transform, newStackLevel);
    }

    void UpdateMapThemedBallVisuals()
    {
        if (isFakeBall)
            return;

        ArenaMapType curMap = ArenaMapManager.CurrentMapType;
        if (appliedBallMapType != curMap)
        {
            ApplyBallMapTheme(curMap);
        }

        // Phình to mượt mà mỗi khi bị Sét Đánh trên Map Khu Vực Bão Sét (tối đa +96% kích thước ở 4 tầng!)
        int stacks = (Object != null && Object.IsValid) ? Mathf.Clamp(LightningChargeStacks, 0, 4) : 0;
        float growthMul = 1f + stacks * 0.24f;
        Vector3 desiredScale = baseBallScale * growthMul;
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 8.5f);

        // Cập nhật hiệu ứng động cho Ball Công Nghệ (vòng Gyro xoay) & Ball Điện (hồ quang sét)
        if (appliedBallMapType == ArenaMapType.CyberTech)
        {
            if (techGyroOuter != null)
                techGyroOuter.Rotate(140f * Time.deltaTime, 220f * Time.deltaTime, 55f * Time.deltaTime, Space.Self);
            if (techGyroInner != null)
                techGyroInner.Rotate(-190f * Time.deltaTime, 120f * Time.deltaTime, -160f * Time.deltaTime, Space.Self);
        }
        else if (appliedBallMapType == ArenaMapType.ThunderStorm && stormBallArcs != null)
        {
            stormArcRefreshTimer -= Time.deltaTime;
            if (stormArcRefreshTimer <= 0f)
            {
                stormArcRefreshTimer = 0.045f;
                float arcRad = 0.62f + stacks * 0.04f;
                float jagAmp = 0.16f + stacks * 0.03f;
                for (int a = 0; a < stormBallArcs.Length; a++)
                {
                    LineRenderer lr = stormBallArcs[a];
                    if (lr == null) continue;
                    lr.startWidth = 0.055f + stacks * 0.015f;
                    lr.endWidth = lr.startWidth;

                    int pts = lr.positionCount;
                    Quaternion rot = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
                    for (int i = 0; i < pts; i++)
                    {
                        float ang = (i / (float)(pts - 1)) * Mathf.PI * 1.45f;
                        Vector3 p = new Vector3(Mathf.Cos(ang) * arcRad, Mathf.Sin(ang) * arcRad, 0f);
                        p += new Vector3(Random.Range(-jagAmp, jagAmp), Random.Range(-jagAmp, jagAmp), Random.Range(-jagAmp, jagAmp));
                        lr.SetPosition(i, rot * p);
                    }
                }
            }
        }
    }

    void ApplyBallMapTheme(ArenaMapType mapType)
    {
        appliedBallMapType = mapType;
        CacheDefaultBallVisuals();
        CleanupMapBallVisuals();

        if (mapType == ArenaMapType.ClassicNight)
        {
            if (defaultFireEffectGo != null) defaultFireEffectGo.SetActive(true);
            if (defaultBallMeshRenderer != null && defaultBallOriginalMat != null)
            {
                defaultBallMeshRenderer.sharedMaterial = defaultBallOriginalMat;
            }
            return;
        }

        // Tắt hiệu ứng cầu lửa ở Map Công Nghệ & Map Bão Sét!
        if (defaultFireEffectGo != null) defaultFireEffectGo.SetActive(false);

        mapBallVisualRoot = new GameObject("MapBallVisual_" + mapType);
        mapBallVisualRoot.transform.SetParent(transform, false);
        mapBallVisualRoot.transform.localPosition = Vector3.zero;
        mapBallVisualRoot.transform.localRotation = Quaternion.identity;
        mapBallVisualRoot.transform.localScale = Vector3.one;

        Shader litSh = Shader.Find("Universal Render Pipeline/Lit");
        if (litSh == null) litSh = Shader.Find("Standard");
        Shader pSh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (pSh == null) pSh = Shader.Find("Sprites/Default");

        if (mapType == ArenaMapType.CyberTech)
        {
            // === BALL CÔNG NGHỆ (CYBER PLASMA CORE + DUAL GYRO RINGS + CYAN DIGITAL TRAIL) ===
            Material coreMat = new Material(litSh);
            if (coreMat.HasProperty("_BaseColor")) coreMat.SetColor("_BaseColor", new Color(0.75f, 0.98f, 1.00f, 1f));
            if (coreMat.HasProperty("_Metallic")) coreMat.SetFloat("_Metallic", 0.55f);
            if (coreMat.HasProperty("_Smoothness")) coreMat.SetFloat("_Smoothness", 0.98f);
            if (coreMat.HasProperty("_EmissionColor"))
            {
                coreMat.EnableKeyword("_EMISSION");
                coreMat.SetColor("_EmissionColor", new Color(0.10f, 0.92f, 1.00f, 1f) * 2.4f);
            }
            ballThemeRuntimeMats.Add(coreMat);
            if (defaultBallMeshRenderer != null) defaultBallMeshRenderer.sharedMaterial = coreMat;

            Material ringMat = CreateBallAdditiveMat(pSh, new Color(0.14f, 0.95f, 1.00f, 0.95f));
            Material ringMat2 = CreateBallAdditiveMat(pSh, new Color(0.32f, 0.56f, 1.00f, 0.90f));

            techGyroOuter = CreateGyroRing(mapBallVisualRoot.transform, "TechGyroOuter", 0.72f, 0.065f, ringMat);
            techGyroInner = CreateGyroRing(mapBallVisualRoot.transform, "TechGyroInner", 0.58f, 0.055f, ringMat2);
            techGyroInner.localRotation = Quaternion.Euler(70f, 45f, 0f);

            AddBallThemeTrail(mapBallVisualRoot.transform, ringMat, new Color(0.10f, 0.94f, 1.00f), new Color(0.18f, 0.42f, 1.00f), 0.72f);
            AddBallThemeParticles(mapBallVisualRoot.transform, pSh, new Color(0.18f, 0.96f, 1.00f, 0.92f), new Color(0.24f, 0.52f, 1.00f, 0.75f), 46f, 0.14f);
        }
        else if (mapType == ArenaMapType.ThunderStorm)
        {
            // === BALL ĐIỆN MÀU TÍM RỰC RỠ (PURPLE LIGHTNING PLASMA CORE + CRACKLING VIOLET ARCS + PURPLE TRAIL) ===
            Material coreMat = new Material(litSh);
            if (coreMat.HasProperty("_BaseColor")) coreMat.SetColor("_BaseColor", new Color(0.88f, 0.56f, 1.00f, 1f));
            if (coreMat.HasProperty("_Metallic")) coreMat.SetFloat("_Metallic", 0.35f);
            if (coreMat.HasProperty("_Smoothness")) coreMat.SetFloat("_Smoothness", 0.98f);
            if (coreMat.HasProperty("_EmissionColor"))
            {
                coreMat.EnableKeyword("_EMISSION");
                coreMat.SetColor("_EmissionColor", new Color(0.74f, 0.20f, 1.00f, 1f) * 3.1f);
            }
            ballThemeRuntimeMats.Add(coreMat);
            if (defaultBallMeshRenderer != null) defaultBallMeshRenderer.sharedMaterial = coreMat;

            Material arcMat = CreateBallAdditiveMat(pSh, new Color(0.90f, 0.48f, 1.00f, 1.00f));
            Material trailMat = CreateBallAdditiveMat(pSh, new Color(0.76f, 0.24f, 1.00f, 0.96f));

            stormBallArcs = new LineRenderer[5];
            for (int i = 0; i < stormBallArcs.Length; i++)
            {
                GameObject arcGo = new GameObject("StormBallArc_" + i);
                arcGo.transform.SetParent(mapBallVisualRoot.transform, false);
                LineRenderer lr = arcGo.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.positionCount = 10;
                lr.startWidth = 0.065f;
                lr.endWidth = 0.065f;
                lr.sharedMaterial = arcMat;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                Gradient arcGrad = new Gradient();
                arcGrad.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(0.98f, 0.80f, 1.00f), 0f),
                        new GradientColorKey(new Color(0.74f, 0.22f, 1.00f), 0.55f),
                        new GradientColorKey(new Color(0.96f, 0.72f, 1.00f), 1f)
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
                lr.colorGradient = arcGrad;
                stormBallArcs[i] = lr;
            }

            AddBallThemeTrail(mapBallVisualRoot.transform, trailMat, new Color(0.95f, 0.64f, 1.00f), new Color(0.58f, 0.12f, 0.98f), 0.82f);
            AddBallThemeParticles(mapBallVisualRoot.transform, pSh, new Color(0.94f, 0.62f, 1.00f, 0.96f), new Color(0.62f, 0.16f, 1.00f, 0.88f), 62f, 0.17f);
        }
    }

    Material CreateBallAdditiveMat(Shader sh, Color col)
    {
        Material m = new Material(sh);
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 2f);
        if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        if (m.HasProperty("_Color")) m.SetColor("_Color", col);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3062;
        ballThemeRuntimeMats.Add(m);
        return m;
    }

    Transform CreateGyroRing(Transform parent, string name, float radius, float width, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        const int segs = 32;
        lr.positionCount = segs;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.sharedMaterial = mat;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        for (int i = 0; i < segs; i++)
        {
            float a = (i / (float)segs) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
        }
        return go.transform;
    }

    void AddBallThemeTrail(Transform parent, Material mat, Color startCol, Color endCol, float width)
    {
        GameObject trGo = new GameObject("MapBallTrail");
        trGo.transform.SetParent(parent, false);
        TrailRenderer tr = trGo.AddComponent<TrailRenderer>();
        tr.time = 0.24f;
        tr.startWidth = width;
        tr.endWidth = 0.04f;
        tr.minVertexDistance = 0.08f;
        tr.sharedMaterial = mat;
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(startCol, 0f), new GradientColorKey(endCol, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        tr.colorGradient = g;
    }

    void AddBallThemeParticles(Transform parent, Shader pSh, Color colA, Color colB, float rate, float size)
    {
        GameObject psGo = new GameObject("MapBallSparks");
        psGo.transform.SetParent(parent, false);
        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.65f, size * 1.45f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(colA, colB);

        var emit = ps.emission;
        emit.rateOverTime = rate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.55f;

        Material pMat = CreateBallAdditiveMat(pSh, Color.white);
        Texture2D dot = ParrySlashVFX.GetSoftGlowTexture();
        pMat.mainTexture = dot;
        if (pMat.HasProperty("_BaseMap")) pMat.SetTexture("_BaseMap", dot);

        ParticleSystemRenderer psr = psGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.sharedMaterial = pMat;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
        }
        ps.Play();
    }

    void CleanupMapBallVisuals()
    {
        techGyroOuter = null;
        techGyroInner = null;
        stormBallArcs = null;
        if (mapBallVisualRoot != null)
        {
            Destroy(mapBallVisualRoot);
            mapBallVisualRoot = null;
        }
        for (int i = 0; i < ballThemeRuntimeMats.Count; i++)
        {
            if (ballThemeRuntimeMats[i] != null) Destroy(ballThemeRuntimeMats[i]);
        }
        ballThemeRuntimeMats.Clear();
    }

    // ================= TARGET =================

    public Transform GetTarget()
    {
        return player;
    }

    public void SetTarget(
        Transform newTarget
    )
    {
        if (newTarget == null)
            return;

        PlayerHealth hp =
            newTarget.GetComponent
            <PlayerHealth>();

        if (hp == null)
            return;

        if (hp.IsDead)
            return;

        if (PlayerFinder.IsPlayerInvisible(newTarget))
            return;

        if (
            ignoreTargets.Contains(
                newTarget
            )
        )
            return;

        player = newTarget;
    }

    public void SwitchTarget(
        Transform currentPlayer
    )
    {
        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        Transform next =
            PlayerFinder.FindNextPlayer(
                currentPlayer
            );

        // fallback
        if (next == null)
        {
            next =
                PlayerFinder
                .FindClosestAlivePlayer(
                    transform.position,
                    currentPlayer
                );
        }

        if (next == null)
        {
            if (currentPlayer != null && PlayerFinder.IsPlayerInvisible(currentPlayer))
            {
                player = null;
            }
            return;
        }

        if (
            ignoreTargets.Contains(next)
        )
        {
            return;
        }

        player = next;
    }

    public void IgnorePlayer(
        Transform p
    )
    {
        if (p == null)
            return;

        if (!ignoreTargets.Contains(p))
        {
            ignoreTargets.Add(p);
        }

        StartCoroutine(
            RemoveIgnoreRoutine(p)
        );
    }

    IEnumerator RemoveIgnoreRoutine(
        Transform p
    )
    {
        yield return new WaitForSeconds(
            0.15f
        );

        if (
            ignoreTargets.Contains(p)
        )
        {
            ignoreTargets.Remove(p);
        }
    }

    public void ResetTarget()
    {
        ignoreTargets.Clear();
    }

    public void SetBallActive(
        bool active
    )
    {
        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        ballActive = active;

        if (!active)
        {
            StopActiveSkillAndResumeRoutines();
            canMove = false;
            waitingNextLaunch = true;
            isInSkill = false;
            isTornadoDiving = false;
            parryArcTimer = 0f;
            isResetting = false;
            player = null;
            ignoreTargets.Clear();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
            ResetVelocity();
        }
        else
        {
            canMove = true;
            waitingNextLaunch = false;
        }

        PublishNetworkState();
    }

    // ================= RESET =================

    void ResetVelocity()
    {
        if (rb == null || rb.isKinematic)
            return;

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;
    }

    void PublishNetworkState()
    {
        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        netPosition =
            transform.position;

        netVelocity =
            rb != null
            ? rb.linearVelocity
            : Vector3.zero;

        if (player != null && player.gameObject.activeInHierarchy)
        {
            NetworkObject targetNet = player.GetComponent<NetworkObject>();
            CurrentTargetId = (targetNet != null && targetNet.IsValid) ? targetNet.Id : default;
        }
        else
        {
            CurrentTargetId = default;
        }
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

    NetworkObject FindNetworkObject(
        NetworkId id
    )
    {
        NetworkObject[] objects =
            FindObjectsByType<NetworkObject>(
                FindObjectsSortMode.None
            );

        foreach (NetworkObject obj in objects)
        {
            if (obj == null)
                continue;

            if (obj.Id == id)
                return obj;
        }

        return null;
    }

    // ================= START MATCH =================

    public void StartMatch()
    {
        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        FindMapCenter();

        if (mapCenter == null)
        {
            Debug.LogError(
                "MapCenter Missing"
            );

            return;
        }

        StopActiveSkillAndResumeRoutines();

        canMove = true;

        isResetting = false;

        isInSkill = false;
        isFreezeSlowing = false;
        freezeSlowTimer = 0f;
        freezeOwner = null;
        isTornadoDiving = false;
        parryArcTimer = 0f;
        LightningChargeStacks = 0;
        nextLightningStrikeTimer = Random.Range(7.0f, 11.0f);

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        waitingNextLaunch = false;

        ballActive = true;

        speed = startSpeed;

        transform.position =
            mapCenter.position;

        ResetVelocity();

        ignoreTargets.Clear();

        player =
            PlayerFinder
            .FindClosestAlivePlayer(
                transform.position
            );

        PublishNetworkState();

        Debug.Log("BALL START");
    }
}
