using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Instance;

    GameObject panel;
    Button rankTabBtn;
    Image rankTabBg;
    Button soloTabBtn;
    Image soloTabBg;
    Button refreshBtn;
    TMP_Text refreshBtnLabel;
    TMP_Text syncStatusText;

    Transform listContent;
    ScrollRect scrollRect;
    TMP_Text colHeaderRightText;
    TMP_Text footerSelfText;
    Image footerRankIcon;

    bool showingSoloTab;
    bool isSyncing;
    TMP_FontAsset uiFont;
    bool built;

    public bool IsOpen => panel != null && panel.activeSelf;

    public static LeaderboardUI EnsureInstance(Canvas canvas, TMP_FontAsset font)
    {
        if (Instance != null)
            return Instance;

        if (canvas == null)
            return null;

        GameObject root = new GameObject("LeaderboardRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        LeaderboardUI ui = root.AddComponent<LeaderboardUI>();
        ui.uiFont = font;
        ui.BuildUI();
        return ui;
    }

    void Awake()
    {
        Instance = this;
    }

    public void OpenLeaderboard(bool openSolo = false)
    {
        if (!built) BuildUI();
        showingSoloTab = openSolo;
        if (panel != null)
        {
            panel.SetActive(true);
            transform.SetAsLastSibling();
        }
        RefreshList();
        OnClickRefreshLeaderboard();
    }

    public void CloseLeaderboard()
    {
        if (panel != null) panel.SetActive(false);
    }

    void BuildUI()
    {
        if (built) return;
        built = true;

        panel = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        RectTransform pRt = panel.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.offsetMin = Vector2.zero;
        pRt.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 0.92f);
        panel.GetComponent<Image>().raycastTarget = true;

        // Main card
        GameObject border = new GameObject("LbBorder", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(panel.transform, false);
        RectTransform bRt = border.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.06f, 0.05f);
        bRt.anchorMax = new Vector2(0.94f, 0.95f);
        bRt.offsetMin = Vector2.zero;
        bRt.offsetMax = Vector2.zero;
        border.GetComponent<Image>().color = UIThemeAssets.CardBorder;
        border.GetComponent<Image>().raycastTarget = false;

        GameObject body = new GameObject("LbBody", typeof(RectTransform), typeof(Image));
        body.transform.SetParent(border.transform, false);
        RectTransform bodyRt = body.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(2.5f, 2.5f);
        bodyRt.offsetMax = new Vector2(-2.5f, -2.5f);
        body.GetComponent<Image>().color = UIThemeAssets.CardBody;
        body.GetComponent<Image>().raycastTarget = false;

        // ===== HEADER BAR =====
        GameObject header = CreatePanel(body.transform, "Header", new Vector2(0f, 0.84f), new Vector2(1f, 1f), new Color(0.10f, 0.14f, 0.23f, 0.98f));

        GameObject trophyGo = new GameObject("Trophy", typeof(RectTransform), typeof(Image));
        trophyGo.transform.SetParent(header.transform, false);
        RectTransform trRt = trophyGo.GetComponent<RectTransform>();
        trRt.anchorMin = new Vector2(0.02f, 0.14f);
        trRt.anchorMax = new Vector2(0.085f, 0.86f);
        trRt.offsetMin = Vector2.zero;
        trRt.offsetMax = Vector2.zero;
        Image trImg = trophyGo.GetComponent<Image>();
        trImg.sprite = UIThemeAssets.IconLeaderboard;
        trImg.preserveAspect = true;
        trImg.raycastTarget = false;

        CreateText("Title", header.transform, new Vector2(0.09f, 0.08f), new Vector2(0.33f, 0.92f),
            "BANG XEP HANG", 10f, 15.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

        // Tabs: RANK | SOLO | CAP NHAT | DONG
        rankTabBtn = CreateButton("RankTab", header.transform, new Vector2(0.34f, 0.15f), new Vector2(0.50f, 0.85f),
            UIThemeAssets.PrimaryCyan, "BXH RANK", 8.5f, 12.5f);
        rankTabBg = rankTabBtn.GetComponent<Image>();
        rankTabBtn.onClick.AddListener(() => { showingSoloTab = false; RefreshList(); });

        soloTabBtn = CreateButton("SoloTab", header.transform, new Vector2(0.515f, 0.15f), new Vector2(0.675f, 0.85f),
            UIThemeAssets.SubCardBg, "BXH SOLO", 8.5f, 12.5f);
        soloTabBg = soloTabBtn.GetComponent<Image>();
        soloTabBtn.onClick.AddListener(() => { showingSoloTab = true; RefreshList(); });

        refreshBtn = CreateButton("RefreshBtn", header.transform, new Vector2(0.69f, 0.15f), new Vector2(0.845f, 0.85f),
            UIThemeAssets.SuccessGreen, "CAP NHAT", 8.5f, 12.5f);
        refreshBtnLabel = refreshBtn.GetComponentInChildren<TMP_Text>();
        refreshBtn.onClick.AddListener(OnClickRefreshLeaderboard);

        Button closeBtn = CreateButton("CloseBtn", header.transform, new Vector2(0.86f, 0.15f), new Vector2(0.98f, 0.85f),
            UIThemeAssets.DangerRed, "DONG", 8.5f, 12.5f);
        closeBtn.onClick.AddListener(CloseLeaderboard);

        // ===== COLUMN HEADER =====
        GameObject colHead = CreatePanel(body.transform, "ColHeader", new Vector2(0.02f, 0.76f), new Vector2(0.98f, 0.83f), new Color(0.08f, 0.12f, 0.20f, 0.95f));
        CreateText("ColRank", colHead.transform, new Vector2(0.02f, 0f), new Vector2(0.14f, 1f),
            "HANG", 7.5f, 10.5f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.65f, 0.78f, 0.92f, 1f));
        CreateText("ColPlayer", colHead.transform, new Vector2(0.16f, 0f), new Vector2(0.60f, 1f),
            "NGUOI CHOI", 7.5f, 10.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.65f, 0.78f, 0.92f, 1f));
        colHeaderRightText = CreateText("ColScore", colHead.transform, new Vector2(0.60f, 0f), new Vector2(0.96f, 1f),
            "BAC RANK / ELO", 7.5f, 10.5f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Color(0.65f, 0.78f, 0.92f, 1f));

        // ===== VERTICAL SCROLL LIST (18% -> 75%) =====
        GameObject scrollArea = new GameObject("LbScrollArea", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollArea.transform.SetParent(body.transform, false);
        RectTransform saRt = scrollArea.GetComponent<RectTransform>();
        saRt.anchorMin = new Vector2(0.02f, 0.18f);
        saRt.anchorMax = new Vector2(0.98f, 0.75f);
        saRt.offsetMin = Vector2.zero;
        saRt.offsetMax = Vector2.zero;
        scrollArea.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.14f, 0.65f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollArea.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = new Vector2(0f, 0f);
        vpRt.anchorMax = new Vector2(0.965f, 1f);
        vpRt.offsetMin = new Vector2(4f, 4f);
        vpRt.offsetMax = new Vector2(-4f, -4f);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.003f);

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform ctRt = content.GetComponent<RectTransform>();
        ctRt.anchorMin = new Vector2(0f, 1f);
        ctRt.anchorMax = new Vector2(1f, 1f);
        ctRt.pivot = new Vector2(0.5f, 1f);
        ctRt.anchoredPosition = Vector2.zero;
        ctRt.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Vertical Scrollbar
        GameObject sbGo = new GameObject("VScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGo.transform.SetParent(scrollArea.transform, false);
        RectTransform sbRt = sbGo.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0.972f, 0.02f);
        sbRt.anchorMax = new Vector2(0.994f, 0.98f);
        sbRt.offsetMin = Vector2.zero;
        sbRt.offsetMax = Vector2.zero;
        sbGo.GetComponent<Image>().color = new Color(0.10f, 0.15f, 0.24f, 0.90f);

        GameObject slide = new GameObject("Slide", typeof(RectTransform));
        slide.transform.SetParent(sbGo.transform, false);
        RectTransform slRt = slide.GetComponent<RectTransform>();
        slRt.anchorMin = Vector2.zero;
        slRt.anchorMax = Vector2.one;
        slRt.offsetMin = Vector2.zero;
        slRt.offsetMax = Vector2.zero;

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(slide.transform, false);
        RectTransform hdRt = handle.GetComponent<RectTransform>();
        hdRt.anchorMin = Vector2.zero;
        hdRt.anchorMax = Vector2.one;
        hdRt.offsetMin = Vector2.zero;
        hdRt.offsetMax = Vector2.zero;
        Image hdImg = handle.GetComponent<Image>();
        hdImg.color = UIThemeAssets.CardBorder;

        Scrollbar vBar = sbGo.GetComponent<Scrollbar>();
        vBar.handleRect = hdRt;
        vBar.targetGraphic = hdImg;
        vBar.direction = Scrollbar.Direction.BottomToTop;

        scrollRect = scrollArea.GetComponent<ScrollRect>();
        scrollRect.content = ctRt;
        scrollRect.viewport = vpRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 25f;
        scrollRect.verticalScrollbar = vBar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        listContent = content.transform;

        // ===== PINNED FOOTER: YOUR CURRENT RANK (3% -> 16%) =====
        GameObject footer = CreatePanel(body.transform, "SelfFooter", new Vector2(0.02f, 0.03f), new Vector2(0.98f, 0.16f), new Color(0.10f, 0.17f, 0.28f, 0.98f));

        GameObject fIconGo = new GameObject("FooterRankIcon", typeof(RectTransform), typeof(Image));
        fIconGo.transform.SetParent(footer.transform, false);
        RectTransform fiRt = fIconGo.GetComponent<RectTransform>();
        fiRt.anchorMin = new Vector2(0.02f, 0.10f);
        fiRt.anchorMax = new Vector2(0.09f, 0.90f);
        fiRt.offsetMin = Vector2.zero;
        fiRt.offsetMax = Vector2.zero;
        footerRankIcon = fIconGo.GetComponent<Image>();
        footerRankIcon.preserveAspect = true;
        footerRankIcon.raycastTarget = false;

        footerSelfText = CreateText("FooterSelf", footer.transform, new Vector2(0.11f, 0.34f), new Vector2(0.97f, 0.96f),
            "", 9f, 13f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

        syncStatusText = CreateText("SyncStatus", footer.transform, new Vector2(0.11f, 0.02f), new Vector2(0.97f, 0.38f),
            "", 7f, 10f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.62f, 0.84f, 0.98f, 0.95f));

        panel.SetActive(false);
    }

    async void OnClickRefreshLeaderboard()
    {
        if (isSyncing) return;
        isSyncing = true;

        if (refreshBtn != null) refreshBtn.interactable = false;
        if (refreshBtnLabel != null) refreshBtnLabel.text = "DANG TAI...";
        if (syncStatusText != null)
        {
            syncStatusText.text = "Dang cap nhat diem & dong bo vi tri BXH...";
            syncStatusText.color = UIThemeAssets.GoldAccent;
        }

        var res = await LeaderboardManager.RefreshFromCloudAsync();

        isSyncing = false;
        if (this == null) return;

        if (refreshBtn != null) refreshBtn.interactable = true;
        if (refreshBtnLabel != null) refreshBtnLabel.text = "CAP NHAT";

        RefreshList();

        MenuUI menu = FindFirstObjectByType<MenuUI>();
        if (menu != null) menu.RefreshPlayerStats();

        if (syncStatusText != null)
        {
            syncStatusText.text = res.statusMessage;
            syncStatusText.color = res.cloudSynced
                ? new Color(0.35f, 0.96f, 0.60f, 1f)
                : new Color(1.00f, 0.78f, 0.32f, 1f);
        }
    }

    void RefreshList()
    {
        if (!built || listContent == null) return;

        for (int i = listContent.childCount - 1; i >= 0; i--)
        {
            Destroy(listContent.GetChild(i).gameObject);
        }

        if (rankTabBg != null) rankTabBg.color = !showingSoloTab ? UIThemeAssets.PrimaryCyan : UIThemeAssets.SubCardBg;
        if (soloTabBg != null) soloTabBg.color = showingSoloTab ? UIThemeAssets.PrimaryCyan : UIThemeAssets.SubCardBg;

        if (!showingSoloTab)
        {
            if (colHeaderRightText != null) colHeaderRightText.text = "BAC RANK  /  ELO";
            List<RankLeaderboardEntry> entries = LeaderboardManager.GetRankLeaderboard(out RankLeaderboardEntry me);
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                string tierName = PlayerStatsManager.GetRankTitle(e.elo).ToUpper();
                Color tierCol = PlayerStatsManager.GetRankColor(e.elo);
                BuildRow(
                    e.position,
                    e.playerName,
                    e.isLocalPlayer,
                    e.isRealPlayer,
                    UIThemeAssets.GetRankIcon(e.elo),
                    $"{tierName}  •  {e.elo} ELO",
                    tierCol
                );
            }

            if (footerRankIcon != null)
            {
                footerRankIcon.enabled = true;
                footerRankIcon.sprite = UIThemeAssets.GetRankIcon(me.elo);
            }
            if (footerSelfText != null)
            {
                string nextInfo = PlayerStatsManager.FormatNextRankProgress(me.elo);
                footerSelfText.text = $"HANG CUA BAN: #{me.position}   •   {me.playerName}   •   {PlayerStatsManager.GetRankTitle(me.elo).ToUpper()} ({nextInfo})";
            }
        }
        else
        {
            if (colHeaderRightText != null) colHeaderRightText.text = "THANG - THUA  (TY LE)";
            List<SoloLeaderboardEntry> entries = LeaderboardManager.GetSoloLeaderboard(out SoloLeaderboardEntry me);
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                int total = e.wins + e.losses;
                int wr = total > 0 ? Mathf.RoundToInt(100f * e.wins / total) : 0;
                BuildRow(
                    e.position,
                    e.playerName,
                    e.isLocalPlayer,
                    e.isRealPlayer,
                    UIThemeAssets.IconLeaderboard,
                    $"{e.wins}W - {e.losses}L  ({wr}%)",
                    e.wins >= 15 ? UIThemeAssets.GoldAccent : new Color(0.72f, 0.90f, 1f, 1f)
                );
            }

            if (footerRankIcon != null)
            {
                footerRankIcon.enabled = true;
                footerRankIcon.sprite = UIThemeAssets.IconLeaderboard;
            }
            if (footerSelfText != null)
            {
                int total = me.wins + me.losses;
                int wr = total > 0 ? Mathf.RoundToInt(100f * me.wins / total) : 0;
                footerSelfText.text = $"HANG SOLO CUA BAN: #{me.position}   •   {me.playerName}   •   {me.wins} THANG - {me.losses} THUA ({wr}%)";
            }
        }

        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    void BuildRow(int pos, string playerName, bool isLocal, bool isReal, Sprite emblem, string rightText, Color rightColor)
    {
        GameObject row = new GameObject("Row_" + pos, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        row.transform.SetParent(listContent, false);
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 34f);
        LayoutElement le = row.GetComponent<LayoutElement>();
        le.preferredHeight = 34f;
        le.minHeight = 34f;

        Image bg = row.GetComponent<Image>();
        bg.color = isLocal
            ? new Color(0.14f, 0.30f, 0.48f, 0.98f)
            : (isReal
                ? new Color(0.10f, 0.20f, 0.32f, 0.96f)
                : (pos % 2 == 0 ? new Color(0.09f, 0.13f, 0.21f, 0.92f) : new Color(0.11f, 0.16f, 0.25f, 0.92f)));
        bg.raycastTarget = false;

        Color posCol = pos == 1
            ? UIThemeAssets.GoldAccent
            : (pos == 2 ? new Color(0.84f, 0.90f, 0.96f, 1f)
            : (pos == 3 ? new Color(0.88f, 0.58f, 0.32f, 1f) : new Color(0.70f, 0.78f, 0.88f, 1f)));

        CreateText("Pos", row.transform, new Vector2(0.01f, 0.05f), new Vector2(0.12f, 0.95f),
            $"#{pos}", 8.5f, 12f, FontStyles.Bold, TextAlignmentOptions.Center, posCol);

        if (emblem != null)
        {
            GameObject icGo = new GameObject("Emblem", typeof(RectTransform), typeof(Image));
            icGo.transform.SetParent(row.transform, false);
            RectTransform icRt = icGo.GetComponent<RectTransform>();
            icRt.anchorMin = new Vector2(0.13f, 0.10f);
            icRt.anchorMax = new Vector2(0.19f, 0.90f);
            icRt.offsetMin = Vector2.zero;
            icRt.offsetMax = Vector2.zero;
            Image icImg = icGo.GetComponent<Image>();
            icImg.sprite = emblem;
            icImg.preserveAspect = true;
            icImg.raycastTarget = false;
        }

        string display = isLocal
            ? $"{playerName}  (BAN)"
            : (isReal ? $"{playerName}  ★" : playerName);
        CreateText("Name", row.transform, new Vector2(0.21f, 0.05f), new Vector2(0.60f, 0.95f),
            display, 8.5f, 12f, (isLocal || isReal) ? FontStyles.Bold : FontStyles.Normal, TextAlignmentOptions.MidlineLeft,
            isLocal ? UIThemeAssets.GoldAccent : (isReal ? new Color(0.62f, 0.92f, 1f, 1f) : Color.white));

        CreateText("Score", row.transform, new Vector2(0.60f, 0.05f), new Vector2(0.97f, 0.95f),
            rightText, 8.5f, 11.5f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, rightColor);
    }

    GameObject CreatePanel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Color col)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = col;
        go.GetComponent<Image>().raycastTarget = false;
        return go;
    }

    TMP_Text CreateText(string name, Transform parent, Vector2 aMin, Vector2 aMax, string content, float fsMin, float fsMax, FontStyles style, TextAlignmentOptions align, Color col)
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

    Button CreateButton(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color bgColor, string label, float fsMin, float fsMax)
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
