// File: Assets/ModularTopDown/Locomotion/StateMachine/States/JumpState.cs
using UnityEngine;

namespace ModularTopDown.Locomotion
{
    public class JumpState : LocomotionState
    {
        private const float jumpStateMinDuration = 0.2f;
        private float timer;

        public JumpState(LocomotionStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            timer = jumpStateMinDuration;
            input.ConsumeJumpInput();
            animator.UpdateMoveSpeed(0f);

            bool jumpSucceeded = locomotion.PerformJump(
                stateMachine.jumpHeight,
                stateMachine.doubleJumpHeight,
                out bool wasDoubleJump
            );

            if (jumpSucceeded)
            {
                animator.PlayTargetAnimation(wasDoubleJump ? "DoubleJump" : "Jump");
            }
            else
            {
                stateMachine.SwitchState(new FallState(stateMachine));
            }
        }

        public override void Tick(float deltaTime)
        {
            timer -= deltaTime;

            if (stateMachine.canDash && input.DashInput)
            {
                stateMachine.SwitchState(new DashState(stateMachine));
                return;
            }


            locomotion.HandleAirborneMovement(input.MoveInput);

            if (timer <= 0 && locomotion.PlayerVelocity.y <= 0)
            {
                stateMachine.SwitchState(new FallState(stateMachine));
            }
        }
    }
}