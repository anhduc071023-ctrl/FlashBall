using System.Collections.Generic;
using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;

public class CustomLobbyUI : MonoBehaviour
{
    public static CustomLobbyUI Instance;

    [Header("Root")]
    public GameObject lobbyPanel;

    [Header("Header Texts")]
    public TMP_Text titleText;
    public TMP_Text roomInfoText;
    public TMP_Text statusText;

    [Header("Room Search")]
    public TMP_InputField roomCodeInput;
    public UnityEngine.UI.Button searchRoomButton;
    public UnityEngine.UI.Button createRoomButton;

    [Header("8 Player Slots")]
    public UnityEngine.UI.GridLayoutGroup slotsGrid;
    public UnityEngine.UI.Image[] slotBackgrounds;
    public TMP_Text[] slotNameTexts;
    public TMP_Text[] slotRoleTexts;

    [Header("Controls")]
    public UnityEngine.UI.Button startMatchButton;
    public TMP_Text startButtonText;
    public UnityEngine.UI.Button addBotButton;
    public UnityEngine.UI.Button removeBotButton;
    public UnityEngine.UI.Button leaveButton;

    static readonly Color OccupiedHostColor = new Color(0.16f, 0.34f, 0.56f, 0.96f);
    static readonly Color OccupiedPlayerColor = new Color(0.14f, 0.28f, 0.42f, 0.96f);
    static readonly Color OccupiedBotColor = new Color(0.24f, 0.20f, 0.42f, 0.94f);
    static readonly Color EmptySlotColor = new Color(0.12f, 0.14f, 0.19f, 0.85f);

    float refreshTimer;

