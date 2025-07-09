using UnityEngine;
using Unity.Netcode;
using PlayerStateMachine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerZoneDamage : NetworkBehaviour
{
    private float checkInterval = 1f;
    private float nextCheckTime = 0f;
    private PlayerDamageFlash damageFlash;
    
    [Header("Zone Visual Effects")]
    private Volume globalVolume;
    private ColorAdjustments colorAdjustments;
    private Camera mainCamera;
    private readonly Color outsideZoneColor = new Color(149f/255f, 149f/255f, 255f/255f); // Blue tint
    private readonly Color insideZoneColor = new Color(255f/255f, 255f/255f, 255f/255f); // White (normal)

    private void Start()
    {
        damageFlash = GetComponentInChildren<PlayerDamageFlash>();
        
        // Only setup volume effects for the local player (owner)
        if (IsOwner)
        {
            SetupVolumeEffects();
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[PlayerZoneDamage] Main camera not found for zone visual effects");
            }
        }
    }

    private void Update()
    {
        if (!IsOwner || ChangeCircle.Instance == null)
            return;

        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;

            float xRad = ChangeCircle.Instance.GetXRadius();
            float yRad = ChangeCircle.Instance.GetYRadius();
            Vector3 center = ChangeCircle.Instance.transform.position;

            // Calculate player position relative to circle center (for damage)
            Vector2 playerOffset = new Vector2(transform.position.x - center.x, transform.position.z - center.z);
            
            // Calculate normalized distance (ellipse formula) for player damage
            float norm = (playerOffset.x * playerOffset.x) / (xRad * xRad) + (playerOffset.y * playerOffset.y) / (yRad * yRad);
            Debug.Log($"[Zone Debug] Player {OwnerClientId} - Norm: {norm:F2}");
            
            // Apply visual effects based on CAMERA position (for local player only)
            if (IsOwner && mainCamera != null)
            {
                // Calculate camera position relative to circle center
                Vector2 cameraOffset = new Vector2(mainCamera.transform.position.x - center.x, mainCamera.transform.position.z - center.z);
                float cameraNorm = (cameraOffset.x * cameraOffset.x) / (xRad * xRad) + (cameraOffset.y * cameraOffset.y) / (yRad * yRad);
                
                UpdateZoneVisualEffects(cameraNorm > 3.9f);
                Debug.Log($"[Zone Debug] Camera Norm: {cameraNorm:F2} - Tint: {(cameraNorm > 4f ? "Blue" : "Normal")}");
            }
            
            // Only damage if player is OUTSIDE the safe zone (norm > 1f means outside circle)
            if (norm > 4f )
            {
                // Check if we're in fight phase and the circle is shrinking
                // Use fallback to ChangeCircle's networked phase if NetworkCountdownManager is unavailable
                GamePhase currentPhase = NetworkCountdownManager.Instance != null ? 
                    NetworkCountdownManager.Instance.GetCurrentPhase() : 
                    ChangeCircle.Instance.GetCurrentPhase();
                
                bool isShrinking = ChangeCircle.Instance.IsShrinking();
                
                // Debug logging to help identify issues
                Debug.Log($"[Zone Debug] Player {OwnerClientId} - Phase: {currentPhase}, IsShrinking: {isShrinking}, Norm: {norm:F2}");
                
                if (currentPhase == GamePhase.FightPhase && isShrinking)
                {
                    float damage = GetZoneDamageBasedOnCircleSize(xRad, yRad);
                    ApplyZoneDamageServerRpc(damage);
                    damageFlash?.Flash();
                    
                    Debug.Log($"[Zone] Player {OwnerClientId} is OUTSIDE safe zone (norm: {norm:F2}) - taking {damage} damage");
                }
                else
                {
                    Debug.Log($"[Zone] Player {OwnerClientId} is outside zone but not taking damage - Phase: {currentPhase}, Shrinking: {isShrinking}");
                }
            }
            else
            {
                // Player is INSIDE the safe zone - no damage
                // Uncomment for debugging: Debug.Log($"[Zone] Player {OwnerClientId} is INSIDE safe zone (norm: {norm:F2}) - safe!");
            }
        }
    }

    private float GetZoneDamageBasedOnCircleSize(float currentXRadius, float currentYRadius)
    {
        // Calculate damage based on current circle size
        // Smaller circle = more damage
        float currentRadius = Mathf.Max(currentXRadius, currentYRadius);
        
        // Reduced damage values - much more reasonable
        if (currentRadius > 2000f)
            return 1f;   // Large circle - very low damage
        else if (currentRadius > 1000f)
            return 2f;   // Medium circle - low damage
        else if (currentRadius > 500f)
            return 3f;   // Small circle - medium damage
        else
            return 5f;   // Very small circle - high damage
    }

    [ServerRpc]
    private void ApplyZoneDamageServerRpc(float amount)
    {
        if (TryGetComponent(out PhaseHealth health))
        {
            health.TakeDamage(amount);
            Debug.Log($"[Zone] Player {OwnerClientId} took {amount} damage (outside safezone)");
        }
    } 
    
    private void SetupVolumeEffects()
    {
        // Find the global volume in the scene
        globalVolume = FindObjectOfType<Volume>();
        
        if (globalVolume == null)
        {
            Debug.LogWarning("[PlayerZoneDamage] No Volume found in scene for visual effects");
            return;
        }
        
        // Get or add ColorAdjustments component
        if (globalVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            Debug.Log("[PlayerZoneDamage] Found existing ColorAdjustments in volume profile");
        }
        else
        {
            Debug.LogWarning("[PlayerZoneDamage] No ColorAdjustments found in volume profile. Please add ColorAdjustments to the volume profile for zone visual effects.");
        }
    }
    
    private void UpdateZoneVisualEffects(bool isOutsideZone)
    {
        if (colorAdjustments == null) return;
        
        // Set color filter based on zone position
        Color targetColor = isOutsideZone ? outsideZoneColor : insideZoneColor;
        
        // Apply the color filter
        colorAdjustments.colorFilter.value = targetColor;
        colorAdjustments.colorFilter.overrideState = true;
        
        // Optional: Add some debug logging
        if (isOutsideZone)
        {
            Debug.Log($"[PlayerZoneDamage] Applied blue tint - Player outside safe zone");
        }
    }
}
