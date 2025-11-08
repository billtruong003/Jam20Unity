using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class RigidbodyBallController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveForce = 15f;
    [SerializeField] private float maxAngularVelocity = 25f;

    private Rigidbody rigidbodyComponent;
    private Transform cameraTransform;

    private void Awake()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        rigidbodyComponent = GetComponent<Rigidbody>();
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        rigidbodyComponent.maxAngularVelocity = maxAngularVelocity;
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = GetMoveDirection(horizontalInput, verticalInput);
        rigidbodyComponent.AddForce(moveDirection * moveForce);
    }

    private Vector3 GetMoveDirection(float horizontal, float vertical)
    {
        if (cameraTransform != null)
        {
            Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camRight = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized;
            return (camForward * vertical + camRight * horizontal);
        }

        return new Vector3(horizontal, 0f, vertical);
    }
}