    void Awake()
    {
        Instance = this;

        EnsureSearchBarUI();
        BindButtons();

        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
        }
    }

    void BindButtons()
    {
        if (startMatchButton != null)
        {
            startMatchButton.onClick.RemoveListener(OnClickStartMatch);
            startMatchButton.onClick.AddListener(OnClickStartMatch);
        }

        if (addBotButton != null)
        {
            addBotButton.onClick.RemoveListener(OnClickAddBot);
            addBotButton.onClick.AddListener(OnClickAddBot);
        }

        if (removeBotButton != null)
        {
            removeBotButton.onClick.RemoveListener(OnClickRemoveBot);
            removeBotButton.onClick.AddListener(OnClickRemoveBot);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(OnClickLeave);
            leaveButton.onClick.AddListener(OnClickLeave);
        }

        if (searchRoomButton != null)
        {
            searchRoomButton.onClick.RemoveListener(OnClickSearchRoom);
            searchRoomButton.onClick.AddListener(OnClickSearchRoom);
        }

        if (createRoomButton != null)
        {
            createRoomButton.onClick.RemoveListener(OnClickCreateNewRoom);
            createRoomButton.onClick.AddListener(OnClickCreateNewRoom);
        }

        if (roomCodeInput != null)
        {
            roomCodeInput.onSubmit.RemoveListener(OnSubmitRoomCode);
            roomCodeInput.onSubmit.AddListener(OnSubmitRoomCode);
        }
    }

    public void EnsureSearchBarUI()
    {
        if (roomCodeInput != null && searchRoomButton != null && createRoomButton != null)
            return;

        if (lobbyPanel == null)
            return;

        Transform headerBar = lobbyPanel.transform.Find("CardBorder/CardBody/HeaderBar");
        if (headerBar == null)
            return;

        // Thu gọn vùng Title/RoomInfo bên trái HeaderBar để nhường chỗ cho Thanh tìm kiếm ở giữa
        if (titleText != null)
        {
            RectTransform trt = titleText.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.02f, 0.48f);
            trt.anchorMax = new Vector2(0.38f, 0.95f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }

        if (roomInfoText != null)
        {
            RectTransform irt = roomInfoText.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.02f, 0.05f);
            irt.anchorMax = new Vector2(0.38f, 0.48f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
            roomInfoText.color = new Color(1.00f, 0.86f, 0.30f, 1f);
        }

        if (leaveButton != null)
        {
            RectTransform lrt = leaveButton.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.84f, 0.16f);
            lrt.anchorMax = new Vector2(0.98f, 0.84f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
        }

        Transform existingBar = headerBar.Find("RoomSearchBar");
        if (existingBar != null)
        {
            roomCodeInput = existingBar.GetComponentInChildren<TMP_InputField>(true);
            Transform sBtn = existingBar.Find("SearchRoomButton");
            if (sBtn != null) searchRoomButton = sBtn.GetComponent<UnityEngine.UI.Button>();
            Transform cBtn = existingBar.Find("CreateRoomButton");
            if (cBtn != null) createRoomButton = cBtn.GetComponent<UnityEngine.UI.Button>();
            return;
        }

        TMP_FontAsset font = titleText != null ? titleText.font : null;

        GameObject searchBar = new GameObject("RoomSearchBar", typeof(RectTransform));
        searchBar.transform.SetParent(headerBar, false);
        RectTransform barRt = searchBar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0.39f, 0.16f);
        barRt.anchorMax = new Vector2(0.825f, 0.84f);
        barRt.offsetMin = Vector2.zero;
        barRt.offsetMax = Vector2.zero;

        // 1. InputField nhập mã phòng (0.00 -> 0.52)
        GameObject inputGo = new GameObject(
            "RoomCodeInput",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(TMP_InputField)
        );
        inputGo.transform.SetParent(searchBar.transform, false);
        RectTransform inputRt = inputGo.GetComponent<RectTransform>();
        inputRt.anchorMin = new Vector2(0f, 0f);
        inputRt.anchorMax = new Vector2(0.52f, 1f);
        inputRt.offsetMin = Vector2.zero;
        inputRt.offsetMax = Vector2.zero;

        UnityEngine.UI.Image inputBg = inputGo.GetComponent<UnityEngine.UI.Image>();
        inputBg.color = new Color(0.05f, 0.08f, 0.14f, 0.98f);
        inputBg.raycastTarget = true;

        GameObject textArea = new GameObject(
            "Text Area",
            typeof(RectTransform),
            typeof(UnityEngine.UI.RectMask2D)
        );
        textArea.transform.SetParent(inputGo.transform, false);
        RectTransform taRt = textArea.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(6f, 2f);
        taRt.offsetMax = new Vector2(-6f, -2f);

        GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderGo.transform.SetParent(textArea.transform, false);
        RectTransform phRt = placeholderGo.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = Vector2.zero;
        phRt.offsetMax = Vector2.zero;
        TextMeshProUGUI phTxt = placeholderGo.GetComponent<TextMeshProUGUI>();
        if (font != null) phTxt.font = font;
        phTxt.text = "Nhap so phong...";
        phTxt.fontSize = 11f;
        phTxt.alignment = TextAlignmentOptions.MidlineLeft;
        phTxt.color = new Color(0.52f, 0.60f, 0.72f, 0.85f);
        phTxt.raycastTarget = false;

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(textArea.transform, false);
        RectTransform tRt = textGo.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;
        TextMeshProUGUI valTxt = textGo.GetComponent<TextMeshProUGUI>();
        if (font != null) valTxt.font = font;
        valTxt.text = "";
        valTxt.fontSize = 12f;
        valTxt.fontStyle = FontStyles.Bold;
        valTxt.alignment = TextAlignmentOptions.MidlineLeft;
        valTxt.color = Color.white;
        valTxt.raycastTarget = false;

        roomCodeInput = inputGo.GetComponent<TMP_InputField>();
        roomCodeInput.textViewport = taRt;
        roomCodeInput.textComponent = valTxt;
        roomCodeInput.placeholder = phTxt;
        roomCodeInput.characterLimit = 8;
        roomCodeInput.contentType = TMP_InputField.ContentType.Alphanumeric;
        roomCodeInput.targetGraphic = inputBg;

        // 2. Nút TIM PHONG (0.54 -> 0.76)
        searchRoomButton = CreateMiniHeaderButton(
            "SearchRoomButton",
            searchBar.transform,
            new Vector2(0.54f, 0f),
            new Vector2(0.76f, 1f),
            new Color(0.14f, 0.50f, 0.88f, 1f),
            "TIM",
            font
        );

        // 3. Nút TAO PHONG MOI (0.78 -> 1.00)
        createRoomButton = CreateMiniHeaderButton(
            "CreateRoomButton",
            searchBar.transform,
            new Vector2(0.78f, 0f),
            new Vector2(1.00f, 1f),
            new Color(0.16f, 0.56f, 0.52f, 1f),
            "TAO MOI",
            font
        );
    }

    static UnityEngine.UI.Button CreateMiniHeaderButton(
        string name,
        Transform parent,
        Vector2 aMin,
        Vector2 aMax,
        Color color,
        string label,
        TMP_FontAsset font
    )
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(UnityEngine.UI.Button)
        );
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        UnityEngine.UI.Image img = go.GetComponent<UnityEngine.UI.Image>();
        img.color = color;
        img.raycastTarget = true;

        UnityEngine.UI.Button btn = go.GetComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;

        GameObject lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lblGo.transform.SetParent(go.transform, false);
        RectTransform lrt = lblGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(2f, 1f);
        lrt.offsetMax = new Vector2(-2f, -1f);

        TextMeshProUGUI ltxt = lblGo.GetComponent<TextMeshProUGUI>();
        if (font != null) ltxt.font = font;
        ltxt.enableAutoSizing = true;
        ltxt.fontSizeMin = 8f;
        ltxt.fontSizeMax = 12f;
        ltxt.fontStyle = FontStyles.Bold;
        ltxt.alignment = TextAlignmentOptions.Center;
        ltxt.color = Color.white;
        ltxt.raycastTarget = false;
        ltxt.text = label;

        return btn;
    }

    void Update()
    {
        if (lobbyPanel == null || !lobbyPanel.activeSelf)
            return;

        UpdateGridCellSize();

        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.25f;
            RefreshLobby();
        }
    }

    void OnRectTransformDimensionsChange()
    {
        UpdateGridCellSize();
    }

    public void UpdateGridCellSize()
    {
        if (slotsGrid == null)
            return;

        RectTransform gridRect = slotsGrid.GetComponent<RectTransform>();
        if (gridRect == null)
            return;

        float w = gridRect.rect.width;
        float h = gridRect.rect.height;
        if (w <= 20f || h <= 20f)
            return;

        int cols = 4;
        int rows = 2;
        float spX = slotsGrid.spacing.x;
        float spY = slotsGrid.spacing.y;
        float padX = slotsGrid.padding.left + slotsGrid.padding.right;
        float padY = slotsGrid.padding.top + slotsGrid.padding.bottom;

        float cellW = Mathf.Max(60f, (w - padX - spX * (cols - 1)) / cols);
        float cellH = Mathf.Max(48f, (h - padY - spY * (rows - 1)) / rows);
        slotsGrid.cellSize = new Vector2(cellW, cellH);
    }

    public void OpenLobby()
    {
        EnsureSearchBarUI();
        BindButtons();

        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
        }

        UpdateGridCellSize();
        refreshTimer = 0f;
        RefreshLobby();
    }

    public void HideLobby()
    {
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
        }
    }

    public bool IsOpen => lobbyPanel != null && lobbyPanel.activeSelf;

    public void RefreshLobby()
    {
        if (lobbyPanel == null || !lobbyPanel.activeSelf)
            return;

        int maxSlots = MatchmakingManager.MaxCustomPlayers;
        MatchmakingManager mm = MatchmakingManager.Instance;
        NetworkRunner runner = mm != null ? mm.runner : null;

        bool isConnected = runner != null && runner.IsRunning;
        bool isHost = isConnected && runner.IsServer;

        List<PlayerRef> players = new List<PlayerRef>();
        if (isConnected)
        {
            players = runner.ActivePlayers
                .OrderBy(p => p.PlayerId)
                .ToList();
        }

        int humanCount = players.Count;
        int botCount = isHost ? Mathf.Clamp(MatchmakingManager.CustomBotCount, 0, Mathf.Max(0, maxSlots - humanCount)) : 0;
        int filledCount = Mathf.Min(maxSlots, humanCount + botCount);

        string roomCode = MatchmakingManager.CurrentCustomRoomCode;
        if (string.IsNullOrEmpty(roomCode))
        {
            roomCode = "----";
        }

        if (titleText != null)
        {
            titleText.text = "CUSTOM LOBBY";
        }

        if (roomInfoText != null)
        {
            roomInfoText.text = $"PHONG #{roomCode}   •   {filledCount} / {maxSlots}";
        }

        if (statusText != null)
        {
            if (!string.IsNullOrEmpty(MatchmakingManager.CustomStatusError))
            {
                statusText.text = MatchmakingManager.CustomStatusError;
                statusText.color = new Color(1.00f, 0.45f, 0.42f, 1f);
            }
            else if (!isConnected)
            {
                statusText.text = $"Dang ket noi\nphong #{roomCode}...";
                statusText.color = new Color(0.82f, 0.88f, 0.96f, 1f);
            }
            else if (isHost)
            {
                statusText.text = $"CHU PHONG #{roomCode}\nChia se so phong\nhoac bam START";
                statusText.color = new Color(0.82f, 0.94f, 1.00f, 1f);
            }
            else
            {
                statusText.text = $"DA VAO PHONG #{roomCode}\nDang cho Chu phong\nbat dau tran...";
                statusText.color = new Color(0.70f, 0.95f, 0.78f, 1f);
            }
        }

        int slotCount = slotNameTexts != null ? slotNameTexts.Length : 0;
        for (int i = 0; i < slotCount; i++)
        {
            UnityEngine.UI.Image bg = (slotBackgrounds != null && i < slotBackgrounds.Length) ? slotBackgrounds[i] : null;
            TMP_Text nameTxt = slotNameTexts[i];
            TMP_Text roleTxt = (slotRoleTexts != null && i < slotRoleTexts.Length) ? slotRoleTexts[i] : null;

            if (i < humanCount)
            {
                PlayerRef p = players[i];
                bool isLocal = runner != null && p == runner.LocalPlayer;
                bool isSlotHost = (i == 0);

                if (bg != null)
                    bg.color = isSlotHost ? OccupiedHostColor : OccupiedPlayerColor;

                if (nameTxt != null)
                {
                    nameTxt.text = isLocal ? $"Player #{p.PlayerId} (YOU)" : $"Player #{p.PlayerId}";
                    nameTxt.color = Color.white;
                }

                if (roleTxt != null)
                {
                    roleTxt.text = isSlotHost ? "HOST" : "PLAYER";
                    roleTxt.color = isSlotHost
                        ? new Color(1.0f, 0.84f, 0.30f, 1f)
                        : new Color(0.55f, 0.88f, 1.0f, 1f);
                }
            }
            else if (i < humanCount + botCount)
            {
                int botNumber = (i - humanCount) + 1;

                if (bg != null)
                    bg.color = OccupiedBotColor;

                if (nameTxt != null)
                {
                    nameTxt.text = $"Bot #{botNumber}";
                    nameTxt.color = new Color(0.90f, 0.86f, 1.0f, 1f);
                }

                if (roleTxt != null)
                {
                    roleTxt.text = "AI BOT";
                    roleTxt.color = new Color(0.76f, 0.62f, 1.0f, 1f);
                }
            }
            else
            {
                if (bg != null)
                    bg.color = EmptySlotColor;

                if (nameTxt != null)
                {
                    nameTxt.text = $"Slot {i + 1}";
                    nameTxt.color = new Color(0.55f, 0.60f, 0.68f, 1f);
                }

                if (roleTxt != null)
                {
                    roleTxt.text = "EMPTY SLOT";
                    roleTxt.color = new Color(0.45f, 0.50f, 0.58f, 1f);
                }
            }
        }

        if (startMatchButton != null)
        {
            startMatchButton.interactable = isHost;
        }

        if (startButtonText != null)
        {
            if (!isConnected)
                startButtonText.text = "CONNECTING...";
            else if (isHost)
                startButtonText.text = "START MATCH";
            else
                startButtonText.text = "WAITING FOR HOST";
        }

        if (addBotButton != null)
        {
            addBotButton.gameObject.SetActive(isHost);
            addBotButton.interactable = isHost && (humanCount + botCount < maxSlots);
        }

        if (removeBotButton != null)
        {
            removeBotButton.gameObject.SetActive(isHost);
            removeBotButton.interactable = isHost && (botCount > 0);
        }
    }

    public void OnClickSearchRoom()
    {
        string code = roomCodeInput != null ? roomCodeInput.text : "";
        if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.JoinCustomRoomByCode(code);
        }
    }

    void OnSubmitRoomCode(string value)
    {
        OnClickSearchRoom();
    }

    public void OnClickCreateNewRoom()
    {
        string desired = roomCodeInput != null ? roomCodeInput.text : "";
        if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.CreateCustomRoom(desired);
        }
    }

    public void OnClickStartMatch()
    {
        if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.StartCustomMatchFromLobby();
        }
    }

    public void OnClickAddBot()
    {
        if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.AddCustomBot();
            RefreshLobby();
        }
    }

    public void OnClickRemoveBot()
    {
        if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.RemoveCustomBot();
            RefreshLobby();
        }
    }

    public void OnClickLeave()
    {
        HideLobby();

        MenuUI menu = FindFirstObjectByType<MenuUI>();
        if (menu != null)
        {
            menu.CancelMatch();
        }
        else if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.ReturnToMenu();
        }
    }
}
