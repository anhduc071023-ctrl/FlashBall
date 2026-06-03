using Fusion;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer Local;

    // ================= NETWORKED =================

    [Networked]
    public NetworkString<_16> PlayerName { get; set; }

    [Networked]
    public int SkillID { get; set; }

    [Networked]
    public bool IsDead { get; set; }

    // ================= REFERENCES =================

    PlayerAbilityController ability;

    PlayerMovement movement;

    // ================= SPAWN =================

    public override void Spawned()
    {
        ability =
            GetComponent<PlayerAbilityController>();

        movement =
            GetComponent<PlayerMovement>();

        // local player
        if (Object.HasInputAuthority)
        {
            Local = this;

            string randomName =
                "Player" + Random.Range(100, 999);

            RPC_SetName(randomName);

            Debug.Log(
                "Local Player Spawned"
            );
        }
    }

    // ================= NAME =================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    public void RPC_SetName(string newName)
    {
        PlayerName = newName;
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