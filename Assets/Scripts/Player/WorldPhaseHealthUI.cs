using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerStateMachine
{
    public class WorldPhaseHealthUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Image extraHPFillImage;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 offset;
        [SerializeField] private TextMeshProUGUI numberText;

        public void SetHealth(float percent)
        {
            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(percent);
        }

        public void SetExtraHP(float percent)
        {
            float clampedPercent = Mathf.Clamp01(percent);
            Debug.Log($"[WorldPhaseHealthUI] SetExtraHP called with {percent} -> clamped to {clampedPercent}");
            
            if (extraHPFillImage != null)
            {
                extraHPFillImage.fillAmount = clampedPercent;
                Debug.Log($"[WorldPhaseHealthUI] ExtraHP fill amount set to {clampedPercent} on {extraHPFillImage.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[WorldPhaseHealthUI] extraHPFillImage is null! Cannot update Extra HP fill.");
            }
        }

        public void SetNumber(string text)
        {
            if (numberText != null)
                numberText.text = text;
        }

        private void Awake()
        {
            Debug.Log($"[WorldPhaseHealthUI] Awake called on: {gameObject.name}");
            
            if (fillImage == null)
            {
                Transform fill = transform.Find("HealthBarContainer/HealthBarBackground/HealthBarFill");
                fillImage = fill?.GetComponent<Image>();
                Debug.Log($"[WorldPhaseHealthUI] Found fillImage: {fillImage?.gameObject.name ?? "NULL"}");

                if (fillImage == null)
                    Debug.LogError("[WorldPhaseHealthUI] Could not find fillImage.");
            }

            if (extraHPFillImage == null)
            {
                Transform extraFill = transform.Find("HealthBarContainer/HealthBarBackground/ExtraHPFill");
                extraHPFillImage = extraFill?.GetComponent<Image>();
                Debug.Log($"[WorldPhaseHealthUI] Found extraHPFillImage: {extraHPFillImage?.gameObject.name ?? "NULL"}");

                if (extraHPFillImage == null)
                {
                    Debug.LogWarning("[WorldPhaseHealthUI] Could not find extraHPFillImage. Searching for alternative names...");
                    
                    // Try alternative paths/names
                    extraFill = transform.Find("HealthBarContainer/HealthBarBackground/FakeHP");
                    extraHPFillImage = extraFill?.GetComponent<Image>();
                    Debug.Log($"[WorldPhaseHealthUI] Alternative search found: {extraHPFillImage?.gameObject.name ?? "NULL"}");
                    
                    if (extraHPFillImage == null)
                        Debug.LogWarning("[WorldPhaseHealthUI] Still could not find extraHPFillImage. Make sure to add ExtraHPFill Image as child of HealthBarBackground.");
                }
            }

            if (numberText == null)
            {
                Transform txt = transform.Find("HealthBarContainer/PhaseNumberText");
                numberText = txt?.GetComponent<TextMeshProUGUI>();
                Debug.Log($"[WorldPhaseHealthUI] Found numberText: {numberText?.gameObject.name ?? "NULL"}");

                if (numberText == null)
                    Debug.LogError("[WorldPhaseHealthUI] Could not find numberText.");
            }
            
            Debug.Log($"[WorldPhaseHealthUI] Setup complete - fillImage: {fillImage != null}, extraHPFillImage: {extraHPFillImage != null}, numberText: {numberText != null}");
        }
    }
}