using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class SkillButtonUI : MonoBehaviour
{
    public Button button;

    public Image cooldown;

    PlayerAbilityController ability;

    void Update()
    {
        // tìm local player nếu chưa có
        if (ability == null)
        {
            FindLocalPlayer();
            return;
        }

        // update cooldown UI
        if (cooldown != null)
        {
            cooldown.fillAmount =
                ability.GetSkillCooldownNormalized();
        }

        // enable/disable button
        if (button != null)
        {
            button.interactable =
                ability.SkillReady();
        }
    }

    void FindLocalPlayer()
    {
        PlayerAbilityController[] players =
            FindObjectsByType<PlayerAbilityController>(
                FindObjectsSortMode.None
            );

        foreach (var p in players)
        {
            NetworkObject obj =
                p.GetComponent<NetworkObject>();

            if (obj != null &&
                obj.HasInputAuthority)
            {
                ability = p;

                break;
            }
        }
    }

    public void OnClickSkill()
    {
        if (ability != null)
        {
            ability.TryUseSkill();
        }
    }
}