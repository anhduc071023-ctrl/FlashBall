using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    public float rotateSpeed = 10f;

    [Header("References")]
    public CharacterController controller;

    public Animator animator;

    public Transform cameraTransform;

    [Header("State")]
    public bool canMove = true;

    [HideInInspector]
    public bool isDashing;

    // input
    Vector2 moveInput;

    // movement
    Vector3 currentMove;

    public Vector3 MoveDirection
    {
        get { return currentMove; }
    }

    // ================= SPAWN =================

    public override void Spawned()
    {
        // local player mới lấy camera
        if (Object.HasInputAuthority)
        {
            Camera mainCam = Camera.main;

            if (mainCam != null)
            {
                cameraTransform =
                    mainCam.transform;
            }
        }
    }

    // ================= INPUT =================

    public void SetMoveInput(Vector2 input)
    {
        // chỉ local player nhận input
        if (!Object.HasInputAuthority)
            return;

        moveInput = input;
    }

    // ================= NETWORK UPDATE =================

    public override void FixedUpdateNetwork()
    {
        // chỉ local player control
        if (!Object.HasInputAuthority)
            return;

        Move();

        Rotate();

        HandleAnimation();
    }

    // ================= MOVE =================

    void Move()
    {
        if (!canMove || isDashing)
        {
            currentMove = Vector3.zero;

            return;
        }

        if (cameraTransform == null)
            return;

        Vector3 camForward =
            cameraTransform.forward;

        Vector3 camRight =
            cameraTransform.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        currentMove =
            camForward * moveInput.y +
            camRight * moveInput.x;

        currentMove =
            Vector3.ClampMagnitude(
                currentMove,
                1f
            );

        controller.Move(
            currentMove *
            moveSpeed *
            Runner.DeltaTime
        );
    }

    // ================= ROTATE =================

    void Rotate()
    {
        if (!canMove)
            return;

        if (currentMove.magnitude > 0.1f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(currentMove);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed *
                    Runner.DeltaTime
                );
        }
    }

    // ================= ANIMATION =================

    void HandleAnimation()
    {
        if (animator == null)
            return;

        float speedPercent =
            currentMove.magnitude;

        if (!canMove)
        {
            speedPercent = 0;
        }

        animator.SetFloat(
            "Speed",
            speedPercent,
            0.1f,
            Runner.DeltaTime
        );
    }
}