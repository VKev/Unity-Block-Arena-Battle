using UnityEngine;

namespace Skill
{
    public class EnemyController : MonoBehaviour
    {
        public int maxHealth = 100;
        private int currentHealth;

        private bool isStunned = false;
        private float stunTimer = 0f;

        public float moveSpeed = 3f;
        private Vector3 moveDirection;

        private Rigidbody rb;

        void Start()
        {
            currentHealth = maxHealth;
            rb = GetComponent<Rigidbody>();
        
            moveDirection = new Vector3(1, 0, 0);
        }

        void Update()
        {
            if (isStunned)
            {
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0f)
                {
                    isStunned = false;
                    Debug.Log($"{gameObject.name} hết stun!");
                }
            }
            else
            {
                Move();
            }
        }

        void Move()
        {
            rb.MovePosition(transform.position + moveDirection * moveSpeed * Time.deltaTime);
        }

        public void TakeDamage(int amount)
        {
            currentHealth -= amount;
            Debug.Log($"{gameObject.name} bị trúng đòn! Máu còn: {currentHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Stun(float duration = 2f)
        {
            isStunned = true;
            stunTimer = duration;
            Debug.Log($"{gameObject.name} bị stun trong {duration} giây!");
        }

        void Die()
        {
            Debug.Log($"{gameObject.name} chết.");
            Destroy(gameObject);
        }
    }
}