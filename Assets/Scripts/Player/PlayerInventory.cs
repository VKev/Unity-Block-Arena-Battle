//using UnityEngine;
//using System.Collections.Generic;
//using System.Linq;
//using TMPro;
//using Unity.Netcode;

//[System.Serializable]
//public class InventoryItem
//{
//    public int itemId;
//    public string itemName;
//    public int quantity;
//    public Sprite icon;

//    public InventoryItem(int id, string name, Sprite sprite)
//    {
//        itemId = id;
//        itemName = name;
//        quantity = 1;
//        icon = sprite;
//    }
//}

//public class PlayerInventory : NetworkBehaviour
//{
//    [Header("Inventory Settings")]
//    public int maxSlots = 20;
//    public int playerMoney = 1000;

//    // Inventory list
//    private List<InventoryItem> inventory = new List<InventoryItem>();

//    // UI References - Tự động tìm khi spawn
//    private GameObject inventoryUI;
//    private Transform itemsContainer;
//    private GameObject itemSlotPrefab;

//    private ShopManager currentShop;

//    public override void OnNetworkSpawn()
//    {
//        base.OnNetworkSpawn();

//        if (!IsOwner) return;

//        // Đợi một frame để đảm bảo scene đã load
//        StartCoroutine(SetupInventory());
//    }

//    System.Collections.IEnumerator SetupInventory()
//    {
//        yield return new WaitForEndOfFrame();

//        Debug.Log("[Inventory] Setting up for player: " + gameObject.name);

//        LoadInventory();

//        // Create UI slots
//        if (itemsContainer != null && itemSlotPrefab != null)
//        {
//            CreateInventorySlots();
//            UpdateInventoryUI();
//        }
//    }

   

//    void Update()
//    {
//        if (!IsOwner || !IsSpawned) return;

//        // Toggle inventory with Tab
//        if (Input.GetKeyDown(KeyCode.I))
//        {
//            ToggleInventory();
//        }
//    }

//    public void ToggleInventory()
//    {
//        if (inventoryUI != null)
//        {
//            inventoryUI.SetActive(!inventoryUI.activeSelf);
//            if (inventoryUI.activeSelf)
//            {
//                UpdateInventoryUI();
//            }
//        }
//    }

//    void CreateInventorySlots()
//    {
//        // Clear existing slots
//        foreach (Transform child in itemsContainer)
//        {
//            Destroy(child.gameObject);
//        }

//        // Create empty slots
//        for (int i = 0; i < maxSlots; i++)
//        {
//            GameObject slot = Instantiate(itemSlotPrefab, itemsContainer);
//            slot.name = $"Slot_{i}";
//        }

//        Debug.Log($"[Inventory] Created {maxSlots} inventory slots");
//    }

//    public void TryBuyItem(ShopItem shopItem)
//    {
//        // Check money
//        if (playerMoney < shopItem.price)
//        {
//            Debug.Log("Not enough money!");
//            ShowNotification("Not enough money!");
//            return;
//        }

//        // Check inventory space
//        if (!HasSpace())
//        {
//            Debug.Log("Inventory full!");
//            ShowNotification("Inventory full!");
//            return;
//        }

//        // Process purchase
//        BuyItem(shopItem);
//    }

//    void BuyItem(ShopItem shopItem)
//    {
//        // Deduct money
//        playerMoney -= shopItem.price;

//        // Add item to inventory
//        AddItemToInventory(shopItem.itemId, shopItem.itemName, shopItem.icon);

//        // Update UI
//        UpdateInventoryUI();

//        // Update shop money display
//        if (currentShop != null)
//            currentShop.UpdateMoneyDisplay();

//        // Save
//        SaveInventory();

//        ShowNotification($"Bought {shopItem.itemName}!");
//    }

//    void AddItemToInventory(int itemId, string itemName, Sprite icon)
//    {
//        // Find if item already exists
//        var existingItem = inventory.FirstOrDefault(i => i.itemId == itemId);

//        if (existingItem != null)
//        {
//            // Stack item
//            existingItem.quantity++;
//        }
//        else
//        {
//            // Add new item
//            inventory.Add(new InventoryItem(itemId, itemName, icon));
//        }
//    }

//    public int GetMoney()
//    {
//        return playerMoney;
//    }

//    public bool HasSpace()
//    {
//        return inventory.Count < maxSlots;
//    }

//    public void SetCurrentShop(ShopManager shop)
//    {
//        currentShop = shop;
//    }

