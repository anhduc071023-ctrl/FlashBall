using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Legacy References (Hidden Automatically)")]
    public Image healthFill;
    public TMP_Text scoreText;

    Image[] heartImages;
    int lastDisplayedHearts = 2;

    void Start()
    {
        HideLegacyElements();
        BuildHeartsHud();
        ArrangeMobileActionButtons();
        UpdateHealthHearts(2, 2);
    }

    void HideLegacyElements()
    {
        // Lược bỏ thanh máu bar cũ (đã thay bằng 2 trái tim Ruby)
        if (healthFill != null)
        {
            Transform parentBar = healthFill.transform.parent;
            if (parentBar != null && parentBar.name.Contains("HealthBar"))
                parentBar.gameObject.SetActive(false);
            else
                healthFill.gameObject.SetActive(false);
        }

        // Bỏ hoàn toàn phần Score
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(false);
        }
    }

    // ================= HEARTS UI (2 REAL RUBY HEARTS) =================

    void BuildHeartsHud()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("HudStatusPanel");
        if (existing != null)
        {
            Transform hc = existing.Find("HeartsContainer");
            if (hc != null) heartImages = hc.GetComponentsInChildren<Image>(true);
            return;
        }

        // Khung 2 Trái Tim gọn gàng đồng nhất theme Cyber-Dark ở góc trên-trái (không còn Score)
        GameObject panelBorder = new GameObject("HudStatusPanel", typeof(RectTransform), typeof(Image));
        panelBorder.transform.SetParent(canvas.transform, false);
        RectTransform bRt = panelBorder.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0f, 1f);
        bRt.anchorMax = new Vector2(0f, 1f);
        bRt.pivot = new Vector2(0f, 1f);
        bRt.localScale = Vector3.one;
        bRt.anchoredPosition = new Vector2(36f, -28f);
        bRt.sizeDelta = new Vector2(216f, 100f);
        Image bImg = panelBorder.GetComponent<Image>();
        bImg.color = UIThemeAssets.CardBorder;
        bImg.raycastTarget = false;

        GameObject panelBody = new GameObject("Body", typeof(RectTransform), typeof(Image));
        panelBody.transform.SetParent(panelBorder.transform, false);
        RectTransform bodyRt = panelBody.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(3f, 3f);
        bodyRt.offsetMax = new Vector2(-3f, -3f);
        Image bodyImg = panelBody.GetComponent<Image>();
        bodyImg.color = UIThemeAssets.CardBody;
        bodyImg.raycastTarget = false;

        GameObject container = new GameObject("HeartsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        container.transform.SetParent(panelBody.transform, false);
        RectTransform crt = container.GetComponent<RectTransform>();
        crt.anchorMin = Vector2.zero;
        crt.anchorMax = Vector2.one;
        crt.offsetMin = new Vector2(12f, 6f);
        crt.offsetMax = new Vector2(-12f, -6f);

        HorizontalLayoutGroup hlg = container.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        heartImages = new Image[2];
        for (int i = 0; i < 2; i++)
        {
            GameObject heartGo = new GameObject("Heart_" + (i + 1), typeof(RectTransform), typeof(Image));
            heartGo.transform.SetParent(container.transform, false);

            RectTransform hrt = heartGo.GetComponent<RectTransform>();
            hrt.sizeDelta = new Vector2(80f, 80f);

            Image img = heartGo.GetComponent<Image>();
            img.sprite = UIThemeAssets.HeartFull;
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = false;

            heartImages[i] = img;
        }
    }

    public void UpdateHealth(float percent)
    {
        int hearts = Mathf.Clamp(Mathf.CeilToInt(percent * 2f), 0, 2);
        UpdateHealthHearts(hearts, 2);
    }

    public void UpdateHealthHearts(int currentHearts, int maxHearts)
    {
        if (heartImages == null || heartImages.Length == 0)
        {
            BuildHeartsHud();
        }

        if (heartImages == null || heartImages.Length == 0)
            return;

        int clamped = Mathf.Clamp(currentHearts, 0, heartImages.Length);
        int prev = lastDisplayedHearts;
        lastDisplayedHearts = clamped;

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null)
                continue;

            bool aliveHeart = i < clamped;
            heartImages[i].sprite = aliveHeart ? UIThemeAssets.HeartFull : UIThemeAssets.HeartBroken;
            heartImages[i].color = aliveHeart
                ? Color.white
                : new Color(0.78f, 0.76f, 0.82f, 0.90f);

            if (!aliveHeart && i < prev)
            {
                StartCoroutine(AnimateBrokenHeart(heartImages[i].rectTransform));
            }
        }
    }

    IEnumerator AnimateBrokenHeart(RectTransform rt)
    {
        if (rt == null)
            yield break;

        float duration = 0.45f;
        float elapsed = 0f;

        while (elapsed < duration && rt != null)
        {
            float t = elapsed / duration;
            float shake = Mathf.Sin(t * Mathf.PI * 8f) * (1f - t) * 16f;
            float scale = t < 0.22f
                ? Mathf.Lerp(1f, 1.30f, t / 0.22f)
                : Mathf.Lerp(1.30f, 0.92f, (t - 0.22f) / 0.78f);

            rt.localScale = Vector3.one * scale;
            rt.localRotation = Quaternion.Euler(0f, 0f, shake);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rt != null)
        {
            rt.localScale = Vector3.one * 0.94f;
            rt.localRotation = Quaternion.identity;
        }
    }

    // ================= UNIFIED MOBILE ACTION BUTTONS (PARRY + DASH + JUMP + SKILL) =================

    void ArrangeMobileActionButtons()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        TMP_FontAsset font = scoreText != null ? scoreText.font : null;

        // 1. PARRY
        ParryChange parry = FindFirstObjectByType<ParryChange>(FindObjectsInactive.Include);
        if (parry != null)
        {
            RectTransform prt = parry.GetComponent<RectTransform>();
            UIThemeAssets.ApplyUnifiedHudCircleButton(
                prt,
                null,
                UIThemeAssets.IconParry,
                "PARRY",
                new Vector2(-185f, 185f),
                226f,
                font
            );

            if (parry.chargeCircle != null)
            {
                RectTransform crt = parry.chargeCircle.rectTransform;
                crt.SetAsLastSibling();
                crt.anchorMin = Vector2.zero;
                crt.anchorMax = Vector2.one;
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.localScale = Vector3.one;
                crt.offsetMin = new Vector2(-8f, -8f);
                crt.offsetMax = new Vector2(8f, 8f);

                if (UIThemeAssets.BtnChargeRing != null)
                {
                    parry.chargeCircle.sprite = UIThemeAssets.BtnChargeRing;
                }
                parry.chargeCircle.preserveAspect = true;
                parry.chargeCircle.type = Image.Type.Filled;
                parry.chargeCircle.fillMethod = Image.FillMethod.Radial360;
                parry.chargeCircle.fillOrigin = (int)Image.Origin360.Top;
                parry.chargeCircle.fillClockwise = true;
                parry.chargeCircle.color = Color.white;
                parry.chargeCircle.raycastTarget = false;
            }
        }

        // 2. DASH
        DashButtonUI dash = FindFirstObjectByType<DashButtonUI>(FindObjectsInactive.Include);
        if (dash != null)
        {
            UIThemeAssets.ApplyUnifiedHudCircleButton(
                dash.GetComponent<RectTransform>(),
                dash.cooldown != null ? dash.cooldown.rectTransform : null,
                UIThemeAssets.IconDash,
                "DASH",
                new Vector2(-405f, 130f),
                148f,
                font
            );
        }

        // 3. SKILL
        SkillButtonUI skill = FindFirstObjectByType<SkillButtonUI>(FindObjectsInactive.Include);
        if (skill != null)
        {
            Image skillIcon = UIThemeAssets.ApplyUnifiedHudCircleButton(
                skill.GetComponent<RectTransform>(),
                skill.cooldown != null ? skill.cooldown.rectTransform : null,
                UIThemeAssets.IconCloneBall,
                "SKILL",
                new Vector2(-152f, 402f),
                148f,
                font
            );
            skill.SetDynamicIconImage(skillIcon);
        }

        // 4. JUMP (Đồng nhất màu & khung)
        EnsureJumpButton(canvas, font);
    }

    void EnsureJumpButton(Canvas canvas, TMP_FontAsset font)
    {
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("JumpButton");
        RectTransform jrt;
        if (existing == null)
        {
            GameObject jumpGo = new GameObject(
                "JumpButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(JumpButtonUI)
            );
            jumpGo.transform.SetParent(canvas.transform, false);
            jrt = jumpGo.GetComponent<RectTransform>();
        }
        else
        {
            jrt = existing as RectTransform;
        }

        UIThemeAssets.ApplyUnifiedHudCircleButton(
            jrt,
            null,
            UIThemeAssets.IconJump,
            "JUMP",
            new Vector2(-355f, 326f),
            148f,
            font
        );

        Transform skillPanel = canvas.transform.Find("SkillSelectPanel");
        if (skillPanel != null) skillPanel.SetAsLastSibling();
        Transform resultRoot = canvas.transform.Find("MatchResultRoot");
        if (resultRoot != null) resultRoot.SetAsLastSibling();
    }
}
