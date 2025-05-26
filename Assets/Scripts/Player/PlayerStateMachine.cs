using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
namespace PlayerStateMachine
{
    public class PlayerStateMachine : StateManager<PlayerStateMachine.EState>
    {
        public enum EState
        {
            Root, Idle, Slide, InAir, Run,
            Grounded, 
        }
        [SerializeField] private bool showGizmos;
        [SerializeField] private bool stateDebug;
        [SerializeField] public PlayerRootState rootState;
        [SerializeField] public PlayerGroundedState groundedState;
        [SerializeField] public PlayerInAirState inAirState;
        [SerializeField] public PlayerRunState runState; 
        [SerializeField] public PlayerIdleState idleState;


        [HideInInspector] public Animator anim;
        [HideInInspector] public Rigidbody rigid;
        [HideInInspector] public Camera mainCamera;
        [HideInInspector] public CapsuleCollider mainCollider;

        public float ColliderHeight { get; private set; }
        public float ColliderRadius { get; private set; }


        public const string VERTICAL_HEIGHT_PERCENTAGE = "Vertical_ClampHeight";
        public const string HORIZONTAL_VELOCITY_PERCENTAGE = "Horizontal_ClampVelocity";

        public void Awake()
        {
            rigid = GetComponent<Rigidbody>();
            anim = GetComponent<Animator>();
            mainCollider = GetComponent<CapsuleCollider>();

            ColliderHeight = GetComponent<CapsuleCollider>().height * transform.lossyScale.y;
            ColliderRadius = GetComponent<CapsuleCollider>().radius * transform.lossyScale.x;

            mainCamera = Camera.main;

            // Initialize states here instead of in constructor
            rootState = new PlayerRootState(EState.Root, this, 0);
            groundedState = new PlayerGroundedState(EState.Grounded, this, 1);
            inAirState = new PlayerInAirState(EState.InAir, this, 1);
            runState = new PlayerRunState(EState.Run, this, 2);
            idleState = new PlayerIdleState(EState.Idle, this, 2);

            States[EState.Root] = rootState;
            States[EState.Grounded] = groundedState;
            States[EState.InAir] = inAirState;
            States[EState.Idle] = idleState;
            States[EState.Run] = runState;
        }

        public void Start()
        {
            // showGizmos = true;

            // States[EState.Root] = rootState;
            // States[EState.Grounded] = groundedState;
            // States[EState.Idle] = idleState;
            // States[EState.Run] = runState;
            States[EState.Root].SetSubState(EState.InAir);
            States[EState.Grounded].SetSubState(EState.Idle);
            // States[EState.Grounded].SetSubState(EState.Idle);

            CurrentState = States[EState.Root];
            CurrentState.EnterState();
        }

        private void OnDrawGizmos()
        {
            if (showGizmos)
                States[EState.Root].OnDrawGizmos();
        }

        public void Update()
        {
            CurrentState.UpdateStates();
            if (stateDebug)
                Debug.Log(CurrentState.GetAllCurrentStatesToString());
        }
        public void FixedUpdate()
        {
            CurrentState.FixedUpdateStates();
        }
        public void LateUpdate()
        {
            CurrentState.LateUpdateStates();
        }
        private void OnAnimatorIK(int layerIndex)
        {
            CurrentState.AnimationIKState(layerIndex);
        }
    }
}
