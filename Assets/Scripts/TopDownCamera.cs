using UnityEngine;

namespace EchoMage.Core
{
    public sealed class TopDownCameraController : MonoBehaviour
    {
        [Header("Target Settings")]
        [Tooltip("Mục tiêu mà camera sẽ đi theo, thường là người chơi.")]
        [SerializeField] private Transform target;

        [Header("Movement Smoothing")]
        [Tooltip("Thời gian để camera bắt kịp mục tiêu. Giá trị nhỏ hơn sẽ khiến camera phản ứng nhanh hơn.")]
        [SerializeField] private float smoothTime = 0.3f;

        [Header("Mouse Look-Ahead")]
        [Tooltip("Mức độ ảnh hưởng của con trỏ chuột lên vị trí camera. 0 = không ảnh hưởng, 1 = camera nằm giữa người chơi và chuột.")]
        [Range(0f, 1f)]
        [SerializeField] private float mouseInfluence = 0.5f;

        [Tooltip("Khoảng cách tối đa mà camera có thể dịch chuyển về phía chuột, tính từ vị trí của người chơi.")]
        [SerializeField] private float maxLookAheadDistance = 4f;

        private Vector3 _currentVelocity;
        private Camera _mainCamera;
        private Plane _gamePlane;

        private void Awake()
        {
            InitializeComponents();
            InitializeGamePlane();
        }

        private void LateUpdate()
        {
            HandleCameraFollowing();
        }

        private void InitializeComponents()
        {
            _mainCamera = GetComponent<Camera>();
        }

        private void InitializeGamePlane()
        {
            _gamePlane = new Plane(Vector3.up, Vector3.zero);
        }

        private bool IsTargetInvalid()
        {
            return target == null;
        }

        private void HandleCameraFollowing()
        {
            if (IsTargetInvalid())
            {
                return;
            }

            Vector3 targetPosition = CalculateTargetCameraPosition();
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _currentVelocity,
                smoothTime
            );
        }

        private Vector3 CalculateTargetCameraPosition()
        {
            Vector3 playerPosition = target.position;
            Vector3 mouseWorldPosition = GetMousePositionOnGamePlane();

            Vector3 directionToMouse = mouseWorldPosition - playerPosition;
            directionToMouse.y = 0;

            Vector3 lookAheadOffset = directionToMouse * mouseInfluence;
            lookAheadOffset = Vector3.ClampMagnitude(lookAheadOffset, maxLookAheadDistance);

            Vector3 goalPosition = playerPosition + lookAheadOffset;
            goalPosition.y = transform.position.y; // Giữ nguyên độ cao của camera

            return goalPosition;
        }

        private Vector3 GetMousePositionOnGamePlane()
        {
            Ray cameraRay = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (_gamePlane.Raycast(cameraRay, out float distance))
            {
                return cameraRay.GetPoint(distance);
            }

            return Vector3.zero; // Fallback an toàn
        }
    }
}