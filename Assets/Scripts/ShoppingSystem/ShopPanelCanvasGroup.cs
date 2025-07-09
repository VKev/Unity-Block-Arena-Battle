using UnityEngine;

public class ShopPanelCanvasGroup : MonoBehaviour
{
    [Header("Shop Panel")]
    public CanvasGroup shopCanvasGroup;

    [Header("Animation Settings")]
    public float fadeSpeed = 5f;

    private bool isShopOpen = false;
    private bool isAnimating = false;

    void Start()
    {
        // Make sure shop panel is closed at start
        if (shopCanvasGroup != null)
        {
            shopCanvasGroup.alpha = 0f;
            shopCanvasGroup.interactable = false;
            shopCanvasGroup.blocksRaycasts = false;
            isShopOpen = false;
        }
    }

    void Update()
    {
        // Check for F key press
        //if (Input.GetKeyDown(KeyCode.F) && !isAnimating)
        //{
        //    ToggleShop();
        //}

        // Handle fade animation
        if (shopCanvasGroup != null && isAnimating)
        {
            float targetAlpha = isShopOpen ? 1f : 0f;
            shopCanvasGroup.alpha = Mathf.MoveTowards(shopCanvasGroup.alpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);

            // Check if animation is complete
            if (Mathf.Approximately(shopCanvasGroup.alpha, targetAlpha))
            {
                isAnimating = false;
                shopCanvasGroup.interactable = isShopOpen;
                shopCanvasGroup.blocksRaycasts = isShopOpen;
            }
        }
    }

    public void ToggleShop()
    {
        if (shopCanvasGroup != null && !isAnimating)
        {
            isShopOpen = !isShopOpen;
            isAnimating = true;

            if (isShopOpen)
            {
                shopCanvasGroup.interactable = true;
                shopCanvasGroup.blocksRaycasts = true;
            }
        }
    }

    public void CloseShop()
    {
        if (shopCanvasGroup != null && isShopOpen)
        {
            isShopOpen = false;
            isAnimating = true;
        }
    }

    public void OpenShop()
    {
        if (shopCanvasGroup != null && !isShopOpen)
        {
            isShopOpen = true;
            isAnimating = true;
            shopCanvasGroup.interactable = true;
            shopCanvasGroup.blocksRaycasts = true;
        }
    }
}