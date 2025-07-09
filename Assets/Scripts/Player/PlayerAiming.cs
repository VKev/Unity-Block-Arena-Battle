using UnityEngine;
using Unity.Netcode;

public class PlayerAiming : NetworkBehaviour
{
    public float turnSpeed = 5f; // tốc độ xoay
    private Camera mainCamera;
    private RaycastWeapon weapon;

    private void Start()
    {
        if (!IsOwner)
        {
            // Không phải chủ sở hữu thì disable script này để tránh xử lý input thừa
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        weapon = GetComponentInChildren<RaycastWeapon>();

        // Đăng ký input (giả sử bạn có InputController chuẩn)
        InputController.FireAction.action.performed += ctx => weapon.StartFiring();
        InputController.FireAction.action.canceled += ctx => weapon.StopFiring();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Get both horizontal (yaw) and vertical (pitch) rotation from camera
        float yawCamera = mainCamera.transform.eulerAngles.y;
        float pitchCamera = mainCamera.transform.eulerAngles.x;
        
        // Apply both horizontal and vertical rotation to the player
        Vector3 targetRotation = new Vector3(pitchCamera, yawCamera, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(targetRotation), turnSpeed * Time.deltaTime);
    }
}