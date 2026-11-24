using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class FakeBallVisual : MonoBehaviour
{
    public static readonly List<FakeBallVisual> ActiveFakeBalls = new List<FakeBallVisual>();

    Transform target;
    Transform owner;
    float speed = 18f;
    float homingStrength = 6.5f;
    float targetHeightOffset = 1.0f;
    float remainingDuration = 5f;
    float age;
    bool destroyed;

    Vector3 velocity;
    LineRenderer targetLine;
    Material targetLineMat;

    public void Init(
        Transform targetTransform,
        Transform ownerTransform,
        float initialSpeed,
        float duration
    )
    {
        target = targetTransform;
        owner = ownerTransform;
        speed = Mathf.Clamp(initialSpeed, 14f, 38f);
        remainingDuration = Mathf.Max(0.5f, duration);
        age = 0f;
        destroyed = false;

        Vector3 dir = target != null
            ? (target.position - transform.position)
            : transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            dir = Vector3.forward;
        dir.Normalize();

        // Lệch nhẹ 30-45 độ + bật nhẹ để đường bay giống hệt ball thật vừa bị parry
        float sign = Random.value < 0.5f ? -1f : 1f;
        Vector3 curvedDir = (Quaternion.AngleAxis(sign * Random.Range(28f, 45f), Vector3.up) * dir).normalized;
        velocity = curvedDir * speed;
        velocity.y = 5.5f;

        EnsureTargetLine();
    }

    void OnEnable()
    {
        if (!ActiveFakeBalls.Contains(this))
        {
            ActiveFakeBalls.Add(this);
        }
    }

    void OnDisable()
    {
        ActiveFakeBalls.Remove(this);
    }

    void OnDestroy()
    {
        ActiveFakeBalls.Remove(this);
        if (targetLineMat != null)
        {
            Destroy(targetLineMat);
        }
    }

    void Update()
    {
        if (destroyed)
            return;

        float dt = Time.deltaTime;
        age += dt;
        remainingDuration -= dt;

        if (remainingDuration <= 0f)
        {
            Vanish(false);
            return;
        }

        if (
            target == null ||
            !target.gameObject.activeInHierarchy ||
            PlayerFinder.IsPlayerInvisible(target)
        )
        {
            target = PlayerFinder.FindClosestAlivePlayer(transform.position, owner);
            if (target == null)
            {
                Vanish(false);
                return;
            }
        }

        PlayerHealth targetHp = target.GetComponent<PlayerHealth>();
        if (targetHp != null && targetHp.Object != null && targetHp.Object.IsValid && targetHp.IsDead)
        {
            target = PlayerFinder.FindClosestAlivePlayer(transform.position, owner);
            if (target == null)
            {
                Vanish(false);
                return;
            }
        }

        float targetY = target.position.y + targetHeightOffset;
        Vector3 targetPos = new Vector3(target.position.x, targetY, target.position.z);

        Vector3 flatDir = targetPos - transform.position;
        flatDir.y = 0f;
        float horizDist = flatDir.magnitude;

        if (flatDir.sqrMagnitude > 0.001f)
        {
            flatDir.Normalize();
        }
        else
        {
            flatDir = Vector3.forward;
        }

        Vector3 horizVel = new Vector3(velocity.x, 0f, velocity.z);
        horizVel = Vector3.Lerp(horizVel, flatDir * speed, dt * homingStrength);

        float velY = velocity.y;
        if (age < 0.25f && horizDist > 6.5f)
        {
            velY -= 36f * dt;
        }
        else
        {
            float timeToTarget = Mathf.Max(0.08f, horizDist / Mathf.Max(speed, 12f));
            float idealDive = Mathf.Clamp((targetY - transform.position.y) / timeToTarget, -30f, 12f);
            velY = Mathf.Lerp(velY, idealDive, dt * 14f);
        }

        velocity = new Vector3(horizVel.x, velY, horizVel.z);
        transform.position += velocity * dt;

        float minY = target.position.y + 0.65f;
        if (transform.position.y < minY)
        {
            Vector3 p = transform.position;
            p.y = minY;
            transform.position = p;
        }

        UpdateTargetLine(targetPos);

        // Chạm player -> biến mất (KHÔNG gây dame!)
        if (age > 0.20f)
        {
            float hitDist = Vector3.Distance(transform.position, targetPos);
            if (hitDist <= 1.15f)
            {
                Vanish(false);
                return;
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, 0.90f);
            foreach (Collider hit in hits)
            {
                if (hit == null)
                    continue;

                PlayerHealth hp = hit.GetComponentInParent<PlayerHealth>();
                if (hp == null)
                    continue;

                if (owner != null && hp.transform == owner && age < 0.45f)
                    continue;

                if (hp.Object != null && hp.Object.IsValid && hp.IsDead)
                    continue;

                Vanish(false);
                return;
            }
        }
    }

    public static bool TryParryNearbyFakeBalls(Vector3 parryOrigin, float range)
    {
        bool parriedAny = false;

        for (int i = ActiveFakeBalls.Count - 1; i >= 0; i--)
        {
            FakeBallVisual fb = ActiveFakeBalls[i];
            if (fb == null || fb.destroyed)
                continue;

            float d = Vector3.Distance(parryOrigin, fb.transform.position);
            if (d <= range)
            {
                fb.Vanish(true);
                parriedAny = true;
            }
        }

        return parriedAny;
    }

    public static Transform FindClosestFakeBall(Vector3 origin, float maxRange)
    {
        Transform best = null;
        float bestDist = maxRange;

        for (int i = ActiveFakeBalls.Count - 1; i >= 0; i--)
        {
            FakeBallVisual fb = ActiveFakeBalls[i];
            if (fb == null || fb.destroyed)
                continue;

            float d = Vector3.Distance(origin, fb.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = fb.transform;
            }
        }

        return best;
    }

    public void Vanish(bool wasParried)
    {
        if (destroyed)
            return;

        destroyed = true;
        ActiveFakeBalls.Remove(this);

        if (targetLine != null)
        {
            targetLine.enabled = false;
        }

        SpawnVanishBurst(transform.position, wasParried);
        Destroy(gameObject);
    }

    void EnsureTargetLine()
    {
        GameObject lineObj = new GameObject("FakeTargetLockLine");
        lineObj.transform.SetParent(transform, false);

        targetLine = lineObj.AddComponent<LineRenderer>();
        targetLine.useWorldSpace = true;
        targetLine.positionCount = 2;
        targetLine.startWidth = 0.07f;
        targetLine.endWidth = 0.045f;
        targetLine.numCapVertices = 4;
        targetLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        targetLine.receiveShadows = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        targetLineMat = new Material(shader);
        if (targetLineMat.HasProperty("_Surface"))
            targetLineMat.SetFloat("_Surface", 1f);
        if (targetLineMat.HasProperty("_SrcBlend"))
            targetLineMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (targetLineMat.HasProperty("_DstBlend"))
            targetLineMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (targetLineMat.HasProperty("_ZWrite"))
            targetLineMat.SetFloat("_ZWrite", 0f);
        if (targetLineMat.HasProperty("_BaseColor"))
            targetLineMat.SetColor("_BaseColor", Color.white);
        targetLineMat.renderQueue = 3100;

        targetLine.sharedMaterial = targetLineMat;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1.00f, 0.12f, 0.12f), 0f),
                new GradientColorKey(new Color(1.00f, 0.25f, 0.10f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.92f, 0f),
                new GradientAlphaKey(0.84f, 1f)
            }
        );
        targetLine.colorGradient = g;
    }

    void UpdateTargetLine(Vector3 targetChestPos)
    {
        if (targetLine == null)
            return;

        targetLine.SetPosition(0, transform.position);
        targetLine.SetPosition(1, targetChestPos);
        targetLine.enabled = true;
    }

    static void SpawnVanishBurst(Vector3 pos, bool wasParried)
    {
        GameObject burstGo = new GameObject("FakeBallVanishVFX");
        burstGo.transform.position = pos;

        ParticleSystem ps = burstGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.25f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.36f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, wasParried ? 10f : 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
        main.startColor = wasParried
            ? new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.92f, 1f, 0.95f), new Color(1f, 1f, 1f, 0.8f))
            : new ParticleSystem.MinMaxGradient(new Color(0.65f, 0.72f, 0.95f, 0.85f), new Color(0.9f, 0.95f, 1f, 0.6f));

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)(wasParried ? 22 : 14))
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f))
        );

        ParticleSystemRenderer psr = burstGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            Material m = new Material(sh);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            psr.sharedMaterial = m;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(m, 0.6f);
        }

        ps.Play();
        Destroy(burstGo, 0.55f);
    }
}
