using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ArenaMapType
{
    ClassicNight = 0, // Dau Truong Nui Lua (Volcano / Magma - hop voi Cau Lua!)
    CyberTech = 1,    // Dau Truong Cong Nghe (Blue-Black Tech - noi bat Ball Cong Nghe Cyan!)
    ThunderStorm = 2  // Khu Vuc Bao Set (Cot trang + Set tim + Ball Dien Tim!)
}

public class ArenaMapManager : MonoBehaviour
{
    public static ArenaMapManager Instance;
    public static ArenaMapType CurrentMapType { get; private set; } = ArenaMapType.ClassicNight;
    public static Vector3 ArenaCenter { get; private set; } = Vector3.zero;
    public const float ArenaPlayableRadius = 29.6f;

    ArenaMapType appliedMap = (ArenaMapType)(-1);
    GameObject themeRoot;
    readonly List<Material> runtimeMaterials = new List<Material>();
    readonly List<Texture2D> runtimeTextures = new List<Texture2D>();
    readonly List<Renderer> hiddenClassicRenderers = new List<Renderer>();
    Material arenaBarrierVisualMat;

    // Animated environment references
    Transform cyberOrbitalRing;
    Transform cyberCenterRing;
    readonly List<Transform> cyberBeacons = new List<Transform>();

    // Volcano animated magma pulse
    Material volcanoLavaMat;
    Transform volcanoCenterRing;

    // ThunderStorm environment references
    Light mainDirectionalLight;
    float baseDirectionalIntensity = 1.0f;
    Color baseDirectionalColor = Color.white;
    Color baseAmbientLight;
    bool baseFogEnabled;
    Color baseFogColor;
    float baseFogDensity;
    bool savedLightingDefaults;

    float ambientLightningTimer = 1.6f;
    float flashDecay;
    readonly List<Vector3> stormTowerTops = new List<Vector3>();
    Material stormBoltMat;
    Material stormBoltGlowMat;

    // Map Banner UI
    GameObject mapBannerGo;
    CanvasGroup mapBannerGroup;
    Coroutine bannerRoutine;

