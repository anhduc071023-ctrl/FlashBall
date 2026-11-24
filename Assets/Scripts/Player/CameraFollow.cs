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

    public float smooth = 12f;

    public float verticalFollowSmooth = 6.5f;

    public float rotationSmooth = 12f;

    Vector3 smoothedTargetPos;
    Vector3 smoothedCamDir = Vector3.back;
    bool initialized;

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
                if (p != null && p.Object != null && p.Object.HasInputAuthority)
                {
                    target = p.transform;
                    initialized = false;
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
                BallController.instance != null
                ? BallController.instance
                : FindFirstObjectByType<BallController>();

            if (b != null)
            {
                ball = b.transform;
            }
        }

        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        // Lọc mượt vị trí mục tiêu riêng trục XZ và trục Y (loại bỏ hoàn toàn giật hình khi nhảy!)
        if (!initialized)
        {
            smoothedTargetPos = target.position;
            initialized = true;
        }
        else
        {
            float xzAlpha = 1f - Mathf.Exp(-18f * dt);
            float yAlpha = 1f - Mathf.Exp(-verticalFollowSmooth * dt);
            smoothedTargetPos.x = Mathf.Lerp(smoothedTargetPos.x, target.position.x, xzAlpha);
            smoothedTargetPos.z = Mathf.Lerp(smoothedTargetPos.z, target.position.z, xzAlpha);
            smoothedTargetPos.y = Mathf.Lerp(smoothedTargetPos.y, target.position.y, yAlpha);
        }

        // ================= CAMERA DIRECTION =================

        Vector3 targetCamDir = smoothedCamDir;

        if (ball != null && ball.gameObject.activeInHierarchy)
        {
            // Tính hướng ngang trước rồi MỚI chuẩn hóa (sửa lỗi zoom giật khi nhảy cao!)
            Vector3 flatToBall = ball.position - smoothedTargetPos;
            flatToBall.y = 0f;

            if (flatToBall.sqrMagnitude > 0.04f)
            {
                targetCamDir = -flatToBall.normalized;
            }
        }
        else
        {
            targetCamDir = Vector3.back;
        }

        float dirAlpha = 1f - Mathf.Exp(-smooth * dt);
        smoothedCamDir = Vector3.Slerp(smoothedCamDir, targetCamDir, dirAlpha).normalized;

        // ================= CAMERA POSITION =================

        Vector3 desiredPos =
            smoothedTargetPos +
            smoothedCamDir * distance +
            Vector3.up * height;

        float posAlpha = 1f - Mathf.Exp(-smooth * dt);
        transform.position = Vector3.Lerp(transform.position, desiredPos, posAlpha);

        // ================= LOOK POINT =================

        Vector3 lookPoint;

        if (ball != null && ball.gameObject.activeInHierarchy)
        {
            lookPoint = ball.position;
        }
        else if (mapCenter != null)
        {
            lookPoint = mapCenter.position;
        }
        else
        {
            lookPoint = smoothedTargetPos;
        }

        // ================= ROTATION =================

        Vector3 lookDir = lookPoint - transform.position;

        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized);
            float rotAlpha = 1f - Mathf.Exp(-rotationSmooth * dt);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotAlpha);
        }
    }
}
