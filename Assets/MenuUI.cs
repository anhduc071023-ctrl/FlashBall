using TMPro;
using UnityEngine;

public class MenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject findingMatchPanel;

    [Header("Texts")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI modeText;

    float timer;

    bool searching;

    // ================= INIT =================

    void Start()
    {
        StopSearching();
    }

    // ================= UPDATE =================

    void Update()
    {
        if (!searching)
            return;

        timer += Time.deltaTime;

        int min =
            Mathf.FloorToInt(
                timer / 60
            );

        int sec =
            Mathf.FloorToInt(
                timer % 60
            );

        if (timerText != null)
        {
            timerText.text =
                $"{min:00}:{sec:00}";
        }
    }

    // ================= CORE =================

    void StartSearching(
        string mode,
        System.Action startAction
    )
    {
        // chống spam
        if (searching)
            return;

        searching = true;

        timer = 0f;

        if (findingMatchPanel != null)
        {
            findingMatchPanel.SetActive(
                true
            );
        }

        if (modeText != null)
        {
            modeText.text = mode;
        }

        if (statusText != null)
        {
            statusText.text =
                "Finding Players...";
        }

        startAction?.Invoke();
    }

    // ================= PLAY =================

    public void PlayRank()
    {
        StartSearching(
            "RANK",
            () =>
            {
                MatchmakingManager
                    .Instance
                    ?.StartRankMatch();
            });
    }

    public void PlaySolo()
    {
        StartSearching(
            "SOLO",
            () =>
            {
                MatchmakingManager
                    .Instance
                    ?.StartSoloMatch();
            });
    }

    public void PlayTraining()
    {
        StartSearching(
            "TRAINING",
            () =>
            {
                MatchmakingManager
                    .Instance
                    ?.StartTraining();
            });
    }

    // ================= CANCEL =================

    public void CancelMatch()
    {
        StopSearching();

        MatchmakingManager
            .Instance
            ?.CancelMatchmaking();
    }

    // ================= STOP =================

    public void StopSearching()
    {
        searching = false;

        timer = 0f;

        if (findingMatchPanel != null)
        {
            findingMatchPanel.SetActive(
                false
            );
        }

        if (statusText != null)
        {
            statusText.text = "";
        }

        if (timerText != null)
        {
            timerText.text = "00:00";
        }
    }
}