using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardedAdUI : MonoBehaviour
{
    public static RewardedAdUI Instance;

    const int AdRewardGold = 100;
    const float AdWatchDuration = 5f;

    GameObject panel;
    TMP_Text statusText;
    Image progressFill;
    GameObject progressTrackGo;
    Button watchBtn;
    TMP_Text watchBtnLabel;
    Button closeBtn;

    TMP_FontAsset uiFont;
    bool built;
    bool isWatching;
    Coroutine watchRoutine;

    public bool IsOpen => panel != null && panel.activeSelf;

    public static RewardedAdUI EnsureInstance(Canvas canvas, TMP_FontAsset font)
    {
        if (Instance != null) return Instance;
        if (canvas == null) return null;

        GameObject root = new GameObject("RewardedAdRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        RewardedAdUI ui = root.AddComponent<RewardedAdUI>();
        ui.uiFont = font;
        ui.BuildUI();
        return ui;
    }

    void Awake()
    {
        Instance = this;
    }

    public void OpenAdModal()
    {
        if (!built) BuildUI();
        if (panel == null) return;

        isWatching = false;
        if (watchRoutine != null) { StopCoroutine(watchRoutine); watchRoutine = null; }

        panel.SetActive(true);
        transform.SetAsLastSibling();

        if (progressTrackGo != null) progressTrackGo.SetActive(false);
        if (progressFill != null) progressFill.fillAmount = 0f;
        if (statusText != null)
        {
            statusText.text = $"Xem quang cao ngan (5 giay) de nhan ngay +{AdRewardGold}!";
            statusText.color = new Color(0.84f, 0.92f, 1f, 1f);
        }
        if (watchBtn != null) watchBtn.interactable = true;
        if (watchBtnLabel != null) watchBtnLabel.text = $"XEM QUANG CAO (+{AdRewardGold})";
        if (closeBtn != null) closeBtn.interactable = true;
    }

    public void CloseAdModal()
    {
        if (isWatching) return;
        if (panel != null) panel.SetActive(false);
    }

    void BuildUI()
    {
        if (built) return;
        built = true;

        panel = new GameObject("RewardedAdOverlay", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        RectTransform pRt = panel.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.offsetMin = Vector2.zero;
        pRt.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 0.92f);
        panel.GetComponent<Image>().raycastTarget = true;

        GameObject border = CreatePanel(panel.transform, "CardBorder", new Vector2(0.22f, 0.16f), new Vector2(0.78f, 0.84f), UIThemeAssets.GoldAccent);
        GameObject body = new GameObject("CardBody", typeof(RectTransform), typeof(Image));
        body.transform.SetParent(border.transform, false);
        RectTransform bRt = body.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.offsetMin = new Vector2(2.5f, 2.5f);
        bRt.offsetMax = new Vector2(-2.5f, -2.5f);
        body.GetComponent<Image>().color = UIThemeAssets.CardBody;
        body.GetComponent<Image>().raycastTarget = false;

        GameObject header = CreatePanel(body.transform, "Header", new Vector2(0f, 0.80f), new Vector2(1f, 1f), new Color(0.10f, 0.14f, 0.24f, 0.98f));
        CreateText("Title", header.transform, new Vector2(0.04f, 0.08f), new Vector2(0.80f, 0.92f),
            "NHAN THEM VANG MIEN PHI", 10f, 15.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, UIThemeAssets.GoldAccent);

        closeBtn = CreateButton("CloseBtn", header.transform, new Vector2(0.84f, 0.16f), new Vector2(0.97f, 0.84f),
            UIThemeAssets.DangerRed, "X", 10f, 14f);
        closeBtn.onClick.AddListener(CloseAdModal);

        // Big Gold Coin + "+100"
        GameObject coinGo = new GameObject("Coin", typeof(RectTransform), typeof(Image));
        coinGo.transform.SetParent(body.transform, false);
        RectTransform cRt = coinGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.32f, 0.48f);
        cRt.anchorMax = new Vector2(0.50f, 0.76f);
        cRt.offsetMin = Vector2.zero;
        cRt.offsetMax = Vector2.zero;
        Image cImg = coinGo.GetComponent<Image>();
        cImg.sprite = UIThemeAssets.IconGold;
        cImg.preserveAspect = true;
        cImg.raycastTarget = false;

        CreateText("Amount", body.transform, new Vector2(0.50f, 0.48f), new Vector2(0.76f, 0.76f),
            $"+{AdRewardGold}", 18f, 28f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, UIThemeAssets.GoldAccent);

        statusText = CreateText("Status", body.transform, new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.46f),
            "", 8.5f, 12f, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);

        // Ad progress bar
        progressTrackGo = CreatePanel(body.transform, "ProgressTrack", new Vector2(0.10f, 0.25f), new Vector2(0.90f, 0.32f), new Color(0.10f, 0.15f, 0.25f, 1f));
        GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(progressTrackGo.transform, false);
        RectTransform fRt = fillGo.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.offsetMin = Vector2.zero;
        fRt.offsetMax = Vector2.zero;
        progressFill = fillGo.GetComponent<Image>();
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFill.color = UIThemeAssets.SuccessGreen;
        progressTrackGo.SetActive(false);

        watchBtn = CreateButton("WatchBtn", body.transform, new Vector2(0.20f, 0.06f), new Vector2(0.80f, 0.22f),
            UIThemeAssets.SuccessGreen, $"XEM QUANG CAO (+{AdRewardGold})", 10f, 14f);
        watchBtnLabel = watchBtn.GetComponentInChildren<TMP_Text>();
        watchBtn.onClick.AddListener(OnClickWatchAd);

        panel.SetActive(false);
    }

    void OnClickWatchAd()
    {
        if (isWatching) return;
        watchRoutine = StartCoroutine(WatchAdRoutine());
    }

    IEnumerator WatchAdRoutine()
    {
        isWatching = true;
        if (watchBtn != null) watchBtn.interactable = false;
        if (closeBtn != null) closeBtn.interactable = false;
        if (progressTrackGo != null) progressTrackGo.SetActive(true);

        float elapsed = 0f;
        while (elapsed < AdWatchDuration)
        {
            float t = Mathf.Clamp01(elapsed / AdWatchDuration);
            int rem = Mathf.CeilToInt(AdWatchDuration - elapsed);
            if (progressFill != null) progressFill.fillAmount = t;
            if (statusText != null)
            {
                statusText.text = $"Dang phat quang cao thuong... con {rem}s";
                statusText.color = UIThemeAssets.GoldAccent;
            }
            if (watchBtnLabel != null) watchBtnLabel.text = $"DANG XEM ({rem}s)...";
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (progressFill != null) progressFill.fillAmount = 1f;

        PlayerStatsManager.AddGold(AdRewardGold);
        MenuUI menu = FindFirstObjectByType<MenuUI>();
        if (menu != null) menu.RefreshPlayerStats();

        isWatching = false;
        if (closeBtn != null) closeBtn.interactable = true;
        if (watchBtn != null) watchBtn.interactable = true;
        if (watchBtnLabel != null) watchBtnLabel.text = $"XEM TIEP (+{AdRewardGold})";
        if (statusText != null)
        {
            statusText.text = $"NHAN THANH CONG +{AdRewardGold}! (Hien co: {PlayerStatsManager.PlayerGold})";
            statusText.color = new Color(0.35f, 0.95f, 0.56f, 1f);
        }

        watchRoutine = null;
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
