using UnityEngine;
using UnityEngine.EventSystems;

public class JumpButtonUI : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        OnClickJump();
    }

    public void OnClickJump()
    {
        if (PlayerMovement.Local != null)
        {
            PlayerMovement.Local.RequestJump();
        }
    }
}
