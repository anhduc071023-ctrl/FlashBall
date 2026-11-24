using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class DashButtonUI : MonoBehaviour
{
    public Button button;

    public Image cooldown;

    PlayerAbilityController ability;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveListener(
                OnClickDash
            );

            button.onClick.AddListener(
                OnClickDash
            );
        }
    }

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
                ability.GetDashCooldownNormalized();
        }

        // enable/disable button
        if (button != null)
        {
            button.interactable =
                ability.DashReady();
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

    public void OnClickDash()
    {
        if (ability != null)
        {
            ability.TryDash();
        }
    }
}
