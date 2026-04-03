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
    [Tooltip("Camera dùng cho tương tác. Nếu để trống sẽ tự tìm Camera.main.")]
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
    private bool _hasCamera = false;

    // --- Gizmo & Debugging Data ---
    private Vector3 lastPlaneHitPoint;
    private RaycastHit initialHit;
    private Plane interactionPlane;
    private bool shouldDrawGizmos = false;

    private void Awake()
    {
        initialPosition = transform.position;
        ResolveCamera();
    }

    /// <summary>
    /// [FIX] Tự tìm camera nếu không được gán trong Inspector.
    /// Không disable component — idle behavior vẫn hoạt động bình thường dù không có camera.
    /// Chỉ disable phần tương tác chuột.
    /// </summary>
    private void ResolveCamera()
    {
        if (interactionCamera != null)
        {
            _hasCamera = true;
            return;
        }

        // Tự tìm Camera.main
        interactionCamera = Camera.main;

        if (interactionCamera != null)
        {
            _hasCamera = true;
            return;
        }

        // Vẫn không tìm thấy → log warning nhưng KHÔNG disable component
        // Idle behavior (rotate, float) vẫn chạy bình thường
        Debug.LogWarning(
            $"[ShowcaseObject] '{gameObject.name}': Không tìm thấy Camera. " +
            "Idle behavior vẫn hoạt động, nhưng tương tác chuột bị tắt. " +
            "Gán camera vào Inspector hoặc đảm bảo có Camera với tag 'MainCamera' trong scene.",
            this
        );
        _hasCamera = false;
    }

    private void Update()
    {
        // [FIX] Thử tìm lại camera nếu chưa có (camera có thể spawn sau)
        if (!_hasCamera)
        {
            if (interactionCamera == null)
            {
                interactionCamera = Camera.main;
            }

            if (interactionCamera != null)
            {
                _hasCamera = true;
            }
        }

        // Chỉ xử lý input khi có camera
        if (_hasCamera)
        {
            HandleInput();
        }

        if (isBeingInteractedWith && _hasCamera)
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

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(initialHit.point, 0.05f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(initialHit.point, initialHit.point + initialHit.normal * 0.5f);

        if (TryGetPlaneHitPoint(out Vector3 currentPointOnPlane))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentPointOnPlane, 0.04f);

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

            Handles.color = Color.white;
            Handles.Label(initialHit.point + initialHit.normal * 0.1f, "Initial Hit Point");
            Handles.Label(currentPointOnPlane, "Current Drag Point");
        }

        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * 100f);
    }
#endif
}