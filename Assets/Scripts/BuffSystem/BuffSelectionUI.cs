using buffSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System; // Required for Action<List<Buff>> and EventHandler

public class BuffSelectionUI : MonoBehaviour
{
    [Header("Buff Button Panel")]
    public GameObject buffPanel;

    [Header("Timer Panel")]
    public GameObject timerPanel; 
    public TMP_Text countdownText;

    [Header("Buff Buttons (Set size = 3)")]
    [field: SerializeField]
    public List<BuffButtonUI> buffButtons;

    [Header("Countdown Settings")]
    public float countdownDuration = 60f;

    private float currentTime;
    private bool isCountingDown = false;

    private List<Buff> currentBuffs = new List<Buff>();

    private void Awake()
    {
        // Auto-assign buff panel if not manually set
        if (buffPanel == null)
        {
            // Try to find a child GameObject named "Panel" or the first GameObject with a Transform in children
            var panelTransform = transform.Find("Panel");
            if (panelTransform != null)
                buffPanel = panelTransform.gameObject;
            else
            {
                // Fallback: search all children for a potential panel (might be too broad)
                foreach (Transform child in transform)
                {
                    if (child.name.Contains("Panel")) // Simple name check
                    {
                        buffPanel = child.gameObject;
                        break;
                    }
                }
            }
            if (buffPanel == null)
            {
                Debug.LogWarning("BuffSelectionUI: Could not auto-assign buffPanel. Please assign it manually in the Inspector.", this);
            }
        }

        // Auto-assign countdown text
        if (countdownText == null)
        {
            // First, try to find the timerPanel if not assigned
            if (timerPanel == null)
            {
                Transform foundTimerPanelTransform = transform.Find("PanelTimerLayout");
                if (foundTimerPanelTransform != null)
                {
                    timerPanel = foundTimerPanelTransform.gameObject;
                    Debug.Log("BuffSelectionUI: Auto-assigned timerPanel to 'PanelTimerLayout'.", this);
                }
                else
                {
                    Debug.LogWarning("BuffSelectionUI: timerPanel is not assigned and 'PanelTimerLayout' not found as a child. Please assign it manually.", this);
                }
            }

            // Now, if timerPanel is available, find the TMP_Text within it
            if (timerPanel != null)
            {
                countdownText = timerPanel.GetComponentInChildren<TMP_Text>(true);
                if (countdownText == null)
                {
                    Debug.LogWarning("BuffSelectionUI: Found timerPanel but could not find TMP_Text within it. Ensure your 'PanelTimerLayout' has a TMP_Text child for the countdown.", this);
                }
            }
            else // Fallback if timerPanel could not be found/assigned
            {
                Debug.LogWarning("BuffSelectionUI: timerPanel is null, cannot search for countdownText within it. Trying broader search as a fallback (might pick wrong text).", this);
                countdownText = GetComponentInChildren<TMP_Text>(true); // This was your original line, now a fallback
            }
        }

        // Auto-assign buff buttons if the list is empty or null
        if (buffButtons == null || buffButtons.Count == 0)
        {
            buffButtons = new List<BuffButtonUI>();
            if (buffPanel != null)
            {
                Button[] buttons = buffPanel.GetComponentsInChildren<Button>(true);
                foreach (Button btn in buttons)
                {
                    BuffButtonUI ui = new BuffButtonUI
                    {
                        button = btn,
                        nameText = btn.transform.Find("TextName")?.GetComponent<TMP_Text>(),
                        tierText = btn.transform.Find("TextTier")?.GetComponent<TMP_Text>(),
                        descriptionText = btn.transform.Find("TextDesc")?.GetComponent<TMP_Text>(),
                        iconImage = btn.transform.Find("ImageIcon")?.GetComponent<Image>()
                    };
                    buffButtons.Add(ui);
                }
                if (buffButtons.Count == 0)
                {
                    Debug.LogWarning("BuffSelectionUI: Could not auto-assign any BuffButtons. Ensure your panel has Button components as children.", this);
                }
            }
            else
            {
                Debug.LogWarning("BuffSelectionUI: buffPanel is null, cannot auto-assign buffButtons.", this);
            }
        }

        // Initially hide the UI panel
        buffPanel?.SetActive(false);
    }

    // Subscribe to events when this component is enabled
    void OnEnable()
    {
        GameEvents.OnRequestBuffSelectionUI += ShowBuffChoices;
        //GameEvents.OnBuffChosen += OnBuffChosenHandler; // This is for when *a* buff is chosen
        Debug.Log("BuffSelectionUI: Subscribed to events.");
    }

    // Unsubscribe from events when this component is disabled or destroyed
    void OnDisable()
    {
        GameEvents.OnRequestBuffSelectionUI -= ShowBuffChoices;
        GameEvents.OnBuffChosen -= OnBuffChosenHandler;
        Debug.Log("BuffSelectionUI: Unsubscribed from events.");

        // Also stop any ongoing countdown if UI is disabled/destroyed mid-countdown
        StopAllCoroutines();
    }

