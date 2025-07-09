
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerStateMachine
{
    [Serializable]
    public class PlayerGroundedState : BaseState<PlayerStateMachine.EState>
    {

        PlayerStateMachine player;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float acceleration = 2f;
        [SerializeField] private float deceleration = 2f;
        [SerializeField] private float walkRunSpeedRatio = 0.5f;
        [SerializeField] private float maxSlopeAngle = 50f;
        [SerializeField] private float maxStairHeight = 0.6f;
        [SerializeField] private float maxRoughSurfaceHeight = 0.05f;
        public float MaxSlopeAngle { get { return maxSlopeAngle; } }
        public float MaxStairHeight { get { return maxStairHeight; } }
        public float MaxRoughSurfaceHeight { get { return maxRoughSurfaceHeight; } }

        public float MoveSpeed 
        { 
            get 
            { 
                var playerStats = player.GetComponent<playerStat.PlayerBaseStats>();
                return playerStats != null ? playerStats.MoveSpeed : 500f; // fallback to 500f if no stats component
            } 
        }
        public float RotationSpeed { get { return rotationSpeed; } }
        public float Acceleration { get { return acceleration; } }
        public float Deceleration { get { return deceleration; } }
        public float WalkRunSpeedRatio { get { return walkRunSpeedRatio; } }

        public PlayerGroundedState(PlayerStateMachine.EState key, PlayerStateMachine context, int level) : base(key, context, level)
        {
            player = context;
        }
        public override void UpdateState()
        {
            Shader.SetGlobalVector("_PlayerWpos", player.transform.position);
            HandleStairMovement();
            CheckTransition();
        }
        public override void EnterState()
        {
            if (InputController.RunAction.isPressed){
                SetSubState(PlayerStateMachine.EState.Run);
            }else{
                SetSubState(PlayerStateMachine.EState.Idle);
            }

            player.rigid.linearVelocity = new Vector3(player.rigid.linearVelocity.x, 0f, player.rigid.linearVelocity.z);
            if (CurrentSubState.StateKey == PlayerStateMachine.EState.Idle)
                player.rigid.linearVelocity = Vector3.zero;

            player.rigid.useGravity = false;

            player.rootState.FloatingHeight = player.rootState.maxFloatingHeight;

            //GameEvents.OnSpeedChange += HandleSpeedChange;
            Debug.Log($"[PlayerGroundedState] Entered state with speed {MoveSpeed}");
        }
        public override void ExitState()
        {
            //GameEvents.OnSpeedChange -= HandleSpeedChange;

        }
        public override void CheckTransition()
        {
            if (!player.rootState.IsGrounded)
            {
                TransitionToState(PlayerStateMachine.EState.InAir);
            }else if (player.rootState.IsJumping == true){
                TransitionToState(PlayerStateMachine.EState.InAir);
            }
        }

        private float positionOffsetY_SmoothDamp;
        private void HandleStairMovement()
        {
            float changeDistance = Mathf.Abs(player.rootState.DistanceToGround - player.rootState.FloatingHeight);
            if (changeDistance < maxStairHeight && (player.rootState.IsJumping == false))
            {
                float positionOffsetY = 0f;
                float floatingToDistance = player.rootState.FloatingHeight - player.rootState.DistanceToGround;

                positionOffsetY = Mathf.SmoothDamp(positionOffsetY, floatingToDistance,
                                                   ref positionOffsetY_SmoothDamp, Time.deltaTime * 3f);
                player.transform.position = player.transform.position + new Vector3(0, positionOffsetY, 0);

            }
        }

        private void HandleSpeedChange(float newSpeed)
        {
            Debug.Log($"[PlayerGroundedState] Speed current  {MoveSpeed}");
            // Speed is now handled by PlayerBaseStats, so we don't need to set it here
            Debug.Log($"[PlayerGroundedState] Speed updated to {newSpeed}");

        }

        public override void OnAnimationIK(int layerIndex)
        {
        }

        public override void OnDrawGizmos()
        {
        }
    

    }
}