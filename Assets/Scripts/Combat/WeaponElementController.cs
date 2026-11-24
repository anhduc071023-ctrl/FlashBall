using System.Collections.Generic;
using UnityEngine;

public class WeaponElementController : MonoBehaviour
{
    public WeaponType CurrentWeapon { get; private set; } = WeaponType.WindBlade;
    public int WeaponVersion { get; private set; }

    struct ParticleEntry
    {
        public ParticleSystem ps;
        public float idleRate;
        public float powerRate;
    }

    Transform rightHand;
    Transform leftHand;
    GameObject defaultSword;
    Transform defaultSwordTransform;

    GameObject customRightRoot;
    GameObject customLeftRoot;

    readonly List<ParticleEntry> ambientParticles = new List<ParticleEntry>();
    readonly List<Renderer> auraShellRenderers = new List<Renderer>();
    readonly List<Material> runtimeMaterials = new List<Material>();
    readonly List<Material> weaponBodyMaterials = new List<Material>();

    float powerAuraTimer;
    float chargeAuraLevel;
    bool initialized;
    bool isStealthHidden;

    void Awake()
    {
        FindHandSockets();
    }

    void OnDestroy()
    {
        CleanupMaterials();
    }

    void Update()
    {
        if (powerAuraTimer > 0f)
        {
            powerAuraTimer = Mathf.Max(0f, powerAuraTimer - Time.deltaTime);
        }

        float auraIntensity = Mathf.Max(chargeAuraLevel, Mathf.Clamp01(powerAuraTimer / 0.90f));
        UpdateAuraVisuals(auraIntensity);
    }

