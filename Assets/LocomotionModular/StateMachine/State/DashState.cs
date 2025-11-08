// File: Assets/ModularTopDown/Locomotion/StateMachine/States/DashState.cs
namespace ModularTopDown.Locomotion
{
    public class DashState : LocomotionState
    {
        private float timer;

        public DashState(LocomotionStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            timer = stateMachine.dashDuration;
            input.ConsumeDashInput();
            animator.PlayTargetAnimation("Dash");
        }

        public override void Tick(float deltaTime)
        {
            timer -= deltaTime;
            locomotion.HandleDash(stateMachine.dashSpeed);

            if (timer <= 0)
            {
                if (locomotion.IsGrounded())
                {
                    stateMachine.SwitchState(new GroundedState(stateMachine));
                }
                else
                {
                    stateMachine.SwitchState(new FallState(stateMachine));
                }
            }
        }
    }
}