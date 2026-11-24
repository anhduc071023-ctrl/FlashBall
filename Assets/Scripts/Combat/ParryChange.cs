using UnityEngine;
using UnityEngine.UI;
using Fusion;
using UnityEngine.EventSystems;

public class ParryChange :
    MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("UI")]
    public Image chargeCircle;

    [Header("Settings")]
    public float maxChargeTime = 0.85f;

    public float attackDuration = 0.34f;

    public float parryRange = 7.5f;

    public float parryBufferDuration = 0.15f;

    PlayerMovement player;

    PlayerAbilityController ability;

    WeaponElementController weaponCtrl;

    BallController ball;

    float charge;

    float attackTimer;

    float parryBufferTimer;

    bool bufferedPowerParry;

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
    }

    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        StartCharge();
    }

    public void OnPointerUp(
        PointerEventData eventData
    )
    {
        ReleaseCharge();
    }

    // ================= UPDATE =================

    void Update()
    {
        if (player == null)
        {
            FindLocalPlayer();
            return;
        }

        if (ball == null)
        {
            ball =
                BallController.instance != null
                ? BallController.instance
                : FindFirstObjectByType<BallController>();
        }

        HandleAttackLock();
        HandleCharge();
        HandleParryBuffer();
    }

    void HandleParryBuffer()
    {
        if (parryBufferTimer <= 0f)
            return;

        parryBufferTimer -= Time.deltaTime;

        if (TryExecuteParry(bufferedPowerParry))
        {
            parryBufferTimer = 0f;
        }
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
                ability = p.GetComponent<PlayerAbilityController>();
                weaponCtrl = p.GetComponent<WeaponElementController>();
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

        // Khi tụ lực parry chuẩn, hào quang nguyên tố trên vũ khí sáng rực dần lên!
        if (weaponCtrl == null && player != null)
        {
            weaponCtrl = player.GetComponent<WeaponElementController>();
        }

        if (weaponCtrl != null)
        {
            weaponCtrl.SetChargeAura(t);
        }
    }

    // ================= START CHARGE =================

    public void StartCharge()
    {
        if (player == null)
            return;

        if (holding || isAttacking)
            return;

        holding = true;
        charge = 0f;
    }

    // ================= RELEASE =================

    public void ReleaseCharge()
    {
        if (player == null)
            return;

        if (!holding || isAttacking)
            return;

        holding = false;

        isPowerParry = charge >= maxChargeTime;

        if (weaponCtrl != null)
        {
            weaponCtrl.SetChargeAura(0f);
        }

        StartAttack(isPowerParry);

        // Thực hiện Parry ngay lập tức (0ms delay), giữ buffer 0.15s nếu bóng sắp vào tầm
        if (!TryExecuteParry(isPowerParry))
        {
            bufferedPowerParry = isPowerParry;
            parryBufferTimer = Mathf.Max(0.12f, parryBufferDuration);
        }
        else
        {
            parryBufferTimer = 0f;
        }

        ResetUI();
    }

    // ================= ATTACK =================

    void StartAttack(bool isPower)
    {
        isAttacking = true;
        attackTimer = Mathf.Clamp(attackDuration, 0.22f, 0.34f);

        if (ability != null)
        {
            ability.RPC_PlayAttackAnim(isPower);
        }
        else
        {
            if (player.animator != null)
            {
                player.animator.ResetTrigger("Attack1");
                player.animator.ResetTrigger("Attack2");
                player.animator.SetTrigger(isPower ? "Attack2" : "Attack1");
            }

            ParrySlashVFX vfx = player.GetComponent<ParrySlashVFX>();
            if (vfx != null)
            {
                vfx.PlaySlash(isPower);
            }
        }
    }

    // ================= PARRY =================

    bool TryExecuteParry(bool power)
    {
        if (player == null)
            return false;

        float effectiveRange = Mathf.Max(parryRange, 7.5f);

        bool hitFakeBall = FakeBallVisual.TryParryNearbyFakeBalls(
            player.transform.position,
            effectiveRange
        );

        if (ball == null)
            return hitFakeBall;

        // Không cho phép Parry bóng khi bóng đang xoay lốc Tornado
        if (ball.Object != null && ball.Object.IsValid && (!ball.ballActive || ball.isInSkill))
            return hitFakeBall;

        float dist =
            Vector3.Distance(
                player.transform.position,
                ball.transform.position
            );

        if (dist > effectiveRange)
        {
            return hitFakeBall;
        }

        ball.RequestParry(
            player.transform,
            power
        );

        Debug.Log(power ? "POWER PARRY" : "NORMAL PARRY");
        return true;
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
