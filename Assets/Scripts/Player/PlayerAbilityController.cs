using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fusion;

public enum SkillType
{
    None,
    CloneBall,
    Invisible,
    Tornado,
    Freeze,
    Forcefield,
    Telekinesis
}

public class PlayerAbilityController : NetworkBehaviour
{
    [Header("References")]
    public PlayerMovement movement;

    [Header("Dash")]
    public float dashForce = 46f;
    public float dashCooldown = 1.5f;

    float dashTimer;

    const float DashDuration = 0.28f;

    [Networked]
    float dashActiveTimer { get; set; }

    [Networked]
    Vector3 dashDirNet { get; set; }

    [Header("Skill")]
    public SkillType currentSkill;

    float skillTimer;
    float skillCooldown;

    [HideInInspector]
    public bool isInvisible;

    [HideInInspector]
    public bool isTornadoActive;

    [Networked]
    public NetworkBool IsInvisibleNet { get; set; }

    [Networked]
    public NetworkBool IsTornadoNet { get; set; }

    [Networked]
    public NetworkBool IsForcefieldNet { get; set; }

    [Header("Clone Ball")]
    public float cloneDuration = 5f;

    public float cloneOffset = 2f;

    [Header("Invisible")]
    public float invisibleDuration = 4.2f;

    public float invisibleSpeedMultiplier = 1.55f;

    [Header("Tornado")]
    public float tornadoDuration = 2.0f;

    public float tornadoRadius = 3f;

    public float tornadoAngularSpeed = 560f;

    public float tornadoRiseHeight = 50f;

    public float tornadoRiseDuration = 0.7f;

    public float tornadoLaunchSpeed = 62f;

    [Header("Blade Ball Skills")]
    public float freezeDuration = 2.8f;

    public float forcefieldDuration = 2.6f;

    public float forcefieldAutoParryRange = 6.2f;

    public float telekinesisHoldDuration = 0.42f;

    public float telekinesisSpeedBoost = 1.95f;

    public float telekinesisMinCounterSpeed = 36f;

    public float telekinesisMaxCounterSpeed = 56f;

    float forcefieldAutoParryCooldown;
    GameObject forcefieldDomeGo;
    Material forcefieldDomeMat;
    float forcefieldFlashTimer;
    bool wasForcefieldActive;

    CharacterController controller;

    BallController ball;

    ParrySlashVFX slashVFX;

    WeaponElementController weaponCtrl;

    PlayerSkinController skinCtrl;

    Renderer[] renderers;

    readonly Dictionary<Renderer, Material[]> savedStealthMaterials = new Dictionary<Renderer, Material[]>();
    Material ghostStealthMat;
    bool isStealthVisualApplied;
    int lastStealthWeaponVersion = -1;
    int lastStealthSkinVersion = -1;

    // ================= INIT =================

    public override void Spawned()
    {
        InitializeComponents();
    }

    void Start()
    {
        InitializeComponents();
        FindBall();
    }

    void InitializeComponents()
    {
        if (movement == null)
            movement = GetComponent<PlayerMovement>();

        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (slashVFX == null)
        {
            slashVFX = GetComponent<ParrySlashVFX>();
            if (slashVFX == null)
            {
                slashVFX = gameObject.AddComponent<ParrySlashVFX>();
            }
        }

        if (weaponCtrl == null)
            weaponCtrl = GetComponent<WeaponElementController>();

        if (renderers == null)
            renderers = GetComponentsInChildren<Renderer>();
    }

    void OnDestroy()
    {
        RestoreStealthMaterials();
        if (ghostStealthMat != null)
        {
            Destroy(ghostStealthMat);
            ghostStealthMat = null;
        }
        if (forcefieldDomeGo != null)
        {
            Destroy(forcefieldDomeGo);
            forcefieldDomeGo = null;
        }
        if (forcefieldDomeMat != null)
        {
            Destroy(forcefieldDomeMat);
            forcefieldDomeMat = null;
        }
    }

    // ================= FIXED UPDATE =================

