using UnityEngine;

namespace EchoMage.Player
{
    public sealed class OrbController : MonoBehaviour
    {
        [Header("Core Configuration")]
        [Tooltip("Đối tượng Transform của người chơi mà quả cầu sẽ xoay quanh.")]
        [SerializeField] private Transform playerTransform;

        [Header("Orbital Mechanics")]
        [Tooltip("Khoảng cách cố định từ quả cầu đến tâm (người chơi).")]
        [SerializeField] private float orbitalRadius = 2.5f;

        [Tooltip("Tốc độ quả cầu di chuyển đến vị trí mục tiêu trên quỹ đạo. Giá trị cao hơn sẽ làm cho nó phản ứng nhanh hơn.")]
        [SerializeField] private float followSpeed = 15f;

        private Camera _mainCamera;
        private Plane _gamePlane;

        private void Awake()
        {
            InitializeDependencies();
            InitializeGamePlane();
        }

        private void LateUpdate()
        {
            if (!IsConfigurationValid())
            {
                return;
            }

            HandleOrbPositioning();
        }

        private void InitializeDependencies()
        {
            _mainCamera = Camera.main;
        }

        private void InitializeGamePlane()
        {
            _gamePlane = new Plane(Vector3.up, Vector3.zero);
        }

        private bool IsConfigurationValid()
        {
            return playerTransform != null && _mainCamera != null;
        }

        private void HandleOrbPositioning()
        {
            Vector3 targetPosition = CalculateTargetPosition();

            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                followSpeed * Time.deltaTime
            );
        }

        private Vector3 CalculateTargetPosition()
        {
            Vector3 playerPosition = playerTransform.position;
            Vector3 mouseWorldPosition = GetMousePositionOnGamePlane();

            Vector3 directionToMouse = (mouseWorldPosition - playerPosition).normalized;
            directionToMouse.y = 0; // Đảm bảo quả cầu luôn di chuyển trên mặt phẳng XZ

            return playerPosition + directionToMouse * orbitalRadius;
        }

        private Vector3 GetMousePositionOnGamePlane()
        {
            Ray cameraRay = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (_gamePlane.Raycast(cameraRay, out float distance))
            {
                return cameraRay.GetPoint(distance);
            }

            // Fallback an toàn, trả về vị trí phía trước người chơi nếu không tìm thấy chuột
            return playerTransform.position + playerTransform.forward;
        }
    }
}