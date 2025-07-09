using System;
using System.Collections;
using UnityEngine;

namespace PlayerStateMachine
{
    [Serializable]
    public class PlayerJumpState : BaseState<PlayerStateMachine.EState>
    {
        PlayerStateMachine player;

        private bool isAddForce = false;
        public PlayerJumpState(PlayerStateMachine.EState key, PlayerStateMachine context, int level) : base(key, context, level)
        {
            player = context;
        }
        public override void EnterState()
        {
            isAddForce = false;
            player.rootState.IsJumping = true;
        }
        public override void CheckTransition()
        {
            if (player.rigid.linearVelocity.y <= -1f)
            {
                TransitionToState(PlayerStateMachine.EState.Fall);
            }
        }

        public override void ExitState()
        {
            player.rootState.IsJumping = false;
        }
        public override void UpdateState()
        {
            CheckTransition();
            if(player.rootState.IsTurnOffGrounded == true && isAddForce == false){
                player.rigid.AddForce(Vector3.up * 500f * Time.deltaTime, ForceMode.Impulse);
                isAddForce = true;
            }
        }
    }
}