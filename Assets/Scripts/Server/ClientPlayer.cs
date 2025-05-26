using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;          // <-- add this

public class ClientPlayer : NetworkBehaviour
{
    [SerializeField] private PlayerStateMachine.PlayerStateMachine playerStateMachine;
    [SerializeField] private InputController inputController;

    private void Awake()
    {
        playerStateMachine.enabled = false;
        // inputController.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)   // Only wire camera for the local player
            return;

        playerStateMachine.enabled = true;

        GameObject camGO = GameObject.FindGameObjectWithTag("Freelook Camera");
        if (camGO == null)
        {
            Debug.LogWarning("No camera tagged 'Freelook Camera' found in the scene.");
            return;
        }

        if (camGO.TryGetComponent<CinemachineCamera>(out var vcam))
        {
            vcam.Target.TrackingTarget = transform;
            return;
        }
    }
}
