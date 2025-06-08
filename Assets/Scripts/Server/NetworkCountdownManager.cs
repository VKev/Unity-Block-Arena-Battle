using Unity.Netcode;
using UnityEngine;

public class NetworkCountdownManager : NetworkBehaviour
{
    public static NetworkCountdownManager Instance;

    [SerializeField] private GameCountdownUI countdownUI;
    private NetworkVariable<float> serverCountdownTime = new(30, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool countdownRunning;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {

        if (!IsServer || !countdownRunning) return;

        serverCountdownTime.Value -= Time.deltaTime;

        if (serverCountdownTime.Value <= 0)
        {
            serverCountdownTime.Value = 0;
            countdownRunning = false;
            OnCountdownComplete();
        }
    }

    public void StartCountdown(float time, bool isServer)
    {
        if (!isServer) return;
        serverCountdownTime.Value = time;
        countdownRunning = true;
    }

    private void OnCountdownComplete()
    {
        Debug.Log("[Countdown] Game start!");
        // Call game-start logic here
    }

    public float GetTimeRemaining() => serverCountdownTime.Value;
}
