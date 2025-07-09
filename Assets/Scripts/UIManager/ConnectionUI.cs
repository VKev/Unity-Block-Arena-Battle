using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Netcode.Transports.UTP;

public class ConnectionUI : MonoBehaviour
{
    public Button hostButton;
    public Button clientButton;
    public Button startButton;
    public GameObject uiRoot;
    
    [Header("Network Settings")]
    public string fallbackIP = "172.20.10.2"; // Fallback IP if LoginRegisterManager not available

    private bool hasHiddenUI = false;
    private List<GameObject> uiChildren = new List<GameObject>();
 
    void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        clientButton.onClick.AddListener(OnClientClicked);
        startButton.onClick.AddListener(OnStartGame);
        startButton.gameObject.SetActive(false);

        // Find all UI children except those tagged "StartPanel" and disable them
        FindAndDisableUIChildren();

        // Register network events for debugging
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        
        // No need for IP input field - we get IP from LoginRegisterManager
        
        // Log initial transport configuration
        LogTransportConfiguration("Initial");
    }

    void Update()
    {
        // ✅ Fallback check for client if UI wasn't hidden
        if (!hasHiddenUI && NetworkManager.Singleton.IsClient && NetworkManager.Singleton.IsConnectedClient)
        {
            HideUI();
        }
        
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
        {
            OnClientClicked();
        }
    }

    string GetServerIP()
    {
        // Debug LoginRegisterManager availability
        if (LoginSystem.LoginRegisterManager.Instance == null)
        {
            Debug.LogWarning("[ConnectionUI] LoginRegisterManager.Instance is NULL! Using fallback IP.");
            return fallbackIP;
        }
        
        Debug.Log("[ConnectionUI] LoginRegisterManager.Instance found!");
        
        try
        {
            string serverIP = LoginSystem.LoginRegisterManager.Instance.GetServerIP();
            if (string.IsNullOrEmpty(serverIP) || serverIP == "localhost")
            {
                Debug.LogWarning($"[ConnectionUI] LoginRegisterManager returned empty/localhost IP: '{serverIP}'. Using fallback IP.");
                return fallbackIP;
            }
            
            Debug.Log($"[ConnectionUI] Successfully got IP from LoginRegisterManager: {serverIP}");
            return serverIP;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ConnectionUI] Exception getting IP from LoginRegisterManager: {ex.Message}");
            return fallbackIP;
        }
    }

    void LogTransportConfiguration(string context)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            Debug.Log($"[ConnectionUI] {context} Transport Config:");
            Debug.Log($"  - Address: {transport.ConnectionData.Address}");
            Debug.Log($"  - Port: {transport.ConnectionData.Port}");
            Debug.Log($"  - Server Listen Address: {transport.ConnectionData.ServerListenAddress}");
        }
        else
        {
            Debug.LogError($"[ConnectionUI] {context}: UnityTransport not found!");
        }
    }

    void OnHostClicked()
    {
        try
        {
            Debug.Log("[ConnectionUI] === STARTING HOST ===");
            
            // Set host to bind to all interfaces (0.0.0.0) to allow external connections
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                Debug.Log($"[ConnectionUI] Before config - Address: {transport.ConnectionData.Address}, Port: {transport.ConnectionData.Port}");
                
                transport.ConnectionData.Address = "0.0.0.0";
                transport.ConnectionData.ServerListenAddress = "0.0.0.0";
                
                Debug.Log($"[ConnectionUI] After config - Address: {transport.ConnectionData.Address}, Port: {transport.ConnectionData.Port}");
                Debug.Log($"[ConnectionUI] Server Listen Address: {transport.ConnectionData.ServerListenAddress}");
            }
            else
            {
                Debug.LogError("[ConnectionUI] UnityTransport component not found on NetworkManager!");
                return;
            }
            
            LogTransportConfiguration("Pre-Host");
            
            bool hostStarted = NetworkManager.Singleton.StartHost();
            Debug.Log($"[ConnectionUI] StartHost returned: {hostStarted}");
            
            if (hostStarted)
            {
                Debug.Log("[ConnectionUI] Host started successfully!");
                
                Debug.Log("[ConnectionUI] Host started, using IP from LoginRegisterManager");
                
                EnableUIChildren();
            }
            else
            {
                Debug.LogError("[ConnectionUI] Failed to start host!");
            }
            
            LogTransportConfiguration("Post-Host");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ConnectionUI] Exception starting host: {ex.Message}");
            Debug.LogError($"[ConnectionUI] Stack trace: {ex.StackTrace}");
        }
    }

    void OnClientClicked()
    {
        try
        {
            Debug.Log($"[ConnectionUI] === STARTING CLIENT ===");
            
            // Get IP from LoginRegisterManager
            string targetIP = GetServerIP();
            
            Debug.Log($"[ConnectionUI] Target IP: {targetIP}");
            
            // Set the connection IP address before starting client
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                Debug.Log($"[ConnectionUI] Before config - Address: {transport.ConnectionData.Address}, Port: {transport.ConnectionData.Port}");
                
                transport.ConnectionData.Address = targetIP;
                
                Debug.Log($"[ConnectionUI] After config - Address: {transport.ConnectionData.Address}, Port: {transport.ConnectionData.Port}");
            }
            else
            {
                Debug.LogError("[ConnectionUI] UnityTransport component not found on NetworkManager!");
                return;
            }
            
            LogTransportConfiguration("Pre-Client");
            
            bool clientStarted = NetworkManager.Singleton.StartClient();
            Debug.Log($"[ConnectionUI] StartClient returned: {clientStarted}");
            
            if (clientStarted)
            {
                Debug.Log("[ConnectionUI] Client connection attempt started!");
                
                Debug.Log("[ConnectionUI] Client started, using IP from LoginRegisterManager");
                
                EnableUIChildren();
            }
            else
            {
                Debug.LogError("[ConnectionUI] Failed to start client!");
            }
            
            LogTransportConfiguration("Post-Client");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ConnectionUI] Exception starting client: {ex.Message}");
            Debug.LogError($"[ConnectionUI] Stack trace: {ex.StackTrace}");
        }
    }

    void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[ConnectionUI] Client connected: {clientId}");
        Debug.Log($"[ConnectionUI] Local client ID: {NetworkManager.Singleton.LocalClientId}");
        Debug.Log($"[ConnectionUI] Is server: {NetworkManager.Singleton.IsServer}");
        Debug.Log($"[ConnectionUI] Is client: {NetworkManager.Singleton.IsClient}");
        Debug.Log($"[ConnectionUI] Is host: {NetworkManager.Singleton.IsHost}");
        
        // Only Host will trigger this — but still good to check
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("[ConnectionUI] OnClientConnected fired — Hiding UI for local client");
            HideUI();
        }
    }

    void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[ConnectionUI] Client disconnected: {clientId}");
        
        Debug.Log("[ConnectionUI] Client disconnected - IP will be retrieved from LoginRegisterManager on reconnection");
    }

    void OnServerStarted()
    {
        Debug.Log("[ConnectionUI] Server started successfully!");
        LogTransportConfiguration("Server-Started");
    }

    void OnClientStarted()
    {
        Debug.Log("[ConnectionUI] Client started successfully!");
        LogTransportConfiguration("Client-Started");
    }

    void OnStartGame()
    {
        Debug.Log("Game started!");
        // Add game start logic here
    }

    void HideUI()
    {
        if (uiRoot != null && uiRoot.activeSelf)
        {
            Debug.Log("Hiding UI for local player");
            uiRoot.SetActive(false);
            hasHiddenUI = true;
        }
    }

    void FindAndDisableUIChildren()
    {
        uiChildren.Clear();
        
        // Get all children of this game object (canvas)
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            
            // Skip objects with "StartPanel" tag
            if (!child.CompareTag("StartPanel") && !child.CompareTag("ShopPanel") && !child.CompareTag("BuffPanel") && !child.CompareTag("SkillPanel"))
            {
                uiChildren.Add(child);
                child.SetActive(false);
            }
        }
        
        Debug.Log($"Disabled {uiChildren.Count} UI children (excluding StartPanel objects)");
    }

    void EnableUIChildren()
    {
        foreach (GameObject child in uiChildren)
        {
            if (child != null)
            {
                child.SetActive(true);
            }
        }
        
        Debug.Log($"Enabled {uiChildren.Count} UI children");
    }
}