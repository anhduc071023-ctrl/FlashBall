using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchFlowUI : MonoBehaviour
{
    public static MatchFlowUI Instance;

    [Header("Panels")]
    public GameObject skillSelectPanel;

    public GameObject skillUseRoot;

    [Header("Texts")]
    public TMP_Text countdownText;

    MatchPhase lastPhase = (MatchPhase)(-1);
    int lastSeconds = -1;
    bool skillSelectStyled;

    // Compact Scrollable Skill Selector UI refs
    RectTransform cardBorderRt;
    TMP_Text goldText;
    GameObject hintBarGo;
    GameObject detailBoxGo;
    TMP_Text detailTitleText;
    TMP_Text detailDescText;
    TMP_Text detailFeedbackText;
    Button detailActionBtn;
    Image detailActionBg;
    TMP_Text detailActionLabel;
    ScrollRect skillScrollRect;

    struct SkillTileUI
    {
        public SkillType type;
        public Image borderImg;
        public Image iconImg;
        public GameObject lockBadgeGo;
        public TMP_Text statusLabel;
    }

    readonly List<SkillTileUI> skillTiles = new List<SkillTileUI>();
    SkillType inspectedSkill = SkillType.None;
    TMP_FontAsset cachedFont;

    const float PanelWidth = 940f;
    const float CollapsedHeight = 356f;
    const float ExpandedHeight = 524f;

    void Awake()
    {
        Instance = this;
        StyleSkillSelectUI();
        ApplyState(MatchPhase.Waiting, 0);
    }

    public void ApplyState(MatchPhase phase, int secondsRemaining)
    {
        if (phase == lastPhase && secondsRemaining == lastSeconds)
            return;

        bool phaseChanged = phase != lastPhase;
        lastPhase = phase;
        lastSeconds = secondsRemaining;

        bool selectingSkill = phase == MatchPhase.SkillSelect;
        bool playing = phase == MatchPhase.Playing;

        if (skillSelectPanel != null)
        {
            if (selectingSkill && !skillSelectStyled)
            {
                StyleSkillSelectUI();
            }

            skillSelectPanel.SetActive(selectingSkill);

            if (selectingSkill)
            {
                skillSelectPanel.transform.SetAsLastSibling();
                if (phaseChanged)
                {
                    // Mở màn chọn kỹ năng ở trạng thái gọn nhẹ — chỉ hiện mô tả khi bấm vào biểu tượng!
                    inspectedSkill = SkillType.None;
                    if (detailFeedbackText != null) detailFeedbackText.text = "";
                    if (skillScrollRect != null) skillScrollRect.horizontalNormalizedPosition = 0f;
                    RefreshSkillSelectorUI();
                }
            }
        }

        if (skillUseRoot != null)
        {
            skillUseRoot.SetActive(playing);
        }

        if (countdownText != null)
        {
            countdownText.text = selectingSkill
                ? $"BAT DAU SAU: {secondsRemaining}s"
                : "";
        }

        if (selectingSkill && secondsRemaining > 0 && secondsRemaining <= 3)
        {
            GameAudioManager.PlaySFX(GameSfxType.CountdownTick, 0.78f, secondsRemaining == 1 ? 1.15f : 1.0f);
        }
        else if (phaseChanged && playing)
        {
            GameAudioManager.PlaySFX(GameSfxType.MatchStart, 0.90f);
        }
    }

    void StyleSkillSelectUI()
    {
        if (skillSelectStyled || skillSelectPanel == null)
            return;

        skillSelectStyled = true;

        // Nền overlay tối nhẹ full-screen
        RectTransform panelRt = skillSelectPanel.GetComponent<RectTransform>();
        if (panelRt != null)
        {
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.localScale = Vector3.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
        }

        Image panelBg = skillSelectPanel.GetComponent<Image>();
        if (panelBg == null) panelBg = skillSelectPanel.AddComponent<Image>();
        panelBg.color = new Color(0.03f, 0.05f, 0.10f, 0.68f);
        panelBg.raycastTarget = true;

        cachedFont = countdownText != null ? countdownText.font : null;

        // Ẩn các thẻ Skill cũ cứng trong Scene (thay bằng danh sách cuộn động từ SkillCatalog)
        SkillSelectButtonUI[] legacyButtons = skillSelectPanel.GetComponentsInChildren<SkillSelectButtonUI>(true);
        foreach (SkillSelectButtonUI sb in legacyButtons)
        {
            if (sb != null) sb.gameObject.SetActive(false);
        }
        Transform oldContainer = skillSelectPanel.transform.Find("SkillContainer");
        if (oldContainer != null) oldContainer.gameObject.SetActive(false);

        // 1. Khung trung tâm GỌN GÀNG (940 x 356 khi thu gọn -> 940 x 524 khi bấm xem mô tả)
        GameObject cardBorder = new GameObject("SkillSelectCardBorder", typeof(RectTransform), typeof(Image));
        cardBorder.transform.SetParent(skillSelectPanel.transform, false);
        cardBorder.transform.SetAsFirstSibling();

        cardBorderRt = cardBorder.GetComponent<RectTransform>();
        cardBorderRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardBorderRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardBorderRt.pivot = new Vector2(0.5f, 0.5f);
        cardBorderRt.localScale = Vector3.one;
        cardBorderRt.anchoredPosition = Vector2.zero;
        cardBorderRt.sizeDelta = new Vector2(PanelWidth, CollapsedHeight);

        Image borderImg = cardBorder.GetComponent<Image>();
        borderImg.color = UIThemeAssets.CardBorder;
        borderImg.raycastTarget = false;

        GameObject cardBody = new GameObject("SkillSelectCardBody", typeof(RectTransform), typeof(Image));
        cardBody.transform.SetParent(cardBorder.transform, false);
        RectTransform bodyRt = cardBody.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(3.5f, 3.5f);
        bodyRt.offsetMax = new Vector2(-3.5f, -3.5f);
        Image bodyImg = cardBody.GetComponent<Image>();
        bodyImg.color = UIThemeAssets.CardBody;
        bodyImg.raycastTarget = false;

        // ===== 2. HEADER BAR GỌN (74px trên cùng) =====
        GameObject header = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(cardBody.transform, false);
        RectTransform hRt = header.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.anchoredPosition = Vector2.zero;
        hRt.sizeDelta = new Vector2(0f, 74f);
        Image hImg = header.GetComponent<Image>();
        hImg.color = new Color(0.10f, 0.14f, 0.23f, 0.98f);
        hImg.raycastTarget = false;

        CreateText(
            "TitleTxt",
            header.transform,
            new Vector2(0.025f, 0.08f),
            new Vector2(0.36f, 0.92f),
            "CHON KY NANG",
            22f,
            30f,
            FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft,
            Color.white
        );

        if (countdownText != null)
        {
            countdownText.transform.SetParent(header.transform, false);
            RectTransform crt = countdownText.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.36f, 0.08f);
            crt.anchorMax = new Vector2(0.71f, 0.92f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.localScale = Vector3.one;
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;

            countdownText.enableAutoSizing = true;
            countdownText.fontSizeMin = 20f;
            countdownText.fontSizeMax = 28f;
            countdownText.fontStyle = FontStyles.Bold;
            countdownText.alignment = TextAlignmentOptions.Center;
            countdownText.color = UIThemeAssets.GoldAccent;
            countdownText.raycastTarget = false;
        }

        // Thanh Vàng để mở khóa kỹ năng ngay tại màn chọn!
        GameObject goldPill = new GameObject("SkillGoldPill", typeof(RectTransform), typeof(Image));
        goldPill.transform.SetParent(header.transform, false);
        RectTransform gpRt = goldPill.GetComponent<RectTransform>();
        gpRt.anchorMin = new Vector2(0.73f, 0.14f);
        gpRt.anchorMax = new Vector2(0.975f, 0.86f);
        gpRt.offsetMin = Vector2.zero;
        gpRt.offsetMax = Vector2.zero;
        Image gpImg = goldPill.GetComponent<Image>();
        gpImg.color = new Color(0.06f, 0.08f, 0.14f, 0.96f);
        gpImg.raycastTarget = false;

        GameObject coinGo = new GameObject("Coin", typeof(RectTransform), typeof(Image));
        coinGo.transform.SetParent(goldPill.transform, false);
        RectTransform ciRt = coinGo.GetComponent<RectTransform>();
        ciRt.anchorMin = new Vector2(0.06f, 0.14f);
        ciRt.anchorMax = new Vector2(0.28f, 0.86f);
        ciRt.offsetMin = Vector2.zero;
        ciRt.offsetMax = Vector2.zero;
        Image ciImg = coinGo.GetComponent<Image>();
        ciImg.sprite = UIThemeAssets.IconGold;
        ciImg.preserveAspect = true;
        ciImg.raycastTarget = false;

        goldText = CreateText(
            "GoldVal",
            goldPill.transform,
            new Vector2(0.30f, 0.05f),
            new Vector2(0.95f, 0.95f),
            "0 VANG",
            18f,
            24f,
            FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft,
            UIThemeAssets.GoldAccent
        );

        // ===== 3. THANH CUỘN NGANG CHỨA CÁC BIỂU TƯỢNG KỸ NĂNG (SCROLLABLE) =====
        GameObject scrollArea = new GameObject("SkillScrollArea", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollArea.transform.SetParent(cardBody.transform, false);
        RectTransform saRt = scrollArea.GetComponent<RectTransform>();
        saRt.anchorMin = new Vector2(0f, 1f);
        saRt.anchorMax = new Vector2(1f, 1f);
        saRt.pivot = new Vector2(0.5f, 1f);
        saRt.anchoredPosition = new Vector2(0f, -84f);
        saRt.sizeDelta = new Vector2(-36f, 198f);
        Image saBg = scrollArea.GetComponent<Image>();
        saBg.color = new Color(0.05f, 0.08f, 0.14f, 0.72f);
        saBg.raycastTarget = true;

        // Viewport (RectMask2D cắt gọn mượt)
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollArea.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = new Vector2(0f, 0.10f);
        vpRt.anchorMax = new Vector2(1f, 1f);
        vpRt.offsetMin = new Vector2(6f, 2f);
        vpRt.offsetMax = new Vector2(-6f, -4f);
        Image vpImg = viewport.GetComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0.003f);
        vpImg.raycastTarget = true;

        // Content (HorizontalLayoutGroup + ContentSizeFitter tự giãn khi thêm kỹ năng mới!)
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform ctRt = content.GetComponent<RectTransform>();
        ctRt.anchorMin = new Vector2(0f, 0f);
        ctRt.anchorMax = new Vector2(0f, 1f);
        ctRt.pivot = new Vector2(0f, 0.5f);
        ctRt.anchoredPosition = Vector2.zero;
        ctRt.sizeDelta = new Vector2(0f, 0f);

        HorizontalLayoutGroup hlg = content.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.padding = new RectOffset(12, 12, 6, 6);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Thanh Scrollbar ngang tinh gọn bên dưới
        Scrollbar hBar = CreateHorizontalScrollbar(scrollArea.transform, new Vector2(0.03f, 0.015f), new Vector2(0.97f, 0.075f));

        skillScrollRect = scrollArea.GetComponent<ScrollRect>();
        skillScrollRect.content = ctRt;
        skillScrollRect.viewport = vpRt;
        skillScrollRect.horizontal = true;
        skillScrollRect.vertical = false;
        skillScrollRect.movementType = ScrollRect.MovementType.Elastic;
        skillScrollRect.elasticity = 0.08f;
        skillScrollRect.inertia = true;
        skillScrollRect.decelerationRate = 0.12f;
        skillScrollRect.scrollSensitivity = 32f;
        skillScrollRect.horizontalScrollbar = hBar;
        skillScrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        // Tạo toàn bộ biểu tượng kỹ năng động từ SkillCatalog.All
        skillTiles.Clear();
        SkillDefinition[] allSkills = SkillCatalog.All;
        for (int i = 0; i < allSkills.Length; i++)
        {
            BuildSkillIconTile(content.transform, allSkills[i]);
        }

        // ===== 4. DÒNG GỢI Ý GỌN (HIỆN KHI CHƯA BẤM BIỂU TƯỢNG NÀO) =====
        hintBarGo = new GameObject("HintBar", typeof(RectTransform), typeof(Image));
        hintBarGo.transform.SetParent(cardBody.transform, false);
        RectTransform hintRt = hintBarGo.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0f, 0f);
        hintRt.anchorMax = new Vector2(1f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.anchoredPosition = new Vector2(0f, 14f);
        hintRt.sizeDelta = new Vector2(-36f, 48f);
        Image hintBg = hintBarGo.GetComponent<Image>();
        hintBg.color = new Color(0.08f, 0.12f, 0.20f, 0.85f);
        hintBg.raycastTarget = false;

        CreateText(
            "HintTxt",
            hintBarGo.transform,
            new Vector2(0.02f, 0.05f),
            new Vector2(0.98f, 0.95f),
            "Cham bieu tuong de xem mo ta & chon ky nang da mo khoa  •  Mua ky nang moi ngoai Menu",
            16f,
            21f,
            FontStyles.Normal,
            TextAlignmentOptions.Center,
            new Color(0.76f, 0.87f, 0.98f, 0.95f)
        );

        // ===== 5. KHUNG MÔ TẢ & MỞ KHÓA (CHỈ HIỆN KHI BẤM VÀO BIỂU TƯỢNG!) =====
        detailBoxGo = new GameObject("SkillDetailBox", typeof(RectTransform), typeof(Image));
        detailBoxGo.transform.SetParent(cardBody.transform, false);
        RectTransform detRt = detailBoxGo.GetComponent<RectTransform>();
        detRt.anchorMin = new Vector2(0f, 0f);
        detRt.anchorMax = new Vector2(1f, 0f);
        detRt.pivot = new Vector2(0.5f, 0f);
        detRt.anchoredPosition = new Vector2(0f, 16f);
        detRt.sizeDelta = new Vector2(-36f, 206f);
        Image detBg = detailBoxGo.GetComponent<Image>();
        detBg.color = new Color(0.09f, 0.13f, 0.22f, 0.98f);
        detBg.raycastTarget = false;

        detailTitleText = CreateText(
            "DetailTitle",
            detailBoxGo.transform,
            new Vector2(0.025f, 0.64f),
            new Vector2(0.67f, 0.94f),
            "",
            20f,
            28f,
            FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft,
            Color.white
        );

        detailDescText = CreateText(
            "DetailDesc",
            detailBoxGo.transform,
            new Vector2(0.025f, 0.08f),
            new Vector2(0.67f, 0.64f),
            "",
            17f,
            22f,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft,
            new Color(0.84f, 0.90f, 0.98f, 1f)
        );
        detailDescText.textWrappingMode = TextWrappingModes.Normal;

        detailFeedbackText = CreateText(
            "DetailFeedback",
            detailBoxGo.transform,
            new Vector2(0.68f, 0.66f),
            new Vector2(0.975f, 0.94f),
            "",
            16f,
            21f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            UIThemeAssets.GoldAccent
        );

        // Nút Mua / Chọn kỹ năng
        GameObject actGo = new GameObject("DetailActionBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        actGo.transform.SetParent(detailBoxGo.transform, false);
        RectTransform actRt = actGo.GetComponent<RectTransform>();
        actRt.anchorMin = new Vector2(0.69f, 0.14f);
        actRt.anchorMax = new Vector2(0.97f, 0.64f);
        actRt.offsetMin = Vector2.zero;
        actRt.offsetMax = Vector2.zero;
        detailActionBg = actGo.GetComponent<Image>();
        detailActionBg.color = UIThemeAssets.SuccessGreen;
        detailActionBg.raycastTarget = true;

        detailActionBtn = actGo.GetComponent<Button>();
        detailActionBtn.targetGraphic = detailActionBg;
        detailActionBtn.onClick.AddListener(OnClickDetailAction);

        detailActionLabel = CreateText(
            "ActionLabel",
            actGo.transform,
            new Vector2(0.05f, 0.05f),
            new Vector2(0.95f, 0.95f),
            "CHON",
            18f,
            25f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            Color.white
        );

        detailBoxGo.SetActive(false);
        RefreshSkillSelectorUI();
    }

    void BuildSkillIconTile(Transform parent, SkillDefinition def)
    {
        SkillType st = def.type;

        GameObject tileGo = new GameObject("SkillTile_" + st, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        tileGo.transform.SetParent(parent, false);
        RectTransform rt = tileGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(150f, 158f);

        LayoutElement le = tileGo.GetComponent<LayoutElement>();
        le.preferredWidth = 150f;
        le.preferredHeight = 158f;
        le.minWidth = 150f;
        le.minHeight = 158f;

        Image borderImg = tileGo.GetComponent<Image>();
        borderImg.color = new Color(0.18f, 0.25f, 0.38f, 1f);
        borderImg.raycastTarget = true;

        Button btn = tileGo.GetComponent<Button>();
        btn.targetGraphic = borderImg;
        btn.onClick.AddListener(() => OnClickSkillTile(st));

        // Inner body
        GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(tileGo.transform, false);
        RectTransform inRt = inner.GetComponent<RectTransform>();
        inRt.anchorMin = Vector2.zero;
        inRt.anchorMax = Vector2.one;
        inRt.offsetMin = new Vector2(3f, 3f);
        inRt.offsetMax = new Vector2(-3f, -3f);
        Image inImg = inner.GetComponent<Image>();
        inImg.color = UIThemeAssets.SubCardBg;
        inImg.raycastTarget = false;

        // Dải màu phân cấp từ Xanh Lá -> Tím phía trên
        GameObject stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
        stripe.transform.SetParent(inner.transform, false);
        RectTransform sRt = stripe.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0.93f);
        sRt.anchorMax = new Vector2(1f, 1f);
        sRt.offsetMin = Vector2.zero;
        sRt.offsetMax = Vector2.zero;
        Image sImg = stripe.GetComponent<Image>();
        sImg.color = def.tierColor;
        sImg.raycastTarget = false;

        // Khung tròn chứa Icon kỹ năng
        GameObject frameGo = new GameObject("IconFrame", typeof(RectTransform), typeof(Image));
        frameGo.transform.SetParent(inner.transform, false);
        RectTransform fRt = frameGo.GetComponent<RectTransform>();
        fRt.anchorMin = new Vector2(0.5f, 0.62f);
        fRt.anchorMax = new Vector2(0.5f, 0.62f);
        fRt.pivot = new Vector2(0.5f, 0.5f);
        fRt.sizeDelta = new Vector2(88f, 88f);
        Image fImg = frameGo.GetComponent<Image>();
        if (UIThemeAssets.BtnCircleFrame != null)
        {
            fImg.sprite = UIThemeAssets.BtnCircleFrame;
            fImg.preserveAspect = true;
        }
        fImg.color = Color.white;
        fImg.raycastTarget = false;

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(frameGo.transform, false);
        RectTransform icRt = iconGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0.14f, 0.14f);
        icRt.anchorMax = new Vector2(0.86f, 0.86f);
        icRt.offsetMin = Vector2.zero;
        icRt.offsetMax = Vector2.zero;
        Image icImg = iconGo.GetComponent<Image>();
        icImg.sprite = UIThemeAssets.GetSkillIcon(st);
        icImg.preserveAspect = true;
        icImg.raycastTarget = false;

        // Biểu tượng ổ khóa góc trên phải nếu bị khóa
        GameObject lockGo = new GameObject("LockBadge", typeof(RectTransform), typeof(Image));
        lockGo.transform.SetParent(inner.transform, false);
        RectTransform lkRt = lockGo.GetComponent<RectTransform>();
        lkRt.anchorMin = new Vector2(1f, 1f);
        lkRt.anchorMax = new Vector2(1f, 1f);
        lkRt.pivot = new Vector2(1f, 1f);
        lkRt.anchoredPosition = new Vector2(-6f, -12f);
        lkRt.sizeDelta = new Vector2(36f, 36f);
        Image lkImg = lockGo.GetComponent<Image>();
        lkImg.sprite = UIThemeAssets.IconLock;
        lkImg.preserveAspect = true;
        lkImg.raycastTarget = false;

        // Tên ngắn gọn
        CreateText(
            "ShortName",
            inner.transform,
            new Vector2(0.04f, 0.17f),
            new Vector2(0.96f, 0.34f),
            def.shortName,
            14f,
            19f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            Color.white
        );

        // Trạng thái gọn (DANG DUNG / MIEN PHI / 300 V)
        TMP_Text statusLbl = CreateText(
            "StatusLbl",
            inner.transform,
            new Vector2(0.04f, 0.02f),
            new Vector2(0.96f, 0.17f),
            "",
            13f,
            17f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            UIThemeAssets.GoldAccent
        );

        skillTiles.Add(new SkillTileUI
        {
            type = st,
            borderImg = borderImg,
            iconImg = icImg,
            lockBadgeGo = lockGo,
            statusLabel = statusLbl
        });
    }

    void OnClickSkillTile(SkillType st)
    {
        inspectedSkill = st;
        if (detailFeedbackText != null) detailFeedbackText.text = "";

        // Nếu kỹ năng ĐÃ MỞ KHÓA, bấm vào vừa hiển thị mô tả vừa trang bị ngay cho nhanh trước khi hết giờ!
        if (PlayerStatsManager.IsSkillOwned(st))
        {
            EquipSkillOnLocalPlayer(st);
        }

        RefreshSkillSelectorUI();
    }

    void OnClickDetailAction()
    {
        if (inspectedSkill == SkillType.None)
            return;

        if (PlayerStatsManager.IsSkillOwned(inspectedSkill))
        {
            EquipSkillOnLocalPlayer(inspectedSkill);
            if (detailFeedbackText != null)
            {
                detailFeedbackText.text = "Da chon ky nang cho tran nay!";
                detailFeedbackText.color = new Color(0.35f, 0.95f, 0.56f, 1f);
            }
        }
        else
        {
            if (detailFeedbackText != null)
            {
                detailFeedbackText.text = "Mo khoa tai Menu -> KY NANG!";
                detailFeedbackText.color = UIThemeAssets.GoldAccent;
            }
        }

        RefreshSkillSelectorUI();
    }

    void EquipSkillOnLocalPlayer(SkillType st)
    {
        PlayerStatsManager.EquipSkill(st);

        PlayerAbilityController[] players = FindObjectsByType<PlayerAbilityController>(FindObjectsSortMode.None);
        foreach (PlayerAbilityController ab in players)
        {
            if (ab == null) continue;
            NetworkObject obj = ab.GetComponent<NetworkObject>();
            if (obj != null && obj.HasInputAuthority)
            {
                ab.SetSelectedSkill(st);
                break;
            }
        }
    }

    SkillType GetCurrentEquippedSkill()
    {
        PlayerAbilityController[] players = FindObjectsByType<PlayerAbilityController>(FindObjectsSortMode.None);
        foreach (PlayerAbilityController ab in players)
        {
            if (ab == null) continue;
            NetworkObject obj = ab.GetComponent<NetworkObject>();
            if (obj != null && obj.HasInputAuthority && ab.currentSkill != SkillType.None)
            {
                return ab.currentSkill;
            }
        }
        return PlayerStatsManager.EquippedSkill;
    }

    void RefreshSkillSelectorUI()
    {
        if (!skillSelectStyled)
            return;

        int gold = PlayerStatsManager.PlayerGold;
        SkillType equipped = GetCurrentEquippedSkill();

        if (goldText != null)
        {
            goldText.text = gold.ToString("N0");
        }

        for (int i = 0; i < skillTiles.Count; i++)
        {
            SkillTileUI tile = skillTiles[i];
            SkillDefinition def = SkillCatalog.Get(tile.type);
            bool owned = PlayerStatsManager.IsSkillOwned(tile.type);
            bool isEquipped = owned && tile.type == equipped;
            bool isInspected = tile.type == inspectedSkill;

            if (tile.borderImg != null)
            {
                // Màu khung xếp bậc từ Xanh Lá -> Tím, sáng nổi khi đang bấm xem!
                tile.borderImg.color = isInspected
                    ? Color.Lerp(def.tierColor, Color.white, 0.45f)
                    : def.tierColor;
            }

            if (tile.iconImg != null)
            {
                tile.iconImg.color = owned ? Color.white : new Color(0.45f, 0.48f, 0.56f, 0.78f);
            }

            if (tile.lockBadgeGo != null)
            {
                tile.lockBadgeGo.SetActive(!owned);
            }

            if (tile.statusLabel != null)
            {
                if (isEquipped)
                {
                    tile.statusLabel.text = "DANG DUNG";
                    tile.statusLabel.color = new Color(0.35f, 0.95f, 0.56f, 1f);
                }
                else if (owned)
                {
                    tile.statusLabel.text = "DA MO";
                    tile.statusLabel.color = new Color(0.62f, 0.86f, 1.00f, 1f);
                }
                else
                {
                    tile.statusLabel.text = $"{def.price} VANG";
                    tile.statusLabel.color = UIThemeAssets.GoldAccent;
                }
            }
        }

        // Chỉ mở khung mô tả khi người chơi đã nhấn vào một biểu tượng!
        bool showDetail = inspectedSkill != SkillType.None;
        if (hintBarGo != null) hintBarGo.SetActive(!showDetail);
        if (detailBoxGo != null) detailBoxGo.SetActive(showDetail);
        if (cardBorderRt != null)
        {
            cardBorderRt.sizeDelta = new Vector2(PanelWidth, showDetail ? ExpandedHeight : CollapsedHeight);
        }

        if (!showDetail)
            return;

        SkillDefinition selDef = SkillCatalog.Get(inspectedSkill);
        bool selOwned = PlayerStatsManager.IsSkillOwned(inspectedSkill);
        bool selEquipped = selOwned && inspectedSkill == equipped;

        if (detailTitleText != null)
        {
            detailTitleText.text = $"{selDef.displayName}   •   [{selDef.tierLabel}]   •   CD: {selDef.cooldown:0}s";
            detailTitleText.color = selDef.tierColor;
        }

        if (detailDescText != null)
        {
            detailDescText.text = selDef.description;
        }

        if (detailActionBtn != null && detailActionLabel != null && detailActionBg != null)
        {
            if (selEquipped)
            {
                detailActionBtn.interactable = false;
                detailActionLabel.text = "DANG CHON";
                detailActionBg.color = new Color(0.22f, 0.28f, 0.38f, 1f);
            }
            else if (selOwned)
            {
                detailActionBtn.interactable = true;
                detailActionLabel.text = "CHON KY NANG";
                detailActionBg.color = UIThemeAssets.PrimaryCyan;
            }
            else
            {
                detailActionBtn.interactable = false;
                detailActionLabel.text = $"MUA NGOAI MENU ({selDef.price}V)";
                detailActionBg.color = new Color(0.32f, 0.24f, 0.34f, 1f);
            }
        }
    }

    Scrollbar CreateHorizontalScrollbar(Transform parent, Vector2 aMin, Vector2 aMax)
    {
        GameObject sbGo = new GameObject("HScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGo.transform.SetParent(parent, false);
        RectTransform sbRt = sbGo.GetComponent<RectTransform>();
        sbRt.anchorMin = aMin;
        sbRt.anchorMax = aMax;
        sbRt.offsetMin = Vector2.zero;
        sbRt.offsetMax = Vector2.zero;
        Image trackImg = sbGo.GetComponent<Image>();
        trackImg.color = new Color(0.10f, 0.15f, 0.24f, 0.90f);

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

        Scrollbar sb = sbGo.GetComponent<Scrollbar>();
        sb.handleRect = hdRt;
        sb.targetGraphic = hdImg;
        sb.direction = Scrollbar.Direction.LeftToRight;
        return sb;
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
        if (cachedFont != null) txt.font = cachedFont;
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
}
