using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerCameraHandler : NetworkBehaviour
{
    private CinemachineVirtualCamera followCam;
    private CinemachineVirtualCamera aimCam;

    [SerializeField] private Transform gunAimTarget;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Tìm theo tag (đã gán sẵn trong Editor)
            GameObject followObj = GameObject.FindGameObjectWithTag("FollowCam");
            GameObject aimObj = GameObject.FindGameObjectWithTag("AimCam");

            if (followObj != null && aimObj != null)
            {
                followCam = followObj.GetComponent<CinemachineVirtualCamera>();
                aimCam = aimObj.GetComponent<CinemachineVirtualCamera>();

                // Gán target
                followCam.Follow = this.transform;
                aimCam.Follow = this.transform;
                aimCam.LookAt = gunAimTarget != null ? gunAimTarget : this.transform;
            }
            else
            {
                Debug.LogError("Không tìm thấy camera theo tag!");
            }
        }
    }

    void Update()
    {
        if (!IsOwner || followCam == null || aimCam == null) return;

        if (Input.GetMouseButton(1)) // Chuột phải = aim
        {
            followCam.Priority = 5;
            aimCam.Priority = 20;
        }
        else
        {
            followCam.Priority = 20;
            aimCam.Priority = 5;
        }
    }
}
