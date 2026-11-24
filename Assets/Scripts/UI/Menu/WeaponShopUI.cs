using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponShopUI : MonoBehaviour
{
    public static WeaponShopUI Instance;

    GameObject shopPanel;
    TMP_Text goldHeaderText;
    TMP_Text detailNameText;
    TMP_Text detailDescText;
    TMP_Text actionButtonText;
    Button actionButton;
    Image actionButtonBg;
    TMP_Text feedbackText;

    Image[] cardBorders;
    TMP_Text[] cardStatusTexts;

    WeaponType selectedWeapon = WeaponType.WindBlade;
    TMP_FontAsset uiFont;
    bool built;

    public bool IsOpen => shopPanel != null && shopPanel.activeSelf;

    public static WeaponShopUI EnsureInstance(Canvas canvas, TMP_FontAsset font)
    {
        if (Instance != null)
            return Instance;

        if (canvas == null)
            return null;

        GameObject root = new GameObject("WeaponShopRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        WeaponShopUI shop = root.AddComponent<WeaponShopUI>();
        shop.uiFont = font;
        shop.BuildShopUI();
        return shop;
    }

    void Awake()
    {
        Instance = this;
    }

    public void OpenShop()
    {
        if (!built) BuildShopUI();
        selectedWeapon = PlayerStatsManager.EquippedWeapon;
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

    void BuildShopUI()
    {
        if (built) return;
        built = true;

        // Overlay nền tối mờ chặn click xuống Menu
        shopPanel = new GameObject("WeaponShopPanel", typeof(RectTransform), typeof(Image));
        shopPanel.transform.SetParent(transform, false);
        RectTransform pRt = shopPanel.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.offsetMin = Vector2.zero;
        pRt.offsetMax = Vector2.zero;
        Image pImg = shopPanel.GetComponent<Image>();
        pImg.color = new Color(0.02f, 0.04f, 0.08f, 0.92f);
        pImg.raycastTarget = true;

        // Khung chính Mobile Landscape (anchor % an toàn tai thỏ!)
        GameObject cardBorder = new GameObject("ShopCardBorder", typeof(RectTransform), typeof(Image));
        cardBorder.transform.SetParent(shopPanel.transform, false);
        RectTransform cbRt = cardBorder.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(0.03f, 0.05f);
        cbRt.anchorMax = new Vector2(0.97f, 0.95f);
        cbRt.offsetMin = Vector2.zero;
        cbRt.offsetMax = Vector2.zero;
        Image cbImg = cardBorder.GetComponent<Image>();
        cbImg.color = UIThemeAssets.CardBorder;
        cbImg.raycastTarget = false;

        GameObject cardBody = new GameObject("ShopCardBody", typeof(RectTransform), typeof(Image));
        cardBody.transform.SetParent(cardBorder.transform, false);
        RectTransform bodyRt = cardBody.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(2.5f, 2.5f);
        bodyRt.offsetMax = new Vector2(-2.5f, -2.5f);
        Image bodyImg = cardBody.GetComponent<Image>();
        bodyImg.color = UIThemeAssets.CardBody;
        bodyImg.raycastTarget = false;

        // ===== HEADER BAR (Top 18%) =====
        GameObject header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(cardBody.transform, false);
        RectTransform hRt = header.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 0.82f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.offsetMin = Vector2.zero;
        hRt.offsetMax = Vector2.zero;
        Image hBg = header.GetComponent<Image>();
        hBg.color = new Color(0.10f, 0.14f, 0.23f, 0.98f);
        hBg.raycastTarget = false;

        CreateText("ShopTitle", header.transform, new Vector2(0.025f, 0.05f), new Vector2(0.45f, 0.95f),
            "CUA HANG VU KHI NGUYEN TO", 11f, 18f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

        // Thanh số Vàng có icon đồng vàng
        GameObject goldPill = new GameObject("GoldPill", typeof(RectTransform), typeof(Image));
        goldPill.transform.SetParent(header.transform, false);
        RectTransform gpRt = goldPill.GetComponent<RectTransform>();
        gpRt.anchorMin = new Vector2(0.56f, 0.15f);
        gpRt.anchorMax = new Vector2(0.81f, 0.85f);
        gpRt.offsetMin = Vector2.zero;
        gpRt.offsetMax = Vector2.zero;
        Image gpImg = goldPill.GetComponent<Image>();
        gpImg.color = new Color(0.06f, 0.08f, 0.14f, 0.96f);
        gpImg.raycastTarget = false;

        GameObject coinIconGo = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        coinIconGo.transform.SetParent(goldPill.transform, false);
        RectTransform ciRt = coinIconGo.GetComponent<RectTransform>();
        ciRt.anchorMin = new Vector2(0.04f, 0.12f);
        ciRt.anchorMax = new Vector2(0.24f, 0.88f);
        ciRt.offsetMin = Vector2.zero;
        ciRt.offsetMax = Vector2.zero;
        Image ciImg = coinIconGo.GetComponent<Image>();
        ciImg.sprite = UIThemeAssets.IconGold;
        ciImg.preserveAspect = true;
        ciImg.raycastTarget = false;

        goldHeaderText = CreateText("GoldValue", goldPill.transform, new Vector2(0.26f, 0.05f), new Vector2(0.95f, 0.95f),
            "0 VANG", 10f, 15f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, UIThemeAssets.GoldAccent);

        // Nút Đóng
        Button closeBtn = CreateButton("CloseButton", header.transform, new Vector2(0.83f, 0.15f), new Vector2(0.975f, 0.85f),
            UIThemeAssets.DangerRed, "DONG", 10f, 14f);
        closeBtn.onClick.AddListener(CloseShop);

        // ===== DANH SÁCH VŨ KHÍ CÓ CUỘN NGANG (DỄ DÀNG MỞ RỘNG KHÔNG GIỚI HẠN VŨ KHÍ MỚI) =====
        GameObject scrollArea = new GameObject("WeaponScrollArea", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollArea.transform.SetParent(cardBody.transform, false);
        RectTransform rowRt = scrollArea.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.02f, 0.27f);
        rowRt.anchorMax = new Vector2(0.98f, 0.80f);
        rowRt.offsetMin = Vector2.zero;
        rowRt.offsetMax = Vector2.zero;
        Image saBg = scrollArea.GetComponent<Image>();
        saBg.color = new Color(0.05f, 0.08f, 0.14f, 0.65f);
        saBg.raycastTarget = true;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollArea.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = new Vector2(0f, 0.08f);
        vpRt.anchorMax = new Vector2(1f, 1f);
        vpRt.offsetMin = new Vector2(4f, 2f);
        vpRt.offsetMax = new Vector2(-4f, -4f);
        Image vpImg = viewport.GetComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0.003f);
        vpImg.raycastTarget = true;

        GameObject content = new GameObject("WeaponCardsContent", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform ctRt = content.GetComponent<RectTransform>();
        ctRt.anchorMin = new Vector2(0f, 0f);
        ctRt.anchorMax = new Vector2(0f, 1f);
        ctRt.pivot = new Vector2(0f, 0.5f);
        ctRt.anchoredPosition = Vector2.zero;
        ctRt.sizeDelta = new Vector2(0f, 0f);

        HorizontalLayoutGroup hlg = content.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.padding = new RectOffset(8, 8, 4, 4);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Thanh cuộn ngang dưới danh sách vũ khí
        GameObject sbGo = new GameObject("WeaponHScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGo.transform.SetParent(scrollArea.transform, false);
        RectTransform sbRt = sbGo.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0.03f, 0.01f);
        sbRt.anchorMax = new Vector2(0.97f, 0.06f);
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

        WeaponDefinition[] all = WeaponCatalog.All;
        cardBorders = new Image[all.Length];
        cardStatusTexts = new TMP_Text[all.Length];

        for (int i = 0; i < all.Length; i++)
        {
            WeaponDefinition def = all[i];
            WeaponType wt = def.type;

            GameObject cardGo = new GameObject("WeaponCard_" + wt, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            cardGo.transform.SetParent(content.transform, false);
            RectTransform cRt = cardGo.GetComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(152f, 0f);

            LayoutElement le = cardGo.GetComponent<LayoutElement>();
            le.preferredWidth = 152f;
            le.minWidth = 152f;

            Image border = cardGo.GetComponent<Image>();
            border.color = new Color(0.18f, 0.24f, 0.36f, 1f);
            border.raycastTarget = true;
            cardBorders[i] = border;

            Button btn = cardGo.GetComponent<Button>();
            btn.targetGraphic = border;
            btn.onClick.AddListener(() => SelectWeapon(wt));

            // Inner body
            GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(cardGo.transform, false);
            RectTransform inRt = inner.GetComponent<RectTransform>();
            inRt.anchorMin = Vector2.zero;
            inRt.anchorMax = Vector2.one;
            inRt.offsetMin = new Vector2(2.5f, 2.5f);
            inRt.offsetMax = new Vector2(-2.5f, -2.5f);
            Image inImg = inner.GetComponent<Image>();
            inImg.color = UIThemeAssets.SubCardBg;
            inImg.raycastTarget = false;

            // Top element stripe
            GameObject stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(inner.transform, false);
            RectTransform sRt = stripe.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0.94f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.offsetMin = Vector2.zero;
            sRt.offsetMax = Vector2.zero;
            Image sImg = stripe.GetComponent<Image>();
            sImg.color = def.primaryColor;
            sImg.raycastTarget = false;

            // Category badge (KIEM / DAO / THUONG / SONG KIEM / LUOI HAI / ...)
            CreateText("Cat", inner.transform, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.93f),
                def.categoryLabel, 7.5f, 10f, FontStyles.Bold, TextAlignmentOptions.Center, def.primaryColor);

            // Weapon Icon (512x512 sprite)
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(inner.transform, false);
            RectTransform icRt = iconGo.GetComponent<RectTransform>();
            icRt.anchorMin = new Vector2(0.14f, 0.36f);
            icRt.anchorMax = new Vector2(0.86f, 0.79f);
            icRt.offsetMin = Vector2.zero;
            icRt.offsetMax = Vector2.zero;
            Image icImg = iconGo.GetComponent<Image>();
            icImg.sprite = UIThemeAssets.GetWeaponIcon(wt);
            icImg.preserveAspect = true;
            icImg.raycastTarget = false;

            // Weapon Name
            CreateText("Name", inner.transform, new Vector2(0.04f, 0.19f), new Vector2(0.96f, 0.35f),
                def.displayName, 8.5f, 12f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            // Price / Owned / Equipped badge
            cardStatusTexts[i] = CreateText("Status", inner.transform, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.18f),
                "", 8f, 11f, FontStyles.Bold, TextAlignmentOptions.Center, UIThemeAssets.GoldAccent);
        }

        // ===== BOTTOM DETAIL & BUY/EQUIP BAR (3% -> 24%) =====
        GameObject bottomBar = new GameObject("DetailBar", typeof(RectTransform), typeof(Image));
        bottomBar.transform.SetParent(cardBody.transform, false);
        RectTransform botRt = bottomBar.GetComponent<RectTransform>();
        botRt.anchorMin = new Vector2(0.02f, 0.03f);
        botRt.anchorMax = new Vector2(0.98f, 0.24f);
        botRt.offsetMin = Vector2.zero;
        botRt.offsetMax = Vector2.zero;
        Image botImg = bottomBar.GetComponent<Image>();
        botImg.color = new Color(0.09f, 0.13f, 0.21f, 0.96f);
        botImg.raycastTarget = false;

        detailNameText = CreateText("DetailName", bottomBar.transform, new Vector2(0.02f, 0.56f), new Vector2(0.66f, 0.94f),
            "", 10f, 15f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

        detailDescText = CreateText("DetailDesc", bottomBar.transform, new Vector2(0.02f, 0.08f), new Vector2(0.66f, 0.56f),
            "", 8.5f, 11.5f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.80f, 0.87f, 0.95f, 1f));

        feedbackText = CreateText("Feedback", bottomBar.transform, new Vector2(0.67f, 0.68f), new Vector2(0.98f, 0.96f),
            "", 8f, 10.5f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.45f, 0.42f, 1f));

        actionButton = CreateButton("ActionButton", bottomBar.transform, new Vector2(0.68f, 0.12f), new Vector2(0.97f, 0.68f),
            UIThemeAssets.SuccessGreen, "TRANG BI", 10f, 14f);
        actionButtonBg = actionButton.GetComponent<Image>();
        actionButtonText = actionButton.GetComponentInChildren<TMP_Text>();
        actionButton.onClick.AddListener(OnClickBuyOrEquip);

        shopPanel.SetActive(false);
    }

    void SelectWeapon(WeaponType wt)
    {
        selectedWeapon = wt;
        if (feedbackText != null) feedbackText.text = "";
        RefreshShop();
    }

    void OnClickBuyOrEquip()
    {
        if (PlayerStatsManager.IsWeaponOwned(selectedWeapon))
        {
            PlayerStatsManager.EquipWeapon(selectedWeapon);
            if (feedbackText != null)
            {
                feedbackText.text = "Da trang bi!";
                feedbackText.color = new Color(0.35f, 0.94f, 0.55f, 1f);
            }
        }
        else
        {
            if (PlayerStatsManager.TryBuyWeapon(selectedWeapon))
            {
                if (feedbackText != null)
                {
                    feedbackText.text = "Mua & trang bi thanh cong!";
                    feedbackText.color = new Color(0.35f, 0.94f, 0.55f, 1f);
                }
            }
            else
            {
                WeaponDefinition def = WeaponCatalog.Get(selectedWeapon);
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
        WeaponType equipped = PlayerStatsManager.EquippedWeapon;

        if (goldHeaderText != null)
        {
            goldHeaderText.text = gold.ToString("N0");
        }

        WeaponDefinition[] all = WeaponCatalog.All;
        for (int i = 0; i < all.Length; i++)
        {
            WeaponDefinition def = all[i];
            bool isSelected = def.type == selectedWeapon;
            bool isEquipped = def.type == equipped;
            bool isOwned = PlayerStatsManager.IsWeaponOwned(def.type);

            if (cardBorders[i] != null)
            {
                cardBorders[i].color = isSelected
                    ? UIThemeAssets.GoldAccent
                    : (isEquipped ? def.primaryColor : new Color(0.18f, 0.24f, 0.36f, 1f));
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

        WeaponDefinition selDef = WeaponCatalog.Get(selectedWeapon);
        bool selOwned = PlayerStatsManager.IsWeaponOwned(selectedWeapon);
        bool selEquipped = selectedWeapon == equipped;

        if (detailNameText != null)
        {
            detailNameText.text = $"{selDef.displayName}  •  {selDef.categoryLabel}  ({selDef.elementLabel})";
            detailNameText.color = selDef.primaryColor;
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
                actionButtonBg.color = canAfford ? UIThemeAssets.SuccessGreen : new Color(0.42f, 0.26f, 0.26f, 1f);
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