    /// <summary>
    /// This method is called by the GameEvents.OnRequestBuffSelectionUI event.
    /// It displays the provided list of buffs on the UI buttons.
    /// </summary>
    /// <param name="buffs">The list of buffs to display.</param>
    public void ShowBuffChoices(List<Buff> buffs)
    {
        currentBuffs = buffs;

        if (buffs.Count != buffButtons.Count) // Dynamic check for button count
        {
            Debug.LogError($"BuffSelectionUI: Mismatch! Expected {buffButtons.Count} buffs based on buttons, but received {buffs.Count}.", this);
            if (buffs.Count == 0 || buffButtons.Count == 0)
            {
                buffPanel.SetActive(false);
                Debug.LogWarning("BuffSelectionUI: No buffs or no buttons to display. Hiding UI.", this);
                return;
            }
        }

        buffPanel.SetActive(true);
        StopAllCoroutines(); // Stop any previous countdown if UI is shown again before old one finished

        // Loop through the smaller of the two counts to prevent index out of bounds
        int displayCount = Mathf.Min(buffs.Count, buffButtons.Count);

        for (int i = 0; i < displayCount; i++)
        {
            Buff buff = buffs[i];
            BuffButtonUI ui = buffButtons[i];

            if (ui == null)
            {
                Debug.LogError($"BuffSelectionUI: buffButtons[{i}] is null! Skipping button setup.", this);
                continue;
            }
            if (ui.button == null)
            {
                Debug.LogError($"BuffSelectionUI: ui.button is NULL for button index {i}! Cannot attach listener.", this);
                continue;
            }

            // Update UI text and image based on buff data
            if (ui.nameText != null) ui.nameText.text = buff.Name;
            if (ui.tierText != null) ui.tierText.text = buff.Tier.ToString();
            if (ui.descriptionText != null) ui.descriptionText.text = buff.Description;
            if (ui.iconImage != null) ui.iconImage.color = GetTierColor(buff.Tier);

            // Set up button listener
            int index = i; // Local copy for closure
            ui.button.onClick.RemoveAllListeners(); // Clear previous listeners
            ui.button.onClick.AddListener(() => {
                Debug.Log($"BuffSelectionUI: Button {index} for {buff.Name} clicked!", this);
                PickBuff(index);
            });
            ui.button.gameObject.SetActive(true); // Ensure button is active
        }

        for (int i = displayCount; i < buffButtons.Count; i++)
        {
            if (buffButtons[i] != null && buffButtons[i].button != null)
            {
                buffButtons[i].button.gameObject.SetActive(false);
            }
        }

        StartCountdown();
        Debug.Log("BuffSelectionUI: Displayed with new choices and countdown started.");
    }

    private void StartCountdown()
    {
        currentTime = countdownDuration;
        isCountingDown = true;
        StartCoroutine(CountdownTimer());
    }

    private IEnumerator CountdownTimer()
    {
        while (currentTime > 0)
        {
            if (countdownText != null)
                countdownText.text = $"Time left: {Mathf.CeilToInt(currentTime)}s";
            yield return new WaitForSeconds(1f);
            currentTime -= 1f;
        }

        if (buffPanel.activeSelf)
        {
            PickBuff(0); // Auto-select first buff
            Debug.Log("BuffSelectionUI: Timer expired. Auto-selected first buff.", this);
        }
    }

    private void PickBuff(int index)
    {
        if (index < 0 || index >= currentBuffs.Count)
        {
            Debug.LogError("BuffSelectionUI: Invalid buff index.", this);
            return;
        }

        Buff chosenBuff = currentBuffs[index];
        GameEvents.TriggerBuffChosen(chosenBuff); // Publish the "Buff Chosen" event
        Debug.Log($"BuffSelectionUI: Selected: {chosenBuff.Name}. Event announced.", this);
        CloseUI();
    }

    // Handler for the EventManager.OnBuffChosen event (optiona)
    private void OnBuffChosenHandler(object sender, BuffChosenEventArgs args)
    {
        // This handler means this specific UI element is also listening to the event it publishes.
        // This can be useful if, for example, multiple UI elements need to react to a buff being chosen.
        // For this specific UI, the primary action (closing UI) is handled directly in PickBuff.
        // You might use this for additional logging or visual feedback.
        // Debug.Log($"BuffSelectionUI: Received OnBuffChosen event for {args.ChosenBuff.Name} (Optional Handler).");
    }

    private void CloseUI()
    {
        isCountingDown = false;
        StopAllCoroutines();
        if (buffPanel != null) buffPanel.SetActive(false);
        if (countdownText != null) countdownText.text = "";
        Debug.Log("BuffSelectionUI: UI closed.");
    }

    private Color GetTierColor(BuffTier tier)
    {
        return tier switch
        {
            BuffTier.Silver => Color.gray,
            BuffTier.Gold => Color.yellow,
            BuffTier.Diamond => Color.cyan,
            _ => Color.white,
        };
    }
}

[System.Serializable]
public class BuffButtonUI
{
    public Button button;
    public TMP_Text nameText;
    public TMP_Text tierText;
    public TMP_Text descriptionText;
    public Image iconImage;
}