using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    public void SetFill(float percent)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(percent);
        }
    }
}