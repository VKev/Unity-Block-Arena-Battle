// Inside your ActiveBuffDisplayUI.cs file
using buffSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Needed for UI components

public class ActiveBuffDisplayUI : MonoBehaviour
{
    [Header("UI Setup")]
    [Tooltip("The parent GameObject that will contain all the buff icon instances. Add a Horizontal/Grid Layout Group to it.")]
    [SerializeField] private GameObject _iconContainer; // Can be left unassigned in Inspector if auto-assigned

    [Tooltip("The prefab with the BuffIconUI script and an Image component. This will be instantiated for each active buff.")]
    [SerializeField] private GameObject _buffIconUIPrefab; 

    private List<BuffIconUI> _activeIconUIs = new List<BuffIconUI>();

    private void Awake()
    {
      
        if (_iconContainer == null)
        {
            _iconContainer = this.gameObject; // Assign THIS GameObject as the container
            Debug.Log($"ActiveBuffDisplayUI: Auto-assigned _iconContainer to '{_iconContainer.name}' (this GameObject).", this);
        }

        
        if (_buffIconUIPrefab == null)
        {
            Debug.LogWarning("ActiveBuffsDisplayUI: _buffIconUIPrefab is not assigned. Please assign the 'BuffIcon_Prefab' asset manually in the Inspector.", this);
        }

        // Initial check for required components on the container
        if (_iconContainer != null && _iconContainer.GetComponent<LayoutGroup>() == null)
        {
            Debug.LogError($"ActiveBuffsDisplayUI: _iconContainer ('{_iconContainer.name}') is missing a Layout Group component! Buff icons won't arrange correctly. Please add Horizontal Layout Group or Grid Layout Group.", _iconContainer);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnActiveBuffsChanged += UpdateActiveBuffsDisplay;
        Debug.Log("ActiveBuffsDisplayUI: Subscribed to OnActiveBuffsChanged event.");
    }

    private void OnDisable()
    {
        GameEvents.OnActiveBuffsChanged -= UpdateActiveBuffsDisplay;
        Debug.Log("ActiveBuffsDisplayUI: Unsubscribed from OnActiveBuffsChanged event.");
    }

    private void UpdateActiveBuffsDisplay(List<Buff> activeBuffs)
    {
        if (_iconContainer == null || _buffIconUIPrefab == null)
        {
            Debug.LogWarning("ActiveBuffsDisplayUI: _iconContainer or _buffIconUIPrefab is not assigned. Cannot display buffs.", this);
            return;
        }

        foreach (BuffIconUI iconUI in _activeIconUIs)
        {
            Destroy(iconUI.gameObject);
        }
        _activeIconUIs.Clear();

        if (activeBuffs == null || activeBuffs.Count == 0)
        {
            Debug.Log("ActiveBuffsDisplayUI: No active buffs to display. UI cleared.");
            return;
        }

        foreach (Buff buff in activeBuffs)
        {
            GameObject newIconGO = Instantiate(_buffIconUIPrefab, _iconContainer.transform);
            BuffIconUI newIconUI = newIconGO.GetComponent<BuffIconUI>();

            if (newIconUI != null)
            {
                newIconUI.SetBuff(buff);
                _activeIconUIs.Add(newIconUI);
            }
            else
            {
                Debug.LogError($"ActiveBuffsDisplayUI: The prefab '{_buffIconUIPrefab.name}' is missing the 'BuffIconUI' script! Cannot display buff.", _buffIconUIPrefab);
                Destroy(newIconGO);
            }
        }
        Debug.Log($"ActiveBuffsDisplayUI: Display updated with {_activeIconUIs.Count} active buff icons.");
    }
}