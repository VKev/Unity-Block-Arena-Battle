using UnityEngine;
using UnityEngine.UI;
using buffSystem; 

public class BuffIconUI : MonoBehaviour
{
    [SerializeField] private Image _buffImage; 
    [Header("Tier Colors (if no icon sprite)")]
    [SerializeField] private Color _silverColor = Color.grey;
    [SerializeField] private Color _goldColor = Color.yellow;
    [SerializeField] private Color _diamondColor = Color.cyan;

    public Buff CurrentBuff { get; private set; }

    private void Awake()
    {
        if (_buffImage == null)
        {
            _buffImage = GetComponent<Image>();
            if (_buffImage == null)
            {
                Debug.LogError("BuffIconUI: No Image component found on this GameObject or assigned! Cannot display buff.", this);
            }
        }
    }

  
    public void SetBuff(Buff buff)
    {
        CurrentBuff = buff; 

        if (_buffImage == null)
        {
            Debug.LogWarning("BuffIconUI: _buffImage is null, cannot set visual for buff: " + buff.Name, this);
            return;
        }

        if (buff.IconSprite != null) // Use the Sprite if it exists
        {
            _buffImage.sprite = buff.IconSprite;
            _buffImage.color = Color.white; 
            _buffImage.enabled = true; 
        }
        else
        {
            _buffImage.sprite = null; // Clear any previous sprite
            _buffImage.color = GetTierColor(buff.Tier);
            _buffImage.enabled = true;
        }
    }

    private Color GetTierColor(BuffTier tier)
    {
        return tier switch
        {
            BuffTier.Silver => _silverColor,
            BuffTier.Gold => _goldColor,
            BuffTier.Diamond => _diamondColor,
            _ => Color.white, // Default fallback color
        };
    }

    /// <summary>
    /// Hides this buff icon.
    /// </summary>
    public void Hide()
    {
        _buffImage.enabled = false;
        _buffImage.sprite = null;
        _buffImage.color = Color.clear; // Make it fully transparent
        CurrentBuff = null;
    }
}