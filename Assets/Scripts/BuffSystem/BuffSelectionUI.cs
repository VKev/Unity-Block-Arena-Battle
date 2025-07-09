using buffSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class BuffSelectionUI : MonoBehaviour
{
    [Header("Buff Button Panel")]
    public GameObject buffPanel;

    [Header("Reroll Button Panel")]
    public GameObject rerollPanel;

    [Header("Timer Panel")]
    public GameObject timerPanel;
    public TMP_Text countdownText;

    [Header("Buff Buttons (Set size = 3)")]
    [field: SerializeField]
    public List<BuffButtonUI> buffButtons;

    [Header("Countdown Settings")]
    public float countdownDuration = 15f;

    private float currentTime;
    private bool isCountingDown = false;

    private List<Buff> currentBuffs = new();

    private void Awake()
    {
        if (buffPanel == null)
        {
            var panelTransform = transform.Find("PanelButtonLayoutUI");
            if (panelTransform != null)
                buffPanel = panelTransform.gameObject;
        }

        if (rerollPanel == null)
        {
            var rerollTransform = transform.Find("PanelButtonRerollUI");
            if (rerollTransform != null)
                rerollPanel = rerollTransform.gameObject;
        }

        if (countdownText == null)
        {
            if (timerPanel == null)
                timerPanel = transform.Find("PanelTimerLayout")?.gameObject;

            countdownText = timerPanel?.GetComponentInChildren<TMP_Text>(true) ?? GetComponentInChildren<TMP_Text>(true);
        }

        if (buffButtons == null || buffButtons.Count == 0)
        {
            buffButtons = new List<BuffButtonUI>();
            Transform layoutPanel = transform.Find("PanelButtonLayoutUI");
            Transform rerollPanelTransform = transform.Find("PanelButtonRerollUI");

            if (layoutPanel == null || rerollPanelTransform == null)
            {
                Debug.LogError("BuffSelectionUI: Cannot find layout or reroll panel.");
            }
            else
            {
                int buttonCount = Mathf.Min(layoutPanel.childCount, rerollPanelTransform.childCount);
                for (int i = 0; i < buttonCount; i++)
                {
                    Transform buffButtonTransform = layoutPanel.GetChild(i);
                    Transform rerollButtonTransform = rerollPanelTransform.GetChild(i);

                    BuffButtonUI ui = new BuffButtonUI
                    {
                        button = buffButtonTransform.GetComponent<Button>(),
                        rerollButton = rerollButtonTransform.GetComponent<Button>(),
                        nameText = buffButtonTransform.Find("TextName")?.GetComponent<TMP_Text>(),
                        tierText = buffButtonTransform.Find("TextTier")?.GetComponent<TMP_Text>(),
                        descriptionText = buffButtonTransform.Find("TextDesc")?.GetComponent<TMP_Text>(),
                        iconImage = buffButtonTransform.Find("ImageIcon")?.GetComponent<Image>(),
                        rerollText = rerollButtonTransform.GetComponentInChildren<TMP_Text>()
                    };

                    buffButtons.Add(ui);
                }
            }
        }

        buffPanel?.SetActive(false);
        rerollPanel?.SetActive(false);
        timerPanel?.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.OnRequestBuffSelectionUI += ShowBuffChoices;
    }

    private void OnDisable()
    {
        GameEvents.OnRequestBuffSelectionUI -= ShowBuffChoices;
        GameEvents.OnBuffChosen -= OnBuffChosenHandler;
        StopAllCoroutines();
    }

    public void ShowBuffChoices(List<Buff> buffs)
    {
        currentBuffs = buffs;

        if (buffs.Count != buffButtons.Count)
        {
            Debug.LogError($"Mismatch! Expected {buffButtons.Count}, got {buffs.Count} buffs.");
            if (buffs.Count == 0 || buffButtons.Count == 0)
            {
                buffPanel.SetActive(false);
                rerollPanel.SetActive(false);
                return;
            }
        }

        buffPanel?.SetActive(true);
        rerollPanel?.SetActive(true);
        //timerPanel?.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        StopAllCoroutines();

        for (int i = 0; i < buffButtons.Count; i++)
        {
            BuffButtonUI ui = buffButtons[i];
            Buff buff = currentBuffs[i];

            ui.rerollCount = 0;

            if (ui.nameText != null) ui.nameText.text = buff.Name;
            if (ui.tierText != null) ui.tierText.text = buff.Tier.ToString();
            if (ui.descriptionText != null) ui.descriptionText.text = buff.Description;
            if (ui.iconImage != null)
            {
                ui.iconImage.sprite = buff.IconSprite;
                ui.iconImage.color = GetTierColor(buff.Tier);
            }

            if (ui.button != null)
            {
                int index = i;
                ui.button.onClick.RemoveAllListeners();
                ui.button.onClick.AddListener(() => PickBuff(index));
                ui.button.gameObject.SetActive(true);
            }

            if (ui.rerollButton != null)
            {
                int index = i;
                ui.rerollButton.onClick.RemoveAllListeners();
                ui.rerollButton.onClick.AddListener(() => RerollBuff(index));
                ui.rerollButton.gameObject.SetActive(true);
                ui.rerollButton.interactable = true;

                if (ui.rerollText != null)
                    ui.rerollText.text = "Reroll (3)";
            }
        }

        StartCountdown();
    }

    private void StartCountdown()
    {
        currentTime = 15f;
        isCountingDown = true;
        StartCoroutine(CountdownTimer());
    }

    private IEnumerator CountdownTimer()
    {
        Debug.Log($"Starting countdown for {countdownDuration} seconds.");

        currentTime = 15f;
        Debug.Log($"Starting countdown for {countdownDuration} seconds.");

        while (currentTime > 0)
        {
            if (countdownText != null)
                countdownText.text = $"Time left: {Mathf.CeilToInt(currentTime)}s";
            yield return new WaitForSeconds(1f);
            currentTime -= 1f;
        }

        if (buffPanel.activeSelf)
        {
            PickBuff(0);
        }
    }

    private void PickBuff(int index)
    {
        if (index < 0 || index >= currentBuffs.Count) return;

        Buff chosenBuff = currentBuffs[index];
        GameEvents.TriggerBuffChosen(chosenBuff);
        CloseUI();
    }

    private void RerollBuff(int index)
    {
        BuffButtonUI ui = buffButtons[index];
        if (ui.rerollCount >= 3)
        {
            ui.rerollButton.interactable = false;
            return;
        }

        Buff currentBuff = currentBuffs[index];
        List<Buff> sameTierBuffs = BuffLibrary.GetBuffsByTier(currentBuff.Tier);

        if (sameTierBuffs.Count <= 1) return;

        Buff newBuff;
        do
        {
            newBuff = sameTierBuffs[UnityEngine.Random.Range(0, sameTierBuffs.Count)];
        } while (newBuff == currentBuff);

        currentBuffs[index] = newBuff;

        ui.nameText.text = newBuff.Name;
        ui.tierText.text = newBuff.Tier.ToString();
        ui.descriptionText.text = newBuff.Description;
        if (ui.iconImage != null)
        {
            ui.iconImage.color = GetTierColor(newBuff.Tier);
            ui.iconImage.sprite = newBuff.IconSprite;
        }

        ui.rerollCount++;
        if (ui.rerollText != null)
            ui.rerollText.text = $"Reroll ({3 - ui.rerollCount})";

        if (ui.rerollCount >= 3)
            ui.rerollButton.interactable = false;
    }

    private void CloseUI()
    {
        isCountingDown = false;
        StopAllCoroutines();

        if (buffPanel != null) buffPanel.SetActive(false);
        if (rerollPanel != null) rerollPanel.SetActive(false);
        if (countdownText != null) countdownText.text = "";

        foreach (var ui in buffButtons)
        {
            if (ui.rerollButton != null)
                ui.rerollButton.gameObject.SetActive(false);
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnBuffChosenHandler(object sender, BuffChosenEventArgs args) { }

    private Color GetTierColor(BuffTier tier) => tier switch
    {
        BuffTier.Silver => Color.gray,
        BuffTier.Gold => Color.yellow,
        BuffTier.Diamond => Color.cyan,
        _ => Color.white,
    };
}

[System.Serializable]
public class BuffButtonUI
{
    public Button button;
    public TMP_Text nameText;
    public TMP_Text tierText;
    public TMP_Text descriptionText;
    public Image iconImage;
    public Button rerollButton;
    public TMP_Text rerollText;
    [HideInInspector] public int rerollCount = 0;
}
