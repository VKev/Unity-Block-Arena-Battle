using UnityEngine;

namespace PlayerStateMachine
{
    public class PhaseHealthTester : MonoBehaviour
    {
        private PhaseHealth phaseHealth;

        void Awake()
        {
            phaseHealth = GetComponent<PhaseHealth>();
            if (phaseHealth == null)
            {
                Debug.LogError("PhaseHealthTester: No PhaseHealth component found on GameObject.");
            }
        }

        
    }
}