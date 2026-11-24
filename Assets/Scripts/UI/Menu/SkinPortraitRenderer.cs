using UnityEngine;

/// <summary>
/// Chụp ảnh đại diện 3D trực tiếp từ chính model nhân vật & ngoại trang thực tế trong game (Player.prefab + PlayerSkinController)
/// để toàn bộ Icon ngoại trang trong Shop & Menu khớp 100% với ngoại hình thực tế trong gameplay!
/// </summary>
public static class SkinPortraitRenderer
{
    static bool rendered;
    const int IconSize = 512;

    public static bool HasRendered => rendered;

    public static void EnsureRendered()
    {
        if (rendered)
            return;

        GameObject sourcePrefab = FindPlayerPrefab();
        if (sourcePrefab == null)
            return;

        rendered = true;
        RenderAllPortraits(sourcePrefab);
    }

    static GameObject FindPlayerPrefab()
    {
        if (MatchmakingManager.Instance != null && MatchmakingManager.Instance.playerPrefab != null)
        {
            return MatchmakingManager.Instance.playerPrefab.gameObject;
        }

        MatchmakingManager mm = Object.FindFirstObjectByType<MatchmakingManager>();
        if (mm != null && mm.playerPrefab != null)
        {
            return mm.playerPrefab.gameObject;
        }

        return Resources.Load<GameObject>("Player");
    }

    public static Texture2D RenderSingleSkinToTexture(GameObject sourcePrefab, SkinType skinType, int size = IconSize)
    {
        if (sourcePrefab == null)
            return null;

        Texture2D result = null;
        RenderAllInternal(sourcePrefab, new SkinType[] { skinType }, size, (st, tex) =>
        {
            result = tex;
        });
        return result;
    }

    static void RenderAllPortraits(GameObject sourcePrefab)
    {
        SkinDefinition[] all = SkinCatalog.All;
        SkinType[] types = new SkinType[all.Length];
        for (int i = 0; i < all.Length; i++) types[i] = all[i].type;

        RenderAllInternal(sourcePrefab, types, IconSize, (st, tex) =>
        {
            if (tex == null) return;
            Sprite sp = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            sp.name = "RuntimePortrait_" + st;
            UIThemeAssets.SetRuntimeSkinIcon(st, sp);
        });
    }

