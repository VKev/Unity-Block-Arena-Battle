//using UnityEngine;
//using UnityEngine.UI;
//using Unity.Netcode;
//using TMPro;
//using System.Linq;

//public class PlayerShopInteraction : NetworkBehaviour
//{
//    [Header("Interaction Settings")]
//    public float interactionDistance = 3f;
//    public string promptMessage = "Press F to buy items";
//    public KeyCode interactionKey = KeyCode.F;
//    private GameObject shopPanel;
//    private Transform npcShop;
//    private Text promptText;
//    private ShopManager shopManager;
//    //private PlayerInventory playerInventory;

//    private bool isPlayerNearShop = false;
//    private bool isShopOpen = false;

//    public override void OnNetworkSpawn()
//    {
//        base.OnNetworkSpawn();
//        if (!IsOwner) return;
//        StartCoroutine(SetupAfterSpawn());
//    }

//    System.Collections.IEnumerator SetupAfterSpawn()
//    {
//        yield return new WaitForEndOfFrame();

//        Debug.Log($"[Shop] Setting up for player: {gameObject.name}, IsOwner: {IsOwner}");

//        // Find scene objects
//        FindSceneReferences();

//        // Hide UI initially
//        if (shopPanel != null)
//            shopPanel.SetActive(false);
//        if (promptText != null)
//            promptText.gameObject.SetActive(false);
//    }

//    void FindSceneReferences()
//    {
//        Debug.Log("[Shop] Finding references...");
//        shopPanel = GameObject.FindGameObjectWithTag("ShopPanel");
//        if (shopPanel == null)
//        {
//            Debug.Log("[Shop] ShopPanel not found with GameObject.Find, trying Canvas...");

//            GameObject canvas = GameObject.Find("Canvas");
//            if (canvas != null)
//            {
//                Debug.Log("[Shop] Canvas found, searching for ShopPanel inside...");
//                Transform shopTransform = canvas.transform.Find("ShopPanel");
//                if (shopTransform != null)
//                {
//                    shopPanel = shopTransform.gameObject;
//                    Debug.Log("[Shop] ShopPanel found inside Canvas!");
//                }
//                else
//                {
//                    shopTransform = canvas.transform.GetComponentInChildren<Transform>()
//                        .Cast<Transform>()
//                        .FirstOrDefault(t => t.name == "ShopPanel");
//                    if (shopTransform != null)
//                        shopPanel = shopTransform.gameObject;
//                }
//            }
//        }

//        Debug.Log($"[Shop] ShopPanel found: {shopPanel != null} - {(shopPanel != null ? shopPanel.name : "NULL")}");

//        GameObject npcShopObj = GameObject.Find("Shop");
//        Debug.Log($"[Shop] NpcShop found: {npcShopObj != null}");

//        if (npcShopObj != null)
//        {
//            npcShop = npcShopObj.transform;
//            shopManager = npcShopObj.GetComponent<ShopManager>();
//            Debug.Log($"[Shop] ShopManager component: {shopManager != null}");

//            Component[] components = npcShopObj.GetComponents<Component>();
//            Debug.Log($"[Shop] Components on NpcShop:");
//            foreach (var comp in components)
//            {
//                Debug.Log($"  - {comp.GetType().Name}");
//            }
//        }
//        else
//        {
//            Debug.LogError("[Shop] NpcShop not found in scene!");
//        }

//        Debug.Log($"[Shop] Setup complete - Panel: {shopPanel != null}, Shop: {npcShop != null}, Manager: {shopManager != null}");
//    }

//    void Update()
//    {
//        if (!IsOwner || !IsSpawned) return;

//        CheckDistanceToShop();
//        HandleInput();
//    }

//    void CheckDistanceToShop()
//    {
//        if (npcShop == null) return;

//        float distance = Vector3.Distance(transform.position, npcShop.position);
//        isPlayerNearShop = distance <= interactionDistance;

