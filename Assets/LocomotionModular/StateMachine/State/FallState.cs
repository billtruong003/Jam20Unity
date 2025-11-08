// File: Assets/ModularTopDown/Locomotion/StateMachine/States/FallState.cs
namespace ModularTopDown.Locomotion
{
    public class FallState : LocomotionState
    {
        public FallState(LocomotionStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            animator.PlayTargetAnimation("Fall");
        }

        public override void Tick(float deltaTime)
        {
            if (input.JumpInput)
            {
                stateMachine.SwitchState(new JumpState(stateMachine));
                return;
            }

            if (stateMachine.canDash && input.DashInput)
            {
                stateMachine.SwitchState(new DashState(stateMachine));
                return;
            }

            locomotion.HandleAirborneMovement(input.MoveInput);

            if (locomotion.IsGrounded())
            {
                stateMachine.SwitchState(new GroundedState(stateMachine));
            }
        }
    }
}