using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillShopUI : MonoBehaviour
{
    public static SkillShopUI Instance;

    GameObject shopPanel;
    TMP_Text goldHeaderText;
    TMP_Text detailNameText;
    TMP_Text detailStatsText;
    TMP_Text detailDescText;
    TMP_Text actionButtonText;
    Button actionButton;
    Image actionButtonBg;
    TMP_Text feedbackText;

    Image[] cardBorders;
    Image[] cardIcons;
    GameObject[] cardLockBadges;
    TMP_Text[] cardStatusTexts;

    // Live Mini Animated Skill Preview refs
    RectTransform previewPlayerRt;
    Image previewPlayerImg;
    RectTransform previewEnemyRt;
    RectTransform previewBallRt;
    Image previewBallImg;
    RectTransform previewFxRt;
    Image previewFxImg;
    RectTransform previewSubBallRt;
    Image previewSubBallImg;
    TMP_Text previewCaptionText;
    float previewTime;

    SkillType selectedSkill = SkillType.CloneBall;
    TMP_FontAsset uiFont;
    bool built;

    public bool IsOpen => shopPanel != null && shopPanel.activeSelf;

    public static SkillShopUI EnsureInstance(Canvas canvas, TMP_FontAsset font)
    {
        if (Instance != null)
            return Instance;

        if (canvas == null)
            return null;

        GameObject root = new GameObject("SkillShopRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        SkillShopUI shop = root.AddComponent<SkillShopUI>();
        shop.uiFont = font;
        shop.BuildUI();
        return shop;
    }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (IsOpen)
        {
            UpdateSkillPreviewAnimation();
        }
    }

    public void OpenShop()
    {
        if (!built) BuildUI();
        selectedSkill = PlayerStatsManager.EquippedSkill;
        previewTime = 0f;
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            transform.SetAsLastSibling();
        }
        if (feedbackText != null) feedbackText.text = "";
        RefreshShop();
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        MenuUI menu = FindFirstObjectByType<MenuUI>();
        if (menu != null)
        {
            menu.RefreshPlayerStats();
        }
    }

    void BuildUI()
    {
        if (built) return;
        built = true;

        // Overlay nền tối mờ
        shopPanel = new GameObject("SkillShopPanel", typeof(RectTransform), typeof(Image));
        shopPanel.transform.SetParent(transform, false);
        RectTransform pRt = shopPanel.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.offsetMin = Vector2.zero;
        pRt.offsetMax = Vector2.zero;
        Image pImg = shopPanel.GetComponent<Image>();
        pImg.color = new Color(0.02f, 0.04f, 0.08f, 0.92f);
        pImg.raycastTarget = true;

        // Khung chính Mobile Landscape
        GameObject cardBorder = new GameObject("SkillShopCardBorder", typeof(RectTransform), typeof(Image));
        cardBorder.transform.SetParent(shopPanel.transform, false);
        RectTransform cbRt = cardBorder.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(0.03f, 0.04f);
        cbRt.anchorMax = new Vector2(0.97f, 0.96f);
        cbRt.offsetMin = Vector2.zero;
        cbRt.offsetMax = Vector2.zero;
        cardBorder.GetComponent<Image>().color = UIThemeAssets.CardBorder;
        cardBorder.GetComponent<Image>().raycastTarget = false;

        GameObject cardBody = new GameObject("SkillShopCardBody", typeof(RectTransform), typeof(Image));
        cardBody.transform.SetParent(cardBorder.transform, false);
        RectTransform bodyRt = cardBody.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(2.5f, 2.5f);
        bodyRt.offsetMax = new Vector2(-2.5f, -2.5f);
        cardBody.GetComponent<Image>().color = UIThemeAssets.CardBody;
        cardBody.GetComponent<Image>().raycastTarget = false;

        // ===== HEADER BAR (83% -> 100%) =====
        GameObject header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(cardBody.transform, false);
        RectTransform hRt = header.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 0.83f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.offsetMin = Vector2.zero;
        hRt.offsetMax = Vector2.zero;
        header.GetComponent<Image>().color = new Color(0.10f, 0.14f, 0.23f, 0.98f);
        header.GetComponent<Image>().raycastTarget = false;

        CreateText("ShopTitle", header.transform, new Vector2(0.025f, 0.05f), new Vector2(0.55f, 0.95f),
            "KHO KY NANG & XEM TRUOC (YEU -> MANH)", 10f, 16f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

        // Gold pill
        GameObject goldPill = new GameObject("GoldPill", typeof(RectTransform), typeof(Image));
        goldPill.transform.SetParent(header.transform, false);
        RectTransform gpRt = goldPill.GetComponent<RectTransform>();
        gpRt.anchorMin = new Vector2(0.57f, 0.14f);
        gpRt.anchorMax = new Vector2(0.81f, 0.86f);
        gpRt.offsetMin = Vector2.zero;
        gpRt.offsetMax = Vector2.zero;
        goldPill.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.14f, 0.96f);
        goldPill.GetComponent<Image>().raycastTarget = false;

        GameObject coinGo = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        coinGo.transform.SetParent(goldPill.transform, false);
        RectTransform ciRt = coinGo.GetComponent<RectTransform>();
        ciRt.anchorMin = new Vector2(0.05f, 0.12f);
        ciRt.anchorMax = new Vector2(0.25f, 0.88f);
        ciRt.offsetMin = Vector2.zero;
        ciRt.offsetMax = Vector2.zero;
        Image ciImg = coinGo.GetComponent<Image>();
        ciImg.sprite = UIThemeAssets.IconGold;
        ciImg.preserveAspect = true;
        ciImg.raycastTarget = false;

        goldHeaderText = CreateText("GoldValue", goldPill.transform, new Vector2(0.27f, 0.05f), new Vector2(0.95f, 0.95f),
            "0 VANG", 9.5f, 14f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, UIThemeAssets.GoldAccent);

        Button closeBtn = CreateButton("CloseButton", header.transform, new Vector2(0.83f, 0.14f), new Vector2(0.975f, 0.86f),
            UIThemeAssets.DangerRed, "DONG", 9.5f, 13.5f);
        closeBtn.onClick.AddListener(CloseShop);

        // ===== DANH SÁCH KỸ NĂNG CUỘN NGANG (36% -> 81%) — XANH LÁ -> TÍM =====
        GameObject scrollArea = new GameObject("SkillShopScrollArea", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollArea.transform.SetParent(cardBody.transform, false);
        RectTransform saRt = scrollArea.GetComponent<RectTransform>();
        saRt.anchorMin = new Vector2(0.02f, 0.36f);
        saRt.anchorMax = new Vector2(0.98f, 0.81f);
        saRt.offsetMin = Vector2.zero;
        saRt.offsetMax = Vector2.zero;
        scrollArea.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.14f, 0.65f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollArea.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = new Vector2(0f, 0.09f);
        vpRt.anchorMax = new Vector2(1f, 1f);
        vpRt.offsetMin = new Vector2(4f, 2f);
        vpRt.offsetMax = new Vector2(-4f, -4f);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.003f);

        GameObject content = new GameObject("SkillCardsContent", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform ctRt = content.GetComponent<RectTransform>();
        ctRt.anchorMin = new Vector2(0f, 0f);
        ctRt.anchorMax = new Vector2(0f, 1f);
        ctRt.pivot = new Vector2(0f, 0.5f);
        ctRt.anchoredPosition = Vector2.zero;
        ctRt.sizeDelta = Vector2.zero;

        HorizontalLayoutGroup hlg = content.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.padding = new RectOffset(8, 8, 4, 4);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Horizontal Scrollbar
        GameObject sbGo = new GameObject("SkillHScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGo.transform.SetParent(scrollArea.transform, false);
        RectTransform sbRt = sbGo.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0.03f, 0.01f);
        sbRt.anchorMax = new Vector2(0.97f, 0.065f);
        sbRt.offsetMin = Vector2.zero;
        sbRt.offsetMax = Vector2.zero;
        sbGo.GetComponent<Image>().color = new Color(0.10f, 0.15f, 0.24f, 0.90f);

        GameObject slideArea = new GameObject("SlidingArea", typeof(RectTransform));
        slideArea.transform.SetParent(sbGo.transform, false);
        RectTransform slRt = slideArea.GetComponent<RectTransform>();
        slRt.anchorMin = Vector2.zero;
        slRt.anchorMax = Vector2.one;
        slRt.offsetMin = Vector2.zero;
        slRt.offsetMax = Vector2.zero;

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(slideArea.transform, false);
        RectTransform hdRt = handle.GetComponent<RectTransform>();
        hdRt.anchorMin = Vector2.zero;
        hdRt.anchorMax = Vector2.one;
        hdRt.offsetMin = Vector2.zero;
        hdRt.offsetMax = Vector2.zero;
        Image hdImg = handle.GetComponent<Image>();
        hdImg.color = UIThemeAssets.CardBorder;

        Scrollbar hBar = sbGo.GetComponent<Scrollbar>();
        hBar.handleRect = hdRt;
        hBar.targetGraphic = hdImg;
        hBar.direction = Scrollbar.Direction.LeftToRight;

        ScrollRect sr = scrollArea.GetComponent<ScrollRect>();
        sr.content = ctRt;
        sr.viewport = vpRt;
        sr.horizontal = true;
        sr.vertical = false;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.elasticity = 0.08f;
        sr.inertia = true;
        sr.decelerationRate = 0.12f;
        sr.scrollSensitivity = 28f;
        sr.horizontalScrollbar = hBar;
        sr.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        SkillDefinition[] all = SkillCatalog.All;
        cardBorders = new Image[all.Length];
        cardIcons = new Image[all.Length];
        cardLockBadges = new GameObject[all.Length];
        cardStatusTexts = new TMP_Text[all.Length];

        for (int i = 0; i < all.Length; i++)
        {
            SkillDefinition def = all[i];
            SkillType st = def.type;

            GameObject cardGo = new GameObject("SkillCard_" + st, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            cardGo.transform.SetParent(content.transform, false);
            cardGo.GetComponent<RectTransform>().sizeDelta = new Vector2(138f, 0f);
            LayoutElement le = cardGo.GetComponent<LayoutElement>();
            le.preferredWidth = 138f;
            le.minWidth = 138f;

            // Khung màu phân cấp từ Xanh Lá -> Xanh Lam -> Tím!
            Image border = cardGo.GetComponent<Image>();
            border.color = def.tierColor;
            border.raycastTarget = true;
            cardBorders[i] = border;

            Button btn = cardGo.GetComponent<Button>();
            btn.targetGraphic = border;
            btn.onClick.AddListener(() => SelectSkill(st));

            GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(cardGo.transform, false);
            RectTransform inRt = inner.GetComponent<RectTransform>();
            inRt.anchorMin = Vector2.zero;
            inRt.anchorMax = Vector2.one;
            inRt.offsetMin = new Vector2(2.8f, 2.8f);
            inRt.offsetMax = new Vector2(-2.8f, -2.8f);
            Image inImg = inner.GetComponent<Image>();
            inImg.color = Color.Lerp(UIThemeAssets.SubCardBg, def.tierColor, 0.08f);
            inImg.raycastTarget = false;

            // Top tier color bar
            GameObject stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(inner.transform, false);
            RectTransform sRt = stripe.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0.93f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.offsetMin = Vector2.zero;
            sRt.offsetMax = Vector2.zero;
            stripe.GetComponent<Image>().color = def.tierColor;
            stripe.GetComponent<Image>().raycastTarget = false;

            // Tier label (CO BAN -> TOI THUONG)
            CreateText("Tier", inner.transform, new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.92f),
                def.tierLabel, 7f, 9.5f, FontStyles.Bold, TextAlignmentOptions.Center, def.tierColor);

            // Skill Icon
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(inner.transform, false);
            RectTransform icRt = iconGo.GetComponent<RectTransform>();
            icRt.anchorMin = new Vector2(0.18f, 0.36f);
            icRt.anchorMax = new Vector2(0.82f, 0.78f);
            icRt.offsetMin = Vector2.zero;
            icRt.offsetMax = Vector2.zero;
            Image icImg = iconGo.GetComponent<Image>();
            icImg.sprite = UIThemeAssets.GetSkillIcon(st);
            icImg.preserveAspect = true;
            icImg.raycastTarget = false;
            cardIcons[i] = icImg;

            // Lock badge top-right
            GameObject lockGo = new GameObject("LockBadge", typeof(RectTransform), typeof(Image));
            lockGo.transform.SetParent(inner.transform, false);
            RectTransform lkRt = lockGo.GetComponent<RectTransform>();
            lkRt.anchorMin = new Vector2(0.70f, 0.56f);
            lkRt.anchorMax = new Vector2(0.95f, 0.78f);
            lkRt.offsetMin = Vector2.zero;
            lkRt.offsetMax = Vector2.zero;
            Image lkImg = lockGo.GetComponent<Image>();
            lkImg.sprite = UIThemeAssets.IconLock;
            lkImg.preserveAspect = true;
            lkImg.raycastTarget = false;
            cardLockBadges[i] = lockGo;

            // Name
            CreateText("Name", inner.transform, new Vector2(0.04f, 0.19f), new Vector2(0.96f, 0.35f),
                def.displayName, 8f, 11.5f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            // Price / Owned / Equipped
            cardStatusTexts[i] = CreateText("Status", inner.transform, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.18f),
                "", 7.5f, 10.5f, FontStyles.Bold, TextAlignmentOptions.Center, UIThemeAssets.GoldAccent);
        }

        // ===== BOTTOM LIVE PREVIEW + DETAILS + BUY/EQUIP PANEL (3% -> 34%) =====
        GameObject bottomBar = new GameObject("SkillDetailPreviewBar", typeof(RectTransform), typeof(Image));
        bottomBar.transform.SetParent(cardBody.transform, false);
        RectTransform botRt = bottomBar.GetComponent<RectTransform>();
        botRt.anchorMin = new Vector2(0.02f, 0.03f);
        botRt.anchorMax = new Vector2(0.98f, 0.34f);
        botRt.offsetMin = Vector2.zero;
        botRt.offsetMax = Vector2.zero;
        bottomBar.GetComponent<Image>().color = new Color(0.09f, 0.13f, 0.21f, 0.96f);
        bottomBar.GetComponent<Image>().raycastTarget = false;

        // 1. Live Mini Preview Box (Left 27%)
        BuildPreviewStage(bottomBar.transform);

        // 2. Details (Middle 43%)
        detailNameText = CreateText("DetailName", bottomBar.transform, new Vector2(0.29f, 0.68f), new Vector2(0.72f, 0.95f),
            "", 9.5f, 14f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

        detailStatsText = CreateText("DetailStats", bottomBar.transform, new Vector2(0.29f, 0.48f), new Vector2(0.72f, 0.68f),
            "", 7.5f, 10.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, UIThemeAssets.GoldAccent);

        detailDescText = CreateText("DetailDesc", bottomBar.transform, new Vector2(0.29f, 0.06f), new Vector2(0.72f, 0.48f),
            "", 7.5f, 10.5f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.82f, 0.89f, 0.97f, 1f));
        detailDescText.textWrappingMode = TextWrappingModes.Normal;

        // 3. Buy / Equip Action (Right 26%)
        feedbackText = CreateText("Feedback", bottomBar.transform, new Vector2(0.73f, 0.66f), new Vector2(0.98f, 0.95f),
            "", 7.5f, 10f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.45f, 0.42f, 1f));

        actionButton = CreateButton("ActionButton", bottomBar.transform, new Vector2(0.74f, 0.14f), new Vector2(0.97f, 0.64f),
            UIThemeAssets.SuccessGreen, "TRANG BI", 9.5f, 13.5f);
        actionButtonBg = actionButton.GetComponent<Image>();
        actionButtonText = actionButton.GetComponentInChildren<TMP_Text>();
        actionButton.onClick.AddListener(OnClickBuyOrEquip);

        shopPanel.SetActive(false);
    }

    void BuildPreviewStage(Transform parent)
    {
        GameObject stage = new GameObject("LivePreviewStage", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        stage.transform.SetParent(parent, false);
        RectTransform stRt = stage.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0.012f, 0.07f);
        stRt.anchorMax = new Vector2(0.275f, 0.93f);
        stRt.offsetMin = Vector2.zero;
        stRt.offsetMax = Vector2.zero;
        Image stBg = stage.GetComponent<Image>();
        stBg.color = new Color(0.04f, 0.06f, 0.12f, 0.98f);
        stBg.raycastTarget = false;

        // Arena floor subtle ring
        GameObject floor = new GameObject("ArenaFloor", typeof(RectTransform), typeof(Image));
        floor.transform.SetParent(stage.transform, false);
        RectTransform fRt = floor.GetComponent<RectTransform>();
        fRt.anchorMin = new Vector2(0.08f, 0.18f);
        fRt.anchorMax = new Vector2(0.92f, 0.78f);
        fRt.offsetMin = Vector2.zero;
        fRt.offsetMax = Vector2.zero;
        Image fImg = floor.GetComponent<Image>();
        if (UIThemeAssets.BtnCircleFrame != null) fImg.sprite = UIThemeAssets.BtnCircleFrame;
        fImg.color = new Color(0.20f, 0.34f, 0.54f, 0.35f);
        fImg.raycastTarget = false;

        // Player dot (Left)
        previewPlayerRt = CreatePreviewNode(stage.transform, "PreviewPlayer", new Vector2(18f, 18f), new Color(0.28f, 0.85f, 1f, 1f), out previewPlayerImg);
        // Enemy dot (Right)
        Image dummyEnemyImg;
        previewEnemyRt = CreatePreviewNode(stage.transform, "PreviewEnemy", new Vector2(18f, 18f), new Color(1f, 0.34f, 0.34f, 1f), out dummyEnemyImg);
        previewEnemyRt.anchoredPosition = new Vector2(62f, 4f);

        // Effect ring/aura
        previewFxRt = CreatePreviewNode(stage.transform, "PreviewFx", new Vector2(36f, 36f), new Color(0.4f, 0.9f, 1f, 0.65f), out previewFxImg);
        if (UIThemeAssets.BtnCircleFrame != null) previewFxImg.sprite = UIThemeAssets.BtnCircleFrame;

        // Main Ball
        previewBallRt = CreatePreviewNode(stage.transform, "PreviewBall", new Vector2(13f, 13f), new Color(1f, 0.65f, 0.12f, 1f), out previewBallImg);

        // Secondary Ball (for CloneBall)
        previewSubBallRt = CreatePreviewNode(stage.transform, "PreviewSubBall", new Vector2(12f, 12f), new Color(0.35f, 0.92f, 1f, 0.85f), out previewSubBallImg);

        previewCaptionText = CreateText("PreviewCaption", stage.transform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.22f),
            "MO PHONG KY NANG", 6.5f, 8.5f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.74f, 0.88f, 1f, 0.95f));
    }

    RectTransform CreatePreviewNode(Transform parent, string name, Vector2 size, Color col, out Image img)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.56f);
        rt.anchorMax = new Vector2(0.5f, 0.56f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        img = go.GetComponent<Image>();
        if (UIThemeAssets.BtnCircleFrame != null) img.sprite = UIThemeAssets.BtnCircleFrame;
        img.color = col;
        img.raycastTarget = false;
        return rt;
    }

    void UpdateSkillPreviewAnimation()
    {
        if (previewPlayerRt == null || previewBallRt == null || previewFxRt == null)
            return;

        previewTime += Time.unscaledDeltaTime;
        float cycle = (previewTime % 2.4f) / 2.4f; // 0..1 loop

        Vector2 pPos = new Vector2(-60f, 4f);
        Vector2 ePos = new Vector2(62f, 4f);
        previewPlayerRt.anchoredPosition = pPos;
        previewPlayerImg.color = new Color(0.28f, 0.85f, 1f, 1f);
        previewEnemyRt.anchoredPosition = ePos;
        previewSubBallRt.gameObject.SetActive(false);
        previewFxRt.gameObject.SetActive(true);
        previewBallImg.color = new Color(1f, 0.62f, 0.10f, 1f);

        switch (selectedSkill)
        {
            case SkillType.CloneBall:
            {
                float t = cycle;
                Vector2 bPos = Vector2.Lerp(pPos, ePos, t);
                previewBallRt.anchoredPosition = bPos + new Vector2(0f, Mathf.Sin(t * Mathf.PI) * 14f);
                previewSubBallRt.gameObject.SetActive(true);
                previewSubBallRt.anchoredPosition = bPos + new Vector2(0f, -Mathf.Sin(t * Mathf.PI) * 16f);
                previewFxRt.anchoredPosition = pPos;
                previewFxRt.sizeDelta = Vector2.one * Mathf.Lerp(16f, 34f, t);
                previewFxImg.color = new Color(0.18f, 0.88f, 0.46f, (1f - t) * 0.7f);
                previewCaptionText.text = "BONG GIA BAY CUNG BONG THAT";
                break;
            }
            case SkillType.Freeze:
            {
                if (cycle < 0.25f)
                {
                    float t = cycle / 0.25f;
                    previewBallRt.anchoredPosition = Vector2.Lerp(ePos, Vector2.zero, t);
                    previewFxRt.gameObject.SetActive(false);
                }
                else if (cycle < 0.75f)
                {
                    previewBallRt.anchoredPosition = Vector2.zero;
                    previewBallImg.color = new Color(0.45f, 0.94f, 1f, 1f);
                    previewFxRt.anchoredPosition = Vector2.zero;
                    previewFxRt.sizeDelta = Vector2.one * 34f;
                    previewFxRt.Rotate(0f, 0f, 80f * Time.unscaledDeltaTime);
                    previewFxImg.color = new Color(0.18f, 0.90f, 0.96f, 0.85f);
                }
                else
                {
                    float t = (cycle - 0.75f) / 0.25f;
                    previewBallRt.anchoredPosition = Vector2.Lerp(Vector2.zero, ePos, t);
                    previewFxRt.anchoredPosition = Vector2.zero;
                    previewFxRt.sizeDelta = Vector2.one * Mathf.Lerp(34f, 54f, t);
                    previewFxImg.color = new Color(0.45f, 0.96f, 1f, (1f - t) * 0.7f);
                }
                previewCaptionText.text = "DONG BANG BONG GIUA KHONG TRUNG";
                break;
            }
            case SkillType.Telekinesis:
            {
                // Bay tu ke dich toi -> bat dung 0.42s -> PHAN NGUOC SIEU NHANH!
                if (cycle < 0.38f)
                {
                    float t = cycle / 0.38f;
                    previewBallRt.anchoredPosition = Vector2.Lerp(ePos, new Vector2(-18f, 4f), t);
                    previewFxRt.gameObject.SetActive(false);
                }
                else if (cycle < 0.62f)
                {
                    float t = (cycle - 0.38f) / 0.24f;
                    previewBallRt.anchoredPosition = new Vector2(-18f, 4f + Mathf.Sin(t * Mathf.PI) * 8f);
                    previewBallImg.color = new Color(0.85f, 0.38f, 1f, 1f);
                    previewFxRt.anchoredPosition = previewBallRt.anchoredPosition;
                    previewFxRt.sizeDelta = Vector2.one * Mathf.Lerp(40f, 22f, t);
                    previewFxImg.color = new Color(0.24f, 0.66f, 1f, 0.95f);
                }
                else
                {
                    float t = Mathf.Clamp01((cycle - 0.62f) / 0.22f); // sieu nhanh!
                    previewBallRt.anchoredPosition = Vector2.Lerp(new Vector2(-18f, 4f), ePos, t);
                    previewBallImg.color = new Color(0.95f, 0.35f, 1f, 1f);
                    previewFxRt.anchoredPosition = previewBallRt.anchoredPosition;
                    previewFxRt.sizeDelta = Vector2.one * 26f;
                    previewFxImg.color = new Color(0.75f, 0.30f, 1f, (1f - t) * 0.8f);
                }
                previewCaptionText.text = "BAT & PHAN NGUOC TOC DO 1.95X!";
                break;
            }
            case SkillType.Forcefield:
            {
                previewFxRt.anchoredPosition = pPos;
                float pulse = 38f + Mathf.Sin(previewTime * 10f) * 3f;
                previewFxRt.sizeDelta = Vector2.one * pulse;
                previewFxImg.color = new Color(0.38f, 0.56f, 1f, 0.78f);

                if (cycle < 0.45f)
                {
                    float t = cycle / 0.45f;
                    previewBallRt.anchoredPosition = Vector2.Lerp(ePos, pPos + new Vector2(18f, 0f), t);
                }
                else
                {
                    float t = (cycle - 0.45f) / 0.55f;
                    previewBallRt.anchoredPosition = Vector2.Lerp(pPos + new Vector2(18f, 0f), ePos, t);
                    if (t < 0.25f) previewFxImg.color = new Color(1f, 0.88f, 0.30f, 0.95f);
                }
                previewCaptionText.text = "VOM KHIEN TU DONG PARRY!";
                break;
            }
            case SkillType.Invisible:
            {
                // Player mo ao + chay nhanh vong len tren, bong lao ve phia ke dich
                float ang = cycle * Mathf.PI * 2f;
                previewPlayerRt.anchoredPosition = new Vector2(-42f + Mathf.Cos(ang) * 24f, 4f + Mathf.Sin(ang) * 18f);
                previewPlayerImg.color = new Color(0.68f, 0.46f, 1f, 0.34f);
                previewFxRt.anchoredPosition = previewPlayerRt.anchoredPosition;
                previewFxRt.sizeDelta = Vector2.one * 28f;
                previewFxImg.color = new Color(0.60f, 0.30f, 1f, 0.45f);
                previewBallRt.anchoredPosition = Vector2.Lerp(new Vector2(-10f, 4f), ePos, cycle);
                previewCaptionText.text = "LAM MO + CHAY NHANH 1.55X!";
                break;
            }
            case SkillType.Tornado:
            {
                if (cycle < 0.65f)
                {
                    float t = cycle / 0.65f;
                    float a = t * Mathf.PI * 6f;
                    float r = Mathf.Lerp(10f, 22f, t);
                    Vector2 spiral = pPos + new Vector2(Mathf.Cos(a) * r, Mathf.Lerp(-4f, 34f, t));
                    previewBallRt.anchoredPosition = spiral;
                    previewFxRt.anchoredPosition = pPos + new Vector2(0f, Mathf.Lerp(0f, 18f, t));
                    previewFxRt.sizeDelta = new Vector2(Mathf.Lerp(20f, 42f, t), Mathf.Lerp(16f, 44f, t));
                    previewFxImg.color = new Color(0.84f, 0.18f, 1f, 0.80f);
                }
                else
                {
                    float t = (cycle - 0.65f) / 0.35f;
                    previewBallRt.anchoredPosition = Vector2.Lerp(pPos + new Vector2(0f, 34f), ePos, t);
                    previewBallImg.color = new Color(0.95f, 0.35f, 1f, 1f);
                    previewFxRt.gameObject.SetActive(false);
                }
                previewCaptionText.text = "LOC XOAY LEN CAO -> BO THANG 62 M/S!";
                break;
            }
        }
    }

    void SelectSkill(SkillType st)
    {
        selectedSkill = st;
        previewTime = 0f;
        if (feedbackText != null) feedbackText.text = "";
        RefreshShop();
    }

    void OnClickBuyOrEquip()
    {
        if (PlayerStatsManager.IsSkillOwned(selectedSkill))
        {
            PlayerStatsManager.EquipSkill(selectedSkill);
            if (feedbackText != null)
            {
                feedbackText.text = "Da trang bi mac dinh!";
                feedbackText.color = new Color(0.35f, 0.94f, 0.55f, 1f);
            }
        }
        else
        {
            if (PlayerStatsManager.TryBuySkill(selectedSkill))
            {
                if (feedbackText != null)
                {
                    feedbackText.text = "Mo khoa & trang bi thanh cong!";
                    feedbackText.color = new Color(0.35f, 0.94f, 0.55f, 1f);
                }
            }
            else
            {
                SkillDefinition def = SkillCatalog.Get(selectedSkill);
                int need = Mathf.Max(0, def.price - PlayerStatsManager.PlayerGold);
                if (feedbackText != null)
                {
                    feedbackText.text = $"Thieu {need} Vang!";
                    feedbackText.color = new Color(1.00f, 0.42f, 0.40f, 1f);
                }
            }
        }

        RefreshShop();
        MenuUI menu = FindFirstObjectByType<MenuUI>();
        if (menu != null) menu.RefreshPlayerStats();
    }

    public void RefreshShop()
    {
        if (!built) return;

        int gold = PlayerStatsManager.PlayerGold;
        SkillType equipped = PlayerStatsManager.EquippedSkill;

        if (goldHeaderText != null)
        {
            goldHeaderText.text = gold.ToString("N0");
        }

        SkillDefinition[] all = SkillCatalog.All;
        for (int i = 0; i < all.Length; i++)
        {
            SkillDefinition def = all[i];
            bool isSelected = def.type == selectedSkill;
            bool isEquipped = def.type == equipped;
            bool isOwned = PlayerStatsManager.IsSkillOwned(def.type);

            if (cardBorders[i] != null)
            {
                // Giữ đúng màu khung phân cấp từ Xanh Lá -> Tím, làm sáng rực hơn khi đang chọn!
                cardBorders[i].color = isSelected
                    ? Color.Lerp(def.tierColor, Color.white, 0.42f)
                    : def.tierColor;
            }

            if (cardIcons[i] != null)
            {
                cardIcons[i].color = isOwned ? Color.white : new Color(0.45f, 0.48f, 0.56f, 0.80f);
            }

            if (cardLockBadges[i] != null)
            {
                cardLockBadges[i].SetActive(!isOwned);
            }

            if (cardStatusTexts[i] != null)
            {
                if (isEquipped)
                {
                    cardStatusTexts[i].text = "DANG DUNG";
                    cardStatusTexts[i].color = new Color(0.35f, 0.95f, 0.56f, 1f);
                }
                else if (isOwned)
                {
                    cardStatusTexts[i].text = "DA SO HUU";
                    cardStatusTexts[i].color = new Color(0.62f, 0.86f, 1.00f, 1f);
                }
                else
                {
                    cardStatusTexts[i].text = $"{def.price} VANG";
                    cardStatusTexts[i].color = UIThemeAssets.GoldAccent;
                }
            }
        }

        SkillDefinition selDef = SkillCatalog.Get(selectedSkill);
        bool selOwned = PlayerStatsManager.IsSkillOwned(selectedSkill);
        bool selEquipped = selectedSkill == equipped;

        if (detailNameText != null)
        {
            detailNameText.text = $"{selDef.displayName}  •  [{selDef.tierLabel}]";
            detailNameText.color = selDef.tierColor;
        }

        if (detailStatsText != null)
        {
            detailStatsText.text = selDef.statsSummary;
        }

        if (detailDescText != null)
        {
            detailDescText.text = selDef.description;
        }

        if (actionButton != null && actionButtonText != null && actionButtonBg != null)
        {
            if (selEquipped)
            {
                actionButton.interactable = false;
                actionButtonText.text = "DANG TRANG BI";
                actionButtonBg.color = new Color(0.22f, 0.28f, 0.38f, 1f);
            }
            else if (selOwned)
            {
                actionButton.interactable = true;
                actionButtonText.text = "TRANG BI";
                actionButtonBg.color = UIThemeAssets.PrimaryCyan;
            }
            else
            {
                bool canAfford = gold >= selDef.price;
                actionButton.interactable = true;
                actionButtonText.text = $"MUA ({selDef.price} VANG)";
                actionButtonBg.color = canAfford ? UIThemeAssets.SuccessGreen : new Color(0.45f, 0.25f, 0.26f, 1f);
            }
        }
    }

    TMP_Text CreateText(
        string name,
        Transform parent,
        Vector2 aMin,
        Vector2 aMax,
        string content,
        float fsMin,
        float fsMax,
        FontStyles style,
        TextAlignmentOptions align,
        Color col
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
        txt.fontSizeMin = fsMin;
        txt.fontSizeMax = fsMax;
        txt.fontStyle = style;
        txt.alignment = align;
        txt.color = col;
        txt.raycastTarget = false;
        return txt;
    }

    Button CreateButton(
        string name,
        Transform parent,
        Vector2 aMin,
        Vector2 aMax,
        Color bgColor,
        string label,
        float fsMin,
        float fsMax
    )
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        CreateText("Label", go.transform, Vector2.zero, Vector2.one, label, fsMin, fsMax, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        return btn;
    }
}
