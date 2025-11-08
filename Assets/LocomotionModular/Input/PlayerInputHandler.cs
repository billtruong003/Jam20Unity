// File: Assets/ModularTopDown/Locomotion/Input/PlayerInputHandler.cs
using UnityEngine;

namespace ModularTopDown.Locomotion
{
    public class PlayerInputHandler : MonoBehaviour, ILocomotionInput
    {
        public Vector2 MoveInput { get; private set; }
        public bool IsRunning { get; private set; }
        public bool JumpInput { get; private set; }
        public bool DashInput { get; private set; }

        private bool jumpInputConsumed;
        private bool dashInputConsumed;

        void Update()
        {
            if (jumpInputConsumed) { JumpInput = false; }
            if (dashInputConsumed) { DashInput = false; }

            MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            IsRunning = Input.GetKey(KeyCode.LeftShift);

            // if (Input.GetButtonDown("Jump")) { JumpInput = true; jumpInputConsumed = false; }
            if (Input.GetKeyDown(KeyCode.Z)) { DashInput = true; dashInputConsumed = false; }
        }

        public void ConsumeJumpInput() => jumpInputConsumed = true;
        public void ConsumeDashInput() => dashInputConsumed = true;
    }
}