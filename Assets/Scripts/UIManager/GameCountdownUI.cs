using TMPro;
using UnityEngine;
using Unity.Netcode;

public class GameCountdownUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI countdownText;

    private void Update()
    {
        if (NetworkCountdownManager.Instance == null) return;
        float timeLeft = NetworkCountdownManager.Instance.GetTimeRemaining();

        if (timeLeft <= 0)
        {
            if (panel.activeSelf)
            {
                Debug.Log("[GameCountdownUI] Countdown finished, hiding panel.");
                panel.SetActive(false);
            }
            return;
        }

        if (!panel.activeSelf)
            panel.SetActive(true);

        int displayTime = Mathf.CeilToInt(timeLeft);
        countdownText.text = $"Game starting in {displayTime}s...";
    }
    
    private void Awake()
    {   
        panel.SetActive(true);
        if (panel == null)
            Debug.LogWarning("[GameCountdownUI] Panel is not assigned!");
        if (countdownText == null)
            Debug.LogWarning("[GameCountdownUI] CountdownText is not assigned!");
    }

}