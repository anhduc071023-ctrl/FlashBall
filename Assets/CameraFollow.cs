using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform target;

    public Transform ball;

    public Transform mapCenter;

    [Header("Camera")]
    public float distance = 8f;

    public float height = 5f;

    public float smooth = 10f;

    public float rotationSmooth = 10f;

    void LateUpdate()
    {
        // ================= FIND PLAYER =================

        if (target == null)
        {
            PlayerMovement[] players =
                FindObjectsByType<PlayerMovement>(
                    FindObjectsSortMode.None
                );

            foreach (var p in players)
            {
                if (p.Object.HasInputAuthority)
                {
                    target = p.transform;
                    break;
                }
            }

            if (target == null)
                return;
        }

        // ================= FIND BALL =================

        if (ball == null)
        {
            BallController b =
                FindFirstObjectByType<BallController>();

            if (b != null)
            {
                ball = b.transform;
            }
        }

        // ================= CAMERA DIRECTION =================

        Vector3 camDir;

        if (
            ball != null &&
            ball.gameObject.activeInHierarchy
        )
        {
            // hướng từ player -> ball
            Vector3 dir =
                (ball.position - target.position)
                .normalized;

            dir.y = 0f;

            // camera nằm phía sau player
            camDir = -dir;
        }
        else
        {
            camDir = Vector3.back;
        }

        // ================= CAMERA POSITION =================

        Vector3 desiredPos =
            target.position +
            camDir * distance +
            Vector3.up * height;

        transform.position =
            Vector3.Lerp(
                transform.position,
                desiredPos,
                smooth * Time.deltaTime
            );

        // ================= LOOK POINT =================

        Vector3 lookPoint;

        if (
            ball != null &&
            ball.gameObject.activeInHierarchy
        )
        {
            lookPoint = ball.position;
        }
        else if (mapCenter != null)
        {
            lookPoint = mapCenter.position;
        }
        else
        {
            lookPoint = target.position;
        }

        // ================= ROTATION =================

        Vector3 lookDir =
            lookPoint - transform.position;

        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot =
                Quaternion.LookRotation(
                    lookDir.normalized
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    rotationSmooth *
                    Time.deltaTime
                );
        }
    }
}