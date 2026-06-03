using Fusion;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Health")]
    public float maxHealth = 3f;

    [Networked]
    public float CurrentHealth { get; set; }

    [Networked]
    public bool IsDead { get; set; }

    [Header("References")]
    public PlayerMovement player;

    public Rigidbody rb;

    public GameUI gameUI;

    [Header("Death Force")]
    public float knockbackForce = 8f;

    public float upForce = 3f;

    // ================= SPAWN =================

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
        }

        if (player == null)
            player = GetComponent<PlayerMovement>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb != null)
            rb.isKinematic = true;

        if (Object.HasInputAuthority)
        {
            gameUI =
                FindFirstObjectByType<GameUI>();

            UpdateUI();
        }
    }

    // ================= RENDER =================

    public override void Render()
    {
        if (Object.HasInputAuthority)
        {
            UpdateUI();
        }
    }

    // ================= DAMAGE =================

    public void TakeDamage(
        float dmg,
        Vector3 hitDirection)
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsDead)
            return;

        CurrentHealth -= dmg;

        CurrentHealth =
            Mathf.Clamp(
                CurrentHealth,
                0,
                maxHealth
            );

        RPC_PlayHit();

        if (CurrentHealth <= 0)
        {
            Die(hitDirection);
        }
    }

    // ================= HIT =================

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    void RPC_PlayHit()
    {
        if (player != null)
        {
            player.canMove = false;

            if (player.animator != null)
            {
                player.animator
                    .SetTrigger("Hit");
            }

            Invoke(
                nameof(EnableMove),
                0.4f
            );
        }
    }

    void EnableMove()
    {
        if (!IsDead &&
            player != null)
        {
            player.canMove = true;
        }
    }

    // ================= UI =================

    void UpdateUI()
    {
        if (!Object.HasInputAuthority)
            return;

        if (gameUI != null)
        {
            gameUI.UpdateHealth(
                CurrentHealth / maxHealth
            );
        }
    }

    // ================= DIE =================

    void Die(Vector3 hitDirection)
    {
        if (IsDead)
            return;

        IsDead = true;

        Debug.Log("PLAYER DIED");

        RPC_Die(hitDirection);

        if (BallController.instance != null)
        {
            BallController.instance
                .SwitchTarget(transform);
        }
    }

    // ================= RPC DIE =================

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    void RPC_Die(Vector3 hitDirection)
    {
        if (player != null)
        {
            player.enabled = false;
        }

        CharacterController cc =
            GetComponent<CharacterController>();

        if (cc != null)
        {
            cc.enabled = false;
        }

        if (rb != null)
        {
            rb.isKinematic = false;

            Vector3 force =
                hitDirection.normalized *
                knockbackForce +
                Vector3.up * upForce;

            rb.AddForce(
                force,
                ForceMode.Impulse
            );

            rb.AddTorque(
                transform.right * 2f,
                ForceMode.Impulse
            );

            Invoke(
                nameof(FreezeBody),
                1f
            );
        }
    }

    // ================= FREEZE =================

    void FreezeBody()
    {
        if (rb == null)
            return;

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        rb.constraints =
            RigidbodyConstraints.FreezeAll;

        transform.rotation =
            Quaternion.Euler(
                90f,
                transform.eulerAngles.y,
                0f
            );
    }

    // ================= HEAL =================

    public void Heal(float value)
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsDead)
            return;

        CurrentHealth += value;

        CurrentHealth =
            Mathf.Clamp(
                CurrentHealth,
                0,
                maxHealth
            );
    }
}