using UnityEngine;

public class FireEffect : MonoBehaviour
{
    ParticleSystem ps;

    void Awake()
    {
        ps =
            GetComponent<ParticleSystem>();

        if (ps == null)
        {
            Debug.LogError(
                "ParticleSystem Missing"
            );

            return;
        }
    }

    void OnEnable()
    {
        if (ps != null)
        {
            ps.Play();
        }
    }

    void Update()
    {
        // nếu particle bị stop thì play lại
        if (
            ps != null &&
            !ps.isPlaying
        )
        {
            ps.Play();
        }
    }
}