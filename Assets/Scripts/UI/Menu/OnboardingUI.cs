using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OnboardingUI : MonoBehaviour
{
    public static OnboardingUI Instance;

    GameObject overlayPanel;
    GameObject authBox;
    GameObject googlePromptBox;
    GameObject tutorialBox;

    // Auth tabs & inputs
    Button loginTabBtn;
    Image loginTabBg;
    Button registerTabBtn;
    Image registerTabBg;
    Button authCloseBtn;

    TMP_InputField usernameInput;
    TMP_InputField passwordInput;
    GameObject displayNameRowGo;
    TMP_InputField displayNameInput;
    TMP_Text authStatusText;
    Button submitAuthBtn;
    TMP_Text submitAuthLabel;
    TMP_Text dbModeHintText;

    // Google Email fallback prompt
    TMP_InputField googleEmailInput;
    TMP_InputField googleNickInput;
    TMP_Text googleStatusText;

    TMP_FontAsset uiFont;
    bool built;
    bool isRegisterMode;
    bool busy;

    public bool IsOpen => overlayPanel != null && overlayPanel.activeSelf;

    public static OnboardingUI EnsureInstance(Canvas canvas, TMP_FontAsset font)
    {
        if (Instance != null) return Instance;
        if (canvas == null) return null;

        GameObject root = new GameObject("OnboardingRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        OnboardingUI ui = root.AddComponent<OnboardingUI>();
        ui.uiFont = font;
        ui.BuildUI();
        return ui;
    }

    void Awake()
    {
        Instance = this;
    }

    public void CheckFirstLaunchFlow()
    {
        AccountDatabaseService.EnsureInitialized();
        if (!built) BuildUI();

        if (!AccountDatabaseService.IsLoggedIn)
        {
            OpenLogin(false);
        }
        else if (!PlayerStatsManager.TutorialCompleted)
        {
            OpenTutorial();
        }
    }

    public void OpenLogin(bool allowClose)
    {
        if (!built) BuildUI();
        if (overlayPanel == null) return;

        isRegisterMode = false;
        busy = false;
        overlayPanel.SetActive(true);
        transform.SetAsLastSibling();
        if (authBox != null) authBox.SetActive(true);
        if (googlePromptBox != null) googlePromptBox.SetActive(false);
        if (tutorialBox != null) tutorialBox.SetActive(false);
        // Luôn hiển thị nút X để người chơi có thể đóng quay về Menu bất cứ lúc nào (kể cả sau khi bấm Đăng Xuất!)
        if (authCloseBtn != null) authCloseBtn.gameObject.SetActive(true);
        if (authStatusText != null) authStatusText.text = "";

        RefreshAuthModeUI();
    }

    public void OpenTutorial()
    {
        if (!built) BuildUI();
        if (overlayPanel == null) return;

        overlayPanel.SetActive(true);
        transform.SetAsLastSibling();
        if (authBox != null) authBox.SetActive(false);
        if (googlePromptBox != null) googlePromptBox.SetActive(false);
        if (tutorialBox != null) tutorialBox.SetActive(true);
    }

    public void CloseAll()
    {
        if (overlayPanel != null) overlayPanel.SetActive(false);
        MenuUI menu = FindFirstObjectByType<MenuUI>();
        if (menu != null) menu.RefreshPlayerStats();
    }

    void BuildUI()
    {
        if (built) return;
        built = true;

        overlayPanel = new GameObject("OnboardingOverlay", typeof(RectTransform), typeof(Image));
        overlayPanel.transform.SetParent(transform, false);
        RectTransform pRt = overlayPanel.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.offsetMin = Vector2.zero;
        pRt.offsetMax = Vector2.zero;
        Image pImg = overlayPanel.GetComponent<Image>();
        pImg.color = new Color(0.02f, 0.04f, 0.09f, 0.94f);
        pImg.raycastTarget = true;

        BuildAuthBox(overlayPanel.transform);
        BuildGoogleEmailPrompt(overlayPanel.transform);
        BuildTutorialBox(overlayPanel.transform);

        overlayPanel.SetActive(false);
    }

    void BuildAuthBox(Transform parent)
    {
        authBox = CreateCardFrame(parent, "AuthBox", new Vector2(0.16f, 0.08f), new Vector2(0.84f, 0.92f), UIThemeAssets.CardBorder, out Transform body);

        // Header + Tabs (DANG NHAP | DANG KY)
        GameObject header = CreatePanel(body, "Header", new Vector2(0f, 0.84f), new Vector2(1f, 1f), new Color(0.10f, 0.14f, 0.24f, 0.98f));

        loginTabBtn = CreateButton("LoginTab", header.transform, new Vector2(0.03f, 0.14f), new Vector2(0.36f, 0.86f),
            UIThemeAssets.PrimaryCyan, "DANG NHAP", 9.5f, 13.5f);
        loginTabBg = loginTabBtn.GetComponent<Image>();
        loginTabBtn.onClick.AddListener(() => { isRegisterMode = false; RefreshAuthModeUI(); });

        registerTabBtn = CreateButton("RegisterTab", header.transform, new Vector2(0.38f, 0.14f), new Vector2(0.71f, 0.86f),
            UIThemeAssets.SubCardBg, "DANG KY NICK", 9.5f, 13.5f);
        registerTabBg = registerTabBtn.GetComponent<Image>();
        registerTabBtn.onClick.AddListener(() => { isRegisterMode = true; RefreshAuthModeUI(); });

        authCloseBtn = CreateButton("CloseAuth", header.transform, new Vector2(0.87f, 0.16f), new Vector2(0.97f, 0.84f),
            UIThemeAssets.DangerRed, "X", 10f, 14f);
        authCloseBtn.onClick.AddListener(CloseAll);

        // Inputs
        usernameInput = BuildLabeledInput(body, "UserRow", new Vector2(0.06f, 0.67f), new Vector2(0.94f, 0.80f),
            "Tai khoan (chu/so, >= 3 ky tu)...", TMP_InputField.ContentType.Standard, 20);

        passwordInput = BuildLabeledInput(body, "PassRow", new Vector2(0.06f, 0.51f), new Vector2(0.94f, 0.64f),
            "Mat khau (>= 4 ky tu)...", TMP_InputField.ContentType.Password, 24);

        displayNameRowGo = new GameObject("NickRowHolder", typeof(RectTransform));
        displayNameRowGo.transform.SetParent(body, false);
        RectTransform nrRt = displayNameRowGo.GetComponent<RectTransform>();
        nrRt.anchorMin = new Vector2(0.06f, 0.35f);
        nrRt.anchorMax = new Vector2(0.94f, 0.48f);
        nrRt.offsetMin = Vector2.zero;
        nrRt.offsetMax = Vector2.zero;
        displayNameInput = BuildLabeledInput(displayNameRowGo.transform, "NickInput", Vector2.zero, Vector2.one,
            "Ten nhan vat hien thi (3 - 14 ky tu)...", TMP_InputField.ContentType.Standard, 14);

        // Status / Error
        authStatusText = CreateText("AuthStatus", body, new Vector2(0.06f, 0.27f), new Vector2(0.94f, 0.34f),
            "", 8f, 11f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.44f, 0.42f, 1f));

        // Submit Button (Left) + Google Sign-In Button (Right)
        submitAuthBtn = CreateButton("SubmitAuthBtn", body, new Vector2(0.06f, 0.11f), new Vector2(0.49f, 0.25f),
            UIThemeAssets.SuccessGreen, "DANG NHAP", 10f, 14f);
        submitAuthLabel = submitAuthBtn.GetComponentInChildren<TMP_Text>();
        submitAuthBtn.onClick.AddListener(OnClickSubmitAuth);

        GameObject gBtnGo = new GameObject("GoogleSignInBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        gBtnGo.transform.SetParent(body, false);
        RectTransform gbRt = gBtnGo.GetComponent<RectTransform>();
        gbRt.anchorMin = new Vector2(0.51f, 0.11f);
        gbRt.anchorMax = new Vector2(0.94f, 0.25f);
        gbRt.offsetMin = Vector2.zero;
        gbRt.offsetMax = Vector2.zero;
        Image gbImg = gBtnGo.GetComponent<Image>();
        gbImg.color = new Color(0.94f, 0.96f, 1.00f, 1f);
        Button gBtn = gBtnGo.GetComponent<Button>();
        gBtn.targetGraphic = gbImg;
        gBtn.onClick.AddListener(OnClickGoogleSignIn);

        GameObject gIc = new GameObject("GIcon", typeof(RectTransform), typeof(Image));
        gIc.transform.SetParent(gBtnGo.transform, false);
        RectTransform giRt = gIc.GetComponent<RectTransform>();
        giRt.anchorMin = new Vector2(0.04f, 0.14f);
        giRt.anchorMax = new Vector2(0.22f, 0.86f);
        giRt.offsetMin = Vector2.zero;
        giRt.offsetMax = Vector2.zero;
        Image giImg = gIc.GetComponent<Image>();
        giImg.sprite = UIThemeAssets.IconGoogle;
        giImg.preserveAspect = true;
        giImg.raycastTarget = false;

        CreateText("GLabel", gBtnGo.transform, new Vector2(0.24f, 0.05f), new Vector2(0.96f, 0.95f),
            "DANG NHAP GOOGLE", 8.5f, 12.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.12f, 0.15f, 0.22f, 1f));

        // DB Mode Footnote
        string dbHint = AccountDatabaseService.HasCloudDatabase
            ? "Cloud Database: DANG KET NOI (Dong bo tai khoan lien thiet bi)"
            : "Database tai khoan da bat (Dien Firebase URL trong CloudAuthConfig.json de dong bo lien thiet bi)";
        dbModeHintText = CreateText("DbHint", body, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.09f),
            dbHint, 6.5f, 9f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.58f, 0.72f, 0.88f, 0.90f));
    }

    void BuildGoogleEmailPrompt(Transform parent)
    {
        googlePromptBox = CreateCardFrame(parent, "GooglePromptBox", new Vector2(0.18f, 0.16f), new Vector2(0.82f, 0.84f), new Color(0.26f, 0.52f, 0.96f, 1f), out Transform body);

        GameObject header = CreatePanel(body, "Header", new Vector2(0f, 0.80f), new Vector2(1f, 1f), new Color(0.10f, 0.14f, 0.24f, 0.98f));
        CreateText("Title", header.transform, new Vector2(0.04f, 0.08f), new Vector2(0.66f, 0.92f),
            "DANG NHAP BANG TAI KHOAN GOOGLE", 9.5f, 15f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

        Button backBtn = CreateButton("BackBtn", header.transform, new Vector2(0.68f, 0.16f), new Vector2(0.86f, 0.84f),
            UIThemeAssets.SubCardBg, "QUAY LAI", 8f, 11f);
        backBtn.onClick.AddListener(() =>
        {
            googlePromptBox.SetActive(false);
            authBox.SetActive(true);
        });

        Button closeGoogleBtn = CreateButton("CloseGoogleBtn", header.transform, new Vector2(0.88f, 0.16f), new Vector2(0.97f, 0.84f),
            UIThemeAssets.DangerRed, "X", 10f, 14f);
        closeGoogleBtn.onClick.AddListener(CloseAll);

        googleEmailInput = BuildLabeledInput(body, "GmailInput", new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.72f),
            "Nhap Gmail cua ban (VD: tenban@gmail.com)...", TMP_InputField.ContentType.EmailAddress, 48);

        googleNickInput = BuildLabeledInput(body, "GNickInput", new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.50f),
            "Ten nhan vat (neu dang ky lan dau)...", TMP_InputField.ContentType.Standard, 14);

        googleStatusText = CreateText("GStatus", body, new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.32f),
            "", 8f, 11f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.44f, 0.42f, 1f));

        Button confirmG = CreateButton("ConfirmGoogleBtn", body, new Vector2(0.22f, 0.06f), new Vector2(0.78f, 0.21f),
            new Color(0.24f, 0.52f, 0.96f, 1f), "XAC NHAN GOOGLE", 10f, 14f);
        confirmG.onClick.AddListener(OnConfirmGoogleEmail);
    }

    void BuildTutorialBox(Transform parent)
    {
        tutorialBox = CreateCardFrame(parent, "TutorialBox", new Vector2(0.05f, 0.07f), new Vector2(0.95f, 0.93f), UIThemeAssets.CardBorder, out Transform body);

        GameObject header = CreatePanel(body, "Header", new Vector2(0f, 0.82f), new Vector2(1f, 1f), new Color(0.10f, 0.14f, 0.24f, 0.98f));
        CreateText("Title", header.transform, new Vector2(0.03f, 0.08f), new Vector2(0.80f, 0.92f),
            "HUONG DAN TAN THU  •  CACH CHOI FLASH BALL", 10f, 16f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

        Button skipBtn = CreateButton("SkipBtn", header.transform, new Vector2(0.83f, 0.16f), new Vector2(0.975f, 0.84f),
            new Color(0.24f, 0.30f, 0.42f, 1f), "BO QUA", 8.5f, 12f);
        skipBtn.onClick.AddListener(() =>
        {
            PlayerStatsManager.TutorialCompleted = true;
            CloseAll();
        });

        BuildGuideStepCard(body, new Vector2(0.025f, 0.24f), new Vector2(0.335f, 0.78f),
            new Color(0.18f, 0.64f, 0.98f, 1f), UIThemeAssets.IconParry,
            "1. PARRY & TU LUC",
            "Khi duong chi DO noi vao ban, bam PARRY luc bong toi gan de danh tra.\nGiu nut PARRY de tu luc cu chem manh!");

        BuildGuideStepCard(body, new Vector2(0.345f, 0.24f), new Vector2(0.655f, 0.78f),
            new Color(0.16f, 0.82f, 0.72f, 1f), UIThemeAssets.IconDash,
            "2. DI CHUYEN & NE",
            "Dung Joystick di chuyen linh hoat.\nBam DASH de luot nhanh thoat hiem va JUMP de nhay tranh bong (khong mat Parry)!");

        BuildGuideStepCard(body, new Vector2(0.665f, 0.24f), new Vector2(0.975f, 0.78f),
            new Color(0.76f, 0.32f, 1.00f, 1f), UIThemeAssets.IconTornado,
            "3. QUA TAN THU & KY NANG",
            "Hoan thanh van dau tien de nhan THU QUA TAN THU (500 + Chieu DONG BANG) trong Hom Thu ngoai Menu!");

        Button playBotBtn = CreateButton("PlayBotTutorialBtn", body, new Vector2(0.22f, 0.05f), new Vector2(0.78f, 0.20f),
            UIThemeAssets.SuccessGreen, "CHOI THU 1 VAN VOI BOT NGAY!", 10f, 15f);
        playBotBtn.onClick.AddListener(OnClickPlayBotTutorial);
    }

    void RefreshAuthModeUI()
    {
        if (loginTabBg != null) loginTabBg.color = !isRegisterMode ? UIThemeAssets.PrimaryCyan : UIThemeAssets.SubCardBg;
        if (registerTabBg != null) registerTabBg.color = isRegisterMode ? UIThemeAssets.PrimaryCyan : UIThemeAssets.SubCardBg;
        if (displayNameRowGo != null) displayNameRowGo.SetActive(isRegisterMode);
        if (submitAuthLabel != null) submitAuthLabel.text = isRegisterMode ? "DANG KY & VAO GAME" : "DANG NHAP";
        if (authStatusText != null) authStatusText.text = "";
    }

    async void OnClickSubmitAuth()
    {
        if (busy) return;
        busy = true;
        if (authStatusText != null)
        {
            authStatusText.text = "Dang xu ly...";
            authStatusText.color = UIThemeAssets.GoldAccent;
        }

        string user = usernameInput != null ? usernameInput.text : "";
        string pass = passwordInput != null ? passwordInput.text : "";
        string nick = displayNameInput != null ? displayNameInput.text : "";

        (bool ok, string msg) res = isRegisterMode
            ? await AccountDatabaseService.RegisterAsync(user, pass, nick)
            : await AccountDatabaseService.LoginAsync(user, pass);

        busy = false;
        if (!res.ok)
        {
            if (authStatusText != null)
            {
                authStatusText.text = res.msg;
                authStatusText.color = new Color(1f, 0.42f, 0.40f, 1f);
            }
            return;
        }

        OnAuthenticatedSuccess();
    }

    async void OnClickGoogleSignIn()
    {
        if (busy) return;

        if (AccountDatabaseService.HasRealGoogleOAuthConfigured())
        {
            busy = true;
            if (authStatusText != null)
            {
                authStatusText.text = "Dang mo trinh duyet dang nhap Google...";
                authStatusText.color = UIThemeAssets.GoldAccent;
            }
            var oauth = await AccountDatabaseService.SignInWithGoogleBrowserOAuthAsync();
            busy = false;
            if (oauth.ok)
            {
                OnAuthenticatedSuccess();
                return;
            }
            if (oauth.message != "USE_EMAIL_PROMPT")
            {
                if (authStatusText != null)
                {
                    authStatusText.text = oauth.message;
                    authStatusText.color = new Color(1f, 0.42f, 0.40f, 1f);
                }
                return;
            }
        }

        authBox.SetActive(false);
        googlePromptBox.SetActive(true);
        if (googleStatusText != null) googleStatusText.text = "";
    }

    async void OnConfirmGoogleEmail()
    {
        if (busy) return;
        busy = true;
        if (googleStatusText != null)
        {
            googleStatusText.text = "Dang dong bo tai khoan Google...";
            googleStatusText.color = UIThemeAssets.GoldAccent;
        }

        string email = googleEmailInput != null ? googleEmailInput.text : "";
        string nick = googleNickInput != null ? googleNickInput.text : "";
        var res = await AccountDatabaseService.SignInWithGoogleEmailAsync(email, nick);
        busy = false;

        if (!res.ok)
        {
            if (googleStatusText != null)
            {
                googleStatusText.text = res.message;
                googleStatusText.color = new Color(1f, 0.42f, 0.40f, 1f);
            }
            return;
        }

        OnAuthenticatedSuccess();
    }

    void OnAuthenticatedSuccess()
    {
        MenuUI menu = FindFirstObjectByType<MenuUI>();
        if (menu != null) menu.RefreshPlayerStats();

        if (!PlayerStatsManager.TutorialCompleted)
        {
            OpenTutorial();
        }
        else
        {
            CloseAll();
        }
    }

    void OnClickPlayBotTutorial()
    {
        PlayerStatsManager.TutorialCompleted = true;
        CloseAll();

        MenuUI menu = FindFirstObjectByType<MenuUI>();
        if (menu != null) menu.PlayTraining();
        else if (MatchmakingManager.Instance != null) MatchmakingManager.Instance.StartTraining();
    }

    void BuildGuideStepCard(Transform parent, Vector2 aMin, Vector2 aMax, Color accent, Sprite icon, string title, string desc)
    {
        CreateCardFrame(parent, "Step_" + title, aMin, aMax, accent, out Transform inner);

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(inner, false);
        RectTransform icRt = iconGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0.28f, 0.56f);
        icRt.anchorMax = new Vector2(0.72f, 0.92f);
        icRt.offsetMin = Vector2.zero;
        icRt.offsetMax = Vector2.zero;
        Image icImg = iconGo.GetComponent<Image>();
        icImg.sprite = icon;
        icImg.preserveAspect = true;
        icImg.raycastTarget = false;

        CreateText("Title", inner, new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.55f),
            title, 8.5f, 12.5f, FontStyles.Bold, TextAlignmentOptions.Center, accent);

        TMP_Text dTxt = CreateText("Desc", inner, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.41f),
            desc, 7.5f, 10.5f, FontStyles.Normal, TextAlignmentOptions.Top, new Color(0.84f, 0.90f, 0.98f, 1f));
        dTxt.textWrappingMode = TextWrappingModes.Normal;
    }

    TMP_InputField BuildLabeledInput(Transform parent, string name, Vector2 aMin, Vector2 aMax, string placeholderTxt, TMP_InputField.ContentType contentType, int charLimit)
    {
        GameObject frame = CreatePanel(parent, name, aMin, aMax, UIThemeAssets.CardBorder);
        GameObject bg = new GameObject("Bg", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        bg.transform.SetParent(frame.transform, false);
        RectTransform rt = bg.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(1.8f, 1.8f);
        rt.offsetMax = new Vector2(-1.8f, -1.8f);
        bg.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.14f, 1f);

        GameObject ta = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        ta.transform.SetParent(bg.transform, false);
        RectTransform taRt = ta.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(10f, 3f);
        taRt.offsetMax = new Vector2(-10f, -3f);

        TMP_Text ph = CreateText("Placeholder", ta.transform, Vector2.zero, Vector2.one,
            placeholderTxt, 8.5f, 12.5f, FontStyles.Italic, TextAlignmentOptions.MidlineLeft, new Color(0.48f, 0.58f, 0.72f, 0.85f));
        TMP_Text txt = CreateText("Text", ta.transform, Vector2.zero, Vector2.one,
            "", 9f, 13f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

        TMP_InputField field = bg.GetComponent<TMP_InputField>();
        field.textViewport = taRt;
        field.textComponent = txt;
        field.placeholder = ph;
        field.contentType = contentType;
        field.characterLimit = charLimit;
        return field;
    }

    GameObject CreateCardFrame(Transform parent, string name, Vector2 aMin, Vector2 aMax, Color borderCol, out Transform body)
    {
        GameObject border = new GameObject(name, typeof(RectTransform), typeof(Image));
        border.transform.SetParent(parent, false);
        RectTransform rt = border.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        border.GetComponent<Image>().color = borderCol;
        border.GetComponent<Image>().raycastTarget = false;

        GameObject inner = new GameObject("Body", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(border.transform, false);
        RectTransform inRt = inner.GetComponent<RectTransform>();
        inRt.anchorMin = Vector2.zero;
        inRt.anchorMax = Vector2.one;
        inRt.offsetMin = new Vector2(2.5f, 2.5f);
        inRt.offsetMax = new Vector2(-2.5f, -2.5f);
        inner.GetComponent<Image>().color = UIThemeAssets.CardBody;
        inner.GetComponent<Image>().raycastTarget = false;

        body = inner.transform;
        return border;
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
