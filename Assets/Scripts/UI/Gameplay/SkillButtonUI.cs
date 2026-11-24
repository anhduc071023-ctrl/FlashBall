using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class SkillButtonUI : MonoBehaviour
{
    [Header("UI")]
    public Button button;
    public Image cooldown;

    PlayerAbilityController ability;
    Image dynamicIconImage;
    SkillType lastRenderedSkill = SkillType.None;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickSkill);
        }
    }

    public void SetDynamicIconImage(Image iconImg)
    {
        dynamicIconImage = iconImg;
        lastRenderedSkill = (SkillType)(-1);
        RefreshSkillIcon();
    }

    void Update()
    {
        if (ability == null)
        {
            FindLocalPlayer();
            return;
        }

        RefreshSkillIcon();

        if (cooldown != null)
        {
            cooldown.fillAmount =
                ability.GetSkillCooldownNormalized();
        }

        if (button != null)
        {
            button.interactable =
                ability.SkillReady();
        }
    }

    void RefreshSkillIcon()
    {
        if (ability == null || dynamicIconImage == null)
            return;

        if (ability.currentSkill == lastRenderedSkill)
            return;

        lastRenderedSkill = ability.currentSkill;
        Sprite sp = UIThemeAssets.GetSkillIcon(ability.currentSkill);
        if (sp != null)
        {
            dynamicIconImage.sprite = sp;
            dynamicIconImage.enabled = true;
        }
    }

    void FindLocalPlayer()
    {
        PlayerAbilityController[] players =
            FindObjectsByType<PlayerAbilityController>(
                FindObjectsSortMode.None);

        foreach (var p in players)
        {
            if (p.Object != null &&
                p.Object.HasInputAuthority)
            {
                ability = p;
                lastRenderedSkill = (SkillType)(-1);
                RefreshSkillIcon();
                return;
            }
        }
    }

    void OnClickSkill()
    {
        if (ability == null)
            return;

        ability.TryUseSkill();
    }
}
