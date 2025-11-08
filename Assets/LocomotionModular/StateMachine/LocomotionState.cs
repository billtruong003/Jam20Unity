// File: Assets/ModularTopDown/Locomotion/StateMachine/LocomotionState.cs
namespace ModularTopDown.Locomotion
{
    public abstract class LocomotionState
    {
        protected readonly LocomotionStateMachine stateMachine;
        protected readonly ILocomotionInput input;
        protected readonly CharacterLocomotion locomotion;
        protected readonly CharacterAnimator animator;

        public LocomotionState(LocomotionStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
            this.input = stateMachine.Input;
            this.locomotion = stateMachine.Locomotion;
            this.animator = stateMachine.Animator;
        }

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void Exit() { }
    }
}