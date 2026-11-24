using System.Collections.Generic;
using UnityEngine;

public class PlayerSkinController : MonoBehaviour
{
    public SkinType CurrentSkin { get; private set; } = SkinType.DefaultStriker;
    public int SkinVersion { get; private set; }

    Transform headBone;
    Transform upperChestBone;
    Transform hipsBone;
    Transform leftUpperLegBone;
    Transform rightUpperLegBone;
    Transform leftLowerLegBone;
    Transform rightLowerLegBone;

    SkinnedMeshRenderer bodyMeshRenderer;
    Mesh originalBodyMesh;
    Material[] originalBodyMaterials;

    readonly List<GameObject> spawnedOutfitParts = new List<GameObject>();
    readonly List<Material> runtimeMaterials = new List<Material>();
    readonly List<Texture2D> runtimeTextures = new List<Texture2D>();
    readonly List<Mesh> runtimeMeshes = new List<Mesh>();

    // Live animation references
    Transform articulatedWingL;
    Transform articulatedWingR;
    bool wingsAreCyber;

    Transform valkyrieHaloOuter;
    Transform valkyrieHaloInner;
    Transform valkyrieSigilDisc;
    LineRenderer[] valkyrieHaloArcs;
    float valkyrieArcRefresh;

    SoftClothSurface activeSoftCloth;
    ParticleSystem ambientAuraPs;

    bool initialized;
    bool isStealthHidden;

    Vector3 lastWorldPos;
    float lastYaw;
    float smoothSpeed;
    float smoothVerticalVel;
    float smoothYawRate;

    class SoftClothSurface
    {
        public Mesh mesh;
        public int cols;
        public int rows;
        public float topHalfWidth;
        public float botHalfWidth;
        public float length;
        public float topY;
        public float topZ;
        public float wrapAngleDeg;
        public float tatterAmp;
        public float waveSpeed;
        public float waveAmp;
        public bool fullWrap360;
        public float frontLength;
        public float backLength;
        public float topRadiusX;
        public float topRadiusZ;
        public float botRadiusX;
        public float botRadiusZ;
        public Vector3[] verts;
        public float phaseOffset;
    }

    void Awake()
    {
        CacheRigReferences();
        lastWorldPos = transform.position;
        lastYaw = transform.eulerAngles.y;
    }

    void OnDestroy()
    {
        CleanupRuntimeAssets();
    }

    void LateUpdate()
    {
        float dt = Mathf.Clamp(Time.deltaTime, 0.0001f, 0.05f);
        Vector3 worldDelta = transform.position - lastWorldPos;
        lastWorldPos = transform.position;

        Vector3 horiz = new Vector3(worldDelta.x, 0f, worldDelta.z);
        float instSpeed = horiz.magnitude / dt;
        float instVert = worldDelta.y / dt;
        float curYaw = transform.eulerAngles.y;
        float yawRate = Mathf.DeltaAngle(lastYaw, curYaw) / dt;
        lastYaw = curYaw;

        smoothSpeed = Mathf.Lerp(smoothSpeed, Mathf.Clamp(instSpeed, 0f, 18f), dt * 7.5f);
        smoothVerticalVel = Mathf.Lerp(smoothVerticalVel, Mathf.Clamp(instVert, -14f, 14f), dt * 8.0f);
        smoothYawRate = Mathf.Lerp(smoothYawRate, Mathf.Clamp(yawRate, -260f, 260f), dt * 6.5f);

        // 1. Đôi Cánh Thiên Thần (Bộ 5 Paladin — TRẠNG THÁI NGHỈ XẾP GỌN SAU LƯNG!) & Đôi Cánh Cyber (Bộ 2)
        if (articulatedWingL != null && articulatedWingR != null)
        {
            float speedNorm = Mathf.Clamp01(smoothSpeed / 10f);
            float airNorm = Mathf.Clamp01(Mathf.Abs(smoothVerticalVel) / 8f);

            if (wingsAreCyber)
            {
                float flap = Mathf.Sin(Time.time * (3.2f + smoothSpeed * 0.25f)) * (5.0f + speedNorm * 7f);
                float yAngle = 18f + speedNorm * 8f + flap;
                articulatedWingR.localRotation = Quaternion.Euler(4f, +yAngle, +(2f + flap * 0.2f));
                articulatedWingL.localRotation = Quaternion.Euler(4f, -yAngle, -(2f + flap * 0.2f));
            }
            else
            {
                // Bộ 5 Hiệp Sĩ: Trạng thái NGHỈ xếp gọn uy nghiêm sau lưng (chỉ nhấp nhô nhịp thở nhẹ, KHÔNG dang cánh quá lố!)
                float breath = Mathf.Sin(Time.time * 2.1f) * (2.2f + speedNorm * 3.5f + airNorm * 4.0f);
                float restFoldY = 42f - speedNorm * 6f + breath;
                float restTiltZ = -4f + breath * 0.25f;

                articulatedWingR.localRotation = Quaternion.Euler(6f, +restFoldY, +restTiltZ);
                articulatedWingL.localRotation = Quaternion.Euler(6f, -restFoldY, -restTiltZ);
            }
        }

        // 2. Vầng Sáng Thần Thánh Lôi Thần phía sau Nữ Valkyrie (Bộ 4) — Cực Đẹp & Mượt!
        if (valkyrieSigilDisc != null)
        {
            float pulse = 1.0f + 0.055f * Mathf.Sin(Time.time * 3.8f);
            valkyrieSigilDisc.localScale = Vector3.one * (1.28f * pulse);
            valkyrieSigilDisc.Rotate(0f, 0f, 22f * dt, Space.Self);
        }
        if (valkyrieHaloOuter != null)
        {
            valkyrieHaloOuter.Rotate(0f, 0f, -48f * dt, Space.Self);
        }
        if (valkyrieHaloInner != null)
        {
            valkyrieHaloInner.Rotate(0f, 0f, 78f * dt, Space.Self);
        }
        if (valkyrieHaloArcs != null && valkyrieHaloArcs.Length > 0)
        {
            valkyrieArcRefresh -= dt;
            if (valkyrieArcRefresh <= 0f)
            {
                valkyrieArcRefresh = 0.055f;
                for (int i = 0; i < valkyrieHaloArcs.Length; i++)
                {
                    LineRenderer lr = valkyrieHaloArcs[i];
                    if (lr == null) continue;
                    int pts = lr.positionCount;
                    float startAng = Random.Range(0f, Mathf.PI * 2f);
                    float span = Random.Range(0.75f, 1.35f);
                    float baseR = (i % 2 == 0) ? 0.44f : 0.60f;
                    for (int p = 0; p < pts; p++)
                    {
                        float f = (float)p / (pts - 1);
                        float a = startAng + f * span;
                        float r = baseR + Random.Range(-0.045f, 0.045f);
                        lr.SetPosition(p, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, Random.Range(-0.02f, 0.02f)));
                    }
                }
            }
        }

