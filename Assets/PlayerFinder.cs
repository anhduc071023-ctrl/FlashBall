using Fusion;
using UnityEngine;
using System.Collections.Generic;

public static class PlayerFinder
{
    // ================= CLOSEST PLAYER =================

    public static Transform FindClosestAlivePlayer(
        Vector3 fromPos,
        Transform ignore = null
    )
    {
        NetworkPlayer[] players =
            Object.FindObjectsByType<NetworkPlayer>(
                FindObjectsSortMode.None
            );

        Transform best = null;

        float bestDist =
            Mathf.Infinity;

        foreach (NetworkPlayer p in players)
        {
            if (p == null)
                continue;

            if (!p.gameObject.activeInHierarchy)
                continue;

            // ignore player
            if (
                ignore != null &&
                p.transform == ignore
            )
            {
                continue;
            }

            PlayerHealth hp =
                p.GetComponent<PlayerHealth>();

            if (hp == null)
                continue;

            // player chết
            if (hp.IsDead)
                continue;

            // component disable
            if (!hp.enabled)
                continue;

            // object bị destroy
            if (
                p.transform == null
            )
            {
                continue;
            }

            // distance
            float dist =
                Vector3.Distance(
                    fromPos,
                    p.transform.position
                );

            if (dist < bestDist)
            {
                bestDist = dist;
                best = p.transform;
            }
        }

        return best;
    }

    // ================= NEXT PLAYER =================

    public static Transform FindNextPlayer(
        Transform current
    )
    {
        // current null
        if (current == null)
        {
            return GetRandomAlivePlayer();
        }

        Transform next =
            FindClosestAlivePlayer(
                current.position,
                current
            );

        // không còn ai
        if (next == null)
        {
            return null;
        }

        return next;
    }

    // ================= COUNT ALIVE =================

    public static int CountAlivePlayers()
    {
        NetworkPlayer[] players =
            Object.FindObjectsByType<NetworkPlayer>(
                FindObjectsSortMode.None
            );

        int count = 0;

        foreach (NetworkPlayer p in players)
        {
            if (p == null)
                continue;

            if (!p.gameObject.activeInHierarchy)
                continue;

            PlayerHealth hp =
                p.GetComponent<PlayerHealth>();

            if (hp == null)
                continue;

            if (hp.IsDead)
                continue;

            count++;
        }

        return count;
    }

    // ================= RANDOM PLAYER =================

    public static Transform GetRandomAlivePlayer()
    {
        NetworkPlayer[] players =
            Object.FindObjectsByType<NetworkPlayer>(
                FindObjectsSortMode.None
            );

        List<Transform> alive =
            new List<Transform>();

        foreach (NetworkPlayer p in players)
        {
            if (p == null)
                continue;

            if (!p.gameObject.activeInHierarchy)
                continue;

            PlayerHealth hp =
                p.GetComponent<PlayerHealth>();

            if (hp == null)
                continue;

            if (hp.IsDead)
                continue;

            alive.Add(p.transform);
        }

        if (alive.Count == 0)
            return null;

        int index =
            Random.Range(
                0,
                alive.Count
            );

        return alive[index];
    }
}