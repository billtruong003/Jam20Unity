using UnityEngine;

[DefaultExecutionOrder(100)]
public class LazyFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Camera Positioning")]
    [SerializeField] private float distance = 10.0f;
    [SerializeField] private float height = 5.0f;

    [Header("Movement Smoothing")]
    [SerializeField] private float positionDamping = 2.0f;
    [SerializeField] private float rotationDamping = 3.0f;
    [SerializeField] private float deadZoneRadius = 1.0f;

    private Transform cameraTransform;

    private void Awake()
    {
        cameraTransform = transform;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        FollowTarget();
    }

    private void FollowTarget()
    {
        Vector3 targetPosition = target.position;
        Vector3 desiredPosition = targetPosition - (target.forward * distance) + (Vector3.up * height);

        if (Vector3.Distance(cameraTransform.position, desiredPosition) > deadZoneRadius)
        {
            cameraTransform.position = Vector3.Lerp(
                cameraTransform.position,
                desiredPosition,
                Time.deltaTime * positionDamping
            );
        }

        Quaternion desiredRotation = Quaternion.LookRotation(targetPosition - cameraTransform.position);
        cameraTransform.rotation = Quaternion.Slerp(
            cameraTransform.rotation,
            desiredRotation,
            Time.deltaTime * rotationDamping
        );
    }
}