//        //if (isPlayerNearShop && !isShopOpen)
//        //    ShowPrompt();
//        //else
//        //    HidePrompt();

//        if (!isPlayerNearShop && isShopOpen)
//            CloseShop();
//    }

//    void HandleInput()
//    {
//        if (Input.GetKeyDown(interactionKey))
//        {
//            if (isPlayerNearShop && !isShopOpen)
//                OpenShop();
//            else if (isShopOpen)
//                CloseShop();
//        }

//        if (Input.GetKeyDown(KeyCode.Escape) && isShopOpen)
//            CloseShop();

//        //if (Input.GetKeyDown(KeyCode.Tab) && !isShopOpen)
//        //{
//        //    playerInventory.ToggleInventory();
//        //}
//    }

//    //void ShowPrompt()
//    //{
//    //    if (promptText != null)
//    //    {
//    //        promptText.text = promptMessage;
//    //        promptText.gameObject.SetActive(true);
//    //    }
//    //}

//    //void HidePrompt()
//    //{
//    //    if (promptText != null)
//    //        promptText.gameObject.SetActive(false);
//    //}

//    void OpenShop()
//    {
//        if (shopPanel == null || shopManager == null)
//        {
//            Debug.LogError($"[Shop] Cannot open - Panel: {shopPanel != null}, Manager: {shopManager != null}");
//            return;
//        }

//        shopPanel.SetActive(true);
//        isShopOpen = true;
//        //HidePrompt();

//        // Connect shop and inventory
//        //shopManager.SetCurrentPlayer(playerInventory);
//        //playerInventory.OnShopOpened(shopManager);

//        // Show cursor
//        Cursor.visible = true;
//        Cursor.lockState = CursorLockMode.None;

//        Debug.Log("[Shop] Opened successfully!");
//    }

//    void CloseShop()
//    {
//        if (shopPanel != null)
//        {
//            shopPanel.SetActive(false);
//            isShopOpen = false;

//            //if (isPlayerNearShop)
//            //    ShowPrompt();

//            // Hide cursor
//            Cursor.visible = false;
//            Cursor.lockState = CursorLockMode.Locked;

//            // Disconnect references
//            //if (shopManager != null)
//            //    shopManager.SetCurrentPlayer(null);
//            //if (playerInventory != null)
//            //    playerInventory.SetCurrentShop(null);
//        }
//    }

//    void OnDrawGizmosSelected()
//    {
//        if (npcShop != null)
//        {
//            Gizmos.color = Color.yellow;
//            Gizmos.DrawWireSphere(npcShop.position, interactionDistance);
//        }
//    }
//}


using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using System.Linq;
using playerStat;

public class PlayerShopInteraction : NetworkBehaviour
{
    [Header("Interaction Settings")]
    public float interactionDistance = 3f;
    public string promptMessage = "Press F to buy items";
    public KeyCode interactionKey = KeyCode.F;

    [Header("Debug Settings")]
    public bool enableDebug = true;
    public bool showDistanceInUpdate = false;

    private GameObject shopPanel;
    private Transform npcShop;
    private Text promptText;
    private ShopManager shopManager;
    //private PlayerInventory playerInventory;

