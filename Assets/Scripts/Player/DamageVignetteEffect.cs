using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Netcode;
using System.Collections;
using PlayerStateMachine;

public class DamageVignetteEffect : NetworkBehaviour
{
    [Header("Damage Vignette Settings")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color speedReductionColor = Color.blue;
    [SerializeField] private float maxVignetteIntensity = 0.8f;
    [SerializeField] private float fadeInSpeed = 5f;
    [SerializeField] private float fadeOutSpeed = 2f;
    [SerializeField] private float healthThresholdForVignette = 0.8f;

    private Vignette vignette;
    private bool isInitialized = false;
    private float targetIntensity = 0f;
    private Coroutine vignetteCoroutine;
    private bool isSpeedReductionActive = false;
    private Color originalVignetteColor;

    void Start()
    {
        // Only run on the owner client
        if (!IsOwner)
        {
            Debug.Log($"[DamageVignette] Not owner, disabling component");
            enabled = false;
            return;
        }

        Debug.Log($"[DamageVignette] Initializing for owner client");
        InitializeVignette();
        
        // Subscribe to phase changes to reset vignette when fight phase ends
        NetworkCountdownManager.OnPhaseChanged += OnPhaseChanged;
    }

    void InitializeVignette()
    {
        // Find post process volume if not assigned
        if (postProcessVolume == null)
        {
            postProcessVolume = FindObjectOfType<Volume>();
        }

        if (postProcessVolume == null)
        {
            Debug.LogError("[DamageVignette] No Volume (Post Process Volume) found in scene!");
            return;
        }

        Debug.Log($"[DamageVignette] Found post process volume: {postProcessVolume.name}");

        // Get or add vignette effect
        if (postProcessVolume.profile.TryGet<Vignette>(out vignette))
        {
            Debug.Log("[DamageVignette] Found existing Vignette effect");
        }
        else
        {
            vignette = postProcessVolume.profile.Add<Vignette>(false);
            Debug.Log("[DamageVignette] Added new Vignette effect to post process profile");
        }

        // Configure vignette
        if (vignette != null)
        {
            vignette.color.value = damageColor;
            originalVignetteColor = damageColor;
            vignette.intensity.value = 0f;
            vignette.smoothness.value = 0.3f;
            vignette.rounded.value = true;
            vignette.active = true;
            isInitialized = true;
            Debug.Log($"[DamageVignette] Vignette configured successfully - Active: {vignette.active}, Color: {vignette.color.value}, Initial Intensity: {vignette.intensity.value}");
        }
    }

    void Update()
    {
        if (!isInitialized || !IsOwner) return;

        // Smoothly interpolate vignette intensity
        if (vignette != null)
        {
            float currentIntensity = vignette.intensity.value;
            float speed = targetIntensity > currentIntensity ? fadeInSpeed : fadeOutSpeed;
            
            float newIntensity = Mathf.MoveTowards(
                currentIntensity, 
                targetIntensity, 
                speed * Time.deltaTime
            );
            
            // Debug the intensity changes
            if (Mathf.Abs(newIntensity - currentIntensity) > 0.001f || targetIntensity > 0)
            {
                Debug.Log($"[DamageVignette] Intensity: {currentIntensity:F3} → {newIntensity:F3} (Target: {targetIntensity:F3})");
            }
            
            vignette.intensity.value = newIntensity;
        }
    }

    public void TriggerDamageEffect(float currentHealth, float maxHealth)
    {
        Debug.Log($"[DamageVignette] TriggerDamageEffect called - Health: {currentHealth}/{maxHealth}, Initialized: {isInitialized}, IsOwner: {IsOwner}");
        
        if (!isInitialized || !IsOwner) return;

        // Flash effect for immediate damage feedback
        if (vignetteCoroutine != null)
        {
            StopCoroutine(vignetteCoroutine);
        }
        vignetteCoroutine = StartCoroutine(DamageFlashCoroutine());

        // Set persistent vignette based on health
        float healthPercentage = currentHealth / maxHealth;
        UpdateHealthVignette(healthPercentage);
        
        Debug.Log($"[DamageVignette] Damage effect triggered, health percentage: {healthPercentage}");
    }

    private IEnumerator DamageFlashCoroutine()
    {
        Debug.Log($"[DamageVignette] Starting damage flash - setting intensity to max: {maxVignetteIntensity}");
        
        // Quick flash effect - use full intensity for maximum impact
        float originalTarget = targetIntensity;
        targetIntensity = 0.8f;  // Strong flash effect
        
        yield return new WaitForSeconds(0.15f);  // Slightly longer flash for better visibility
        
        targetIntensity = originalTarget;
        Debug.Log($"[DamageVignette] Flash complete - returning to target: {originalTarget:F3}");
    }

    public void UpdateHealthVignette(float healthPercentage)
    {
        if (!isInitialized || !IsOwner) return;

        Debug.Log($"[DamageVignette] UpdateHealthVignette called with health percentage: {healthPercentage}");

        if (healthPercentage <= healthThresholdForVignette)
        {
            // Calculate vignette intensity based on how low health is
            float vignetteAmount = Mathf.InverseLerp(healthThresholdForVignette, 0f, healthPercentage);
            
            // Ensure minimum visibility and stronger effect
            float minIntensity = 0.3f;  // Minimum 30% intensity when health is low
            float maxIntensity = 0.7f;  // Maximum 70% intensity when health is critical
            
            targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, vignetteAmount);
            
            Debug.Log($"[DamageVignette] Health below threshold, setting target intensity to: {targetIntensity:F3} (vignetteAmount: {vignetteAmount:F3})");
        }
        else
        {
            targetIntensity = 0f;
            Debug.Log($"[DamageVignette] Health above threshold, clearing vignette");
        }
    }

