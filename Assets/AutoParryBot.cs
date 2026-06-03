using Fusion;
using UnityEngine;

public class AutoParryBot : NetworkBehaviour
{
    [Header("Parry")]
    public float parryRange = 7f;

    public float parryCooldown = 0.25f;

    public float reactionTime = 0.08f;

    float cooldownTimer;

    BallController ball;

    PlayerHealth hp;

    // ================= SPAWN =================

    public override void Spawned()
    {
        gameObject.tag = "Player";

        hp =
            GetComponent<PlayerHealth>();

        FindBall();
    }

    // ================= NETWORK UPDATE =================

    public override void FixedUpdateNetwork()
    {
        // chỉ host xử lý bot
        if (!Object.HasStateAuthority)
            return;

        if (ball == null)
        {
            FindBall();
            return;
        }

        if (hp == null)
            return;

        if (hp.IsDead)
            return;

        cooldownTimer -= Runner.DeltaTime;

        Transform target =
            ball.GetTarget();

        if (target == null)
            return;

        // check target bằng NetworkObject
        NetworkObject myNet =
            GetComponent<NetworkObject>();

        NetworkObject targetNet =
            target.GetComponent<NetworkObject>();

        if (
            myNet == null ||
            targetNet == null
        )
        {
            return;
        }

        // ball không target bot
        if (
            targetNet.Id != myNet.Id
        )
        {
            return;
        }

        float dist =
            Vector3.Distance(
                transform.position,
                ball.transform.position
            );

        // khoảng cách đủ gần
        if (
            dist <= parryRange &&
            cooldownTimer <= 0f
        )
        {
            Runner.StartCoroutine(
                ParryDelay()
            );

            cooldownTimer =
                parryCooldown;
        }
    }

    // ================= DELAY =================

    System.Collections.IEnumerator ParryDelay()
    {
        yield return new WaitForSeconds(
            reactionTime
        );

        if (ball == null)
            yield break;

        if (hp == null)
            yield break;

        if (hp.IsDead)
            yield break;

        ball.Parry(
            transform,
            true
        );

        Debug.Log("BOT PARRY");
    }

    // ================= FIND BALL =================

    void FindBall()
    {
        if (BallController.instance != null)
        {
            ball =
                BallController.instance;
        }
        else
        {
            ball =
                FindFirstObjectByType
                <BallController>();
        }
    }
}