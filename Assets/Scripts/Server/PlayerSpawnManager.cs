using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnManager : NetworkBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }
    
    [Header("Spawn Settings")]
    public float spawnHeightY = 1f; // Fixed Y position for spawning
    public float circlePercentage = 0.8f; // 80% of circle radius
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Set up connection approval callback when this script starts
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = HandleConnectionApproval;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Additional server-only initialization if needed
            Debug.Log("PlayerSpawnManager: Server initialized");
        }
    }

    private void HandleConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // Always approve the connection
        response.Approved = true;
        response.CreatePlayerObject = true;
        
        // Get random spawn position within 80% of safe zone
        Vector3 spawnPosition = GetRandomSpawnPosition();
        response.Position = spawnPosition;
        response.Rotation = Quaternion.identity;
        
        Debug.Log($"PlayerSpawnManager: Approved connection for client {request.ClientNetworkId} at position {spawnPosition}");
    }

    public Vector3 GetRandomSpawnPosition()
    {
        Vector3 spawnPosition = Vector3.zero;
        
        if (ChangeCircle.Instance != null)
        {
            // Get circle center and radius
            Vector3 circleCenter = ChangeCircle.Instance.transform.position;
            float xRadius = ChangeCircle.Instance.GetXRadius();
            float yRadius = ChangeCircle.Instance.GetYRadius();
            
            // Calculate spawn area within 80% of circle radius
            float spawnXRadius = xRadius * circlePercentage;
            float spawnYRadius = yRadius * circlePercentage;
            
            // Generate random position within ellipse
            Vector2 randomPoint = GetRandomPointInEllipse(spawnXRadius, spawnYRadius);
            
            // Set spawn position
            spawnPosition = new Vector3(
                circleCenter.x + randomPoint.x,
                spawnHeightY, // Fixed Y position
                circleCenter.z + randomPoint.y
            );
            
            Debug.Log($"PlayerSpawnManager: Generated spawn position {spawnPosition} within circle bounds (XRadius: {spawnXRadius}, YRadius: {spawnYRadius})");
        }
        else
        {
            // Fallback if ChangeCircle.Instance is not available
            spawnPosition = new Vector3(
                Random.Range(-50f, 50f),
                spawnHeightY,
                Random.Range(-50f, 50f)
            );
            
            Debug.LogWarning("PlayerSpawnManager: ChangeCircle.Instance not found, using fallback spawn position");
        }
        
        return spawnPosition;
    }

    private Vector2 GetRandomPointInEllipse(float xRadius, float yRadius)
    {
        // Generate random point in unit circle, then scale to ellipse
        float angle = Random.Range(0f, 2f * Mathf.PI);
        float radius = Mathf.Sqrt(Random.Range(0f, 1f)); // Square root for uniform distribution
        
        float x = radius * Mathf.Cos(angle) * xRadius;
        float y = radius * Mathf.Sin(angle) * yRadius;
        
        return new Vector2(x, y);
    }
    
    // Method to manually respawn a player at a random position (if needed)
    public void RespawnPlayerAtRandomPosition(ulong clientId)
    {
        if (!IsServer) return;
        
        Vector3 newPosition = GetRandomSpawnPosition();
        
        // Find the player's NetworkObject and move them
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject != null)
            {
                client.PlayerObject.transform.position = newPosition;
                Debug.Log($"PlayerSpawnManager: Respawned player {clientId} at {newPosition}");
            }
        }
    }
} 