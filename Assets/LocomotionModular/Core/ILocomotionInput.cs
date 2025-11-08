// File: Assets/ModularTopDown/Locomotion/Core/ILocomotionInput.cs
using UnityEngine;

namespace ModularTopDown.Locomotion
{
    public interface ILocomotionInput
    {
        Vector2 MoveInput { get; }
        bool IsRunning { get; }
        bool JumpInput { get; }
        bool DashInput { get; }

        void ConsumeJumpInput();
        void ConsumeDashInput();
    }
}