    public static ArenaMapManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("ArenaMapManager");
        Instance = go.AddComponent<ArenaMapManager>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ArenaCenter = FindArenaCenter();
    }

    public static Vector3 ClampPositionToArena(Vector3 worldPos)
    {
        Vector3 c = ArenaCenter;
        float dx = worldPos.x - c.x;
        float dz = worldPos.z - c.z;
        float distSq = dx * dx + dz * dz;
        float maxR = ArenaPlayableRadius;
        if (distSq > maxR * maxR)
        {
            float inv = maxR / Mathf.Sqrt(distSq);
            worldPos.x = c.x + dx * inv;
            worldPos.z = c.z + dz * inv;
        }
        return worldPos;
    }

    void OnDestroy()
    {
        RestoreClassicVisuals();
        CleanupThemeAssets();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (appliedMap == ArenaMapType.ClassicNight)
        {
            if (volcanoCenterRing != null)
            {
                volcanoCenterRing.Rotate(0f, 10f * Time.deltaTime, 0f, Space.Self);
            }
            if (volcanoLavaMat != null && volcanoLavaMat.HasProperty("_EmissionColor"))
            {
                float pulse = 0.56f + 0.14f * Mathf.Sin(Time.time * 2.0f);
                volcanoLavaMat.SetColor("_EmissionColor", new Color(0.85f, 0.20f, 0.02f, 1f) * pulse);
            }
        }
        else if (appliedMap == ArenaMapType.CyberTech)
        {
            if (cyberOrbitalRing != null)
                cyberOrbitalRing.Rotate(0f, 7.5f * Time.deltaTime, 0f, Space.Self);
            if (cyberCenterRing != null)
                cyberCenterRing.Rotate(0f, -22f * Time.deltaTime, 0f, Space.Self);

            for (int i = 0; i < cyberBeacons.Count; i++)
            {
                if (cyberBeacons[i] != null)
                {
                    Vector3 p = cyberBeacons[i].localPosition;
                    p.y = 7.4f + Mathf.Sin(Time.time * 2.2f + i * 0.6f) * 0.32f;
                    cyberBeacons[i].localPosition = p;
                    cyberBeacons[i].Rotate(0f, 48f * Time.deltaTime, 0f, Space.Self);
                }
            }
        }
        else if (appliedMap == ArenaMapType.ThunderStorm)
        {
            UpdateThunderStormAmbience();
        }

        if (arenaBarrierVisualMat != null)
        {
            arenaBarrierVisualMat.mainTextureOffset = new Vector2(Time.time * 0.14f, Time.time * 0.22f);
        }
    }

    public static string GetMapDisplayName(ArenaMapType type)
    {
        switch (type)
        {
            case ArenaMapType.CyberTech: return "DAU TRUONG CONG NGHE";
            case ArenaMapType.ThunderStorm: return "KHU VUC BAO SET";
            default: return "DAU TRUONG NUI LUA";
        }
    }

    public static string GetMapSubtitle(ArenaMapType type)
    {
        switch (type)
        {
            case ArenaMapType.CyberTech: return "BALL CONG NGHE PLASMA  •  DAU TRUONG BLUE-BLACK CYBER";
            case ArenaMapType.ThunderStorm: return "BALL DIEN TIM  •  SET TIM DANH XUONG KHIEU BONG TO HON!";
            default: return "CAU LUA DUNG NHAM  •  DAU TRUONG NUI LUA PHUN TRAO";
        }
    }

    public static Color GetMapAccentColor(ArenaMapType type)
    {
        switch (type)
        {
            case ArenaMapType.CyberTech: return new Color(0.14f, 0.72f, 1.00f, 1f);
            case ArenaMapType.ThunderStorm: return new Color(0.76f, 0.34f, 1.00f, 1f);
            default: return new Color(1.00f, 0.44f, 0.10f, 1f);
        }
    }

    public void ApplyMap(ArenaMapType mapType)
    {
        CurrentMapType = mapType;
        if (appliedMap == mapType)
            return;

        appliedMap = mapType;
        CacheDefaultLighting();
        RestoreClassicVisuals();
        CleanupThemeAssets();

        Vector3 center = FindArenaCenter();
        ArenaCenter = center;

        // Ẩn mesh map cũ (giữ nguyên 100% collider mặt sàn gốc để vật lý & di chuyển luôn chuẩn!)
        HideClassicMapRenderers();

        switch (mapType)
        {
            case ArenaMapType.CyberTech:
                BuildCyberTechArena(center);
                break;

            case ArenaMapType.ThunderStorm:
                BuildThunderStormArena(center);
                break;

            default:
                BuildVolcanoLavaArena(center);
                break;
        }

        ShowMapAnnouncementBanner(mapType);
        GameAudioManager.PlayMapMusic(mapType);
    }

    Vector3 FindArenaCenter()
    {
        GameObject mc = GameObject.Find("MapCenter");
        if (mc != null)
        {
            return new Vector3(mc.transform.position.x, 0f, mc.transform.position.z);
        }
        return Vector3.zero;
    }

    void CacheDefaultLighting()
    {
        if (savedLightingDefaults)
            return;

        savedLightingDefaults = true;
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in lights)
        {
            if (l != null && l.type == LightType.Directional)
            {
                mainDirectionalLight = l;
                baseDirectionalIntensity = l.intensity;
                baseDirectionalColor = l.color;
                break;
            }
        }
        baseAmbientLight = RenderSettings.ambientLight;
        baseFogEnabled = RenderSettings.fog;
        baseFogColor = RenderSettings.fogColor;
        baseFogDensity = RenderSettings.fogDensity;
    }

    void HideClassicMapRenderers()
    {
        hiddenClassicRenderers.Clear();
        string[] names = { "MapCombined", "Night_Plane" };
        foreach (string n in names)
        {
            GameObject go = GameObject.Find(n);
            if (go == null) continue;
            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in rends)
            {
                if (r != null && r.enabled)
                {
                    r.enabled = false;
                    hiddenClassicRenderers.Add(r);
                }
            }
        }
    }

    void RestoreClassicVisuals()
    {
        for (int i = 0; i < hiddenClassicRenderers.Count; i++)
        {
            if (hiddenClassicRenderers[i] != null)
            {
                hiddenClassicRenderers[i].enabled = true;
            }
        }
        hiddenClassicRenderers.Clear();
    }

    void CleanupThemeAssets()
    {
        cyberOrbitalRing = null;
        cyberCenterRing = null;
        cyberBeacons.Clear();
        volcanoLavaMat = null;
        volcanoCenterRing = null;
        stormTowerTops.Clear();
        stormBoltMat = null;
        stormBoltGlowMat = null;
        arenaBarrierVisualMat = null;

        if (themeRoot != null)
        {
            Destroy(themeRoot);
            themeRoot = null;
        }
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
    }

    // =========================================================================
    // MAP 0: DAU TRUONG NUI LUA & DUNG NHAM (VOLCANO LAVA ARENA - HOP CAU LUA!)
    // =========================================================================

    void BuildVolcanoLavaArena(Vector3 center)
    {
        themeRoot = new GameObject("ArenaTheme_VolcanoLava");
        themeRoot.transform.position = center;

        // Giảm độ sáng tổng thể để Cầu Lửa, hiệu ứng chém & ngoại trang nổi bật cực rõ!
        if (mainDirectionalLight != null)
        {
            mainDirectionalLight.color = new Color(0.92f, 0.62f, 0.42f, 1f);
            mainDirectionalLight.intensity = 0.68f;
        }
        RenderSettings.ambientLight = new Color(0.10f, 0.045f, 0.035f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.045f, 0.015f, 0.010f, 1f);
        RenderSettings.fogDensity = 0.0092f;

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = new Color(0.035f, 0.012f, 0.008f, 1f);
        }

        // 1. Sàn Đá Hắc Diện Thạch tối sâu với khe nứt dung nham mảnh tinh tế (giữ nguyên collider sàn gốc!)
        Texture2D magmaFloorTex = CreateVolcanoFloorTexture();
        Material floorMat = CreateLitMaterial(
            new Color(0.76f, 0.72f, 0.70f, 1f),
            0.22f,
            0.38f,
            new Color(0.16f, 0.035f, 0.006f, 1f)
        );
        floorMat.mainTexture = magmaFloorTex;
        if (floorMat.HasProperty("_BaseMap")) floorMat.SetTexture("_BaseMap", magmaFloorTex);
        floorMat.mainTextureScale = new Vector2(8f, 8f);

        GameObject floorDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        floorDisc.name = "VolcanoArenaFloor";
        floorDisc.transform.SetParent(themeRoot.transform, false);
        floorDisc.transform.localPosition = new Vector3(0f, -0.04f, 0f);
        floorDisc.transform.localScale = new Vector3(66f, 0.08f, 66f);
        Destroy(floorDisc.GetComponent<Collider>());
        floorDisc.GetComponent<Renderer>().sharedMaterial = floorMat;

        // 2. Biển Dung Nham đỏ thẫm dịu mắt bao quanh đấu trường (không quá chói!)
        Texture2D lavaLakeTex = CreateMoltenLavaTexture();
        volcanoLavaMat = CreateLitMaterial(
            new Color(0.55f, 0.14f, 0.02f, 1f),
            0.05f,
            0.62f,
            new Color(0.85f, 0.20f, 0.02f, 1f) * 0.58f
        );
        volcanoLavaMat.mainTexture = lavaLakeTex;
        if (volcanoLavaMat.HasProperty("_BaseMap")) volcanoLavaMat.SetTexture("_BaseMap", lavaLakeTex);
        volcanoLavaMat.mainTextureScale = new Vector2(14f, 14f);

        GameObject lavaSea = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lavaSea.name = "MoltenLavaLake";
        lavaSea.transform.SetParent(themeRoot.transform, false);
        lavaSea.transform.localPosition = new Vector3(0f, -0.45f, 0f);
        lavaSea.transform.localScale = new Vector3(210f, 0.14f, 210f);
        Destroy(lavaSea.GetComponent<Collider>());
        lavaSea.GetComponent<Renderer>().sharedMaterial = volcanoLavaMat;

        // Đường viền nham thạch mảnh gọn tạo bố cục đấu trường rõ nét
        Material magmaGlowMat = CreateUnlitAdditiveMaterial(new Color(0.88f, 0.28f, 0.04f, 0.62f));
        Material emberGoldMat = CreateUnlitAdditiveMaterial(new Color(0.95f, 0.46f, 0.08f, 0.58f));
        CreateFloorNeonRing(themeRoot.transform, "MagmaCenterRing", 5.2f, 0.22f, 0.07f, emberGoldMat);
        CreateFloorNeonRing(themeRoot.transform, "MagmaMidRing", 17.5f, 0.18f, 0.06f, magmaGlowMat);
        CreateFloorNeonRing(themeRoot.transform, "MagmaRimRing", 31.5f, 0.36f, 0.08f, magmaGlowMat);

        // 3. Dãy 14 Ngọn Núi Lửa dáng mượt sạch (ĐÃ BỎ HẾT các cục đá vuông/tròn thừa thãi!)
        Material obsidianRockMat = CreateLitMaterial(new Color(0.06f, 0.045f, 0.05f, 1f), 0.18f, 0.25f, new Color(0.03f, 0.008f, 0.003f, 1f));
        Material volcanicSlopeMat = CreateLitMaterial(new Color(0.085f, 0.055f, 0.055f, 1f), 0.12f, 0.22f, new Color(0.05f, 0.012f, 0.004f, 1f));

        Mesh volcanoConeMesh = BuildVolcanoPeakMesh();
        int peakCount = 14;
        for (int i = 0; i < peakCount; i++)
        {
            float ang = (i / (float)peakCount) * Mathf.PI * 2f + 0.12f;
            float dist = (i % 2 == 0) ? 50f : 60f;
            float peakHeight = (i % 3 == 0) ? 25f : ((i % 2 == 0) ? 18f : 13.5f);
            float baseRadius = peakHeight * 0.88f;

            GameObject peakGo = new GameObject("VolcanoMountain_" + i);
            peakGo.transform.SetParent(themeRoot.transform, false);
            peakGo.transform.localPosition = new Vector3(Mathf.Cos(ang) * dist, -0.5f, Mathf.Sin(ang) * dist);
            peakGo.transform.localRotation = Quaternion.Euler(0f, -ang * Mathf.Rad2Deg + 90f, 0f);

            GameObject body = new GameObject("Cone");
            body.transform.SetParent(peakGo.transform, false);
            body.transform.localScale = new Vector3(baseRadius, peakHeight, baseRadius);
            body.AddComponent<MeshFilter>().sharedMesh = volcanoConeMesh;
            MeshRenderer mr = body.AddComponent<MeshRenderer>();
            mr.sharedMaterial = (i % 2 == 0) ? volcanicSlopeMat : obsidianRockMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // 4. Tàn lửa nhỏ dịu mắt bay bốc nhẹ lên từ biển dung nham xung quanh
        CreateAmbientParticles(
            themeRoot.transform,
            "VolcanoRisingEmbers",
            new Vector3(0f, 0.6f, 0f),
            34f,
            30f,
            new Color(1.00f, 0.56f, 0.12f, 0.80f),
            new Color(0.92f, 0.18f, 0.03f, 0.60f),
            0.10f,
            0.24f,
            -0.34f,
            1.5f
        );

        // 5. Giới hạn đấu trường (Tường vật lý + Viền rào chắn nhiệt dung nham để không đi ra ngoài!)
        BuildArenaBoundaryBarrier(themeRoot.transform, 30.2f, new Color(1.00f, 0.36f, 0.06f, 0.48f), false);
    }

    Mesh BuildVolcanoPeakMesh()
    {
        // Truncated jagged volcano cone (radius 1.0 at bottom -> 0.24 open crater rim at top, y = 0..1)
        const int segs = 12;
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        for (int i = 0; i < segs; i++)
        {
            float a = (i / (float)segs) * Mathf.PI * 2f;
            float jagB = 0.88f + 0.24f * Mathf.Sin(i * 3.7f);
            float jagT = 0.20f + 0.06f * Mathf.Cos(i * 2.9f);
            float topY = 0.95f + 0.08f * Mathf.Sin(i * 4.1f);
            verts.Add(new Vector3(Mathf.Cos(a) * jagB, 0f, Mathf.Sin(a) * jagB));
            verts.Add(new Vector3(Mathf.Cos(a) * jagT, topY, Mathf.Sin(a) * jagT));
        }
        int rimCenter = verts.Count;
        verts.Add(new Vector3(0f, 0.84f, 0f));

        for (int i = 0; i < segs; i++)
        {
            int next = (i + 1) % segs;
            int b0 = i * 2;
            int t0 = i * 2 + 1;
            int b1 = next * 2;
            int t1 = next * 2 + 1;

            // Outward-facing side quad
            tris.Add(b0); tris.Add(t0); tris.Add(b1);
            tris.Add(b1); tris.Add(t0); tris.Add(t1);

            // Inner crater cap
            tris.Add(t0); tris.Add(rimCenter); tris.Add(t1);
        }

        Mesh mesh = new Mesh();
        mesh.name = "VolcanoPeakMesh";
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // =========================================================================
    // MAP 1: DAU TRUONG CONG NGHE (BLUE-BLACK CYBER TECH — NOI BAT BALL CYAN!)
    // =========================================================================

    void BuildCyberTechArena(Vector3 center)
    {
        themeRoot = new GameObject("ArenaTheme_CyberTech");
        themeRoot.transform.position = center;

        // Tone Blue-Black sâu lắng & sắc sảo để Ball Công Nghệ Cyan cực kỳ nổi bật!
        if (mainDirectionalLight != null)
        {
            mainDirectionalLight.color = new Color(0.46f, 0.64f, 0.96f, 1f);
            mainDirectionalLight.intensity = 0.92f;
        }
        RenderSettings.ambientLight = new Color(0.05f, 0.09f, 0.18f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.015f, 0.030f, 0.075f, 1f);
        RenderSettings.fogDensity = 0.0095f;

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = new Color(0.010f, 0.022f, 0.058f, 1f);
        }

        // 1. Sàn Carbon Blue-Black ánh kim với lưới mạch xanh dương đậm tinh tế (không chói lấn át Ball!)
        Texture2D hexTex = CreateCyberBlueBlackFloorTexture();
        Material floorMat = CreateLitMaterial(
            new Color(0.86f, 0.90f, 1.00f, 1f),
            0.78f,
            0.84f,
            new Color(0.02f, 0.08f, 0.22f, 1f)
        );
        floorMat.mainTexture = hexTex;
        if (floorMat.HasProperty("_BaseMap")) floorMat.SetTexture("_BaseMap", hexTex);
        floorMat.mainTextureScale = new Vector2(12f, 12f);

        GameObject floorDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        floorDisc.name = "CyberArenaFloor";
        floorDisc.transform.SetParent(themeRoot.transform, false);
        floorDisc.transform.localPosition = new Vector3(0f, -0.04f, 0f);
        floorDisc.transform.localScale = new Vector3(68f, 0.08f, 68f);
        Destroy(floorDisc.GetComponent<Collider>());
        floorDisc.GetComponent<Renderer>().sharedMaterial = floorMat;

        Material outerDeckMat = CreateLitMaterial(new Color(0.018f, 0.028f, 0.065f, 1f), 0.65f, 0.72f, new Color(0.005f, 0.015f, 0.045f, 1f));
        GameObject outerFloor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        outerFloor.name = "CyberOuterDeck";
        outerFloor.transform.SetParent(themeRoot.transform, false);
        outerFloor.transform.localPosition = new Vector3(0f, -0.35f, 0f);
        outerFloor.transform.localScale = new Vector3(190f, 0.12f, 190f);
        Destroy(outerFloor.GetComponent<Collider>());
        outerFloor.GetComponent<Renderer>().sharedMaterial = outerDeckMat;

        // 2. Đường viền mạch Royal Blue (Xanh dương sâu) tương phản cực đẹp với Ball Cyan sáng!
        Material deepBlueLineMat = CreateUnlitAdditiveMaterial(new Color(0.10f, 0.36f, 0.92f, 0.72f));
        Material cobaltAccentMat = CreateUnlitAdditiveMaterial(new Color(0.14f, 0.50f, 1.00f, 0.82f));
        Material blueBlackSteelMat = CreateLitMaterial(new Color(0.04f, 0.06f, 0.12f, 1f), 0.90f, 0.85f, new Color(0.01f, 0.03f, 0.09f, 1f));
        Material blueLedTrimMat = CreateLitMaterial(new Color(0.12f, 0.44f, 0.98f, 1f), 0.35f, 0.95f, new Color(0.10f, 0.40f, 0.98f, 1f) * 1.65f);

        CreateFloorNeonRing(themeRoot.transform, "InnerRing", 6.5f, 0.20f, 0.08f, cobaltAccentMat);
        CreateFloorNeonRing(themeRoot.transform, "MidRing", 17.5f, 0.24f, 0.08f, deepBlueLineMat);
        CreateFloorNeonRing(themeRoot.transform, "OuterRing", 30.5f, 0.40f, 0.10f, cobaltAccentMat);

        // Tâm xoay Hologram Blue-Black
        GameObject centerRingGo = new GameObject("CyberCenterHologram");
        centerRingGo.transform.SetParent(themeRoot.transform, false);
        centerRingGo.transform.localPosition = new Vector3(0f, 0.09f, 0f);
        cyberCenterRing = centerRingGo.transform;
        for (int k = 0; k < 6; k++)
        {
            float a = k * 60f * Mathf.Deg2Rad;
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.transform.SetParent(centerRingGo.transform, false);
            seg.transform.localPosition = new Vector3(Mathf.Cos(a) * 3.2f, 0f, Mathf.Sin(a) * 3.2f);
            seg.transform.localRotation = Quaternion.Euler(0f, -k * 60f, 0f);
            seg.transform.localScale = new Vector3(0.18f, 0.06f, 2.2f);
            Destroy(seg.GetComponent<Collider>());
            seg.GetComponent<Renderer>().sharedMaterial = blueLedTrimMat;
        }

        // 3. 12 Trụ Công Nghệ Thép Đen-Xanh + Đường Laser xanh dương trầm
        int pylonCount = 12;
        float pylonRadius = 31.5f;
        Vector3[] pylonPts = new Vector3[pylonCount];

        for (int i = 0; i < pylonCount; i++)
        {
            float ang = (i / (float)pylonCount) * Mathf.PI * 2f;
            Vector3 p = new Vector3(Mathf.Cos(ang) * pylonRadius, 0f, Mathf.Sin(ang) * pylonRadius);
            pylonPts[i] = p;

            GameObject pylon = new GameObject("CyberPylon_" + i);
            pylon.transform.SetParent(themeRoot.transform, false);
            pylon.transform.localPosition = p;
            pylon.transform.localRotation = Quaternion.LookRotation(-p.normalized, Vector3.up);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(pylon.transform, false);
            body.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            body.transform.localScale = new Vector3(1.65f, 6.4f, 1.45f);
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = blueBlackSteelMat;

            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.transform.SetParent(pylon.transform, false);
            strip.transform.localPosition = new Vector3(0f, 3.3f, 0.74f);
            strip.transform.localScale = new Vector3(0.28f, 5.4f, 0.10f);
            Destroy(strip.GetComponent<Collider>());
            strip.GetComponent<Renderer>().sharedMaterial = blueLedTrimMat;

            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beacon.transform.SetParent(pylon.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 7.4f, 0f);
            beacon.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            beacon.transform.localScale = Vector3.one * 0.90f;
            Destroy(beacon.GetComponent<Collider>());
            beacon.GetComponent<Renderer>().sharedMaterial = blueLedTrimMat;
            cyberBeacons.Add(beacon.transform);

            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.transform.SetParent(pylon.transform, false);
            beam.transform.localPosition = new Vector3(0f, 17f, 0f);
            beam.transform.localScale = new Vector3(0.18f, 11f, 0.18f);
            Destroy(beam.GetComponent<Collider>());
            beam.GetComponent<Renderer>().sharedMaterial = deepBlueLineMat;
        }

        for (int row = 0; row < 2; row++)
        {
            GameObject railGo = new GameObject("CyberLaserRail_" + row);
            railGo.transform.SetParent(themeRoot.transform, false);
            LineRenderer lr = railGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = pylonCount;
            lr.startWidth = row == 0 ? 0.20f : 0.12f;
            lr.endWidth = lr.startWidth;
            lr.sharedMaterial = row == 0 ? cobaltAccentMat : deepBlueLineMat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float y = row == 0 ? 2.2f : 4.8f;
            for (int i = 0; i < pylonCount; i++)
            {
                lr.SetPosition(i, pylonPts[i] + Vector3.up * y);
            }
        }

        // 4. Vòng Quỹ Đạo Blue-Black trên cao
        GameObject orbitalGo = new GameObject("CyberOrbitalRing");
        orbitalGo.transform.SetParent(themeRoot.transform, false);
        orbitalGo.transform.localPosition = new Vector3(0f, 24f, 0f);
        cyberOrbitalRing = orbitalGo.transform;

        CreateFloorNeonRing(orbitalGo.transform, "SkyRingMain", 36f, 0.55f, 0f, deepBlueLineMat);
        for (int i = 0; i < 8; i++)
        {
            float a = (i / 8f) * Mathf.PI * 2f;
            GameObject node = GameObject.CreatePrimitive(PrimitiveType.Cube);
            node.transform.SetParent(orbitalGo.transform, false);
            node.transform.localPosition = new Vector3(Mathf.Cos(a) * 36f, 0f, Mathf.Sin(a) * 36f);
            node.transform.localScale = new Vector3(2.8f, 1.2f, 2.8f);
            Destroy(node.GetComponent<Collider>());
            node.GetComponent<Renderer>().sharedMaterial = blueBlackSteelMat;
        }

        CreateAmbientParticles(
            themeRoot.transform,
            "CyberAmbientData",
            new Vector3(0f, 0.5f, 0f),
            30f,
            22f,
            new Color(0.14f, 0.44f, 0.98f, 0.55f),
            new Color(0.08f, 0.24f, 0.72f, 0.38f),
            0.14f,
            0.30f,
            -0.22f,
            1.2f
        );

        // 5. Rào chắn giới hạn đấu trường Công Nghệ (Tường năng lượng Hologram + Collider kín 360 độ!)
        BuildArenaBoundaryBarrier(themeRoot.transform, 30.2f, new Color(0.14f, 0.68f, 1.00f, 0.62f), true);
    }

    // =========================================================================
    // MAP 2: KHU VUC BAO SET (COT TRANG + LUONG SET TIM + BALL DIEN TIM!)
    // =========================================================================

    void BuildThunderStormArena(Vector3 center)
    {
        themeRoot = new GameObject("ArenaTheme_ThunderStorm");
        themeRoot.transform.position = center;

        if (mainDirectionalLight != null)
        {
            mainDirectionalLight.color = new Color(0.68f, 0.56f, 0.96f, 1f);
            mainDirectionalLight.intensity = 0.82f;
        }
        RenderSettings.ambientLight = new Color(0.12f, 0.08f, 0.22f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.05f, 0.03f, 0.11f, 1f);
        RenderSettings.fogDensity = 0.0105f;

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = new Color(0.04f, 0.02f, 0.09f, 1f);
        }

        // 1. Sàn đá bão ẩm ướt nứt khe phát sáng ĐIỆN TÍCH TÍM nổi bật!
        Texture2D stormTex = CreateStormFloorTexture();
        Material floorMat = CreateLitMaterial(new Color(0.86f, 0.84f, 0.96f, 1f), 0.35f, 0.82f, new Color(0.24f, 0.08f, 0.44f, 1f));
        floorMat.mainTexture = stormTex;
        if (floorMat.HasProperty("_BaseMap")) floorMat.SetTexture("_BaseMap", stormTex);
        floorMat.mainTextureScale = new Vector2(8f, 8f);

        GameObject floorDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        floorDisc.name = "StormArenaFloor";
        floorDisc.transform.SetParent(themeRoot.transform, false);
        floorDisc.transform.localPosition = new Vector3(0f, -0.04f, 0f);
        floorDisc.transform.localScale = new Vector3(68f, 0.08f, 68f);
        Destroy(floorDisc.GetComponent<Collider>());
        floorDisc.GetComponent<Renderer>().sharedMaterial = floorMat;

        Material outerMat = CreateLitMaterial(new Color(0.04f, 0.03f, 0.09f, 1f), 0.2f, 0.65f, new Color(0.02f, 0.01f, 0.06f, 1f));
        GameObject outerFloor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        outerFloor.name = "StormOuterGround";
        outerFloor.transform.SetParent(themeRoot.transform, false);
        outerFloor.transform.localPosition = new Vector3(0f, -0.35f, 0f);
        outerFloor.transform.localScale = new Vector3(190f, 0.12f, 190f);
        Destroy(outerFloor.GetComponent<Collider>());
        outerFloor.GetComponent<Renderer>().sharedMaterial = outerMat;

        // Vòng điện màu tím trên sàn
        Material purpleRingMat = CreateUnlitAdditiveMaterial(new Color(0.76f, 0.32f, 1.00f, 0.90f));
        CreateFloorNeonRing(themeRoot.transform, "StormCenterRing", 5.5f, 0.26f, 0.08f, purpleRingMat);
        CreateFloorNeonRing(themeRoot.transform, "StormOuterRing", 30.5f, 0.44f, 0.10f, purpleRingMat);

        // 2. 10 Cột Thu Lôi (Tesla Conductor Towers) MÀU TRẮNG SÁNG nổi bật tương phản với luồng Sét Tím!
        Material whiteSteelMat = CreateLitMaterial(new Color(0.84f, 0.87f, 0.94f, 1f), 0.72f, 0.82f, new Color(0.10f, 0.10f, 0.14f, 1f));
        Material whiteCoilGlowMat = CreateLitMaterial(new Color(0.96f, 0.97f, 1.00f, 1f), 0.30f, 0.96f, new Color(0.88f, 0.90f, 1.00f, 1f) * 1.8f);

        int towerCount = 10;
        float towerRadius = 32f;
        stormTowerTops.Clear();

        for (int i = 0; i < towerCount; i++)
        {
            float ang = (i / (float)towerCount) * Mathf.PI * 2f;
            Vector3 p = new Vector3(Mathf.Cos(ang) * towerRadius, 0f, Mathf.Sin(ang) * towerRadius);

            GameObject tower = new GameObject("StormTeslaTower_" + i);
            tower.transform.SetParent(themeRoot.transform, false);
            tower.transform.localPosition = p;

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(tower.transform, false);
            pole.transform.localPosition = new Vector3(0f, 4.8f, 0f);
            pole.transform.localScale = new Vector3(0.75f, 4.8f, 0.75f);
            Destroy(pole.GetComponent<Collider>());
            pole.GetComponent<Renderer>().sharedMaterial = whiteSteelMat;

            for (int r = 0; r < 3; r++)
            {
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.transform.SetParent(tower.transform, false);
                ring.transform.localPosition = new Vector3(0f, 6.2f + r * 1.15f, 0f);
                float w = 2.0f - r * 0.35f;
                ring.transform.localScale = new Vector3(w, 0.16f, w);
                Destroy(ring.GetComponent<Collider>());
                ring.GetComponent<Renderer>().sharedMaterial = whiteCoilGlowMat;
            }

            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.transform.SetParent(tower.transform, false);
            orb.transform.localPosition = new Vector3(0f, 9.8f, 0f);
            orb.transform.localScale = Vector3.one * 1.18f;
            Destroy(orb.GetComponent<Collider>());
            orb.GetComponent<Renderer>().sharedMaterial = whiteCoilGlowMat;

            stormTowerTops.Add(center + p + Vector3.up * 9.8f);
        }

        // 3. Tầng mây giông tím đen u ám trên bầu trời
        Material cloudMat = CreateLitMaterial(new Color(0.09f, 0.06f, 0.16f, 1f), 0.05f, 0.12f, new Color(0.06f, 0.03f, 0.14f, 1f));
        for (int c = 0; c < 18; c++)
        {
            float a = (c / 18f) * Mathf.PI * 2f;
            float dist = (c % 2 == 0) ? 28f : 44f;
            GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cloud.name = "StormCloud_" + c;
            cloud.transform.SetParent(themeRoot.transform, false);
            cloud.transform.localPosition = new Vector3(Mathf.Cos(a) * dist, 30f + (c % 3) * 3.5f, Mathf.Sin(a) * dist);
            cloud.transform.localScale = new Vector3(24f + (c % 4) * 5f, 7f, 20f + (c % 3) * 4f);
            Destroy(cloud.GetComponent<Collider>());
            cloud.GetComponent<Renderer>().sharedMaterial = cloudMat;
        }

        // 4. Hiệu ứng Mưa Bão rơi chéo toàn đấu trường
        GameObject rainGo = new GameObject("StormRainPS");
        rainGo.transform.SetParent(themeRoot.transform, false);
        rainGo.transform.localPosition = new Vector3(0f, 28f, 0f);
        rainGo.transform.localRotation = Quaternion.Euler(102f, 15f, 0f);

        ParticleSystem rainPs = rainGo.AddComponent<ParticleSystem>();
        rainPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var rMain = rainPs.main;
        rMain.loop = true;
        rMain.playOnAwake = false;
        rMain.startLifetime = 1.25f;
        rMain.startSpeed = 30f;
        rMain.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        rMain.maxParticles = 600;
        rMain.simulationSpace = ParticleSystemSimulationSpace.World;
        rMain.startColor = new Color(0.80f, 0.72f, 1.00f, 0.40f);

        var rEmit = rainPs.emission;
        rEmit.rateOverTime = 240f;

        var rShape = rainPs.shape;
        rShape.enabled = true;
        rShape.shapeType = ParticleSystemShapeType.Box;
        rShape.scale = new Vector3(62f, 62f, 2f);

        Material rainMat = CreateUnlitAdditiveMaterial(new Color(0.82f, 0.70f, 1.00f, 0.50f));
        ParticleSystemRenderer rainPsr = rainGo.GetComponent<ParticleSystemRenderer>();
        if (rainPsr != null)
        {
            rainPsr.renderMode = ParticleSystemRenderMode.Stretch;
            rainPsr.lengthScale = 12f;
            rainPsr.velocityScale = 0.04f;
            rainPsr.sharedMaterial = rainMat;
            rainPsr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rainPsr.receiveShadows = false;
        }
        rainPs.Play();

        // Vật liệu luồng SÉT MÀU TÍM RỰC RỠ (lõi tím sáng + viền tím đậm!)
        stormBoltMat = CreateUnlitAdditiveMaterial(new Color(0.84f, 0.44f, 1.00f, 1.00f));
        stormBoltGlowMat = CreateUnlitAdditiveMaterial(new Color(0.62f, 0.16f, 1.00f, 0.70f));
        ambientLightningTimer = 1.1f;

        // 5. Rào chắn giới hạn đấu trường Bão Sét (Hàng rào năng lượng điện tím + Collider kín 360 độ!)
        BuildArenaBoundaryBarrier(themeRoot.transform, 30.2f, new Color(0.76f, 0.30f, 1.00f, 0.68f), true);
    }

    /// <summary>
    /// Dựng vòng tường vật lý kín 360 độ + hàng rào năng lượng bán trong suốt quanh đấu trường
    /// để người chơi nhìn rõ ranh giới và tuyệt đối không thể chạy/dash ra khỏi sàn thi đấu!
    /// </summary>
    void BuildArenaBoundaryBarrier(Transform parent, float innerRadius, Color barrierColor, bool highForcefield)
    {
        GameObject boundaryRoot = new GameObject("ArenaBoundaryBarrier");
        boundaryRoot.transform.SetParent(parent, false);
        boundaryRoot.transform.localPosition = Vector3.zero;

        const int segCount = 36;
        float wallThickness = 2.0f;
        float colliderRadius = innerRadius + wallThickness * 0.5f;
        float segWidth = (2f * Mathf.PI * colliderRadius / segCount) + 0.85f;
        float colliderHeight = 20f;

        // 1. 36 đoạn BoxCollider kín khít quanh chu vi (cao 20m để nhảy hay Dash đều không vượt qua được!)
        for (int i = 0; i < segCount; i++)
        {
            float ang = (i / (float)segCount) * Mathf.PI * 2f;
            GameObject wall = new GameObject("BoundaryWallCol_" + i);
            wall.transform.SetParent(boundaryRoot.transform, false);
            wall.transform.localPosition = new Vector3(Mathf.Cos(ang) * colliderRadius, colliderHeight * 0.45f, Mathf.Sin(ang) * colliderRadius);
            wall.transform.localRotation = Quaternion.LookRotation(new Vector3(-Mathf.Cos(ang), 0f, -Mathf.Sin(ang)), Vector3.up);
            BoxCollider bc = wall.AddComponent<BoxCollider>();
            bc.size = new Vector3(segWidth, colliderHeight, wallThickness);
        }

        // 2. Màng chắn năng lượng phát sáng bán trong suốt chạy tròn 360 độ để người chơi thấy rõ giới hạn!
        float visualHeight = highForcefield ? 4.4f : 2.1f;
        float visRadius = innerRadius + 0.08f;

        Texture2D barrierTex = CreateForcefieldBarrierTexture(barrierColor);
        Material wallMat = CreateUnlitAdditiveMaterial(barrierColor);
        wallMat.mainTexture = barrierTex;
        if (wallMat.HasProperty("_BaseMap")) wallMat.SetTexture("_BaseMap", barrierTex);
        if (wallMat.HasProperty("_Cull")) wallMat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        wallMat.mainTextureScale = new Vector2(18f, 1f);
        arenaBarrierVisualMat = wallMat;

        Mesh curtainMesh = new Mesh();
        curtainMesh.name = "ArenaBarrierCurtainMesh";
        Vector3[] verts = new Vector3[(segCount + 1) * 2];
        Vector2[] uvs = new Vector2[(segCount + 1) * 2];
        int[] tris = new int[segCount * 6];

        for (int i = 0; i <= segCount; i++)
        {
            float u = i / (float)segCount;
            float a = u * Mathf.PI * 2f;
            float x = Mathf.Cos(a) * visRadius;
            float z = Mathf.Sin(a) * visRadius;
            verts[i * 2] = new Vector3(x, 0.02f, z);
            verts[i * 2 + 1] = new Vector3(x, visualHeight, z);
            uvs[i * 2] = new Vector2(u, 0f);
            uvs[i * 2 + 1] = new Vector2(u, 1f);
        }

        int ti = 0;
        for (int i = 0; i < segCount; i++)
        {
            int b0 = i * 2;
            int t0 = b0 + 1;
            int b1 = b0 + 2;
            int t1 = b0 + 3;
            tris[ti++] = b0; tris[ti++] = b1; tris[ti++] = t0;
            tris[ti++] = b1; tris[ti++] = t1; tris[ti++] = t0;
        }
        curtainMesh.vertices = verts;
        curtainMesh.uv = uvs;
        curtainMesh.triangles = tris;
        curtainMesh.RecalculateNormals();
        curtainMesh.RecalculateBounds();

        GameObject curtainGo = new GameObject("ArenaBarrierForcefieldVisual");
        curtainGo.transform.SetParent(boundaryRoot.transform, false);
        curtainGo.AddComponent<MeshFilter>().sharedMesh = curtainMesh;
        MeshRenderer mr = curtainGo.AddComponent<MeshRenderer>();
        mr.sharedMaterial = wallMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // 3. Các vòng tia giới hạn sáng rõ ở chân, giữa và đỉnh tường năng lượng
        Material railMat = CreateUnlitAdditiveMaterial(new Color(barrierColor.r, barrierColor.g, barrierColor.b, 0.90f));
        CreateFloorNeonRing(boundaryRoot.transform, "BoundaryBaseRail", visRadius, 0.28f, 0.14f, railMat);
        if (highForcefield)
        {
            CreateFloorNeonRing(boundaryRoot.transform, "BoundaryMidRail", visRadius, 0.14f, visualHeight * 0.50f, railMat);
            CreateFloorNeonRing(boundaryRoot.transform, "BoundaryTopRail", visRadius, 0.18f, visualHeight, railMat);
        }
        else
        {
            CreateFloorNeonRing(boundaryRoot.transform, "BoundaryTopRail", visRadius, 0.14f, visualHeight * 0.72f, railMat);
        }
    }

    Texture2D CreateForcefieldBarrierTexture(Color tint)
    {
        const int w = 128;
        const int h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < h; y++)
        {
            float v = y / (float)(h - 1);
            // Đậm rõ ở sát mặt đất & đỉnh rào, trong suốt dịu ở giữa để không che tầm nhìn!
            float bottomGlow = Mathf.Exp(-v * 4.8f) * 0.95f;
            float topEdge = Mathf.Exp(-Mathf.Pow((1f - v) * 9f, 2f)) * 0.65f;
            float bodyFade = (1f - v) * 0.36f;

            for (int x = 0; x < w; x++)
            {
                // Họa tiết lưới chéo năng lượng công nghệ / điện trường
                int d1 = (x + y) % 24;
                int d2 = (x - y + 256) % 24;
                bool diag = (d1 < 2 || d2 < 2);
                bool hBand = (y % 32 < 2);

                float pattern = (diag ? 0.42f : 0f) + (hBand ? 0.28f : 0f);
                float alpha = Mathf.Clamp01((bottomGlow + topEdge + bodyFade * (0.45f + pattern)) * (0.72f + 0.28f * (1f - v)));
                Color c = Color.Lerp(tint, Color.white, bottomGlow * 0.38f + (diag ? 0.20f : 0f));
                c.a = alpha;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        runtimeTextures.Add(tex);
        return tex;
    }

    void UpdateThunderStormAmbience()
    {
        if (flashDecay > 0f)
        {
            flashDecay = Mathf.Max(0f, flashDecay - Time.deltaTime * 6.8f);
            if (mainDirectionalLight != null)
            {
                mainDirectionalLight.intensity = Mathf.Lerp(0.82f, 2.25f, flashDecay);
                mainDirectionalLight.color = Color.Lerp(new Color(0.68f, 0.56f, 0.96f), new Color(0.88f, 0.62f, 1.00f), flashDecay);
            }
            RenderSettings.ambientLight = Color.Lerp(new Color(0.12f, 0.08f, 0.22f), new Color(0.36f, 0.16f, 0.64f), flashDecay * 0.68f);
        }

        // Tần suất sét giật dày & kịch tính hơn (0.26s - 0.78s, đôi khi phóng 2-3 tia cùng lúc!)
        ambientLightningTimer -= Time.deltaTime;
        if (ambientLightningTimer <= 0f)
        {
            ambientLightningTimer = Random.Range(0.26f, 0.78f);
            int burstCount = Random.value < 0.48f ? 2 : (Random.value < 0.18f ? 3 : 1);
            for (int b = 0; b < burstCount; b++)
            {
                SpawnAmbientStormLightning();
            }
        }
    }

    void SpawnAmbientStormLightning()
    {
        if (stormTowerTops.Count == 0 || stormBoltMat == null)
            return;

        float mode = Random.value;
        Vector3 start;
        Vector3 target;

        if (mode < 0.36f && stormTowerTops.Count >= 2)
        {
            // Tia sét tím phóng ngang rực rỡ giữa 2 cột thu lôi trắng liền kề hoặc đối diện!
            int idxA = Random.Range(0, stormTowerTops.Count);
            int idxB = (idxA + Random.Range(1, 3)) % stormTowerTops.Count;
            start = stormTowerTops[idxA];
            target = stormTowerTops[idxB];
        }
        else if (mode < 0.78f)
        {
            // Tia sét tím đánh thẳng từ tầng mây giông xuống đỉnh cột thu lôi trắng
            target = stormTowerTops[Random.Range(0, stormTowerTops.Count)];
            start = target + new Vector3(Random.Range(-11f, 11f), 26f, Random.Range(-11f, 11f));
        }
        else
        {
            // Sét rạch ngang bầu trời phía trên đấu trường
            Vector3 c = themeRoot != null ? themeRoot.transform.position : Vector3.zero;
            float a1 = Random.Range(0f, Mathf.PI * 2f);
            float a2 = a1 + Random.Range(1.2f, 2.4f);
            start = c + new Vector3(Mathf.Cos(a1) * 34f, Random.Range(24f, 32f), Mathf.Sin(a1) * 34f);
            target = c + new Vector3(Mathf.Cos(a2) * 34f, Random.Range(22f, 30f), Mathf.Sin(a2) * 34f);
        }

        StartCoroutine(LightningBoltRoutine(start, target, Random.Range(0.24f, 0.42f), Random.Range(0.45f, 0.72f), 1.9f, true));
        flashDecay = Mathf.Max(flashDecay, 0.52f);
    }

    /// <summary>
    /// Tia sét TÍM khổng lồ đánh thẳng từ bầu trời bão xuống trúng quả bóng -> kích hoạt phình to!
    /// </summary>
    public void SpawnBallLightningStrikeVFX(Transform ballTransform, int newStackLevel)
    {
        if (ballTransform == null)
            return;

        if (stormBoltMat == null)
        {
            stormBoltMat = CreateUnlitAdditiveMaterial(new Color(0.84f, 0.44f, 1.00f, 1.00f));
        }
        if (stormBoltGlowMat == null)
        {
            stormBoltGlowMat = CreateUnlitAdditiveMaterial(new Color(0.62f, 0.16f, 1.00f, 0.70f));
        }

        StartCoroutine(BallLightningStrikeRoutine(ballTransform, newStackLevel));
    }

    IEnumerator BallLightningStrikeRoutine(Transform ballTf, int newStackLevel)
    {
        // 1. Vòng tụ điện TÍM cảnh báo trên quả bóng trong 0.40s
        GameObject warnGo = new GameObject("BallLightningTelegraph");
        LineRenderer warnLr = warnGo.AddComponent<LineRenderer>();
        warnLr.useWorldSpace = true;
        warnLr.loop = true;
        const int segs = 28;
        warnLr.positionCount = segs;
        warnLr.startWidth = 0.18f;
        warnLr.endWidth = 0.18f;
        warnLr.sharedMaterial = stormBoltMat;
        warnLr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        float tWarn = 0f;
        while (tWarn < 0.40f && ballTf != null)
        {
            float p = tWarn / 0.40f;
            float rad = Mathf.Lerp(2.6f, 0.65f, p);
            Vector3 c = ballTf.position;
            for (int i = 0; i < segs; i++)
            {
                float a = (i / (float)segs) * Mathf.PI * 2f + Time.time * 10f;
                warnLr.SetPosition(i, c + new Vector3(Mathf.Cos(a) * rad, 0f, Mathf.Sin(a) * rad));
            }
            tWarn += Time.deltaTime;
            yield return null;
        }
        Destroy(warnGo);

        if (ballTf == null)
            yield break;

        // 2. CỘT SÉT TÍM KHỔNG LỒ ĐÁNH THẲNG TỪ MÂY XUỐNG QUẢ BÓNG!
        Vector3 hitPos = ballTf.position;
        Vector3 skyPos = hitPos + new Vector3(Random.Range(-3.5f, 3.5f), 32f, Random.Range(-3.5f, 3.5f));
        flashDecay = 1.0f;

        StartCoroutine(LightningBoltRoutine(skyPos, hitPos, 0.48f, 1.05f, 2.2f, true));
        StartCoroutine(LightningBoltRoutine(skyPos + new Vector3(2.4f, -4f, -1.8f), hitPos, 0.34f, 0.48f, 2.6f, false));
        StartCoroutine(LightningBoltRoutine(skyPos + new Vector3(-2.2f, -6f, 2.1f), hitPos, 0.32f, 0.44f, 2.6f, false));

        // 3. Vòng sóng xung kích Sét Tím quanh bóng
        GameObject ringGo = new GameObject("LightningImpactShockwave");
        LineRenderer ringLr = ringGo.AddComponent<LineRenderer>();
        ringLr.useWorldSpace = true;
        ringLr.loop = true;
        ringLr.positionCount = segs;
        ringLr.sharedMaterial = stormBoltMat;
        ringLr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        float elapsed = 0f;
        const float waveDur = 0.46f;
        float maxWaveRad = 3.2f + newStackLevel * 0.78f;
        while (elapsed < waveDur)
        {
            float f = elapsed / waveDur;
            float r = Mathf.Lerp(0.6f, maxWaveRad, 1f - Mathf.Pow(1f - f, 2.5f));
            ringLr.startWidth = Mathf.Lerp(0.46f, 0.02f, f);
            ringLr.endWidth = ringLr.startWidth;
            Vector3 c = ballTf != null ? ballTf.position : hitPos;
            for (int i = 0; i < segs; i++)
            {
                float a = (i / (float)segs) * Mathf.PI * 2f;
                float jag = Mathf.Sin(i * 5f + elapsed * 35f) * 0.18f * (1f - f);
                ringLr.SetPosition(i, c + new Vector3(Mathf.Cos(a) * (r + jag), jag * 0.6f, Mathf.Sin(a) * (r + jag)));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(ringGo);
    }

    IEnumerator LightningBoltRoutine(Vector3 start, Vector3 end, float duration, float width, float jitter, bool withGlowOuter)
    {
        GameObject boltGo = new GameObject("PurpleLightningBoltVFX");
        LineRenderer lr = boltGo.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        const int pts = 14;
        lr.positionCount = pts;
        lr.sharedMaterial = stormBoltMat;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.numCornerVertices = 2;

        // Dải màu SÉT TÍM rực rỡ (tím hồng chói -> tím hoàng gia -> tím tử đinh hương)
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.96f, 0.76f, 1.00f), 0f),
                new GradientColorKey(new Color(0.76f, 0.24f, 1.00f), 0.50f),
                new GradientColorKey(new Color(0.92f, 0.64f, 1.00f), 1f)
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        lr.colorGradient = g;

        LineRenderer outerLr = null;
        if (withGlowOuter && stormBoltGlowMat != null)
        {
            GameObject haloGo = new GameObject("PurpleBoltGlow");
            haloGo.transform.SetParent(boltGo.transform, false);
            outerLr = haloGo.AddComponent<LineRenderer>();
            outerLr.useWorldSpace = true;
            outerLr.positionCount = pts;
            outerLr.sharedMaterial = stormBoltGlowMat;
            outerLr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        float elapsed = 0f;
        float reseedTimer = 0f;
        while (elapsed < duration)
        {
            float f = elapsed / duration;
            lr.startWidth = width * Mathf.Lerp(1.1f, 0.06f, f);
            lr.endWidth = width * 0.65f * Mathf.Lerp(1.1f, 0.04f, f);
            if (outerLr != null)
            {
                outerLr.startWidth = lr.startWidth * 2.3f;
                outerLr.endWidth = lr.endWidth * 2.3f;
            }

            reseedTimer -= Time.deltaTime;
            if (reseedTimer <= 0f)
            {
                reseedTimer = 0.045f;
                for (int i = 0; i < pts; i++)
                {
                    float t = i / (float)(pts - 1);
                    Vector3 p = Vector3.Lerp(start, end, t);
                    if (i > 0 && i < pts - 1)
                    {
                        float env = Mathf.Sin(t * Mathf.PI);
                        p += new Vector3(
                            Random.Range(-jitter, jitter) * env,
                            Random.Range(-jitter * 0.35f, jitter * 0.35f) * env,
                            Random.Range(-jitter, jitter) * env
                        );
                    }
                    lr.SetPosition(i, p);
                    if (outerLr != null) outerLr.SetPosition(i, p);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(boltGo);
    }

    // =========================================================================
    // PROCEDURAL TEXTURES & HELPERS
    // =========================================================================

    Texture2D CreateVolcanoFloorTexture()
    {
        const int sz = 256;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        Color basaltDark = new Color(0.045f, 0.036f, 0.040f, 1f);
        Color basaltMid = new Color(0.095f, 0.070f, 0.068f, 1f);
        Color lavaRed = new Color(0.74f, 0.16f, 0.02f, 1f);
        Color lavaCore = new Color(0.95f, 0.45f, 0.08f, 1f);

        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float u = x / (float)sz;
                float v = y / (float)sz;
                float rockN = Mathf.PerlinNoise(u * 9f + 2.1f, v * 9f + 4.3f);
                float fissure1 = Mathf.Abs(Mathf.PerlinNoise(u * 5.5f + 8.2f, v * 5.5f + 1.7f) - 0.5f);
                float fissure2 = Mathf.Abs(Mathf.PerlinNoise(u * 11f + 3.4f, v * 11f + 7.9f) - 0.5f);
                float crack = Mathf.Min(fissure1, fissure2 * 1.4f);

                Color c = Color.Lerp(basaltDark, basaltMid, rockN);
                if (crack < 0.022f)
                {
                    float glow = 1f - (crack / 0.022f);
                    Color magma = Color.Lerp(lavaRed, lavaCore, glow * glow);
                    c = Color.Lerp(c, magma, Mathf.Clamp01(glow * 0.90f));
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        runtimeTextures.Add(tex);
        return tex;
    }

    Texture2D CreateMoltenLavaTexture()
    {
        const int sz = 256;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        Color crust = new Color(0.24f, 0.05f, 0.02f, 1f);
        Color orange = new Color(0.98f, 0.34f, 0.04f, 1f);
        Color brightGold = new Color(1.00f, 0.82f, 0.22f, 1f);

        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float u = x / (float)sz;
                float v = y / (float)sz;
                float n1 = Mathf.PerlinNoise(u * 6f, v * 6f);
                float n2 = Mathf.PerlinNoise(u * 14f + 5f, v * 14f + 2f);
                float m = n1 * 0.65f + n2 * 0.35f;
                Color c = m < 0.42f
                    ? Color.Lerp(crust, orange, m / 0.42f)
                    : Color.Lerp(orange, brightGold, (m - 0.42f) / 0.58f);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        runtimeTextures.Add(tex);
        return tex;
    }

    Texture2D CreateCyberBlueBlackFloorTexture()
    {
        const int sz = 256;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        // Tone Blue-Black sắc nét, tối sâu để Ball Công Nghệ sáng rực nổi bật tuyệt đối!
        Color tileBlack = new Color(0.018f, 0.028f, 0.058f, 1f);
        Color tileDeepBlue = new Color(0.032f, 0.058f, 0.125f, 1f);
        Color lineRoyalBlue = new Color(0.10f, 0.38f, 0.88f, 1f);
        Color nodeBlue = new Color(0.22f, 0.58f, 1.00f, 1f);

        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                int gx = x % 64;
                int gy = y % 64;
                bool majorLine = (x < 3 || x > sz - 3 || y < 3 || y > sz - 3);
                bool subLine = (gx < 2 || gy < 2);
                bool nodeDot = (gx < 4 && gy < 4);

                Color c = ((x / 64 + y / 64) % 2 == 0) ? tileBlack : tileDeepBlue;
                if (subLine) c = Color.Lerp(c, lineRoyalBlue, 0.28f);
                if (majorLine) c = Color.Lerp(c, lineRoyalBlue, 0.72f);
                if (nodeDot) c = nodeBlue;

                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        runtimeTextures.Add(tex);
        return tex;
    }

    Texture2D CreateStormFloorTexture()
    {
        const int sz = 256;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        Color rockDark = new Color(0.05f, 0.05f, 0.10f, 1f);
        Color rockMid = new Color(0.10f, 0.09f, 0.18f, 1f);
        Color purpleCrack = new Color(0.76f, 0.30f, 1.00f, 1f);

        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float u = x / (float)sz;
                float v = y / (float)sz;
                float n = Mathf.PerlinNoise(u * 8f + 1.3f, v * 8f + 2.7f);
                float crack = Mathf.Abs(Mathf.PerlinNoise(u * 6f + 11f, v * 6f + 4f) - 0.5f);
                Color c = Color.Lerp(rockDark, rockMid, n);
                if (crack < 0.026f)
                {
                    float glow = 1f - (crack / 0.026f);
                    c = Color.Lerp(c, purpleCrack, glow * 0.90f);
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        runtimeTextures.Add(tex);
        return tex;
    }

    void CreateFloorNeonRing(Transform parent, string name, float radius, float width, float y, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        const int segs = 72;
        lr.positionCount = segs;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.sharedMaterial = mat;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        for (int i = 0; i < segs; i++)
        {
            float a = (i / (float)segs) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius));
        }
    }

    void CreateAmbientParticles(
        Transform parent,
        string name,
        Vector3 localPos,
        float radius,
        float rate,
        Color colA,
        Color colB,
        float minSize,
        float maxSize,
        float gravity,
        float speed
    )
    {
        GameObject psGo = new GameObject(name);
        psGo.transform.SetParent(parent, false);
        psGo.transform.localPosition = localPos;

        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(colA, colB);

        var emit = ps.emission;
        emit.rateOverTime = rate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;

        Material mat = CreateUnlitAdditiveMaterial(Color.white);
        Texture2D dot = ParrySlashVFX.GetSoftGlowTexture();
        mat.mainTexture = dot;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", dot);

        ParticleSystemRenderer psr = psGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.sharedMaterial = mat;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
        }
        ps.Play();
    }

    Material CreateLitMaterial(Color baseColor, float metallic, float smoothness, Color emission)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);
        if (m.HasProperty("_Color")) m.SetColor("_Color", baseColor);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
        }
        runtimeMaterials.Add(m);
        return m;
    }

    Material CreateUnlitAdditiveMaterial(Color col)
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
        m.renderQueue = 3060;
        runtimeMaterials.Add(m);
        return m;
    }

    // =========================================================================
    // MAP ANNOUNCEMENT BANNER
    // =========================================================================

    void ShowMapAnnouncementBanner(ArenaMapType mapType)
    {
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(MapBannerRoutine(mapType));
    }

    IEnumerator MapBannerRoutine(ArenaMapType mapType)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) yield break;

        if (mapBannerGo != null) Destroy(mapBannerGo);

        Color accent = GetMapAccentColor(mapType);
        mapBannerGo = new GameObject("MapAnnouncementBanner", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        mapBannerGo.transform.SetParent(canvas.transform, false);
        mapBannerGo.transform.SetAsLastSibling();

        RectTransform rt = mapBannerGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.24f, 0.86f);
        rt.anchorMax = new Vector2(0.76f, 0.97f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image border = mapBannerGo.GetComponent<Image>();
        border.color = accent;
        border.raycastTarget = false;

        mapBannerGroup = mapBannerGo.GetComponent<CanvasGroup>();
        mapBannerGroup.blocksRaycasts = false;
        mapBannerGroup.alpha = 0f;

        GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(mapBannerGo.transform, false);
        RectTransform inRt = inner.GetComponent<RectTransform>();
        inRt.anchorMin = Vector2.zero;
        inRt.anchorMax = Vector2.one;
        inRt.offsetMin = new Vector2(2f, 2f);
        inRt.offsetMax = new Vector2(-2f, -2f);
        inner.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.16f, 0.92f);
        inner.GetComponent<Image>().raycastTarget = false;

        CreateBannerText("MapTitle", inner.transform, new Vector2(0.03f, 0.44f), new Vector2(0.97f, 0.96f),
            "MAP: " + GetMapDisplayName(mapType), 13f, 22f, FontStyles.Bold, accent);
        CreateBannerText("MapSub", inner.transform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.46f),
            GetMapSubtitle(mapType), 9f, 14f, FontStyles.Normal, new Color(0.86f, 0.92f, 1f, 0.95f));

        float t = 0f;
        while (t < 0.35f && mapBannerGroup != null)
        {
            t += Time.deltaTime;
            mapBannerGroup.alpha = Mathf.Clamp01(t / 0.35f);
            yield return null;
        }

        yield return new WaitForSeconds(3.2f);

        t = 0f;
        while (t < 0.55f && mapBannerGroup != null)
        {
            t += Time.deltaTime;
            mapBannerGroup.alpha = Mathf.Clamp01(1f - t / 0.55f);
            yield return null;
        }

        if (mapBannerGo != null) Destroy(mapBannerGo);
        mapBannerGo = null;
        bannerRoutine = null;
    }

    TMP_Text CreateBannerText(string name, Transform parent, Vector2 aMin, Vector2 aMax, string text, float minS, float maxS, FontStyles style, Color col)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = minS;
        tmp.fontSizeMax = maxS;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = col;
        tmp.raycastTarget = false;
        return tmp;
    }
}
