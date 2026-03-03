using UnityEngine;

namespace TestAI.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float airMultiplier = 0.4f;
        [SerializeField] private float groundDrag = 5f;

        [Header("Ground Check")]
        [SerializeField] private float playerHeight = 2f;
        [SerializeField] private LayerMask groundLayer;

        private Rigidbody rb;
        private Vector3 moveDirection;
        private bool isGrounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private void Update()
        {
            UpdateGroundStatus();
            HandleInput();
            ApplyDrag();
        }

        private void FixedUpdate()
        {
            ApplyMovement();
        }

        private void UpdateGroundStatus()
        {
            isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, groundLayer);
        }

        private void HandleInput()
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");

            moveDirection = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;

            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                Jump();
            }
        }

        private void ApplyDrag()
        {
            rb.linearDamping = isGrounded ? groundDrag : 0f;
        }

        private void ApplyMovement()
        {
            float currentMaxSpeed = GetTargetSpeed();
            float forceMultiplier = 10f;

            if (isGrounded)
            {
                rb.AddForce(moveDirection * currentMaxSpeed * forceMultiplier, ForceMode.Force);
            }
            else
            {
                rb.AddForce(moveDirection * currentMaxSpeed * forceMultiplier * airMultiplier, ForceMode.Force);
            }

            LimitVelocity(currentMaxSpeed);
        }

        private float GetTargetSpeed()
        {
            return Input.GetKey(KeyCode.LeftShift) ? moveSpeed * sprintMultiplier : moveSpeed;
        }

        private void LimitVelocity(float maxSpeed)
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            if (horizontalVelocity.magnitude > maxSpeed)
            {
                Vector3 limitedVelocity = horizontalVelocity.normalized * maxSpeed;
                rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
            }
        }

        private void Jump()
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}