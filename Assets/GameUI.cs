using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("Health")]
    public Image healthFill;

    [Header("Score")]
    public TMP_Text scoreText;
    private int score = 0;

    void Start()
    {
        UpdateScoreUI();
    }

    public void UpdateHealth(float percent)
    {
        healthFill.fillAmount = percent;
    }

   
    public void AddScoreNormal()
    {
        score += 2;
        UpdateScoreUI();
    }

    
    public void AddScorePower()
    {
        score += 5;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        scoreText.text = "Score: " + score;
    }
}