using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingSceneManager : MonoBehaviour
{
    [Header("Legacy References (auto-hidden when modern UI builds)")]
    public Slider slider;
    public TextMeshProUGUI percentText;

    // Runtime modern UI references
    RectTransform bgZoomRt;
    RectTransform barFillRt;
    RectTransform barFlareRt;
    Image barFillImg;
    TMP_Text modernPercentText;
    TMP_Text stageStatusText;
    TMP_Text tipBodyText;
    CanvasGroup tipCanvasGroup;

    RectTransform orbOuterRing;
    RectTransform orbInnerRing;
    RectTransform orbCoreRt;
    Image orbCoreGlowImg;

    Image[] mapChipBorders;
    TMP_Text[] mapChipLabels;
    Color[] mapChipColors;

    struct FloatingSpark
    {
        public RectTransform rt;
        public Image img;
        public float speedY;
        public float driftX;
        public float phase;
        public float baseAlpha;
        public Vector2 normPos;
    }

    readonly List<FloatingSpark> sparks = new List<FloatingSpark>();
    readonly List<Object> runtimeAssets = new List<Object>();

    float progress;
    float visualProgress;
    float elapsedTime;
    int currentTipIndex;
    float tipTimer;
    bool uiBuilt;
    bool isStandalonePreview;

    Color modeAccent = new Color(0.18f, 0.78f, 1.00f, 1f);
    Color modeSecondary = new Color(0.08f, 0.28f, 0.62f, 1f);
    TMP_FontAsset uiFont;

    static readonly string[] CombatTips = new string[]
    {
        "Bam PARRY dung khoanh khac bong lao sat nguoi de phan don toc do cuc cao!",
        "Chieu DONG BANG lam qua bong bay cham lai ve phia ban — tan dung de canh Parry chuan xac!",
        "Khu Vuc Bao Set: Can than nhung tia set tim danh xuong lam qua bong phong to bat ngo!",
        "Dau Truong Cong Nghe: Bong Plasma se tang toc cuc nhanh sau moi nhip danh qua lai!",
        "Ket hop NHAY va DASH sang ngang de doi goc phan bong va khien doi thu tro tay khong kip!",
        "Nhan qua hom thu tan thu de so huu vang va mo khoa them Ngoai Trang, Vu Khi, Ky Nang!"
    };

    void Awake()
    {
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0.02f, 0.035f, 0.07f, 1f);
        }

        BuildModernLoadingUI();
    }

    void OnDestroy()
    {
        for (int i = 0; i < runtimeAssets.Count; i++)
        {
            if (runtimeAssets[i] != null)
            {
                Destroy(runtimeAssets[i]);
            }
        }
        runtimeAssets.Clear();
    }

    IEnumerator Start()
    {
        progress = 0f;
        visualProgress = 0f;

        // Cho MatchmakingManager ket noi (hoac tu chuyen sang che do xem truoc neu bam Play truc tiep trong LoadingScene)
        float waitRunner = 0f;
        while (MatchmakingManager.Instance == null || MatchmakingManager.Instance.runner == null)
        {
            waitRunner += Time.deltaTime;
            if (waitRunner > 0.45f)
            {
                isStandalonePreview = true;
                break;
            }
            yield return null;
        }

        if (isStandalonePreview)
        {
            // Che do xem truoc muot ma khi mo truc tiep LoadingScene trong Unity Editor
            while (true)
            {
                progress = Mathf.Clamp01(progress + Time.deltaTime * 0.28f);
                if (progress >= 1f)
                {
                    yield return new WaitForSeconds(1.1f);
                    progress = 0f;
                    visualProgress = 0f;
                }
                yield return null;
            }
        }

        // ================= LOADING LOOP (ONLINE / GAMEPLAY FLOW) =================
        while (true)
        {
            if (!MatchmakingManager.SceneLoaded)
            {
                progress += Time.deltaTime * 0.30f;
                progress = Mathf.Clamp(progress, 0f, 0.90f);
            }
            else
            {
                progress += Time.deltaTime * 1.65f;
                progress = Mathf.Clamp01(progress);
            }

            if (slider != null) slider.value = progress;
            if (percentText != null) percentText.text = Mathf.RoundToInt(progress * 100f) + "%";

            if (progress >= 1f)
            {
                break;
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.22f);
        Debug.Log("Loading Complete");
    }

    void Update()
    {
        float dt = Time.deltaTime;
        elapsedTime += dt;

        // Lam muot thanh tien trinh
        visualProgress = Mathf.Lerp(visualProgress, progress, Mathf.Clamp01(dt * 10f));
        if (Mathf.Abs(progress - visualProgress) < 0.002f) visualProgress = progress;

        UpdateProgressVisuals(visualProgress);
        UpdateAmbientAnimations(dt);
        UpdateTipsRotation(dt);
        UpdateMapScannerHighlight();
    }

    // ================= BUILD MODERN LOADING UI =================

    void BuildModernLoadingUI()
    {
        if (uiBuilt) return;
        uiBuilt = true;

        AccountDatabaseService.EnsureInitialized();

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cGo = new GameObject("LoadingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Lay font TMP san co trong scene (hoac fallback TMP default)
        if (percentText != null && percentText.font != null)
        {
            uiFont = percentText.font;
        }
        else
        {
            TMP_Text anyTmp = canvas.GetComponentInChildren<TMP_Text>(true);
            if (anyTmp != null) uiFont = anyTmp.font;
        }

        // An cac thanh phan giao dien cu (Background.png / FireBall-xoanen.png / Slider mac dinh)
        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            Transform ch = canvas.transform.GetChild(i);
            if (ch != null && ch.name != "ModernLoadingRoot")
            {
                ch.gameObject.SetActive(false);
            }
        }

        // Xac dinh chu de mau sac & hinh nen phu hop theo che do choi
        GameMatchMode mode = MatchmakingManager.CurrentMatchMode;
        string modeTitle;
        string modeSub;
        Sprite bgSprite;

        switch (mode)
        {
            case GameMatchMode.Solo:
                modeTitle = "SOLO 1V1  •  DAU TAY DOI";
                modeSub = "CHUAN BI GIAO TRAN TRUC DIEN";
                modeAccent = new Color(0.18f, 0.88f, 1.00f, 1f);
                modeSecondary = new Color(0.06f, 0.24f, 0.48f, 1f);
                bgSprite = UIThemeAssets.BgCardSolo != null ? UIThemeAssets.BgCardSolo : UIThemeAssets.BgMenuArena;
                break;

            case GameMatchMode.Custom:
                modeTitle = string.IsNullOrEmpty(MatchmakingManager.CurrentCustomRoomCode)
                    ? "PHONG TUY CHON  •  CUSTOM"
                    : $"PHONG TUY CHON  •  MA {MatchmakingManager.CurrentCustomRoomCode}";
                modeSub = "DANG KET NOI TAT CA NGUOI CHOI TRONG PHONG";
                modeAccent = new Color(0.76f, 0.42f, 1.00f, 1f);
                modeSecondary = new Color(0.24f, 0.08f, 0.44f, 1f);
                bgSprite = UIThemeAssets.BgMenuArena;
                break;

            case GameMatchMode.Training:
                modeTitle = "LUYEN TAP  •  TRAINING ARENA";
                modeSub = "REN PHAN XA PARRY & LAM CHU KY NANG";
                modeAccent = new Color(0.22f, 0.94f, 0.62f, 1f);
                modeSecondary = new Color(0.06f, 0.30f, 0.22f, 1f);
                bgSprite = UIThemeAssets.BgMenuArena;
                break;

            default:
                modeTitle = "XEP HANG  •  RANKED ARENA";
                modeSub = "DAU TRUONG SINH TU  •  TRANH DIEM ELO";
                modeAccent = new Color(1.00f, 0.72f, 0.20f, 1f);
                modeSecondary = new Color(0.46f, 0.14f, 0.08f, 1f);
                bgSprite = UIThemeAssets.BgCardRank != null ? UIThemeAssets.BgCardRank : UIThemeAssets.BgMenuArena;
                break;
        }

        // Root full-screen
        GameObject root = new GameObject("ModernLoadingRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        StretchFull(rootRt);

        // 1. CINEMATIC LAYERED BACKDROP
        GameObject bgHolder = new GameObject("BackdropHolder", typeof(RectTransform), typeof(RectMask2D));
        bgHolder.transform.SetParent(root.transform, false);
        StretchFull(bgHolder.GetComponent<RectTransform>());

        GameObject bgImgGo = new GameObject("BackdropArt", typeof(RectTransform), typeof(Image));
        bgImgGo.transform.SetParent(bgHolder.transform, false);
        bgZoomRt = bgImgGo.GetComponent<RectTransform>();
        StretchFull(bgZoomRt);
        bgZoomRt.localScale = Vector3.one * 1.06f;
        Image bgImg = bgImgGo.GetComponent<Image>();
        bgImg.sprite = bgSprite;
        bgImg.preserveAspect = false;
        bgImg.color = bgSprite != null ? new Color(0.84f, 0.88f, 0.96f, 1f) : new Color(0.04f, 0.07f, 0.14f, 1f);
        bgImg.raycastTarget = false;

        // Lop phu vignette toi sang trong + gradient duoi + duong scanline Cyber
        GameObject vignetteGo = new GameObject("CinematicVignette", typeof(RectTransform), typeof(Image));
        vignetteGo.transform.SetParent(root.transform, false);
        StretchFull(vignetteGo.GetComponent<RectTransform>());
        Image vigImg = vignetteGo.GetComponent<Image>();
        vigImg.sprite = CreateVignetteSprite(modeSecondary, modeAccent);
        vigImg.color = Color.white;
        vigImg.raycastTarget = false;

        // Cac dom nang luong bay len nhe nhang
        BuildFloatingSparks(root.transform);

        // 2. TOP HEADER BAR (LOGO + MODE BADGE + RANDOM MAP POOL)
        BuildTopHeader(root.transform, modeTitle, modeSub);

        // 3. CENTER HERO CONTENT (PLAYER LOADOUT CARD + ANIMATED FLASHBALL CORE)
        BuildCenterShowcase(root.transform);

        // 4. BOTTOM PROGRESS BAR & ROTATING COMBAT TIPS
        BuildBottomProgressArea(root.transform);

        currentTipIndex = Random.Range(0, CombatTips.Length);
        if (tipBodyText != null)
        {
            tipBodyText.text = CombatTips[currentTipIndex];
        }
    }

    void BuildTopHeader(Transform parent, string modeTitle, string modeSub)
    {
        // Thanh mo phia tren
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(parent, false);
        RectTransform tbRt = topBar.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0f, 0.82f);
        tbRt.anchorMax = new Vector2(1f, 1f);
        tbRt.offsetMin = Vector2.zero;
        tbRt.offsetMax = Vector2.zero;
        Image tbImg = topBar.GetComponent<Image>();
        tbImg.color = new Color(0.02f, 0.04f, 0.08f, 0.58f);
        tbImg.raycastTarget = false;

        // Duong vien phat sang duoi topbar
        GameObject line = new GameObject("TopGlowLine", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(topBar.transform, false);
        RectTransform lRt = line.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0.04f, 0f);
        lRt.anchorMax = new Vector2(0.96f, 0.018f);
        lRt.offsetMin = Vector2.zero;
        lRt.offsetMax = Vector2.zero;
        Image lImg = line.GetComponent<Image>();
        lImg.color = new Color(modeAccent.r, modeAccent.g, modeAccent.b, 0.65f);
        lImg.raycastTarget = false;

        // Trai: Ten game FLASHBALL + Badge che do dau
        CreateText("GameLogo", topBar.transform, new Vector2(0.045f, 0.36f), new Vector2(0.32f, 0.90f),
            "FLASHBALL", 32f, 54f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

        CreateText("ModeSub", topBar.transform, new Vector2(0.047f, 0.08f), new Vector2(0.45f, 0.38f),
            modeSub, 14f, 20f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.76f, 0.85f, 0.96f, 0.92f));

        // Pill che do choi noi bat
        GameObject modePill = new GameObject("ModePill", typeof(RectTransform), typeof(Image));
        modePill.transform.SetParent(topBar.transform, false);
        RectTransform mpRt = modePill.GetComponent<RectTransform>();
        mpRt.anchorMin = new Vector2(0.24f, 0.44f);
        mpRt.anchorMax = new Vector2(0.55f, 0.82f);
        mpRt.offsetMin = Vector2.zero;
        mpRt.offsetMax = Vector2.zero;
        Image mpImg = modePill.GetComponent<Image>();
        mpImg.color = new Color(modeAccent.r, modeAccent.g, modeAccent.b, 0.18f);
        mpImg.raycastTarget = false;

        GameObject mpAccent = new GameObject("LeftBar", typeof(RectTransform), typeof(Image));
        mpAccent.transform.SetParent(modePill.transform, false);
        RectTransform maRt = mpAccent.GetComponent<RectTransform>();
        maRt.anchorMin = new Vector2(0f, 0f);
        maRt.anchorMax = new Vector2(0.022f, 1f);
        maRt.offsetMin = Vector2.zero;
        maRt.offsetMax = Vector2.zero;
        mpAccent.GetComponent<Image>().color = modeAccent;
        mpAccent.GetComponent<Image>().raycastTarget = false;

        CreateText("ModeTitle", modePill.transform, new Vector2(0.05f, 0.05f), new Vector2(0.96f, 0.95f),
            modeTitle, 16f, 24f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, modeAccent);

        // Phai: 3 Chip xem truoc 3 dau truong ngau nhien (Nui Lua • Cong Nghe • Bao Set)
        CreateText("MapPoolTitle", topBar.transform, new Vector2(0.58f, 0.64f), new Vector2(0.955f, 0.92f),
            "DAU TRUONG NGAU NHIEN  •  3 BAN DO", 13f, 18f, FontStyles.Bold, TextAlignmentOptions.MidlineRight,
            new Color(0.74f, 0.82f, 0.94f, 0.88f));

        string[] mapNames = { "NUI LUA", "CONG NGHE", "BAO SET" };
        mapChipColors = new Color[]
        {
            new Color(1.00f, 0.44f, 0.16f, 1f),
            new Color(0.14f, 0.78f, 1.00f, 1f),
            new Color(0.78f, 0.36f, 1.00f, 1f)
        };
        mapChipBorders = new Image[3];
        mapChipLabels = new TMP_Text[3];

        float startX = 0.585f;
        float chipW = 0.116f;
        float gap = 0.011f;
        for (int i = 0; i < 3; i++)
        {
            float x0 = startX + i * (chipW + gap);
            float x1 = x0 + chipW;

            GameObject chip = new GameObject("MapChip_" + i, typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(topBar.transform, false);
            RectTransform cRt = chip.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(x0, 0.16f);
            cRt.anchorMax = new Vector2(x1, 0.58f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            Image borderImg = chip.GetComponent<Image>();
            borderImg.color = new Color(mapChipColors[i].r, mapChipColors[i].g, mapChipColors[i].b, 0.35f);
            borderImg.raycastTarget = false;
            mapChipBorders[i] = borderImg;

            GameObject chipIn = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            chipIn.transform.SetParent(chip.transform, false);
            RectTransform ciRt = chipIn.GetComponent<RectTransform>();
            ciRt.anchorMin = Vector2.zero;
            ciRt.anchorMax = Vector2.one;
            ciRt.offsetMin = new Vector2(2f, 2f);
            ciRt.offsetMax = new Vector2(-2f, -2f);
            chipIn.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.14f, 0.92f);
            chipIn.GetComponent<Image>().raycastTarget = false;

            mapChipLabels[i] = CreateText("Label", chipIn.transform, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f),
                mapNames[i], 13f, 19f, FontStyles.Bold, TextAlignmentOptions.Center, mapChipColors[i]);
        }
    }

    void BuildCenterShowcase(Transform parent)
    {
        // ===== BEN TRAI: THE TRANG BI CHIEN DAU CUA NGUOI CHOI =====
        GameObject cardOuter = new GameObject("LoadoutCardBorder", typeof(RectTransform), typeof(Image));
        cardOuter.transform.SetParent(parent, false);
        RectTransform coRt = cardOuter.GetComponent<RectTransform>();
        coRt.anchorMin = new Vector2(0.045f, 0.255f);
        coRt.anchorMax = new Vector2(0.61f, 0.745f);
        coRt.offsetMin = Vector2.zero;
        coRt.offsetMax = Vector2.zero;
        Image coImg = cardOuter.GetComponent<Image>();
        coImg.color = new Color(modeAccent.r, modeAccent.g, modeAccent.b, 0.65f);
        coImg.raycastTarget = false;

        GameObject cardBody = new GameObject("LoadoutCardBody", typeof(RectTransform), typeof(Image));
        cardBody.transform.SetParent(cardOuter.transform, false);
        RectTransform cbRt = cardBody.GetComponent<RectTransform>();
        cbRt.anchorMin = Vector2.zero;
        cbRt.anchorMax = Vector2.one;
        cbRt.offsetMin = new Vector2(2.5f, 2.5f);
        cbRt.offsetMax = new Vector2(-2.5f, -2.5f);
        cardBody.GetComponent<Image>().color = new Color(0.045f, 0.07f, 0.13f, 0.88f);
        cardBody.GetComponent<Image>().raycastTarget = false;

        // Dai mau nhan phia tren the
        GameObject topStripe = new GameObject("TopStripe", typeof(RectTransform), typeof(Image));
        topStripe.transform.SetParent(cardBody.transform, false);
        RectTransform tsRt = topStripe.GetComponent<RectTransform>();
        tsRt.anchorMin = new Vector2(0f, 0.975f);
        tsRt.anchorMax = new Vector2(1f, 1f);
        tsRt.offsetMin = Vector2.zero;
        tsRt.offsetMax = Vector2.zero;
        topStripe.GetComponent<Image>().color = modeAccent;
        topStripe.GetComponent<Image>().raycastTarget = false;

        // Hang thong tin nguoi choi (Rank Icon + Ten + ELO)
        int elo = PlayerStatsManager.PlayerElo;
        string rankTitle = PlayerStatsManager.GetRankTitle(elo).ToUpper();
        Color rankCol = PlayerStatsManager.GetRankColor(elo);

        GameObject profileRow = new GameObject("ProfileRow", typeof(RectTransform), typeof(Image));
        profileRow.transform.SetParent(cardBody.transform, false);
        RectTransform prRt = profileRow.GetComponent<RectTransform>();
        prRt.anchorMin = new Vector2(0.03f, 0.72f);
        prRt.anchorMax = new Vector2(0.97f, 0.94f);
        prRt.offsetMin = Vector2.zero;
        prRt.offsetMax = Vector2.zero;
        profileRow.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.21f, 0.86f);
        profileRow.GetComponent<Image>().raycastTarget = false;

        GameObject rankIconGo = new GameObject("RankIcon", typeof(RectTransform), typeof(Image));
        rankIconGo.transform.SetParent(profileRow.transform, false);
        RectTransform riRt = rankIconGo.GetComponent<RectTransform>();
        riRt.anchorMin = new Vector2(0.02f, 0.08f);
        riRt.anchorMax = new Vector2(0.13f, 0.92f);
        riRt.offsetMin = Vector2.zero;
        riRt.offsetMax = Vector2.zero;
        Image riImg = rankIconGo.GetComponent<Image>();
        riImg.sprite = UIThemeAssets.GetRankIcon(elo);
        riImg.preserveAspect = true;
        riImg.raycastTarget = false;

        CreateText("PlayerName", profileRow.transform, new Vector2(0.145f, 0.44f), new Vector2(0.68f, 0.92f),
            PlayerStatsManager.PlayerName.ToUpper(), 20f, 30f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

        CreateText("PlayerRank", profileRow.transform, new Vector2(0.145f, 0.08f), new Vector2(0.68f, 0.46f),
            $"{rankTitle}  •  {elo} ELO", 14f, 20f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, rankCol);

        CreateText("ReadyBadge", profileRow.transform, new Vector2(0.70f, 0.20f), new Vector2(0.97f, 0.80f),
            "TRANG BI SAN SANG", 13f, 18f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Color(0.32f, 0.96f, 0.62f, 1f));

        // 3 O trang bi: NGOAI TRANG • VU KHI • KY NANG
        SkinDefinition skinDef = SkinCatalog.Get(PlayerStatsManager.EquippedSkin);
        WeaponDefinition wepDef = WeaponCatalog.Get(PlayerStatsManager.EquippedWeapon);
        SkillDefinition skillDef = SkillCatalog.Get(PlayerStatsManager.EquippedSkill);

        BuildLoadoutTile(
            cardBody.transform,
            new Vector2(0.03f, 0.07f),
            new Vector2(0.325f, 0.67f),
            "NGOAI TRANG",
            skinDef != null ? skinDef.displayName : "TAN BINH",
            UIThemeAssets.GetSkinIcon(PlayerStatsManager.EquippedSkin),
            skinDef != null ? skinDef.suitAccentColor : modeAccent
        );

        BuildLoadoutTile(
            cardBody.transform,
            new Vector2(0.352f, 0.07f),
            new Vector2(0.648f, 0.67f),
            "VU KHI",
            wepDef != null ? wepDef.displayName : "PHONG KIEM",
            UIThemeAssets.GetWeaponIcon(PlayerStatsManager.EquippedWeapon),
            wepDef != null ? wepDef.primaryColor : modeAccent
        );

        BuildLoadoutTile(
            cardBody.transform,
            new Vector2(0.675f, 0.07f),
            new Vector2(0.97f, 0.67f),
            "KY NANG",
            skillDef != null ? skillDef.displayName : "PHAN THAN",
            UIThemeAssets.GetSkillIcon(PlayerStatsManager.EquippedSkill),
            skillDef != null ? skillDef.tierColor : modeAccent
        );

        // ===== BEN PHAI: BIEU TUONG QUA CAU NANG LUONG FLASHBALL QUAY MUOT =====
        GameObject orbHolder = new GameObject("FlashBallCoreEmblem", typeof(RectTransform));
        orbHolder.transform.SetParent(parent, false);
        RectTransform ohRt = orbHolder.GetComponent<RectTransform>();
        ohRt.anchorMin = new Vector2(0.66f, 0.27f);
        ohRt.anchorMax = new Vector2(0.94f, 0.74f);
        ohRt.offsetMin = Vector2.zero;
        ohRt.offsetMax = Vector2.zero;

        // Vong hao quang ngoai xoay thuan chieu
        GameObject outerGo = new GameObject("OrbOuterRing", typeof(RectTransform), typeof(Image));
        outerGo.transform.SetParent(orbHolder.transform, false);
        orbOuterRing = outerGo.GetComponent<RectTransform>();
        orbOuterRing.anchorMin = new Vector2(0.5f, 0.54f);
        orbOuterRing.anchorMax = new Vector2(0.5f, 0.54f);
        orbOuterRing.sizeDelta = new Vector2(330f, 330f);
        Image outerImg = outerGo.GetComponent<Image>();
        outerImg.sprite = CreateTechRingSprite(modeAccent, true);
        outerImg.color = Color.white;
        outerImg.raycastTarget = false;

        // Vong luoi kiem ben trong xoay nguoc chieu
        GameObject innerGo = new GameObject("OrbInnerRing", typeof(RectTransform), typeof(Image));
        innerGo.transform.SetParent(orbHolder.transform, false);
        orbInnerRing = innerGo.GetComponent<RectTransform>();
        orbInnerRing.anchorMin = new Vector2(0.5f, 0.54f);
        orbInnerRing.anchorMax = new Vector2(0.5f, 0.54f);
        orbInnerRing.sizeDelta = new Vector2(238f, 238f);
        Image innerImg = innerGo.GetComponent<Image>();
        innerImg.sprite = CreateTechRingSprite(Color.Lerp(Color.white, modeAccent, 0.55f), false);
        innerImg.color = new Color(1f, 1f, 1f, 0.92f);
        innerImg.raycastTarget = false;

        // Loi qua cau Plasma phat sang nhip tho
        GameObject coreGo = new GameObject("OrbPlasmaCore", typeof(RectTransform), typeof(Image));
        coreGo.transform.SetParent(orbHolder.transform, false);
        orbCoreRt = coreGo.GetComponent<RectTransform>();
        orbCoreRt.anchorMin = new Vector2(0.5f, 0.54f);
        orbCoreRt.anchorMax = new Vector2(0.5f, 0.54f);
        orbCoreRt.sizeDelta = new Vector2(150f, 150f);
        orbCoreGlowImg = coreGo.GetComponent<Image>();
        orbCoreGlowImg.sprite = CreatePlasmaCoreSprite(modeAccent);
        orbCoreGlowImg.color = Color.white;
        orbCoreGlowImg.raycastTarget = false;

        // Tieu de nho duoi qua cau
        CreateText("CoreLabel", orbHolder.transform, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.16f),
            "DONG BO MAY CHU & DAU TRUONG", 14f, 20f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Color(0.84f, 0.92f, 1.00f, 0.92f));
    }

    void BuildLoadoutTile(
        Transform parent,
        Vector2 aMin,
        Vector2 aMax,
        string category,
        string itemName,
        Sprite icon,
        Color accentCol
    )
    {
        GameObject tile = new GameObject("Tile_" + category, typeof(RectTransform), typeof(Image));
        tile.transform.SetParent(parent, false);
        RectTransform rt = tile.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        tile.GetComponent<Image>().color = new Color(accentCol.r, accentCol.g, accentCol.b, 0.72f);
        tile.GetComponent<Image>().raycastTarget = false;

        GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(tile.transform, false);
        RectTransform inRt = inner.GetComponent<RectTransform>();
        inRt.anchorMin = Vector2.zero;
        inRt.anchorMax = Vector2.one;
        inRt.offsetMin = new Vector2(2f, 2f);
        inRt.offsetMax = new Vector2(-2f, -2f);
        inner.GetComponent<Image>().color = new Color(0.07f, 0.10f, 0.18f, 0.95f);
        inner.GetComponent<Image>().raycastTarget = false;

        CreateText("Category", inner.transform, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.96f),
            category, 12f, 16f, FontStyles.Bold, TextAlignmentOptions.Center, accentCol);

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(inner.transform, false);
        RectTransform icRt = iconGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0.12f, 0.24f);
        icRt.anchorMax = new Vector2(0.88f, 0.80f);
        icRt.offsetMin = Vector2.zero;
        icRt.offsetMax = Vector2.zero;
        Image icImg = iconGo.GetComponent<Image>();
        icImg.sprite = icon;
        icImg.preserveAspect = true;
        icImg.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0.12f);
        icImg.raycastTarget = false;

        CreateText("ItemName", inner.transform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.22f),
            itemName, 13f, 19f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
    }

    void BuildBottomProgressArea(Transform parent)
    {
        GameObject bottom = new GameObject("BottomProgressPanel", typeof(RectTransform), typeof(Image));
        bottom.transform.SetParent(parent, false);
        RectTransform bRt = bottom.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0f, 0f);
        bRt.anchorMax = new Vector2(1f, 0.215f);
        bRt.offsetMin = Vector2.zero;
        bRt.offsetMax = Vector2.zero;
        bottom.GetComponent<Image>().color = new Color(0.02f, 0.035f, 0.075f, 0.82f);
        bottom.GetComponent<Image>().raycastTarget = false;

        // Hang MEO CHIEN DAU (trai) + Trang thai & % (phai)
        GameObject tipTag = new GameObject("TipTag", typeof(RectTransform), typeof(Image));
        tipTag.transform.SetParent(bottom.transform, false);
        RectTransform ttRt = tipTag.GetComponent<RectTransform>();
        ttRt.anchorMin = new Vector2(0.045f, 0.56f);
        ttRt.anchorMax = new Vector2(0.155f, 0.82f);
        ttRt.offsetMin = Vector2.zero;
        ttRt.offsetMax = Vector2.zero;
        tipTag.GetComponent<Image>().color = new Color(modeAccent.r, modeAccent.g, modeAccent.b, 0.24f);
        tipTag.GetComponent<Image>().raycastTarget = false;

        CreateText("TipLabel", tipTag.transform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f),
            "MEO CHIEN DAU", 13f, 18f, FontStyles.Bold, TextAlignmentOptions.Center, modeAccent);

        GameObject tipHolder = new GameObject("TipBodyHolder", typeof(RectTransform), typeof(CanvasGroup));
        tipHolder.transform.SetParent(bottom.transform, false);
        RectTransform thRt = tipHolder.GetComponent<RectTransform>();
        thRt.anchorMin = new Vector2(0.168f, 0.52f);
        thRt.anchorMax = new Vector2(0.68f, 0.86f);
        thRt.offsetMin = Vector2.zero;
        thRt.offsetMax = Vector2.zero;
        tipCanvasGroup = tipHolder.GetComponent<CanvasGroup>();

        tipBodyText = CreateText("TipText", tipHolder.transform, Vector2.zero, Vector2.one,
            CombatTips[0], 15f, 22f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.88f, 0.93f, 1.00f, 0.96f));

        stageStatusText = CreateText("StageStatus", bottom.transform, new Vector2(0.68f, 0.56f), new Vector2(0.875f, 0.84f),
            "DANG KET NOI PHONG DAU...", 14f, 19f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Color(0.76f, 0.86f, 0.98f, 0.94f));

        modernPercentText = CreateText("PercentValue", bottom.transform, new Vector2(0.88f, 0.50f), new Vector2(0.955f, 0.90f),
            "0%", 24f, 38f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, modeAccent);

        // Thanh tien trinh Gradient Cyber co tia sang dan dau
        GameObject trackBorder = new GameObject("ProgressTrackBorder", typeof(RectTransform), typeof(Image));
        trackBorder.transform.SetParent(bottom.transform, false);
        RectTransform trRt = trackBorder.GetComponent<RectTransform>();
        trRt.anchorMin = new Vector2(0.045f, 0.24f);
        trRt.anchorMax = new Vector2(0.955f, 0.42f);
        trRt.offsetMin = Vector2.zero;
        trRt.offsetMax = Vector2.zero;
        trackBorder.GetComponent<Image>().color = new Color(modeAccent.r, modeAccent.g, modeAccent.b, 0.45f);
        trackBorder.GetComponent<Image>().raycastTarget = false;

        GameObject trackBg = new GameObject("ProgressTrackBg", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        trackBg.transform.SetParent(trackBorder.transform, false);
        RectTransform bgRt = trackBg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = new Vector2(2f, 2f);
        bgRt.offsetMax = new Vector2(-2f, -2f);
        trackBg.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.15f, 0.98f);
        trackBg.GetComponent<Image>().raycastTarget = false;

        GameObject fillGo = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(trackBg.transform, false);
        barFillRt = fillGo.GetComponent<RectTransform>();
        barFillRt.anchorMin = new Vector2(0f, 0f);
        barFillRt.anchorMax = new Vector2(0f, 1f);
        barFillRt.offsetMin = Vector2.zero;
        barFillRt.offsetMax = Vector2.zero;
        barFillImg = fillGo.GetComponent<Image>();
        barFillImg.sprite = CreateProgressGradientSprite(modeSecondary, modeAccent);
        barFillImg.color = Color.white;
        barFillImg.raycastTarget = false;

        // Dom sang trang ruc chay o dau mui thanh tien trinh
        GameObject flareGo = new GameObject("ProgressLeadingFlare", typeof(RectTransform), typeof(Image));
        flareGo.transform.SetParent(trackBorder.transform, false);
        barFlareRt = flareGo.GetComponent<RectTransform>();
        barFlareRt.anchorMin = new Vector2(0f, 0.5f);
        barFlareRt.anchorMax = new Vector2(0f, 0.5f);
        barFlareRt.sizeDelta = new Vector2(68f, 54f);
        Image flareImg = flareGo.GetComponent<Image>();
        flareImg.sprite = CreatePlasmaCoreSprite(modeAccent);
        flareImg.color = new Color(1f, 1f, 1f, 0.92f);
        flareImg.raycastTarget = false;
    }

    void BuildFloatingSparks(Transform parent)
    {
        Sprite sparkSprite = CreatePlasmaCoreSprite(modeAccent);
        GameObject holder = new GameObject("FloatingSparks", typeof(RectTransform));
        holder.transform.SetParent(parent, false);
        StretchFull(holder.GetComponent<RectTransform>());

        for (int i = 0; i < 18; i++)
        {
            GameObject sp = new GameObject("Spark_" + i, typeof(RectTransform), typeof(Image));
            sp.transform.SetParent(holder.transform, false);
            RectTransform rt = sp.GetComponent<RectTransform>();
            float sz = Random.Range(14f, 34f);
            rt.sizeDelta = new Vector2(sz, sz);
            Vector2 np = new Vector2(Random.Range(0.04f, 0.96f), Random.Range(0.05f, 0.95f));
            rt.anchorMin = np;
            rt.anchorMax = np;
            rt.anchoredPosition = Vector2.zero;

            Image img = sp.GetComponent<Image>();
            img.sprite = sparkSprite;
            img.raycastTarget = false;
            float alpha = Random.Range(0.22f, 0.58f);
            img.color = new Color(modeAccent.r, modeAccent.g, modeAccent.b, alpha);

            sparks.Add(new FloatingSpark
            {
                rt = rt,
                img = img,
                speedY = Random.Range(0.025f, 0.068f),
                driftX = Random.Range(-0.014f, 0.014f),
                phase = Random.Range(0f, 6.28f),
                baseAlpha = alpha,
                normPos = np
            });
        }
    }

    // ================= RUNTIME ANIMATIONS =================

    void UpdateProgressVisuals(float p)
    {
        float clamped = Mathf.Clamp01(p);
        if (barFillRt != null)
        {
            barFillRt.anchorMax = new Vector2(clamped, 1f);
        }

        if (barFlareRt != null)
        {
            barFlareRt.anchorMin = new Vector2(clamped, 0.5f);
            barFlareRt.anchorMax = new Vector2(clamped, 0.5f);
            barFlareRt.anchoredPosition = Vector2.zero;
            barFlareRt.gameObject.SetActive(clamped > 0.015f);
            float flareScale = 0.88f + 0.18f * Mathf.Sin(elapsedTime * 11f);
            barFlareRt.localScale = Vector3.one * flareScale;
        }

        int pct = Mathf.RoundToInt(clamped * 100f);
        if (modernPercentText != null)
        {
            modernPercentText.text = pct + "%";
        }

        if (stageStatusText != null)
        {
            if (pct < 25) stageStatusText.text = "DANG KET NOI PHONG DAU...";
            else if (pct < 55) stageStatusText.text = "DANG KHOI TAO DAU TRUONG & HIEU UNG...";
            else if (pct < 85) stageStatusText.text = "DANG DONG BO NGOAI TRANG & VU KHI...";
            else if (pct < 100) stageStatusText.text = "CHUAN BI VAO TRAN...";
            else stageStatusText.text = "SAN SANG CHIEN DAU!";
        }
    }

    void UpdateAmbientAnimations(float dt)
    {
        // Ken Burns zoom nhe nhang tren hinh nen
        if (bgZoomRt != null)
        {
            float zoom = 1.06f + 0.045f * Mathf.Sin(elapsedTime * 0.35f);
            bgZoomRt.localScale = Vector3.one * zoom;
            bgZoomRt.anchoredPosition = new Vector2(Mathf.Sin(elapsedTime * 0.25f) * 14f, Mathf.Cos(elapsedTime * 0.22f) * 8f);
        }

        // Bieu tuong qua cau nang luong FlashBall xoay tang toc theo tien trinh
        float speedBoost = 1f + visualProgress * 1.6f;
        if (orbOuterRing != null)
        {
            orbOuterRing.Rotate(0f, 0f, -38f * speedBoost * dt);
        }
        if (orbInnerRing != null)
        {
            orbInnerRing.Rotate(0f, 0f, 68f * speedBoost * dt);
        }
        if (orbCoreRt != null)
        {
            float pulse = 0.94f + 0.10f * Mathf.Sin(elapsedTime * (4.2f + visualProgress * 4f));
            orbCoreRt.localScale = Vector3.one * pulse;
        }

        // Cac dom nang luong bay len
        for (int i = 0; i < sparks.Count; i++)
        {
            FloatingSpark s = sparks[i];
            if (s.rt == null) continue;
            s.normPos.y += s.speedY * dt;
            s.normPos.x += (s.driftX + Mathf.Sin(elapsedTime * 1.4f + s.phase) * 0.008f) * dt;
            if (s.normPos.y > 1.05f)
            {
                s.normPos.y = -0.05f;
                s.normPos.x = Random.Range(0.04f, 0.96f);
            }
            s.rt.anchorMin = s.normPos;
            s.rt.anchorMax = s.normPos;
            if (s.img != null)
            {
                float a = s.baseAlpha * (0.65f + 0.35f * Mathf.Sin(elapsedTime * 3.2f + s.phase));
                s.img.color = new Color(modeAccent.r, modeAccent.g, modeAccent.b, a);
            }
            sparks[i] = s;
        }
    }

    void UpdateTipsRotation(float dt)
    {
        if (tipBodyText == null || tipCanvasGroup == null) return;

        const float tipDuration = 2.6f;
        tipTimer += dt;
        if (tipTimer >= tipDuration)
        {
            tipTimer = 0f;
            currentTipIndex = (currentTipIndex + 1) % CombatTips.Length;
            tipBodyText.text = CombatTips[currentTipIndex];
        }

        // Fade muot dau & cuoi moi cau meo
        float alpha = 1f;
        if (tipTimer < 0.25f) alpha = tipTimer / 0.25f;
        else if (tipTimer > tipDuration - 0.25f) alpha = (tipDuration - tipTimer) / 0.25f;
        tipCanvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    void UpdateMapScannerHighlight()
    {
        if (mapChipBorders == null || mapChipLabels == null) return;
        int activeIdx = Mathf.FloorToInt(elapsedTime * 2.2f) % mapChipBorders.Length;
        for (int i = 0; i < mapChipBorders.Length; i++)
        {
            if (mapChipBorders[i] == null) continue;
            Color c = mapChipColors[i];
            bool active = (i == activeIdx);
            mapChipBorders[i].color = active
                ? new Color(c.r, c.g, c.b, 0.98f)
                : new Color(c.r, c.g, c.b, 0.28f);
            if (mapChipLabels[i] != null)
            {
                mapChipLabels[i].color = active ? Color.white : new Color(c.r, c.g, c.b, 0.85f);
            }
        }
    }

    // ================= PROCEDURAL SPRITE HELPERS =================

    Sprite CreateVignetteSprite(Color tint, Color accent)
    {
        const int w = 256;
        const int h = 144;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / (w - 1);
                float dx = (u - 0.5f) * 2f;
                float dy = (v - 0.5f) * 2f;
                float rad = Mathf.Sqrt(dx * dx + dy * dy);

                float edgeDark = Mathf.Clamp01((rad - 0.35f) / 0.95f);
                float bottomDark = Mathf.Pow(1f - v, 2.2f) * 0.78f;
                float topDark = Mathf.Pow(v, 3.0f) * 0.42f;
                float scan = ((y % 4) == 0) ? 0.035f : 0f;

                float alpha = Mathf.Clamp01(0.32f + edgeDark * 0.52f + bottomDark + topDark + scan);
                Color col = Color.Lerp(new Color(0.02f, 0.035f, 0.075f, 1f), tint, 0.28f * (1f - edgeDark));
                // Anh sang accent nhe o goc phai giua (sau qua cau FlashBall)
                float orbGlow = Mathf.Exp(-(Mathf.Pow((u - 0.80f) / 0.22f, 2f) + Mathf.Pow((v - 0.52f) / 0.28f, 2f))) * 0.30f;
                col = Color.Lerp(col, accent, orbGlow);
                col.a = alpha;
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();
        runtimeAssets.Add(tex);

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        runtimeAssets.Add(sp);
        return sp;
    }

    Sprite CreateProgressGradientSprite(Color startCol, Color endCol)
    {
        const int w = 256;
        const int h = 16;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color hotTip = Color.Lerp(endCol, Color.white, 0.68f);
        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);
            float sheen = 0.84f + 0.16f * Mathf.Sin(v * Mathf.PI);
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / (w - 1);
                Color c = Color.Lerp(startCol, endCol, Mathf.Pow(u, 0.65f));
                if (u > 0.86f)
                {
                    c = Color.Lerp(c, hotTip, (u - 0.86f) / 0.14f);
                }
                // Cac vach cheo cyber tinh te chay doc thanh load
                float stripe = (((x + y * 2) % 24) < 5) ? 1.12f : 1.0f;
                c = new Color(Mathf.Clamp01(c.r * sheen * stripe), Mathf.Clamp01(c.g * sheen * stripe), Mathf.Clamp01(c.b * sheen * stripe), 1f);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        runtimeAssets.Add(tex);

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        runtimeAssets.Add(sp);
        return sp;
    }

    Sprite CreateTechRingSprite(Color col, bool outerStyle)
    {
        const int sz = 256;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float half = (sz - 1) * 0.5f;
        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);

                float alpha = 0f;
                if (outerStyle)
                {
                    float mainRing = Mathf.Exp(-Mathf.Pow((r - 0.86f) / 0.036f, 2f));
                    float segMask = Mathf.Sin(ang * 3f) > -0.25f ? 1f : 0.15f;
                    float tickRing = Mathf.Exp(-Mathf.Pow((r - 0.74f) / 0.018f, 2f)) * (Mathf.Abs(Mathf.Sin(ang * 12f)) > 0.72f ? 0.85f : 0f);
                    alpha = Mathf.Clamp01(mainRing * segMask + tickRing);
                }
                else
                {
                    // 2 vong cung luoi kiem doi xung
                    float slashRing = Mathf.Exp(-Mathf.Pow((r - 0.80f) / 0.045f, 2f));
                    float arc = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang)), 2.2f);
                    float thinInner = Mathf.Exp(-Mathf.Pow((r - 0.62f) / 0.018f, 2f)) * 0.55f;
                    alpha = Mathf.Clamp01(slashRing * arc + thinInner);
                }

                Color c = Color.Lerp(col, Color.white, alpha * 0.35f);
                c.a = alpha;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        runtimeAssets.Add(tex);

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
        runtimeAssets.Add(sp);
        return sp;
    }

    Sprite CreatePlasmaCoreSprite(Color col)
    {
        const int sz = 128;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float half = (sz - 1) * 0.5f;
        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r >= 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float core = Mathf.Exp(-r * r * 11f);
                float halo = Mathf.Exp(-r * r * 3.0f) * 0.85f;
                float intensity = Mathf.Clamp01(core + halo);
                Color c = Color.Lerp(col, Color.white, Mathf.Clamp01(core * 1.15f));
                c.a = intensity;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        runtimeAssets.Add(tex);

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
        runtimeAssets.Add(sp);
        return sp;
    }

    // ================= UI HELPERS =================

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    TMP_Text CreateText(
        string name,
        Transform parent,
        Vector2 aMin,
        Vector2 aMax,
        string content,
        float minSize,
        float maxSize,
        FontStyles style,
        TextAlignmentOptions align,
        Color color
    )
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) txt.font = uiFont;
        txt.text = content;
        txt.enableAutoSizing = true;
        txt.fontSizeMin = minSize;
        txt.fontSizeMax = maxSize;
        txt.fontStyle = style;
        txt.alignment = align;
        txt.color = color;
        txt.textWrappingMode = TextWrappingModes.Normal;
        txt.raycastTarget = false;
        return txt;
    }
}
