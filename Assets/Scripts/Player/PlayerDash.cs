using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Player
{
    public class PlayerDash : NetworkBehaviour
    {
        public float dashDistance = 5f;
        public float dashDuration = 0.55f;
        public float dashCooldown = 1f;
        public LayerMask dashObstacleMask;
        public float raycastHeight = 0.5f; // Height from ground to cast ray
        public float raycastDistance = 10f; // Maximum raycast distance

        private float lastDashTime;
        private Rigidbody rb;
        private Vector3 moveInput;
        private bool isDashing;

        private Animator animator;
        private TrailRenderer trail;
        private Camera mainCamera;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            trail = GetComponent<TrailRenderer>();

            if (IsOwner)
                mainCamera = Camera.main;

            if (trail != null)
                trail.emitting = false;
        }

        void Update()
        {
            if (!IsOwner || isDashing) return;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 camForward = mainCamera.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = mainCamera.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            moveInput = (camRight * h + camForward * v).normalized;

            if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastDashTime + dashCooldown)
            {
                Vector3 dashDirection = moveInput != Vector3.zero ? moveInput : transform.forward;
                DashServerRpc(dashDirection);
            }
        }

        [ServerRpc]
        void DashServerRpc(Vector3 direction, ServerRpcParams rpcParams = default)
        {
            if (isDashing) return;

            StartCoroutine(DashCoroutine(direction));
            DashEffectClientRpc();
        }

        [ClientRpc]
        void DashEffectClientRpc()
        {
            if (trail != null)
                trail.emitting = true;

            if (animator != null)
                animator.SetTrigger("Dash");
        
            StartCoroutine(StopTrailAfterDelay());
        }

        IEnumerator StopTrailAfterDelay()
        {
            yield return new WaitForSeconds(dashDuration);
            if (trail != null)
                trail.emitting = false;
        }

        private IEnumerator DashCoroutine(Vector3 direction)
        {
            isDashing = true;
            lastDashTime = Time.time;

            Vector3 startPos = rb.position;
            Vector3 dashTarget = startPos + direction * dashDistance;

            // Cast ray from slightly above ground to detect obstacles
            Vector3 rayStart = startPos + Vector3.up * raycastHeight;
            if (Physics.Raycast(rayStart, direction, out RaycastHit hit, raycastDistance, dashObstacleMask))
            {
                // Use the hit point as the target, but keep it at the same height as the player
                dashTarget = new Vector3(hit.point.x, startPos.y, hit.point.z);
            
                // Add a small offset to prevent getting stuck in walls
                dashTarget -= direction * 0.5f;
            }

            float elapsed = 0f;

            while (elapsed < dashDuration)
            {
                float t = elapsed / dashDuration;
                t = 1f - (1f - t) * (1f - t); // Ease out quad

                Vector3 newPos = Vector3.Lerp(startPos, dashTarget, t);
            
                // Keep the same height as the start position
                newPos.y = startPos.y;
            
                rb.MovePosition(newPos);
                
                // Set Y velocity to 0 to prevent vertical movement during dash
                Vector3 velocity = rb.linearVelocity;
                velocity.y = 0;
                rb.linearVelocity = velocity;

                elapsed += Time.deltaTime;
                yield return null;
            }

            rb.MovePosition(dashTarget);
            isDashing = false;
        }
    }
}
