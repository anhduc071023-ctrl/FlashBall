using UnityEngine;
using Fusion;

public class JoystickInput : MonoBehaviour
{
    public FixedJoystick joystick;

    PlayerMovement player;

    void Update()
    {
        // chưa có joystick
        if (joystick == null)
            return;

        // tìm local player
        if (player == null)
        {
            FindLocalPlayer();
            return;
        }

        Vector2 input =
            new Vector2(
                joystick.Horizontal,
                joystick.Vertical
            );

        player.SetMoveInput(input);
    }

    void FindLocalPlayer()
    {
        PlayerMovement[] players =
            FindObjectsByType<PlayerMovement>(
                FindObjectsSortMode.None
            );

        foreach (var p in players)
        {
            NetworkObject obj =
                p.GetComponent<NetworkObject>();

            if (obj != null &&
                obj.HasInputAuthority)
            {
                player = p;
                break;
            }
        }
    }
}