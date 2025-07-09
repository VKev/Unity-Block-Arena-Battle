using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerStateMachine
{
    [Serializable]
    public class PlayerIdleState : BaseState<PlayerStateMachine.EState>
    {
        PlayerStateMachine player; 
        public PlayerIdleState(PlayerStateMachine.EState key, PlayerStateMachine context, int level) : base(key, context, level)
        {
            player = context;
        }
        private void WalkActionPerform(InputAction.CallbackContext context)
        {
            TransitionToState(PlayerStateMachine.EState.Run);
            
        }
        public override void EnterState()
        {
            Debug.Log("WalkActionPerform"); 
            InputController.WalkAction.AddPerformed(WalkActionPerform);
        }


        public override void UpdateState()
        {

        }
        public override void LateUpdateState()
        {

        }

        public override void ExitState()
        {
            InputController.WalkAction.RemovePerformed(WalkActionPerform);
        }
    }
}