using UnityEngine;

public class CoroutineRunner : MonoBehaviour
{
    public static CoroutineRunner Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
}