//    void UpdateInventoryUI()
//    {
//        if (itemsContainer == null) return;

//        // Clear all slots
//        for (int i = 0; i < itemsContainer.childCount; i++)
//        {
//            var slot = itemsContainer.GetChild(i);
//            var icon = slot.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
//            var quantityText = slot.Find("Quantity")?.GetComponent<TextMeshProUGUI>();

//            if (icon != null)
//            {
//                icon.enabled = false;
//                icon.sprite = null;
//            }
//            if (quantityText != null)
//            {
//                quantityText.text = "";
//            }
//        }

//        // Fill with items
//        for (int i = 0; i < inventory.Count && i < maxSlots; i++)
//        {
//            var slot = itemsContainer.GetChild(i);
//            var icon = slot.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
//            var quantityText = slot.Find("Quantity")?.GetComponent<TextMeshProUGUI>();

//            if (icon != null && inventory[i].icon != null)
//            {
//                icon.enabled = true;
//                icon.sprite = inventory[i].icon;
//            }

//            if (quantityText != null)
//            {
//                quantityText.text = inventory[i].quantity > 1 ? inventory[i].quantity.ToString() : "";
//            }
//        }
//    }

//    // Save/Load using PlayerPrefs
//    void SaveInventory()
//    {
//        // Save money
//        PlayerPrefs.SetInt("PlayerMoney", playerMoney);

//        // Save inventory count
//        PlayerPrefs.SetInt("InventoryCount", inventory.Count);

//        // Save each item
//        for (int i = 0; i < inventory.Count; i++)
//        {
//            PlayerPrefs.SetInt($"Item_{i}_Id", inventory[i].itemId);
//            PlayerPrefs.SetString($"Item_{i}_Name", inventory[i].itemName);
//            PlayerPrefs.SetInt($"Item_{i}_Quantity", inventory[i].quantity);
//        }

//        PlayerPrefs.Save();
//        Debug.Log("[Inventory] Saved!");
//    }

//    void LoadInventory()
//    {
//        // Load money
//        if (PlayerPrefs.HasKey("PlayerMoney"))
//        {
//            playerMoney = PlayerPrefs.GetInt("PlayerMoney");
//        }

//        // Load inventory
//        inventory.Clear();
//        int count = PlayerPrefs.GetInt("InventoryCount", 0);

//        for (int i = 0; i < count; i++)
//        {
//            int itemId = PlayerPrefs.GetInt($"Item_{i}_Id");
//            string itemName = PlayerPrefs.GetString($"Item_{i}_Name");
//            int quantity = PlayerPrefs.GetInt($"Item_{i}_Quantity");

//            // Find icon from shop
//            var shopItem = FindShopItem(itemId);
//            if (shopItem != null)
//            {
//                var invItem = new InventoryItem(itemId, itemName, shopItem.icon);
//                invItem.quantity = quantity;
//                inventory.Add(invItem);
//            }
//        }

//        Debug.Log($"[Inventory] Loaded {count} items from save");
//    }

//    ShopItem FindShopItem(int itemId)
//    {
//        // Find shop in scene and search for item
//        ShopManager shop = FindObjectOfType<ShopManager>();
//        if (shop != null)
//        {
//            return shop.shopItems.FirstOrDefault(item => item.itemId == itemId);
//        }
//        return null;
//    }

//    void ShowNotification(string message)
//    {
//        Debug.Log(message);
//        // TODO: Add UI notification
//    }

//    // Call this when opening shop
//    public void OnShopOpened(ShopManager shop)
//    {
//        SetCurrentShop(shop);
//        shop.SetCurrentPlayer(this);
//    }

//    // Clear save data for testing
//    [ContextMenu("Clear Save Data")]
//    public void ClearSaveData()
//    {
//        PlayerPrefs.DeleteAll();
//        inventory.Clear();
//        if (itemsContainer != null)
//            UpdateInventoryUI();
//        Debug.Log("[Inventory] Save data cleared!");
//    }

//    // Get inventory items (for other systems)
//    public List<InventoryItem> GetInventoryItems()
//    {
//        return inventory;
//    }


//    // Check if player has specific item
//    public bool HasItem(int itemId)
//    {
//        return inventory.Any(item => item.itemId == itemId);
//    }

//    // Get item quantity
//    public int GetItemQuantity(int itemId)
//    {
//        var item = inventory.FirstOrDefault(i => i.itemId == itemId);
//        return item != null ? item.quantity : 0;
//    }
//}