using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public struct NetworkInputData : INetworkInput
{
    public Vector3 moveDirection;
    public NetworkBool jumpPressed;
}

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    public static PlayerMovement Local;

    [Header("Movement")]
    public float moveSpeed = 5f;

    public float rotateSpeed = 10f;

    [Header("Gravity & Jump")]
    public float gravity = -26f;

    public float jumpForce = 10.5f;

    public float jumpCooldown = 0.22f;

    [Header("References")]
    public CharacterController controller;

    public Animator animator;

    public Transform cameraTransform;

    [Header("State")]
    public bool canMove = true;

    [HideInInspector]
    public bool isDashing;

    [Networked]
    public float NetSpeed { get; set; }

    [Networked]
    public NetworkBool IsGroundedNet { get; set; }

    [Networked]
    public int JumpTriggerCount { get; set; }

    [Networked]
    float NetVerticalVelocity { get; set; }

    [Networked]
    float NetCoyoteTimer { get; set; }

    [Networked]
    float NetJumpCooldownTimer { get; set; }

    // input
    Vector2 moveInput;
    bool pendingJumpInput;
    int lastRenderedJumpCount;

    // movement
    Vector3 currentMove;

    PlayerAbilityController abilityController;

    public Vector3 MoveDirection
    {
        get { return currentMove; }
    }

    // ================= SPAWN =================

    public override void Spawned()
    {
        if (controller == null)
        {
            controller =
                GetComponent<CharacterController>();
        }

        if (Object.HasStateAuthority)
        {
            SnapToGround();
        }
        else if (controller != null && controller.enabled)
        {
            controller.enabled = false;
            controller.enabled = true;
        }

        pendingJumpInput = false;
        lastRenderedJumpCount = 0;

        if (Object.HasStateAuthority)
        {
            NetVerticalVelocity = 0f;
            NetCoyoteTimer = 0.12f;
            NetJumpCooldownTimer = 0f;
            IsGroundedNet = true;
            JumpTriggerCount = 0;
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // local player mới lấy camera
        if (Object.HasInputAuthority)
        {
            Local = this;

            Camera mainCam = Camera.main;

            if (mainCam != null)
            {
                cameraTransform =
                    mainCam.transform;
            }
        }
    }

    void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * 5f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 25f);

        bool foundGround = false;
        float bestY = float.MinValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger)
                continue;

            if (hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.collider.GetComponentInParent<BallController>() != null)
                continue;

            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                foundGround = true;
            }
        }

        bool wasEnabled = controller != null && controller.enabled;
        if (controller != null)
            controller.enabled = false;

        if (foundGround)
        {
            Vector3 p = transform.position;
            p.y = bestY + 0.02f;
            transform.position = p;
        }

        if (controller != null && wasEnabled)
            controller.enabled = true;
    }

    // ================= INPUT =================

    public void SetMoveInput(Vector2 input)
    {
        // chỉ local player nhận input
        if (Object == null || !Object.HasInputAuthority)
            return;

        moveInput = input;
    }

    public void RequestJump()
    {
        if (Object == null || !Object.HasInputAuthority)
            return;

        pendingJumpInput = true;
    }

    public bool ConsumeJumpInput()
    {
        if (Object == null || !Object.HasInputAuthority)
            return false;

        bool jump = pendingJumpInput;
        pendingJumpInput = false;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.spaceKey.isPressed)
        {
            jump = true;
        }

        return jump;
    }

    void Update()
    {
        if (Object != null && Object.IsValid && Object.HasInputAuthority)
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame)
            {
                pendingJumpInput = true;
            }
        }
    }

    public Vector3 GetDesiredWorldMoveDirection()
    {
        if (Object == null || !Object.HasInputAuthority)
            return Vector3.zero;

        Vector2 input = moveInput;

        Keyboard kb = Keyboard.current;

        if (kb != null)
        {
            float kx = 0f;
            float ky = 0f;

            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) kx -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) kx += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) ky -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) ky += 1f;

            Vector2 kbInput = new Vector2(kx, ky);

            if (kbInput.sqrMagnitude > input.sqrMagnitude)
            {
                input = kbInput;
            }
        }

        if (input.sqrMagnitude < 0.001f)
            return Vector3.zero;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        Vector3 camForward =
            cameraTransform != null
            ? cameraTransform.forward
            : Vector3.forward;

        Vector3 camRight =
            cameraTransform != null
            ? cameraTransform.right
            : Vector3.right;

        camForward.y = 0f;
        camRight.y = 0f;

        if (camForward.sqrMagnitude > 0.0001f)
            camForward.Normalize();
        else
            camForward = Vector3.forward;

        if (camRight.sqrMagnitude > 0.0001f)
            camRight.Normalize();
        else
            camRight = Vector3.right;

        Vector3 dir =
            camForward * input.y +
            camRight * input.x;

        return Vector3.ClampMagnitude(dir, 1f);
    }

    Vector3 botMoveDirection;
    bool hasBotMoveOverride;

    public void SetBotMoveDirection(Vector3 worldDir)
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        hasBotMoveOverride = true;
        worldDir.y = 0f;
        botMoveDirection = Vector3.ClampMagnitude(worldDir, 1f);
    }

    // ================= NETWORK UPDATE =================

    public override void FixedUpdateNetwork()
    {
        if (Object == null)
            return;

        if (!Object.HasStateAuthority && !Object.HasInputAuthority)
            return;

        bool wantsJump = false;

        if (GetInput(out NetworkInputData inputData))
        {
            currentMove =
                Vector3.ClampMagnitude(
                    inputData.moveDirection,
                    1f
                );

            currentMove.y = 0f;
            wantsJump = inputData.jumpPressed;
        }
        else if (Object.HasInputAuthority)
        {
            currentMove =
                GetDesiredWorldMoveDirection();
            wantsJump = ConsumeJumpInput();
        }
        else if (Object.HasStateAuthority && hasBotMoveOverride)
        {
            currentMove = botMoveDirection;
        }
        else
        {
            currentMove = Vector3.zero;
        }

        Move(wantsJump);

        Rotate();

        if (Object.HasStateAuthority)
        {
            NetSpeed =
                (canMove && !isDashing)
                ? currentMove.magnitude
                : 0f;

            IsGroundedNet = controller != null && controller.isGrounded;
        }
    }

    public override void Render()
    {
        HandleAnimation();
    }

    // ================= MOVE =================

    void Move(bool wantsJump)
    {
        if (controller == null || !controller.enabled)
            return;

        if (
            !Object.HasStateAuthority &&
            Runner != null &&
            Runner.IsResimulation
        )
        {
            Physics.SyncTransforms();
        }

        float dt = Runner.DeltaTime;

        float vertVel = NetVerticalVelocity;
        float coyote = NetCoyoteTimer;
        float jumpCd = NetJumpCooldownTimer;

        if (jumpCd > 0f)
        {
            jumpCd = Mathf.Max(0f, jumpCd - dt);
        }

        bool grounded = controller.isGrounded;
        if (grounded)
        {
            coyote = 0.12f;
            if (vertVel < 0f)
            {
                vertVel = -2f;
            }
        }
        else
        {
            coyote = Mathf.Max(0f, coyote - dt);
            vertVel += gravity * dt;
        }

        // Nhảy mượt không bị giật rollback (đồng bộ hoàn toàn qua Networked state) và không khóa parry!
        if (wantsJump && canMove && !isDashing && coyote > 0f && jumpCd <= 0f)
        {
            vertVel = Mathf.Max(jumpForce, 10.2f);
            coyote = 0f;
            jumpCd = jumpCooldown;

            if (Object.HasStateAuthority)
            {
                IsGroundedNet = false;
                JumpTriggerCount++;
            }
        }

        NetVerticalVelocity = vertVel;
        NetCoyoteTimer = coyote;
        NetJumpCooldownTimer = jumpCd;

        if (!canMove || isDashing)
        {
            currentMove = Vector3.zero;

            if (!isDashing)
            {
                controller.Move(
                    Vector3.up *
                    vertVel *
                    dt
                );
            }

            return;
        }

        float speedMul = 1f;
        if (abilityController == null)
        {
            abilityController = GetComponent<PlayerAbilityController>();
        }
        if (abilityController != null && (abilityController.IsInvisibleNet || abilityController.isInvisible))
        {
            speedMul = Mathf.Max(1.1f, abilityController.invisibleSpeedMultiplier);
        }

        Vector3 velocity = currentMove * (moveSpeed * speedMul);
        velocity.y = vertVel;

        controller.Move(
            velocity *
            dt
        );

        EnforceArenaBoundary();
    }

    void EnforceArenaBoundary()
    {
        Vector3 cur = transform.position;
        Vector3 clamped = ArenaMapManager.ClampPositionToArena(cur);
        Vector3 diff = clamped - cur;
        diff.y = 0f;
        if (diff.sqrMagnitude > 0.00001f)
        {
            if (controller != null && controller.enabled)
            {
                controller.Move(diff);
            }
            else
            {
                transform.position = clamped;
            }
        }
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
            Object != null && Object.IsValid
            ? NetSpeed
            : currentMove.magnitude;

        if (!canMove)
        {
            speedPercent = 0f;
        }

        animator.SetFloat(
            "Speed",
            speedPercent,
            0.1f,
            Time.deltaTime
        );

        bool grounded =
            Object != null && Object.IsValid
            ? (bool)IsGroundedNet
            : (controller == null || controller.isGrounded);

        animator.SetBool("IsGrounded", grounded);

        if (Object != null && Object.IsValid && JumpTriggerCount != lastRenderedJumpCount)
        {
            lastRenderedJumpCount = JumpTriggerCount;
            animator.ResetTrigger("Jump");
            animator.SetTrigger("Jump");
            if (Object.HasInputAuthority)
            {
                GameAudioManager.PlaySFX(GameSfxType.Jump, 0.65f);
            }
        }
    }
}