        // 3. Áo Choàng Vải Mềm Bay Thật (Nữ Tử Thần / Khăn Ronin / Tabard Paladin)!
        if (activeSoftCloth != null && activeSoftCloth.mesh != null)
        {
            UpdateSoftClothMesh(activeSoftCloth);
        }
    }

    void UpdateSoftClothMesh(SoftClothSurface cloth)
    {
        float t = Time.time + cloth.phaseOffset;
        int cols = cloth.cols;
        int rows = cloth.rows;
        float speedNorm = Mathf.Clamp01(smoothSpeed / 10.5f);
        float yawBend = Mathf.Clamp(smoothYawRate / 180f, -1.2f, 1.2f);
        float fallLift = Mathf.Clamp(-smoothVerticalVel * 0.045f, -0.18f, 0.45f);

        float wrapRad = cloth.wrapAngleDeg * Mathf.Deg2Rad;
        float sinWrap = Mathf.Max(0.25f, Mathf.Sin(wrapRad));

        if (cloth.fullWrap360)
        {
            // ÁO CHOÀNG TRÙM KÍN 360 ĐỘ TOÀN THÂN (phủ kín vai, ngực, thân xuống giữa đùi trước / dài hơn ở lưng, ĐỂ LỘ ĐÔI CHÂN DÀI TRẮNG NGẦN!)
            int idx360 = 0;
            for (int r = 0; r <= rows; r++)
            {
                float v = (float)r / rows;
                float vCurve = Mathf.Pow(v, 1.55f);
                float vDrag = Mathf.Pow(v, 1.32f);

                // Phom dáng chữ A rủ mềm từ cổ/vai xuống ngang giữa đùi (lộ trọn đôi chân dài trắng bên dưới!)
                float rx = Mathf.Lerp(cloth.topRadiusX, cloth.botRadiusX, Mathf.Pow(v, 0.72f));
                float rz = Mathf.Lerp(cloth.topRadiusZ, cloth.botRadiusZ, Mathf.Pow(v, 0.78f));

                // Phồng nhẹ ở vùng vai (v ~ 0.16..0.34) để phủ kín hai bên vai & bắp tay thật mềm mại
                float shoulderBulge = Mathf.Sin(Mathf.Clamp01(v / 0.42f) * Mathf.PI) * 0.105f;
                rx += shoulderBulge;
                rz += shoulderBulge * 0.55f;

                float trailBack = (speedNorm * 0.34f + fallLift * 0.30f) * vDrag;
                float turnSwingX = -yawBend * 0.22f * vCurve;

                for (int c = 0; c <= cols; c++)
                {
                    float u = (float)c / cols; // 0..1 vòng tròn kín 360 độ (u=0 -> +Z trước ngực, u=0.5 -> -Z sau lưng)
                    float ang = u * Mathf.PI * 2f;
                    float sinA = Mathf.Sin(ang);
                    float cosA = Mathf.Cos(ang); // +1 ở phía trước, -1 ở phía sau

                    // Trước ngắn ngang giữa đùi (lộ đôi chân dài trắng!), sau rủ dài thướt tha hơn
                    float backFactor = (1f - cosA) * 0.5f; // 0 ở trước -> 1 ở sau lưng
                    float localLen = Mathf.Lerp(cloth.frontLength, cloth.backLength, Mathf.Pow(backFactor, 1.15f));

                    // Sóng vải mềm chạy dọc theo vạt áo choàng (bay bay thật tự nhiên khi chạy & đứng gió!)
                    float waveFreq = cloth.waveSpeed + speedNorm * 3.6f;
                    float radialWave = Mathf.Sin(v * 6.2f - t * waveFreq + ang * 3.0f)
                                     + Mathf.Cos(v * 10.5f - t * (waveFreq * 1.4f) - ang * 5.0f) * 0.38f;
                    float pleat = Mathf.Cos(ang * 7.0f) * 0.022f * (0.25f + 0.75f * v);

                    float waveAmp = (cloth.waveAmp + speedNorm * 0.065f) * vCurve * (0.55f + 0.65f * backFactor);
                    float rWave = radialWave * waveAmp;

                    // Vạt dưới lượn sóng nhẹ mềm mại
                    float hemWave = (cloth.tatterAmp > 0.001f && v > 0.70f)
                        ? (Mathf.Sin(ang * 6.0f) * 0.5f + 0.5f) * cloth.tatterAmp * ((v - 0.70f) / 0.30f) * backFactor
                        : 0f;

                    float px = sinA * (rx + pleat + rWave) + turnSwingX * (0.45f + 0.55f * backFactor);
                    float py = cloth.topY - (localLen * v + hemWave) + (speedNorm * 0.10f + fallLift * 0.16f) * vCurve * backFactor;
                    float pz = cosA * (rz + pleat + rWave) - trailBack * (0.30f + 0.85f * backFactor);

                    cloth.verts[idx360++] = new Vector3(px, py, pz);
                }
            }

            cloth.mesh.vertices = cloth.verts;
            cloth.mesh.RecalculateNormals();
            cloth.mesh.RecalculateBounds();
            return;
        }

        int idx = 0;
        for (int r = 0; r <= rows; r++)
        {
            float v = (float)r / rows;
            float vCurve = Mathf.Pow(v, 1.62f);
            float vDrag = Mathf.Pow(v, 1.35f);

            float halfW = Mathf.Lerp(cloth.topHalfWidth, cloth.botHalfWidth, Mathf.Pow(v, 1.08f));
            float trailBack = (0.16f + speedNorm * 0.62f + fallLift * 0.55f) * vDrag;
            float liftUp = (speedNorm * 0.24f + fallLift * 0.34f) * vCurve;
            float turnSwingX = -yawBend * 0.34f * vCurve;

            for (int c = 0; c <= cols; c++)
            {
                float u = (float)c / cols;
                float uS = (u - 0.5f) * 2f;

                float ang = uS * wrapRad * Mathf.Lerp(1.0f, 0.72f, v);
                float baseX = (cloth.wrapAngleDeg > 8f)
                    ? Mathf.Sin(ang) * (halfW / sinWrap)
                    : (uS * halfW);
                float shoulderWrapZ = (cloth.wrapAngleDeg > 8f)
                    ? (1f - Mathf.Cos(ang)) * (0.13f + 0.07f * (1f - v))
                    : 0f;

                float waveFreq = cloth.waveSpeed + speedNorm * 3.8f;
                float mainWave = Mathf.Sin(v * 6.5f - t * waveFreq + uS * 2.2f);
                float subWave = Mathf.Cos(v * 11.5f - t * (waveFreq * 1.45f) - uS * 4.0f) * 0.42f;
                float edgeFlutter = Mathf.Sin(t * (waveFreq * 1.7f) + u * 9.0f) * (0.28f + 0.45f * Mathf.Abs(uS));

                float amp = (cloth.waveAmp + speedNorm * 0.095f) * vCurve;
                float waveZ = (mainWave + subWave) * amp;
                float waveX = (edgeFlutter * 0.55f) * amp;
                float foldPleat = Mathf.Cos(uS * Mathf.PI * 3.0f) * 0.025f * (0.3f + 0.7f * v);

                float hemOffset = 0f;
                if (cloth.tatterAmp > 0.001f && v > 0.65f)
                {
                    float env = (v - 0.65f) / 0.35f;
                    hemOffset = (Mathf.Sin(u * Mathf.PI * 5.0f) * 0.5f + 0.5f) * cloth.tatterAmp * env;
                }

                float px = baseX + turnSwingX + waveX;
                float py = cloth.topY - (cloth.length * v + hemOffset) + liftUp;
                float pz = cloth.topZ - shoulderWrapZ - trailBack + foldPleat + waveZ;

                cloth.verts[idx++] = new Vector3(px, py, pz);
            }
        }

        cloth.mesh.vertices = cloth.verts;
        cloth.mesh.RecalculateNormals();
        cloth.mesh.RecalculateBounds();
    }

    void CacheRigReferences()
    {
        if (headBone != null && upperChestBone != null && hipsBone != null && bodyMeshRenderer != null)
            return;

        Transform[] all = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t == null) continue;
            switch (t.name)
            {
                case "Head": headBone = t; break;
                case "UpperChest": upperChestBone = t; break;
                case "Chest": if (upperChestBone == null) upperChestBone = t; break;
                case "Hips": hipsBone = t; break;
                case "Left_UpperLeg": leftUpperLegBone = t; break;
                case "Right_UpperLeg": rightUpperLegBone = t; break;
                case "Left_LowerLeg": leftLowerLegBone = t; break;
                case "Right_LowerLeg": rightLowerLegBone = t; break;
            }
        }

        SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer smr in smrs)
        {
            if (smr != null)
            {
                bodyMeshRenderer = smr;
                if (originalBodyMesh == null)
                {
                    originalBodyMesh = smr.sharedMesh;
                }
                if (originalBodyMaterials == null)
                {
                    originalBodyMaterials = smr.sharedMaterials;
                }
                break;
            }
        }
    }

    public void ApplySkin(SkinType type)
    {
        if (initialized && CurrentSkin == type)
            return;

        initialized = true;
        CurrentSkin = type;
        SkinVersion++;

        CacheRigReferences();
        ClearSkinAccessories();

        SkinDefinition def = SkinCatalog.Get(type);

        // 1. Vóc dáng Nam / Nữ tinh tế & chất liệu da / trang phục PBR
        ApplyGenderBodyProportions(def);
        ApplyBodySuitMaterial(def);

        // 2. Phụ kiện đầu (Tóc Nam / Kính Cyber / Nón Kasa / Tóc Đuôi Ngựa Nữ Valkyrie / Hào quang / Tóc Dài Nữ Tử Thần)
        if (headBone != null && !string.IsNullOrEmpty(def.headModelResource))
        {
            SpawnSkinAccessory(headBone, def.headModelResource, def, "SkinHead_" + type);
        }

        // 3. Áo / Giáp thân trên
        if (upperChestBone != null && !string.IsNullOrEmpty(def.bodyModelResource))
        {
            SpawnSkinAccessory(upperChestBone, def.bodyModelResource, def, "SkinBody_" + type);
        }

        // 4. Quần ngắn / Váy / Đai lưng / Song kiếm ở hông
        if (hipsBone != null && !string.IsNullOrEmpty(def.waistModelResource))
        {
            SpawnSkinAccessory(hipsBone, def.waistModelResource, def, "SkinWaist_" + type);
        }

        // 5. Đôi Cánh 3D (Bộ 2 Cyber Runner & Bộ 5 Thánh Hiệp Ánh Kim — ĐÃ SỬA ĐÚNG CHIỀU HƯỚNG RA SAU!)
        if (upperChestBone != null && !string.IsNullOrEmpty(def.leftWingResource) && !string.IsNullOrEmpty(def.rightWingResource))
        {
            SpawnArticulatedWings(upperChestBone, def, def.style == SkinArchetypeStyle.CyberWingedMale);
        }

        // 6. Phong cách ĐẶC TRƯNG riêng từng bộ (Quần ngắn + Bốt Nữ / Vầng sáng Lôi Thần / Áo Choàng Mềm Nữ Tử Thần / Khăn Ronin!)
        switch (def.style)
        {
            case SkinArchetypeStyle.StarterSimpleMale:
                // Bộ 1 Nam Tân Binh: Quần dài thể thao tối màu + giày thể thao đơn giản gọn gàng!
                SpawnLegGarments(false, false, def);
                break;

            case SkinArchetypeStyle.ValkyrieShortsFemale:
                // Bộ 4 Nữ Valkyrie: Quần ngắn chiến đấu (để lộ đùi da tự nhiên) + Bốt cao cổ + Vầng Sáng Thần Thánh cực đẹp!
                SpawnLegGarments(true, true, def);
                if (upperChestBone != null)
                {
                    BuildValkyrieDivineHalo(upperChestBone, def);
                }
                break;

            case SkinArchetypeStyle.RoninKasaMale:
                // Bộ 3 Nam Ronin: Khăn choàng lụa đỏ bay mềm mại tự nhiên!
                if (upperChestBone != null)
                {
                    BuildSoftClothElement(
                        upperChestBone,
                        "RoninSilkScarf",
                        cols: 10,
                        rows: 16,
                        topHalfW: 0.13f,
                        botHalfW: 0.19f,
                        length: 1.02f,
                        topY: 0.18f,
                        topZ: -0.10f,
                        wrapDeg: 20f,
                        tatterAmp: 0.08f,
                        waveSpeed: 6.2f,
                        waveAmp: 0.085f,
                        topCol: new Color(0.76f, 0.06f, 0.09f, 1f),
                        botCol: new Color(1.00f, 0.34f, 0.10f, 1f),
                        trimCol: def.suitAccentColor,
                        emissiveStrength: 0.35f
                    );
                }
                break;

            case SkinArchetypeStyle.PaladinWingsMale:
                // Bộ 5 Nam Thánh Hiệp: Dải lụa Tabard hoàng gia bay mềm từ hông!
                if (hipsBone != null)
                {
                    BuildSoftClothElement(
                        hipsBone,
                        "PaladinTabardCloth",
                        cols: 10,
                        rows: 12,
                        topHalfW: 0.16f,
                        botHalfW: 0.24f,
                        length: 0.72f,
                        topY: 0.04f,
                        topZ: -0.14f,
                        wrapDeg: 18f,
                        tatterAmp: 0.0f,
                        waveSpeed: 5.0f,
                        waveAmp: 0.055f,
                        topCol: new Color(0.96f, 0.97f, 1.00f, 1f),
                        botCol: new Color(1.00f, 0.84f, 0.24f, 1f),
                        trimCol: def.suitAccentColor,
                        emissiveStrength: 0.45f
                    );
                }
                break;

            case SkinArchetypeStyle.ReaperCloakFemale:
                // Bộ 6 Nữ Tử Thần (theo đúng ảnh mẫu!): Mũ trùm nhọn viền vàng + tóc bạc trắng + corset tím than xẻ cao lộ đôi chân dài trắng + ÁO CHOÀNG RỘNG VIỀN HOA VĂN VÀNG ĐỒNG BAY MỀM MẠI!
                SpawnFemaleAnkleShoes(def);
                if (upperChestBone != null)
                {
                    BuildSoftClothElement(
                        upperChestBone,
                        "ReaperGothicGoldCloak",
                        cols: 24,
                        rows: 18,
                        topHalfW: 0.28f,
                        botHalfW: 0.78f,
                        length: 1.44f,
                        topY: 0.20f,
                        topZ: -0.09f,
                        wrapDeg: 70f,
                        tatterAmp: 0.14f,
                        waveSpeed: 5.2f,
                        waveAmp: 0.105f,
                        topCol: new Color(0.11f, 0.05f, 0.17f, 1f),
                        botCol: new Color(0.24f, 0.09f, 0.36f, 1f),
                        trimCol: new Color(0.86f, 0.70f, 0.36f, 1f), // Viền hoa văn Vàng Đồng cổ điển như ảnh mẫu!
                        emissiveStrength: 0.40f
                    );
                }
                break;
        }

        if (def.hasAmbientParticles && upperChestBone != null)
        {
            SpawnSkinAuraParticles(upperChestBone, def);
        }

        if (isStealthHidden && ambientAuraPs != null)
        {
            ambientAuraPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void ApplyGenderBodyProportions(SkinDefinition def)
    {
        // Khi mặc đồ nữ: thân thể SmoothFemaleBodyBuilder đã có đường cong eo-hông-vai nữ mềm mại tự nhiên nên giữ tỷ lệ xương cân đối!
        if (upperChestBone != null)
        {
            upperChestBone.localScale = def.isFemale
                ? new Vector3(0.97f, 0.99f, 0.98f)
                : Vector3.one;
        }
        if (hipsBone != null)
        {
            hipsBone.localScale = def.isFemale
                ? new Vector3(1.01f, 0.99f, 1.00f)
                : Vector3.one;
        }
    }

    /// <summary>
    /// Cập nhật ngay tư thế cánh nghỉ, vạt áo choàng mềm và tia hào quang (dùng cho chụp ảnh đại diện Skin 3D trong Shop!).
    /// </summary>
    public void ApplyPreviewPose()
    {
        if (articulatedWingL != null && articulatedWingR != null)
        {
            if (wingsAreCyber)
            {
                articulatedWingR.localRotation = Quaternion.Euler(4f, +20f, +2f);
                articulatedWingL.localRotation = Quaternion.Euler(4f, -20f, -2f);
            }
            else
            {
                articulatedWingR.localRotation = Quaternion.Euler(6f, +42f, -4f);
                articulatedWingL.localRotation = Quaternion.Euler(6f, -42f, +4f);
            }
        }

        if (activeSoftCloth != null && activeSoftCloth.mesh != null)
        {
            smoothSpeed = 2.4f;
            smoothVerticalVel = 0f;
            smoothYawRate = 12f;
            UpdateSoftClothMesh(activeSoftCloth);
        }

        if (valkyrieHaloArcs != null)
        {
            for (int i = 0; i < valkyrieHaloArcs.Length; i++)
            {
                LineRenderer lr = valkyrieHaloArcs[i];
                if (lr == null) continue;
                int pts = lr.positionCount;
                float startAng = i * Mathf.PI * 0.5f + 0.25f;
                float span = 1.05f;
                float baseR = (i % 2 == 0) ? 0.44f : 0.60f;
                for (int p = 0; p < pts; p++)
                {
                    float f = (float)p / (pts - 1);
                    float a = startAng + f * span;
                    float r = baseR + Mathf.Sin(p * 2.1f + i) * 0.032f;
                    lr.SetPosition(p, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
                }
            }
        }
    }

    /// <summary>
    /// Tạo ống quần ngắn (Nữ Valkyrie), quần dài đơn giản (Nam Tân Binh) và bốt cao cổ bám đúng trục xương chân!
    /// </summary>
    void SpawnLegGarments(bool isFemaleShorts, bool kneeHighBoots, SkinDefinition def)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        Material pantsMat = CreateArmorMat(urpLit, def.suitSecondaryColor, 0.18f, 0.42f, def.emissionColor * 0.10f);
        Material bootMat = CreateArmorMat(urpLit, def.suitBaseColor, 0.45f, 0.76f, def.emissionColor * 0.20f);
        Material trimMat = CreateArmorMat(urpLit, def.suitAccentColor, 0.70f, 0.92f, def.suitAccentColor * 0.85f);

        Transform[] upperLegs = { leftUpperLegBone, rightUpperLegBone };
        for (int i = 0; i < 2; i++)
        {
            Transform leg = upperLegs[i];
            if (leg == null) continue;
            float dirSign = (i == 1) ? -1f : 1f; // Right leg chạy theo -Y, Left leg chạy theo +Y!

            if (isFemaleShorts)
            {
                // Ống quần ngắn ôm gọn phần trên đùi (bọc nhẹ ngoài đùi nữ mềm mại, để lộ đùi da mịn bên dưới!)
                GameObject cuff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cuff.name = "FemaleShortLeg_" + i;
                cuff.transform.SetParent(leg, false);
                cuff.transform.localPosition = new Vector3(0f, 0.088f * dirSign, 0f);
                cuff.transform.localScale = new Vector3(0.168f, 0.082f, 0.178f);
                Destroy(cuff.GetComponent<Collider>());
                cuff.GetComponent<Renderer>().sharedMaterial = bootMat;
                cuff.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                spawnedOutfitParts.Add(cuff);
            }
            else if (!def.isFemale)
            {
                // Quần dài thể thao đơn giản phủ gọn đùi cho Tân Binh Nam
                GameObject thigh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                thigh.name = "MalePantsUpper_" + i;
                thigh.transform.SetParent(leg, false);
                thigh.transform.localPosition = new Vector3(0f, 0.20f * dirSign, 0f);
                thigh.transform.localScale = new Vector3(0.146f, 0.21f, 0.152f);
                Destroy(thigh.GetComponent<Collider>());
                thigh.GetComponent<Renderer>().sharedMaterial = pantsMat;
                thigh.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                spawnedOutfitParts.Add(thigh);
            }
        }

        Transform[] lowerLegs = { leftLowerLegBone, rightLowerLegBone };
        for (int i = 0; i < 2; i++)
        {
            Transform shin = lowerLegs[i];
            if (shin == null) continue;
            float dirSign = (i == 1) ? -1f : 1f;

            if (kneeHighBoots)
            {
                // Bốt chiến đấu cao cổ nữ (phủ cẳng chân dưới đầu gối + viền sáng tinh tế!)
                GameObject boot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                boot.name = "FemaleBoot_" + i;
                boot.transform.SetParent(shin, false);
                boot.transform.localPosition = new Vector3(0f, 0.23f * dirSign, 0f);
                boot.transform.localScale = new Vector3(0.130f, 0.175f, 0.138f);
                Destroy(boot.GetComponent<Collider>());
                boot.GetComponent<Renderer>().sharedMaterial = bootMat;
                boot.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                spawnedOutfitParts.Add(boot);

                GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rim.name = "BootRim_" + i;
                rim.transform.SetParent(shin, false);
                rim.transform.localPosition = new Vector3(0f, 0.065f * dirSign, 0f);
                rim.transform.localScale = new Vector3(0.138f, 0.018f, 0.146f);
                Destroy(rim.GetComponent<Collider>());
                rim.GetComponent<Renderer>().sharedMaterial = trimMat;
                rim.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                spawnedOutfitParts.Add(rim);
            }
            else if (!def.isFemale)
            {
                // Ống quần dài xuống mắt cá chân cho Tân Binh Nam
                GameObject calf = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                calf.name = "MalePantsLower_" + i;
                calf.transform.SetParent(shin, false);
                calf.transform.localPosition = new Vector3(0f, 0.20f * dirSign, 0f);
                calf.transform.localScale = new Vector3(0.124f, 0.205f, 0.128f);
                Destroy(calf.GetComponent<Collider>());
                calf.GetComponent<Renderer>().sharedMaterial = pantsMat;
                calf.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                spawnedOutfitParts.Add(calf);
            }
        }
    }

    void SpawnFemaleAnkleShoes(SkinDefinition def)
    {
        // Giày Gothic thấp cổ nhỏ gọn ở sát mắt cá chân để lộ TRỌN VẸN đôi chân dài trắng!
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        Material shoeMat = CreateArmorMat(urpLit, def.suitBaseColor, 0.28f, 0.65f, def.emissionColor * 0.14f);
        Material trimMat = CreateArmorMat(urpLit, def.suitAccentColor, 0.65f, 0.90f, def.suitAccentColor * 0.75f);

        Transform[] lowerLegs = { leftLowerLegBone, rightLowerLegBone };
        for (int i = 0; i < 2; i++)
        {
            Transform shin = lowerLegs[i];
            if (shin == null) continue;
            float dirSign = (i == 1) ? -1f : 1f;

            GameObject shoe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shoe.name = "FemaleAnkleShoe_" + i;
            shoe.transform.SetParent(shin, false);
            shoe.transform.localPosition = new Vector3(0f, 0.365f * dirSign, 0f);
            shoe.transform.localScale = new Vector3(0.098f, 0.055f, 0.108f);
            Destroy(shoe.GetComponent<Collider>());
            shoe.GetComponent<Renderer>().sharedMaterial = shoeMat;
            shoe.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spawnedOutfitParts.Add(shoe);

            GameObject strap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            strap.name = "FemaleAnkleStrap_" + i;
            strap.transform.SetParent(shin, false);
            strap.transform.localPosition = new Vector3(0f, 0.315f * dirSign, 0f);
            strap.transform.localScale = new Vector3(0.104f, 0.012f, 0.112f);
            Destroy(strap.GetComponent<Collider>());
            strap.GetComponent<Renderer>().sharedMaterial = trimMat;
            strap.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spawnedOutfitParts.Add(strap);
        }
    }

    void BuildFullWrapReaperCloak(Transform parent, SkinDefinition def)
    {
        GameObject go = new GameObject("FemaleReaperFullWrapCloak");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        spawnedOutfitParts.Add(go);

        const int cols = 28;
        const int rows = 16;
        int vertCount = (cols + 1) * (rows + 1);
        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] tris = new int[cols * rows * 6];

        int idx = 0;
        for (int r = 0; r <= rows; r++)
        {
            float v = (float)r / rows;
            for (int c = 0; c <= cols; c++)
            {
                float u = (float)c / cols;
                uvs[idx] = new Vector2(u, 1f - v);
                verts[idx] = Vector3.zero;
                idx++;
            }
        }

        int ti = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int i0 = r * (cols + 1) + c;
                int i1 = i0 + 1;
                int i2 = i0 + (cols + 1);
                int i3 = i2 + 1;
                tris[ti++] = i0; tris[ti++] = i1; tris[ti++] = i2;
                tris[ti++] = i1; tris[ti++] = i3; tris[ti++] = i2;
            }
        }

        Mesh clothMesh = new Mesh();
        clothMesh.name = "FemaleReaperWrapCloakMesh";
        clothMesh.MarkDynamic();
        clothMesh.vertices = verts;
        clothMesh.uv = uvs;
        clothMesh.triangles = tris;
        runtimeMeshes.Add(clothMesh);

        go.AddComponent<MeshFilter>().sharedMesh = clothMesh;

        Texture2D clothTex = CreateSoftClothGradientTexture(
            new Color(0.035f, 0.016f, 0.068f, 1f),
            new Color(0.32f, 0.07f, 0.56f, 1f),
            new Color(0.88f, 0.30f, 1.00f, 1f)
        );
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material mat = new Material(sh);
        mat.mainTexture = clothTex;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", clothTex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.06f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.30f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.36f, 0.08f, 0.62f, 1f) * 0.42f);
        }
        runtimeMaterials.Add(mat);

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // Trùm kín cổ/vai/thân từ y=+0.24 (UpperChest ~ 1.48m) xuống ngang giữa đùi trước (~0.80m -> frontLength=0.68m)
        // và dài thướt tha hơn ở lưng (~0.52m -> backLength=0.96m) để LỘ TRỌN ĐÔI CHÂN DÀI TRẮNG NGẦN!
        activeSoftCloth = new SoftClothSurface
        {
            mesh = clothMesh,
            cols = cols,
            rows = rows,
            fullWrap360 = true,
            topY = 0.24f,
            topZ = 0f,
            frontLength = 0.68f,
            backLength = 0.98f,
            topRadiusX = 0.145f,
            topRadiusZ = 0.135f,
            botRadiusX = 0.34f,
            botRadiusZ = 0.28f,
            tatterAmp = 0.09f,
            waveSpeed = 5.4f,
            waveAmp = 0.068f,
            verts = verts,
            phaseOffset = Random.Range(0f, 6.28f)
        };

        UpdateSoftClothMesh(activeSoftCloth);
    }

    // ================= VẦNG HÀO QUANG THẦN THÁNH LÔI THẦN (BỘ 4 NỮ VALKYRIE) =================

    void BuildValkyrieDivineHalo(Transform parent, SkinDefinition def)
    {
        GameObject root = new GameObject("ValkyrieDivineHaloRoot");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0f, 0.24f, -0.25f);
        spawnedOutfitParts.Add(root);

        Shader pSh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (pSh == null) pSh = Shader.Find("Sprites/Default");

        // 1. Đĩa Hào Quang Thần Thánh phát sáng mềm (Ngôi sao 8 cánh + vòng cổ ngữ Lôi Thần vẽ bằng Texture 256x256!)
        Texture2D sigilTex = CreateValkyrieHaloSigilTexture();
        Material discMat = new Material(pSh);
        if (discMat.HasProperty("_Surface")) discMat.SetFloat("_Surface", 1f);
        if (discMat.HasProperty("_Blend")) discMat.SetFloat("_Blend", 2f);
        if (discMat.HasProperty("_SrcBlend")) discMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (discMat.HasProperty("_DstBlend")) discMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (discMat.HasProperty("_ZWrite")) discMat.SetFloat("_ZWrite", 0f);
        if (discMat.HasProperty("_Cull")) discMat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        if (discMat.HasProperty("_BaseColor")) discMat.SetColor("_BaseColor", new Color(0.55f, 0.94f, 1.00f, 0.92f));
        if (discMat.HasProperty("_Color")) discMat.SetColor("_Color", new Color(0.55f, 0.94f, 1.00f, 0.92f));
        discMat.mainTexture = sigilTex;
        if (discMat.HasProperty("_BaseMap")) discMat.SetTexture("_BaseMap", sigilTex);
        discMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        discMat.renderQueue = 3058;
        runtimeMaterials.Add(discMat);

        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Quad);
        disc.name = "ValkyrieSigilDisc";
        disc.transform.SetParent(root.transform, false);
        disc.transform.localPosition = Vector3.zero;
        disc.transform.localScale = Vector3.one * 1.28f;
        Destroy(disc.GetComponent<Collider>());
        Renderer dr = disc.GetComponent<Renderer>();
        dr.sharedMaterial = discMat;
        dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        valkyrieSigilDisc = disc.transform;

        // 2. Vòng ngoài gồm 8 tinh thể pha lê kim cương phát sáng quay ngược chiều
        Shader litSh = Shader.Find("Universal Render Pipeline/Lit");
        if (litSh == null) litSh = Shader.Find("Standard");
        Material crystalMat = CreateArmorMat(litSh, new Color(0.76f, 0.96f, 1.00f, 1f), 0.35f, 0.98f, new Color(0.32f, 0.92f, 1.00f, 1f) * 2.2f);
        Material goldShardMat = CreateArmorMat(litSh, new Color(1.00f, 0.92f, 0.56f, 1f), 0.85f, 0.95f, new Color(0.95f, 0.82f, 0.30f, 1f) * 1.4f);

        GameObject outer = new GameObject("ValkyrieOuterCrystals");
        outer.transform.SetParent(root.transform, false);
        valkyrieHaloOuter = outer.transform;
        for (int k = 0; k < 8; k++)
        {
            float a = (k / 8f) * Mathf.PI * 2f;
            float r = (k % 2 == 0) ? 0.62f : 0.54f;
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.transform.SetParent(outer.transform, false);
            shard.transform.localPosition = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
            shard.transform.localRotation = Quaternion.Euler(0f, 0f, k * 45f - 90f);
            shard.transform.localScale = (k % 2 == 0) ? new Vector3(0.055f, 0.18f, 0.035f) : new Vector3(0.042f, 0.11f, 0.028f);
            Destroy(shard.GetComponent<Collider>());
            shard.GetComponent<Renderer>().sharedMaterial = (k % 2 == 0) ? crystalMat : goldShardMat;
            shard.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // 3. Vòng trong xoay thuận chiều + 4 tia hồ quang sét nhảy lách tách quanh vòng!
        GameObject inner = new GameObject("ValkyrieInnerArcs");
        inner.transform.SetParent(root.transform, false);
        valkyrieHaloInner = inner.transform;

        Material arcMat = new Material(pSh);
        if (arcMat.HasProperty("_Surface")) arcMat.SetFloat("_Surface", 1f);
        if (arcMat.HasProperty("_Blend")) arcMat.SetFloat("_Blend", 2f);
        if (arcMat.HasProperty("_SrcBlend")) arcMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (arcMat.HasProperty("_DstBlend")) arcMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (arcMat.HasProperty("_ZWrite")) arcMat.SetFloat("_ZWrite", 0f);
        if (arcMat.HasProperty("_BaseColor")) arcMat.SetColor("_BaseColor", new Color(0.84f, 0.97f, 1.00f, 0.98f));
        if (arcMat.HasProperty("_Color")) arcMat.SetColor("_Color", new Color(0.84f, 0.97f, 1.00f, 0.98f));
        arcMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        arcMat.renderQueue = 3062;
        runtimeMaterials.Add(arcMat);

        valkyrieHaloArcs = new LineRenderer[4];
        for (int i = 0; i < valkyrieHaloArcs.Length; i++)
        {
            GameObject aGo = new GameObject("ValkyrieSparkArc_" + i);
            aGo.transform.SetParent(inner.transform, false);
            LineRenderer lr = aGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 8;
            lr.startWidth = 0.032f;
            lr.endWidth = 0.018f;
            lr.sharedMaterial = arcMat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            valkyrieHaloArcs[i] = lr;
        }
    }

    Texture2D CreateValkyrieHaloSigilTexture()
    {
        const int sz = 256;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float half = (sz - 1) * 0.5f;
        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);

                if (r > 1.0f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Hào quang tỏa sáng dịu ở tâm
                float softGlow = Mathf.Exp(-r * r * 3.8f) * 0.55f;

                // 2 Vòng tròn thần thánh sắc nét
                float outerRing = Mathf.Exp(-Mathf.Pow((r - 0.84f) / 0.038f, 2f));
                float innerRing = Mathf.Exp(-Mathf.Pow((r - 0.58f) / 0.028f, 2f)) * 0.85f;

                // Ngôi sao 8 cánh thần thánh (Starburst rays)
                float starWave = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 4f)), 10f);
                float starRay = starWave * Mathf.Clamp01(1.0f - r / 0.94f) * Mathf.Clamp01((r - 0.14f) * 4f);

                // Các vạch cổ ngữ rải đều giữa 2 vòng tròn
                float runeBand = (r > 0.62f && r < 0.80f)
                    ? (Mathf.Pow(Mathf.Abs(Mathf.Sin(ang * 12f)), 6f) * 0.55f)
                    : 0f;

                float intensity = Mathf.Clamp01(softGlow + outerRing + innerRing + starRay * 0.92f + runeBand);
                Color cyanCol = new Color(0.28f, 0.86f, 1.00f);
                Color whiteGold = new Color(0.95f, 0.98f, 1.00f);
                Color finalCol = Color.Lerp(cyanCol, whiteGold, Mathf.Clamp01(outerRing + starRay));
                finalCol.a = intensity;
                tex.SetPixel(x, y, finalCol);
            }
        }
        tex.Apply();
        runtimeTextures.Add(tex);
        return tex;
    }

    // ================= ARTICULATED WINGS (CYBER RUNNER & SOLAR PALADIN) =================

    void SpawnArticulatedWings(Transform parent, SkinDefinition def, bool isCyber)
    {
        wingsAreCyber = isCyber;

        GameObject rightPivot = new GameObject("WingPivot_R");
        rightPivot.transform.SetParent(parent, false);
        rightPivot.transform.localPosition = new Vector3(0.08f, 0.12f, -0.15f);
        spawnedOutfitParts.Add(rightPivot);
        articulatedWingR = rightPivot.transform;

        GameObject leftPivot = new GameObject("WingPivot_L");
        leftPivot.transform.SetParent(parent, false);
        leftPivot.transform.localPosition = new Vector3(-0.08f, 0.12f, -0.15f);
        spawnedOutfitParts.Add(leftPivot);
        articulatedWingL = leftPivot.transform;

        GameObject prefR = Resources.Load<GameObject>("Skins/" + def.rightWingResource);
        GameObject prefL = Resources.Load<GameObject>("Skins/" + def.leftWingResource);

        if (prefR != null)
        {
            GameObject instR = Instantiate(prefR, rightPivot.transform, false);
            foreach (Collider c in instR.GetComponentsInChildren<Collider>(true)) Destroy(c);
            UpgradeAccessoryMaterials(instR, def);
        }
        if (prefL != null)
        {
            GameObject instL = Instantiate(prefL, leftPivot.transform, false);
            foreach (Collider c in instL.GetComponentsInChildren<Collider>(true)) Destroy(c);
            UpgradeAccessoryMaterials(instL, def);
        }
    }

    // ================= SOFT CLOTH FABRIC =================

    void BuildSoftClothElement(
        Transform parent,
        string goName,
        int cols,
        int rows,
        float topHalfW,
        float botHalfW,
        float length,
        float topY,
        float topZ,
        float wrapDeg,
        float tatterAmp,
        float waveSpeed,
        float waveAmp,
        Color topCol,
        Color botCol,
        Color trimCol,
        float emissiveStrength
    )
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        spawnedOutfitParts.Add(go);

        int vertCount = (cols + 1) * (rows + 1);
        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] tris = new int[cols * rows * 6];

        int idx = 0;
        for (int r = 0; r <= rows; r++)
        {
            float v = (float)r / rows;
            for (int c = 0; c <= cols; c++)
            {
                float u = (float)c / cols;
                uvs[idx] = new Vector2(u, 1f - v);
                verts[idx] = Vector3.zero;
                idx++;
            }
        }

        int ti = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int i0 = r * (cols + 1) + c;
                int i1 = i0 + 1;
                int i2 = i0 + (cols + 1);
                int i3 = i2 + 1;

                tris[ti++] = i0; tris[ti++] = i1; tris[ti++] = i2;
                tris[ti++] = i1; tris[ti++] = i3; tris[ti++] = i2;
            }
        }

        Mesh clothMesh = new Mesh();
        clothMesh.name = goName + "_SoftMesh";
        clothMesh.MarkDynamic();
        clothMesh.vertices = verts;
        clothMesh.uv = uvs;
        clothMesh.triangles = tris;
        runtimeMeshes.Add(clothMesh);

        go.AddComponent<MeshFilter>().sharedMesh = clothMesh;

        Texture2D clothTex = CreateSoftClothGradientTexture(topCol, botCol, trimCol);
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material mat = new Material(sh);
        mat.mainTexture = clothTex;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", clothTex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.08f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.32f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", botCol * emissiveStrength);
        }
        runtimeMaterials.Add(mat);

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        activeSoftCloth = new SoftClothSurface
        {
            mesh = clothMesh,
            cols = cols,
            rows = rows,
            topHalfWidth = topHalfW,
            botHalfWidth = botHalfW,
            length = length,
            topY = topY,
            topZ = topZ,
            wrapAngleDeg = wrapDeg,
            tatterAmp = tatterAmp,
            waveSpeed = waveSpeed,
            waveAmp = waveAmp,
            verts = verts,
            phaseOffset = Random.Range(0f, 6.28f)
        };

        UpdateSoftClothMesh(activeSoftCloth);
    }

    Texture2D CreateSoftClothGradientTexture(Color topCol, Color botCol, Color trimCol)
    {
        const int w = 128;
        const int h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < h; y++)
        {
            float v = 1f - (float)y / (h - 1);
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / (w - 1);
                float edgeDist = Mathf.Min(u, 1f - u);
                float fabric = 0.92f + 0.08f * Mathf.Sin(x * 0.9f + y * 0.45f);
                float grad = Mathf.Pow(v, 1.55f);
                Color c = Color.Lerp(topCol, botCol, grad) * fabric;

                if (v > 0.89f)
                {
                    float trimBlend = Mathf.Clamp01((v - 0.89f) / 0.11f);
                    c = Color.Lerp(c, trimCol, trimBlend * 0.82f);
                }
                else if (v > 0.70f)
                {
                    // Hoa văn Gothic viền vàng hình vòm nhọn chạy dọc gấu áo choàng như ảnh mẫu!
                    float arch = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 8f));
                    float bandV = (v - 0.70f) / 0.19f;
                    if (Mathf.Abs(bandV - arch) < 0.11f)
                    {
                        c = Color.Lerp(c, trimCol, 0.78f);
                    }
                }
                else if (edgeDist < 0.055f)
                {
                    float eBlend = 1f - (edgeDist / 0.055f);
                    c = Color.Lerp(c, trimCol, eBlend * 0.74f);
                }

                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        runtimeTextures.Add(tex);
        return tex;
    }

    // ================= BODY SUIT / SKIN TONE MATERIALS =================

    void ApplyBodySuitMaterial(SkinDefinition def)
    {
        if (bodyMeshRenderer == null)
            return;

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        // Luôn khôi phục mesh gốc trước khi quyết định dùng dáng nữ mượt hay mannequin giáp nam!
        if (originalBodyMesh != null)
        {
            bodyMeshRenderer.sharedMesh = originalBodyMesh;
        }

        if (def.isFemale)
        {
            // THAY THẾ hoàn toàn mesh robot "khúc xương" bằng thân hình nữ SkinnedMesh mềm mại, liền mạch, uốn lượn tự nhiên!
            Mesh smoothFemaleMesh = SmoothFemaleBodyBuilder.GetOrCreate(bodyMeshRenderer);
            if (smoothFemaleMesh != null)
            {
                bodyMeshRenderer.sharedMesh = smoothFemaleMesh;
            }

            Color eyeCol = (def.type == SkinType.VoidReaper)
                ? new Color(0.68f, 0.26f, 0.96f, 1f) // Mắt tím thạch anh quyến rũ cho Nữ Tử Thần
                : new Color(0.18f, 0.72f, 1.00f, 1f); // Mắt xanh lôi điện cho Nữ Valkyrie

            Texture2D softSkinTex = SmoothFemaleBodyBuilder.CreateSoftFemaleSkinTexture(def.skinToneColor, eyeCol);
            runtimeTextures.Add(softSkinTex);

            Material skinMat = new Material(urpLit);
            skinMat.name = "SmoothFemaleSkinMat_" + def.type;
            skinMat.mainTexture = softSkinTex;
            if (skinMat.HasProperty("_BaseMap")) skinMat.SetTexture("_BaseMap", softSkinTex);
            if (skinMat.HasProperty("_BaseColor")) skinMat.SetColor("_BaseColor", Color.white);
            if (skinMat.HasProperty("_Color")) skinMat.SetColor("_Color", Color.white);
            if (skinMat.HasProperty("_Metallic")) skinMat.SetFloat("_Metallic", 0.01f);
            if (skinMat.HasProperty("_Smoothness")) skinMat.SetFloat("_Smoothness", 0.26f);
            if (skinMat.HasProperty("_Cull")) skinMat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            if (skinMat.HasProperty("_EmissionColor"))
            {
                skinMat.EnableKeyword("_EMISSION");
                skinMat.SetColor("_EmissionColor", def.skinToneColor * 0.09f);
            }
            runtimeMaterials.Add(skinMat);
            bodyMeshRenderer.sharedMaterials = new Material[] { skinMat };
            return;
        }

        Texture2D patternTex = CreateSuitPatternTexture(def);
        int count = Mathf.Max(1, originalBodyMaterials != null ? originalBodyMaterials.Length : 1);
        Material[] mats = new Material[count];

        bool showNaturalSkinBase = def.type == SkinType.DefaultStriker;

        for (int i = 0; i < count; i++)
        {
            Material m = new Material(urpLit);
            m.name = "SkinSuitMat_" + def.type + "_" + i;

            Color col;
            float met;
            float sm;
            Color em;

            if (showNaturalSkinBase)
            {
                // Với Nữ Tử Thần & Nữ Valkyrie: tô mượt toàn thân/khớp chân bằng màu da trắng tự nhiên
                // để đôi chân dài trắng mượt mà, không bị lộ khớp đen của mannequin!
                bool smoothFullSkin = def.isFemale;
                if (i == 0 || smoothFullSkin)
                {
                    col = def.skinToneColor;
                    met = 0.02f;
                    sm = 0.34f;
                    em = Color.black;
                }
                else if (i == 1)
                {
                    col = def.suitBaseColor;
                    met = def.suitMetallic * 0.6f;
                    sm = def.suitSmoothness * 0.8f;
                    em = def.emissionColor * 0.12f;
                }
                else
                {
                    col = def.suitAccentColor;
                    met = 0.40f;
                    sm = 0.88f;
                    em = def.suitAccentColor * 0.55f;
                }
            }
            else
            {
                if (i == 0)
                {
                    col = Color.white;
                    m.mainTexture = patternTex;
                    if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", patternTex);
                    m.mainTextureScale = new Vector2(4f, 4f);
                    met = def.suitMetallic;
                    sm = def.suitSmoothness;
                    em = def.emissionColor * 0.22f;
                }
                else if (i == 1)
                {
                    col = def.suitSecondaryColor;
                    met = Mathf.Clamp01(def.suitMetallic + 0.10f);
                    sm = Mathf.Clamp01(def.suitSmoothness + 0.05f);
                    em = def.emissionColor * 0.16f;
                }
                else
                {
                    col = def.suitAccentColor;
                    met = 0.45f;
                    sm = 0.94f;
                    em = def.suitAccentColor * 0.95f;
                }
            }

            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            if (m.HasProperty("_Color")) m.SetColor("_Color", col);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", met);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", sm);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", em);
            }

            runtimeMaterials.Add(m);
            mats[i] = m;
        }
        bodyMeshRenderer.sharedMaterials = mats;
    }

    Texture2D CreateSuitPatternTexture(SkinDefinition def)
    {
        const int sz = 128;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        Color cBase = def.suitBaseColor;
        Color cSec = def.suitSecondaryColor;
        Color cAcc = def.suitAccentColor;

        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                Color c = cBase;
                if (def.type == SkinType.CyberRunner)
                {
                    bool hex = ((x / 16 + y / 16) % 2 == 0);
                    c = hex ? cBase : Color.Lerp(cBase, cSec, 0.50f);
                    if (x % 32 < 2 || y % 32 < 2) c = Color.Lerp(c, cAcc, 0.65f);
                }
                else if (def.type == SkinType.CrimsonSamurai)
                {
                    bool stripe = ((x + y) % 16 < 3);
                    c = stripe ? Color.Lerp(cBase, cSec, 0.65f) : cBase;
                    if (y % 48 == 0) c = Color.Lerp(c, cAcc, 0.55f);
                }
                else
                {
                    bool panel = (x % 32 > 3 && y % 32 > 3);
                    c = panel ? cBase : Color.Lerp(cBase, cSec, 0.55f);
                    if (x % 32 == 2 || y % 32 == 2) c = Color.Lerp(c, cAcc, 0.52f);
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        runtimeTextures.Add(tex);
        return tex;
    }

    GameObject SpawnSkinAccessory(Transform bone, string resourceName, SkinDefinition def, string goName)
    {
        GameObject prefab = Resources.Load<GameObject>("Skins/" + resourceName);
        GameObject root = new GameObject(goName);
        root.transform.SetParent(bone, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        spawnedOutfitParts.Add(root);

        if (prefab != null)
        {
            GameObject inst = Instantiate(prefab, root.transform, false);
            inst.name = resourceName;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            Collider[] cols = inst.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++) Destroy(cols[i]);

            UpgradeAccessoryMaterials(inst, def);
        }

        return root;
    }

    void UpgradeAccessoryMaterials(GameObject inst, SkinDefinition def)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        MeshRenderer[] renderers = inst.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer mr in renderers)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            Material[] srcMats = mr.sharedMaterials;
            Material[] dstMats = new Material[Mathf.Max(1, srcMats.Length)];
            string objName = mr.gameObject.name;

            for (int i = 0; i < dstMats.Length; i++)
            {
                string matName = (i < srcMats.Length && srcMats[i] != null) ? srcMats[i].name : objName;
                bool isEmissive = matName.Contains("Emissive") || objName.Contains("Emissive");
                bool isTrim = matName.Contains("Trim") || objName.Contains("Trim");
                bool isHair = matName.Contains("Hair") || objName.Contains("Hair");

                // Giữ màu nguyên bản từ file .mtl nếu có (tóc bạch kim Nữ Tử Thần, viền vàng cổ điển, tóc bạc Valkyrie...)
                Color srcCol = def.suitBaseColor;
                if (i < srcMats.Length && srcMats[i] != null)
                {
                    if (srcMats[i].HasProperty("_BaseColor")) srcCol = srcMats[i].GetColor("_BaseColor");
                    else if (srcMats[i].HasProperty("_Color")) srcCol = srcMats[i].GetColor("_Color");
                }

                Color col;
                float met;
                float sm;
                Color em;

                if (isEmissive)
                {
                    col = def.suitAccentColor;
                    met = 0.22f;
                    sm = 0.96f;
                    em = def.suitAccentColor * (def.type == SkinType.VoidReaper ? 2.10f : 1.65f);
                }
                else if (isHair)
                {
                    // Tóc bạch kim / vàng / đen mềm mượt, không bị ám màu tím neon
                    col = srcCol;
                    met = 0.08f;
                    sm = 0.48f;
                    em = srcCol * 0.05f;
                }
                else if (isTrim)
                {
                    // Giữ đúng ánh kim nguyên bản (Vàng cổ điển Champagne Gold cho Nữ Tử Thần, Vàng Samurai/Paladin, Bạc Valkyrie)
                    bool keepPureTrim = def.type == SkinType.VoidReaper || def.type == SkinType.SolarPaladin || def.type == SkinType.CrimsonSamurai;
                    col = keepPureTrim ? srcCol : Color.Lerp(srcCol, def.suitAccentColor, 0.28f);
                    met = 0.86f;
                    sm = 0.90f;
                    em = keepPureTrim ? (srcCol * 0.22f) : (def.suitAccentColor * 0.26f);
                }
                else
                {
                    col = srcCol;
                    bool isSoftMat = def.type == SkinType.VoidReaper || def.type == SkinType.DefaultStriker || def.type == SkinType.CrimsonSamurai;
                    met = isSoftMat ? 0.14f : def.suitMetallic * 0.75f;
                    sm = isSoftMat ? 0.42f : def.suitSmoothness * 0.85f;
                    em = def.emissionColor * 0.10f;
                }

                dstMats[i] = CreateArmorMat(urpLit, col, met, sm, em);
            }
            mr.sharedMaterials = dstMats;
        }
    }

    Material CreateArmorMat(Shader sh, Color baseCol, float metallic, float smoothness, Color emission)
    {
        Material m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseCol);
        if (m.HasProperty("_Color")) m.SetColor("_Color", baseCol);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Cull"))
        {
            m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        }
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
        }
        runtimeMaterials.Add(m);
        return m;
    }

    void SpawnSkinAuraParticles(Transform parent, SkinDefinition def)
    {
        GameObject psGo = new GameObject("SkinAuraParticles");
        psGo.transform.SetParent(parent, false);
        psGo.transform.localPosition = new Vector3(0f, -0.35f, -0.24f);
        spawnedOutfitParts.Add(psGo);

        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        bool isReaper = def.type == SkinType.VoidReaper;
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.90f);
        main.startSize = isReaper
            ? new ParticleSystem.MinMaxCurve(0.15f, 0.30f)
            : new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.gravityModifier = isReaper ? -0.40f : -0.20f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = isReaper
            ? new ParticleSystem.MinMaxGradient(new Color(0.74f, 0.20f, 1.00f, 0.80f), new Color(0.06f, 0.01f, 0.16f, 0.88f))
            : new ParticleSystem.MinMaxGradient(
                new Color(def.suitAccentColor.r, def.suitAccentColor.g, def.suitAccentColor.b, 0.84f),
                new Color(def.emissionColor.r, def.emissionColor.g, def.emissionColor.b, 0.56f)
              );

        var emit = ps.emission;
        emit.rateOverTime = isReaper ? 24f : (def.type == SkinType.StormValkyrie || def.type == SkinType.SolarPaladin ? 16f : 10f);

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = isReaper ? 0.62f : 0.48f;

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0f))
        );

        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        Material pMat = new Material(sh);
        if (pMat.HasProperty("_Surface")) pMat.SetFloat("_Surface", 1f);
        if (pMat.HasProperty("_Blend")) pMat.SetFloat("_Blend", isReaper ? 0f : 2f);
        if (pMat.HasProperty("_SrcBlend")) pMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (pMat.HasProperty("_DstBlend")) pMat.SetFloat("_DstBlend", (float)(isReaper ? UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : UnityEngine.Rendering.BlendMode.One));
        if (pMat.HasProperty("_ZWrite")) pMat.SetFloat("_ZWrite", 0f);
        Texture2D glowTex = ParrySlashVFX.GetSoftGlowTexture();
        pMat.mainTexture = glowTex;
        if (pMat.HasProperty("_BaseMap")) pMat.SetTexture("_BaseMap", glowTex);
        pMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        pMat.renderQueue = 3055;
        runtimeMaterials.Add(pMat);

        ParticleSystemRenderer psr = psGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.sharedMaterial = pMat;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
        }

        ps.Play();
        ambientAuraPs = ps;
    }

    public void SetStealthHidden(bool hidden)
    {
        isStealthHidden = hidden;
        if (ambientAuraPs == null) return;
        if (hidden)
        {
            ambientAuraPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        else if (!ambientAuraPs.isPlaying)
        {
            ambientAuraPs.Play();
        }
    }

    void ClearSkinAccessories()
    {
        articulatedWingL = null;
        articulatedWingR = null;
        wingsAreCyber = false;
        valkyrieHaloOuter = null;
        valkyrieHaloInner = null;
        valkyrieSigilDisc = null;
        valkyrieHaloArcs = null;
        activeSoftCloth = null;
        ambientAuraPs = null;

        for (int i = 0; i < spawnedOutfitParts.Count; i++)
        {
            if (spawnedOutfitParts[i] != null) Destroy(spawnedOutfitParts[i]);
        }
        spawnedOutfitParts.Clear();
        CleanupRuntimeAssets();
    }

    void CleanupRuntimeAssets()
    {
        for (int i = 0; i < runtimeMaterials.Count; i++)
        {
            if (runtimeMaterials[i] != null) Destroy(runtimeMaterials[i]);
        }
        runtimeMaterials.Clear();
        for (int i = 0; i < runtimeTextures.Count; i++)
        {
            if (runtimeTextures[i] != null) Destroy(runtimeTextures[i]);
        }
        runtimeTextures.Clear();
        for (int i = 0; i < runtimeMeshes.Count; i++)
        {
            if (runtimeMeshes[i] != null) Destroy(runtimeMeshes[i]);
        }
        runtimeMeshes.Clear();
    }
}