    public void ClearVignette()
    {
        Debug.Log("[DamageVignette] Clearing vignette to normal state");
        
        targetIntensity = 0f;
        isSpeedReductionActive = false;
        
        if (vignette != null)
        {
            vignette.color.value = originalVignetteColor;
            vignette.intensity.value = 0f; // Immediately set to 0 for instant clear
        }
        
        // Stop any ongoing vignette animations
        if (vignetteCoroutine != null)
        {
            StopCoroutine(vignetteCoroutine);
            vignetteCoroutine = null;
        }
    }

    public void TriggerSpeedReductionEffect()
    {
        Debug.Log($"[DamageVignette] Speed reduction effect triggered - turning vignette blue");
        
        if (!isInitialized || !IsOwner) return;

        isSpeedReductionActive = true;
        
        if (vignette != null)
        {
            vignette.color.value = speedReductionColor;
            targetIntensity = 0.5f; // Moderate blue vignette intensity
        }

        // Flash effect for immediate feedback
        if (vignetteCoroutine != null)
        {
            StopCoroutine(vignetteCoroutine);
        }
        vignetteCoroutine = StartCoroutine(SpeedReductionFlashCoroutine());
    }

    private IEnumerator SpeedReductionFlashCoroutine()
    {
        Debug.Log($"[DamageVignette] Starting speed reduction flash - blue vignette");
        
        // Quick blue flash effect
        float originalTarget = targetIntensity;
        targetIntensity = 0.7f; // Strong blue flash
        
        yield return new WaitForSeconds(0.2f);
        
        targetIntensity = 0.5f; // Return to moderate blue intensity
        Debug.Log($"[DamageVignette] Speed reduction flash complete");
    }

    public void ClearSpeedReductionEffect()
    {
        Debug.Log($"[DamageVignette] Speed reduction ended - clearing blue vignette");
        
        if (!isInitialized || !IsOwner) return;

        isSpeedReductionActive = false;
        
        if (vignette != null)
        {
            vignette.color.value = originalVignetteColor; // Return to red/normal color
        }
        
        // Clear vignette intensity unless health-based vignette should be active
        var phaseHealth = GetComponent<PhaseHealth>();
        if (phaseHealth != null)
        {
            UpdateHealthVignette(phaseHealth.GetHealthPercentage());
        }
        else
        {
            targetIntensity = 0f;
        }
    }

    void OnDestroy()
    {
        if (vignetteCoroutine != null)
        {
            StopCoroutine(vignetteCoroutine);
        }
        
        // Unsubscribe from phase changes to prevent memory leaks
        NetworkCountdownManager.OnPhaseChanged -= OnPhaseChanged;
    }
    
    private void OnPhaseChanged(GamePhase newPhase)
    {
        if (!isInitialized || !IsOwner) return;
        
        Debug.Log($"[DamageVignette] Phase changed to {newPhase}");
        
        // Reset vignette to default when fighting phase ends
        if (newPhase != GamePhase.FightPhase)
        {
            Debug.Log($"[DamageVignette] Fight phase ended, resetting vignette to normal state");
            ClearVignette();
            
            // Force complete reset of all vignette effects
            if (vignette != null)
            {
                vignette.intensity.value = 0f;
                vignette.color.value = originalVignetteColor;
            }
            
            Debug.Log($"[DamageVignette] Vignette completely reset to normal");
        }
    }

    // Test function - call this from inspector or debug console
    [ContextMenu("Test Vignette Effect")]
    public void TestVignetteEffect()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[DamageVignette] Not initialized, cannot test");
            return;
        }
        
        Debug.Log("[DamageVignette] TESTING: Setting vignette intensity to max for 2 seconds");
        StartCoroutine(TestVignetteCoroutine());
    }
    
    private IEnumerator TestVignetteCoroutine()
    {
        float originalTarget = targetIntensity;
        targetIntensity = maxVignetteIntensity;
        
        yield return new WaitForSeconds(2f);
        
        targetIntensity = originalTarget;
        Debug.Log("[DamageVignette] Test complete");
    }
} 