using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    private void Awake()
    {
        if (scoreText == null)
            scoreText = GetComponent<TMP_Text>();
    }

    private void UpdateScoreDisplay(int scoreAmount)
    { 
        if (scoreText != null)
        {
            scoreText.text = scoreAmount.ToString();
        }
        else
        {
            Debug.LogWarning("ScoreUI: scoreText is null!");
        }
    }

    private void OnEnable()
    {
        GameEvents.OnScoreChanged += UpdateScoreDisplay;
    }

    private void OnDisable()
    {
        GameEvents.OnScoreChanged -= UpdateScoreDisplay;
    }
} 