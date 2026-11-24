using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MailboxUI : MonoBehaviour
{
    public static MailboxUI Instance;

    GameObject panel;
    GameObject mailItemRow;
    Image mailItemBg;
    GameObject mailUnreadDot;
    TMP_Text mailItemStatusText;
    GameObject emptyHintGo;

    GameObject mailDetailBox;
    TMP_Text detailFeedbackText;
    Button claimBtn;
    Image claimBtnBg;
    TMP_Text claimBtnLabel;

    TMP_FontAsset uiFont;
    bool built;
    bool newbieMailOpened;

    public bool IsOpen => panel != null && panel.activeSelf;

    public static MailboxUI EnsureInstance(Canvas canvas, TMP_FontAsset font)
    {
        if (Instance != null) return Instance;
        if (canvas == null) return null;

        GameObject root = new GameObject("MailboxRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        MailboxUI ui = root.AddComponent<MailboxUI>();
        ui.uiFont = font;
        ui.BuildUI();
        return ui;
    }

    void Awake()
    {
        Instance = this;
    }

    public void OpenMailbox()
    {
        if (!built) BuildUI();
        newbieMailOpened = false;
        if (panel != null)
        {
            panel.SetActive(true);
            transform.SetAsLastSibling();
        }
        if (detailFeedbackText != null) detailFeedbackText.text = "";
        RefreshUI();
    }

    public void CloseMailbox()
    {
        if (panel != null) panel.SetActive(false);
        MenuUI menu = FindFirstObjectByType<MenuUI>();
        if (menu != null) menu.RefreshPlayerStats();
    }

    void BuildUI()
    {
        if (built) return;
        built = true;

        panel = new GameObject("MailboxOverlay", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        RectTransform pRt = panel.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.offsetMin = Vector2.zero;
        pRt.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 0.92f);
        panel.GetComponent<Image>().raycastTarget = true;

        GameObject border = CreatePanel(panel.transform, "CardBorder", new Vector2(0.08f, 0.09f), new Vector2(0.92f, 0.91f), UIThemeAssets.CardBorder);
        GameObject body = new GameObject("CardBody", typeof(RectTransform), typeof(Image));
        body.transform.SetParent(border.transform, false);
        RectTransform bRt = body.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.offsetMin = new Vector2(2.5f, 2.5f);
        bRt.offsetMax = new Vector2(-2.5f, -2.5f);
        body.GetComponent<Image>().color = UIThemeAssets.CardBody;
        body.GetComponent<Image>().raycastTarget = false;

        // Header
        GameObject header = CreatePanel(body.transform, "Header", new Vector2(0f, 0.82f), new Vector2(1f, 1f), new Color(0.10f, 0.14f, 0.24f, 0.98f));
        GameObject mIcon = new GameObject("MailIcon", typeof(RectTransform), typeof(Image));
        mIcon.transform.SetParent(header.transform, false);
        RectTransform miRt = mIcon.GetComponent<RectTransform>();
        miRt.anchorMin = new Vector2(0.02f, 0.14f);
        miRt.anchorMax = new Vector2(0.09f, 0.86f);
        miRt.offsetMin = Vector2.zero;
        miRt.offsetMax = Vector2.zero;
        Image miImg = mIcon.GetComponent<Image>();
        miImg.sprite = UIThemeAssets.IconMail;
        miImg.preserveAspect = true;
        miImg.raycastTarget = false;

        CreateText("Title", header.transform, new Vector2(0.10f, 0.08f), new Vector2(0.75f, 0.92f),
            "HOM THU QUA TANG", 11f, 16.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

        Button closeBtn = CreateButton("CloseBtn", header.transform, new Vector2(0.84f, 0.16f), new Vector2(0.975f, 0.84f),
            UIThemeAssets.DangerRed, "DONG", 9.5f, 13.5f);
        closeBtn.onClick.AddListener(CloseMailbox);

        // Left Column: Mail List (2% -> 38%)
        GameObject leftCol = CreatePanel(body.transform, "MailListCol", new Vector2(0.02f, 0.04f), new Vector2(0.38f, 0.79f), new Color(0.06f, 0.09f, 0.16f, 0.95f));

        emptyHintGo = new GameObject("EmptyHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        emptyHintGo.transform.SetParent(leftCol.transform, false);
        RectTransform ehRt = emptyHintGo.GetComponent<RectTransform>();
        ehRt.anchorMin = new Vector2(0.08f, 0.20f);
        ehRt.anchorMax = new Vector2(0.92f, 0.80f);
        ehRt.offsetMin = Vector2.zero;
        ehRt.offsetMax = Vector2.zero;
        TextMeshProUGUI ehTxt = emptyHintGo.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) ehTxt.font = uiFont;
        ehTxt.text = "Chua co thu moi!\n\nHoan thanh 1 tran dau dau tien de nhan THU QUA TAN THU!";
        ehTxt.enableAutoSizing = true;
        ehTxt.fontSizeMin = 8f;
        ehTxt.fontSizeMax = 11.5f;
        ehTxt.alignment = TextAlignmentOptions.Center;
        ehTxt.color = new Color(0.68f, 0.78f, 0.90f, 0.95f);

        // Mail Row: QUA TAN THU
        mailItemRow = new GameObject("NewbieMailRow", typeof(RectTransform), typeof(Image), typeof(Button));
        mailItemRow.transform.SetParent(leftCol.transform, false);
        RectTransform mrRt = mailItemRow.GetComponent<RectTransform>();
        mrRt.anchorMin = new Vector2(0.04f, 0.72f);
        mrRt.anchorMax = new Vector2(0.96f, 0.96f);
        mrRt.offsetMin = Vector2.zero;
        mrRt.offsetMax = Vector2.zero;
        mailItemBg = mailItemRow.GetComponent<Image>();
        mailItemBg.color = new Color(0.12f, 0.22f, 0.38f, 0.98f);
        Button mailBtn = mailItemRow.GetComponent<Button>();
        mailBtn.targetGraphic = mailItemBg;
        mailBtn.onClick.AddListener(() => { newbieMailOpened = true; RefreshUI(); });

        GameObject giftIc = new GameObject("GiftIc", typeof(RectTransform), typeof(Image));
        giftIc.transform.SetParent(mailItemRow.transform, false);
        RectTransform giRt = giftIc.GetComponent<RectTransform>();
        giRt.anchorMin = new Vector2(0.04f, 0.14f);
        giRt.anchorMax = new Vector2(0.26f, 0.86f);
        giRt.offsetMin = Vector2.zero;
        giRt.offsetMax = Vector2.zero;
        Image giImg = giftIc.GetComponent<Image>();
        giImg.sprite = UIThemeAssets.IconGiftBox;
        giImg.preserveAspect = true;
        giImg.raycastTarget = false;

        CreateText("MailTitle", mailItemRow.transform, new Vector2(0.28f, 0.46f), new Vector2(0.88f, 0.92f),
            "QUA TAN THU", 9f, 12.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, UIThemeAssets.GoldAccent);

        mailItemStatusText = CreateText("MailSub", mailItemRow.transform, new Vector2(0.28f, 0.08f), new Vector2(0.88f, 0.48f),
            "Bam de xem & nhan qua!", 7.5f, 10f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.80f, 0.90f, 1f, 1f));

        mailUnreadDot = new GameObject("UnreadDot", typeof(RectTransform), typeof(Image));
        mailUnreadDot.transform.SetParent(mailItemRow.transform, false);
        RectTransform udRt = mailUnreadDot.GetComponent<RectTransform>();
        udRt.anchorMin = new Vector2(0.89f, 0.62f);
        udRt.anchorMax = new Vector2(0.96f, 0.86f);
        udRt.offsetMin = Vector2.zero;
        udRt.offsetMax = Vector2.zero;
        Image udImg = mailUnreadDot.GetComponent<Image>();
        if (UIThemeAssets.BtnCircleFrame != null) udImg.sprite = UIThemeAssets.BtnCircleFrame;
        udImg.color = new Color(1f, 0.22f, 0.24f, 1f);
        udImg.raycastTarget = false;

        // Right Column: Mail Content & Claim (40% -> 98%)
        GameObject rightCol = CreatePanel(body.transform, "MailDetailCol", new Vector2(0.40f, 0.04f), new Vector2(0.98f, 0.79f), new Color(0.07f, 0.11f, 0.19f, 0.96f));

        mailDetailBox = new GameObject("MailDetailBox", typeof(RectTransform));
        mailDetailBox.transform.SetParent(rightCol.transform, false);
        RectTransform mdRt = mailDetailBox.GetComponent<RectTransform>();
        mdRt.anchorMin = Vector2.zero;
        mdRt.anchorMax = Vector2.one;
        mdRt.offsetMin = Vector2.zero;
        mdRt.offsetMax = Vector2.zero;

        CreateText("DetailHeader", mailDetailBox.transform, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.96f),
            "THU CHAO MUNG  •  QUA TAN THU", 10f, 15f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, UIThemeAssets.GoldAccent);

        TMP_Text letterTxt = CreateText("LetterBody", mailDetailBox.transform, new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.81f),
            "Chuc mung ban da hoan thanh tran dau dau tien tai Flash Ball!\nGui tang ban phan qua tan thu de mua vu khi & chinh chien:",
            8f, 11.5f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.92f, 1f, 1f));
        letterTxt.textWrappingMode = TextWrappingModes.Normal;

        // 2 Reward Cards: +500 Gold & Freeze Skill!
        BuildRewardSlot(mailDetailBox.transform, new Vector2(0.08f, 0.24f), new Vector2(0.48f, 0.56f),
            UIThemeAssets.GoldAccent, UIThemeAssets.IconGold, "+500");

        BuildRewardSlot(mailDetailBox.transform, new Vector2(0.52f, 0.24f), new Vector2(0.92f, 0.56f),
            new Color(0.18f, 0.88f, 0.96f, 1f), UIThemeAssets.IconFreeze, "FREEZE (DONG BANG)");

        detailFeedbackText = CreateText("Feedback", mailDetailBox.transform, new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.24f),
            "", 8f, 11f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.35f, 0.95f, 0.56f, 1f));

        claimBtn = CreateButton("ClaimBtn", mailDetailBox.transform, new Vector2(0.24f, 0.03f), new Vector2(0.76f, 0.16f),
            UIThemeAssets.SuccessGreen, "CLAIM QUA TAN THU", 10f, 14f);
        claimBtnBg = claimBtn.GetComponent<Image>();
        claimBtnLabel = claimBtn.GetComponentInChildren<TMP_Text>();
        claimBtn.onClick.AddListener(OnClickClaimNewbieGift);

        panel.SetActive(false);
    }

    void BuildRewardSlot(Transform parent, Vector2 aMin, Vector2 aMax, Color borderCol, Sprite icon, string label)
    {
        GameObject border = CreatePanel(parent, "Reward_" + label, aMin, aMax, borderCol);
        GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(border.transform, false);
        RectTransform rt = inner.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(2f, 2f);
        rt.offsetMax = new Vector2(-2f, -2f);
        inner.GetComponent<Image>().color = UIThemeAssets.SubCardBg;
        inner.GetComponent<Image>().raycastTarget = false;

        GameObject icGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icGo.transform.SetParent(inner.transform, false);
        RectTransform icRt = icGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0.22f, 0.30f);
        icRt.anchorMax = new Vector2(0.78f, 0.92f);
        icRt.offsetMin = Vector2.zero;
        icRt.offsetMax = Vector2.zero;
        Image icImg = icGo.GetComponent<Image>();
        icImg.sprite = icon;
        icImg.preserveAspect = true;
        icImg.raycastTarget = false;

        CreateText("Lbl", inner.transform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.28f),
            label, 8f, 12f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
    }

    void OnClickClaimNewbieGift()
    {
        if (PlayerStatsManager.ClaimNewbieGiftMail())
        {
            if (detailFeedbackText != null)
            {
                detailFeedbackText.text = "Da nhan thanh cong +500 & mo khoa chieu FREEZE!";
            }
            RefreshUI();
            MenuUI menu = FindFirstObjectByType<MenuUI>();
            if (menu != null) menu.RefreshPlayerStats();
        }
    }

    void RefreshUI()
    {
        if (!built) return;

        bool unlocked = PlayerStatsManager.NewbieMailUnlocked;
        bool claimed = PlayerStatsManager.NewbieMailClaimed;

        if (emptyHintGo != null) emptyHintGo.SetActive(!unlocked);
        if (mailItemRow != null) mailItemRow.SetActive(unlocked);
        if (mailUnreadDot != null) mailUnreadDot.SetActive(unlocked && !claimed);

        if (mailItemStatusText != null)
        {
            mailItemStatusText.text = claimed ? "Da nhan qua" : "Bam de doc & Claim!";
            mailItemStatusText.color = claimed ? new Color(0.55f, 0.65f, 0.78f, 1f) : new Color(0.35f, 0.95f, 0.56f, 1f);
        }

        if (mailItemBg != null)
        {
            mailItemBg.color = newbieMailOpened
                ? new Color(0.16f, 0.30f, 0.50f, 1f)
                : new Color(0.11f, 0.16f, 0.26f, 0.98f);
        }

        if (mailDetailBox != null)
        {
            mailDetailBox.SetActive(unlocked && newbieMailOpened);
        }

        if (claimBtn != null && claimBtnBg != null && claimBtnLabel != null)
        {
            claimBtn.interactable = unlocked && !claimed;
            claimBtnLabel.text = claimed ? "DA NHAN QUA" : "CLAIM";
            claimBtnBg.color = claimed ? new Color(0.24f, 0.28f, 0.36f, 1f) : UIThemeAssets.SuccessGreen;
        }
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
