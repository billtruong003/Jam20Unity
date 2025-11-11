using UnityEngine;

namespace ModularTopDown.Locomotion
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterLocomotion : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private CharacterAnimator characterAnimator;

        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 7f;
        [SerializeField] private float airControlSpeed = 5f;

        [Header("Rotation & Gravity")]
        [SerializeField] private float rotationSpeed = 15f;
        [SerializeField] private float gravity = -20.0f;

        [Header("Physics & Feel Improvements")]
        [SerializeField] private float movementSmoothTime = 0.1f;
        [SerializeField] private float speedSmoothTime = 0.05f; // Smooth riêng cho speed

        [SerializeField] private float slopeSlideSpeed = 8f;
        [SerializeField] private float coyoteTime = 0.15f;


        [Header("Ground Check Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 1.1f;

        private CharacterController controller;
        private Vector3 playerVelocity;
        private Vector3 moveDampVelocity;
        private float coyoteTimeCounter;
        private bool isCurrentlyGrounded;
        private Vector3 groundNormal;
        private int jumpsLeft;
        private int currentMaxJumps;
        private float currentSpeed; // Speed cứng, không jitter
        private float speedDampVelocity;

        public Vector3 PlayerVelocity => playerVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (characterAnimator == null) characterAnimator = GetComponentInChildren<CharacterAnimator>();
        }

        private void Update()
        {
            PerformGroundCheck();
            HandleGravity();
        }

        public void ConfigureJumps(bool allowDoubleJump)
        {
            currentMaxJumps = allowDoubleJump ? 2 : 1;
            jumpsLeft = currentMaxJumps;
        }

        private void PerformGroundCheck()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
            {
                if (!isCurrentlyGrounded)
                {
                    groundNormal = hit.normal;
                    jumpsLeft = currentMaxJumps;
                }
                isCurrentlyGrounded = true;
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                isCurrentlyGrounded = false;
                coyoteTimeCounter -= Time.deltaTime;
            }
        }

        public bool IsGrounded()
        {
            return isCurrentlyGrounded || controller.isGrounded;
        }

        public bool PerformJump(float jumpHeight, float doubleJumpHeight, out bool isDoubleJump)
        {
            isDoubleJump = false;
            if (coyoteTimeCounter > 0f)
            {
                jumpsLeft = currentMaxJumps - 1;
                coyoteTimeCounter = 0f;
                playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                return true;
            }

            if (jumpsLeft > 0)
            {
                isDoubleJump = true;
                jumpsLeft--;
                playerVelocity.y = Mathf.Sqrt(doubleJumpHeight * -2f * gravity);
                return true;
            }

            return false;
        }

        private void HandleGravity()
        {
            if (IsGrounded() && playerVelocity.y < 0)
            {
                playerVelocity.y = -2f;
            }
            else
            {
                playerVelocity.y += gravity * Time.deltaTime;
            }
        }

        private Vector3 CalculateWorldSpaceMoveDirection(Vector2 moveInput)
        {
            return new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }

        public void HandleGroundedMovement(Vector2 moveInput, bool isRunning)
        {
            // TÍNH TARGET SPEED TRƯỚC - ĐÂY LÀ SPEED CỨNG
            float targetSpeed = moveInput.magnitude * (isRunning ? runSpeed : walkSpeed);
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedDampVelocity, speedSmoothTime);

            // TÍNH MOVE DIRECTION RIÊNG
            Vector3 moveDirection = CalculateWorldSpaceMoveDirection(moveInput);
            Vector3 targetMoveVector = moveDirection * currentSpeed;

            // Smooth velocity như cũ
            Vector3 horizontalVelocity = new Vector3(playerVelocity.x, 0, playerVelocity.z);
            horizontalVelocity = Vector3.SmoothDamp(horizontalVelocity, targetMoveVector, ref moveDampVelocity, movementSmoothTime);
            playerVelocity.x = horizontalVelocity.x;
            playerVelocity.z = horizontalVelocity.z;

            // Apply slope slide NHƯNG KHÔNG LÀM SPEED JITTER
            Vector3 finalMove = HandleSlopeSlide(playerVelocity);

            // DÙNG currentSpeed cho animator - KHÔNG DÙNG physical speed!
            if (moveInput.magnitude > 0 || moveInput.magnitude < 0)
            {
                characterAnimator.UpdateMoveSpeed(currentSpeed / runSpeed);
            }
            else
            {
                characterAnimator.UpdateMoveSpeed(0);
            }
            HandleRotation(moveDirection);
            controller.Move(finalMove * Time.deltaTime);
        }

        private Vector3 HandleSlopeSlide(Vector3 currentVelocity)
        {
            if (!isCurrentlyGrounded) return currentVelocity;

            float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);
            if (slopeAngle > controller.slopeLimit)
            {
                // Chỉ override horizontal velocity khi slide, giữ nguyên speed tracking
                Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                return new Vector3(slideDirection.x * slopeSlideSpeed, currentVelocity.y, slideDirection.z * slopeSlideSpeed);
            }
            return currentVelocity;
        }

        public void HandleAirborneMovement(Vector2 moveInput)
        {
            Vector3 moveDirection = CalculateWorldSpaceMoveDirection(moveInput);

            playerVelocity.x = Mathf.Lerp(playerVelocity.x, moveDirection.x * airControlSpeed, Time.deltaTime * airControlSpeed * 0.5f);
            playerVelocity.z = Mathf.Lerp(playerVelocity.z, moveDirection.z * airControlSpeed, Time.deltaTime * airControlSpeed * 0.5f);

            HandleRotation(moveDirection);
            controller.Move(playerVelocity * Time.deltaTime);
        }

        public void HandleRotation(Vector3 moveDirection)
        {
            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }

        public void HandleDash(float dashSpeed)
        {
            Vector3 dashDirection = transform.forward;
            Vector3 currentMoveDirection = new Vector3(playerVelocity.x, 0, playerVelocity.z);
            if (currentMoveDirection.magnitude > 0.1f)
            {
                dashDirection = currentMoveDirection.normalized;
            }
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up, playerVelocity);

            Vector3 rayStartPoint = transform.position + Vector3.up * 0.1f;

            bool hasHit = Physics.Raycast(rayStartPoint, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer);

            Gizmos.color = (Application.isPlaying ? isCurrentlyGrounded : hasHit) ? Color.green : Color.red;
            Gizmos.DrawRay(rayStartPoint, Vector3.down * groundCheckDistance);

            if (hasHit)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(hit.point, 0.05f);

                Gizmos.color = Color.blue;
                Gizmos.DrawRay(hit.point, hit.normal * 0.5f);
            }
        }
#endif
    }
}