    void FindHandSockets()
    {
        if (rightHand != null && leftHand != null)
            return;

        Transform[] all = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t == null) continue;
            if (t.name == "Right_Hand") rightHand = t;
            else if (t.name == "Left_Hand") leftHand = t;
            else if (t.name == "SM_Wep_Sword_03")
            {
                defaultSword = t.gameObject;
                defaultSwordTransform = t;
            }
        }
    }

    public void ApplyWeapon(WeaponType type)
    {
        if (initialized && CurrentWeapon == type && customRightRoot != null)
            return;

        initialized = true;
        CurrentWeapon = type;
        WeaponVersion++;

        FindHandSockets();
        ClearCustomWeapons();

        if (rightHand == null)
            return;

        WeaponDefinition def = WeaponCatalog.Get(type);

        // Nạp model 3D thật tải từ Poly Pizza / Quaternius trong Assets/Resources/Weapons/
        GameObject weaponPrefab = !string.IsNullOrEmpty(def.modelResource)
            ? Resources.Load<GameObject>("Weapons/" + def.modelResource)
            : null;

        // Ẩn mesh kiếm mặc định cũ (chỉ giữ lại Vetchem TrailRenderer bên dưới nó!)
        SetDefaultSwordMeshVisible(weaponPrefab == null && type == WeaponType.WindBlade);

        Transform rRoot = CreateSocketRoot(rightHand, "EquippedWeapon_Right_" + type);
        customRightRoot = rRoot.gameObject;

        if (weaponPrefab != null)
        {
            SpawnExternalWeaponModel(rRoot, weaponPrefab, def, false);
        }
        else
        {
            BuildFallbackProceduralWeapon(rRoot, def);
        }

        // Song Kiếm Băng cầm CẢ 2 TAY (tay trái thêm 1 thanh song kiếm + hiệu ứng băng riêng!)
        if (type == WeaponType.FrostDualBlades && leftHand != null)
        {
            Transform lRoot = CreateSocketRoot(leftHand, "EquippedWeapon_Left_" + type);
            lRoot.localPosition = new Vector3(0f, 0.04f, 0.02f);
            lRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);
            customLeftRoot = lRoot.gameObject;

            if (weaponPrefab != null)
            {
                SpawnExternalWeaponModel(lRoot, weaponPrefab, def, true);
            }
            else
            {
                BuildFallbackProceduralWeapon(lRoot, def);
            }
        }

        UpdateAuraVisuals(0f);
    }

    void SpawnExternalWeaponModel(Transform socket, GameObject prefab, WeaponDefinition def, bool isLeftHand)
    {
        GameObject inst = Instantiate(prefab, socket, false);
        inst.name = def.modelResource + (isLeftHand ? "_L" : "_R");
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localScale = Vector3.one;

        // Căn góc quay lưỡi vũ khí hướng về phía trước theo tay cầm của SM_Wep_Sword_03
        switch (def.type)
        {
            case WeaponType.FlameSaber:
                inst.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                break;
            case WeaponType.VoidScythe:
                inst.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                break;
            default:
                inst.transform.localRotation = Quaternion.identity;
                break;
        }

        // Gỡ collider thừa nếu có & chuyển vật liệu sang URP Lit có ánh phát quang nguyên tố tinh tế
        Collider[] cols = inst.GetComponentsInChildren<Collider>(true);
        foreach (Collider c in cols) Destroy(c);

        MeshRenderer[] renderers = inst.GetComponentsInChildren<MeshRenderer>(true);
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        Bounds combinedBounds = new Bounds(Vector3.up * 0.7f, new Vector3(0.2f, 1.0f, 0.2f));
        bool hasBounds = false;

        foreach (MeshRenderer mr in renderers)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            MeshFilter mf = mr.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                if (!hasBounds)
                {
                    combinedBounds = mf.sharedMesh.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(mf.sharedMesh.bounds);
                }
            }

            Material[] srcMats = mr.sharedMaterials;
            Material[] newMats = new Material[srcMats.Length];
            for (int i = 0; i < srcMats.Length; i++)
            {
                Color baseCol = Color.white;
                if (srcMats[i] != null)
                {
                    if (srcMats[i].HasProperty("_BaseColor")) baseCol = srcMats[i].GetColor("_BaseColor");
                    else if (srcMats[i].HasProperty("_Color")) baseCol = srcMats[i].GetColor("_Color");
                }

                // Giữ chi tiết màu gốc của model (cán gỗ/da tối), phủ sắc nguyên tố lên phần lưỡi sáng
                float lum = baseCol.r * 0.3f + baseCol.g * 0.59f + baseCol.b * 0.11f;
                bool isBladePart = lum > 0.38f || srcMats.Length == 1;

                Material m = new Material(urpLit);
                Color tinted;
                float metallic = isBladePart ? 0.72f : 0.25f;
                float smoothness = isBladePart ? 0.88f : 0.45f;
                Color baseEmission = isBladePart ? def.primaryColor * 0.55f : def.secondaryColor * 0.10f;

                if (def.type == WeaponType.FrostDualBlades)
                {
                    // Song Kiếm Băng: tinh thể pha lê băng xanh-trắng lấp lánh cực rõ hệ Băng!
                    tinted = isBladePart
                        ? Color.Lerp(baseCol, new Color(0.62f, 0.94f, 1.00f, 1f), 0.72f)
                        : Color.Lerp(baseCol, new Color(0.14f, 0.45f, 0.78f, 1f), 0.45f);
                    metallic = isBladePart ? 0.32f : 0.40f;
                    smoothness = isBladePart ? 0.97f : 0.70f;
                    baseEmission = isBladePart
                        ? new Color(0.28f, 0.84f, 1.00f, 1f) * 0.80f
                        : new Color(0.08f, 0.35f, 0.65f, 1f) * 0.22f;
                }
                else if (def.type == WeaponType.FlameSaber)
                {
                    tinted = isBladePart
                        ? Color.Lerp(baseCol, new Color(1.00f, 0.48f, 0.08f, 1f), 0.58f)
                        : Color.Lerp(baseCol, def.secondaryColor, 0.22f);
                    baseEmission = isBladePart
                        ? new Color(1.00f, 0.34f, 0.04f, 1f) * 0.85f
                        : new Color(0.55f, 0.10f, 0.02f, 1f) * 0.14f;
                }
                else if (def.type == WeaponType.VoidScythe)
                {
                    // Lưỡi Hái Hư Không: cán hắc diện thạch đen tuyền + lưỡi năng lượng tím-đen ma mị
                    tinted = isBladePart
                        ? Color.Lerp(baseCol, new Color(0.66f, 0.16f, 1.00f, 1f), 0.68f)
                        : Color.Lerp(baseCol, new Color(0.06f, 0.02f, 0.12f, 1f), 0.55f);
                    metallic = isBladePart ? 0.60f : 0.42f;
                    smoothness = isBladePart ? 0.94f : 0.55f;
                    baseEmission = isBladePart
                        ? new Color(0.68f, 0.16f, 1.00f, 1f) * 0.92f
                        : new Color(0.14f, 0.02f, 0.24f, 1f) * 0.18f;
                }
                else
                {
                    tinted = isBladePart
                        ? Color.Lerp(baseCol, def.primaryColor, 0.45f)
                        : Color.Lerp(baseCol, def.secondaryColor, 0.18f);
                }

                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tinted);
                if (m.HasProperty("_Color")) m.SetColor("_Color", tinted);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);

                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", baseEmission);
                }

                runtimeMaterials.Add(m);
                if (isBladePart)
                {
                    weaponBodyMaterials.Add(m);
                }
                newMats[i] = m;
            }
            mr.sharedMaterials = newMats;
        }

        // Tạo vỏ hào quang ôm sát kích thước thực của vũ khí (chỉ sáng bùng lên khi tụ lực / Power Parry!)
        Vector3 auraCenter = def.type == WeaponType.VoidScythe
            ? new Vector3(0.46f, 1.08f, 0f)
            : new Vector3(0f, Mathf.Max(0.55f, combinedBounds.center.y + combinedBounds.extents.y * 0.25f), 0f);
        float auraHalfHeight = Mathf.Clamp(combinedBounds.extents.y * 0.80f, 0.40f, 0.95f);
        float auraThickness = 0.18f;

        if (def.type == WeaponType.VoidScythe)
        {
            // Dùng CHÍNH MESH 3D của Lưỡi Hái đẩy nhẹ theo pháp tuyến để vầng sáng tụ Parry ôm KHÍT 100% đường cong lưỡi hái!
            CreateExactMeshAuraShell(inst, def.primaryColor, 0.038f);
        }
        else
        {
            CreateAuraShell(
                socket,
                PrimitiveType.Capsule,
                auraCenter,
                Vector3.zero,
                new Vector3(auraThickness, auraHalfHeight, auraThickness),
                def.primaryColor
            );
        }

        // Hiệu ứng hạt nguyên tố đặc trưng từng vũ khí
        switch (def.type)
        {
            case WeaponType.FlameSaber:
                SpawnFlameSaberVFX(socket, auraCenter);
                break;

            case WeaponType.FrostDualBlades:
                SpawnFrostBladeVFX(socket, auraCenter);
                break;

            case WeaponType.VoidScythe:
                // Lưỡi hái Sci-Fi quay +90 độ quanh Y nên lưỡi cong vươn dọc theo +X trên đỉnh cán!
                SpawnVoidDarkFlameVFX(socket, new Vector3(0.58f, 1.08f, 0f));
                break;

            default:
                // WindBlade (giữ nguyên như bản đẹp) & ThunderSpear
                Vector3 fxCenter = def.type == WeaponType.ThunderSpear
                    ? new Vector3(0f, combinedBounds.max.y * 0.82f, 0f)
                    : auraCenter;
                AddElementParticles(socket, fxCenter, def, 18f, 0.45f, 0.14f, 0f);
                break;
        }
    }

    void BuildFallbackProceduralWeapon(Transform root, WeaponDefinition def)
    {
        Material bladeMat = CreateAuraAdditiveMat(def.primaryColor);
        CreatePart(root, PrimitiveType.Cube, "FallbackBlade", new Vector3(0f, 0.65f, 0f), Vector3.zero, new Vector3(0.06f, 1.1f, 0.12f), bladeMat);
        CreateAuraShell(root, PrimitiveType.Capsule, new Vector3(0f, 0.68f, 0f), Vector3.zero, new Vector3(0.18f, 0.62f, 0.18f), def.primaryColor);
        AddElementParticles(root, new Vector3(0f, 0.72f, 0f), def, 18f, 0.42f, 0.13f, 0f);
    }

    public void SetChargeAura(float normalizedCharge)
    {
        chargeAuraLevel = Mathf.Clamp01(normalizedCharge);
    }

    public void TriggerPowerAura(float duration = 1.20f)
    {
        powerAuraTimer = Mathf.Max(powerAuraTimer, duration);
    }

    public void SetStealthHidden(bool hidden)
    {
        if (isStealthHidden == hidden)
            return;

        isStealthHidden = hidden;

        for (int i = 0; i < ambientParticles.Count; i++)
        {
            ParticleSystem ps = ambientParticles[i].ps;
            if (ps == null) continue;
            if (hidden)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            else if (!ps.isPlaying)
            {
                ps.Play();
            }
        }

        float auraIntensity = Mathf.Max(chargeAuraLevel, Mathf.Clamp01(powerAuraTimer / 0.90f));
        UpdateAuraVisuals(auraIntensity);
    }

    void SetDefaultSwordMeshVisible(bool visible)
    {
        if (defaultSword == null) return;
        MeshRenderer mr = defaultSword.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = visible;
    }

    void ClearCustomWeapons()
    {
        if (customRightRoot != null) { Destroy(customRightRoot); customRightRoot = null; }
        if (customLeftRoot != null) { Destroy(customLeftRoot); customLeftRoot = null; }
        ambientParticles.Clear();
        auraShellRenderers.Clear();
        weaponBodyMaterials.Clear();
        CleanupMaterials();
    }

    void CleanupMaterials()
    {
        for (int i = 0; i < runtimeMaterials.Count; i++)
        {
            if (runtimeMaterials[i] != null) Destroy(runtimeMaterials[i]);
        }
        runtimeMaterials.Clear();
    }

    Transform CreateSocketRoot(Transform hand, string name)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(hand, false);
        if (defaultSwordTransform != null && hand == rightHand)
        {
            root.transform.localPosition = defaultSwordTransform.localPosition;
            root.transform.localRotation = defaultSwordTransform.localRotation;
            root.transform.localScale = Vector3.one;
        }
        else
        {
            root.transform.localPosition = new Vector3(0f, 0.05f, 0.02f);
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
        }
        return root.transform;
    }

    GameObject CreatePart(Transform parent, PrimitiveType primitive, string partName, Vector3 localPos, Vector3 localEuler, Vector3 localScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(primitive);
        go.name = partName;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localEulerAngles = localEuler;
        go.transform.localScale = localScale;

        Collider col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        return go;
    }

    void CreateAuraShell(Transform parent, PrimitiveType primitive, Vector3 localPos, Vector3 localEuler, Vector3 localScale, Color auraColor)
    {
        Material auraMat = CreateAuraAdditiveMat(auraColor);
        GameObject go = CreatePart(parent, primitive, "PowerAuraShell", localPos, localEuler, localScale, auraMat);
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.enabled = false;
            auraShellRenderers.Add(r);
        }
    }

    void CreateExactMeshAuraShell(GameObject weaponInstance, Color col, float inflateAmount)
    {
        if (weaponInstance == null) return;
        Material auraMat = CreateAuraAdditiveMat(col);
        if (auraMat.HasProperty("_Cull"))
            auraMat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);

        MeshFilter[] filters = weaponInstance.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter mf in filters)
        {
            if (mf == null || mf.sharedMesh == null) continue;

            Mesh src = mf.sharedMesh;
            GameObject shellGo = new GameObject("PowerAuraShell");
            shellGo.transform.SetParent(mf.transform, false);
            shellGo.transform.localPosition = Vector3.zero;
            shellGo.transform.localRotation = Quaternion.identity;
            shellGo.transform.localScale = Vector3.one;

            MeshFilter shellMf = shellGo.AddComponent<MeshFilter>();
            if (src.isReadable)
            {
                Mesh inflated = Instantiate(src);
                inflated.name = src.name + "_AuraInflated";
                Vector3[] verts = inflated.vertices;
                Vector3[] norms = inflated.normals;
                if (norms == null || norms.Length != verts.Length)
                {
                    inflated.RecalculateNormals();
                    norms = inflated.normals;
                }
                for (int i = 0; i < verts.Length; i++)
                {
                    verts[i] += norms[i] * inflateAmount;
                }
                inflated.vertices = verts;
                inflated.RecalculateBounds();
                shellMf.sharedMesh = inflated;
            }
            else
            {
                // Scale quanh đúng tâm bounds.center để vầng sáng ôm khít lưỡi hái không bị lệch!
                shellMf.sharedMesh = src;
                Vector3 c = src.bounds.center;
                Vector3 sz = src.bounds.size;
                Vector3 s = new Vector3(
                    1f + (inflateAmount * 2f) / Mathf.Max(0.08f, sz.x),
                    1f + (inflateAmount * 2f) / Mathf.Max(0.25f, sz.y),
                    1f + (inflateAmount * 2f) / Mathf.Max(0.25f, sz.z)
                );
                s.x = Mathf.Clamp(s.x, 1.04f, 1.85f);
                s.y = Mathf.Clamp(s.y, 1.02f, 1.09f);
                s.z = Mathf.Clamp(s.z, 1.02f, 1.09f);
                shellGo.transform.localScale = s;
                shellGo.transform.localPosition = c - Vector3.Scale(c, s);
            }

            MeshRenderer mr = shellGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = auraMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.enabled = false;
            auraShellRenderers.Add(mr);
        }
    }

    void AddElementParticles(Transform parent, Vector3 localPos, WeaponDefinition def, float baseRate, float radius, float size, float gravityMod)
    {
        GameObject psGo = new GameObject("ElementAmbientVFX");
        psGo.transform.SetParent(parent, false);
        psGo.transform.localPosition = localPos;

        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.58f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.30f, 1.30f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.65f, size * 1.35f);
        main.gravityModifier = gravityMod;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(def.coreColor, def.primaryColor);

        var emission = ps.emission;
        emission.rateOverTime = baseRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 0.85f), new Keyframe(0.4f, 1f), new Keyframe(1f, 0f))
        );

        ParticleSystemRenderer psr = psGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            Material m = CreateAuraAdditiveMat(Color.white);
            Texture2D softDot = ParrySlashVFX.GetSoftGlowTexture();
            if (softDot != null)
            {
                m.mainTexture = softDot;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", softDot);
            }
            psr.sharedMaterial = m;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
        }

        ps.Play();
        ambientParticles.Add(new ParticleEntry
        {
            ps = ps,
            idleRate = baseRate,
            powerRate = baseRate * 4.0f
        });
    }

    // ================= HỎA ĐAO: LỬA CHÁY RỰC NHƯ QUẢ CẦU LỬA (BALL) =================

    void SpawnFlameSaberVFX(Transform parent, Vector3 localPos)
    {
        // Lớp 1: Thân lửa cháy bùng vàng -> cam -> đỏ rực dọc theo lưỡi đao giống hệt Fire Ball
        GameObject fireGo = new GameObject("FlameSaberFireBodyVFX");
        fireGo.transform.SetParent(parent, false);
        fireGo.transform.localPosition = localPos;

        ParticleSystem firePs = fireGo.AddComponent<ParticleSystem>();
        firePs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = firePs.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.52f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.26f, 0.48f);
        main.gravityModifier = -0.82f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(1.00f, 0.88f, 0.18f, 1.00f);

        var emission = firePs.emission;
        emission.rateOverTime = 56f;

        var shape = firePs.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.14f, 0.92f, 0.14f);

        var colLife = firePs.colorOverLifetime;
        colLife.enabled = true;
        Gradient fireGrad = new Gradient();
        fireGrad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1.00f, 0.98f, 0.20f), 0.00f),
                new GradientColorKey(new Color(1.00f, 0.50f, 0.03f), 0.38f),
                new GradientColorKey(new Color(1.00f, 0.16f, 0.00f), 0.72f),
                new GradientColorKey(new Color(0.75f, 0.04f, 0.00f), 1.00f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.95f, 0.00f),
                new GradientAlphaKey(0.88f, 0.48f),
                new GradientAlphaKey(0.45f, 0.78f),
                new GradientAlphaKey(0.00f, 1.00f)
            }
        );
        colLife.color = fireGrad;

        var sizeOver = firePs.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 0.65f), new Keyframe(0.32f, 1.05f), new Keyframe(1f, 0.12f))
        );

        ParticleSystemRenderer psr = fireGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            Material m = CreateAuraAdditiveMat(Color.white);
            Texture2D softDot = ParrySlashVFX.GetSoftGlowTexture();
            if (softDot != null)
            {
                m.mainTexture = softDot;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", softDot);
            }
            psr.sharedMaterial = m;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
        }
        firePs.Play();
        ambientParticles.Add(new ParticleEntry { ps = firePs, idleRate = 56f, powerRate = 145f });

        // Lớp 2: Tàn lửa nhỏ nóng rực bay lên khỏi lưỡi đao
        GameObject emberGo = new GameObject("FlameSaberEmbersVFX");
        emberGo.transform.SetParent(parent, false);
        emberGo.transform.localPosition = localPos;

        ParticleSystem emberPs = emberGo.AddComponent<ParticleSystem>();
        emberPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var eMain = emberPs.main;
        eMain.loop = true;
        eMain.playOnAwake = true;
        eMain.startLifetime = new ParticleSystem.MinMaxCurve(0.40f, 0.80f);
        eMain.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
        eMain.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.095f);
        eMain.gravityModifier = -1.05f;
        eMain.simulationSpace = ParticleSystemSimulationSpace.World;
        eMain.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1.00f, 0.96f, 0.45f, 1f),
            new Color(1.00f, 0.36f, 0.02f, 1f)
        );

        var eEmit = emberPs.emission;
        eEmit.rateOverTime = 24f;

        var eShape = emberPs.shape;
        eShape.enabled = true;
        eShape.shapeType = ParticleSystemShapeType.Box;
        eShape.scale = new Vector3(0.20f, 0.95f, 0.20f);

        ParticleSystemRenderer epsr = emberGo.GetComponent<ParticleSystemRenderer>();
        if (epsr != null)
        {
            Material m = CreateAuraAdditiveMat(Color.white);
            Texture2D softDot = ParrySlashVFX.GetSoftGlowTexture();
            if (softDot != null)
            {
                m.mainTexture = softDot;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", softDot);
            }
            epsr.sharedMaterial = m;
            epsr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            epsr.receiveShadows = false;
        }
        emberPs.Play();
        ambientParticles.Add(new ParticleEntry { ps = emberPs, idleRate = 24f, powerRate = 75f });
    }

    // ================= SONG KIẾM BĂNG: SƯƠNG LẠNH + TINH THỂ TUYẾT LẤP LÁNH =================

    void SpawnFrostBladeVFX(Transform parent, Vector3 localPos)
    {
        // Lớp 1: Sương khí băng lạnh trắng-xanh tỏa mờ quanh cả 2 thanh kiếm
        GameObject mistGo = new GameObject("FrostBladeMistVFX");
        mistGo.transform.SetParent(parent, false);
        mistGo.transform.localPosition = localPos;

        ParticleSystem mistPs = mistGo.AddComponent<ParticleSystem>();
        mistPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var mMain = mistPs.main;
        mMain.loop = true;
        mMain.playOnAwake = true;
        mMain.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        mMain.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
        mMain.startSize = new ParticleSystem.MinMaxCurve(0.24f, 0.44f);
        mMain.gravityModifier = 0.16f;
        mMain.simulationSpace = ParticleSystemSimulationSpace.World;
        mMain.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.84f, 0.97f, 1.00f, 0.36f),
            new Color(0.36f, 0.84f, 1.00f, 0.28f)
        );

        var mEmit = mistPs.emission;
        mEmit.rateOverTime = 24f;

        var mShape = mistPs.shape;
        mShape.enabled = true;
        mShape.shapeType = ParticleSystemShapeType.Box;
        mShape.scale = new Vector3(0.16f, 0.82f, 0.16f);

        var mCol = mistPs.colorOverLifetime;
        mCol.enabled = true;
        Gradient mistGrad = new Gradient();
        mistGrad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.86f, 0.98f, 1.00f), 0f),
                new GradientColorKey(new Color(0.40f, 0.88f, 1.00f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.00f, 0.00f),
                new GradientAlphaKey(0.38f, 0.25f),
                new GradientAlphaKey(0.00f, 1.00f)
            }
        );
        mCol.color = mistGrad;

        ParticleSystemRenderer mpsr = mistGo.GetComponent<ParticleSystemRenderer>();
        if (mpsr != null)
        {
            Material m = CreateAlphaParticleMat(Color.white, 3046);
            Texture2D softDot = ParrySlashVFX.GetSoftGlowTexture();
            if (softDot != null)
            {
                m.mainTexture = softDot;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", softDot);
            }
            mpsr.sharedMaterial = m;
            mpsr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mpsr.receiveShadows = false;
        }
        mistPs.Play();
        ambientParticles.Add(new ParticleEntry { ps = mistPs, idleRate = 24f, powerRate = 65f });

        // Lớp 2: Bông tuyết / tinh thể băng 6 cánh sáng lấp lánh rơi nhẹ quanh lưỡi kiếm
        GameObject snowGo = new GameObject("FrostBladeCrystalVFX");
        snowGo.transform.SetParent(parent, false);
        snowGo.transform.localPosition = localPos;

        ParticleSystem snowPs = snowGo.AddComponent<ParticleSystem>();
        snowPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var sMain = snowPs.main;
        sMain.loop = true;
        sMain.playOnAwake = true;
        sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.90f);
        sMain.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.95f);
        sMain.startSize = new ParticleSystem.MinMaxCurve(0.11f, 0.22f);
        sMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        sMain.gravityModifier = 0.28f;
        sMain.simulationSpace = ParticleSystemSimulationSpace.World;
        sMain.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.94f, 0.99f, 1.00f, 0.95f),
            new Color(0.38f, 0.88f, 1.00f, 0.88f)
        );

        var sEmit = snowPs.emission;
        sEmit.rateOverTime = 22f;

        var sShape = snowPs.shape;
        sShape.enabled = true;
        sShape.shapeType = ParticleSystemShapeType.Box;
        sShape.scale = new Vector3(0.22f, 0.88f, 0.22f);

        var sRot = snowPs.rotationOverLifetime;
        sRot.enabled = true;
        sRot.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);

        var sSize = snowPs.sizeOverLifetime;
        sSize.enabled = true;
        sSize.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f))
        );

        ParticleSystemRenderer spsr = snowGo.GetComponent<ParticleSystemRenderer>();
        if (spsr != null)
        {
            Material m = CreateAuraAdditiveMat(Color.white);
            Texture2D iceTex = ParrySlashVFX.GetIceCrystalTexture();
            if (iceTex != null)
            {
                m.mainTexture = iceTex;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", iceTex);
            }
            spsr.sharedMaterial = m;
            spsr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spsr.receiveShadows = false;
        }
        snowPs.Play();
        ambientParticles.Add(new ParticleEntry { ps = snowPs, idleRate = 22f, powerRate = 68f });
    }

    // ================= LƯỠI HÁI HƯ KHÔNG: LỬA ĐEN TÍM ĐẶC TRƯNG HỆ BÓNG TỐI =================

    void SpawnVoidDarkFlameVFX(Transform parent, Vector3 localPos)
    {
        // Lớp 1 (QUAN TRỌNG): Hạt LỬA ĐEN-TÍM dùng Alpha-Blend (không Additive!) để màu đen tuyền hiển thị cực sắc nét
        GameObject darkGo = new GameObject("VoidBlackFlameVFX");
        darkGo.transform.SetParent(parent, false);
        darkGo.transform.localPosition = localPos;

        ParticleSystem darkPs = darkGo.AddComponent<ParticleSystem>();
        darkPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var dMain = darkPs.main;
        dMain.loop = true;
        dMain.playOnAwake = true;
        dMain.startLifetime = new ParticleSystem.MinMaxCurve(0.38f, 0.72f);
        dMain.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.15f);
        dMain.startSize = new ParticleSystem.MinMaxCurve(0.24f, 0.48f);
        dMain.gravityModifier = -0.58f;
        dMain.simulationSpace = ParticleSystemSimulationSpace.World;
        dMain.startColor = Color.white;

        var dEmit = darkPs.emission;
        dEmit.rateOverTime = 46f;

        var dShape = darkPs.shape;
        dShape.enabled = true;
        dShape.shapeType = ParticleSystemShapeType.Box;
        dShape.scale = new Vector3(0.95f, 0.28f, 0.16f);

        var dCol = darkPs.colorOverLifetime;
        dCol.enabled = true;
        Gradient darkGrad = new Gradient();
        darkGrad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.03f, 0.01f, 0.07f), 0.00f),
                new GradientColorKey(new Color(0.24f, 0.03f, 0.42f), 0.40f),
                new GradientColorKey(new Color(0.10f, 0.01f, 0.18f), 0.76f),
                new GradientColorKey(new Color(0.03f, 0.00f, 0.06f), 1.00f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.94f, 0.00f),
                new GradientAlphaKey(0.84f, 0.46f),
                new GradientAlphaKey(0.45f, 0.78f),
                new GradientAlphaKey(0.00f, 1.00f)
            }
        );
        dCol.color = darkGrad;

        var dSize = darkPs.sizeOverLifetime;
        dSize.enabled = true;
        dSize.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 0.65f), new Keyframe(0.35f, 1.10f), new Keyframe(1f, 0.15f))
        );

        ParticleSystemRenderer dpsr = darkGo.GetComponent<ParticleSystemRenderer>();
        if (dpsr != null)
        {
            Material m = CreateAlphaParticleMat(Color.white, 3045);
            Texture2D softDot = ParrySlashVFX.GetSoftGlowTexture();
            if (softDot != null)
            {
                m.mainTexture = softDot;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", softDot);
            }
            dpsr.sharedMaterial = m;
            dpsr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dpsr.receiveShadows = false;
        }
        darkPs.Play();
        ambientParticles.Add(new ParticleEntry { ps = darkPs, idleRate = 46f, powerRate = 120f });

        // Lớp 2: Hạt lửa tím-hồng neon rực sáng nhảy múa xen lẫn trong ngọn lửa đen
        GameObject sparkGo = new GameObject("VoidPurpleSparksVFX");
        sparkGo.transform.SetParent(parent, false);
        sparkGo.transform.localPosition = localPos;

        ParticleSystem sparkPs = sparkGo.AddComponent<ParticleSystem>();
        sparkPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var sMain = sparkPs.main;
        sMain.loop = true;
        sMain.playOnAwake = true;
        sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.68f);
        sMain.startSpeed = new ParticleSystem.MinMaxCurve(0.65f, 1.90f);
        sMain.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        sMain.gravityModifier = -0.52f;
        sMain.simulationSpace = ParticleSystemSimulationSpace.World;
        sMain.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.78f, 0.24f, 1.00f, 0.98f),
            new Color(1.00f, 0.42f, 0.94f, 0.92f)
        );

        var sEmit = sparkPs.emission;
        sEmit.rateOverTime = 28f;

        var sShape = sparkPs.shape;
        sShape.enabled = true;
        sShape.shapeType = ParticleSystemShapeType.Box;
        sShape.scale = new Vector3(1.00f, 0.32f, 0.18f);

        var sSize = sparkPs.sizeOverLifetime;
        sSize.enabled = true;
        sSize.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 0.9f), new Keyframe(0.4f, 1f), new Keyframe(1f, 0f))
        );

        ParticleSystemRenderer spsr = sparkGo.GetComponent<ParticleSystemRenderer>();
        if (spsr != null)
        {
            Material m = CreateAuraAdditiveMat(Color.white);
            Texture2D softDot = ParrySlashVFX.GetSoftGlowTexture();
            if (softDot != null)
            {
                m.mainTexture = softDot;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", softDot);
            }
            spsr.sharedMaterial = m;
            spsr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spsr.receiveShadows = false;
        }
        sparkPs.Play();
        ambientParticles.Add(new ParticleEntry { ps = sparkPs, idleRate = 28f, powerRate = 82f });
    }

    void UpdateAuraVisuals(float intensity)
    {
        bool showShell = !isStealthHidden && intensity > 0.05f;
        WeaponDefinition def = WeaponCatalog.Get(CurrentWeapon);
        float pulse = 0.86f + 0.14f * Mathf.Sin(Time.time * 22f);

        // Vỏ hào quang phát sáng rực dọc thân vũ khí khi tụ lực / Power Parry
        for (int i = 0; i < auraShellRenderers.Count; i++)
        {
            Renderer r = auraShellRenderers[i];
            if (r == null) continue;

            r.enabled = showShell;
            if (showShell && r.sharedMaterial != null)
            {
                Color c = Color.Lerp(def.primaryColor, def.coreColor, intensity * 0.55f);
                c.a = Mathf.Clamp01(intensity * 0.85f * pulse);
                if (r.sharedMaterial.HasProperty("_BaseColor")) r.sharedMaterial.SetColor("_BaseColor", c);
                if (r.sharedMaterial.HasProperty("_Color")) r.sharedMaterial.SetColor("_Color", c);
            }
        }

        // Tăng độ phát sáng Emissive trên chính lưỡi vũ khí 3D khi tụ lực / Power Parry!
        Color idleEmissive = def.type == WeaponType.VoidScythe
            ? new Color(0.68f, 0.16f, 1.00f, 1f) * 0.92f
            : (def.type == WeaponType.FrostDualBlades
                ? new Color(0.28f, 0.84f, 1.00f, 1f) * 0.80f
                : (def.type == WeaponType.FlameSaber
                    ? new Color(1.00f, 0.34f, 0.04f, 1f) * 0.85f
                    : def.primaryColor * 0.55f));

        Color bladeEmissive = Color.Lerp(idleEmissive, def.coreColor * 2.4f, intensity) * (showShell ? pulse : 1f);
        for (int i = 0; i < weaponBodyMaterials.Count; i++)
        {
            Material m = weaponBodyMaterials[i];
            if (m != null && m.HasProperty("_EmissionColor"))
            {
                m.SetColor("_EmissionColor", bladeEmissive);
            }
        }

        // Bùng hạt nguyên tố mạnh hơn khi tụ lực / Power Parry (tắt tạm khi đang tàng hình)
        for (int i = 0; i < ambientParticles.Count; i++)
        {
            ParticleEntry entry = ambientParticles[i];
            if (entry.ps == null) continue;
            var emission = entry.ps.emission;
            emission.rateOverTime = isStealthHidden
                ? 0f
                : Mathf.Lerp(entry.idleRate, entry.powerRate, intensity);
        }
    }

    Material CreateAlphaParticleMat(Color col, int queue)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        Material m = new Material(sh);
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
        if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        if (m.HasProperty("_Color")) m.SetColor("_Color", col);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = queue;
        runtimeMaterials.Add(m);
        return m;
    }

    Material CreateAuraAdditiveMat(Color col)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        Material m = new Material(sh);
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 2f);
        if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        if (m.HasProperty("_Color")) m.SetColor("_Color", col);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3070;
        runtimeMaterials.Add(m);
        return m;
    }
}
