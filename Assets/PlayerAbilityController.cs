using UnityEngine;
using System.Collections;
using Fusion;

public enum SkillType
{
    None,
    CloneBall,
    Invisible,
    Tornado
}

public class PlayerAbilityController : NetworkBehaviour
{
    [Header("References")]
    public PlayerMovement movement;

    [Header("Dash")]
    public float dashForce = 30f;
    public float dashCooldown = 1.5f;

    float dashTimer;

    [Header("Skill")]
    public SkillType currentSkill;

    float skillTimer;
    float skillCooldown;

    [HideInInspector]
    public bool isInvisible;

    [HideInInspector]
    public bool isTornadoActive;

    CharacterController controller;

    bool isDashing;

    BallController ball;

    // ================= INIT =================

    void Start()
    {
        controller =
            GetComponent<CharacterController>();

        ball = BallController.instance;
    }

    // ================= FIXED UPDATE =================

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority)
            return;

        if (dashTimer > 0)
            dashTimer -= Runner.DeltaTime;

        if (skillTimer > 0)
            skillTimer -= Runner.DeltaTime;
    }

    // ================= DASH =================

    IEnumerator DashRoutine(Vector3 dir)
    {
        isDashing = true;

        movement.isDashing = true;

        float dashTime = 0.15f;

        float timer = 0;

        while (timer < dashTime)
        {
            float t = timer / dashTime;

            float speed =
                Mathf.Lerp(
                    dashForce,
                    0,
                    t
                );

            controller.Move(
                dir *
                speed *
                Time.deltaTime
            );

            timer += Time.deltaTime;

            yield return null;
        }

        movement.isDashing = false;

        isDashing = false;
    }

    public void TryDash()
    {
        if (!Object.HasInputAuthority)
            return;

        if (dashTimer > 0 || isDashing)
            return;

        Vector3 dir =
            movement.MoveDirection;

        if (dir.magnitude < 0.1f)
        {
            dir = transform.forward;
        }

        StartCoroutine(
            DashRoutine(dir.normalized)
        );

        dashTimer = dashCooldown;
    }

    // ================= SKILL =================

    public void TryUseSkill()
    {
        if (!Object.HasInputAuthority)
            return;

        if (skillTimer > 0)
            return;

        ActivateSkill();
    }

    void ActivateSkill()
    {
        switch (currentSkill)
        {
            case SkillType.CloneBall:

                skillCooldown = 10f;
                skillTimer = skillCooldown;

                RPC_CloneBall();

                break;

            case SkillType.Invisible:

                skillCooldown = 20f;
                skillTimer = skillCooldown;

                StartCoroutine(Invisible());

                break;

            case SkillType.Tornado:

                skillCooldown = 20f;
                skillTimer = skillCooldown;

                RPC_Tornado();

                break;
        }
    }

    // ================= RPC CLONE =================

    [Rpc(RpcSources.InputAuthority,
        RpcTargets.StateAuthority)]
    void RPC_CloneBall()
    {
        SpawnCloneBall();
    }

    // ================= RPC TORNADO =================

    [Rpc(RpcSources.InputAuthority,
        RpcTargets.StateAuthority)]
    void RPC_Tornado()
    {
        StartCoroutine(Tornado());
    }

    // ================= CLONE BALL =================

    void SpawnCloneBall()
    {
        if (ball == null)
            return;

        Rigidbody realRb =
            ball.GetComponent<Rigidbody>();

        if (realRb == null)
            return;

        Vector3 offset =
            Random.insideUnitSphere * 2f;

        offset.y = 0;

        // clone local fake
        GameObject fakeBall =
            Instantiate(
                ball.gameObject,
                ball.transform.position + offset,
                Quaternion.identity
            );

        Destroy(fakeBall, 5f);

        Rigidbody rbFake =
            fakeBall.GetComponent<Rigidbody>();

        if (rbFake != null)
        {
            Vector3 dir =
                realRb.linearVelocity.normalized;

            rbFake.linearVelocity =
                dir * realRb.linearVelocity.magnitude;
        }
    }

    // ================= INVISIBLE =================

    IEnumerator Invisible()
    {
        isInvisible = true;

        float timer = 5f;

        Renderer[] rends =
            GetComponentsInChildren<Renderer>();

        foreach (var r in rends)
        {
            r.enabled = false;
        }

        while (timer > 0)
        {
            timer -= Time.deltaTime;

            yield return null;
        }

        foreach (var r in rends)
        {
            r.enabled = true;
        }

        isInvisible = false;
    }

    // ================= TORNADO =================

    IEnumerator Tornado()
    {
        if (ball == null)
            yield break;

        Rigidbody rb =
            ball.GetComponent<Rigidbody>();

        if (rb == null)
            yield break;

        isTornadoActive = true;

        rb.linearVelocity = Vector3.zero;

        float radius = 3f;

        float angle = 0;

        float timer = 3f;

        while (timer > 0)
        {
            angle += 500f * Time.deltaTime;

            float rad =
                angle * Mathf.Deg2Rad;

            Vector3 offset =
                new Vector3(
                    Mathf.Cos(rad),
                    0,
                    Mathf.Sin(rad)
                ) * radius;

            ball.transform.position =
                transform.position + offset;

            timer -= Time.deltaTime;

            yield return null;
        }

        Transform target =
            PlayerFinder.FindNextPlayer(transform);

        if (target != null)
        {
            Vector3 dir =
                (target.position -
                ball.transform.position)
                .normalized;

            rb.linearVelocity =
                dir * 120f;
        }

        isTornadoActive = false;
    }

    // ================= UI =================

    public bool DashReady()
    {
        return dashTimer <= 0;
    }

    public bool SkillReady()
    {
        return skillTimer <= 0;
    }

    public float GetDashCooldownNormalized()
    {
        return dashTimer / dashCooldown;
    }

    public float GetSkillCooldownNormalized()
    {
        if (skillCooldown <= 0)
            return 0;

        return skillTimer / skillCooldown;
    }
}