    private bool isPlayerNearShop = false;
    private bool isShopOpen = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) return;

        DebugLog($"OnNetworkSpawn called for {gameObject.name}");
        StartCoroutine(SetupAfterSpawn());

        //var playerStats = FindObjectOfType<PlayerBaseStats>();
        //if (playerStats != null)
        //{
        //    Debug.Log($"Found PlayerBaseStats on: {playerStats.gameObject.name}");
        //}
    }

    System.Collections.IEnumerator SetupAfterSpawn()
    {
        yield return new WaitForEndOfFrame();

        DebugLog($"Setting up for player: {gameObject.name}, IsOwner: {IsOwner}");

        // Find scene objects
        FindSceneReferences();

        // Hide UI initially
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            DebugLog("ShopPanel hidden initially");
        }
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
            DebugLog("PromptText hidden initially");
        }
    }

    void FindSceneReferences()
    {
        DebugLog("=== FINDING SCENE REFERENCES ===");

        // Find ShopPanel with multiple methods
        FindShopPanel();

        // Find PromptText
        FindPromptText();

        // Find Shop object and ShopManager
        FindShopAndManager();

        DebugLog($"=== SETUP COMPLETE ===");
        DebugLog($"Panel: {shopPanel?.name ?? "NULL"}");
        DebugLog($"PromptText: {promptText?.name ?? "NULL"}");
        DebugLog($"Shop: {npcShop?.name ?? "NULL"}");
        DebugLog($"Manager: {shopManager?.name ?? "NULL"}");
    }

    void FindShopPanel()
    {
        DebugLog("--- Finding ShopPanel ---");

        // Method 1: Find by tag
        shopPanel = GameObject.FindGameObjectWithTag("ShopPanel");
        if (shopPanel != null)
        {
            DebugLog($"✓ ShopPanel found by tag: {shopPanel.name}");
            ValidateShopPanel();
            return;
        }

        DebugLog("✗ ShopPanel not found by tag, trying other methods...");

        // Method 2: Find by name
        shopPanel = GameObject.Find("ShopPanel");
        if (shopPanel != null)
        {
            DebugLog($"✓ ShopPanel found by name: {shopPanel.name}");
            ValidateShopPanel();
            return;
        }

        DebugLog("✗ ShopPanel not found by name, searching in Canvas...");

        // Method 3: Search in all Canvases
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        DebugLog($"Found {canvases.Length} Canvas objects in scene");

        foreach (Canvas canvas in canvases)
        {
            DebugLog($"Searching in Canvas: {canvas.name}");

            // Direct child search
            Transform shopTransform = canvas.transform.Find("ShopPanel");
            if (shopTransform != null)
            {
                shopPanel = shopTransform.gameObject;
                DebugLog($"✓ ShopPanel found as child of {canvas.name}");
                ValidateShopPanel();
                return;
            }

            // Deep search in canvas
            Transform[] allChildren = canvas.GetComponentsInChildren<Transform>();
            foreach (Transform child in allChildren)
            {
                if (child.name == "ShopPanel")
                {
                    shopPanel = child.gameObject;
                    DebugLog($"✓ ShopPanel found deep in {canvas.name} hierarchy");
                    ValidateShopPanel();
                    return;
                }
            }
        }

        DebugError("✗ ShopPanel not found anywhere in scene!");

        // List all GameObjects with "Panel" in name for debugging
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        DebugLog("Objects with 'Panel' in name:");
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Panel"))
            {
                DebugLog($"  - {obj.name} (Tag: {obj.tag})");
            }
        }
    }

    void ValidateShopPanel()
    {
        if (shopPanel == null) return;

        DebugLog($"--- Validating ShopPanel: {shopPanel.name} ---");
        DebugLog($"Active Self: {shopPanel.activeSelf}");
        DebugLog($"Active In Hierarchy: {shopPanel.activeInHierarchy}");
        DebugLog($"Tag: {shopPanel.tag}");
        DebugLog($"Layer: {shopPanel.layer}");
        DebugLog($"Parent: {shopPanel.transform.parent?.name ?? "ROOT"}");

        // Check Canvas
        Canvas parentCanvas = shopPanel.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            DebugLog($"Parent Canvas: {parentCanvas.name}");
            DebugLog($"Canvas Render Mode: {parentCanvas.renderMode}");
            DebugLog($"Canvas Sort Order: {parentCanvas.sortingOrder}");
            DebugLog($"Canvas Active: {parentCanvas.gameObject.activeInHierarchy}");
        }
        else
        {
            DebugWarning("ShopPanel has no parent Canvas!");
        }

        // Check CanvasGroup
        CanvasGroup canvasGroup = shopPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            DebugLog($"CanvasGroup - Alpha: {canvasGroup.alpha}, Interactable: {canvasGroup.interactable}, BlocksRaycasts: {canvasGroup.blocksRaycasts}");
        }

        // List child objects
        DebugLog($"ShopPanel has {shopPanel.transform.childCount} children:");
        for (int i = 0; i < shopPanel.transform.childCount; i++)
        {
            Transform child = shopPanel.transform.GetChild(i);
            DebugLog($"  - {child.name} (Active: {child.gameObject.activeSelf})");
        }
    }

    void FindPromptText()
    {
        DebugLog("--- Finding PromptText ---");

        // Try to find Text component
        Text[] textComponents = FindObjectsOfType<Text>();
        DebugLog($"Found {textComponents.Length} Text components in scene");

        foreach (Text text in textComponents)
        {
            if (text.name.Contains("Prompt") || text.name.Contains("Interaction"))
            {
                promptText = text;
                DebugLog($"✓ PromptText found: {text.name}");
                break;
            }
        }

        if (promptText == null)
        {
            DebugWarning("✗ PromptText not found, interaction prompts will not show");
        }
    }

    void FindShopAndManager()
    {
        DebugLog("--- Finding Shop and ShopManager ---");

        GameObject npcShopObj = GameObject.Find("Shop");
        if (npcShopObj != null)
        {
            npcShop = npcShopObj.transform;
            DebugLog($"✓ Shop object found: {npcShopObj.name} at position {npcShop.position}");

            shopManager = npcShopObj.GetComponent<ShopManager>();
            if (shopManager != null)
            {
                DebugLog($"✓ ShopManager component found");
            }
            else
            {
                DebugError("✗ ShopManager component not found on Shop object!");
            }

            // List all components on Shop object
            Component[] components = npcShopObj.GetComponents<Component>();
            DebugLog($"Shop object has {components.Length} components:");
            foreach (var comp in components)
            {
                DebugLog($"  - {comp.GetType().Name}");
            }
        }
        else
        {
            DebugError("✗ Shop object not found in scene!");

            // List objects with "Shop" in name
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            DebugLog("Objects with 'Shop' in name:");
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.ToLower().Contains("shop"))
                {
                    DebugLog($"  - {obj.name}");
                }
            }
        }
    }

    void Update()
    {
        if (!IsOwner || !IsSpawned) return;

        CheckDistanceToShop();
        HandleInput();
    }

    void CheckDistanceToShop()
    {
        if (npcShop == null) return;

        float distance = Vector3.Distance(transform.position, npcShop.position);
        bool wasNearShop = isPlayerNearShop;
        isPlayerNearShop = distance <= interactionDistance;

        // Debug distance if enabled
        if (showDistanceInUpdate)
        {
            DebugLog($"Distance to shop: {distance:F2}, Near: {isPlayerNearShop}, Open: {isShopOpen}");
        }

        // Log when near state changes
        if (wasNearShop != isPlayerNearShop)
        {
            DebugLog($"Player near shop changed: {wasNearShop} -> {isPlayerNearShop} (Distance: {distance:F2})");
        }

        if (!isPlayerNearShop && isShopOpen)
        {
            DebugLog("Player moved away from shop, closing...");
            CloseShop();
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(interactionKey))
        {
            DebugLog($"=== {interactionKey} KEY PRESSED ===");
            DebugLog($"Player Near Shop: {isPlayerNearShop}");
            DebugLog($"Shop Open: {isShopOpen}");
            DebugLog($"Shop Panel Null: {shopPanel == null}");
            DebugLog($"Shop Manager Null: {shopManager == null}");

            if (isPlayerNearShop && !isShopOpen)
            {
                DebugLog("Attempting to open shop...");
                OpenShop();
            }
            else if (isShopOpen)
            {
                DebugLog("Attempting to close shop...");
                CloseShop();
            }
            else
            {
                DebugWarning("Conditions not met for shop interaction!");
                if (!isPlayerNearShop)
                    DebugWarning("- Player not near shop");
                if (isShopOpen)
                    DebugWarning("- Shop already open");
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) && isShopOpen)
        {
            DebugLog("ESC pressed, closing shop...");
            CloseShop();
        }
    }

    void OpenShop()
    {

        DebugLog("=== OPENING SHOP ===");

        // Debug player info trước khi gọi shop
        DebugLog($"Current player object: {gameObject.name}");
        DebugLog($"Player has PlayerBaseStats: {GetComponent<PlayerBaseStats>() != null}");

        // Kiểm tra PlayerBaseStats trước khi mở shop
        var playerStats = GetComponent<PlayerBaseStats>();
        if (playerStats == null)
        {
            playerStats = GetComponentInChildren<PlayerBaseStats>();
            if (playerStats == null)
            {
                playerStats = GetComponentInParent<PlayerBaseStats>();
            }
        }

        if (playerStats == null)
        {
            DebugError("PlayerBaseStats not found on player! Cannot open shop.");

            // Note: The new simplified ShopManager automatically detects nearby players
            // No need to manually set player stats anymore
            PlayerBaseStats[] allStats = FindObjectsOfType<PlayerBaseStats>();
            if (allStats.Length > 0)
            {
                DebugLog($"Found {allStats.Length} PlayerBaseStats in scene. New ShopManager will auto-detect them.");
            }
        }
        // -----

        if (shopPanel == null)
        {
            DebugError("Cannot open shop - ShopPanel is null!");
            DebugLog("Attempting to re-find ShopPanel...");
            FindShopPanel();

            if (shopPanel == null)
            {
                DebugError("Still cannot find ShopPanel after re-search!");
                return;
            }
        }

        if (shopManager == null)
        {
            DebugError("Cannot open shop - ShopManager is null!");
            return;
        }

        DebugLog($"Setting ShopPanel active: {shopPanel.name}");
        shopPanel.SetActive(true);

        // Verify it's actually active
        DebugLog($"ShopPanel active after setting: {shopPanel.activeSelf}");
        DebugLog($"ShopPanel hierarchy active: {shopPanel.activeInHierarchy}");

        isShopOpen = true;

        // Bring panel to front
        shopPanel.transform.SetAsLastSibling();
        DebugLog("ShopPanel moved to front");

        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        DebugLog("Cursor shown and unlocked");

        DebugLog("✓ Shop opened successfully!");
    }


    void CloseShop()
    {
        DebugLog("=== CLOSING SHOP ===");

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            DebugLog("ShopPanel deactivated");
        }

        isShopOpen = false;

        // Hide cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        DebugLog("Cursor hidden and locked");

        DebugLog("✓ Shop closed successfully!");
    }

    void OnDrawGizmosSelected()
    {
        if (npcShop != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(npcShop.position, interactionDistance);

            // Draw line to player if in range
            if (isPlayerNearShop)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, npcShop.position);
            }
        }
    }

    // Debug helper methods
    void DebugLog(string message)
    {
        if (enableDebug)
            Debug.Log($"[ShopDebug] {message}");
    }

    void DebugWarning(string message)
    {
        if (enableDebug)
            Debug.LogWarning($"[ShopDebug] {message}");
    }

    void DebugError(string message)
    {
        if (enableDebug)
            Debug.LogError($"[ShopDebug] {message}");
    }

    // Public method to manually test shop opening (for debugging)
    [ContextMenu("Force Open Shop")]
    public void ForceOpenShop()
    {
        DebugLog("=== FORCE OPENING SHOP (DEBUG) ===");
        OpenShop();
    }

    [ContextMenu("Force Close Shop")]
    public void ForceCloseShop()
    {
        DebugLog("=== FORCE CLOSING SHOP (DEBUG) ===");
        CloseShop();
    }

    [ContextMenu("Re-find References")]
    public void RefindReferences()
    {
        DebugLog("=== RE-FINDING REFERENCES (DEBUG) ===");
        FindSceneReferences();
    }
}

