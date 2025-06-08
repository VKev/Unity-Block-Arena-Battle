using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionUI : MonoBehaviour
{
    public Button hostButton;
    public Button clientButton;
    public Button startButton;
    public GameObject uiRoot;

    private bool hasHiddenUI = false;

    void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        clientButton.onClick.AddListener(OnClientClicked);
        startButton.onClick.AddListener(OnStartGame);
        startButton.gameObject.SetActive(false);

        // Still register the callback (only works on host)
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void Update()
    {
        // ✅ Fallback check for client if UI wasn't hidden
        if (!hasHiddenUI && NetworkManager.Singleton.IsClient && NetworkManager.Singleton.IsConnectedClient)
        {
            HideUI();
        }
    }

    void OnHostClicked()
    {
        NetworkManager.Singleton.StartHost();
        Debug.Log("Hosting...");
    }

    void OnClientClicked()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("Connecting as client...");
    }

    void OnClientConnected(ulong clientId)
    {
        // Only Host will trigger this — but still good to check
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("OnClientConnected fired — Hiding UI for local client");
            HideUI();
        }
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
}