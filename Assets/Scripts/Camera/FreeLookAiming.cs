using UnityEngine;
using Unity.Cinemachine;

public class AimZoomCameraOffsetV3 : MonoBehaviour
{
    public CinemachineCamera cineCam;

    public Vector3 aimingOffset = new Vector3(0.2f, 1.6f, -1.2f);
    public float normalFOV = 60f;
    public float aimingFOV = 40f;
    public float smoothSpeed = 5f;

    private CinemachineCameraOffset offsetComponent;
    private Vector3 originalOffset;
    private bool isAiming;

    void Start()
    {
        offsetComponent = cineCam.GetComponent<CinemachineCameraOffset>();
        if (offsetComponent == null)
        {
            Debug.LogError("CinemachineCameraOffset not found. Add it to your CinemachineCamera.");
            return;
        }

        // Lưu giá trị offset ban đầu trong Editor
        originalOffset = offsetComponent.Offset;

        // Optional: lưu luôn FOV ban đầu (nếu bạn set trong Inspector)
        normalFOV = cineCam.Lens.FieldOfView;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
            isAiming = true;
        if (Input.GetMouseButtonUp(1))
            isAiming = false;

        Vector3 targetOffset = isAiming ? aimingOffset : originalOffset;
        float targetFOV = isAiming ? aimingFOV : normalFOV;

        offsetComponent.Offset = Vector3.Lerp(offsetComponent.Offset, targetOffset, Time.deltaTime * smoothSpeed);
        cineCam.Lens.FieldOfView = Mathf.Lerp(cineCam.Lens.FieldOfView, targetFOV, Time.deltaTime * smoothSpeed);
    }
}
