using UnityEngine;


namespace PlayerStateMachine
{
    public class PlayerInAirState : BaseState<PlayerStateMachine.EState>
    {
        PlayerStateMachine player;
        public PlayerInAirState(PlayerStateMachine.EState key, PlayerStateMachine context, int level) : base(key, context, level)
        {
            player = context;
        }

        public override void EnterState()
        {
            player.rigid.useGravity = true;

            if(player.rootState.IsJumping == true){
                SetSubState(PlayerStateMachine.EState.Jump);
            }
        }
        public override void ExitState()
        {
            player.rigid.useGravity = false;
        }
        public override void CheckTransition()
        {
            if (player.rootState.IsGrounded && player.rootState.IsJumping == false)
            {
                TransitionToState(PlayerStateMachine.EState.Grounded);
            }
        } 
        public override void UpdateState()
        {
            CheckTransition();
        }
    }
}