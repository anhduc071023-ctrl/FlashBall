using System.Collections;
using Fusion;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Health")]
    public float maxHealth = 2f;

    [Networked]
    public float CurrentHealth { get; set; }

    [Networked]
    public bool IsDead { get; set; }

    [Header("References")]
    public PlayerMovement player;

    public Rigidbody rb;

    public GameUI gameUI;

    [Header("Death Force")]
    public float knockbackForce = 9f;

    public float upForce = 4.2f;

    bool deathSequenceStarted;

    // ================= SPAWN =================

    public override void Spawned()
    {
        maxHealth = 2f;
        deathSequenceStarted = false;

        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
        }

        if (player == null)
            player = GetComponent<PlayerMovement>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb != null)
            rb.isKinematic = true;

        if (Object.HasInputAuthority)
        {
            gameUI = FindFirstObjectByType<GameUI>();
            UpdateUI();
        }
    }

    // ================= RENDER =================

    public override void Render()
    {
        if (Object.HasInputAuthority)
        {
            UpdateUI();
        }

        if (IsDead && !deathSequenceStarted)
        {
            StartCoroutine(DeathSequenceRoutine(Vector3.back));
        }
    }

    // ================= DAMAGE =================

    public void TakeDamage(float dmg, Vector3 hitDirection)
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsDead)
            return;

        CurrentHealth -= dmg;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        RPC_PlayHit();

        if (CurrentHealth <= 0)
        {
            Die(hitDirection);
        }
    }

    // ================= HIT =================

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_PlayHit()
    {
        GameAudioManager.PlaySFX(GameSfxType.PlayerHit, 0.92f);

        if (player != null)
        {
            player.canMove = false;

            if (player.animator != null)
            {
                player.animator.SetTrigger("Hit");
            }

            Invoke(nameof(EnableMove), 0.4f);
        }
    }

    void EnableMove()
    {
        if (!IsDead && player != null)
        {
            player.canMove = true;
        }
    }

    // ================= UI =================

    void UpdateUI()
    {
        if (!Object.HasInputAuthority)
            return;

        if (gameUI == null)
        {
            gameUI = FindFirstObjectByType<GameUI>();
        }

        if (gameUI != null)
        {
            int curHearts = Mathf.Clamp(Mathf.CeilToInt(CurrentHealth), 0, 2);
            gameUI.UpdateHealthHearts(curHearts, 2);
        }
    }

    // ================= DIE =================

    void Die(Vector3 hitDirection)
    {
        if (IsDead)
            return;

        IsDead = true;

        Debug.Log("PLAYER DIED");

        RPC_Die(hitDirection);

        if (BallController.instance != null)
        {
            BallController.instance.SwitchTarget(transform);
        }
    }

    // ================= RPC DIE =================

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_Die(Vector3 hitDirection)
    {
        GameAudioManager.PlaySFX(GameSfxType.PlayerDeath, 0.96f);

        if (!deathSequenceStarted)
        {
            StartCoroutine(DeathSequenceRoutine(hitDirection));
        }

        if (
            Object != null &&
            Object.HasInputAuthority &&
            MatchResultUI.Instance != null &&
            !MatchResultUI.Instance.IsShown
        )
        {
            int totalPlayers =
                GameManager.Instance != null
                ? Mathf.Max(2, GameManager.Instance.TotalMatchPlayers)
                : Mathf.Max(2, MatchmakingManager.TargetMatchPlayers);

            int aliveOthers = PlayerFinder.CountAlivePlayers();

            int placement = Mathf.Clamp(aliveOthers + 1, 2, totalPlayers);

            MatchResultUI.Instance.ShowResult(false, placement, totalPlayers);
        }
    }

    // ================= HIỆU ỨNG CHẾT & TAN BIẾN SẠCH XÁC =================

    IEnumerator DeathSequenceRoutine(Vector3 hitDirection)
    {
        deathSequenceStarted = true;

        if (player != null)
        {
            if (player.animator != null)
            {
                player.animator.SetTrigger("Hit");
            }
            player.canMove = false;
            player.enabled = false;
        }

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Tắt va chạm thừa để bóng không va vào xác đang tan biến
        Collider[] cols = GetComponentsInChildren<Collider>(true);
        foreach (Collider c in cols)
        {
            if (c != null) c.enabled = false;
        }

        WeaponElementController wCtrl = GetComponent<WeaponElementController>();
        if (wCtrl != null) wCtrl.SetStealthHidden(true);

        // Hiệu ứng sóng xung kích & hạt linh hồn vỡ tung ngay khi bị hạ gục
        SpawnDeathBurstVFX(transform.position + Vector3.up * 1.0f, true);

        // Hất văng & ngã xoay nhẹ bằng vật lý
        Vector3 dir = hitDirection.sqrMagnitude > 0.001f ? hitDirection.normalized : -transform.forward;
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(dir * knockbackForce + Vector3.up * upForce, ForceMode.Impulse);
            rb.AddTorque(transform.right * 3.5f + transform.forward * 1.5f, ForceMode.Impulse);
        }

        // Chờ 0.55s cho động tác văng ngã rõ nét
        yield return new WaitForSeconds(0.55f);

        // Khóa Rigidbody và bắt đầu tan biến / thu nhỏ xác trong 0.45s
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        Vector3 startScale = transform.localScale;
        float dissolveDur = 0.45f;
        float elapsed = 0f;
        while (elapsed < dissolveDur)
        {
            float t = Mathf.Clamp01(elapsed / dissolveDur);
            float ease = 1f - t * t;
            transform.localScale = startScale * Mathf.Max(0.02f, ease);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Bùng khói/hạt tan biến cuối cùng rồi ẨN HOÀN TOÀN xác + vũ khí!
        SpawnDeathBurstVFX(transform.position + Vector3.up * 0.6f, false);
        HideAllVisuals();
    }

    void HideAllVisuals()
    {
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in allRenderers)
        {
            if (r != null) r.enabled = false;
        }

        ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in allParticles)
        {
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void SpawnDeathBurstVFX(Vector3 pos, bool isInitialHit)
    {
        // 1. Vụ nổ hạt năng lượng đỏ-cam / trắng-xanh khi chết & tan biến
        GameObject vfxGo = new GameObject(isInitialHit ? "PlayerDeathHitVFX" : "PlayerDeathVanishVFX");
        vfxGo.transform.position = pos;

        ParticleSystem ps = vfxGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.35f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.72f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(isInitialHit ? 3.5f : 1.5f, isInitialHit ? 10.5f : 5.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(isInitialHit ? 0.18f : 0.28f, isInitialHit ? 0.45f : 0.65f);
        main.gravityModifier = isInitialHit ? 0.15f : -0.45f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = isInitialHit
            ? new ParticleSystem.MinMaxGradient(new Color(1.00f, 0.24f, 0.18f, 0.96f), new Color(1.00f, 0.76f, 0.22f, 0.90f))
            : new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.90f, 1.00f, 0.85f), new Color(1.00f, 0.38f, 0.32f, 0.72f));

        var emit = ps.emission;
        emit.rateOverTime = 0f;
        emit.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)(isInitialHit ? 36 : 28))
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = isInitialHit ? 0.45f : 0.65f;

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f))
        );

        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        Material mat = new Material(sh);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        Texture2D dot = ParrySlashVFX.GetSoftGlowTexture();
        if (dot != null)
        {
            mat.mainTexture = dot;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", dot);
        }
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3065;

        ParticleSystemRenderer psr = vfxGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.sharedMaterial = mat;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
        }

        ps.Play();
        Destroy(mat, 1.1f);
        Destroy(vfxGo, 1.0f);

        // 2. Vòng sóng đỏ lan nhanh trên mặt đất khi vừa bị hạ gục
        if (isInitialHit)
        {
            StartCoroutine(DeathShockwaveRingRoutine(pos));
        }
    }

    IEnumerator DeathShockwaveRingRoutine(Vector3 pos)
    {
        GameObject ringGo = new GameObject("DeathShockwaveRing");
        ringGo.transform.position = new Vector3(pos.x, pos.y - 0.65f, pos.z);

        LineRenderer lr = ringGo.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        const int segs = 32;
        lr.positionCount = segs;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        Material mat = new Material(sh);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3062;
        lr.sharedMaterial = mat;

        float dur = 0.42f;
        float elapsed = 0f;
        while (elapsed < dur && ringGo != null)
        {
            float t = Mathf.Clamp01(elapsed / dur);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            float r = Mathf.Lerp(0.4f, 4.2f, ease);
            lr.startWidth = Mathf.Lerp(0.32f, 0.02f, t);
            lr.endWidth = lr.startWidth;

            for (int i = 0; i < segs; i++)
            {
                float a = (float)i / segs * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }

            Color c = new Color(1.00f, 0.24f, 0.16f, (1f - t) * 0.92f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (mat != null) Destroy(mat);
        if (ringGo != null) Destroy(ringGo);
    }

    // ================= HEAL =================

    public void Heal(float value)
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsDead)
            return;

        CurrentHealth += value;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
    }
}
