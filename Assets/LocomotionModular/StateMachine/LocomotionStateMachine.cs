// File: Assets/ModularTopDown/Locomotion/StateMachine/LocomotionStateMachine.cs
using UnityEngine;

namespace ModularTopDown.Locomotion
{
    public class LocomotionStateMachine : MonoBehaviour
    {
        [field: Header("Dependencies")]
        [field: SerializeField] public CharacterLocomotion Locomotion { get; private set; }
        [field: SerializeField] public CharacterAnimator Animator { get; private set; }

        [Header("State Parameters")]
        public float jumpHeight = 1.5f;
        public float doubleJumpHeight = 1.3f;
        public float dashDuration = 0.4f;
        public float dashSpeed = 15f;

        [Header("Abilities")]
        public bool canDash = true;
        public bool canDoubleJump = true;

        public ILocomotionInput Input { get; private set; }
        public LocomotionState CurrentState { get; private set; }

        void Awake()
        {
            Input = GetComponent<ILocomotionInput>();
        }

        void Start()
        {
            Locomotion.ConfigureJumps(canDoubleJump);

            SwitchState(new GroundedState(this));
        }

        void Update()
        {
            CurrentState?.Tick(Time.deltaTime);
            Animator.SetGrounded(Locomotion.IsGrounded());
        }

        public void SwitchState(LocomotionState newState)
        {
            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState?.Enter();
        }
    }
}