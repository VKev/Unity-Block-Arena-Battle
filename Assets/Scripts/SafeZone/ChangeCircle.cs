using System;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

[RequireComponent(typeof(LineRenderer))]
public class ChangeCircle : NetworkBehaviour
{
    [Range(0, 360)]
    public int Segments;
    [Range(0, 5000)]
    public float XRadius;
    [Range(0, 5000)]
    public float YRadius;
    public GameObject ZoneWall;

    public UnityEvent OnZoneWillShrink;  // Event UI cảnh báo sắp co

    private WorldCircle circle;
    private LineRenderer renderer;
    private bool isShrinking = false;
    private bool hasStartedShrinking = false;
    
    // Store initial radius values
    private float initialXRadius;
    private float initialYRadius;
    
    // Target minimum radius values
    private float minXRadius = 5f;
    private float minYRadius = 5f;
    
    // Current phase tracking
    private GamePhase currentPhase = GamePhase.WaitingToStart;

    public static ChangeCircle Instance { get; private set; }

    private NetworkVariable<float> netXRadius = new(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<float> netYRadius = new(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> zoneWallActive = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> netIsShrinking = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<GamePhase> netCurrentPhase = new(GamePhase.WaitingToStart, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        renderer = GetComponent<LineRenderer>();
        circle = new WorldCircle(ref renderer, Segments, new float[] { XRadius, YRadius });

        // Store initial radius values
        initialXRadius = XRadius;
        initialYRadius = YRadius;

        ZoneWall = GameObject.FindGameObjectWithTag("ZoneWall");
        netXRadius.Value = XRadius;
        netYRadius.Value = YRadius;
        
        // Subscribe to phase changes
        if (NetworkCountdownManager.Instance != null)
        {
            NetworkCountdownManager.OnPhaseChanged += OnPhaseChanged;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from phase changes
        if (NetworkCountdownManager.Instance != null)
        {
            NetworkCountdownManager.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    private void OnPhaseChanged(GamePhase newPhase)
    {
        currentPhase = newPhase;
        
        if (!IsServer) return;
        
        // Update networked phase for all clients
        netCurrentPhase.Value = newPhase;
        
        if (newPhase == GamePhase.FightPhase && !hasStartedShrinking)
        {
            // Start shrinking when fight phase begins
            OnZoneWillShrink?.Invoke(); // phát cảnh báo UI
            isShrinking = true;
            hasStartedShrinking = true;
            netIsShrinking.Value = true; // Sync to clients
        }
        else if (newPhase != GamePhase.FightPhase && hasStartedShrinking)
        {
            // Reset circle when leaving fight phase
            ResetCircle();
        }
    }

    private void ResetCircle()
    {
        if (!IsServer) return;
        
        // Reset to initial size
        XRadius = initialXRadius;
        YRadius = initialYRadius;
        netXRadius.Value = XRadius;
        netYRadius.Value = YRadius;
        
        isShrinking = false;
        hasStartedShrinking = false;
        netIsShrinking.Value = false; // Sync to clients
        zoneWallActive.Value = true;
        
        circle.Draw(Segments, XRadius, YRadius);
    }

    void Update()
    {
        if (!IsServer)
        {
            XRadius = netXRadius.Value;
            YRadius = netYRadius.Value;
            circle.Draw(Segments, XRadius, YRadius);
        }

        if (ZoneWall)
        {
            ZoneWall.SetActive(zoneWallActive.Value);
            ZoneWall.transform.localScale = new Vector3(XRadius * 0.02f, 1, YRadius * 0.02f);
        }

        if (!IsServer) return;

        // Only shrink during fight phase based on time progress
        if (isShrinking && currentPhase == GamePhase.FightPhase && NetworkCountdownManager.Instance != null)
        {
            // Calculate how much time has passed in the fight phase
            float totalFightTime = GetFightPhaseDuration();
            float timeRemaining = NetworkCountdownManager.Instance.GetTimeRemaining();
            float timeElapsed = totalFightTime - timeRemaining;
            
            // Calculate progress (0 = start of fight phase, 1 = end of fight phase)
            float progress = Mathf.Clamp01(timeElapsed / totalFightTime);
            
            // Interpolate radius based on progress
            XRadius = Mathf.Lerp(initialXRadius, minXRadius, progress);
            YRadius = Mathf.Lerp(initialYRadius, minYRadius, progress);
            
            netXRadius.Value = XRadius;
            netYRadius.Value = YRadius;

            circle.Draw(Segments, XRadius, YRadius);

            // Zone wall stays active throughout the entire fight phase
            // It will only be disabled when the phase actually ends in ResetCircle()
        }
    }

    private float GetFightPhaseDuration()
    {
        // Get the actual fight phase duration from NetworkCountdownManager
        if (NetworkCountdownManager.Instance != null)
        {
            return NetworkCountdownManager.Instance.GetFightPhaseDuration();
        }
        return 60f; // Fallback default
    }

    public float GetXRadius() => XRadius;
    public float GetYRadius() => YRadius;
    public bool IsShrinking() => netIsShrinking.Value;
    public GamePhase GetCurrentPhase() => netCurrentPhase.Value;
}
