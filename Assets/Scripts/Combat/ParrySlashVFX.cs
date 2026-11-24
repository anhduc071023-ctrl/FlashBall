using System.Collections;
using UnityEngine;

public class ParrySlashVFX : MonoBehaviour
{
    [Header("Sword Trail (Slim & Smooth)")]
    public TrailRenderer swordTrail;

    [Header("Timing")]
    public float normalDuration = 0.24f;
    public float powerDuration = 0.34f;

    [Header("Slash Arc Geometry (Compact, Crisp & Camera-Readable)")]
    public float normalRadius = 2.65f;
    public float powerRadius = 3.45f;
    public float slashHeight = 1.25f;
    public float slashForwardOffset = 0.48f;

    static Material sharedAdditiveMat;
    static Material sharedAlphaMat;
    static Texture2D sharedSlashTex;
    static Texture2D sharedSparkTex;
    static Texture2D sharedIceCrystalTex;
    static Mesh sharedIceShardMesh;

    Coroutine trailRoutine;
    WeaponElementController weaponCtrl;

    void Awake()
    {
        EnsureSwordTrail();
        weaponCtrl = GetComponent<WeaponElementController>();
        ConfigureTrailAppearance(false, GetCurrentWeaponDef());
        DisableTrailImmediate();
    }

    void OnEnable()
    {
        EnsureSwordTrail();
        DisableTrailImmediate();
    }

    void OnDisable()
    {
        DisableTrailImmediate();
    }

    public static Texture2D GetSoftGlowTexture()
    {
        return GetSharedSparkTexture();
    }

    public static Texture2D GetIceCrystalTexture()
    {
        if (sharedIceCrystalTex != null)
            return sharedIceCrystalTex;

        const int size = 64;
        sharedIceCrystalTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        sharedIceCrystalTex.wrapMode = TextureWrapMode.Clamp;

        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);

