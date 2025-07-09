using UnityEngine;

public class UIManager : MonoBehaviour 
{
    public GameObject inventoryPanel;
    public GameObject shopPanel;

    private static UIManager instance;
    public static UIManager Instance => instance;

    void Awake()
    {
        instance = this;
    }
}