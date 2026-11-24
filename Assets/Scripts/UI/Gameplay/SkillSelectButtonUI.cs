using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSelectButtonUI : MonoBehaviour
{
    public SkillType skillType;

    public Button button;

    [Header("Selected Effect")]
    public GameObject selectedBorder;

    static SkillSelectButtonUI currentSelected;

    GameObject styledSelectedTag;
    Image cardOutlineImage;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveListener(
                SelectSkill
            );

            button.onClick.AddListener(
                SelectSkill
            );
        }

        SetSelectedVisual(false);
    }

    public void ApplyCardStyle(TMP_FontAsset fallbackFont)
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(368f, 415f);
            rt.anchoredPosition = Vector2.zero;
        }

        Color accentColor = UIThemeAssets.GetSkillAccentColor(skillType);
        Sprite skillIcon = UIThemeAssets.GetSkillIcon(skillType);
        string skillTitle;
        string skillDesc;
        string cooldownLabel;

        switch (skillType)
        {
            case SkillType.CloneBall:
                skillTitle = "CLONE BALL";
                skillDesc = "Tao qua bong gia duoi dich\nVo tan khi cham hoac bi parry";
                cooldownLabel = "CD: 10s";
                break;

            case SkillType.Invisible:
                skillTitle = "INVISIBLE";
                skillDesc = "Tang hinh trong 5 giay\nBong tu bo qua ban";
                cooldownLabel = "CD: 20s";
                break;

            case SkillType.Tornado:
                skillTitle = "TORNADO";
                skillDesc = "Cuon bong xoay len cao\nRoi lao thang ve phia dich";
                cooldownLabel = "CD: 20s";
                break;

            default:
                skillTitle = skillType.ToString().ToUpper();
                skillDesc = "Ky nang dac biet";
                cooldownLabel = "";
                break;
        }

        // Ẩn các text cũ bị lệch/méo
        TMP_Text[] oldTexts = GetComponentsInChildren<TMP_Text>(true);
        TMP_FontAsset font = fallbackFont;
        foreach (TMP_Text t in oldTexts)
        {
            if (font == null && t.font != null)
                font = t.font;
            t.gameObject.SetActive(false);
        }

        // Nền thẻ đồng nhất theme Cyber-Dark
        Image bg = GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.sprite = null;
        bg.color = UIThemeAssets.SubCardBg;
        bg.raycastTarget = true;

        if (button != null)
        {
            button.targetGraphic = bg;
        }

        if (selectedBorder != null)
        {
            selectedBorder.SetActive(false);
        }

        // Khung viền ngoài (màu xanh theme khi bình thường, đổi sang vàng sáng khi được chọn)
        GameObject outlineGo = new GameObject("SelectedOutline", typeof(RectTransform), typeof(Image));
        outlineGo.transform.SetParent(transform, false);
        outlineGo.transform.SetAsFirstSibling();
        RectTransform outRt = outlineGo.GetComponent<RectTransform>();
        outRt.anchorMin = Vector2.zero;
        outRt.anchorMax = Vector2.one;
        outRt.offsetMin = new Vector2(-5f, -5f);
        outRt.offsetMax = new Vector2(5f, 5f);
        cardOutlineImage = outlineGo.GetComponent<Image>();
        cardOutlineImage.color = UIThemeAssets.GoldAccent;
        cardOutlineImage.raycastTarget = false;
        outlineGo.SetActive(false);

        // Nền trong thẻ
        GameObject innerBg = new GameObject("CardInner", typeof(RectTransform), typeof(Image));
        innerBg.transform.SetParent(transform, false);
        RectTransform inRt = innerBg.GetComponent<RectTransform>();
        inRt.anchorMin = Vector2.zero;
        inRt.anchorMax = Vector2.one;
        inRt.offsetMin = Vector2.zero;
        inRt.offsetMax = Vector2.zero;
        Image inImg = innerBg.GetComponent<Image>();
        inImg.color = UIThemeAssets.SubCardBg;
        inImg.raycastTarget = false;

        // Dải nhấn mỏng phía trên cùng (tinh tế, đồng nhất)
        GameObject accentBar = new GameObject("AccentTopBar", typeof(RectTransform), typeof(Image));
        accentBar.transform.SetParent(innerBg.transform, false);
        RectTransform barRt = accentBar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.anchoredPosition = Vector2.zero;
        barRt.sizeDelta = new Vector2(0f, 8f);
        Image barImg = accentBar.GetComponent<Image>();
        barImg.color = accentColor;
        barImg.raycastTarget = false;

        // Vòng tròn trưng bày Icon tượng trưng của kỹ năng (136 x 136)
        GameObject iconFrameGo = new GameObject("SkillIconFrame", typeof(RectTransform), typeof(Image));
        iconFrameGo.transform.SetParent(innerBg.transform, false);
        RectTransform ifRt = iconFrameGo.GetComponent<RectTransform>();
        ifRt.anchorMin = new Vector2(0.5f, 1f);
        ifRt.anchorMax = new Vector2(0.5f, 1f);
        ifRt.pivot = new Vector2(0.5f, 1f);
        ifRt.anchoredPosition = new Vector2(0f, -24f);
        ifRt.sizeDelta = new Vector2(138f, 138f);
        Image ifImg = iconFrameGo.GetComponent<Image>();
        if (UIThemeAssets.BtnCircleFrame != null)
        {
            ifImg.sprite = UIThemeAssets.BtnCircleFrame;
            ifImg.preserveAspect = true;
        }
        ifImg.color = Color.white;
        ifImg.raycastTarget = false;

        GameObject iconGo = new GameObject("SkillIcon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(iconFrameGo.transform, false);
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.14f, 0.14f);
        iconRt.anchorMax = new Vector2(0.86f, 0.86f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        Image iconImg = iconGo.GetComponent<Image>();
        iconImg.sprite = skillIcon;
        iconImg.preserveAspect = true;
        iconImg.color = Color.white;
        iconImg.raycastTarget = false;

        // Tên chiêu ngay dưới icon
        CreateCardText(
            "TitleLabel",
            innerBg.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(16f, -214f),
            new Vector2(-16f, -172f),
            skillTitle,
            30f,
            FontStyles.Bold,
            Color.white,
            font
        );

        // Mô tả ngắn gọn rõ ràng
        CreateCardText(
            "DescLabel",
            innerBg.transform,
            new Vector2(0f, 0.22f),
            new Vector2(1f, 0.48f),
            new Vector2(18f, 0f),
            new Vector2(-18f, 0f),
            skillDesc,
            21f,
            FontStyles.Normal,
            new Color(0.84f, 0.90f, 0.98f, 1f),
            font
        );

        // Nhãn hồi chiêu
        CreateCardText(
            "CooldownLabel",
            innerBg.transform,
            new Vector2(0f, 0.12f),
            new Vector2(1f, 0.22f),
            new Vector2(16f, 0f),
            new Vector2(-16f, 0f),
            cooldownLabel,
            21f,
            FontStyles.Bold,
            accentColor,
            font
        );

        // Nhãn DA CHON ở đáy thẻ
        GameObject tagGo = new GameObject("SelectedBadge", typeof(RectTransform), typeof(Image));
        tagGo.transform.SetParent(innerBg.transform, false);
        RectTransform tagRt = tagGo.GetComponent<RectTransform>();
        tagRt.anchorMin = new Vector2(0.20f, 0.03f);
        tagRt.anchorMax = new Vector2(0.80f, 0.11f);
        tagRt.offsetMin = Vector2.zero;
        tagRt.offsetMax = Vector2.zero;
        Image tagImg = tagGo.GetComponent<Image>();
        tagImg.color = UIThemeAssets.GoldAccent;
        tagImg.raycastTarget = false;

        CreateCardText(
            "BadgeText",
            tagGo.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            "DA CHON",
            19f,
            FontStyles.Bold,
            new Color(0.08f, 0.09f, 0.12f, 1f),
            font
        );

        styledSelectedTag = tagGo;
        SetSelectedVisual(currentSelected == this);
    }

    static void CreateCardText(
        string name,
        Transform parent,
        Vector2 aMin,
        Vector2 aMax,
        Vector2 oMin,
        Vector2 oMax,
        string content,
        float fontSize,
        FontStyles style,
        Color color,
        TMP_FontAsset font
    )
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = oMin;
        rt.offsetMax = oMax;

        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        if (font != null) txt.font = font;
        txt.text = content;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = color;
        txt.raycastTarget = false;
    }

    void SetSelectedVisual(bool selected)
    {
        if (selectedBorder != null)
            selectedBorder.SetActive(selected);

        if (cardOutlineImage != null)
            cardOutlineImage.gameObject.SetActive(selected);

        if (styledSelectedTag != null)
            styledSelectedTag.SetActive(selected);
    }

    public void SelectSkill()
    {
        PlayerAbilityController ability =
            FindLocalAbility();

        if (ability == null)
            return;

        ability.SetSelectedSkill(
            skillType
        );

        if (currentSelected != null && currentSelected != this)
        {
            currentSelected.SetSelectedVisual(false);
        }

        SetSelectedVisual(true);
        currentSelected = this;
    }

    PlayerAbilityController FindLocalAbility()
    {
        PlayerAbilityController[] players =
            FindObjectsByType<PlayerAbilityController>(
                FindObjectsSortMode.None
            );

        foreach (PlayerAbilityController ability in players)
        {
            if (ability == null)
                continue;

            NetworkObject obj =
                ability.GetComponent<NetworkObject>();

            if (
                obj != null &&
                obj.HasInputAuthority
            )
            {
                return ability;
            }
        }

        return null;
    }
}
