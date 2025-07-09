using PlayerStateMachine;
using System.Collections;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class RaycastWeapon : NetworkBehaviour
{
    public bool isFiring = false;
    public ParticleSystem[] muzzleFlash;
    public ParticleSystem hitEffect;
    public TrailRenderer tracerEffect;
    public Transform raycastOrigin;
    public Transform raycastDestination;
    private Camera mainCamera;
    [SerializeField] private Transform weaponPivot;
    [Header("Weapon Settings")]
    public WeaponData weaponData;

    [Header("Audio & Effects")]
    public AudioClip shootingSound;
    [Range(0.1f, 2f)]
    public float recoilForce = 1f;

    // References found via tags
    private AudioSource audioSource;
    private CinemachineImpulseSource impulseSource;

    private Ray ray;
    private RaycastHit hitInfo;
    private Coroutine fireCoroutine;
    private PlayerDamageFlash damageFlash;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Only proceed if this object belongs to the local player
        if (!IsOwner) return;

        // Find AudioSource on tagged GameObject
        GameObject audioGO = GameObject.FindGameObjectWithTag("WeaponAudio");
        if (audioGO != null)
        {
            audioSource = audioGO.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogWarning("GameObject with tag 'WeaponAudio' found but no AudioSource component!");
            }
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'WeaponAudio' found!");
        }

        // Find CinemachineImpulseSource on tagged GameObject
        GameObject cameraGO = GameObject.FindGameObjectWithTag("Freelook Camera");
        if (cameraGO != null)
        {
            impulseSource = cameraGO.GetComponent<CinemachineImpulseSource>();
            if (impulseSource == null)
            {
                Debug.LogWarning("GameObject with tag 'Freelook Camera' found but no CinemachineImpulseSource component!");
            }
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'Freelook Camera' found!");
        }

        // Ensure that the camera is only set for the local player
        if (mainCamera == null)
        {
            Debug.Log("Assigning camera to local player weapon");

            // Try to find the camera tagged as "MainCamera"
            GameObject camGO = GameObject.FindGameObjectWithTag("MainCamera");
            if (camGO != null)
            {
                Debug.Log("Found camera: " + camGO.name);
                mainCamera = camGO.GetComponent<Camera>();
            }
            else
            {
                Debug.LogError("Camera with tag 'MainCamera' not found in the scene.");
            }

            // If the camera is still null, log an error
            if (mainCamera == null)
            {
                Debug.LogError("Camera is still null on local player!");
            }
        }

        // You can also disable cameras on remote players, if necessary:
        DisableOtherPlayerCameras();
    }
    private void DisableOtherPlayerCameras()
    {
        if (mainCamera != null)
        {
            // Disable all other cameras in the scene that aren't this player's camera
            Camera[] allCameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in allCameras)
            {
                if (cam != mainCamera)  // Disable all cameras except the local player's camera
                {
                    cam.enabled = false;
                }
            }
        }
    }
    public void AssignWeaponDataFrom(GameObject weaponObject)
    {
        var weaponBase = weaponObject.GetComponent<WeaponBase>();
        if (weaponBase != null)
        {
            weaponData = weaponBase.GetData();
            muzzleFlash = weaponBase.muzzleFlashes;
            hitEffect = weaponBase.hitEffect;
            tracerEffect = weaponBase.tracerEffect;
            raycastOrigin = weaponBase.raycastOrigin;
            raycastDestination = weaponBase.crosshairTarget;
            Debug.Log($"✅ WeaponData set: {weaponData.weaponName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Weapon prefab {weaponObject.name} thiếu WeaponBase");
        }
    }

    private Ray GetRayFromCenter()
    {
        // Only allow local client to get camera ray
        if (!IsOwner) 
        {
            Debug.LogWarning("GetRayFromCenter called on non-owner client");
            return new Ray();
        }
        
        if (mainCamera != null)
        {
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            return mainCamera.ScreenPointToRay(screenCenter);
        }
        else
        {
            Debug.LogError("Main camera is null. Cannot get ray from center.");
            return new Ray(); // Return an empty ray if the camera is null
        }
    }

    private void RotateWeaponToCrosshair()
    {
        Ray camRay = GetRayFromCenter();

        Vector3 targetPoint;
        if (Physics.Raycast(camRay, out RaycastHit aimHit, 100f))
        {
            targetPoint = aimHit.point;
        }
        else
        {
            targetPoint = camRay.origin + camRay.direction * 100f;
        }

        Vector3 direction = (targetPoint - weaponPivot.position).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Xoay mềm để không bị giật
        weaponPivot.rotation = Quaternion.Lerp(weaponPivot.rotation, targetRotation, Time.deltaTime * 15f);
    }


    public void StartFiring()
    {
        if (!IsOwner || isFiring) return;
        isFiring = true;

        var myPhaseHealth = GetComponent<PhaseHealth>();
        if (myPhaseHealth != null && myPhaseHealth.IsPlayerDead())
        {
            Debug.Log("[RaycastWeapon] Dead players cannot shoot!");
            return;
        }



        if (NetworkCountdownManager.Instance != null &&
            NetworkCountdownManager.Instance.GetCurrentPhase() != GamePhase.FightPhase)
        {
            Debug.Log("[RaycastWeapon] Can only shoot during Fight Phase!");
            return;
        }
        // Bắn theo mode
        if (weaponData.fireMode == FireMode.Tap)
        {
            fireCoroutine = StartCoroutine(SingleShotRoutine());
        }
        else if (weaponData.fireMode == FireMode.Hold)
        {
            fireCoroutine = StartCoroutine(AutoFireRoutine());
        }
    }

    public void StopFiring()
    {
        if (!IsOwner) return;

        isFiring = false;
        if (fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
        }
    }

    private IEnumerator SingleShotRoutine()
    {
        yield return new WaitForSeconds(weaponData.fireDelay);
        
        // Calculate ray on client side
        Ray clientRay = GetRayFromCenter();
        ShootServerRpc(clientRay.origin, clientRay.direction);
    }

    private IEnumerator AutoFireRoutine()
    {
        yield return new WaitForSeconds(weaponData.fireDelay);

        while (isFiring)
        {
            // Calculate ray on client side
            Ray clientRay = GetRayFromCenter();
            ShootServerRpc(clientRay.origin, clientRay.direction);
            yield return new WaitForSeconds(weaponData.fireRate);
        }
    }


    [ClientRpc]
    private void TriggerHitFlashClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        if (damageFlash == null)
            damageFlash = GetComponentInChildren<PlayerDamageFlash>();

        damageFlash?.Flash();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ShootServerRpc(Vector3 rayOrigin, Vector3 rayDirection, ServerRpcParams rpcParams = default)
    {
        var attackerHealth = GetComponent<PhaseHealth>();
        if (attackerHealth != null && attackerHealth.IsPlayerDead())
        {
            Debug.Log("[RaycastWeapon] Dead attacker attempted to shoot — blocked");
            return;
        }

        // Use the ray data sent from the client
        Ray cameraRay = new Ray(rayOrigin, rayDirection);
        Vector3 aimDirection;

        if (Physics.Raycast(cameraRay, out hitInfo))
        {
            aimDirection = (hitInfo.point - raycastOrigin.position).normalized;
        }
        else
        {
            aimDirection = rayDirection; // Use the client's ray direction
        }
        ray.origin = raycastOrigin.position;
        ray.direction = aimDirection;

        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);

        bool hasHit = Physics.Raycast(ray, out hitInfo);
        Vector3 hitPoint = hasHit ? hitInfo.point : Vector3.zero;
        Vector3 hitNormal = hasHit ? hitInfo.normal : Vector3.zero;


        if (hasHit)
        {
            var targetHealth = hitInfo.collider.GetComponentInParent<PhaseHealth>();

            if (targetHealth != null && targetHealth != attackerHealth)
            {
                if (targetHealth.IsPlayerDead())
                {
                    Debug.Log("[RaycastWeapon] Target already dead — no damage");
                    return;
                }

                if (NetworkCountdownManager.Instance != null &&
                    NetworkCountdownManager.Instance.GetCurrentPhase() == GamePhase.FightPhase)
                {
                    float oldHealth = targetHealth.GetHealthPercentage(); // check pre-damage

                    targetHealth.TakeDamage(10f, OwnerClientId);

                    Debug.Log($"[RaycastWeapon] Dealt 10 damage to {hitInfo.collider.name}");
                    // Trigger flash effect on target client
                    TriggerHitFlashClientRpc(targetHealth.OwnerClientId);

                    // ✅ Check if they died from this shot
                    if (targetHealth.IsPlayerDead())
                    {
                        NetworkCountdownManager.ReportKill(OwnerClientId, targetHealth.OwnerClientId);
                }
                }
                else
                {
                    Debug.Log("[RaycastWeapon] Damage ignored — not in Fight Phase");
                }
            }
        }

        PlayEffectsClientRpc(hasHit, hitPoint, hitNormal);
    }

    private void Update()
    {
        if (!IsOwner) return;

        RotateWeaponToCrosshair();
    }

    [ClientRpc]
    private void PlayEffectsClientRpc(bool hasHit, Vector3 hitPoint, Vector3 hitNormal)
    {
        // Play muzzle flash effects
        foreach (var particle in muzzleFlash)
            particle.Emit(1);

        // Play shooting sound (only for the owner)
        if (IsOwner && audioSource != null && shootingSound != null)
        {
            audioSource.PlayOneShot(shootingSound);
        }

        // Trigger camera shake (only for the owner)
        if (IsOwner && impulseSource != null)
        {
            // Create recoil with both backward and upward movement
            Vector3 recoilDirection = new Vector3(0, 0.2f, -0.5f).normalized; // Up and back
            impulseSource.GenerateImpulse(recoilDirection * recoilForce);
        }

        var tracer = Instantiate(tracerEffect, raycastOrigin.position, Quaternion.identity);
        tracer.AddPosition(raycastOrigin.position);

        if (hasHit)
        {
            tracer.transform.position = hitPoint;
            var effect = Instantiate(hitEffect, hitPoint, Quaternion.LookRotation(hitNormal));
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
            hitEffect.transform.position = hitPoint;
            hitEffect.transform.forward = hitNormal;
            hitEffect.Emit(1);
        }
    }
}

