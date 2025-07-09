using TMPro;
using UnityEngine;
using Unity.Netcode;

public class GameCountdownUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI countdownText;

    private void Awake()
    {
        if (panel == null)
            Debug.LogWarning("[GameCountdownUI] Panel is not assigned!");
        if (countdownText == null)
            Debug.LogWarning("[GameCountdownUI] CountdownText is not assigned!");
    }

    private void Update()
    {
        if (NetworkCountdownManager.Instance == null) return;

        float timeLeft = NetworkCountdownManager.Instance.GetTimeRemaining();
        GamePhase phase = NetworkCountdownManager.Instance.GetCurrentPhase();

        if (timeLeft <= 0f)
        {
            if (panel.activeSelf)
                panel.SetActive(false);
            return;
        }

        if (!panel.activeSelf)
            panel.SetActive(true);

        int displayTime = Mathf.CeilToInt(timeLeft);

        switch (phase)
        {
            case GamePhase.WaitingToStart:
                countdownText.text = $"Game starting in {displayTime}s...";
                break;
            case GamePhase.BuffPhase:
                countdownText.text = $"Choose your buff - {displayTime}s left";
                break;
            case GamePhase.SafePhase:
                countdownText.text = $"Safe Phase - {displayTime}s left";
                break;
            case GamePhase.FightPhase:
                countdownText.text = $"Fighting Phase - {displayTime}s left";
                break;
        }
    }
}