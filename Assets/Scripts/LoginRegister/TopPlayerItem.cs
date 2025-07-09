using TMPro;
using UnityEngine;
public class TopPlayerItem : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI scoreText;

    public void Setup(int rank, string username, float score)
    {
        if (rankText != null)
            rankText.text = $"#{rank}";

        if (usernameText != null)
            usernameText.text = username;

        if (scoreText != null)
            scoreText.text = score.ToString("F0");
    }
}
