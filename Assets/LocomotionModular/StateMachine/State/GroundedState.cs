// File: Assets/ModularTopDown/Locomotion/StateMachine/States/GroundedState.cs
namespace ModularTopDown.Locomotion
{
    public class GroundedState : LocomotionState
    {
        public GroundedState(LocomotionStateMachine stateMachine) : base(stateMachine) { }

        public override void Tick(float deltaTime)
        {
            if (!locomotion.IsGrounded())
            {
                stateMachine.SwitchState(new FallState(stateMachine));
                return;
            }

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

            locomotion.HandleGroundedMovement(input.MoveInput, input.IsRunning);
        }
    }
}