using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject findingMatchPanel;

    [Header("Texts")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI modeText;
    public TMP_Text playerStatsText;

    float timer;
    bool searching;
    bool menuUpgraded;
    bool findMatchUpgraded;

    TMP_Text topGoldText;
    TMP_Text topPlayerNameText;
    TMP_Text topRankTitleText;
    TMP_Text topRankNextText;
    Image topRankIcon;
    Image topRankFill;
    GameObject mailUnreadBadge;
    Image audioToggleBg;
    Image audioToggleIcon;
    Sprite audioOnSprite;
    Sprite audioOffSprite;
    TMP_Text equippedWeaponBadgeText;
    Image equippedWeaponIcon;
    TMP_Text equippedSkillBadgeText;
    Image equippedSkillIcon;
    TMP_Text equippedSkinBadgeText;
    Image equippedSkinIcon;

    // Modern FindMatch UI references
    Image fmCardBorderImg;
    Image fmTopStripeImg;
    Image fmModePillBg;
    TMP_Text fmModeBadgeText;
    TMP_Text fmModeSubText;
    TMP_Text fmStatusText;
    TMP_Text fmTimerText;
    TMP_Text fmSlotCountText;
    TMP_Text fmTipText;
    RectTransform fmRadarOuterRt;
    RectTransform fmRadarInnerRt;
    RectTransform fmRadarPulseRt;
    Image fmRadarOuterImg;
    Image fmRadarInnerImg;
    Image fmRadarPulseImg;
    Image fmRadarCenterIcon;
    RectTransform fmScanBarRt;
    Image fmScanBarImg;
    Image[] fmSlotDots;
    Image fmSkinIcon;
    TMP_Text fmSkinName;
    Image fmWeaponIcon;
    TMP_Text fmWeaponName;
    Image fmSkillIcon;
    TMP_Text fmSkillName;
    Color fmAccent = new Color(0.18f, 0.84f, 1.00f, 1f);
    string fmCurrentMode = "RANK";
    int fmTargetSlots = 4;
    int fmTipIndex;
    float fmTipTimer;
    readonly List<Object> menuRuntimeAssets = new List<Object>();

    static readonly string[] QueueTips = new string[]
    {
        "MEO: Bam PARRY dung khoanh khac bong ap sat de phan don toc do cuc cao!",
        "MEO: Giu nut PARRY de tu luc Power Parry danh bay bong cuc manh!",
        "MEO: Ket hop NHAY + DASH sang ngang de doi goc phan bong bat ngo!",
        "MEO: Moi ban do co co che rieng: Cau Lua, Bong Plasma va Set Tim phong to bong!",
        "MEO: Dung ky nang dung thoi diem de lat keo trong nhung pha can chien!"
    };

    // ================= INIT =================

    void Start()
    {
        AccountDatabaseService.EnsureInitialized();
        StopSearching();
        UpgradeMenuLayout();
        RefreshPlayerStats();

        if (OnboardingUI.Instance != null)
        {
            OnboardingUI.Instance.CheckFirstLaunchFlow();
        }
    }

    void OnEnable()
    {
        RefreshPlayerStats();
        RefreshAudioButtonVisual();
    }

    void OnDestroy()
    {
        for (int i = 0; i < menuRuntimeAssets.Count; i++)
        {
            if (menuRuntimeAssets[i] != null)
            {
                Destroy(menuRuntimeAssets[i]);
            }
        }
        menuRuntimeAssets.Clear();
    }

    public void RefreshPlayerStats()
    {
        int elo = PlayerStatsManager.PlayerElo;
        string tier = PlayerStatsManager.GetRankTitle(elo);
        Color tierCol = PlayerStatsManager.GetRankColor(elo);
        int gold = PlayerStatsManager.PlayerGold;
        WeaponDefinition eq = WeaponCatalog.Get(PlayerStatsManager.EquippedWeapon);
        SkillDefinition eqSkill = SkillCatalog.Get(PlayerStatsManager.EquippedSkill);
        SkinDefinition eqSkin = SkinCatalog.Get(PlayerStatsManager.EquippedSkin);

        if (topPlayerNameText != null)
        {
            string accountTag = AccountDatabaseService.IsLoggedIn ? "" : " (KHACH - DANG NHAP)";
            topPlayerNameText.text = $"{PlayerStatsManager.PlayerName}{accountTag}  •  {PlayerStatsManager.SoloWins}W-{PlayerStatsManager.SoloLosses}L";
        }

        if (topRankIcon != null)
        {
            topRankIcon.sprite = UIThemeAssets.GetRankIcon(elo);
        }

        if (topRankTitleText != null)
        {
            topRankTitleText.text = $"{tier.ToUpper()}  •  {elo} ELO";
            topRankTitleText.color = tierCol;
        }

        if (topRankNextText != null)
        {
            topRankNextText.text = PlayerStatsManager.FormatNextRankProgress(elo);
        }

        if (topRankFill != null)
        {
            topRankFill.fillAmount = PlayerStatsManager.GetRankProgressNormalized(elo);
            topRankFill.color = tierCol;
        }

        // Chỉ hiển thị số (không cần chữ "VANG" bên cạnh biểu tượng đồng vàng!)
        if (topGoldText != null)
        {
            topGoldText.text = gold.ToString("N0");
        }

        if (mailUnreadBadge != null)
        {
            mailUnreadBadge.SetActive(PlayerStatsManager.HasUnreadMail);
        }

        if (equippedWeaponBadgeText != null)
        {
            equippedWeaponBadgeText.text = eq.displayName;
            equippedWeaponBadgeText.color = eq.primaryColor;
        }

        if (equippedWeaponIcon != null)
        {
            equippedWeaponIcon.sprite = UIThemeAssets.GetWeaponIcon(eq.type);
        }

        if (equippedSkillBadgeText != null && eqSkill != null)
        {
            equippedSkillBadgeText.text = eqSkill.displayName;
            equippedSkillBadgeText.color = eqSkill.tierColor;
        }

        if (equippedSkillIcon != null && eqSkill != null)
        {
            equippedSkillIcon.sprite = UIThemeAssets.GetSkillIcon(eqSkill.type);
        }

        if (equippedSkinBadgeText != null && eqSkin != null)
        {
            equippedSkinBadgeText.text = eqSkin.displayName;
            equippedSkinBadgeText.color = eqSkin.suitAccentColor;
        }

        if (equippedSkinIcon != null && eqSkin != null)
        {
            equippedSkinIcon.sprite = UIThemeAssets.GetSkinIcon(eqSkin.type);
        }

        if (playerStatsText != null)
        {
            playerStatsText.text = PlayerStatsManager.FormatMenuStatsLine();
        }
    }

    // ================= MODERN MOBILE LANDSCAPE MENU =================

    void UpgradeMenuLayout()
    {
        if (menuUpgraded) return;
        menuUpgraded = true;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        TMP_FontAsset font = playerStatsText != null ? playerStatsText.font : (modeText != null ? modeText.font : null);

        // Nâng cấp nền Menu bằng ảnh đấu trường Cyber Blade Ball atmospheric + lớp tối mờ sang trọng
        Transform bgTf = canvas.transform.Find("BackGround");
        if (bgTf != null)
        {
            RectTransform bgRt = bgTf as RectTransform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgTf.GetComponent<Image>();
            if (bgImg != null && UIThemeAssets.BgMenuArena != null)
            {
                bgImg.sprite = UIThemeAssets.BgMenuArena;
                bgImg.color = Color.white;
            }
        }

        GameObject dimOverlay = new GameObject("MenuBackdropDim", typeof(RectTransform), typeof(Image));
        dimOverlay.transform.SetParent(canvas.transform, false);
        dimOverlay.transform.SetSiblingIndex(bgTf != null ? bgTf.GetSiblingIndex() + 1 : 0);
        RectTransform dRt = dimOverlay.GetComponent<RectTransform>();
        dRt.anchorMin = Vector2.zero;
        dRt.anchorMax = Vector2.one;
        dRt.offsetMin = Vector2.zero;
        dRt.offsetMax = Vector2.zero;
        Image dImg = dimOverlay.GetComponent<Image>();
        dImg.color = new Color(0.02f, 0.04f, 0.10f, 0.48f);
        dImg.raycastTarget = false;

        // Ẩn PlayerStatsBar cũ (đã thay bằng TopHeader hiện đại có Gold + Rank + Vũ khí)
        Transform oldStatsBar = canvas.transform.Find("PlayerStatsBar");
        if (oldStatsBar != null)
        {
            oldStatsBar.gameObject.SetActive(false);
        }

        // ===== 1. TOP HEADER BAR (82% -> 100%) =====
        GameObject topBar = new GameObject("MenuTopBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(canvas.transform, false);
        RectTransform tbRt = topBar.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0f, 0.83f);
        tbRt.anchorMax = new Vector2(1f, 1f);
        tbRt.offsetMin = Vector2.zero;
        tbRt.offsetMax = Vector2.zero;
        Image tbImg = topBar.GetComponent<Image>();
        tbImg.color = new Color(0.05f, 0.08f, 0.15f, 0.90f);
        tbImg.raycastTarget = false;

        // Viền mảnh dưới TopBar
        GameObject tbLine = new GameObject("Line", typeof(RectTransform), typeof(Image));
        tbLine.transform.SetParent(topBar.transform, false);
        RectTransform lRt = tbLine.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0f, 0f);
        lRt.anchorMax = new Vector2(1f, 0f);
        lRt.pivot = new Vector2(0.5f, 0f);
        lRt.sizeDelta = new Vector2(0f, 2f);
        Image lImg = tbLine.GetComponent<Image>();
        lImg.color = UIThemeAssets.CardBorder;
        lImg.raycastTarget = false;

        // Brand Logo & Account Badge + Logout Left
        CreateMenuText("BrandTitle", topBar.transform, new Vector2(0.02f, 0.44f), new Vector2(0.26f, 0.96f),
            "FLASH BALL", 13f, 20f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white, font);

        GameObject nameBtnGo = new GameObject("PlayerNameBadge", typeof(RectTransform), typeof(Image), typeof(Button));
        nameBtnGo.transform.SetParent(topBar.transform, false);
        RectTransform nbRt = nameBtnGo.GetComponent<RectTransform>();
        nbRt.anchorMin = new Vector2(0.02f, 0.06f);
        nbRt.anchorMax = new Vector2(0.26f, 0.44f);
        nbRt.offsetMin = Vector2.zero;
        nbRt.offsetMax = Vector2.zero;
        nameBtnGo.GetComponent<Image>().color = new Color(0.10f, 0.16f, 0.28f, 0.90f);
        nameBtnGo.GetComponent<Button>().onClick.AddListener(() => OnboardingUI.Instance?.OpenLogin(true));

        topPlayerNameText = CreateMenuText("PlayerNameTxt", nameBtnGo.transform, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f),
            "Player", 7.5f, 10f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.74f, 0.92f, 1f, 1f), font);

        // Nút Đăng Xuất gọn ngay cạnh tên tài khoản
        GameObject logoutGo = new GameObject("LogoutBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        logoutGo.transform.SetParent(topBar.transform, false);
        RectTransform loRt = logoutGo.GetComponent<RectTransform>();
        loRt.anchorMin = new Vector2(0.268f, 0.14f);
        loRt.anchorMax = new Vector2(0.318f, 0.86f);
        loRt.offsetMin = Vector2.zero;
        loRt.offsetMax = Vector2.zero;
        Image loImg = logoutGo.GetComponent<Image>();
        loImg.sprite = UIThemeAssets.IconLogout;
        loImg.preserveAspect = true;
        logoutGo.GetComponent<Button>().onClick.AddListener(OnClickLogout);

        // Center: Rank Emblem + Next-Rank Progress Bar
        GameObject rankPill = new GameObject("RankProgressPill", typeof(RectTransform), typeof(Image));
        rankPill.transform.SetParent(topBar.transform, false);
        RectTransform spRt = rankPill.GetComponent<RectTransform>();
        spRt.anchorMin = new Vector2(0.324f, 0.10f);
        spRt.anchorMax = new Vector2(0.602f, 0.90f);
        spRt.offsetMin = Vector2.zero;
        spRt.offsetMax = Vector2.zero;
        rankPill.GetComponent<Image>().color = new Color(0.09f, 0.14f, 0.24f, 0.96f);
        rankPill.GetComponent<Image>().raycastTarget = false;

        GameObject rIconGo = new GameObject("RankEmblem", typeof(RectTransform), typeof(Image));
        rIconGo.transform.SetParent(rankPill.transform, false);
        RectTransform riRt = rIconGo.GetComponent<RectTransform>();
        riRt.anchorMin = new Vector2(0.02f, 0.08f);
        riRt.anchorMax = new Vector2(0.21f, 0.92f);
        riRt.offsetMin = Vector2.zero;
        riRt.offsetMax = Vector2.zero;
        topRankIcon = rIconGo.GetComponent<Image>();
        topRankIcon.preserveAspect = true;
        topRankIcon.raycastTarget = false;

        topRankTitleText = CreateMenuText("RankTitle", rankPill.transform, new Vector2(0.22f, 0.56f), new Vector2(0.97f, 0.96f),
            "BRONZE • 100 ELO", 8f, 11f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white, font);

        GameObject rTrack = new GameObject("RankTrack", typeof(RectTransform), typeof(Image));
        rTrack.transform.SetParent(rankPill.transform, false);
        RectTransform rtRt = rTrack.GetComponent<RectTransform>();
        rtRt.anchorMin = new Vector2(0.22f, 0.34f);
        rtRt.anchorMax = new Vector2(0.96f, 0.54f);
        rtRt.offsetMin = Vector2.zero;
        rtRt.offsetMax = Vector2.zero;
        rTrack.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.14f, 1f);
        rTrack.GetComponent<Image>().raycastTarget = false;

        GameObject rFillGo = new GameObject("RankFill", typeof(RectTransform), typeof(Image));
        rFillGo.transform.SetParent(rTrack.transform, false);
        RectTransform rfRt = rFillGo.GetComponent<RectTransform>();
        rfRt.anchorMin = Vector2.zero;
        rfRt.anchorMax = Vector2.one;
        rfRt.offsetMin = Vector2.zero;
        rfRt.offsetMax = Vector2.zero;
        topRankFill = rFillGo.GetComponent<Image>();
        topRankFill.type = Image.Type.Filled;
        topRankFill.fillMethod = Image.FillMethod.Horizontal;
        topRankFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        topRankFill.raycastTarget = false;

        topRankNextText = CreateMenuText("RankNext", rankPill.transform, new Vector2(0.22f, 0.02f), new Vector2(0.97f, 0.34f),
            "100 / 250 -> SILVER", 7f, 9.5f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.76f, 0.88f, 0.98f, 0.95f), font);

        // Gold Pill (CHỈ SỐ + ICON ĐỒNG VÀNG + NÚT "+" XEM QUẢNG CÁO NHẬN +100!)
        GameObject goldPill = new GameObject("GoldPill", typeof(RectTransform), typeof(Image));
        goldPill.transform.SetParent(topBar.transform, false);
        RectTransform gpRt = goldPill.GetComponent<RectTransform>();
        gpRt.anchorMin = new Vector2(0.610f, 0.14f);
        gpRt.anchorMax = new Vector2(0.794f, 0.86f);
        gpRt.offsetMin = Vector2.zero;
        gpRt.offsetMax = Vector2.zero;
        goldPill.GetComponent<Image>().color = new Color(0.14f, 0.11f, 0.06f, 0.96f);
        goldPill.GetComponent<Image>().raycastTarget = false;

        GameObject coinGo = new GameObject("Coin", typeof(RectTransform), typeof(Image));
        coinGo.transform.SetParent(goldPill.transform, false);
        RectTransform cRt = coinGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.04f, 0.12f);
        cRt.anchorMax = new Vector2(0.26f, 0.88f);
        cRt.offsetMin = Vector2.zero;
        cRt.offsetMax = Vector2.zero;
        Image cImg = coinGo.GetComponent<Image>();
        cImg.sprite = UIThemeAssets.IconGold;
        cImg.preserveAspect = true;
        cImg.raycastTarget = false;

        topGoldText = CreateMenuText("GoldTxt", goldPill.transform, new Vector2(0.27f, 0.05f), new Vector2(0.74f, 0.95f),
            "0", 9f, 13.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, UIThemeAssets.GoldAccent, font);

        // Nút "+" xem quảng cáo nhận +100 ngay bên cạnh vàng!
        GameObject plusGo = new GameObject("AdPlusBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        plusGo.transform.SetParent(goldPill.transform, false);
        RectTransform plRt = plusGo.GetComponent<RectTransform>();
        plRt.anchorMin = new Vector2(0.75f, 0.10f);
        plRt.anchorMax = new Vector2(0.97f, 0.90f);
        plRt.offsetMin = Vector2.zero;
        plRt.offsetMax = Vector2.zero;
        Image plImg = plusGo.GetComponent<Image>();
        plImg.sprite = UIThemeAssets.IconPlus;
        plImg.preserveAspect = true;
        plusGo.GetComponent<Button>().onClick.AddListener(() => RewardedAdUI.Instance?.OpenAdModal());

        // Nút HÒM THƯ (kèm chấm đỏ thông báo Quà Tân Thủ!)
        GameObject mailGo = new GameObject("MailBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        mailGo.transform.SetParent(topBar.transform, false);
        RectTransform mlRt = mailGo.GetComponent<RectTransform>();
        mlRt.anchorMin = new Vector2(0.802f, 0.14f);
        mlRt.anchorMax = new Vector2(0.858f, 0.86f);
        mlRt.offsetMin = Vector2.zero;
        mlRt.offsetMax = Vector2.zero;
        Image mlImg = mailGo.GetComponent<Image>();
        mlImg.sprite = UIThemeAssets.IconMail;
        mlImg.preserveAspect = true;
        mailGo.GetComponent<Button>().onClick.AddListener(() => MailboxUI.Instance?.OpenMailbox());

        mailUnreadBadge = new GameObject("MailRedDot", typeof(RectTransform), typeof(Image));
        mailUnreadBadge.transform.SetParent(mailGo.transform, false);
        RectTransform dotRt = mailUnreadBadge.GetComponent<RectTransform>();
        dotRt.anchorMin = new Vector2(0.64f, 0.62f);
        dotRt.anchorMax = new Vector2(0.98f, 0.98f);
        dotRt.offsetMin = Vector2.zero;
        dotRt.offsetMax = Vector2.zero;
        Image dotImg = mailUnreadBadge.GetComponent<Image>();
        if (UIThemeAssets.BtnCircleFrame != null) dotImg.sprite = UIThemeAssets.BtnCircleFrame;
        dotImg.color = new Color(1f, 0.20f, 0.24f, 1f);
        dotImg.raycastTarget = false;
        CreateMenuText("DotOne", mailUnreadBadge.transform, Vector2.zero, Vector2.one, "1", 6f, 9f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, font);

        // Nút Bật / Tắt Âm Thanh gọn ngay cạnh Hòm Thư
        GameObject audioGo = new GameObject("AudioToggleBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        audioGo.transform.SetParent(topBar.transform, false);
        RectTransform auRt = audioGo.GetComponent<RectTransform>();
        auRt.anchorMin = new Vector2(0.865f, 0.14f);
        auRt.anchorMax = new Vector2(0.921f, 0.86f);
        auRt.offsetMin = Vector2.zero;
        auRt.offsetMax = Vector2.zero;
        audioToggleBg = audioGo.GetComponent<Image>();
        audioToggleBg.color = new Color(0.10f, 0.22f, 0.34f, 0.96f);
        Button auBtn = audioGo.GetComponent<Button>();
        auBtn.targetGraphic = audioToggleBg;
        auBtn.onClick.AddListener(OnClickToggleAudio);

        GameObject auIconGo = new GameObject("SpeakerIcon", typeof(RectTransform), typeof(Image));
        auIconGo.transform.SetParent(audioGo.transform, false);
        RectTransform auiRt = auIconGo.GetComponent<RectTransform>();
        auiRt.anchorMin = new Vector2(0.14f, 0.14f);
        auiRt.anchorMax = new Vector2(0.86f, 0.86f);
        auiRt.offsetMin = Vector2.zero;
        auiRt.offsetMax = Vector2.zero;
        audioToggleIcon = auIconGo.GetComponent<Image>();
        audioToggleIcon.preserveAspect = true;
        audioToggleIcon.raycastTarget = false;
        audioOnSprite = CreateSpeakerIconSprite(false);
        audioOffSprite = CreateSpeakerIconSprite(true);
        RefreshAudioButtonVisual();

        // Nút Hướng Dẫn "?" nhỏ gọn góc trên phải
        GameObject guideGo = new GameObject("GuideBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        guideGo.transform.SetParent(topBar.transform, false);
        RectTransform guRt = guideGo.GetComponent<RectTransform>();
        guRt.anchorMin = new Vector2(0.928f, 0.14f);
        guRt.anchorMax = new Vector2(0.984f, 0.86f);
        guRt.offsetMin = Vector2.zero;
        guRt.offsetMax = Vector2.zero;
        Image guImg = guideGo.GetComponent<Image>();
        guImg.sprite = UIThemeAssets.IconGuide;
        guImg.preserveAspect = true;
        guImg.raycastTarget = true;
        guideGo.GetComponent<Button>().onClick.AddListener(() => OnboardingUI.Instance?.OpenTutorial());

        // ===== 2. BLADE-BALL-STYLE ILLUSTRATED ACTION CARDS =====
        StyleActionCard(
            canvas.transform.Find("PlayRank"),
            new Vector2(0.04f, 0.44f),
            new Vector2(0.49f, 0.77f),
            new Color(0.06f, 0.14f, 0.28f, 0.96f),
            UIThemeAssets.CardBorder,
            "PLAY RANK",
            "",
            font,
            UIThemeAssets.BgCardRank
        );

        StyleActionCard(
            canvas.transform.Find("Solo"),
            new Vector2(0.04f, 0.09f),
            new Vector2(0.49f, 0.40f),
            new Color(0.09f, 0.10f, 0.22f, 0.96f),
            new Color(0.42f, 0.62f, 1.00f, 0.95f),
            "SOLO 1v1",
            "",
            font,
            UIThemeAssets.BgCardSolo
        );

        StyleActionCard(
            canvas.transform.Find("Custom"),
            new Vector2(0.52f, 0.49f),
            new Vector2(0.73f, 0.77f),
            new Color(0.09f, 0.14f, 0.26f, 0.92f),
            new Color(0.26f, 0.68f, 1.00f, 0.95f),
            "CUSTOM",
            "",
            font
        );

        StyleActionCard(
            canvas.transform.Find("Trainning"),
            new Vector2(0.75f, 0.49f),
            new Vector2(0.96f, 0.77f),
            new Color(0.08f, 0.16f, 0.24f, 0.92f),
            new Color(0.16f, 0.82f, 0.74f, 0.95f),
            "TRAINING",
            "",
            font
        );

        BuildWeaponShopMenuButton(canvas, font);
        BuildSkillShopMenuButton(canvas, font);
        BuildSkinShopMenuButton(canvas, font);
        BuildLeaderboardMenuButton(canvas, font);

        StyleActionCard(
            canvas.transform.Find("Exit"),
            new Vector2(0.75f, 0.09f),
            new Vector2(0.96f, 0.21f),
            UIThemeAssets.DangerRed,
            new Color(1f, 0.42f, 0.44f, 0.85f),
            "THOAT GAME",
            "",
            font
        );

        // Khởi tạo sẵn toàn bộ overlay đồng bộ & nâng cấp giao diện FindMatch hiện đại
        UpgradeFindMatchPanel(canvas, font);
        WeaponShopUI.EnsureInstance(canvas, font);
        SkillShopUI.EnsureInstance(canvas, font);
        SkinShopUI.EnsureInstance(canvas, font);
        LeaderboardUI.EnsureInstance(canvas, font);
        MailboxUI.EnsureInstance(canvas, font);
        RewardedAdUI.EnsureInstance(canvas, font);
        OnboardingUI.EnsureInstance(canvas, font);

        if (findingMatchPanel != null) findingMatchPanel.transform.SetAsLastSibling();
        Transform customRoot = canvas.transform.Find("CustomLobbyRoot");
        if (customRoot != null) customRoot.SetAsLastSibling();
        if (WeaponShopUI.Instance != null) WeaponShopUI.Instance.transform.SetAsLastSibling();
        if (SkillShopUI.Instance != null) SkillShopUI.Instance.transform.SetAsLastSibling();
        if (SkinShopUI.Instance != null) SkinShopUI.Instance.transform.SetAsLastSibling();
        if (LeaderboardUI.Instance != null) LeaderboardUI.Instance.transform.SetAsLastSibling();
        if (MailboxUI.Instance != null) MailboxUI.Instance.transform.SetAsLastSibling();
        if (RewardedAdUI.Instance != null) RewardedAdUI.Instance.transform.SetAsLastSibling();
        if (OnboardingUI.Instance != null) OnboardingUI.Instance.transform.SetAsLastSibling();
    }

    public void OnClickLogout()
    {
        AccountDatabaseService.Logout();
        RefreshPlayerStats();
        OnboardingUI.Instance?.OpenLogin(true);
    }

    public void OnClickToggleAudio()
    {
        GameAudioManager.ToggleMute();
        RefreshAudioButtonVisual();
    }

    void RefreshAudioButtonVisual()
    {
        bool muted = GameAudioManager.IsMuted;
        if (audioToggleBg != null)
        {
            audioToggleBg.color = muted
                ? new Color(0.32f, 0.10f, 0.14f, 0.96f)
                : new Color(0.08f, 0.22f, 0.34f, 0.96f);
        }
        if (audioToggleIcon != null)
        {
            audioToggleIcon.sprite = muted ? audioOffSprite : audioOnSprite;
            audioToggleIcon.color = muted
                ? new Color(1.00f, 0.44f, 0.46f, 1f)
                : new Color(0.36f, 0.95f, 1.00f, 1f);
        }
    }

    void BuildLeaderboardMenuButton(Canvas canvas, TMP_FontAsset font)
    {
        Transform existing = canvas.transform.Find("LeaderboardButton");
        if (existing != null) return;

        GameObject lbGo = new GameObject("LeaderboardButton", typeof(RectTransform), typeof(Image), typeof(Button));
        lbGo.transform.SetParent(canvas.transform, false);
        RectTransform rt = lbGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.52f, 0.09f);
        rt.anchorMax = new Vector2(0.73f, 0.21f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image borderImg = lbGo.GetComponent<Image>();
        borderImg.color = UIThemeAssets.CardBorder;
        borderImg.raycastTarget = true;

        Button btn = lbGo.GetComponent<Button>();
        btn.targetGraphic = borderImg;
        btn.onClick.AddListener(() => LeaderboardUI.Instance?.OpenLeaderboard(false));

        GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(lbGo.transform, false);
        RectTransform inRt = inner.GetComponent<RectTransform>();
        inRt.anchorMin = Vector2.zero;
        inRt.anchorMax = Vector2.one;
        inRt.offsetMin = new Vector2(2f, 2f);
        inRt.offsetMax = new Vector2(-2f, -2f);
        inner.GetComponent<Image>().color = new Color(0.09f, 0.16f, 0.28f, 0.96f);
        inner.GetComponent<Image>().raycastTarget = false;

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(inner.transform, false);
        RectTransform icRt = iconGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0.06f, 0.14f);
        icRt.anchorMax = new Vector2(0.28f, 0.86f);
        icRt.offsetMin = Vector2.zero;
        icRt.offsetMax = Vector2.zero;
        Image icImg = iconGo.GetComponent<Image>();
        icImg.sprite = UIThemeAssets.IconLeaderboard;
        icImg.preserveAspect = true;
        icImg.raycastTarget = false;

        CreateMenuText("Label", inner.transform, new Vector2(0.30f, 0.08f), new Vector2(0.96f, 0.92f),
            "BANG XEP HANG", 8.5f, 12.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white, font);
    }

    void BuildWeaponShopMenuButton(Canvas canvas, TMP_FontAsset font)
    {
        Transform existing = canvas.transform.Find("WeaponShopButton");
        if (existing != null) return;

        GameObject shopGo = new GameObject("WeaponShopButton", typeof(RectTransform), typeof(Image), typeof(Button));
        shopGo.transform.SetParent(canvas.transform, false);
        RectTransform rt = shopGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.52f, 0.24f);
        rt.anchorMax = new Vector2(0.66f, 0.46f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image borderImg = shopGo.GetComponent<Image>();
        borderImg.color = UIThemeAssets.GoldAccent;
        borderImg.raycastTarget = true;

        Button btn = shopGo.GetComponent<Button>();
        btn.targetGraphic = borderImg;
        btn.onClick.AddListener(OpenWeaponShop);

        GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(shopGo.transform, false);
        RectTransform inRt = inner.GetComponent<RectTransform>();
        inRt.anchorMin = Vector2.zero;
        inRt.anchorMax = Vector2.one;
        inRt.offsetMin = new Vector2(2f, 2f);
        inRt.offsetMax = new Vector2(-2f, -2f);
        Image inImg = inner.GetComponent<Image>();
        inImg.color = new Color(0.16f, 0.13f, 0.08f, 0.96f);
        inImg.raycastTarget = false;

        GameObject iconGo = new GameObject("EquippedIcon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(inner.transform, false);
        RectTransform icRt = iconGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0.18f, 0.40f);
        icRt.anchorMax = new Vector2(0.82f, 0.92f);
        icRt.offsetMin = Vector2.zero;
        icRt.offsetMax = Vector2.zero;
        equippedWeaponIcon = iconGo.GetComponent<Image>();
        equippedWeaponIcon.preserveAspect = true;
        equippedWeaponIcon.raycastTarget = false;

        CreateMenuText("ShopLabel", inner.transform, new Vector2(0.04f, 0.20f), new Vector2(0.96f, 0.40f),
            "VU KHI", 8f, 12f, FontStyles.Bold, TextAlignmentOptions.Center, UIThemeAssets.GoldAccent, font);

        equippedWeaponBadgeText = CreateMenuText("EquippedLabel", inner.transform, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.20f),
            "PHONG KIEM", 6.5f, 9.5f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, font);
    }

    void BuildSkillShopMenuButton(Canvas canvas, TMP_FontAsset font)
    {
        Transform existing = canvas.transform.Find("SkillShopButton");
        if (existing != null) return;

        GameObject skillGo = new GameObject("SkillShopButton", typeof(RectTransform), typeof(Image), typeof(Button));
        skillGo.transform.SetParent(canvas.transform, false);
        RectTransform rt = skillGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.67f, 0.24f);
        rt.anchorMax = new Vector2(0.81f, 0.46f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image borderImg = skillGo.GetComponent<Image>();
        borderImg.color = new Color(0.74f, 0.34f, 1.00f, 1f);
        borderImg.raycastTarget = true;

        Button btn = skillGo.GetComponent<Button>();
        btn.targetGraphic = borderImg;
        btn.onClick.AddListener(OpenSkillShop);

        GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(skillGo.transform, false);
        RectTransform inRt = inner.GetComponent<RectTransform>();
        inRt.anchorMin = Vector2.zero;
        inRt.anchorMax = Vector2.one;
        inRt.offsetMin = new Vector2(2f, 2f);
        inRt.offsetMax = new Vector2(-2f, -2f);
        Image inImg = inner.GetComponent<Image>();
        inImg.color = new Color(0.13f, 0.09f, 0.22f, 0.96f);
        inImg.raycastTarget = false;

        GameObject iconGo = new GameObject("EquippedSkillIcon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(inner.transform, false);
        RectTransform icRt = iconGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0.18f, 0.40f);
        icRt.anchorMax = new Vector2(0.82f, 0.92f);
        icRt.offsetMin = Vector2.zero;
        icRt.offsetMax = Vector2.zero;
        equippedSkillIcon = iconGo.GetComponent<Image>();
        equippedSkillIcon.preserveAspect = true;
        equippedSkillIcon.raycastTarget = false;

        CreateMenuText("SkillShopLabel", inner.transform, new Vector2(0.04f, 0.20f), new Vector2(0.96f, 0.40f),
            "KY NANG", 8f, 12f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.84f, 0.54f, 1.00f, 1f), font);

        equippedSkillBadgeText = CreateMenuText("EquippedSkillLabel", inner.transform, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.20f),
            "CLONE BALL", 6.5f, 9.5f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, font);
    }

    void BuildSkinShopMenuButton(Canvas canvas, TMP_FontAsset font)
    {
        Transform existing = canvas.transform.Find("SkinShopButton");
        if (existing != null) return;

        GameObject skinGo = new GameObject("SkinShopButton", typeof(RectTransform), typeof(Image), typeof(Button));
        skinGo.transform.SetParent(canvas.transform, false);
        RectTransform rt = skinGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.82f, 0.24f);
        rt.anchorMax = new Vector2(0.96f, 0.46f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image borderImg = skinGo.GetComponent<Image>();
        borderImg.color = new Color(0.16f, 0.88f, 0.84f, 1f);
        borderImg.raycastTarget = true;

        Button btn = skinGo.GetComponent<Button>();
        btn.targetGraphic = borderImg;
        btn.onClick.AddListener(OpenSkinShop);

        GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(skinGo.transform, false);
        RectTransform inRt = inner.GetComponent<RectTransform>();
        inRt.anchorMin = Vector2.zero;
        inRt.anchorMax = Vector2.one;
        inRt.offsetMin = new Vector2(2f, 2f);
        inRt.offsetMax = new Vector2(-2f, -2f);
        Image inImg = inner.GetComponent<Image>();
        inImg.color = new Color(0.07f, 0.16f, 0.22f, 0.96f);
        inImg.raycastTarget = false;

        GameObject iconGo = new GameObject("EquippedSkinIcon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(inner.transform, false);
        RectTransform icRt = iconGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0.18f, 0.40f);
        icRt.anchorMax = new Vector2(0.82f, 0.92f);
        icRt.offsetMin = Vector2.zero;
        icRt.offsetMax = Vector2.zero;
        equippedSkinIcon = iconGo.GetComponent<Image>();
        equippedSkinIcon.preserveAspect = true;
        equippedSkinIcon.raycastTarget = false;

        CreateMenuText("SkinShopLabel", inner.transform, new Vector2(0.04f, 0.20f), new Vector2(0.96f, 0.40f),
            "NGOAI TRANG", 7.5f, 11.5f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.32f, 0.95f, 0.92f, 1f), font);

        equippedSkinBadgeText = CreateMenuText("EquippedSkinLabel", inner.transform, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.20f),
            "TAN BINH", 6.5f, 9.5f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, font);
    }

    void StyleActionCard(
        Transform btnTf,
        Vector2 aMin,
        Vector2 aMax,
        Color bodyColor,
        Color borderColor,
        string title,
        string subtitle,
        TMP_FontAsset font,
        Sprite bgArtwork = null
    )
    {
        if (btnTf == null) return;

        RectTransform rt = btnTf as RectTransform;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Ẩn text cũ đơn điệu
        TMP_Text[] oldTmps = btnTf.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text t in oldTmps) t.gameObject.SetActive(false);
        Text[] oldLegacy = btnTf.GetComponentsInChildren<Text>(true);
        foreach (Text t in oldLegacy) t.gameObject.SetActive(false);

        Image border = btnTf.GetComponent<Image>();
        if (border == null) border = btnTf.gameObject.AddComponent<Image>();
        border.sprite = null;
        border.color = borderColor;
        border.raycastTarget = true;

        Button btn = btnTf.GetComponent<Button>();
        if (btn != null) btn.targetGraphic = border;

        GameObject inner = new GameObject("CardFill", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        inner.transform.SetParent(btnTf, false);
        RectTransform inRt = inner.GetComponent<RectTransform>();
        inRt.anchorMin = Vector2.zero;
        inRt.anchorMax = Vector2.one;
        inRt.offsetMin = new Vector2(2f, 2f);
        inRt.offsetMax = new Vector2(-2f, -2f);
        Image inImg = inner.GetComponent<Image>();
        inImg.color = bodyColor;
        inImg.raycastTarget = false;

        // Chèn ảnh combat mờ nghệ thuật phong cách Blade Ball bên dưới chữ!
        if (bgArtwork != null)
        {
            GameObject artGo = new GameObject("CardArtwork", typeof(RectTransform), typeof(Image));
            artGo.transform.SetParent(inner.transform, false);
            RectTransform aRt = artGo.GetComponent<RectTransform>();
            aRt.anchorMin = Vector2.zero;
            aRt.anchorMax = Vector2.one;
            aRt.offsetMin = Vector2.zero;
            aRt.offsetMax = Vector2.zero;
            Image aImg = artGo.GetComponent<Image>();
            aImg.sprite = bgArtwork;
            aImg.color = new Color(1f, 1f, 1f, 0.72f);
            aImg.raycastTarget = false;

            // Lớp phủ tối dịu phía dưới để chữ nổi bật cực rõ
            GameObject scrimGo = new GameObject("Scrim", typeof(RectTransform), typeof(Image));
            scrimGo.transform.SetParent(inner.transform, false);
            RectTransform scRt = scrimGo.GetComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0f, 0f);
            scRt.anchorMax = new Vector2(1f, 0.44f);
            scRt.offsetMin = Vector2.zero;
            scRt.offsetMax = Vector2.zero;
            Image scImg = scrimGo.GetComponent<Image>();
            scImg.color = new Color(0.03f, 0.06f, 0.14f, 0.78f);
            scImg.raycastTarget = false;
        }

        // Dải nhấn sáng trên cùng kiểu Blade Ball
        GameObject topGloss = new GameObject("TopGloss", typeof(RectTransform), typeof(Image));
        topGloss.transform.SetParent(inner.transform, false);
        RectTransform tgRt = topGloss.GetComponent<RectTransform>();
        tgRt.anchorMin = new Vector2(0f, 0.94f);
        tgRt.anchorMax = new Vector2(1f, 1f);
        tgRt.offsetMin = Vector2.zero;
        tgRt.offsetMax = Vector2.zero;
        Image tgImg = topGloss.GetComponent<Image>();
        tgImg.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0.55f);
        tgImg.raycastTarget = false;

        bool hasSub = !string.IsNullOrEmpty(subtitle);
        Vector2 tMin = bgArtwork != null ? new Vector2(0.06f, 0.04f) : new Vector2(0.06f, hasSub ? 0.44f : 0.08f);
        Vector2 tMax = bgArtwork != null ? new Vector2(0.94f, 0.42f) : new Vector2(0.94f, hasSub ? 0.90f : 0.92f);

        CreateMenuText(
            "Title",
            inner.transform,
            tMin,
            tMax,
            title,
            11f,
            22f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            Color.white,
            font
        );

        if (hasSub)
        {
            CreateMenuText(
                "Sub",
                inner.transform,
                new Vector2(0.06f, 0.08f),
                new Vector2(0.94f, 0.44f),
                subtitle,
                7.5f,
                11f,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Color(0.84f, 0.92f, 1.00f, 0.92f),
                font
            );
        }
    }

    static TMP_Text CreateMenuText(
        string name,
        Transform parent,
        Vector2 aMin,
        Vector2 aMax,
        string content,
        float fsMin,
        float fsMax,
        FontStyles style,
        TextAlignmentOptions align,
        Color col,
        TMP_FontAsset font
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
        if (font != null) txt.font = font;
        txt.text = content;
        txt.enableAutoSizing = true;
        txt.fontSizeMin = fsMin;
        txt.fontSizeMax = fsMax;
        txt.fontStyle = style;
        txt.alignment = align;
        txt.color = col;
        txt.raycastTarget = false;
        return txt;
    }

    // ================= MODERN CYBER FIND-MATCH UI =================

    void UpgradeFindMatchPanel(Canvas canvas, TMP_FontAsset font)
    {
        if (findMatchUpgraded || canvas == null)
            return;

        findMatchUpgraded = true;

        if (findingMatchPanel == null)
        {
            findingMatchPanel = new GameObject("FindingMatchPanel", typeof(RectTransform), typeof(Image));
            findingMatchPanel.transform.SetParent(canvas.transform, false);
        }

        RectTransform panelRt = findingMatchPanel.GetComponent<RectTransform>();
        if (panelRt != null)
        {
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.localScale = Vector3.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
        }

        Image backdrop = findingMatchPanel.GetComponent<Image>();
        if (backdrop == null) backdrop = findingMatchPanel.AddComponent<Image>();
        backdrop.sprite = null;
        backdrop.color = new Color(0.02f, 0.04f, 0.09f, 0.88f);
        backdrop.raycastTarget = true;

        // Ẩn toàn bộ phần tử FindMatch cũ đơn giản để thay bằng giao diện Cyber-Esports hiện đại
        for (int i = 0; i < findingMatchPanel.transform.childCount; i++)
        {
            Transform ch = findingMatchPanel.transform.GetChild(i);
            if (ch != null && ch.name != "ModernFindMatchCard")
            {
                ch.gameObject.SetActive(false);
            }
        }

        // 1. Khung trung tâm Cyber-Esports responsive
        GameObject cardBorder = new GameObject("ModernFindMatchCard", typeof(RectTransform), typeof(Image));
        cardBorder.transform.SetParent(findingMatchPanel.transform, false);
        RectTransform cbRt = cardBorder.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(0.14f, 0.13f);
        cbRt.anchorMax = new Vector2(0.86f, 0.87f);
        cbRt.offsetMin = Vector2.zero;
        cbRt.offsetMax = Vector2.zero;
        fmCardBorderImg = cardBorder.GetComponent<Image>();
        fmCardBorderImg.color = fmAccent;
        fmCardBorderImg.raycastTarget = false;

        GameObject cardBody = new GameObject("Body", typeof(RectTransform), typeof(Image));
        cardBody.transform.SetParent(cardBorder.transform, false);
        RectTransform bdRt = cardBody.GetComponent<RectTransform>();
        bdRt.anchorMin = Vector2.zero;
        bdRt.anchorMax = Vector2.one;
        bdRt.offsetMin = new Vector2(2.5f, 2.5f);
        bdRt.offsetMax = new Vector2(-2.5f, -2.5f);
        cardBody.GetComponent<Image>().color = new Color(0.052f, 0.082f, 0.155f, 0.98f);
        cardBody.GetComponent<Image>().raycastTarget = false;

        // Dải sáng trên cùng theo màu chế độ đấu
        GameObject topStripe = new GameObject("TopStripe", typeof(RectTransform), typeof(Image));
        topStripe.transform.SetParent(cardBody.transform, false);
        RectTransform tsRt = topStripe.GetComponent<RectTransform>();
        tsRt.anchorMin = new Vector2(0f, 0.975f);
        tsRt.anchorMax = new Vector2(1f, 1f);
        tsRt.offsetMin = Vector2.zero;
        tsRt.offsetMax = Vector2.zero;
        fmTopStripeImg = topStripe.GetComponent<Image>();
        fmTopStripeImg.color = fmAccent;
        fmTopStripeImg.raycastTarget = false;

        // 2. HEADER ROW (0.81 -> 0.97)
        GameObject header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(cardBody.transform, false);
        RectTransform hdRt = header.GetComponent<RectTransform>();
        hdRt.anchorMin = new Vector2(0f, 0.81f);
        hdRt.anchorMax = new Vector2(1f, 0.975f);
        hdRt.offsetMin = Vector2.zero;
        hdRt.offsetMax = Vector2.zero;
        header.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.22f, 0.94f);
        header.GetComponent<Image>().raycastTarget = false;

        GameObject modePill = new GameObject("ModePill", typeof(RectTransform), typeof(Image));
        modePill.transform.SetParent(header.transform, false);
        RectTransform mpRt = modePill.GetComponent<RectTransform>();
        mpRt.anchorMin = new Vector2(0.03f, 0.16f);
        mpRt.anchorMax = new Vector2(0.32f, 0.84f);
        mpRt.offsetMin = Vector2.zero;
        mpRt.offsetMax = Vector2.zero;
        fmModePillBg = modePill.GetComponent<Image>();
        fmModePillBg.color = new Color(fmAccent.r, fmAccent.g, fmAccent.b, 0.22f);
        fmModePillBg.raycastTarget = false;

        fmModeBadgeText = CreateMenuText("ModeBadge", modePill.transform, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f),
            "RANKED", 10f, 16f, FontStyles.Bold, TextAlignmentOptions.Center, fmAccent, font);

        fmModeSubText = CreateMenuText("ModeSub", header.transform, new Vector2(0.34f, 0.14f), new Vector2(0.73f, 0.86f),
            "DAU TRUONG SINH TU  •  TRANH DIEM ELO", 8f, 12.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
            new Color(0.84f, 0.91f, 1.00f, 0.95f), font);

        CreateMenuText("ServerTag", header.transform, new Vector2(0.74f, 0.16f), new Vector2(0.97f, 0.84f),
            "ASIA  •  CLOUD", 7.5f, 11f, FontStyles.Bold, TextAlignmentOptions.MidlineRight,
            new Color(0.36f, 0.96f, 0.68f, 0.96f), font);

        // 3. LEFT COLUMN: RADAR QUÉT ĐỐI THỦ ĐỘNG (0.03 -> 0.36, 0.21 -> 0.78)
        GameObject radarBox = new GameObject("RadarPanel", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        radarBox.transform.SetParent(cardBody.transform, false);
        RectTransform rbRt = radarBox.GetComponent<RectTransform>();
        rbRt.anchorMin = new Vector2(0.03f, 0.21f);
        rbRt.anchorMax = new Vector2(0.36f, 0.78f);
        rbRt.offsetMin = Vector2.zero;
        rbRt.offsetMax = Vector2.zero;
        radarBox.GetComponent<Image>().color = new Color(0.035f, 0.058f, 0.118f, 0.94f);
        radarBox.GetComponent<Image>().raycastTarget = false;

        Sprite outerRingSp = CreateRadarRingSprite(true);
        Sprite innerRingSp = CreateRadarRingSprite(false);
        Sprite pulseSp = CreateSoftCircleGlowSprite();

        // Sóng radar lan tỏa
        GameObject pulseGo = new GameObject("RadarPulse", typeof(RectTransform), typeof(Image));
        pulseGo.transform.SetParent(radarBox.transform, false);
        fmRadarPulseRt = pulseGo.GetComponent<RectTransform>();
        fmRadarPulseRt.anchorMin = new Vector2(0.12f, 0.16f);
        fmRadarPulseRt.anchorMax = new Vector2(0.88f, 0.92f);
        fmRadarPulseRt.offsetMin = Vector2.zero;
        fmRadarPulseRt.offsetMax = Vector2.zero;
        fmRadarPulseImg = pulseGo.GetComponent<Image>();
        fmRadarPulseImg.sprite = pulseSp;
        fmRadarPulseImg.preserveAspect = true;
        fmRadarPulseImg.color = new Color(fmAccent.r, fmAccent.g, fmAccent.b, 0.32f);
        fmRadarPulseImg.raycastTarget = false;

        // Vòng ngoài xoay thuận chiều
        GameObject outerGo = new GameObject("RadarOuter", typeof(RectTransform), typeof(Image));
        outerGo.transform.SetParent(radarBox.transform, false);
        fmRadarOuterRt = outerGo.GetComponent<RectTransform>();
        fmRadarOuterRt.anchorMin = new Vector2(0.10f, 0.16f);
        fmRadarOuterRt.anchorMax = new Vector2(0.90f, 0.94f);
        fmRadarOuterRt.offsetMin = Vector2.zero;
        fmRadarOuterRt.offsetMax = Vector2.zero;
        fmRadarOuterImg = outerGo.GetComponent<Image>();
        fmRadarOuterImg.sprite = outerRingSp;
        fmRadarOuterImg.preserveAspect = true;
        fmRadarOuterImg.color = fmAccent;
        fmRadarOuterImg.raycastTarget = false;

        // Vòng trong xoay ngược chiều
        GameObject innerGo = new GameObject("RadarInner", typeof(RectTransform), typeof(Image));
        innerGo.transform.SetParent(radarBox.transform, false);
        fmRadarInnerRt = innerGo.GetComponent<RectTransform>();
        fmRadarInnerRt.anchorMin = new Vector2(0.22f, 0.28f);
        fmRadarInnerRt.anchorMax = new Vector2(0.78f, 0.82f);
        fmRadarInnerRt.offsetMin = Vector2.zero;
        fmRadarInnerRt.offsetMax = Vector2.zero;
        fmRadarInnerImg = innerGo.GetComponent<Image>();
        fmRadarInnerImg.sprite = innerRingSp;
        fmRadarInnerImg.preserveAspect = true;
        fmRadarInnerImg.color = Color.Lerp(Color.white, fmAccent, 0.55f);
        fmRadarInnerImg.raycastTarget = false;

        // Biểu tượng trung tâm (Rank Emblem / Parry Icon)
        GameObject centerIcGo = new GameObject("RadarCenterIcon", typeof(RectTransform), typeof(Image));
        centerIcGo.transform.SetParent(radarBox.transform, false);
        RectTransform ciRt = centerIcGo.GetComponent<RectTransform>();
        ciRt.anchorMin = new Vector2(0.32f, 0.36f);
        ciRt.anchorMax = new Vector2(0.68f, 0.74f);
        ciRt.offsetMin = Vector2.zero;
        ciRt.offsetMax = Vector2.zero;
        fmRadarCenterIcon = centerIcGo.GetComponent<Image>();
        fmRadarCenterIcon.preserveAspect = true;
        fmRadarCenterIcon.raycastTarget = false;

        CreateMenuText("RadarCaption", radarBox.transform, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.14f),
            "DANG QUET PHONG DAU", 7f, 10f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Color(0.76f, 0.88f, 1.00f, 0.88f), font);

        // 4. RIGHT COLUMN: TRẠNG THÁI + ĐỒNG HỒ + TRANG BỊ HIỆN TẠI (0.38 -> 0.97, 0.21 -> 0.78)
        GameObject rightCol = new GameObject("MatchInfoCol", typeof(RectTransform));
        rightCol.transform.SetParent(cardBody.transform, false);
        RectTransform rcRt = rightCol.GetComponent<RectTransform>();
        rcRt.anchorMin = new Vector2(0.38f, 0.21f);
        rcRt.anchorMax = new Vector2(0.97f, 0.78f);
        rcRt.offsetMin = Vector2.zero;
        rcRt.offsetMax = Vector2.zero;

        // 4a. Khung Trạng Thái & Slot người chơi (0.70 -> 1.00)
        GameObject statusBox = new GameObject("StatusBox", typeof(RectTransform), typeof(Image));
        statusBox.transform.SetParent(rightCol.transform, false);
        RectTransform sbRt = statusBox.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0f, 0.70f);
        sbRt.anchorMax = new Vector2(1f, 1f);
        sbRt.offsetMin = Vector2.zero;
        sbRt.offsetMax = Vector2.zero;
        statusBox.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.22f, 0.92f);
        statusBox.GetComponent<Image>().raycastTarget = false;

        fmStatusText = CreateMenuText("LiveStatus", statusBox.transform, new Vector2(0.03f, 0.50f), new Vector2(0.97f, 0.94f),
            "DANG TIM DOI THU...", 10f, 15.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white, font);

        // 4 chấm trạng thái slot người chơi trong phòng
        fmSlotDots = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject dot = new GameObject("SlotDot_" + i, typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(statusBox.transform, false);
            RectTransform dRt = dot.GetComponent<RectTransform>();
            float x0 = 0.03f + i * 0.075f;
            dRt.anchorMin = new Vector2(x0, 0.14f);
            dRt.anchorMax = new Vector2(x0 + 0.062f, 0.42f);
            dRt.offsetMin = Vector2.zero;
            dRt.offsetMax = Vector2.zero;
            Image dImg = dot.GetComponent<Image>();
            dImg.color = new Color(0.18f, 0.24f, 0.36f, 1f);
            dImg.raycastTarget = false;
            fmSlotDots[i] = dImg;
        }

        fmSlotCountText = CreateMenuText("SlotCount", statusBox.transform, new Vector2(0.36f, 0.10f), new Vector2(0.97f, 0.46f),
            "1 / 4 NGUOI CHOI", 8f, 11.5f, FontStyles.Bold, TextAlignmentOptions.MidlineRight,
            new Color(0.76f, 0.90f, 1.00f, 0.95f), font);

        // 4b. Khung Đồng Hồ Số & Thanh Quét Động (0.37 -> 0.66)
        GameObject timerBox = new GameObject("TimerBox", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        timerBox.transform.SetParent(rightCol.transform, false);
        RectTransform tbRt = timerBox.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0f, 0.37f);
        tbRt.anchorMax = new Vector2(1f, 0.66f);
        tbRt.offsetMin = Vector2.zero;
        tbRt.offsetMax = Vector2.zero;
        timerBox.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.14f, 0.96f);
        timerBox.GetComponent<Image>().raycastTarget = false;

        CreateMenuText("TimerCap", timerBox.transform, new Vector2(0.03f, 0.22f), new Vector2(0.42f, 0.84f),
            "THOI GIAN TIM TRAN", 8.5f, 12.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
            new Color(0.72f, 0.84f, 0.98f, 0.92f), font);

        fmTimerText = CreateMenuText("DigitalTimer", timerBox.transform, new Vector2(0.45f, 0.16f), new Vector2(0.97f, 0.92f),
            "00:00", 16f, 26f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, fmAccent, font);

        GameObject scanTrack = new GameObject("ScanTrack", typeof(RectTransform), typeof(Image));
        scanTrack.transform.SetParent(timerBox.transform, false);
        RectTransform stRt = scanTrack.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0f, 0f);
        stRt.anchorMax = new Vector2(1f, 0.11f);
        stRt.offsetMin = Vector2.zero;
        stRt.offsetMax = Vector2.zero;
        scanTrack.GetComponent<Image>().color = new Color(0.10f, 0.16f, 0.28f, 1f);
        scanTrack.GetComponent<Image>().raycastTarget = false;

        GameObject scanBar = new GameObject("ScanBar", typeof(RectTransform), typeof(Image));
        scanBar.transform.SetParent(scanTrack.transform, false);
        fmScanBarRt = scanBar.GetComponent<RectTransform>();
        fmScanBarRt.anchorMin = new Vector2(0f, 0f);
        fmScanBarRt.anchorMax = new Vector2(0.28f, 1f);
        fmScanBarRt.offsetMin = Vector2.zero;
        fmScanBarRt.offsetMax = Vector2.zero;
        fmScanBarImg = scanBar.GetComponent<Image>();
        fmScanBarImg.color = fmAccent;
        fmScanBarImg.raycastTarget = false;

        // 4c. Dải Xem Nhanh Trang Bị Hiện Tại (Ngoại Trang • Vũ Khí • Kỹ Năng) (0.00 -> 0.33)
        BuildMiniLoadoutChip(rightCol.transform, new Vector2(0.00f, 0.00f), new Vector2(0.32f, 0.33f), "NGOAI TRANG", font, out fmSkinIcon, out fmSkinName);
        BuildMiniLoadoutChip(rightCol.transform, new Vector2(0.34f, 0.00f), new Vector2(0.66f, 0.33f), "VU KHI", font, out fmWeaponIcon, out fmWeaponName);
        BuildMiniLoadoutChip(rightCol.transform, new Vector2(0.68f, 0.00f), new Vector2(1.00f, 0.33f), "KY NANG", font, out fmSkillIcon, out fmSkillName);

        // 5. BOTTOM ACTION BAR: MẸO CHIẾN ĐẤU + NÚT HỦY TÌM TRẬN (0.03 -> 0.18)
        GameObject tipBg = new GameObject("QueueTipBg", typeof(RectTransform), typeof(Image));
        tipBg.transform.SetParent(cardBody.transform, false);
        RectTransform tpRt = tipBg.GetComponent<RectTransform>();
        tpRt.anchorMin = new Vector2(0.03f, 0.035f);
        tpRt.anchorMax = new Vector2(0.66f, 0.175f);
        tpRt.offsetMin = Vector2.zero;
        tpRt.offsetMax = Vector2.zero;
        tipBg.GetComponent<Image>().color = new Color(0.07f, 0.11f, 0.20f, 0.90f);
        tipBg.GetComponent<Image>().raycastTarget = false;

        fmTipText = CreateMenuText("QueueTipTxt", tipBg.transform, new Vector2(0.03f, 0.06f), new Vector2(0.97f, 0.94f),
            QueueTips[0], 7.5f, 11f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft,
            new Color(0.82f, 0.90f, 1.00f, 0.95f), font);

        GameObject cancelBtnGo = new GameObject("CancelMatchBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        cancelBtnGo.transform.SetParent(cardBody.transform, false);
        RectTransform cnRt = cancelBtnGo.GetComponent<RectTransform>();
        cnRt.anchorMin = new Vector2(0.69f, 0.035f);
        cnRt.anchorMax = new Vector2(0.97f, 0.175f);
        cnRt.offsetMin = Vector2.zero;
        cnRt.offsetMax = Vector2.zero;
        Image cnBorder = cancelBtnGo.GetComponent<Image>();
        cnBorder.color = new Color(1.00f, 0.40f, 0.44f, 0.95f);
        Button cnBtn = cancelBtnGo.GetComponent<Button>();
        cnBtn.targetGraphic = cnBorder;
        cnBtn.onClick.AddListener(CancelMatch);

        GameObject cnInner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        cnInner.transform.SetParent(cancelBtnGo.transform, false);
        RectTransform cniRt = cnInner.GetComponent<RectTransform>();
        cniRt.anchorMin = Vector2.zero;
        cniRt.anchorMax = Vector2.one;
        cniRt.offsetMin = new Vector2(2f, 2f);
        cniRt.offsetMax = new Vector2(-2f, -2f);
        cnInner.GetComponent<Image>().color = UIThemeAssets.DangerRed;
        cnInner.GetComponent<Image>().raycastTarget = false;

        CreateMenuText("CancelLbl", cnInner.transform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f),
            "HUY TIM TRAN", 9.5f, 14f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, font);
    }

    void BuildMiniLoadoutChip(
        Transform parent,
        Vector2 aMin,
        Vector2 aMax,
        string catLabel,
        TMP_FontAsset font,
        out Image iconImg,
        out TMP_Text nameTxt
    )
    {
        GameObject chip = new GameObject("Chip_" + catLabel, typeof(RectTransform), typeof(Image));
        chip.transform.SetParent(parent, false);
        RectTransform rt = chip.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        chip.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.22f, 0.94f);
        chip.GetComponent<Image>().raycastTarget = false;

        GameObject icGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icGo.transform.SetParent(chip.transform, false);
        RectTransform icRt = icGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0.05f, 0.12f);
        icRt.anchorMax = new Vector2(0.36f, 0.88f);
        icRt.offsetMin = Vector2.zero;
        icRt.offsetMax = Vector2.zero;
        iconImg = icGo.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        CreateMenuText("Cat", chip.transform, new Vector2(0.39f, 0.52f), new Vector2(0.96f, 0.92f),
            catLabel, 6.5f, 9f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
            new Color(0.66f, 0.80f, 0.96f, 0.90f), font);

        nameTxt = CreateMenuText("Val", chip.transform, new Vector2(0.39f, 0.08f), new Vector2(0.96f, 0.54f),
            "-", 7.5f, 11f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white, font);
    }

    void RefreshFindMatchPresentation(string mode)
    {
        fmCurrentMode = string.IsNullOrEmpty(mode) ? "RANK" : mode.ToUpperInvariant();
        string badge;
        string sub;

        switch (fmCurrentMode)
        {
            case "SOLO":
                badge = "SOLO 1v1";
                sub = "DAU TAY DOI TRUC DIEN  •  PHAN DON TOC DO CAO";
                fmAccent = new Color(0.18f, 0.86f, 1.00f, 1f);
                fmTargetSlots = 2;
                break;
            case "TRAINING":
                badge = "PHONG TAP";
                sub = "LUYEN PHAN XA PARRY & LAM CHU KY NANG";
                fmAccent = new Color(0.22f, 0.94f, 0.64f, 1f);
                fmTargetSlots = 1;
                break;
            case "CUSTOM":
                badge = "PHONG TUY CHON";
                sub = "DANG KET NOI PHONG CUSTOM RIENG";
                fmAccent = new Color(0.76f, 0.42f, 1.00f, 1f);
                fmTargetSlots = 4;
                break;
            default:
                badge = "XEP HANG • RANK";
                sub = "DAU TRUONG SINH TU  •  TRANH DIEM ELO";
                fmAccent = new Color(1.00f, 0.74f, 0.20f, 1f);
                fmTargetSlots = 4;
                break;
        }

        if (fmCardBorderImg != null) fmCardBorderImg.color = fmAccent;
        if (fmTopStripeImg != null) fmTopStripeImg.color = fmAccent;
        if (fmModePillBg != null) fmModePillBg.color = new Color(fmAccent.r, fmAccent.g, fmAccent.b, 0.22f);
        if (fmModeBadgeText != null) { fmModeBadgeText.text = badge; fmModeBadgeText.color = fmAccent; }
        if (fmModeSubText != null) fmModeSubText.text = sub;
        if (fmTimerText != null) { fmTimerText.text = "00:00"; fmTimerText.color = fmAccent; }
        if (fmRadarOuterImg != null) fmRadarOuterImg.color = fmAccent;
        if (fmRadarInnerImg != null) fmRadarInnerImg.color = Color.Lerp(Color.white, fmAccent, 0.50f);
        if (fmScanBarImg != null) fmScanBarImg.color = fmAccent;

        if (fmRadarCenterIcon != null)
        {
            fmRadarCenterIcon.sprite = fmCurrentMode == "RANK"
                ? UIThemeAssets.GetRankIcon(PlayerStatsManager.PlayerElo)
                : UIThemeAssets.IconParry;
        }

        SkinDefinition sk = SkinCatalog.Get(PlayerStatsManager.EquippedSkin);
        WeaponDefinition wp = WeaponCatalog.Get(PlayerStatsManager.EquippedWeapon);
        SkillDefinition ab = SkillCatalog.Get(PlayerStatsManager.EquippedSkill);

        if (fmSkinIcon != null) fmSkinIcon.sprite = UIThemeAssets.GetSkinIcon(PlayerStatsManager.EquippedSkin);
        if (fmSkinName != null && sk != null) { fmSkinName.text = sk.displayName; fmSkinName.color = sk.suitAccentColor; }

        if (fmWeaponIcon != null) fmWeaponIcon.sprite = UIThemeAssets.GetWeaponIcon(PlayerStatsManager.EquippedWeapon);
        if (fmWeaponName != null && wp != null) { fmWeaponName.text = wp.displayName; fmWeaponName.color = wp.primaryColor; }

        if (fmSkillIcon != null) fmSkillIcon.sprite = UIThemeAssets.GetSkillIcon(PlayerStatsManager.EquippedSkill);
        if (fmSkillName != null && ab != null) { fmSkillName.text = ab.shortName; fmSkillName.color = ab.tierColor; }

        fmTipIndex = Random.Range(0, QueueTips.Length);
        fmTipTimer = 0f;
        if (fmTipText != null) fmTipText.text = QueueTips[fmTipIndex];

        UpdateFindMatchSlotDots(1);
    }

    void UpdateFindMatchSlotDots(int connectedPlayers)
    {
        int target = Mathf.Clamp(fmTargetSlots, 1, 4);
        int active = Mathf.Clamp(connectedPlayers, 1, target);

        if (fmSlotDots != null)
        {
            for (int i = 0; i < fmSlotDots.Length; i++)
            {
                if (fmSlotDots[i] == null) continue;
                bool visible = i < target;
                fmSlotDots[i].gameObject.SetActive(visible);
                if (visible)
                {
                    fmSlotDots[i].color = i < active
                        ? fmAccent
                        : new Color(0.18f, 0.24f, 0.36f, 0.90f);
                }
            }
        }

        if (fmSlotCountText != null)
        {
            fmSlotCountText.text = target == 1
                ? "SAN SANG VAO PHONG TAP"
                : $"{active} / {target} NGUOI CHOI";
        }
    }

    Sprite CreateRadarRingSprite(bool outer)
    {
        const int sz = 192;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[sz * sz];
        float c = (sz - 1) * 0.5f;
        float rMid = outer ? 0.88f : 0.78f;
        float halfW = outer ? 0.048f : 0.036f;

        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - c) / c;
                float dy = (y - c) / c;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 180f;

                float band = 1f - Mathf.Clamp01(Mathf.Abs(dist - rMid) / halfW);
                bool dash = outer ? ((ang % 90f) < 62f) : ((ang % 45f) < 24f);
                float a = dash ? band * band : band * 0.14f;
                px[y * sz + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
        menuRuntimeAssets.Add(tex);
        menuRuntimeAssets.Add(sp);
        return sp;
    }

    Sprite CreateSoftCircleGlowSprite()
    {
        const int sz = 128;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[sz * sz];
        float c = (sz - 1) * 0.5f;

        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - c) / c;
                float dy = (y - c) / c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float ring = Mathf.Exp(-Mathf.Pow((d - 0.78f) * 6.5f, 2f)) * 0.85f;
                float glow = Mathf.Clamp01(1f - d) * 0.25f;
                px[y * sz + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(ring + glow));
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
        menuRuntimeAssets.Add(tex);
        menuRuntimeAssets.Add(sp);
        return sp;
    }

    Sprite CreateSpeakerIconSprite(bool muted)
    {
        const int sz = 64;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[sz * sz];

        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float nx = x / (float)(sz - 1);
                float ny = y / (float)(sz - 1);
                float a = 0f;

                // Speaker body (left rectangle + trapezoid cone)
                if (nx >= 0.14f && nx <= 0.30f && ny >= 0.36f && ny <= 0.64f)
                    a = 1f;
                else if (nx > 0.30f && nx <= 0.52f)
                {
                    float t = (nx - 0.30f) / 0.22f;
                    float halfH = Mathf.Lerp(0.14f, 0.32f, t);
                    if (Mathf.Abs(ny - 0.50f) <= halfH) a = 1f;
                }

                if (!muted)
                {
                    // Two sound wave arcs on the right
                    float dx = nx - 0.50f;
                    float dy = ny - 0.50f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dx > 0.04f && Mathf.Abs(dy) < dx * 1.15f)
                    {
                        if (Mathf.Abs(r - 0.18f) < 0.032f || Mathf.Abs(r - 0.31f) < 0.032f)
                            a = 1f;
                    }
                }
                else
                {
                    // Clean X mark on the right
                    float cx = nx - 0.74f;
                    float cy = ny - 0.50f;
                    if (Mathf.Abs(cx) < 0.15f && Mathf.Abs(cy) < 0.15f)
                    {
                        if (Mathf.Abs(cx - cy) < 0.040f || Mathf.Abs(cx + cy) < 0.040f)
                            a = 1f;
                    }
                }

                px[y * sz + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
        menuRuntimeAssets.Add(tex);
        menuRuntimeAssets.Add(sp);
        return sp;
    }

    // ================= UPDATE =================

    void Update()
    {
        if (!searching)
            return;

        float dt = Time.deltaTime;
        timer += dt;

        int min = Mathf.FloorToInt(timer / 60f);
        int sec = Mathf.FloorToInt(timer % 60f);
        string formattedTime = $"{min:00}:{sec:00}";

        if (timerText != null) timerText.text = formattedTime;
        if (fmTimerText != null) fmTimerText.text = formattedTime;

        // Xoay radar mượt & sóng xung kích quét tìm trận
        if (fmRadarOuterRt != null) fmRadarOuterRt.Rotate(0f, 0f, -68f * dt);
        if (fmRadarInnerRt != null) fmRadarInnerRt.Rotate(0f, 0f, 112f * dt);

        if (fmRadarPulseRt != null && fmRadarPulseImg != null)
        {
            float p = (timer * 0.85f) % 1f;
            float sc = Mathf.Lerp(0.38f, 1.16f, p);
            fmRadarPulseRt.localScale = Vector3.one * sc;
            fmRadarPulseImg.color = new Color(fmAccent.r, fmAccent.g, fmAccent.b, (1f - p) * 0.45f);
        }

        // Thanh quét qua lại mượt dưới đồng hồ
        if (fmScanBarRt != null)
        {
            float s = 0.5f + 0.5f * Mathf.Sin(timer * 2.8f);
            float startX = s * 0.72f;
            fmScanBarRt.anchorMin = new Vector2(startX, 0f);
            fmScanBarRt.anchorMax = new Vector2(startX + 0.28f, 1f);
        }

        // Cập nhật trạng thái động & số người chơi đã vào phòng
        int activeCount = 1;
        bool runnerConnected = false;
        if (MatchmakingManager.Instance != null && MatchmakingManager.Instance.runner != null && MatchmakingManager.Instance.runner.IsRunning)
        {
            runnerConnected = true;
            int cnt = 0;
            foreach (var _ in MatchmakingManager.Instance.runner.ActivePlayers) cnt++;
            if (cnt > 0) activeCount = cnt;
        }
        UpdateFindMatchSlotDots(activeCount);

        if (fmStatusText != null)
        {
            int dotCount = (Mathf.FloorToInt(timer * 2.5f) % 3) + 1;
            string dots = new string('.', dotCount);
            if (fmCurrentMode == "TRAINING")
                fmStatusText.text = "DANG KHOI TAO PHONG TAP" + dots;
            else if (runnerConnected && activeCount >= fmTargetSlots)
                fmStatusText.text = "DA DU NGUOI! CHUAN BI VAO TRAN" + dots;
            else if (runnerConnected)
                fmStatusText.text = "DA KET NOI PHONG  •  DANG GHEP DOI THU" + dots;
            else
                fmStatusText.text = "DANG TIM PHONG & DOI THU" + dots;
        }

        // Luân phiên mẹo chiến đấu mỗi 4 giây
        fmTipTimer += dt;
        if (fmTipTimer >= 4.0f)
        {
            fmTipTimer = 0f;
            fmTipIndex = (fmTipIndex + 1) % QueueTips.Length;
            if (fmTipText != null) fmTipText.text = QueueTips[fmTipIndex];
        }
    }

    // ================= CORE =================

    void StartSearching(string mode, System.Action startAction)
    {
        if (searching)
            return;

        searching = true;
        timer = 0f;

        if (findingMatchPanel != null)
        {
            findingMatchPanel.SetActive(true);
            findingMatchPanel.transform.SetAsLastSibling();
        }

        RefreshFindMatchPresentation(mode);

        if (modeText != null) modeText.text = mode;
        if (statusText != null) statusText.text = "Finding Players...";

        startAction?.Invoke();
    }

    // ================= PLAY =================

    public void PlayRank()
    {
        StartSearching("RANK", () => MatchmakingManager.Instance?.StartRankMatch());
    }

    public void PlaySolo()
    {
        StartSearching("SOLO", () => MatchmakingManager.Instance?.StartSoloMatch());
    }

    public void PlayTraining()
    {
        StartSearching("TRAINING", () => MatchmakingManager.Instance?.StartTraining());
    }

    public void PlayCustom()
    {
        if (CustomLobbyUI.Instance != null)
        {
            StopSearching();
            CustomLobbyUI.Instance.OpenLobby();
            MatchmakingManager.Instance?.StartCustomMatch();
            return;
        }

        StartSearching("CUSTOM", () => MatchmakingManager.Instance?.StartCustomMatch());
    }

    public void OpenWeaponShop()
    {
        if (WeaponShopUI.Instance != null)
        {
            WeaponShopUI.Instance.OpenShop();
        }
    }

    public void OpenSkillShop()
    {
        if (SkillShopUI.Instance != null)
        {
            SkillShopUI.Instance.OpenShop();
        }
    }

    public void OpenSkinShop()
    {
        if (SkinShopUI.Instance != null)
        {
            SkinShopUI.Instance.OpenShop();
        }
    }

    // ================= CANCEL =================

    public void CancelMatch()
    {
        StopSearching();
        CustomLobbyUI.Instance?.HideLobby();
        MatchmakingManager.Instance?.CancelMatchmaking();
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ================= STOP =================

    public void StopSearching()
    {
        searching = false;
        timer = 0f;

        if (findingMatchPanel != null) findingMatchPanel.SetActive(false);
        if (statusText != null) statusText.text = "";
        if (timerText != null) timerText.text = "00:00";
        if (fmStatusText != null) fmStatusText.text = "";
        if (fmTimerText != null) fmTimerText.text = "00:00";
    }
}
