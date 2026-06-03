using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class ParryChange : MonoBehaviour
{
    [Header("UI")]
    public Image chargeCircle;

    [Header("FX")]
    public TrailRenderer trail;

    [Header("Settings")]
    public float maxChargeTime = 1f;

    public float attackDuration = 0.8f;

    public float parryRange = 5f;

    PlayerMovement player;

    PlayerAbilityController ability;

    GameUI gameUI;

    BallController ball;

    float charge;

    float attackTimer;

    bool holding;

    bool isAttacking;

    bool isPowerParry;

    // ================= START =================

    void Start()
    {
        if (chargeCircle != null)
        {
            chargeCircle.fillAmount = 0f;
        }

        if (trail != null)
        {
            trail.emitting = false;
        }
    }

    // ================= UPDATE =================

    void Update()
    {
        // tìm local player
        if (player == null)
        {
            FindLocalPlayer();
            return;
        }

        // tìm ball
        if (ball == null)
        {
            ball = FindFirstObjectByType<BallController>();
        }

        HandleAttackLock();

        HandleCharge();
    }

    // ================= FIND PLAYER =================

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

                ability =
                    p.GetComponent<PlayerAbilityController>();

                gameUI =
                    FindFirstObjectByType<GameUI>();

                break;
            }
        }
    }

    // ================= ATTACK TIMER =================

    void HandleAttackLock()
    {
        if (!isAttacking)
            return;

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            isAttacking = false;
        }
    }

    // ================= CHARGE =================

    void HandleCharge()
    {
        if (!holding || isAttacking)
            return;

        charge += Time.deltaTime;

        float t =
            Mathf.Clamp01(
                charge / maxChargeTime
            );

        if (chargeCircle != null)
        {
            chargeCircle.fillAmount = t;
        }
    }

    // ================= START CHARGE =================

    public void StartCharge()
    {
        if (player == null)
            return;

        if (isAttacking)
            return;

        holding = true;

        charge = 0f;
    }

    // ================= RELEASE =================

    public void ReleaseCharge()
    {
        if (player == null)
            return;

        if (isAttacking)
            return;

        holding = false;

        isPowerParry =
            charge >= maxChargeTime;

        StartAttack(isPowerParry);

        Invoke(nameof(TryParry), 0.05f);

        ResetUI();
    }

    // ================= ATTACK =================

    void StartAttack(bool isPower)
    {
        isAttacking = true;

        attackTimer = attackDuration;

        if (trail != null)
        {
            trail.Clear();

            trail.emitting = true;

            trail.time =
                isPower ? 0.4f : 0.35f;

            Invoke(
                nameof(StopTrail),
                trail.time
            );
        }

        if (player.animator != null)
        {
            player.animator.ResetTrigger("Attack1");

            player.animator.ResetTrigger("Attack2");

            if (isPower)
            {
                player.animator.SetTrigger("Attack2");
            }
            else
            {
                player.animator.SetTrigger("Attack1");
            }
        }
    }

    void StopTrail()
    {
        if (trail != null)
        {
            trail.emitting = false;
        }
    }

    // ================= PARRY =================

    void TryParry()
    {
        if (ball == null)
            return;

        float dist =
            Vector3.Distance(
                player.transform.position,
                ball.transform.position
            );

        if (dist > parryRange)
        {
            Debug.Log("MISS");

            return;
        }

        ball.Parry(
            player.transform,
            isPowerParry
        );

        if (gameUI != null)
        {
            if (isPowerParry)
            {
                gameUI.AddScorePower();
            }
            else
            {
                gameUI.AddScoreNormal();
            }
        }

        Debug.Log(
            isPowerParry
            ? "POWER PARRY"
            : "NORMAL PARRY"
        );
    }

    // ================= UI =================

    void ResetUI()
    {
        charge = 0f;

        if (chargeCircle != null)
        {
            chargeCircle.fillAmount = 0f;
        }
    }
}