using UnityEngine;

public class ShopPanelController : MonoBehaviour
{
    [Header("Shop Panel")]
    public GameObject shopPanel; 

    private bool isShopOpen = false;

    void Start()
    {
        // Make sure shop panel is closed at start
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            isShopOpen = false;
        }
    }

    void Update()
    {
        // Check for F key press
        //if (Input.GetKeyDown(KeyCode.F))
        //{
        //    ToggleShop();
        //}
    }

    public void ToggleShop()
    {
        if (shopPanel != null)
        {
            isShopOpen = !isShopOpen;
            shopPanel.SetActive(isShopOpen);

            // Optional: Pause/unpause game when shop is open
            // Time.timeScale = isShopOpen ? 0f : 1f;

            // Optional: Show/hide cursor
            // Cursor.visible = isShopOpen;
            // Cursor.lockState = isShopOpen ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            isShopOpen = false;

            // Optional: Resume game
            // Time.timeScale = 1f;

            // Optional: Hide cursor
            // Cursor.visible = false;
            // Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            isShopOpen = true;

            // Optional: Pause game
            // Time.timeScale = 0f;

            // Optional: Show cursor
            // Cursor.visible = true;
            // Cursor.lockState = CursorLockMode.None;
        }
    }
}