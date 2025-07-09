using UnityEngine;
using UnityEngine.UI;

public class NpcShopInteraction : MonoBehaviour
{
    [Header("Shop Settings")]
    public GameObject shopPanel; // Drag your ShopPanel here
    public Transform npcShop; // Drag your NpcShop GameObject here
    public Transform player; // Drag your Player GameObject here

    [Header("Interaction Settings")]
    public float interactionDistance = 3f; // Distance to interact
    public Text promptText; // UI Text showing "Press F to buy items"
    public string promptMessage = "Press F to buy items";

    [Header("Optional Settings")]
    public KeyCode interactionKey = KeyCode.F;
    public bool pauseGameWhenShopOpen = true;

    private bool isPlayerNearShop = false;
    private bool isShopOpen = false;

    void Start()
    {
        // Initialize shop as closed
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        // Hide prompt text initially
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }

        // Auto-find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    void Update()
    {
        CheckDistanceToShop();
        HandleInput();
    }

    void CheckDistanceToShop()
    {
        if (player == null || npcShop == null) return;

        float distance = Vector3.Distance(player.position, npcShop.position);
        bool wasNearShop = isPlayerNearShop;
        isPlayerNearShop = distance <= interactionDistance;

        // Show/hide prompt based on distance
        if (isPlayerNearShop && !isShopOpen)
        {
            ShowPrompt();
        }
        else
        {
            HidePrompt();
        }

        // Optional: Close shop if player walks too far away
        if (!isPlayerNearShop && isShopOpen)
        {
            CloseShop();
        }
    }

    void HandleInput()
    {
        // Only allow interaction when near shop and shop is not already open
        if (Input.GetKeyDown(interactionKey))
        {
            if (isPlayerNearShop && !isShopOpen)
            {
                OpenShop();
            }
            else if (isShopOpen)
            {
                CloseShop();
            }
        }

        // Allow ESC to close shop
        if (Input.GetKeyDown(KeyCode.Escape) && isShopOpen)
        {
            CloseShop();
        }
    }

    void ShowPrompt()
    {
        if (promptText != null)
        {
            promptText.text = promptMessage;
            promptText.gameObject.SetActive(true);
        }
    }

    void HidePrompt()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            isShopOpen = true;
            HidePrompt(); // Hide the prompt when shop opens

            // Optional: Pause game and show cursor
            if (pauseGameWhenShopOpen)
            {
                Time.timeScale = 0f;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            isShopOpen = false;

            // Show prompt again if still near shop
            if (isPlayerNearShop)
            {
                ShowPrompt();
            }

            // Optional: Resume game and hide cursor
            if (pauseGameWhenShopOpen)
            {
                Time.timeScale = 1f;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    // Optional: Visualize interaction range in Scene view
    void OnDrawGizmosSelected()
    {
        if (npcShop != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(npcShop.position, interactionDistance);
        }
    }
}