using System.Collections;
using Fusion;
using UnityEngine;

public class AutoParryBot : NetworkBehaviour
{
    [Header("Parry")]
    public float parryRange = 5.5f;

    public float parryCooldown = 0.25f;

    public float reactionTime = 0.08f;

    [Header("Movement")]
    public float strafeSpeed = 0.65f;

    public float preferredRadius = 11f;

    float cooldownTimer;

    float strafeSign = 1f;

    float strafeSwitchTimer;

    bool pendingParry;

    BallController ball;

    PlayerHealth hp;

    PlayerMovement movement;

    PlayerAbilityController ability;

    NetworkObject netObj;

    Vector3 anchorPos;

    bool hasAnchor;

    void Awake()
    {
        InitializeRefs();
    }

    void Start()
    {
        InitializeRefs();
    }

    public override void Spawned()
    {
        InitializeRefs();
        gameObject.tag = "Player";
        anchorPos = transform.position;
        hasAnchor = true;
        strafeSign = Random.value > 0.5f ? 1f : -1f;
        strafeSwitchTimer = Random.Range(1.5f, 3.5f);
    }

    void InitializeRefs()
    {
        if (netObj == null)
            netObj = GetComponent<NetworkObject>();

        if (hp == null)
            hp = GetComponent<PlayerHealth>();

        if (movement == null)
            movement = GetComponent<PlayerMovement>();

        if (ability == null)
            ability = GetComponent<PlayerAbilityController>();

        if (!hasAnchor)
        {
            anchorPos = transform.position;
            hasAnchor = true;
        }

        if (strafeSwitchTimer <= 0f)
        {
            strafeSign = Random.value > 0.5f ? 1f : -1f;
            strafeSwitchTimer = Random.Range(1.5f, 3.5f);
        }

        FindBall();
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null)
            return;

        TickBot(Runner != null ? Runner.DeltaTime : Time.fixedDeltaTime);
    }

    void FixedUpdate()
    {
        if (Object != null)
            return;

        TickBot(Time.fixedDeltaTime);
    }

    void TickBot(float dt)
    {
        if (netObj == null || !netObj.IsValid)
            return;

        bool isServerBot =
            netObj.HasStateAuthority &&
            netObj.InputAuthority == PlayerRef.None;

        bool isAutoClient =
            MatchmakingManager.AutoPlayLocalClient &&
            netObj.HasInputAuthority &&
            !netObj.HasStateAuthority;

        if (!isServerBot && !isAutoClient)
            return;

        if (hp == null)
            hp = GetComponent<PlayerHealth>();

        if (hp == null || hp.Object == null || !hp.Object.IsValid || hp.IsDead)
            return;

        if (ball == null || !ball.gameObject.activeInHierarchy || ball.Object == null || !ball.Object.IsValid)
        {
            FindBall();
            if (ball == null || ball.Object == null || !ball.Object.IsValid)
                return;
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= dt;
        }

        if (isServerBot)
        {
            UpdateBotMovement(dt);
        }

        if (ball.Runner == null || !ball.Runner.IsRunning || !ball.ballActive || ball.isResetting || ball.isInSkill)
            return;

        Transform target = ball.GetTarget();
        if (target == null)
            return;

        NetworkObject targetNet = target.GetComponent<NetworkObject>();
        if (targetNet == null || targetNet.Id != netObj.Id)
            return;

        float dist = Vector3.Distance(transform.position, ball.transform.position);

        if (dist <= parryRange && cooldownTimer <= 0f && !pendingParry)
        {
            cooldownTimer = parryCooldown;
            pendingParry = true;

            if (isServerBot)
            {
                StartCoroutine(ServerParryDelay());
            }
            else
            {
                StartCoroutine(ClientAutoParryDelay());
            }
        }
    }

    void UpdateBotMovement(float dt)
    {
        if (movement == null || !movement.canMove)
            return;

        if (
            GameManager.Instance != null &&
            GameManager.Instance.Object != null &&
            GameManager.Instance.Object.IsValid &&
            GameManager.Instance.phase != MatchPhase.Playing
        )
        {
            movement.SetBotMoveDirection(Vector3.zero);
            return;
        }

        strafeSwitchTimer -= dt;
        if (strafeSwitchTimer <= 0f)
        {
            strafeSign = -strafeSign;
            strafeSwitchTimer = Random.Range(1.5f, 3.5f);
        }

        Vector3 toBall = ball.transform.position - transform.position;
        toBall.y = 0f;

        Vector3 dir = Vector3.zero;

        if (toBall.sqrMagnitude > 0.01f)
        {
            Vector3 forward = toBall.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            dir = right * (strafeSign * strafeSpeed);

            Vector3 center =
                GameManager.Instance != null && GameManager.Instance.mapCenter != null
                ? GameManager.Instance.mapCenter.position
                : anchorPos;

            Vector3 fromCenter = transform.position - center;
            fromCenter.y = 0f;

            if (fromCenter.magnitude > preferredRadius + 3f)
            {
                dir -= fromCenter.normalized * 0.6f;
            }
            else if (fromCenter.magnitude < preferredRadius - 4f)
            {
                dir += fromCenter.normalized * 0.4f;
            }
        }

        movement.SetBotMoveDirection(dir);
    }

    IEnumerator ServerParryDelay()
    {
        yield return new WaitForSeconds(reactionTime);
        pendingParry = false;

        if (
            ball == null ||
            ball.Object == null ||
            !ball.Object.IsValid ||
            ball.Runner == null ||
            !ball.Runner.IsRunning ||
            !ball.ballActive ||
            ball.isInSkill ||
            hp == null ||
            hp.Object == null ||
            !hp.Object.IsValid ||
            hp.IsDead
        )
            yield break;

        bool isPower = Random.value > 0.45f;

        if (ability != null)
        {
            ability.RPC_ServerPlayAttackAnim(isPower);
        }

        ball.Parry(transform, isPower);

        Debug.Log((netObj != null ? name : "Bot") + (isPower ? " POWER BOT PARRY" : " BOT PARRY"));
    }

    IEnumerator ClientAutoParryDelay()
    {
        yield return new WaitForSeconds(reactionTime);
        pendingParry = false;

        if (
            ball == null ||
            ball.Object == null ||
            !ball.Object.IsValid ||
            ball.Runner == null ||
            !ball.Runner.IsRunning ||
            !ball.ballActive ||
            ball.isInSkill ||
            hp == null ||
            hp.Object == null ||
            !hp.Object.IsValid ||
            hp.IsDead
        )
            yield break;

        bool isPower = Random.value > 0.5f;

        if (ability != null)
        {
            ability.RPC_PlayAttackAnim(isPower);
        }

        ball.RequestParry(transform, isPower);
    }

    void FindBall()
    {
        if (BallController.instance != null)
        {
            ball = BallController.instance;
        }
        else
        {
            ball = FindFirstObjectByType<BallController>();
        }
    }
}