    static void RenderAllInternal(
        GameObject sourcePrefab,
        SkinType[] types,
        int size,
        System.Action<SkinType, Texture2D> onCaptured
    )
    {
        Vector3 stageOrigin = new Vector3(0f, -420f, 0f);

        // Tạo host ẩn để clone prefab mà KHÔNG kích hoạt Awake/OnEnable của script gameplay/Photon!
        GameObject inactiveHost = new GameObject("__SkinPortraitInactiveHost");
        inactiveHost.SetActive(false);

        GameObject studioRoot = new GameObject("__SkinPortraitStudio");
        studioRoot.transform.position = stageOrigin;

        RenderTexture rt = null;
        Texture2D backdropTex = null;
        Material backdropMat = null;

        try
        {
            GameObject previewChar = Object.Instantiate(sourcePrefab, inactiveHost.transform);
            previewChar.name = "PortraitPreviewCharacter";
            StripGameplayComponents(previewChar);

            // Ẩn vũ khí mặc định cầm tay để icon tập trung trọn vẹn vào ngoại trang nhân vật
            HideWeaponMeshes(previewChar.transform);

            previewChar.transform.SetParent(studioRoot.transform, false);
            previewChar.transform.localPosition = Vector3.zero;
            previewChar.transform.localRotation = Quaternion.Euler(0f, 16f, 0f);
            previewChar.transform.localScale = Vector3.one;
            previewChar.SetActive(true);

            PlayerSkinController skinCtrl = previewChar.AddComponent<PlayerSkinController>();

            // Tạo tấm phông nền studio chuyển sắc mượt sau lưng nhân vật
            GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdrop.name = "StudioBackdrop";
            backdrop.transform.SetParent(studioRoot.transform, false);
            backdrop.transform.localPosition = new Vector3(0f, 1.05f, -1.6f);
            backdrop.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            backdrop.transform.localScale = new Vector3(5.2f, 5.2f, 1f);
            Object.DestroyImmediate(backdrop.GetComponent<Collider>());

            Shader unlitSh = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitSh == null) unlitSh = Shader.Find("Unlit/Texture");
            backdropMat = new Material(unlitSh);
            MeshRenderer bdMr = backdrop.GetComponent<MeshRenderer>();
            bdMr.sharedMaterial = backdropMat;
            bdMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bdMr.receiveShadows = false;

            // Hệ thống 3 nguồn sáng studio chuyên nghiệp (Key + Fill + Accent Rim)
            Light keyLight = CreateStudioLight(studioRoot.transform, "KeyLight", LightType.Point,
                new Vector3(1.8f, 2.3f, 2.6f), new Color(1.00f, 0.96f, 0.92f), 26f, 12f);
            Light fillLight = CreateStudioLight(studioRoot.transform, "FillLight", LightType.Point,
                new Vector3(-2.1f, 1.4f, 2.2f), new Color(0.72f, 0.84f, 1.00f), 14f, 12f);
            Light rimLight = CreateStudioLight(studioRoot.transform, "RimLight", LightType.Point,
                new Vector3(-0.3f, 2.1f, -1.4f), Color.white, 22f, 10f);
            Light dirKey = CreateStudioLight(studioRoot.transform, "DirKey", LightType.Directional,
                Vector3.zero, new Color(1.0f, 0.97f, 0.94f), 1.35f, 20f);
            dirKey.transform.localRotation = Quaternion.Euler(24f, 152f, 0f);

            // Camera chân dung offscreen MSAA 4x
            GameObject camGo = new GameObject("PortraitCamera");
            camGo.transform.SetParent(studioRoot.transform, false);
            Camera cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = 27f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 20f;
            cam.allowHDR = false;
            cam.allowMSAA = true;

            rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            rt.Create();
            cam.targetTexture = rt;

            RenderTexture prevActive = RenderTexture.active;

            for (int i = 0; i < types.Length; i++)
            {
                SkinType st = types[i];
                SkinDefinition def = SkinCatalog.Get(st);
                if (def == null) continue;

                // Góc xoay & khung hình riêng giúp từng bộ ngoại trang khoe trọn điểm đẹp nhất!
                float yaw = 16f;
                float camY = 1.12f;
                float lookY = 1.06f;
                float camZ = 3.55f;

                switch (st)
                {
                    case SkinType.VoidReaper:
                        // Nữ Tử Thần: lấy toàn thân từ đỉnh mũ trùm nhọn viền vàng tới đôi chân dài trắng & tà áo choàng bay!
                        yaw = 15f; camY = 1.04f; lookY = 0.98f; camZ = 4.05f;
                        break;
                    case SkinType.StormValkyrie:
                        // Nữ Valkyrie: lấy trọn vầng hào quang Lôi Thần phía sau + quần ngắn + bốt!
                        yaw = 14f; camY = 1.12f; lookY = 1.06f; camZ = 4.10f;
                        break;
                    case SkinType.SolarPaladin:
                        // Thánh Hiệp: lấy trọn vòng hào quang trên đầu + giáp vàng + cả 2 đôi cánh thiên thần xếp gọn sau lưng!
                        yaw = 10f; camY = 1.16f; lookY = 1.08f; camZ = 3.95f;
                        break;
                    case SkinType.CyberRunner:
                        // Cyber Runner: lấy trọn kính Visor neon & cả 2 đôi cánh Cyber đối xứng phía sau!
                        yaw = 12f; camY = 1.16f; lookY = 1.08f; camZ = 3.95f;
                        break;
                    case SkinType.CrimsonSamurai:
                        // Huyết Ronin: lấy trọn nón Kasa rộng + mặt nạ Oni + khăn lụa đỏ & song kiếm!
                        yaw = 18f; camY = 1.14f; lookY = 1.06f; camZ = 3.80f;
                        break;
                    default:
                        // Tân Binh: khung hình nửa-toàn thân gọn gàng năng động
                        yaw = 16f; camY = 1.12f; lookY = 1.06f; camZ = 3.65f;
                        break;
                }

                previewChar.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                skinCtrl.ApplySkin(st);
                skinCtrl.ApplyPreviewPose();

                // Cập nhật phông nền & đèn rim theo bảng màu đặc trưng của từng skin
                if (backdropTex != null) Object.DestroyImmediate(backdropTex);
                backdropTex = CreatePortraitBackdropTexture(def.suitBaseColor, def.suitAccentColor);
                backdropMat.mainTexture = backdropTex;
                if (backdropMat.HasProperty("_BaseMap")) backdropMat.SetTexture("_BaseMap", backdropTex);
                if (backdropMat.HasProperty("_BaseColor")) backdropMat.SetColor("_BaseColor", Color.white);

                rimLight.color = Color.Lerp(Color.white, def.suitAccentColor, 0.72f);
                cam.backgroundColor = Color.Lerp(def.suitBaseColor, new Color(0.04f, 0.05f, 0.09f, 1f), 0.78f);

                cam.transform.localPosition = new Vector3(0f, camY, camZ);
                cam.transform.LookAt(studioRoot.transform.position + new Vector3(0f, lookY, 0f));

                cam.Render();

                RenderTexture.active = rt;
                Texture2D shot = new Texture2D(size, size, TextureFormat.RGBA32, false);
                shot.ReadPixels(new Rect(0f, 0f, size, size), 0, 0);
                shot.Apply();
                shot.wrapMode = TextureWrapMode.Clamp;

                onCaptured?.Invoke(st, shot);
            }

            RenderTexture.active = prevActive;
            cam.targetTexture = null;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[SkinPortraitRenderer] Loi chup anh 3D ngoai trang: " + ex.Message);
        }
        finally
        {
            if (rt != null)
            {
                rt.Release();
                Object.DestroyImmediate(rt);
            }
            if (backdropTex != null) Object.DestroyImmediate(backdropTex);
            if (backdropMat != null) Object.DestroyImmediate(backdropMat);
            if (studioRoot != null) Object.DestroyImmediate(studioRoot);
            if (inactiveHost != null) Object.DestroyImmediate(inactiveHost);
        }
    }

    static Light CreateStudioLight(Transform parent, string name, LightType type, Vector3 localPos, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        Light l = go.AddComponent<Light>();
        l.type = type;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        return l;
    }

    static void StripGameplayComponents(GameObject root)
    {
        Component[] all = root.GetComponentsInChildren<Component>(true);
        // Xóa các MonoBehaviour & thành phần vật lý/âm thanh/mạng trước (chỉ giữ Transform, MeshFilter, MeshRenderer, SkinnedMeshRenderer)
        for (int i = all.Length - 1; i >= 0; i--)
        {
            Component c = all[i];
            if (c == null) continue;
            if (c is Transform || c is SkinnedMeshRenderer || c is MeshRenderer || c is MeshFilter)
                continue;
            Object.DestroyImmediate(c);
        }
    }

    static void HideWeaponMeshes(Transform root)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            string n = all[i].name;
            if (n.Contains("Sword") || n.Contains("Wep") || n.Contains("Vetchem") || n.Contains("Weapon") || n.Contains("Trail"))
            {
                all[i].gameObject.SetActive(false);
            }
        }
    }

    static Texture2D CreatePortraitBackdropTexture(Color baseCol, Color accentCol)
    {
        const int sz = 128;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color darkBg = new Color(0.04f, 0.055f, 0.10f, 1f);
        Color midBg = Color.Lerp(darkBg, baseCol, 0.42f);
        Color glowCol = Color.Lerp(midBg, accentCol, 0.58f);

        for (int y = 0; y < sz; y++)
        {
            float v = (float)y / (sz - 1);
            for (int x = 0; x < sz; x++)
            {
                float u = (float)x / (sz - 1);
                float dx = (u - 0.50f) * 2f;
                float dy = (v - 0.54f) * 2f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float radialGlow = Mathf.Exp(-dist * dist * 2.2f);
                float vignette = Mathf.Clamp01(1f - dist * 0.52f);

                Color c = Color.Lerp(darkBg, midBg, v * 0.75f + 0.15f);
                c = Color.Lerp(c, glowCol, radialGlow * 0.62f);
                c *= (0.68f + 0.32f * vignette);
                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }
}
