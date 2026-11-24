using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchResultUI : MonoBehaviour
{
    public static MatchResultUI Instance;

    [Header("UI")]
    public GameObject resultPanel;
    public Image cardBorder;
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    public TMP_Text statsPrimaryText;
    public TMP_Text statsDeltaText;
    public Button returnMenuButton;

    public bool IsShown { get; private set; }
    public MatchResultSummary LastSummary { get; private set; }

    static readonly Color VictoryAccent = new Color(1.00f, 0.80f, 0.22f, 1f);
    static readonly Color DefeatAccent = new Color(0.92f, 0.28f, 0.32f, 1f);
    static readonly Color PositiveDeltaColor = new Color(0.35f, 0.92f, 0.52f, 1f);
    static readonly Color NegativeDeltaColor = new Color(0.96f, 0.40f, 0.42f, 1f);
    static readonly Color NeutralColor = new Color(0.78f, 0.84f, 0.92f, 1f);

    // Stage 1: Rank Progress Bar Widget (above Elo panel)
    GameObject rankWidgetRoot;
    RectTransform rankHaloRt;
    Image rankHaloImg;
    RectTransform rankIconRt;
    Image rankIconImg;
    Image rankProgressFill;
    TMP_Text rankNextMilestoneText;
    TMP_Text rankUpBannerText;
    Coroutine rankAnimRoutine;

    // Stage 2: Full-Screen Epic Rank-Up Assembly Panel (Old Rank -> Assembles -> New Rank!)
    GameObject rankUpModal;
    RectTransform ruHaloOuterRt;
    RectTransform ruHaloInnerRt;
    RectTransform ruOldIconRt;
    Image ruOldIconImg;
    RectTransform ruNewIconRt;
    Image ruNewIconImg;
    RectTransform[] ruShards;
    Image[] ruShardImgs;
    Image ruFlashOverlay;
    TMP_Text ruTitleText;
    TMP_Text ruTransitionText;
    Button ruContinueBtn;

    // Stage 3: Gold Reward Claim Panel
    GameObject goldClaimModal;
    RectTransform gcCoinRt;
    TMP_Text gcAmountText;
    TMP_Text gcSubText;
    TMP_Text gcNewbieNoticeText;
    Button gcClaimBtn;
    Image gcClaimBtnBg;
    TMP_Text gcClaimBtnLabel;
    bool goldClaimed;

    // Spectate when Host dies early while other live humans remain
    Button spectateButton;
    GameObject spectatorOverlayBanner;
    bool isSpectating;

    int currentStage; // 1 = Elo Result, 2 = Rank-Up Assembly, 3 = Gold Claim

    void Awake()
    {
        Instance = this;
        IsShown = false;

        if (returnMenuButton != null)
        {
            returnMenuButton.onClick.RemoveListener(OnClickPrimaryAction);
            returnMenuButton.onClick.RemoveListener(OnClickReturnToMenu);
            returnMenuButton.onClick.AddListener(OnClickPrimaryAction);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (isSpectating)
        {
            bool matchOver =
                (GameManager.Instance != null && PlayerFinder.CountAlivePlayers() <= 1) ||
                (MatchmakingManager.Instance == null || !MatchmakingManager.Instance.IsHostWithOtherLiveHumans());

            if (matchOver)
            {
                ExitSpectateShowResult();
            }
        }
    }

    public void ShowResult(bool isVictory, int placement, int totalPlayers)
    {
        if (IsShown)
            return;

        IsShown = true;
        goldClaimed = false;
        currentStage = 1;

        GameMatchMode mode = MatchmakingManager.CurrentMatchMode;

        // Ghi nhận Elo / Solo trước, CHƯA cộng Vàng vội để user tự nhấn CLAIM ở Panel thưởng Vàng!
        MatchResultSummary summary = PlayerStatsManager.RecordMatchResult(
            mode,
            isVictory,
            placement,
            totalPlayers,
            creditGoldImmediately: false
        );

        LastSummary = summary;
        ApplyStage1SummaryToUI(summary);
        GameAudioManager.SetMusicDuck(true);
        GameAudioManager.PlaySFX(isVictory ? GameSfxType.Victory : GameSfxType.Defeat, 0.92f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();
        }

        SetupSpectateOptionIfHost();

        if (summary.mode == GameMatchMode.Rank)
        {
            EnsureRankWidget();
            if (rankWidgetRoot != null)
            {
                rankWidgetRoot.SetActive(true);
                if (rankAnimRoutine != null) StopCoroutine(rankAnimRoutine);
                rankAnimRoutine = StartCoroutine(AnimateStage1EloBarRoutine(summary));
            }
        }
        else if (rankWidgetRoot != null)
        {
            rankWidgetRoot.SetActive(false);
        }
    }

    void ApplyStage1SummaryToUI(MatchResultSummary summary)
    {
        Color accent = summary.isVictory ? VictoryAccent : DefeatAccent;

        if (cardBorder != null) cardBorder.color = accent;

        if (titleText != null)
        {
            titleText.text = summary.isVictory ? "VICTORY!" : "DEFEAT";
            titleText.color = accent;
        }

        if (subtitleText != null)
        {
            string modeLabel = summary.mode.ToString().ToUpper();
            subtitleText.text = $"{modeLabel} MATCH   •   HANG #{summary.placement} / {summary.totalPlayers}";
        }

        if (summary.mode == GameMatchMode.Solo)
        {
            if (statsPrimaryText != null)
            {
                statsPrimaryText.text = $"SOLO: {summary.soloWins} THANG  |  {summary.soloLosses} THUA";
                statsPrimaryText.color = Color.white;
            }

            if (statsDeltaText != null)
            {
                statsDeltaText.text = summary.isVictory ? "+1 THANG" : "+1 THUA";
                statsDeltaText.color = summary.isVictory ? PositiveDeltaColor : NegativeDeltaColor;
            }
        }
        else if (summary.mode == GameMatchMode.Rank)
        {
            if (statsPrimaryText != null)
            {
                statsPrimaryText.text = $"RANK: {PlayerStatsManager.GetRankTitleByTier(summary.oldTier).ToUpper()}   •   {summary.oldElo} ELO";
                statsPrimaryText.color = PlayerStatsManager.GetRankColorByTier(summary.oldTier);
            }

            if (statsDeltaText != null)
            {
                string sign = summary.eloDelta >= 0 ? "+" : "";
                statsDeltaText.text = $"{sign}{summary.eloDelta} ELO  ({summary.oldElo} -> {summary.newElo})";
                statsDeltaText.color = summary.eloDelta >= 0 ? PositiveDeltaColor : NegativeDeltaColor;
            }
        }
        else
        {
            if (statsPrimaryText != null)
            {
                statsPrimaryText.text = summary.mode == GameMatchMode.Custom
                    ? "HOAN THANH TRAN CUSTOM"
                    : "HOAN THANH LUYEN TAP";
                statsPrimaryText.color = NeutralColor;
            }

            if (statsDeltaText != null)
            {
                statsDeltaText.text = "San sang nhan phan thuong tran dau!";
                statsDeltaText.color = UIThemeAssets.GoldAccent;
            }
        }

        UpdateStage1ButtonText();
    }

    void UpdateStage1ButtonText()
    {
        if (returnMenuButton == null) return;
        TMP_Text btnLabel = returnMenuButton.GetComponentInChildren<TMP_Text>();
        if (btnLabel == null) return;

        if (LastSummary.isRankUp)
            btnLabel.text = "THANG HANG! ->";
        else if (LastSummary.goldEarned > 0 && !goldClaimed)
            btnLabel.text = "NHAN THUONG ->";
        else
            btnLabel.text = "VE MENU";
    }

    void OnClickPrimaryAction()
    {
        if (currentStage == 1)
        {
            if (LastSummary.isRankUp)
            {
                OpenStage2RankUpAssembly();
                return;
            }

            if (LastSummary.goldEarned > 0 && !goldClaimed)
            {
                OpenStage3GoldClaim();
                return;
            }
        }

        OnClickReturnToMenu();
    }

    // ================= STAGE 1: ANIMATED ELO COUNT & PROGRESS BAR =================

    void EnsureRankWidget()
    {
        if (rankWidgetRoot != null || resultPanel == null)
            return;

        Transform parent = cardBorder != null ? cardBorder.transform : resultPanel.transform;
        TMP_FontAsset font = titleText != null ? titleText.font : null;

        rankWidgetRoot = new GameObject("RankProgressionWidget", typeof(RectTransform), typeof(Image));
        rankWidgetRoot.transform.SetParent(parent, false);
        RectTransform rt = rankWidgetRoot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 18f);
        rt.sizeDelta = new Vector2(760f, 148f);
        rankWidgetRoot.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.16f, 0.96f);

        GameObject haloGo = new GameObject("RankHalo", typeof(RectTransform), typeof(Image));
        haloGo.transform.SetParent(rankWidgetRoot.transform, false);
        rankHaloRt = haloGo.GetComponent<RectTransform>();
        rankHaloRt.anchorMin = new Vector2(0f, 0.5f);
        rankHaloRt.anchorMax = new Vector2(0f, 0.5f);
        rankHaloRt.pivot = new Vector2(0.5f, 0.5f);
        rankHaloRt.anchoredPosition = new Vector2(82f, 0f);
        rankHaloRt.sizeDelta = new Vector2(138f, 138f);
        rankHaloImg = haloGo.GetComponent<Image>();
        if (UIThemeAssets.BtnChargeRing != null) rankHaloImg.sprite = UIThemeAssets.BtnChargeRing;
        rankHaloImg.color = new Color(1f, 0.85f, 0.25f, 0.35f);

        GameObject iconGo = new GameObject("RankEmblem", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(rankWidgetRoot.transform, false);
        rankIconRt = iconGo.GetComponent<RectTransform>();
        rankIconRt.anchorMin = new Vector2(0f, 0.5f);
        rankIconRt.anchorMax = new Vector2(0f, 0.5f);
        rankIconRt.pivot = new Vector2(0.5f, 0.5f);
        rankIconRt.anchoredPosition = new Vector2(82f, 0f);
        rankIconRt.sizeDelta = new Vector2(112f, 112f);
        rankIconImg = iconGo.GetComponent<Image>();
        rankIconImg.preserveAspect = true;

        rankUpBannerText = CreateText("RankBanner", rankWidgetRoot.transform, new Vector2(0.22f, 0.58f), new Vector2(0.96f, 0.92f),
            "", 22f, 30f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, VictoryAccent, font);

        GameObject barTrack = new GameObject("BarTrack", typeof(RectTransform), typeof(Image));
        barTrack.transform.SetParent(rankWidgetRoot.transform, false);
        RectTransform tRt = barTrack.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.22f, 0.34f);
        tRt.anchorMax = new Vector2(0.96f, 0.52f);
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;
        barTrack.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.26f, 1f);

        GameObject barFillGo = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
        barFillGo.transform.SetParent(barTrack.transform, false);
        RectTransform fRt = barFillGo.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.offsetMin = Vector2.zero;
        fRt.offsetMax = Vector2.zero;
        rankProgressFill = barFillGo.GetComponent<Image>();
        rankProgressFill.type = Image.Type.Filled;
        rankProgressFill.fillMethod = Image.FillMethod.Horizontal;
        rankProgressFill.fillOrigin = (int)Image.OriginHorizontal.Left;

        rankNextMilestoneText = CreateText("MilestoneTxt", rankWidgetRoot.transform, new Vector2(0.22f, 0.06f), new Vector2(0.96f, 0.32f),
            "", 18f, 23f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.84f, 0.92f, 1f, 1f), font);
    }

    IEnumerator AnimateStage1EloBarRoutine(MatchResultSummary s)
    {
        float startFill = PlayerStatsManager.GetRankProgressNormalized(s.oldElo);
        float endFill = s.isRankUp ? 1f : PlayerStatsManager.GetRankProgressNormalized(s.newElo);
        Color oldCol = PlayerStatsManager.GetRankColorByTier(s.oldTier);
        Color newCol = PlayerStatsManager.GetRankColorByTier(s.newTier);

        rankIconImg.sprite = UIThemeAssets.GetRankIconByTier(s.oldTier);
        rankProgressFill.fillAmount = startFill;
        rankProgressFill.color = oldCol;
        rankUpBannerText.text = $"TIEN TRINH RANK  •  {PlayerStatsManager.GetRankTitleByTier(s.oldTier).ToUpper()}";
        rankUpBannerText.color = oldCol;
        rankNextMilestoneText.text = $"Moc tiep theo: {PlayerStatsManager.FormatNextRankProgress(s.oldElo)}";

        yield return new WaitForSecondsRealtime(0.25f);

        float dur = 0.85f;
        float e = 0f;
        while (e < dur)
        {
            float t = Mathf.Clamp01(e / dur);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            int curElo = Mathf.RoundToInt(Mathf.Lerp(s.oldElo, s.newElo, ease));
            rankProgressFill.fillAmount = Mathf.Lerp(startFill, endFill, ease);
            if (statsPrimaryText != null)
            {
                statsPrimaryText.text = $"RANK: {PlayerStatsManager.GetRankTitle(curElo).ToUpper()}   •   {curElo} ELO";
                statsPrimaryText.color = PlayerStatsManager.GetRankColor(curElo);
            }
            rankNextMilestoneText.text = $"Moc tiep theo: {PlayerStatsManager.FormatNextRankProgress(curElo)}";
            rankHaloRt.Rotate(0f, 0f, -90f * Time.unscaledDeltaTime);
            e += Time.unscaledDeltaTime;
            yield return null;
        }

        rankProgressFill.fillAmount = endFill;
        if (s.isRankUp)
        {
            rankUpBannerText.text = $"★ DU DIEM THANG HANG -> {s.rankTitle.ToUpper()}! ★";
            rankUpBannerText.color = VictoryAccent;
            // Tự động mở màn hình Lắp Ráp Thăng Hạng sau 0.65s (hoặc bấm nút mở ngay)
            yield return new WaitForSecondsRealtime(0.65f);
            if (currentStage == 1 && !isSpectating)
            {
                OpenStage2RankUpAssembly();
            }
        }
        else
        {
            rankIconImg.sprite = UIThemeAssets.GetRankIconByTier(s.newTier);
            rankProgressFill.color = newCol;
            rankNextMilestoneText.text = $"Moc tiep theo: {PlayerStatsManager.FormatNextRankProgress(s.newElo)}";
        }
    }

    // ================= STAGE 2: EPIC FULL-SCREEN RANK-UP ASSEMBLY ANIMATION =================

    void OpenStage2RankUpAssembly()
    {
        if (currentStage == 2) return;
        currentStage = 2;

        if (resultPanel != null) resultPanel.SetActive(false);
        EnsureRankUpAssemblyModal();

        rankUpModal.SetActive(true);
        rankUpModal.transform.SetAsLastSibling();
        StartCoroutine(PlayRankUpAssemblyAnimation(LastSummary));
    }

    void EnsureRankUpAssemblyModal()
    {
        if (rankUpModal != null) return;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        TMP_FontAsset font = titleText != null ? titleText.font : null;

        rankUpModal = new GameObject("RankUpAssemblyModal", typeof(RectTransform), typeof(Image));
        rankUpModal.transform.SetParent(canvas.transform, false);
        RectTransform rt = rankUpModal.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rankUpModal.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.08f, 0.96f);

        // Dual spinning radiant halos behind emblem
        ruHaloOuterRt = CreateCenteredSprite(rankUpModal.transform, "HaloOuter", UIThemeAssets.BtnChargeRing, new Vector2(540f, 540f), new Vector2(0f, 35f), new Color(1f, 0.82f, 0.25f, 0.45f), out _);
        ruHaloInnerRt = CreateCenteredSprite(rankUpModal.transform, "HaloInner", UIThemeAssets.BtnCircleFrame, new Vector2(400f, 400f), new Vector2(0f, 35f), new Color(0.24f, 0.86f, 1f, 0.50f), out _);

        // Old Rank Emblem
        ruOldIconRt = CreateCenteredSprite(rankUpModal.transform, "OldRankIcon", null, new Vector2(260f, 260f), new Vector2(0f, 35f), Color.white, out ruOldIconImg);

        // 4 Metallic Assembly Shards (fly in from 4 corners to forge the new emblem!)
        ruShards = new RectTransform[4];
        ruShardImgs = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            ruShards[i] = CreateCenteredSprite(rankUpModal.transform, "AssemblyShard_" + i, UIThemeAssets.BtnCircleFrame, new Vector2(120f, 120f), Vector2.zero, UIThemeAssets.GoldAccent, out ruShardImgs[i]);
            ruShards[i].gameObject.SetActive(false);
        }

        // New Rank Emblem
        ruNewIconRt = CreateCenteredSprite(rankUpModal.transform, "NewRankIcon", null, new Vector2(310f, 310f), new Vector2(0f, 35f), Color.white, out ruNewIconImg);
        ruNewIconRt.gameObject.SetActive(false);

        // White-gold assembly flash overlay
        GameObject flashGo = new GameObject("AssemblyFlash", typeof(RectTransform), typeof(Image));
        flashGo.transform.SetParent(rankUpModal.transform, false);
        RectTransform flRt = flashGo.GetComponent<RectTransform>();
        flRt.anchorMin = Vector2.zero;
        flRt.anchorMax = Vector2.one;
        flRt.offsetMin = Vector2.zero;
        flRt.offsetMax = Vector2.zero;
        ruFlashOverlay = flashGo.GetComponent<Image>();
        ruFlashOverlay.color = new Color(1f, 0.96f, 0.78f, 0f);
        ruFlashOverlay.raycastTarget = false;

        ruTitleText = CreateText("RuTitle", rankUpModal.transform, new Vector2(0.10f, 0.80f), new Vector2(0.90f, 0.93f),
            "NANG CAP BAC RANK...", 32f, 56f, FontStyles.Bold, TextAlignmentOptions.Center, VictoryAccent, font);

        ruTransitionText = CreateText("RuTrans", rankUpModal.transform, new Vector2(0.10f, 0.19f), new Vector2(0.90f, 0.30f),
            "", 26f, 40f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, font);

        // Continue Button -> opens Gold Claim Panel!
        GameObject btnGo = new GameObject("RuContinueBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(rankUpModal.transform, false);
        RectTransform bRt = btnGo.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.36f, 0.06f);
        bRt.anchorMax = new Vector2(0.64f, 0.16f);
        bRt.offsetMin = Vector2.zero;
        bRt.offsetMax = Vector2.zero;
        btnGo.GetComponent<Image>().color = UIThemeAssets.SuccessGreen;
        ruContinueBtn = btnGo.GetComponent<Button>();
        ruContinueBtn.onClick.AddListener(OpenStage3GoldClaim);
        CreateText("Lbl", btnGo.transform, Vector2.zero, Vector2.one, "NHAN THUONG ->", 22f, 32f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, font);
        ruContinueBtn.gameObject.SetActive(false);

        rankUpModal.SetActive(false);
    }

    IEnumerator PlayRankUpAssemblyAnimation(MatchResultSummary s)
    {
        Color oldCol = PlayerStatsManager.GetRankColorByTier(s.oldTier);
        Color newCol = PlayerStatsManager.GetRankColorByTier(s.newTier);
        string oldName = PlayerStatsManager.GetRankTitleByTier(s.oldTier).ToUpper();
        string newName = PlayerStatsManager.GetRankTitleByTier(s.newTier).ToUpper();

        ruOldIconImg.sprite = UIThemeAssets.GetRankIconByTier(s.oldTier);
        ruNewIconImg.sprite = UIThemeAssets.GetRankIconByTier(s.newTier);
        ruOldIconRt.gameObject.SetActive(true);
        ruNewIconRt.gameObject.SetActive(false);
        ruContinueBtn.gameObject.SetActive(false);
        ruTitleText.text = "DANG TIEN HOA BAC RANK...";
        ruTitleText.color = oldCol;
        ruTransitionText.text = oldName;
        ruTransitionText.color = oldCol;

        // Phase 1 (0.75s): Huy hiệu cũ xuất hiện & tích tụ năng lượng rung lắc
        float e = 0f;
        while (e < 0.75f)
        {
            float t = e / 0.75f;
            float shake = Mathf.Sin(t * Mathf.PI * 16f) * (16f * t);
            ruOldIconRt.localRotation = Quaternion.Euler(0f, 0f, shake);
            ruOldIconRt.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.18f, t);
            ruHaloOuterRt.Rotate(0f, 0f, -120f * Time.unscaledDeltaTime);
            ruHaloInnerRt.Rotate(0f, 0f, 180f * Time.unscaledDeltaTime);
            e += Time.unscaledDeltaTime;
            yield return null;
        }

        // Phase 2 (0.70s): 4 mảnh giáp/cánh năng lượng bay từ 4 góc LAO VÀO LẮP RÁP quanh tâm!
        Vector2[] startOffsets = new Vector2[]
        {
            new Vector2(-460f, 340f),
            new Vector2(460f, 340f),
            new Vector2(-460f, -280f),
            new Vector2(460f, -280f)
        };
        for (int i = 0; i < 4; i++)
        {
            ruShards[i].gameObject.SetActive(true);
            ruShards[i].anchoredPosition = startOffsets[i];
            ruShardImgs[i].color = Color.Lerp(newCol, Color.white, 0.35f);
        }

        e = 0f;
        while (e < 0.70f)
        {
            float t = Mathf.Clamp01(e / 0.70f);
            float snap = t * t * t; // tăng tốc cực mạnh khi ghép vào tâm!
            for (int i = 0; i < 4; i++)
            {
                ruShards[i].anchoredPosition = Vector2.Lerp(startOffsets[i], new Vector2(0f, 35f), snap);
                ruShards[i].localRotation = Quaternion.Euler(0f, 0f, (1f - t) * (i % 2 == 0 ? 360f : -360f));
                ruShards[i].localScale = Vector3.one * Mathf.Lerp(1.35f, 0.45f, snap);
            }
            ruOldIconRt.localScale = Vector3.one * Mathf.Lerp(1.18f, 0.15f, snap);
            ruHaloOuterRt.Rotate(0f, 0f, -380f * Time.unscaledDeltaTime);
            ruHaloInnerRt.Rotate(0f, 0f, 440f * Time.unscaledDeltaTime);
            e += Time.unscaledDeltaTime;
            yield return null;
        }

        // Phase 3: BÙNG NỔ ÁNH SÁNG LẮP RÁP HOÀN TẤT -> XUẤT HIỆN HUY HIỆU RANK MỚI!
        GameAudioManager.PlaySFX(GameSfxType.RankUp, 0.96f);
        ruOldIconRt.gameObject.SetActive(false);
        for (int i = 0; i < 4; i++) ruShards[i].gameObject.SetActive(false);

        ruNewIconRt.gameObject.SetActive(true);
        ruNewIconRt.localScale = Vector3.one * 0.25f;
        ruTitleText.text = $"★ THANG HANG THANH CONG! ★";
        ruTitleText.color = VictoryAccent;
        ruTransitionText.text = $"{oldName}   >>>   {newName}  ({s.newElo} ELO)";
        ruTransitionText.color = newCol;

        e = 0f;
        while (e < 0.85f)
        {
            float t = Mathf.Clamp01(e / 0.85f);
            ruFlashOverlay.color = new Color(1f, 0.96f, 0.80f, Mathf.Pow(1f - t, 2.2f) * 0.92f);
            float bounce = t < 0.35f
                ? Mathf.Lerp(0.25f, 1.38f, t / 0.35f)
                : Mathf.Lerp(1.38f, 1.00f, (t - 0.35f) / 0.65f);
            ruNewIconRt.localScale = Vector3.one * bounce;
            ruHaloOuterRt.Rotate(0f, 0f, -220f * Time.unscaledDeltaTime);
            ruHaloInnerRt.Rotate(0f, 0f, 160f * Time.unscaledDeltaTime);
            e += Time.unscaledDeltaTime;
            yield return null;
        }

        ruFlashOverlay.color = new Color(1f, 1f, 1f, 0f);
        ruContinueBtn.gameObject.SetActive(true);

        // Hào quang xoay ăn mừng liên tục
        while (rankUpModal != null && rankUpModal.activeInHierarchy)
        {
            ruHaloOuterRt.Rotate(0f, 0f, -55f * Time.unscaledDeltaTime);
            ruHaloInnerRt.Rotate(0f, 0f, 40f * Time.unscaledDeltaTime);
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4.5f) * 0.05f;
            ruNewIconRt.localScale = Vector3.one * pulse;
            yield return null;
        }
    }

    // ================= STAGE 3: GOLD REWARD CLAIM PANEL =================

    void OpenStage3GoldClaim()
    {
        currentStage = 3;
        if (resultPanel != null) resultPanel.SetActive(false);
        if (rankUpModal != null) rankUpModal.SetActive(false);

        EnsureGoldClaimModal();
        goldClaimModal.SetActive(true);
        goldClaimModal.transform.SetAsLastSibling();

        gcAmountText.text = $"+{LastSummary.goldEarned}";
        gcSubText.text = LastSummary.isVictory ? "Phan thuong chien thang tran dau!" : "Phan thuong tham gia tran dau!";
        gcNewbieNoticeText.text = LastSummary.newbieMailJustUnlocked
            ? "★ Ban vua nhan duoc thu QUA TAN THU (+500 & chieu FREEZE) trong Hom Thu ngoai Menu! ★"
            : "";

        gcClaimBtn.interactable = true;
        gcClaimBtnBg.color = UIThemeAssets.SuccessGreen;
        gcClaimBtnLabel.text = "CLAIM";
    }

    void EnsureGoldClaimModal()
    {
        if (goldClaimModal != null) return;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        TMP_FontAsset font = titleText != null ? titleText.font : null;

        goldClaimModal = new GameObject("GoldClaimModal", typeof(RectTransform), typeof(Image));
        goldClaimModal.transform.SetParent(canvas.transform, false);
        RectTransform rt = goldClaimModal.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        goldClaimModal.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.09f, 0.90f);

        // Card
        GameObject border = new GameObject("CardBorder", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(goldClaimModal.transform, false);
        RectTransform bRt = border.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.5f, 0.5f);
        bRt.anchorMax = new Vector2(0.5f, 0.5f);
        bRt.pivot = new Vector2(0.5f, 0.5f);
        bRt.sizeDelta = new Vector2(780f, 520f);
        border.GetComponent<Image>().color = UIThemeAssets.GoldAccent;

        GameObject body = new GameObject("CardBody", typeof(RectTransform), typeof(Image));
        body.transform.SetParent(border.transform, false);
        RectTransform bodyRt = body.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(4f, 4f);
        bodyRt.offsetMax = new Vector2(-4f, -4f);
        body.GetComponent<Image>().color = UIThemeAssets.CardBody;

        CreateText("Title", body.transform, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.95f),
            "PHAN THUONG TRAN DAU", 28f, 42f, FontStyles.Bold, TextAlignmentOptions.Center, VictoryAccent, font);

        gcSubText = CreateText("Sub", body.transform, new Vector2(0.05f, 0.69f), new Vector2(0.95f, 0.80f),
            "", 18f, 24f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.84f, 0.92f, 1f, 1f), font);

        // Coin icon + big numeric reward (NO "VANG" word!)
        gcCoinRt = CreateCenteredSprite(body.transform, "RewardCoin", UIThemeAssets.IconGold, new Vector2(150f, 150f), new Vector2(-95f, 12f), Color.white, out _);
        gcAmountText = CreateText("Amount", body.transform, new Vector2(0.47f, 0.36f), new Vector2(0.90f, 0.68f),
            "+120", 40f, 66f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, VictoryAccent, font);

        gcNewbieNoticeText = CreateText("NewbieNotice", body.transform, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.36f),
            "", 16f, 22f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.32f, 0.94f, 1.00f, 1f), font);
        gcNewbieNoticeText.textWrappingMode = TextWrappingModes.Normal;

        // CLAIM button
        GameObject btnGo = new GameObject("ClaimGoldBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(body.transform, false);
        RectTransform cRt = btnGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.24f, 0.05f);
        cRt.anchorMax = new Vector2(0.76f, 0.20f);
        cRt.offsetMin = Vector2.zero;
        cRt.offsetMax = Vector2.zero;
        gcClaimBtnBg = btnGo.GetComponent<Image>();
        gcClaimBtnBg.color = UIThemeAssets.SuccessGreen;
        gcClaimBtn = btnGo.GetComponent<Button>();
        gcClaimBtn.targetGraphic = gcClaimBtnBg;
        gcClaimBtn.onClick.AddListener(OnClickClaimMatchGold);
        gcClaimBtnLabel = CreateText("Lbl", btnGo.transform, Vector2.zero, Vector2.one, "CLAIM", 24f, 34f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, font);

        goldClaimModal.SetActive(false);
    }

    void OnClickClaimMatchGold()
    {
        if (!goldClaimed)
        {
            goldClaimed = true;
            PlayerStatsManager.AddGold(LastSummary.goldEarned);
            StartCoroutine(CoinClaimPopRoutine());
        }
        else
        {
            OnClickReturnToMenu();
        }
    }

    IEnumerator CoinClaimPopRoutine()
    {
        gcClaimBtn.interactable = false;
        gcClaimBtnBg.color = UIThemeAssets.PrimaryCyan;
        gcClaimBtnLabel.text = "DA NHAN!";
        gcSubText.text = $"Da cong +{LastSummary.goldEarned}! (Tong hien co: {PlayerStatsManager.PlayerGold})";
        gcSubText.color = PositiveDeltaColor;

        float e = 0f;
        while (e < 0.45f)
        {
            float t = e / 0.45f;
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.35f;
            if (gcCoinRt != null) gcCoinRt.localScale = Vector3.one * s;
            e += Time.unscaledDeltaTime;
            yield return null;
        }
        if (gcCoinRt != null) gcCoinRt.localScale = Vector3.one;

        gcClaimBtn.interactable = true;
        gcClaimBtnLabel.text = "VE MENU";
    }

    // ================= HOST SPECTATE PROTECTION =================

    void SetupSpectateOptionIfHost()
    {
        bool hostHasOthers =
            MatchmakingManager.Instance != null &&
            MatchmakingManager.Instance.IsHostWithOtherLiveHumans();

        if (!hostHasOthers)
        {
            if (spectateButton != null) spectateButton.gameObject.SetActive(false);
            return;
        }

        if (spectateButton == null && resultPanel != null)
        {
            Transform parent = cardBorder != null ? cardBorder.transform : resultPanel.transform;
            TMP_FontAsset font = titleText != null ? titleText.font : null;

            GameObject go = new GameObject("SpectateButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -14f);
            rt.sizeDelta = new Vector2(460f, 64f);

            Image img = go.GetComponent<Image>();
            img.color = UIThemeAssets.PrimaryCyan;
            spectateButton = go.GetComponent<Button>();
            spectateButton.targetGraphic = img;
            spectateButton.onClick.AddListener(EnterSpectateMode);

            CreateText("Label", go.transform, Vector2.zero, Vector2.one,
                "QUAN SAT (GIU TRAN CHO NGUOI CON LAI)", 16f, 24f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, font);
        }

        if (spectateButton != null) spectateButton.gameObject.SetActive(true);
    }

    void EnterSpectateMode()
    {
        isSpectating = true;
        if (resultPanel != null) resultPanel.SetActive(false);

        if (spectatorOverlayBanner == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                TMP_FontAsset font = titleText != null ? titleText.font : null;
                spectatorOverlayBanner = new GameObject("SpectatorTopBanner", typeof(RectTransform), typeof(Image));
                spectatorOverlayBanner.transform.SetParent(canvas.transform, false);
                RectTransform rt = spectatorOverlayBanner.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -24f);
                rt.sizeDelta = new Vector2(640f, 64f);
                spectatorOverlayBanner.GetComponent<Image>().color = new Color(0.06f, 0.10f, 0.18f, 0.90f);

                CreateText("Txt", spectatorOverlayBanner.transform, new Vector2(0.03f, 0.05f), new Vector2(0.72f, 0.95f),
                    "DANG QUAN SAT  •  CHO TRAN KET THUC", 18f, 22f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, VictoryAccent, font);

                GameObject btnGo = new GameObject("BackBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(spectatorOverlayBanner.transform, false);
                RectTransform brt = btnGo.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.74f, 0.12f);
                brt.anchorMax = new Vector2(0.97f, 0.88f);
                brt.offsetMin = Vector2.zero;
                brt.offsetMax = Vector2.zero;
                btnGo.GetComponent<Image>().color = UIThemeAssets.DangerRed;
                btnGo.GetComponent<Button>().onClick.AddListener(ExitSpectateShowResult);
                CreateText("Lbl", btnGo.transform, Vector2.zero, Vector2.one, "BANG KET QUA", 16f, 20f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, font);
            }
        }

        if (spectatorOverlayBanner != null) spectatorOverlayBanner.SetActive(true);
    }

    void ExitSpectateShowResult()
    {
        isSpectating = false;
        if (spectatorOverlayBanner != null) spectatorOverlayBanner.SetActive(false);
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();
        }
        SetupSpectateOptionIfHost();
    }

    public void OnClickReturnToMenu()
    {
        // Phòng trường hợp thoát sớm chưa kịp Claim vàng -> vẫn cộng an toàn nếu đã vào màn claim
        if (!goldClaimed && LastSummary.goldEarned > 0 && currentStage >= 3)
        {
            goldClaimed = true;
            PlayerStatsManager.AddGold(LastSummary.goldEarned);
        }

        if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.ReturnToMenu();
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    // ================= HELPERS =================

    RectTransform CreateCenteredSprite(Transform parent, string name, Sprite sp, Vector2 size, Vector2 pos, Color col, out Image img)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        img = go.GetComponent<Image>();
        img.sprite = sp;
        img.preserveAspect = true;
        img.color = col;
        img.raycastTarget = false;
        return rt;
    }

    TMP_Text CreateText(string name, Transform parent, Vector2 aMin, Vector2 aMax, string content, float fsMin, float fsMax, FontStyles style, TextAlignmentOptions align, Color col, TMP_FontAsset font)
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
}
