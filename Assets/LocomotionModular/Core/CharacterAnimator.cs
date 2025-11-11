// File: Assets/ModularTopDown/Locomotion/Core/CharacterAnimator.cs
using UnityEngine;

namespace ModularTopDown.Locomotion
{
    public class CharacterAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        public void UpdateMoveSpeed(float normalizedSpeed)
        {
            animator.SetFloat(MoveSpeed, normalizedSpeed);
            // animator.SetFloat(MoveSpeed, normalizedSpeed, 0.1f, Time.deltaTime);
        }

        public void SetGrounded(bool isGrounded)
        {
            animator.SetBool(IsGrounded, isGrounded);
        }

        public void PlayTargetAnimation(string stateName, float crossFadeDuration = 0.1f)
        {
            animator.CrossFade(stateName, crossFadeDuration);
        }
    }
}