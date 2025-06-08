using UnityEngine;
using UnityEngine.UI;

namespace PlayerStateMachine
{
    public class WorldPhaseHealthUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 offset;


        public void SetHealth(float percent)
        {
            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(percent);
        }

        private void Awake()
        {
            if (fillImage == null)
            {
                Transform fill = transform.Find("HealthBarContainer/HealthBarBackground/HealthBarFill");
                fillImage = fill?.GetComponent<Image>();

                if (fillImage == null)
                    Debug.LogError("[WorldPhaseHealthUI] Could not find fillImage.");
            }
        }
    }
}