    public override void FixedUpdateNetwork()
    {
        if (Object == null)
            return;

        if (Object.HasInputAuthority)
        {
            if (dashTimer > 0f)
                dashTimer -= Runner.DeltaTime;

            if (skillTimer > 0f)
                skillTimer -= Runner.DeltaTime;
        }

        if (Object.HasStateAuthority)
        {
            if (dashActiveTimer > 0f)
            {
                InitializeComponents();

                float t = Mathf.Clamp01(1f - (dashActiveTimer / DashDuration));
                float effectiveDashForce = Mathf.Max(dashForce, 46f);
                // Đường cong ease-out mượt mà giúp lướt xa ~6.5-7.5m mà dừng êm
                float speed = effectiveDashForce * Mathf.Pow(1f - t, 1.35f);

                if (dashDirNet.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dashDirNet, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Runner.DeltaTime * 24f);
                }

                if (controller != null && controller.enabled)
                {
                    Vector3 moveStep = dashDirNet * speed;
                    // Giữ lực bám đất nhẹ khi đang chạm đất để không bị lơ lửng khi lướt
                    moveStep.y = controller.isGrounded ? -4f : -2f;
                    controller.Move(moveStep * Runner.DeltaTime);

                    // Giữ người chơi luôn nằm trong ranh giới đấu trường ngay cả khi Dash tốc độ cao!
                    Vector3 curPos = transform.position;
                    Vector3 clampedPos = ArenaMapManager.ClampPositionToArena(curPos);
                    Vector3 clampDiff = clampedPos - curPos;
                    clampDiff.y = 0f;
                    if (clampDiff.sqrMagnitude > 0.00001f)
                    {
                        controller.Move(clampDiff);
                    }
                }

                dashActiveTimer -= Runner.DeltaTime;

                if (dashActiveTimer <= 0f)
                {
                    dashActiveTimer = 0f;

                    if (movement != null)
                    {
                        movement.isDashing = false;
                    }
                }
            }
            else if (movement != null && movement.isDashing)
            {
                movement.isDashing = false;
            }

            // FORCEFIELD (Blade Ball): Tự động phản đòn (Auto-Parry) khi bóng bay vào tầm khiên!
            if (forcefieldAutoParryCooldown > 0f)
            {
                forcefieldAutoParryCooldown -= Runner.DeltaTime;
            }

            if (IsForcefieldNet)
            {
                FindBall();
                if (
                    ball != null &&
                    ball.Object != null &&
                    ball.Object.IsValid &&
                    ball.ballActive &&
                    !ball.isInSkill &&
                    !ball.isResetting &&
                    ball.GetTarget() == transform &&
                    forcefieldAutoParryCooldown <= 0f
                )
                {
                    float dist = Vector3.Distance(transform.position, ball.transform.position);
                    if (dist <= forcefieldAutoParryRange)
                    {
                        forcefieldAutoParryCooldown = 0.42f;
                        ball.Parry(transform, true);
                        RPC_ServerPlayAttackAnim(true);
                        RPC_ForcefieldDeflectFlash();
                    }
                }
            }
        }
    }

    public override void Render()
    {
        ApplyInvisibleState();
        UpdateForcefieldVisual();
    }

    // ================= DASH =================

    public void TryDash()
    {
        if (Object == null || !Object.HasInputAuthority)
            return;

        if (dashTimer > 0f || dashActiveTimer > 0f)
            return;

        InitializeComponents();

        Vector3 dir =
            movement != null
            ? movement.GetDesiredWorldMoveDirection()
            : Vector3.zero;

        if (dir.sqrMagnitude < 0.01f && movement != null)
        {
            dir = movement.MoveDirection;
        }

        if (dir.sqrMagnitude < 0.01f)
        {
            dir = transform.forward;
        }

        dashTimer = dashCooldown;

        RPC_RequestDash(dir.normalized);
    }

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    void RPC_RequestDash(Vector3 dir)
    {
        PlayerHealth hp = GetComponent<PlayerHealth>();
        if (hp != null && hp.IsDead)
            return;

        if (dashActiveTimer > 0f)
            return;

        InitializeComponents();

        if (dir.sqrMagnitude < 0.001f)
        {
            dir = transform.forward;
        }

        dashDirNet = dir.normalized;
        dashActiveTimer = DashDuration;

        if (movement != null)
        {
            movement.isDashing = true;
        }

        RPC_PlayDashVFX(dashDirNet);
    }

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    void RPC_PlayDashVFX(Vector3 dir)
    {
        GameAudioManager.PlaySFX(GameSfxType.Dash, 0.78f);

        if (IsInvisibleNet || isInvisible)
            return;

        StartCoroutine(DashWindBurstRoutine(dir));
    }

    IEnumerator DashWindBurstRoutine(Vector3 dir)
    {
        GameObject vfxGo = new GameObject("DashWindVFX");
        vfxGo.transform.position = transform.position + Vector3.up * 0.95f;
        Vector3 backDir = dir.sqrMagnitude > 0.001f ? -dir.normalized : -transform.forward;
        vfxGo.transform.rotation = Quaternion.LookRotation(backDir, Vector3.up);

        ParticleSystem ps = vfxGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = DashDuration;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.30f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.24f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.75f, 0.96f, 1.00f, 0.85f),
            new Color(0.20f, 0.72f, 1.00f, 0.65f)
        );

        var emission = ps.emission;
        emission.rateOverTime = 55f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = 0.45f;

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f))
        );

        ParticleSystemRenderer psr = vfxGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.renderMode = ParticleSystemRenderMode.Stretch;
            psr.lengthScale = 3.2f;
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            Material m = new Material(sh);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            psr.sharedMaterial = m;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(m, 0.8f);
        }

        ps.Play();

        float elapsed = 0f;
        while (elapsed < DashDuration && vfxGo != null && this != null)
        {
            vfxGo.transform.position = transform.position + Vector3.up * 0.95f;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (vfxGo != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(vfxGo, 0.45f);
        }
    }

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.All
    )]
    public void RPC_PlayAttackAnim(bool isPower)
    {
        PlayAttackAnimLocal(isPower);
    }

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    public void RPC_ServerPlayAttackAnim(bool isPower)
    {
        PlayAttackAnimLocal(isPower);
    }

    void PlayAttackAnimLocal(bool isPower)
    {
        InitializeComponents();

        // Xoay nhẹ mặt nhân vật hướng về phía bóng để cú chém luôn chuẩn hướng bóng
        if (slashVFX != null)
        {
            Vector3 aimDir = slashVFX.GetSlashAimDirection();
            if (aimDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(aimDir, Vector3.up);
            }
        }

        // Phá hủy Fake Ball gần người parry (đồng bộ trên tất cả client)
        FakeBallVisual.TryParryNearbyFakeBalls(transform.position, 8.8f);

        if (movement != null && movement.animator != null)
        {
            movement.animator.ResetTrigger("Attack1");
            movement.animator.ResetTrigger("Attack2");
            movement.animator.SetTrigger(isPower ? "Attack2" : "Attack1");
        }

        if (slashVFX != null && !IsInvisibleNet && !isInvisible)
        {
            slashVFX.PlaySlash(isPower);
        }

        WeaponType curWeapon = weaponCtrl != null ? weaponCtrl.CurrentWeapon : WeaponType.WindBlade;
        GameAudioManager.PlayWeaponSwing(curWeapon, isPower);
    }

    // ================= SKILL =================

    public void TryUseSkill()
    {
        if (!Object.HasInputAuthority)
            return;

        if (skillTimer > 0)
            return;

        ActivateSkill();
    }

    public void SetSelectedSkill(
        SkillType skill
    )
    {
        if (!Object.HasInputAuthority)
            return;

        if (!PlayerStatsManager.IsSkillOwned(skill))
            return;

        currentSkill = skill;
        PlayerStatsManager.EquipSkill(skill);

        NetworkPlayer networkPlayer =
            GetComponent<NetworkPlayer>();

        if (networkPlayer != null)
        {
            networkPlayer.RPC_SetSkill(
                (int)skill
            );
        }
    }

    void ActivateSkill()
    {
        SkillDefinition def = SkillCatalog.Get(currentSkill);
        float cd = def != null ? def.cooldown : 15f;

        switch (currentSkill)
        {
            case SkillType.CloneBall:
                skillCooldown = cd;
                skillTimer = skillCooldown;
                RPC_RequestCloneBall();
                break;

            case SkillType.Invisible:
                skillCooldown = cd;
                skillTimer = skillCooldown;
                RPC_RequestInvisible();
                break;

            case SkillType.Tornado:
                skillCooldown = cd;
                skillTimer = skillCooldown;
                RPC_RequestTornado();
                break;

            case SkillType.Freeze:
                skillCooldown = cd;
                skillTimer = skillCooldown;
                RPC_RequestFreeze();
                break;

            case SkillType.Forcefield:
                skillCooldown = cd;
                skillTimer = skillCooldown;
                RPC_RequestForcefield();
                break;

            case SkillType.Telekinesis:
                skillCooldown = cd;
                skillTimer = skillCooldown;
                RPC_RequestTelekinesis();
                break;
        }
    }

    // ================= RPC CLONE =================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    void RPC_RequestCloneBall()
    {
        SpawnCloneBall();
    }

    // ================= RPC TORNADO =================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    void RPC_RequestTornado()
    {
        StartTornado();
    }

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    void RPC_RequestInvisible()
    {
        StartCoroutine(
            InvisibleRoutine()
        );
    }

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    void RPC_RequestFreeze()
    {
        FindBall();
        if (ball != null)
        {
            ball.BeginFreeze(transform, freezeDuration);
        }
    }

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    void RPC_RequestForcefield()
    {
        StartCoroutine(ForcefieldRoutine());
    }

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    void RPC_RequestTelekinesis()
    {
        FindBall();
        if (ball != null)
        {
            ball.BeginTelekinesis(
                transform,
                telekinesisHoldDuration,
                telekinesisSpeedBoost,
                telekinesisMinCounterSpeed,
                telekinesisMaxCounterSpeed
            );
        }
    }

    IEnumerator ForcefieldRoutine()
    {
        IsForcefieldNet = true;
        forcefieldAutoParryCooldown = 0f;
        yield return new WaitForSeconds(forcefieldDuration);
        IsForcefieldNet = false;
    }

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    void RPC_ForcefieldDeflectFlash()
    {
        forcefieldFlashTimer = 0.28f;
        GameAudioManager.PlaySFX(GameSfxType.SkillForcefieldDeflect, 0.95f);
        FakeBallVisual.TryParryNearbyFakeBalls(transform.position, forcefieldAutoParryRange + 1.5f);
    }

    void UpdateForcefieldVisual()
    {
        bool active = (bool)IsForcefieldNet;
        if (active && !wasForcefieldActive)
        {
            GameAudioManager.PlaySFX(GameSfxType.SkillForcefield, 0.88f);
        }
        wasForcefieldActive = active;
        if (forcefieldFlashTimer > 0f)
        {
            forcefieldFlashTimer = Mathf.Max(0f, forcefieldFlashTimer - Time.deltaTime);
        }

        if (!active)
        {
            if (forcefieldDomeGo != null && forcefieldDomeGo.activeSelf)
            {
                forcefieldDomeGo.SetActive(false);
            }
            return;
        }

        if (forcefieldDomeGo == null)
        {
            forcefieldDomeGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            forcefieldDomeGo.name = "ForcefieldDomeVFX";
            forcefieldDomeGo.transform.SetParent(transform, false);
            forcefieldDomeGo.transform.localPosition = Vector3.up * 1.15f;
            forcefieldDomeGo.transform.localScale = Vector3.one * 3.15f;

            Collider col = forcefieldDomeGo.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            forcefieldDomeMat = new Material(sh);
            if (forcefieldDomeMat.HasProperty("_Surface")) forcefieldDomeMat.SetFloat("_Surface", 1f);
            if (forcefieldDomeMat.HasProperty("_Blend")) forcefieldDomeMat.SetFloat("_Blend", 0f);
            if (forcefieldDomeMat.HasProperty("_SrcBlend")) forcefieldDomeMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (forcefieldDomeMat.HasProperty("_DstBlend")) forcefieldDomeMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (forcefieldDomeMat.HasProperty("_ZWrite")) forcefieldDomeMat.SetFloat("_ZWrite", 0f);
            if (forcefieldDomeMat.HasProperty("_Smoothness")) forcefieldDomeMat.SetFloat("_Smoothness", 0.95f);
            forcefieldDomeMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            forcefieldDomeMat.renderQueue = 3040;

            Renderer r = forcefieldDomeGo.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = forcefieldDomeMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        if (!forcefieldDomeGo.activeSelf)
        {
            forcefieldDomeGo.SetActive(true);
        }

        float flash = Mathf.Clamp01(forcefieldFlashTimer / 0.28f);
        float pulse = 1.0f + 0.04f * Mathf.Sin(Time.time * 12f) + flash * 0.16f;
        forcefieldDomeGo.transform.localScale = Vector3.one * (3.15f * pulse);
        forcefieldDomeGo.transform.Rotate(0f, 75f * Time.deltaTime, 0f, Space.Self);

        if (forcefieldDomeMat != null)
        {
            Color baseCol = Color.Lerp(
                new Color(0.22f, 0.86f, 1.00f, 0.24f),
                new Color(1.00f, 0.88f, 0.36f, 0.56f),
                flash
            );
            if (forcefieldDomeMat.HasProperty("_BaseColor")) forcefieldDomeMat.SetColor("_BaseColor", baseCol);
            if (forcefieldDomeMat.HasProperty("_Color")) forcefieldDomeMat.SetColor("_Color", baseCol);
            if (forcefieldDomeMat.HasProperty("_EmissionColor"))
            {
                forcefieldDomeMat.EnableKeyword("_EMISSION");
                Color em = Color.Lerp(new Color(0.14f, 0.72f, 0.96f), new Color(1.00f, 0.85f, 0.28f), flash) * (0.65f + flash * 1.6f);
                forcefieldDomeMat.SetColor("_EmissionColor", em);
            }
        }
    }

    // ================= CLONE BALL =================

    void SpawnCloneBall()
    {
        FindBall();

        if (ball == null)
            return;

        Transform target =
            PlayerFinder.FindClosestAlivePlayer(
                transform.position,
                transform
            );

        if (target == null)
        {
            target = ball.GetTarget();
        }

        if (target == null)
            return;

        Vector3 dir =
            (
                target.position -
                ball.transform.position
            );
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            dir = transform.forward;
        dir.Normalize();

        Vector3 spawnPos =
            ball.transform.position +
            dir * cloneOffset;

        NetworkObject targetNet = target.GetComponent<NetworkObject>();
        NetworkId targetId = (targetNet != null && targetNet.IsValid) ? targetNet.Id : default;
        NetworkId ownerId = (Object != null && Object.IsValid) ? Object.Id : default;
        float fakeSpeed = Mathf.Max(ball.speed * 1.15f, 18f);

        RPC_SpawnCloneVisual(
            spawnPos,
            targetId,
            ownerId,
            fakeSpeed,
            cloneDuration
        );
    }

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    void RPC_SpawnCloneVisual(
        Vector3 position,
        NetworkId targetId,
        NetworkId ownerId,
        float fakeSpeed,
        float duration
    )
    {
        GameAudioManager.PlaySFX(GameSfxType.SkillCloneBall, 0.88f);
        FindBall();

        GameObject fakeBall =
            GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );

        fakeBall.name =
            "FakeBall";

        fakeBall.tag =
            "Ball";

        fakeBall.transform.position =
            position;

        fakeBall.transform.localScale =
            ball != null
            ? ball.transform.localScale
            : Vector3.one * 1.1f;

        if (ball != null)
        {
            Renderer ballRenderer =
                ball.GetComponent<MeshRenderer>();
            if (ballRenderer == null)
                ballRenderer = ball.GetComponentInChildren<Renderer>();

            Renderer fakeRenderer =
                fakeBall.GetComponent<Renderer>();

            if (
                ballRenderer != null &&
                fakeRenderer != null
            )
            {
                fakeRenderer.sharedMaterial =
                    ballRenderer.sharedMaterial;
            }
        }

        SphereCollider col =
            fakeBall.GetComponent<SphereCollider>();

        if (col != null)
        {
            col.isTrigger = true;
        }

        Transform targetTransform = null;
        Transform ownerTransform = null;

        if (Runner != null && targetId.IsValid && Runner.TryFindObject(targetId, out NetworkObject tObj) && tObj != null)
        {
            targetTransform = tObj.transform;
        }

        if (Runner != null && ownerId.IsValid && Runner.TryFindObject(ownerId, out NetworkObject oObj) && oObj != null)
        {
            ownerTransform = oObj.transform;
        }

        if (targetTransform == null)
        {
            targetTransform = PlayerFinder.FindClosestAlivePlayer(position, ownerTransform);
        }

        FakeBallVisual fb = fakeBall.AddComponent<FakeBallVisual>();
        fb.Init(targetTransform, ownerTransform, fakeSpeed, duration);
    }

    // ================= INVISIBLE =================

    IEnumerator InvisibleRoutine()
    {
        IsInvisibleNet = true;
        isInvisible = true;

        FindBall();
        if (ball != null && ball.GetTarget() == transform)
        {
            ball.SwitchTarget(transform);
        }

        yield return new WaitForSeconds(
            invisibleDuration
        );

        IsInvisibleNet = false;
        isInvisible = false;
    }

    // ================= TORNADO =================

    void StartTornado()
    {
        FindBall();

        if (ball == null)
            return;

        IsTornadoNet = true;
        isTornadoActive = true;

        ball.BeginTornado(
            transform,
            tornadoDuration,
            tornadoRadius,
            tornadoAngularSpeed,
            Mathf.Clamp(tornadoRiseHeight, 14f, 22f),
            tornadoRiseDuration,
            Mathf.Clamp(tornadoLaunchSpeed, 48f, 64f)
        );

        StartCoroutine(
            ClearTornadoFlag()
        );
    }

    IEnumerator ClearTornadoFlag()
    {
        yield return new WaitForSeconds(
            tornadoDuration
        );

        IsTornadoNet = false;
        isTornadoActive = false;
    }

    void ApplyInvisibleState()
    {
        bool stealth = (bool)IsInvisibleNet;

        if (weaponCtrl == null)
            weaponCtrl = GetComponent<WeaponElementController>();

        if (skinCtrl == null)
            skinCtrl = GetComponent<PlayerSkinController>();

        int curWeaponVer = weaponCtrl != null ? weaponCtrl.WeaponVersion : 0;
        int curSkinVer = skinCtrl != null ? skinCtrl.SkinVersion : 0;

        if (stealth)
        {
            if (!isStealthVisualApplied)
            {
                GameAudioManager.PlaySFX(GameSfxType.SkillInvisible, 0.86f);
            }

            if (!isStealthVisualApplied || lastStealthWeaponVersion != curWeaponVer || lastStealthSkinVersion != curSkinVer)
            {
                ApplyGhostStealthMaterials();
                isStealthVisualApplied = true;
                lastStealthWeaponVersion = curWeaponVer;
                lastStealthSkinVersion = curSkinVer;
            }
        }
        else if (isStealthVisualApplied)
        {
            RestoreStealthMaterials();
            isStealthVisualApplied = false;
            lastStealthWeaponVersion = curWeaponVer;
            lastStealthSkinVersion = curSkinVer;
        }
    }

    void ApplyGhostStealthMaterials()
    {
        bool isLocal = Object != null && Object.HasInputAuthority;
        Material ghostMat = GetOrCreateGhostStealthMaterial(isLocal);

        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in allRenderers)
        {
            if (r == null)
                continue;

            // Không can thiệp vỏ hào quang, khiên Forcefield, kiếm mặc định cũ, trail hoặc hạt VFX
            if (
                r.gameObject.name == "PowerAuraShell" ||
                r.gameObject.name == "SM_Wep_Sword_03" ||
                r.gameObject.name == "ForcefieldDomeVFX"
            )
                continue;

            if (r is TrailRenderer || r is ParticleSystemRenderer || r is LineRenderer)
                continue;

            if (!savedStealthMaterials.ContainsKey(r))
            {
                savedStealthMaterials[r] = r.sharedMaterials;
            }

            int count = Mathf.Max(1, r.sharedMaterials.Length);
            Material[] ghostMats = new Material[count];
            for (int i = 0; i < count; i++)
            {
                ghostMats[i] = ghostMat;
            }

            r.sharedMaterials = ghostMats;
            r.enabled = true;
        }

        if (weaponCtrl != null)
        {
            weaponCtrl.SetStealthHidden(true);
        }
        if (skinCtrl != null)
        {
            skinCtrl.SetStealthHidden(true);
        }
    }

    void RestoreStealthMaterials()
    {
        foreach (var kv in savedStealthMaterials)
        {
            if (kv.Key != null && kv.Value != null)
            {
                kv.Key.sharedMaterials = kv.Value;
                kv.Key.enabled = true;
            }
        }
        savedStealthMaterials.Clear();

        if (weaponCtrl != null)
        {
            weaponCtrl.SetStealthHidden(false);
        }
        if (skinCtrl != null)
        {
            skinCtrl.SetStealthHidden(false);
        }
    }

    Material GetOrCreateGhostStealthMaterial(bool isLocal)
    {
        if (ghostStealthMat == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Sprites/Default");

            ghostStealthMat = new Material(sh);
            ghostStealthMat.name = "StealthGhostMaterial";

            if (ghostStealthMat.HasProperty("_Surface")) ghostStealthMat.SetFloat("_Surface", 1f);
            if (ghostStealthMat.HasProperty("_Blend")) ghostStealthMat.SetFloat("_Blend", 0f);
            if (ghostStealthMat.HasProperty("_SrcBlend")) ghostStealthMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (ghostStealthMat.HasProperty("_DstBlend")) ghostStealthMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (ghostStealthMat.HasProperty("_ZWrite")) ghostStealthMat.SetFloat("_ZWrite", 0f);
            if (ghostStealthMat.HasProperty("_Metallic")) ghostStealthMat.SetFloat("_Metallic", 0.1f);
            if (ghostStealthMat.HasProperty("_Smoothness")) ghostStealthMat.SetFloat("_Smoothness", 0.85f);

            ghostStealthMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            ghostStealthMat.renderQueue = 3000;
        }

        // Local player thấy bóng mờ rõ hơn (28% alpha) để dễ điều khiển; đối thủ chỉ thấy bóng mờ thoang thoảng (10% alpha)
        float alpha = isLocal ? 0.28f : 0.10f;
        Color col = new Color(0.76f, 0.90f, 1.00f, alpha);
        if (ghostStealthMat.HasProperty("_BaseColor")) ghostStealthMat.SetColor("_BaseColor", col);
        if (ghostStealthMat.HasProperty("_Color")) ghostStealthMat.SetColor("_Color", col);

        if (ghostStealthMat.HasProperty("_EmissionColor"))
        {
            ghostStealthMat.EnableKeyword("_EMISSION");
            Color emissive = isLocal
                ? new Color(0.14f, 0.30f, 0.44f, 1f)
                : new Color(0.04f, 0.09f, 0.14f, 1f);
            ghostStealthMat.SetColor("_EmissionColor", emissive);
        }

        return ghostStealthMat;
    }

    void FindBall()
    {
        if (ball != null)
            return;

        if (BallController.instance != null)
        {
            ball = BallController.instance;
            return;
        }

        ball =
            FindFirstObjectByType
            <BallController>();
    }

    // ================= UI =================

    public bool DashReady()
    {
        return dashTimer <= 0;
    }

    public bool SkillReady()
    {
        return skillTimer <= 0;
    }

    public float GetDashCooldownNormalized()
    {
        return dashTimer / dashCooldown;
    }

    public float GetSkillCooldownNormalized()
    {
        if (skillCooldown <= 0)
            return 0;

        return skillTimer / skillCooldown;
    }
}
