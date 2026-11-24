using Fusion;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer Local;

    // ================= NETWORKED =================

    [Networked]
    public NetworkString<_16> PlayerName { get; set; }

    [Networked]
    public int PlayerEloNet { get; set; }

    [Networked]
    public int SoloWinsNet { get; set; }

    [Networked]
    public int SoloLossesNet { get; set; }

    [Networked]
    public NetworkBool IsRealHumanNet { get; set; }

    [Networked]
    public int SkillID { get; set; }

    [Networked]
    public int WeaponID { get; set; }

    [Networked]
    public int SkinID { get; set; }

    [Networked]
    public bool IsDead { get; set; }

    // ================= REFERENCES =================

    PlayerAbilityController ability;

    PlayerMovement movement;

    WeaponElementController weaponController;

    PlayerSkinController skinController;

    int lastAppliedWeaponId = -1;
    int lastAppliedSkinId = -1;
    bool recordedToLeaderboard;

    // ================= SPAWN =================

    public override void Spawned()
    {
        ability =
            GetComponent<PlayerAbilityController>();

        movement =
            GetComponent<PlayerMovement>();

        weaponController = GetComponent<WeaponElementController>();
        if (weaponController == null)
        {
            weaponController = gameObject.AddComponent<WeaponElementController>();
        }

        skinController = GetComponent<PlayerSkinController>();
        if (skinController == null)
        {
            skinController = gameObject.AddComponent<PlayerSkinController>();
        }

        // local player
        if (Object.HasInputAuthority)
        {
            Local = this;

            string chosenName = PlayerStatsManager.HasPlayerName
                ? PlayerStatsManager.PlayerName
                : ("Player" + Random.Range(100, 999));

            RPC_SetName(chosenName);
            RPC_SetProfileStats(
                PlayerStatsManager.PlayerElo,
                PlayerStatsManager.SoloWins,
                PlayerStatsManager.SoloLosses
            );
            RPC_SetWeapon((int)PlayerStatsManager.EquippedWeapon);
            RPC_SetSkin((int)PlayerStatsManager.EquippedSkin);

            SkillType savedSkill = PlayerStatsManager.EquippedSkill;
            if (ability != null)
            {
                ability.currentSkill = savedSkill;
            }
            RPC_SetSkill((int)savedSkill);

            if (
                MatchmakingManager.AutoPlayLocalClient &&
                GetComponent<AutoParryBot>() == null
            )
            {
                gameObject.AddComponent<AutoParryBot>();
            }

            Debug.Log(
                "Local Player Spawned"
            );
        }

        ApplyWeaponVisualIfNeeded();
        ApplySkinVisualIfNeeded();
    }

    public override void Render()
    {
        ApplyWeaponVisualIfNeeded();
        ApplySkinVisualIfNeeded();

        // Lưu tự động dữ liệu người chơi thật gặp trong trận vào Bảng Xếp Hạng Rank & Solo!
        if (!recordedToLeaderboard && Object != null && Object.IsValid && IsRealHumanNet)
        {
            string pName = PlayerName.ToString();
            if (!string.IsNullOrWhiteSpace(pName) && !pName.StartsWith("Bot_"))
            {
                recordedToLeaderboard = true;
                LeaderboardManager.RecordRealPlayer(
                    pName,
                    PlayerEloNet,
                    SoloWinsNet,
                    SoloLossesNet
                );
            }
        }
    }

    void ApplyWeaponVisualIfNeeded()
    {
        if (Object == null || !Object.IsValid)
            return;

        if (weaponController == null)
        {
            weaponController = GetComponent<WeaponElementController>();
            if (weaponController == null)
            {
                weaponController = gameObject.AddComponent<WeaponElementController>();
            }
        }

        if (WeaponID != lastAppliedWeaponId)
        {
            lastAppliedWeaponId = WeaponID;
            weaponController.ApplyWeapon((WeaponType)WeaponID);
        }
    }

    void ApplySkinVisualIfNeeded()
    {
        if (Object == null || !Object.IsValid)
            return;

        if (skinController == null)
        {
            skinController = GetComponent<PlayerSkinController>();
            if (skinController == null)
            {
                skinController = gameObject.AddComponent<PlayerSkinController>();
            }
        }

        if (SkinID != lastAppliedSkinId)
        {
            lastAppliedSkinId = SkinID;
            skinController.ApplySkin((SkinType)SkinID);
        }
    }

    // ================= WEAPON & SKIN =================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    public void RPC_SetWeapon(int weaponId)
    {
        int clamped = Mathf.Clamp(weaponId, 0, WeaponCatalog.All.Length - 1);
        WeaponID = clamped;
    }

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    public void RPC_SetSkin(int skinId)
    {
        int clamped = Mathf.Clamp(skinId, 0, SkinCatalog.All.Length - 1);
        SkinID = clamped;
    }

    // ================= NAME =================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    public void RPC_SetName(string newName)
    {
        PlayerName = newName;
        IsRealHumanNet = true;
    }

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    public void RPC_SetProfileStats(int elo, int soloWins, int soloLosses)
    {
        PlayerEloNet = Mathf.Max(0, elo);
        SoloWinsNet = Mathf.Max(0, soloWins);
        SoloLossesNet = Mathf.Max(0, soloLosses);
        IsRealHumanNet = true;
    }

    // ================= SKILL =================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    public void RPC_SetSkill(int id)
    {
        SkillID = id;

        // sync sang ability controller
        if (ability != null)
        {
            ability.currentSkill =
                (SkillType)id;
        }

        Debug.Log(
            PlayerName +
            " selected skill: " +
            ((SkillType)id).ToString()
        );
    }

    // ================= DIE =================

    public void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        gameObject.SetActive(false);

        Debug.Log(PlayerName + " died");
    }

    // ================= GETTERS =================

    public bool IsLocalPlayer()
    {
        return Object != null &&
               Object.HasInputAuthority;
    }
}