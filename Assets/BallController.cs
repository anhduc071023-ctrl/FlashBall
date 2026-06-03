using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : NetworkBehaviour
{
    public static BallController instance;

    // ================= NETWORK =================

    [Networked]
    public float speed { get; set; }

    [Networked]
    public bool isInSkill { get; set; }

    [Networked]
    public bool isResetting { get; set; }

    // ================= REFERENCES =================

    [Header("References")]
    public Transform mapCenter;

    Rigidbody rb;

    Transform player;

    // ================= SPEED =================

    [Header("Speed")]
    public float startSpeed = 10f;

    public float speedMultiplier = 1.1f;

    public float maxSpeed = 35f;

    // ================= PARRY =================

    [Header("Parry")]
    public float parryForce = 70f;

    public float liftForce = 2f;

    public float sideForce = 2f;

    public float extraGravity = 15f;

    // ================= HOMING =================

    [Header("Homing")]
    public float homingStrength = 5f;

    // ================= POWER =================

    [Header("Power")]
    public float powerForceMultiplier = 1.8f;

    public float powerSpeedMultiplier = 1.5f;

    // ================= STATE =================

    [Header("State")]
    public bool isFakeBall = false;

    public float preSkillSpeed;

    bool canMove = true;

    bool waitingNextLaunch;

    HashSet<Transform> ignoreTargets =
        new HashSet<Transform>();

    // ================= SPAWN =================

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();

        if (!isFakeBall)
        {
            instance = this;
        }

        rb.useGravity = false;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.constraints =
            RigidbodyConstraints.FreezeRotation;

        rb.sleepThreshold = 0f;

        speed = startSpeed;

        ResetVelocity();

        if (
            Object != null &&
            Object.HasStateAuthority &&
            mapCenter != null
        )
        {
            transform.position =
                mapCenter.position;
        }
    }

    // ================= INIT =================

    public void Init(
        Transform firstPlayer,
        Transform center
    )
    {
        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        player = firstPlayer;

        mapCenter = center;

        if (mapCenter != null)
        {
            transform.position =
                mapCenter.position;
        }

        speed = startSpeed;

        ResetVelocity();
    }

    // ================= UPDATE =================

    public override void FixedUpdateNetwork()
    {
        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        // fake ball chỉ bay theo force
        if (isFakeBall)
            return;

        if (!canMove)
            return;

        if (isResetting)
            return;

        if (waitingNextLaunch)
            return;

        if (isInSkill)
            return;

        if (rb == null)
            return;

        // ================= TARGET =================

        if (
            player == null ||
            !player.gameObject.activeInHierarchy
        )
        {
            player =
                PlayerFinder
                .FindClosestAlivePlayer(
                    transform.position
                );

            if (player == null)
            {
                ResetVelocity();
                return;
            }
        }

        PlayerHealth hp =
            player.GetComponent<PlayerHealth>();

        if (hp == null || hp.IsDead)
        {
            SwitchTarget(player);

            if (player == null)
            {
                ResetVelocity();
                return;
            }
        }

        // ================= HOMING =================

        Vector3 targetPos =
            player.position + Vector3.up;

        Vector3 dir =
            targetPos - transform.position;

        dir.y = 0f;

        // tránh đứng im
        if (dir.sqrMagnitude < 0.001f)
        {
            dir = transform.forward;
        }

        dir.Normalize();

        Vector3 targetVelocity =
            dir * speed;

        rb.linearVelocity =
            Vector3.Lerp(
                rb.linearVelocity,
                targetVelocity,
                Runner.DeltaTime *
                homingStrength
            );

        rb.linearVelocity +=
            Vector3.down *
            extraGravity *
            Runner.DeltaTime;
    }

    // ================= PARRY =================

    public void Parry(
        Transform parryPlayer,
        bool isPower
    )
    {
        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        if (isResetting)
            return;

        if (rb == null)
            return;

        if (parryPlayer == null)
            return;

        StopAllCoroutines();

        // thoát skill
        if (isInSkill)
        {
            isInSkill = false;

            rb.isKinematic = false;

            speed = preSkillSpeed;
        }

        // ignore người vừa parry
        IgnorePlayer(parryPlayer);

        // target mới
        if (!isFakeBall)
        {
            SwitchTarget(parryPlayer);

            // fallback
            if (
                player == null ||
                player == parryPlayer
            )
            {
                player =
                    PlayerFinder
                    .FindClosestAlivePlayer(
                        transform.position,
                        parryPlayer
                    );
            }
        }

        // speed
        float speedMul =
            isPower
            ? powerSpeedMultiplier
            : speedMultiplier;

        speed =
            Mathf.Clamp(
                speed * speedMul,
                startSpeed,
                maxSpeed
            );

        // direction
        Vector3 dir =
            transform.position -
            parryPlayer.position;

        dir.y = 0f;

        dir.Normalize();

        Vector3 right =
            Vector3.Cross(
                Vector3.up,
                dir
            ).normalized;

        Vector3 finalDir =
            (
                dir +
                Vector3.up *
                liftForce *
                0.2f +
                right *
                Random.Range(
                    -sideForce,
                    sideForce
                )
            ).normalized;

        rb.linearVelocity =
            Vector3.zero;

        float force =
            isPower
            ? parryForce *
            powerForceMultiplier
            : parryForce;

        rb.AddForce(
            finalDir * force,
            ForceMode.VelocityChange
        );
    }

    // ================= COLLISION =================

    void OnCollisionEnter(
        Collision collision
    )
    {
        // ================= FAKE BALL =================

        if (isFakeBall)
        {
            if (
                Runner != null &&
                Object != null
            )
            {
                Runner.Despawn(Object);
            }

            return;
        }

        // ================= SERVER =================

        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        if (isResetting)
            return;

        if (collision == null)
            return;

        if (collision.transform == null)
            return;

        Transform hit =
            collision.transform;

        if (hit == null)
            return;

        // ================= PLAYER =================

        PlayerHealth hp =
            hit.GetComponentInParent
            <PlayerHealth>();

        if (hp == null)
            return;

        if (hp.IsDead)
            return;

        if (hp.transform == null)
            return;

        StopAllCoroutines();

        isResetting = true;

        canMove = false;

        isInSkill = false;

        waitingNextLaunch = true;

        ResetVelocity();

        Vector3 dir =
            (
                hp.transform.position -
                transform.position
            ).normalized;

        hp.TakeDamage(1, dir);

        // reset ball
        if (mapCenter != null)
        {
            transform.position =
                mapCenter.position;
        }

        speed = startSpeed;

        player = null;

        ignoreTargets.Clear();

        StartCoroutine(
            ResumeRoutine()
        );
    }

    // ================= RESUME =================

    IEnumerator ResumeRoutine()
    {
        yield return new WaitForSeconds(
            0.5f
        );

        canMove = true;

        isResetting = false;

        speed = startSpeed;

        ResetVelocity();

        yield return new WaitForSeconds(
            0.2f
        );

        player =
            PlayerFinder
            .FindClosestAlivePlayer(
                transform.position
            );

        waitingNextLaunch = false;
    }

    // ================= TARGET =================

    public Transform GetTarget()
    {
        return player;
    }

    public void SetTarget(
        Transform newTarget
    )
    {
        if (newTarget == null)
            return;

        PlayerHealth hp =
            newTarget.GetComponent
            <PlayerHealth>();

        if (hp == null)
            return;

        if (hp.IsDead)
            return;

        if (
            ignoreTargets.Contains(
                newTarget
            )
        )
            return;

        player = newTarget;
    }

    public void SwitchTarget(
        Transform currentPlayer
    )
    {
        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        Transform next =
            PlayerFinder.FindNextPlayer(
                currentPlayer
            );

        // fallback
        if (next == null)
        {
            next =
                PlayerFinder
                .FindClosestAlivePlayer(
                    transform.position
                );
        }

        if (next == null)
            return;

        if (
            ignoreTargets.Contains(next)
        )
        {
            return;
        }

        player = next;
    }

    public void IgnorePlayer(
        Transform p
    )
    {
        if (p == null)
            return;

        if (!ignoreTargets.Contains(p))
        {
            ignoreTargets.Add(p);
        }

        StartCoroutine(
            RemoveIgnoreRoutine(p)
        );
    }

    IEnumerator RemoveIgnoreRoutine(
        Transform p
    )
    {
        yield return new WaitForSeconds(
            0.15f
        );

        if (
            ignoreTargets.Contains(p)
        )
        {
            ignoreTargets.Remove(p);
        }
    }

    public void ResetTarget()
    {
        ignoreTargets.Clear();
    }

    // ================= RESET =================

    void ResetVelocity()
    {
        if (rb == null)
            return;

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;
    }

    // ================= START MATCH =================

    public void StartMatch()
    {
        if (
            Object == null ||
            !Object.HasStateAuthority
        )
            return;

        if (mapCenter == null)
        {
            Debug.LogError(
                "MapCenter Missing"
            );

            return;
        }

        canMove = true;

        isResetting = false;

        isInSkill = false;

        waitingNextLaunch = false;

        speed = startSpeed;

        transform.position =
            mapCenter.position;

        ResetVelocity();

        ignoreTargets.Clear();

        player =
            PlayerFinder
            .FindClosestAlivePlayer(
                transform.position
            );

        Debug.Log("BALL START");
    }
}