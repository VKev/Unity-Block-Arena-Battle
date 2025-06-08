using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public HealthBarUI healthBar;

    void Start()
    {
        healthBar.SetFill(currentHealth / maxHealth);
    }
    
    
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            TakeDamage(10f);
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        healthBar.SetFill(currentHealth / maxHealth);
    }
}