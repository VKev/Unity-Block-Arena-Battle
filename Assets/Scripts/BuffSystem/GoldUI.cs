using UnityEngine;
using TMPro;

public class GoldUI : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;


    private void Awake()
    {
        goldText.gameObject.SetActive(false);
    }

    private void UpdateGoldDisplay(int goldAmount)
    {
        goldText.gameObject.SetActive(true);
        if (goldText != null)
        {
            goldText.text = $"Gold: {goldAmount}";
        }
    }

    private void OnEnable()
    {
        GameEvents.OnGoldChanged += UpdateGoldDisplay;
    }

    private void OnDisable()
    {
        GameEvents.OnGoldChanged -= UpdateGoldDisplay;
    }

    
}
