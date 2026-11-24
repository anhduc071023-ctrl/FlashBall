using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinShopUI : MonoBehaviour
{
    public static SkinShopUI Instance;

    GameObject shopPanel;
    TMP_Text goldHeaderText;
    TMP_Text detailNameText;
    TMP_Text detailDescText;
    TMP_Text actionButtonText;
    Button actionButton;
    Image actionButtonBg;
    TMP_Text feedbackText;

    Image[] cardBorders;
    Image[] cardPortraits;
    TMP_Text[] cardStatusTexts;

    SkinType selectedSkin = SkinType.DefaultStriker;
    TMP_FontAsset uiFont;
    bool built;

    public bool IsOpen => shopPanel != null && shopPanel.activeSelf;

    public static SkinShopUI EnsureInstance(Canvas canvas, TMP_FontAsset font)
    {
        if (Instance != null)
            return Instance;

        if (canvas == null)
            return null;

        GameObject root = new GameObject("SkinShopRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        SkinShopUI shop = root.AddComponent<SkinShopUI>();
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
        selectedSkin = PlayerStatsManager.EquippedSkin;
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

        shopPanel = new GameObject("SkinShopPanel", typeof(RectTransform), typeof(Image));
        shopPanel.transform.SetParent(transform, false);
        RectTransform pRt = shopPanel.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.offsetMin = Vector2.zero;
        pRt.offsetMax = Vector2.zero;
        Image pImg = shopPanel.GetComponent<Image>();
        pImg.color = new Color(0.02f, 0.04f, 0.08f, 0.92f);
        pImg.raycastTarget = true;

        GameObject cardBorder = new GameObject("SkinCardBorder", typeof(RectTransform), typeof(Image));
        cardBorder.transform.SetParent(shopPanel.transform, false);
        RectTransform cbRt = cardBorder.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(0.03f, 0.05f);
        cbRt.anchorMax = new Vector2(0.97f, 0.95f);
        cbRt.offsetMin = Vector2.zero;
        cbRt.offsetMax = Vector2.zero;
        cardBorder.GetComponent<Image>().color = new Color(0.18f, 0.88f, 0.82f, 0.96f);
        cardBorder.GetComponent<Image>().raycastTarget = false;

        GameObject cardBody = new GameObject("SkinCardBody", typeof(RectTransform), typeof(Image));
        cardBody.transform.SetParent(cardBorder.transform, false);
        RectTransform bodyRt = cardBody.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(2.5f, 2.5f);
        bodyRt.offsetMax = new Vector2(-2.5f, -2.5f);
        cardBody.GetComponent<Image>().color = UIThemeAssets.CardBody;
        cardBody.GetComponent<Image>().raycastTarget = false;

        // ===== HEADER =====
        GameObject header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(cardBody.transform, false);
        RectTransform hRt = header.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 0.82f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.offsetMin = Vector2.zero;
        hRt.offsetMax = Vector2.zero;
        header.GetComponent<Image>().color = new Color(0.10f, 0.14f, 0.23f, 0.98f);
        header.GetComponent<Image>().raycastTarget = false;

        CreateText("ShopTitle", header.transform, new Vector2(0.025f, 0.05f), new Vector2(0.50f, 0.95f),
            "CUA HANG NGOAI TRANG NHAN VAT", 11f, 18f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

        GameObject goldPill = new GameObject("GoldPill", typeof(RectTransform), typeof(Image));
        goldPill.transform.SetParent(header.transform, false);
        RectTransform gpRt = goldPill.GetComponent<RectTransform>();
        gpRt.anchorMin = new Vector2(0.58f, 0.15f);
        gpRt.anchorMax = new Vector2(0.81f, 0.85f);
        gpRt.offsetMin = Vector2.zero;
        gpRt.offsetMax = Vector2.zero;
        goldPill.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.14f, 0.96f);
        goldPill.GetComponent<Image>().raycastTarget = false;

        GameObject coinIconGo = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        coinIconGo.transform.SetParent(goldPill.transform, false);
        RectTransform ciRt = coinIconGo.GetComponent<RectTransform>();
        ciRt.anchorMin = new Vector2(0.05f, 0.12f);
        ciRt.anchorMax = new Vector2(0.28f, 0.88f);
        ciRt.offsetMin = Vector2.zero;
        ciRt.offsetMax = Vector2.zero;
        Image ciImg = coinIconGo.GetComponent<Image>();
        ciImg.sprite = UIThemeAssets.IconGold;
        ciImg.preserveAspect = true;
        ciImg.raycastTarget = false;

        goldHeaderText = CreateText("GoldValue", goldPill.transform, new Vector2(0.30f, 0.05f), new Vector2(0.95f, 0.95f),
            "0", 10f, 15f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, UIThemeAssets.GoldAccent);

        Button closeBtn = CreateButton("CloseButton", header.transform, new Vector2(0.83f, 0.15f), new Vector2(0.975f, 0.85f),
            UIThemeAssets.DangerRed, "DONG", 10f, 14f);
        closeBtn.onClick.AddListener(CloseShop);

        // ===== HORIZONTAL SCROLLABLE SKIN CARDS =====
        GameObject scrollArea = new GameObject("SkinScrollArea", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollArea.transform.SetParent(cardBody.transform, false);
        RectTransform rowRt = scrollArea.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.02f, 0.27f);
        rowRt.anchorMax = new Vector2(0.98f, 0.80f);
        rowRt.offsetMin = Vector2.zero;
        rowRt.offsetMax = Vector2.zero;
        scrollArea.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.14f, 0.65f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollArea.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = new Vector2(0f, 0.08f);
        vpRt.anchorMax = new Vector2(1f, 1f);
        vpRt.offsetMin = new Vector2(4f, 2f);
        vpRt.offsetMax = new Vector2(-4f, -4f);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.003f);

        GameObject content = new GameObject("SkinCardsContent", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform ctRt = content.GetComponent<RectTransform>();
        ctRt.anchorMin = new Vector2(0f, 0f);
        ctRt.anchorMax = new Vector2(0f, 1f);
        ctRt.pivot = new Vector2(0f, 0.5f);
        ctRt.anchoredPosition = Vector2.zero;
        ctRt.sizeDelta = Vector2.zero;

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

        GameObject sbGo = new GameObject("SkinHScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
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
        hdImg.color = new Color(0.18f, 0.88f, 0.82f, 1f);

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

        SkinDefinition[] all = SkinCatalog.All;
        cardBorders = new Image[all.Length];
        cardPortraits = new Image[all.Length];
        cardStatusTexts = new TMP_Text[all.Length];

        for (int i = 0; i < all.Length; i++)
        {
            SkinDefinition def = all[i];
            SkinType st = def.type;

            GameObject cardGo = new GameObject("SkinCard_" + st, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            cardGo.transform.SetParent(content.transform, false);
            RectTransform cRt = cardGo.GetComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(154f, 0f);

            LayoutElement le = cardGo.GetComponent<LayoutElement>();
            le.preferredWidth = 154f;
            le.minWidth = 154f;

            Image border = cardGo.GetComponent<Image>();
            border.color = new Color(0.18f, 0.24f, 0.36f, 1f);
            border.raycastTarget = true;
            cardBorders[i] = border;

            Button btn = cardGo.GetComponent<Button>();
            btn.targetGraphic = border;
            btn.onClick.AddListener(() => SelectSkin(st));

            GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(cardGo.transform, false);
            RectTransform inRt = inner.GetComponent<RectTransform>();
            inRt.anchorMin = Vector2.zero;
            inRt.anchorMax = Vector2.one;
            inRt.offsetMin = new Vector2(2.5f, 2.5f);
            inRt.offsetMax = new Vector2(-2.5f, -2.5f);
            inner.GetComponent<Image>().color = UIThemeAssets.SubCardBg;
            inner.GetComponent<Image>().raycastTarget = false;

            GameObject stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(inner.transform, false);
            RectTransform sRt = stripe.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0.94f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.offsetMin = Vector2.zero;
            sRt.offsetMax = Vector2.zero;
            stripe.GetComponent<Image>().color = def.suitAccentColor;
            stripe.GetComponent<Image>().raycastTarget = false;

            CreateText("Rarity", inner.transform, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.93f),
                def.rarityLabel, 7.5f, 10f, FontStyles.Bold, TextAlignmentOptions.Center, def.suitAccentColor);

            GameObject iconGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(inner.transform, false);
            RectTransform icRt = iconGo.GetComponent<RectTransform>();
            icRt.anchorMin = new Vector2(0.10f, 0.35f);
            icRt.anchorMax = new Vector2(0.90f, 0.79f);
            icRt.offsetMin = Vector2.zero;
            icRt.offsetMax = Vector2.zero;
            Image icImg = iconGo.GetComponent<Image>();
            icImg.sprite = UIThemeAssets.GetSkinIcon(st);
            icImg.preserveAspect = true;
            icImg.raycastTarget = false;
            cardPortraits[i] = icImg;

            CreateText("Name", inner.transform, new Vector2(0.04f, 0.19f), new Vector2(0.96f, 0.34f),
                def.displayName, 8f, 11.5f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            cardStatusTexts[i] = CreateText("Status", inner.transform, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.18f),
                "", 8f, 11f, FontStyles.Bold, TextAlignmentOptions.Center, UIThemeAssets.GoldAccent);
        }

        // ===== BOTTOM DETAIL BAR =====
        GameObject bottomBar = new GameObject("DetailBar", typeof(RectTransform), typeof(Image));
        bottomBar.transform.SetParent(cardBody.transform, false);
        RectTransform botRt = bottomBar.GetComponent<RectTransform>();
        botRt.anchorMin = new Vector2(0.02f, 0.03f);
        botRt.anchorMax = new Vector2(0.98f, 0.24f);
        botRt.offsetMin = Vector2.zero;
        botRt.offsetMax = Vector2.zero;
        bottomBar.GetComponent<Image>().color = new Color(0.09f, 0.13f, 0.21f, 0.96f);
        bottomBar.GetComponent<Image>().raycastTarget = false;

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

    void SelectSkin(SkinType st)
    {
        selectedSkin = st;
        if (feedbackText != null) feedbackText.text = "";
        RefreshShop();
    }

    void OnClickBuyOrEquip()
    {
        if (PlayerStatsManager.IsSkinOwned(selectedSkin))
        {
            PlayerStatsManager.EquipSkin(selectedSkin);
            if (feedbackText != null)
            {
                feedbackText.text = "Da trang bi ngoai trang!";
                feedbackText.color = new Color(0.35f, 0.94f, 0.55f, 1f);
            }
        }
        else
        {
            if (PlayerStatsManager.TryBuySkin(selectedSkin))
            {
                if (feedbackText != null)
                {
                    feedbackText.text = "Mua & trang bi thanh cong!";
                    feedbackText.color = new Color(0.35f, 0.94f, 0.55f, 1f);
                }
            }
            else
            {
                SkinDefinition def = SkinCatalog.Get(selectedSkin);
                int need = Mathf.Max(0, def.price - PlayerStatsManager.PlayerGold);
                if (feedbackText != null)
                {
                    feedbackText.text = $"Thieu {need:N0}!";
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
        SkinType equipped = PlayerStatsManager.EquippedSkin;

        if (goldHeaderText != null)
        {
            goldHeaderText.text = gold.ToString("N0");
        }

        SkinDefinition[] all = SkinCatalog.All;
        for (int i = 0; i < all.Length; i++)
        {
            SkinDefinition def = all[i];
            bool isSelected = def.type == selectedSkin;
            bool isEquipped = def.type == equipped;
            bool isOwned = PlayerStatsManager.IsSkinOwned(def.type);

            if (cardPortraits != null && i < cardPortraits.Length && cardPortraits[i] != null)
            {
                cardPortraits[i].sprite = UIThemeAssets.GetSkinIcon(def.type);
            }

            if (cardBorders[i] != null)
            {
                cardBorders[i].color = isSelected
                    ? UIThemeAssets.GoldAccent
                    : (isEquipped ? def.suitAccentColor : new Color(0.18f, 0.24f, 0.36f, 1f));
            }

            if (cardStatusTexts[i] != null)
            {
                if (isEquipped)
                {
                    cardStatusTexts[i].text = "DANG MAC";
                    cardStatusTexts[i].color = new Color(0.35f, 0.95f, 0.56f, 1f);
                }
                else if (isOwned)
                {
                    cardStatusTexts[i].text = "DA SO HUU";
                    cardStatusTexts[i].color = new Color(0.62f, 0.86f, 1.00f, 1f);
                }
                else
                {
                    cardStatusTexts[i].text = def.price.ToString("N0");
                    cardStatusTexts[i].color = UIThemeAssets.GoldAccent;
                }
            }
        }

        SkinDefinition selDef = SkinCatalog.Get(selectedSkin);
        bool selOwned = PlayerStatsManager.IsSkinOwned(selectedSkin);
        bool selEquipped = selectedSkin == equipped;

        if (detailNameText != null)
        {
            detailNameText.text = $"{selDef.displayName}  •  {selDef.rarityLabel}";
            detailNameText.color = selDef.suitAccentColor;
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
                actionButtonText.text = "DANG MAC";
                actionButtonBg.color = new Color(0.22f, 0.28f, 0.38f, 1f);
            }
            else if (selOwned)
            {
                actionButton.interactable = true;
                actionButtonText.text = "MAC NGAY";
                actionButtonBg.color = UIThemeAssets.PrimaryCyan;
            }
            else
            {
                bool canAfford = gold >= selDef.price;
                actionButton.interactable = true;
                actionButtonText.text = $"MUA ({selDef.price:N0})";
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

    Button CreateButton(
        string name,
        Transform parent,
        Vector2 aMin,
        Vector2 aMax,
        Color bgColor,
        string label,
        float minSize,
        float maxSize
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

        CreateText("Label", go.transform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f),
            label, minSize, maxSize, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

        return btn;
    }
}
