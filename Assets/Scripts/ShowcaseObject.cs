#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.EventSystems;

public class ShowcaseObject : MonoBehaviour
{
    public enum IdleBehavior
    {
        Static,
        AutoRotate,
        Floating,
        FloatingAndAutoRotate
    }

    [Header("Core Dependencies")]
    [Tooltip("The camera used for interaction.")]
    [SerializeField] private Camera interactionCamera;

    [Header("Idle Behavior")]
    [SerializeField] private IdleBehavior idleBehavior = IdleBehavior.FloatingAndAutoRotate;

    [Header("Manual Rotation")]
    [Tooltip("The sensitivity of rotation based on mouse movement.")]
    [SerializeField] private float rotationSensitivity = 1.0f;

    [Header("Auto-Rotation")]
    [SerializeField] private float autoRotationSpeed = 10f;
    [SerializeField] private Vector3 autoRotationAxis = Vector3.up;

    [Header("Floating")]
    [SerializeField] private float floatAmplitude = 0.1f;
    [SerializeField] private float floatFrequency = 0.5f;

    private Vector3 initialPosition;
    private bool isBeingInteractedWith = false;

    // --- Gizmo & Debugging Data ---
    private Vector3 lastPlaneHitPoint;
    private RaycastHit initialHit;
    private Plane interactionPlane;
    private bool shouldDrawGizmos = false;

    private void Awake()
    {
        ValidateDependencies();
        initialPosition = transform.position;
    }

    private void Update()
    {
        HandleInput();

        if (isBeingInteractedWith)
        {
            HandleManualRotation();
            shouldDrawGizmos = true;
        }
        else
        {
            ApplyIdleBehavior();
            shouldDrawGizmos = false;
        }
    }

    private void ValidateDependencies()
    {
        if (interactionCamera == null)
        {
            Debug.LogError("ShowcaseObject Error: Interaction Camera has not been assigned. Disabling component.", this);
            enabled = false;
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI()) return;

            if (TryStartInteraction(out RaycastHit hitInfo))
            {
                isBeingInteractedWith = true;
                initialHit = hitInfo;
                interactionPlane = new Plane(-interactionCamera.transform.forward, initialHit.point);
                TryGetPlaneHitPoint(out lastPlaneHitPoint);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isBeingInteractedWith = false;
        }
    }

    private bool TryStartInteraction(out RaycastHit hitInfo)
    {
        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out hitInfo) && hitInfo.collider.gameObject == gameObject;
    }

    private void HandleManualRotation()
    {
        if (!TryGetPlaneHitPoint(out Vector3 currentPlaneHitPoint)) return;

        Vector3 moveVector = currentPlaneHitPoint - lastPlaneHitPoint;

        Vector3 pivotPoint = transform.position;
        Vector3 xAxis = interactionCamera.transform.right;
        Vector3 yAxis = interactionCamera.transform.up;

        transform.RotateAround(pivotPoint, yAxis, -moveVector.x * rotationSensitivity);
        transform.RotateAround(pivotPoint, xAxis, moveVector.y * rotationSensitivity);

        lastPlaneHitPoint = currentPlaneHitPoint;
    }

    private bool TryGetPlaneHitPoint(out Vector3 point)
    {
        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        if (interactionPlane.Raycast(ray, out float enter))
        {
            point = ray.GetPoint(enter);
            return true;
        }
        point = Vector3.zero;
        return false;
    }

    private void ApplyIdleBehavior()
    {
        // ... (The rest of the idle behavior code remains unchanged)
        switch (idleBehavior)
        {
            case IdleBehavior.AutoRotate:
                ApplyAutoRotation();
                break;
            case IdleBehavior.Floating:
                ApplyFloating();
                break;
            case IdleBehavior.FloatingAndAutoRotate:
                ApplyAutoRotation();
                ApplyFloating();
                break;
            case IdleBehavior.Static:
            default:
                break;
        }
    }

    private void ApplyAutoRotation()
    {
        transform.Rotate(autoRotationAxis, autoRotationSpeed * Time.deltaTime, Space.World);
    }

    private void ApplyFloating()
    {
        float sineWaveOffset = Mathf.Sin(Time.time * Mathf.PI * floatFrequency) * floatAmplitude;
        transform.position = initialPosition + new Vector3(0, sineWaveOffset, 0);
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !shouldDrawGizmos || interactionCamera == null)
        {
            return;
        }

        // --- Draw the initial hit information ---
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(initialHit.point, 0.05f); // Initial click point on the object surface

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(initialHit.point, initialHit.point + initialHit.normal * 0.5f); // Surface normal at hit point

        // --- Draw the interaction plane and the current mouse position on it ---
        if (TryGetPlaneHitPoint(out Vector3 currentPointOnPlane))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentPointOnPlane, 0.04f); // Current mouse drag point on the plane

            // Draw the plane itself as a wireframe square
            Vector3 planeCenter = initialHit.point;
            Vector3 planeUp = Vector3.Cross(interactionPlane.normal, interactionCamera.transform.right).normalized;
            Vector3 planeRight = interactionCamera.transform.right;

            float planeSize = 2.0f;
            Vector3 p1 = planeCenter + planeRight * planeSize + planeUp * planeSize;
            Vector3 p2 = planeCenter + planeRight * planeSize - planeUp * planeSize;
            Vector3 p3 = planeCenter - planeRight * planeSize - planeUp * planeSize;
            Vector3 p4 = planeCenter - planeRight * planeSize + planeUp * planeSize;

            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.5f);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);

            // Add labels for clarity
            Handles.color = Color.white;
            Handles.Label(initialHit.point + initialHit.normal * 0.1f, "Initial Hit Point");
            Handles.Label(currentPointOnPlane, "Current Drag Point");
        }

        // --- Draw the main ray from the camera ---
        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * 100f);
    }
#endif
}