                // Hình sao băng 6 cánh sắc nét kết hợp lõi sáng tinh thể
                float spokes = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 3f)), 7f);
                float starArm = spokes * Mathf.Clamp01(1f - r * 1.02f);
                float core = Mathf.Clamp01(1f - r * 2.1f);
                core = core * core;
                float diamond = Mathf.Clamp01(1f - (Mathf.Abs(dx) + Mathf.Abs(dy)) * 1.18f);

                float alpha = Mathf.Clamp01(starArm * 0.95f + core * 0.85f + diamond * 0.45f);
                float bright = Mathf.Lerp(0.80f, 1.00f, Mathf.Clamp01(core + starArm * 0.5f));
                sharedIceCrystalTex.SetPixel(x, y, new Color(bright, bright, 1f, alpha));
            }
        }

        sharedIceCrystalTex.Apply();
        return sharedIceCrystalTex;
    }

    WeaponDefinition GetCurrentWeaponDef()
    {
        if (weaponCtrl == null)
            weaponCtrl = GetComponent<WeaponElementController>();

        WeaponType wt = weaponCtrl != null ? weaponCtrl.CurrentWeapon : WeaponType.WindBlade;
        return WeaponCatalog.Get(wt);
    }

    public void EnsureSwordTrail()
    {
        if (swordTrail != null)
            return;

        TrailRenderer[] trails = GetComponentsInChildren<TrailRenderer>(true);
        foreach (TrailRenderer tr in trails)
        {
            if (tr != null)
            {
                swordTrail = tr;
                break;
            }
        }
    }

    public void DisableTrailImmediate()
    {
        if (trailRoutine != null)
        {
            StopCoroutine(trailRoutine);
            trailRoutine = null;
        }

        if (swordTrail != null)
        {
            swordTrail.emitting = false;
            swordTrail.Clear();
        }
    }

    public void PlaySlash(bool isPower)
    {
        EnsureSwordTrail();
        if (weaponCtrl == null)
            weaponCtrl = GetComponent<WeaponElementController>();

        WeaponDefinition def = GetCurrentWeaponDef();
        WeaponType wt = def.type;

        // Parry chuẩn (Power Parry): giữ CÙNG màu nguyên tố, kích hoạt hào quang rực rỡ trên vũ khí!
        if (isPower && weaponCtrl != null)
        {
            weaponCtrl.TriggerPowerAura(1.25f);
        }

        if (trailRoutine != null)
        {
            StopCoroutine(trailRoutine);
        }

        trailRoutine = StartCoroutine(SwordTrailRoutine(isPower, def));

        float sizeBoost = wt == WeaponType.VoidScythe ? 1.12f : 1.0f;
        float radius = (isPower ? 3.45f : 2.65f) * sizeBoost;
        float bladeWidth = isPower ? 1.32f : 0.96f;
        float arcDeg = isPower ? 215f : 185f;
        float dur = isPower ? 0.34f : 0.26f;

        // 1. Vết chém vòng cung chéo gọn đẹp & sắc nét ngay trước mặt nhân vật
        SpawnElementCrescent(def, isPower, -46f, 28f, 1f, radius, bladeWidth, arcDeg, dur);

        // Song Kiếm Băng luôn chém đôi; mọi vũ khí khi Power Parry đều có nhát chữ X rõ rệt hơn
        if (isPower || wt == WeaponType.FrostDualBlades)
        {
            float rad2 = isPower ? radius * 0.95f : radius * 0.88f;
            float w2 = isPower ? bladeWidth * 0.90f : bladeWidth * 0.78f;
            SpawnElementCrescent(def, isPower, 46f, 26f, -1f, rad2, w2, arcDeg * 0.95f, dur * 0.95f);
        }

        // 2. Vòng tròn năng lượng đặc trưng theo từng nguyên tố — gọn gần người & RÕ NÉT cho cả 5 vũ khí!
        SpawnSignatureElementRing(def, isPower);

        // 3. Hiệu ứng hạt mượt (hình tròn mềm, KHÔNG vuông pixel!) đặc trưng từng nguyên tố
        SpawnElementSparks(def, isPower);
    }

    IEnumerator SwordTrailRoutine(bool isPower, WeaponDefinition def)
    {
        if (swordTrail == null)
            yield break;

        ConfigureTrailAppearance(isPower, def);
        swordTrail.Clear();
        swordTrail.emitting = true;

        // Giữ vệt kiếm ngắn & gọn đúng khoảnh khắc vung tay
        float activeTime = isPower ? 0.16f : 0.12f;
        yield return new WaitForSeconds(activeTime);

        if (swordTrail != null)
        {
            swordTrail.emitting = false;
        }

        trailRoutine = null;
    }

    void ConfigureTrailAppearance(bool isPower, WeaponDefinition def)
    {
        if (swordTrail == null)
            return;

        // Thu nhỏ mạnh vệt kiếm từ cẳng tay/kiếm để thanh thoát, không bị mảng đa giác to thô che người
        swordTrail.time = isPower ? 0.14f : 0.10f;
        swordTrail.minVertexDistance = 0.015f;
        swordTrail.autodestruct = false;
        swordTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        swordTrail.receiveShadows = false;
        swordTrail.numCornerVertices = 6;
        swordTrail.numCapVertices = 6;

        AnimationCurve widthCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.02f),
            new Keyframe(0.20f, isPower ? 0.30f : 0.19f),
            new Keyframe(0.65f, isPower ? 0.14f : 0.08f),
            new Keyframe(1.00f, 0.00f)
        );
        swordTrail.widthCurve = widthCurve;
        swordTrail.widthMultiplier = 1f;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(def.coreColor, 0.00f),
                new GradientColorKey(def.primaryColor, 0.40f),
                new GradientColorKey(def.secondaryColor, 1.00f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(isPower ? 0.92f : 0.72f, 0.00f),
                new GradientAlphaKey(isPower ? 0.65f : 0.42f, 0.50f),
                new GradientAlphaKey(0.00f, 1.00f)
            }
        );

        swordTrail.colorGradient = grad;
        swordTrail.sharedMaterial = GetSharedAdditiveMaterial();
    }

    // ================= VÒNG TRÒN NĂNG LƯỢNG ĐẶC TRƯNG 5 NGUYÊN TỐ (GỌN & RÕ) =================

    void SpawnSignatureElementRing(WeaponDefinition def, bool isPower)
    {
        float baseMaxRadius = isPower ? 2.85f : 2.10f;
        float ringWidth = isPower ? 0.28f : 0.18f;
        float dur = isPower ? 0.34f : 0.25f;

        switch (def.type)
        {
            case WeaponType.WindBlade:
                // GIÓ (giữ nguyên bản đẹp): 2 vòng lốc gió xanh ngọc đồng tâm xoay nhanh gọn trước mặt
                SpawnCustomRing(def, baseMaxRadius, ringWidth, dur, 36, RingStyle.WindSwirl, 0f, false);
                SpawnCustomRing(def, baseMaxRadius * 0.72f, ringWidth * 0.8f, dur * 0.9f, 36, RingStyle.WindSwirl, 90f, true);
                break;

            case WeaponType.FlameSaber:
                // LỬA: Vòng lửa mượt rực nóng + luồng lửa cháy bùng vàng-cam-đỏ như quả cầu Fire Ball!
                SpawnCustomRing(def, baseMaxRadius * 1.02f, ringWidth * 1.10f, dur, 36, RingStyle.Smooth, 0f, false);
                if (isPower)
                    SpawnCustomRing(def, baseMaxRadius * 0.70f, ringWidth * 0.90f, dur * 0.85f, 36, RingStyle.Smooth, 0f, true);
                SpawnFireBallSlashWave(isPower);
                break;

            case WeaponType.ThunderSpear:
                // SẤM SÉT (trả về bản cũ đẹp hơn): Vòng điện mượt + các tia sét zig-zag phóng dài thẳng về phía bóng!
                SpawnCustomRing(def, baseMaxRadius, ringWidth, dur, 36, RingStyle.Smooth, 0f, false);
                SpawnLightningBolts(def, isPower ? 6 : 3, isPower ? 6.5f : 4.5f);
                break;

            case WeaponType.FrostDualBlades:
                // BĂNG: Vòng sương băng mềm + mảnh tinh thể băng 3D nhọn sắc bắn tỏa + sương tuyết lấp lánh!
                SpawnCustomRing(def, baseMaxRadius * 0.98f, ringWidth * 0.95f, dur, 36, RingStyle.Smooth, 0f, false);
                SpawnIceCrystalShards(def, isPower);
                SpawnFrostSlashBurst(isPower);
                break;

            case WeaponType.VoidScythe:
                // BÓNG TỐI / HƯ KHÔNG: Vòng hút-nở tím đậm + ngọn LỬA ĐEN-TÍM bùng lên cực đặc trưng!
                SpawnCustomRing(def, baseMaxRadius * 1.12f, ringWidth * 1.25f, dur * 1.05f, 40, RingStyle.Smooth, 0f, false);
                SpawnCustomRing(def, baseMaxRadius * 0.78f, ringWidth * 0.90f, dur * 0.95f, 40, RingStyle.VoidPulse, 0f, true);
                SpawnVoidDarkFlameBurst(isPower);
                break;
        }
    }

    enum RingStyle
    {
        Smooth,
        WindSwirl,
        FlameBurst,
        ElectricJagged,
        VoidPulse
    }

    void SpawnCustomRing(
        WeaponDefinition def,
        float targetRadius,
        float startWidth,
        float duration,
        int segments,
        RingStyle style,
        float initialRollDeg,
        bool isInnerCore
    )
    {
        GameObject ringGo = new GameObject("ElementRing_" + def.type);
        Vector3 forward = GetSlashAimDirection();

        // Kéo gọn lại gần player hơn (0.68m thay vì 1.15m) để vòng tròn luôn nằm trọn trong khung hình camera!
        ringGo.transform.position =
            transform.position +
            Vector3.up * Mathf.Max(slashHeight, 1.25f) +
            forward * 0.68f;

        ringGo.transform.rotation =
            Quaternion.LookRotation(forward, Vector3.up) *
            Quaternion.Euler(14f, 0f, initialRollDeg);

        LineRenderer lr = ringGo.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = (style != RingStyle.WindSwirl);
        lr.positionCount = segments;
        lr.startWidth = startWidth;
        lr.endWidth = style == RingStyle.WindSwirl ? startWidth * 0.25f : startWidth;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        // Dùng Alpha đậm cho vòng chính (cực rõ kể cả trên nền sáng!) và Additive cho vòng lõi sáng
        Material mat = new Material(isInnerCore ? GetSharedAdditiveMaterial() : GetSharedAlphaSlashMaterial());
        Color startCol = isInnerCore ? def.coreColor : Color.Lerp(def.primaryColor, def.coreColor, 0.35f);
        startCol.a = 0.98f;
        SetMaterialColor(mat, startCol);
        lr.sharedMaterial = mat;

        StartCoroutine(AnimateCustomRing(ringGo, lr, mat, startCol, def.primaryColor, targetRadius, startWidth, duration, style));
    }

    IEnumerator AnimateCustomRing(
        GameObject ringGo,
        LineRenderer lr,
        Material mat,
        Color startCol,
        Color endCol,
        float maxRadius,
        float baseWidth,
        float duration,
        RingStyle style
    )
    {
        float elapsed = 0f;
        int segs = lr.positionCount;
        Quaternion baseRot = ringGo.transform.rotation;

        while (elapsed < duration && ringGo != null)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            float r;
            if (style == RingStyle.VoidPulse)
            {
                // Co nhẹ vào trong rồi bùng rộng ra cực ma mị
                r = t < 0.25f
                    ? Mathf.Lerp(maxRadius * 0.72f, maxRadius * 0.42f, t / 0.25f)
                    : Mathf.Lerp(maxRadius * 0.42f, maxRadius * 1.15f, (t - 0.25f) / 0.75f);
            }
            else
            {
                r = Mathf.Lerp(0.45f, maxRadius, ease);
            }

            if (style == RingStyle.WindSwirl)
            {
                ringGo.transform.rotation = baseRot * Quaternion.Euler(0f, 0f, elapsed * 540f);
            }
            else if (style == RingStyle.Smooth && segs == 6)
            {
                ringGo.transform.rotation = baseRot * Quaternion.Euler(0f, 0f, elapsed * 90f);
            }

            for (int i = 0; i < segs; i++)
            {
                float frac = (float)i / (style == RingStyle.WindSwirl ? (segs - 1) : segs);
                float ang = style == RingStyle.WindSwirl ? frac * Mathf.PI * 1.55f : frac * Mathf.PI * 2f;
                float radMod = r;

                if (style == RingStyle.FlameBurst)
                {
                    radMod += Mathf.Sin(ang * 8f + elapsed * 24f) * (0.24f * (1f - t * 0.4f));
                }
                else if (style == RingStyle.ElectricJagged)
                {
                    radMod += ((i % 2 == 0) ? 1f : -1f) * (0.22f * (1f - t * 0.3f));
                }

                lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * radMod, Mathf.Sin(ang) * radMod, 0f));
            }

            float w = Mathf.Lerp(baseWidth, baseWidth * 0.18f, t);
            lr.startWidth = w;
            lr.endWidth = style == RingStyle.WindSwirl ? w * 0.2f : w;

            Color c = Color.Lerp(startCol, endCol, t * 0.7f);
            c.a = t < 0.2f ? 0.98f : Mathf.Lerp(0.98f, 0f, (t - 0.2f) / 0.8f);
            SetMaterialColor(mat, c);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (mat != null) Destroy(mat);
        if (ringGo != null) Destroy(ringGo);
    }

    // ================= HỎA ĐAO: SÓNG LỬA CHÁY RỰC NHƯ FIRE BALL =================

    void SpawnFireBallSlashWave(bool isPower)
    {
        Vector3 forward = GetSlashAimDirection();
        Vector3 origin = transform.position + Vector3.up * slashHeight + forward * 0.78f;

        GameObject fireGo = new GameObject("FlameSlashFireBallWave");
        fireGo.transform.position = origin;
        fireGo.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        ParticleSystem ps = fireGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.25f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, isPower ? 0.62f : 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, isPower ? 7.5f : 5.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.42f, isPower ? 0.98f : 0.72f);
        main.gravityModifier = -0.90f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(1.00f, 0.86f, 0.14f, 1.00f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)(isPower ? 48 : 28))
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = isPower ? 58f : 48f;
        shape.radius = isPower ? 1.15f : 0.85f;

        var colLife = ps.colorOverLifetime;
        colLife.enabled = true;
        Gradient fireGrad = new Gradient();
        fireGrad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1.00f, 0.98f, 0.18f), 0.00f),
                new GradientColorKey(new Color(1.00f, 0.48f, 0.02f), 0.36f),
                new GradientColorKey(new Color(1.00f, 0.14f, 0.00f), 0.70f),
                new GradientColorKey(new Color(0.65f, 0.03f, 0.00f), 1.00f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.98f, 0.00f),
                new GradientAlphaKey(0.90f, 0.45f),
                new GradientAlphaKey(0.45f, 0.76f),
                new GradientAlphaKey(0.00f, 1.00f)
            }
        );
        colLife.color = fireGrad;

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 0.60f), new Keyframe(0.28f, 1.12f), new Keyframe(1f, 0.18f))
        );

        ParticleSystemRenderer psr = fireGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            Material mat = new Material(GetSharedAdditiveMaterial());
            Texture2D dot = GetSharedSparkTexture();
            mat.mainTexture = dot;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", dot);
            psr.sharedMaterial = mat;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
            Destroy(mat, 0.85f);
        }

        ps.Play();
        Destroy(fireGo, 0.80f);
    }

    // ================= SONG KIẾM BĂNG: MẢNH TINH THỂ BĂNG 3D + SƯƠNG TUYẾT =================

    static Mesh GetSharedIceShardMesh()
    {
        if (sharedIceShardMesh != null)
            return sharedIceShardMesh;

        // Tinh thể băng hình kim cương 6 cạnh nhọn 2 đầu (icicle diamond)
        Mesh mesh = new Mesh();
        mesh.name = "IceCrystalShardMesh";

        const int sides = 6;
        Vector3[] verts = new Vector3[sides + 2];
        verts[0] = new Vector3(0f, 0f, -0.30f); // đuôi nhọn
        verts[sides + 1] = new Vector3(0f, 0f, 0.72f); // mũi nhọn dài về phía trước

        for (int i = 0; i < sides; i++)
        {
            float a = (float)i / sides * Mathf.PI * 2f;
            verts[i + 1] = new Vector3(Mathf.Cos(a) * 0.15f, Mathf.Sin(a) * 0.15f, 0.08f);
        }

        int[] tris = new int[sides * 6];
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            int idx = i * 6;
            // Nửa sau
            tris[idx] = 0;
            tris[idx + 1] = next + 1;
            tris[idx + 2] = i + 1;
            // Nửa trước (mũi băng nhọn)
            tris[idx + 3] = sides + 1;
            tris[idx + 4] = i + 1;
            tris[idx + 5] = next + 1;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        sharedIceShardMesh = mesh;
        return sharedIceShardMesh;
    }

    void SpawnIceCrystalShards(WeaponDefinition def, bool isPower)
    {
        Vector3 forward = GetSlashAimDirection();
        Vector3 origin = transform.position + Vector3.up * Mathf.Max(slashHeight, 1.25f) + forward * 0.65f;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        int count = isPower ? 11 : 7;
        float dur = isPower ? 0.42f : 0.32f;

        Mesh shardMesh = GetSharedIceShardMesh();

        for (int i = 0; i < count; i++)
        {
            float fanT = count > 1 ? ((float)i / (count - 1) - 0.5f) * 2f : 0f;
            float yaw = fanT * (isPower ? 58f : 48f) + Random.Range(-6f, 6f);
            float pitch = Random.Range(-26f, 26f);

            Vector3 dir = (Quaternion.Euler(pitch, yaw, 0f) * forward).normalized;

            GameObject shardGo = new GameObject("IceCrystalShard_" + i);
            shardGo.transform.position = origin + right * (fanT * 0.55f) + Vector3.up * Random.Range(-0.22f, 0.25f);
            shardGo.transform.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            float s = Random.Range(isPower ? 0.72f : 0.55f, isPower ? 1.25f : 0.92f);
            shardGo.transform.localScale = new Vector3(s * 0.85f, s * 0.85f, s * 1.45f);

            MeshFilter mf = shardGo.AddComponent<MeshFilter>();
            mf.sharedMesh = shardMesh;
            MeshRenderer mr = shardGo.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            Material mat = new Material(GetSharedAlphaSlashMaterial());
            Color iceCol = (i % 2 == 0)
                ? new Color(0.76f, 0.97f, 1.00f, 0.95f)
                : new Color(0.24f, 0.82f, 1.00f, 0.92f);
            SetMaterialColor(mat, iceCol);
            mr.sharedMaterial = mat;

            float speed = Random.Range(isPower ? 6.5f : 4.5f, isPower ? 10.5f : 7.5f);
            StartCoroutine(AnimateIceShard(shardGo, mat, iceCol, dir, speed, dur));
        }
    }

    IEnumerator AnimateIceShard(GameObject shardGo, Material mat, Color startCol, Vector3 dir, float speed, float duration)
    {
        float elapsed = 0f;
        Vector3 startScale = shardGo.transform.localScale;
        float spinRate = Random.Range(-340f, 340f);

        while (elapsed < duration && shardGo != null)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float stepSpeed = speed * Mathf.Pow(1f - t, 1.4f);
            shardGo.transform.position += dir * (stepSpeed * Time.deltaTime);
            shardGo.transform.Rotate(0f, 0f, spinRate * Time.deltaTime, Space.Self);

            float scaleMul = t < 0.15f
                ? Mathf.Lerp(0.35f, 1.05f, t / 0.15f)
                : Mathf.Lerp(1.05f, 0.15f, (t - 0.15f) / 0.85f);
            shardGo.transform.localScale = startScale * scaleMul;

            Color c = startCol;
            c.a = startCol.a * (1f - t * t);
            SetMaterialColor(mat, c);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (mat != null) Destroy(mat);
        if (shardGo != null) Destroy(shardGo);
    }

    void SpawnFrostSlashBurst(bool isPower)
    {
        Vector3 forward = GetSlashAimDirection();
        Vector3 origin = transform.position + Vector3.up * slashHeight + forward * 0.78f;

        // 1. Màn sương lạnh trắng-xanh tỏa ra rõ rệt
        GameObject mistGo = new GameObject("FrostSlashMist");
        mistGo.transform.position = origin;
        mistGo.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        ParticleSystem mPs = mistGo.AddComponent<ParticleSystem>();
        mPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var mMain = mPs.main;
        mMain.loop = false;
        mMain.playOnAwake = false;
        mMain.duration = 0.25f;
        mMain.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, isPower ? 0.70f : 0.55f);
        mMain.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, isPower ? 4.8f : 3.2f);
        mMain.startSize = new ParticleSystem.MinMaxCurve(0.65f, isPower ? 1.35f : 1.00f);
        mMain.gravityModifier = 0.18f;
        mMain.simulationSpace = ParticleSystemSimulationSpace.World;
        mMain.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.86f, 0.98f, 1.00f, 0.44f),
            new Color(0.30f, 0.84f, 1.00f, 0.34f)
        );

        var mEmit = mPs.emission;
        mEmit.rateOverTime = 0f;
        mEmit.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)(isPower ? 24 : 15)) });

        var mShape = mPs.shape;
        mShape.enabled = true;
        mShape.shapeType = ParticleSystemShapeType.Cone;
        mShape.angle = 55f;
        mShape.radius = 0.95f;

        var mCol = mPs.colorOverLifetime;
        mCol.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.88f, 0.99f, 1f), 0f), new GradientColorKey(new Color(0.36f, 0.86f, 1f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.46f, 0f), new GradientAlphaKey(0.34f, 0.45f), new GradientAlphaKey(0f, 1f) }
        );
        mCol.color = g;

        ParticleSystemRenderer mPsr = mistGo.GetComponent<ParticleSystemRenderer>();
        if (mPsr != null)
        {
            Material mat = new Material(GetSharedAlphaSlashMaterial());
            Texture2D dot = GetSharedSparkTexture();
            mat.mainTexture = dot;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", dot);
            mPsr.sharedMaterial = mat;
            mPsr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mPsr.receiveShadows = false;
            Destroy(mat, 0.90f);
        }
        mPs.Play();
        Destroy(mistGo, 0.85f);

        // 2. Bông tuyết / tinh thể băng 6 cánh bắn tỏa lấp lánh
        GameObject snowGo = new GameObject("FrostSlashSnowflakes");
        snowGo.transform.position = origin;
        snowGo.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        ParticleSystem sPs = snowGo.AddComponent<ParticleSystem>();
        sPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var sMain = sPs.main;
        sMain.loop = false;
        sMain.playOnAwake = false;
        sMain.duration = 0.25f;
        sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, isPower ? 0.72f : 0.55f);
        sMain.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, isPower ? 7.5f : 5.2f);
        sMain.startSize = new ParticleSystem.MinMaxCurve(0.22f, isPower ? 0.46f : 0.34f);
        sMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        sMain.gravityModifier = 0.35f;
        sMain.simulationSpace = ParticleSystemSimulationSpace.World;
        sMain.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.95f, 1.00f, 1.00f, 1f),
            new Color(0.36f, 0.88f, 1.00f, 0.92f)
        );

        var sEmit = sPs.emission;
        sEmit.rateOverTime = 0f;
        sEmit.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)(isPower ? 30 : 18)) });

        var sShape = sPs.shape;
        sShape.enabled = true;
        sShape.shapeType = ParticleSystemShapeType.Cone;
        sShape.angle = 58f;
        sShape.radius = 0.85f;

        var sRot = sPs.rotationOverLifetime;
        sRot.enabled = true;
        sRot.z = new ParticleSystem.MinMaxCurve(-3.5f, 3.5f);

        ParticleSystemRenderer sPsr = snowGo.GetComponent<ParticleSystemRenderer>();
        if (sPsr != null)
        {
            Material mat = new Material(GetSharedAdditiveMaterial());
            Texture2D iceTex = GetIceCrystalTexture();
            mat.mainTexture = iceTex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", iceTex);
            sPsr.sharedMaterial = mat;
            sPsr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sPsr.receiveShadows = false;
            Destroy(mat, 0.90f);
        }
        sPs.Play();
        Destroy(snowGo, 0.85f);
    }

    // ================= LƯỠI HÁI HƯ KHÔNG: LỬA ĐEN-TÍM BÙNG LÊN THEO NHÁT CHÉM =================

    void SpawnVoidDarkFlameBurst(bool isPower)
    {
        Vector3 forward = GetSlashAimDirection();
        Vector3 origin = transform.position + Vector3.up * slashHeight + forward * 0.78f;

        // Lớp 1: Đám mây LỬA ĐEN-TÍM dùng Alpha-Blend để màu đen tuyền hiển thị rõ rệt
        GameObject darkGo = new GameObject("VoidSlashBlackFlame");
        darkGo.transform.position = origin;
        darkGo.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        ParticleSystem dPs = darkGo.AddComponent<ParticleSystem>();
        dPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var dMain = dPs.main;
        dMain.loop = false;
        dMain.playOnAwake = false;
        dMain.duration = 0.25f;
        dMain.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, isPower ? 0.68f : 0.50f);
        dMain.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, isPower ? 6.8f : 4.6f);
        dMain.startSize = new ParticleSystem.MinMaxCurve(0.45f, isPower ? 1.05f : 0.76f);
        dMain.gravityModifier = -0.62f;
        dMain.simulationSpace = ParticleSystemSimulationSpace.World;
        dMain.startColor = Color.white;

        var dEmit = dPs.emission;
        dEmit.rateOverTime = 0f;
        dEmit.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)(isPower ? 40 : 24)) });

        var dShape = dPs.shape;
        dShape.enabled = true;
        dShape.shapeType = ParticleSystemShapeType.Cone;
        dShape.angle = 58f;
        dShape.radius = isPower ? 1.15f : 0.85f;

        var dCol = dPs.colorOverLifetime;
        dCol.enabled = true;
        Gradient darkGrad = new Gradient();
        darkGrad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.03f, 0.01f, 0.07f), 0.00f),
                new GradientColorKey(new Color(0.28f, 0.04f, 0.48f), 0.42f),
                new GradientColorKey(new Color(0.11f, 0.01f, 0.20f), 0.78f),
                new GradientColorKey(new Color(0.03f, 0.00f, 0.06f), 1.00f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.96f, 0.00f),
                new GradientAlphaKey(0.86f, 0.45f),
                new GradientAlphaKey(0.45f, 0.76f),
                new GradientAlphaKey(0.00f, 1.00f)
            }
        );
        dCol.color = darkGrad;

        var dSize = dPs.sizeOverLifetime;
        dSize.enabled = true;
        dSize.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 0.60f), new Keyframe(0.32f, 1.12f), new Keyframe(1f, 0.16f))
        );

        ParticleSystemRenderer dPsr = darkGo.GetComponent<ParticleSystemRenderer>();
        if (dPsr != null)
        {
            Material mat = new Material(GetSharedAlphaSlashMaterial());
            mat.renderQueue = 3045;
            Texture2D dot = GetSharedSparkTexture();
            mat.mainTexture = dot;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", dot);
            dPsr.sharedMaterial = mat;
            dPsr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dPsr.receiveShadows = false;
            Destroy(mat, 0.90f);
        }

        dPs.Play();
        Destroy(darkGo, 0.85f);
    }

    // ================= THƯƠNG LÔI (BẢN CŨ): TIA SÉT ZIG-ZAG PHÓNG DÀI THẲNG VỀ PHÍA BÓNG =================

    void SpawnLightningBolts(WeaponDefinition def, int boltCount, float length)
    {
        Vector3 forward = GetSlashAimDirection();
        Vector3 origin = transform.position + Vector3.up * Mathf.Max(slashHeight, 1.25f) + forward * 0.65f;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        for (int b = 0; b < boltCount; b++)
        {
            GameObject boltGo = new GameObject("ThunderSlashBolt");
            LineRenderer lr = boltGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            int pts = 8;
            lr.positionCount = pts;
            lr.startWidth = 0.14f;
            lr.endWidth = 0.03f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Material mat = new Material(GetSharedAdditiveMaterial());
            SetMaterialColor(mat, def.coreColor);
            lr.sharedMaterial = mat;

            Vector3 dir = Quaternion.Euler(Random.Range(-22f, 22f), Random.Range(-38f, 38f), 0f) * forward;

            for (int i = 0; i < pts; i++)
            {
                float f = (float)i / (pts - 1);
                Vector3 p = origin + dir * (length * f);
                if (i > 0 && i < pts - 1)
                {
                    p += Vector3.up * Random.Range(-0.42f, 0.42f) + right * Random.Range(-0.42f, 0.42f);
                }
                lr.SetPosition(i, p);
            }

            Destroy(mat, 0.24f);
            Destroy(boltGo, 0.22f);
        }
    }

    // ================= VẾT CHÉM VÒNG CUNG CHÉO =================

    void SpawnElementCrescent(
        WeaponDefinition def,
        bool isPower,
        float rollTiltDeg,
        float pitchTiltDeg,
        float sweepSign,
        float radius,
        float bladeWidth,
        float arcDegrees,
        float duration
    )
    {
        Color outerCol = def.primaryColor;
        outerCol.a = isPower ? 0.96f : 0.78f;

        SpawnCrescentWave(
            isPower ? "PowerSlashArc" : "NormalSlashArc",
            rollTiltDeg,
            pitchTiltDeg,
            sweepSign,
            radius,
            bladeWidth,
            arcDegrees,
            duration,
            outerCol,
            false
        );

        Color coreCol = def.coreColor;
        coreCol.a = isPower ? 1.00f : 0.86f;

        SpawnCrescentWave(
            isPower ? "PowerSlashCore" : "NormalSlashCore",
            rollTiltDeg,
            pitchTiltDeg,
            sweepSign,
            radius * 0.99f,
            bladeWidth * (isPower ? 0.65f : 0.48f),
            arcDegrees * 0.95f,
            duration * 0.90f,
            coreCol,
            true
        );

        if (isPower)
        {
            Color auraCol = def.secondaryColor;
            auraCol.a = 0.62f;
            SpawnCrescentWave(
                "PowerSlashAura",
                rollTiltDeg,
                pitchTiltDeg,
                sweepSign,
                radius * 1.08f,
                bladeWidth * 1.18f,
                arcDegrees,
                duration * 1.05f,
                auraCol,
                true
            );
        }
    }

    public Vector3 GetSlashAimDirection()
    {
        Transform bestBall = null;
        float bestDist = 26f;

        BallController realBall = BallController.instance;
        if (
            realBall != null &&
            realBall.gameObject.activeInHierarchy &&
            realBall.Object != null &&
            realBall.Object.IsValid &&
            realBall.ballActive
        )
        {
            float d = Vector3.Distance(transform.position, realBall.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                bestBall = realBall.transform;
            }
        }

        Transform fakeBall = FakeBallVisual.FindClosestFakeBall(transform.position, bestDist);
        if (fakeBall != null)
        {
            bestBall = fakeBall;
        }

        if (bestBall != null)
        {
            Vector3 toBall = bestBall.position - transform.position;
            toBall.y = 0f;
            if (toBall.sqrMagnitude > 0.001f)
            {
                return toBall.normalized;
            }
        }

        Vector3 fallback = transform.forward;
        fallback.y = 0f;
        return fallback.sqrMagnitude > 0.001f ? fallback.normalized : Vector3.forward;
    }

    void SpawnCrescentWave(
        string objectName,
        float rollTiltDeg,
        float pitchTiltDeg,
        float sweepSign,
        float radius,
        float bladeWidth,
        float arcDegrees,
        float duration,
        Color color,
        bool additive
    )
    {
        GameObject slashGo = new GameObject(objectName);
        Vector3 forward = GetSlashAimDirection();

        Vector3 origin =
            transform.position +
            Vector3.up * slashHeight +
            forward * slashForwardOffset;

        slashGo.transform.position = origin;

        Quaternion baseRot =
            Quaternion.LookRotation(forward, Vector3.up) *
            Quaternion.Euler(pitchTiltDeg, -sweepSign * 40f, rollTiltDeg);

        slashGo.transform.rotation = baseRot;

        MeshFilter mf = slashGo.AddComponent<MeshFilter>();
        MeshRenderer mr = slashGo.AddComponent<MeshRenderer>();

        mf.sharedMesh = BuildCrescentMesh(radius, bladeWidth, arcDegrees, 32);

        Material mat = new Material(additive ? GetSharedAdditiveMaterial() : GetSharedAlphaSlashMaterial());
        mat.mainTexture = GetSharedSlashTexture();
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", GetSharedSlashTexture());
        }

        SetMaterialColor(mat, color);

        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        StartCoroutine(AnimateSlashArc(slashGo, mat, color, forward, sweepSign, duration));
    }

    IEnumerator AnimateSlashArc(
        GameObject slashGo,
        Material mat,
        Color startColor,
        Vector3 aimDir,
        float sweepSign,
        float duration
    )
    {
        float elapsed = 0f;
        Quaternion startRot = slashGo.transform.rotation;
        Vector3 startPos = slashGo.transform.position;
        Vector3 fwd = aimDir.sqrMagnitude > 0.001f ? aimDir.normalized : Vector3.forward;

        while (elapsed < duration && slashGo != null)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float easeOut = 1f - Mathf.Pow(1f - t, 3f);

            float yawSpin = sweepSign * Mathf.Lerp(0f, 102f, easeOut);
            slashGo.transform.rotation = startRot * Quaternion.Euler(0f, yawSpin, 0f);

            float scaleXZ = Mathf.Lerp(0.82f, 1.18f, easeOut);
            float scaleY = Mathf.Lerp(1.04f, 0.80f, t);
            slashGo.transform.localScale = new Vector3(scaleXZ, scaleY, scaleXZ);

            // Giữ gọn gàng gần player (0.32m thay vì 0.85m bay quá xa)
            slashGo.transform.position = startPos + fwd * (0.32f * easeOut);

            float alpha = t < 0.14f
                ? Mathf.Lerp(0.65f, 1.0f, t / 0.14f)
                : Mathf.Lerp(1.0f, 0.0f, (t - 0.14f) / 0.86f);

            Color c = startColor;
            c.a = startColor.a * alpha;
            SetMaterialColor(mat, c);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (mat != null) Destroy(mat);
        if (slashGo != null) Destroy(slashGo);
    }

    void SpawnElementSparks(WeaponDefinition def, bool isPower)
    {
        GameObject sparkGo = new GameObject("ParrySlashSparks");
        Vector3 forward = GetSlashAimDirection();

        sparkGo.transform.position =
            transform.position +
            Vector3.up * slashHeight +
            forward * 0.80f;

        sparkGo.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        ParticleSystem ps = sparkGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.30f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, isPower ? 0.42f : 0.28f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4.0f, isPower ? 12.5f : 8.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, isPower ? 0.26f : 0.17f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(def.coreColor, def.primaryColor);

        switch (def.type)
        {
            case WeaponType.FlameSaber:
                main.gravityModifier = -0.65f;
                break;
            case WeaponType.FrostDualBlades:
                main.gravityModifier = 0.48f;
                break;
            case WeaponType.VoidScythe:
                main.gravityModifier = -0.20f;
                break;
            default:
                main.gravityModifier = 0f;
                break;
        }

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)(isPower ? 42 : 20))
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = isPower ? 58f : 44f;
        shape.radius = 0.45f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f))
        );

        ParticleSystemRenderer psr = sparkGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.renderMode = def.type == WeaponType.FrostDualBlades
                ? ParticleSystemRenderMode.Billboard
                : ParticleSystemRenderMode.Stretch;
            psr.lengthScale = 2.4f;
            psr.velocityScale = 0.10f;
            Material sparkMat = new Material(GetSharedAdditiveMaterial());
            sparkMat.mainTexture = GetSharedSparkTexture();
            if (sparkMat.HasProperty("_BaseMap"))
            {
                sparkMat.SetTexture("_BaseMap", GetSharedSparkTexture());
            }
            psr.sharedMaterial = sparkMat;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
            Destroy(sparkMat, 0.65f);
        }

        ps.Play();
        Destroy(sparkGo, 0.60f);
    }

    static Mesh BuildCrescentMesh(
        float outerRadius,
        float maxWidth,
        float arcDegrees,
        int segments
    )
    {
        Mesh mesh = new Mesh();
        mesh.name = "ParryCrescentMesh";

        int vertCount = (segments + 1) * 2;
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        Color[] colors = new Color[vertCount];
        int[] triangles = new int[segments * 6];

        float halfArc = arcDegrees * 0.5f;

        for (int i = 0; i <= segments; i++)
        {
            float u = (float)i / segments;
            float angleDeg = Mathf.Lerp(-halfArc, halfArc, u);
            float rad = angleDeg * Mathf.Deg2Rad;

            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);
            Vector3 radialDir = new Vector3(sin, 0f, cos);

            float taper = Mathf.Sin(u * Mathf.PI);
            float bladeWidth = maxWidth * Mathf.Pow(taper, 0.68f);
            float innerRadius = Mathf.Max(0.20f, outerRadius - bladeWidth);

            float innerLift = -0.16f * taper;
            float outerLift = 0.26f * taper;

            int idx = i * 2;
            vertices[idx] = radialDir * innerRadius + Vector3.up * innerLift;
            vertices[idx + 1] = radialDir * outerRadius + Vector3.up * outerLift;

            uvs[idx] = new Vector2(u, 0f);
            uvs[idx + 1] = new Vector2(u, 1f);

            float tipAlpha = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 0.68f);
            colors[idx] = new Color(1f, 1f, 1f, 0.20f * tipAlpha);
            colors[idx + 1] = new Color(1f, 1f, 1f, 1.00f * tipAlpha);

            if (i < segments)
            {
                int tIdx = i * 6;
                triangles[tIdx] = idx;
                triangles[tIdx + 1] = idx + 1;
                triangles[tIdx + 2] = idx + 2;

                triangles[tIdx + 3] = idx + 2;
                triangles[tIdx + 4] = idx + 1;
                triangles[tIdx + 5] = idx + 3;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Material GetSharedAlphaSlashMaterial()
    {
        if (sharedAlphaMat != null)
            return sharedAlphaMat;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

        sharedAlphaMat = new Material(shader);
        sharedAlphaMat.name = "ParrySlashAlphaMat";

        if (sharedAlphaMat.HasProperty("_Surface")) sharedAlphaMat.SetFloat("_Surface", 1f);
        if (sharedAlphaMat.HasProperty("_Blend")) sharedAlphaMat.SetFloat("_Blend", 0f);
        if (sharedAlphaMat.HasProperty("_SrcBlend")) sharedAlphaMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (sharedAlphaMat.HasProperty("_DstBlend")) sharedAlphaMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (sharedAlphaMat.HasProperty("_ZWrite")) sharedAlphaMat.SetFloat("_ZWrite", 0f);
        if (sharedAlphaMat.HasProperty("_Cull")) sharedAlphaMat.SetFloat("_Cull", 0f);

        sharedAlphaMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        sharedAlphaMat.renderQueue = 3050;

        SetMaterialColor(sharedAlphaMat, Color.white);
        return sharedAlphaMat;
    }

    static Material GetSharedAdditiveMaterial()
    {
        if (sharedAdditiveMat != null)
            return sharedAdditiveMat;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        sharedAdditiveMat = new Material(shader);
        sharedAdditiveMat.name = "ParrySlashAdditiveMat";

        if (sharedAdditiveMat.HasProperty("_Surface")) sharedAdditiveMat.SetFloat("_Surface", 1f);
        if (sharedAdditiveMat.HasProperty("_Blend")) sharedAdditiveMat.SetFloat("_Blend", 2f);
        if (sharedAdditiveMat.HasProperty("_SrcBlend")) sharedAdditiveMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (sharedAdditiveMat.HasProperty("_DstBlend")) sharedAdditiveMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (sharedAdditiveMat.HasProperty("_ZWrite")) sharedAdditiveMat.SetFloat("_ZWrite", 0f);
        if (sharedAdditiveMat.HasProperty("_Cull")) sharedAdditiveMat.SetFloat("_Cull", 0f);

        sharedAdditiveMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        sharedAdditiveMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        sharedAdditiveMat.renderQueue = 3060;

        SetMaterialColor(sharedAdditiveMat, Color.white);
        return sharedAdditiveMat;
    }

    static Texture2D GetSharedSlashTexture()
    {
        if (sharedSlashTex != null)
            return sharedSlashTex;

        const int w = 128;
        const int h = 64;
        sharedSlashTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        sharedSlashTex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);
            float body = Mathf.Pow(v, 1.2f);
            float hotEdge = Mathf.Clamp01(1f - Mathf.Abs(v - 0.86f) * 5.2f);
            float outerSoft = v > 0.95f ? Mathf.Lerp(1f, 0.4f, (v - 0.95f) / 0.05f) : 1f;

            for (int x = 0; x < w; x++)
            {
                float u = (float)x / (w - 1);
                float along = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 0.65f);
                float alpha = Mathf.Clamp01((body * 0.90f + hotEdge * 0.72f) * along * outerSoft);
                float brightness = Mathf.Lerp(0.88f, 1.0f, hotEdge);
                sharedSlashTex.SetPixel(x, y, new Color(brightness, brightness, brightness, alpha));
            }
        }

        sharedSlashTex.Apply();
        return sharedSlashTex;
    }

    static Texture2D GetSharedSparkTexture()
    {
        if (sharedSparkTex != null)
            return sharedSparkTex;

        const int size = 64;
        sharedSparkTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        sharedSparkTex.wrapMode = TextureWrapMode.Clamp;

        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = a * a * a;
                sharedSparkTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        sharedSparkTex.Apply();
        return sharedSparkTex;
    }

    static void SetMaterialColor(Material mat, Color col)
    {
        if (mat == null) return;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
    }
}
