using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BillUtils
{
    [ExecuteAlways]
    [AddComponentMenu("Rendering/Billboard")]
    public class Billboard : MonoBehaviour
    {
        public enum BillboardConstraint
        {
            Free,
            YAxis,
            XAxis,
            ZAxis
        }

        [Tooltip("Các tùy chọn ràng buộc trục xoay.")]
        [SerializeField] private BillboardConstraint constraint = BillboardConstraint.Free;

        [Tooltip("Điều chỉnh xoay cục bộ sau khi tấm plane đã hướng về camera.")]
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

        private Transform mainCameraTransform;

        void Start()
        {
            InitializeCameraTransform();
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.update += EditorUpdate;
            }
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.update -= EditorUpdate;
            }
#endif
        }

        void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            UpdateRotation(mainCameraTransform);
        }

#if UNITY_EDITOR
        private void EditorUpdate()
        {
            if (this == null || transform == null)
            {
                EditorApplication.update -= EditorUpdate;
                return;
            }

            Transform sceneCameraTransform = GetSceneViewCameraTransform();
            UpdateRotation(sceneCameraTransform);
        }
#endif

        private void InitializeCameraTransform()
        {
            if (Application.isPlaying)
            {
                if (Camera.main != null)
                {
                    mainCameraTransform = Camera.main.transform;
                }
                else
                {
                    Debug.LogError("Billboard script requires a main camera in the scene. Please tag your camera with 'MainCamera'.", this);
                }
            }
        }

        private void UpdateRotation(Transform targetCamera)
        {
            if (targetCamera == null)
            {
                return;
            }

            Vector3 directionToCamera = targetCamera.position - transform.position;
            Quaternion targetRotation;

            switch (constraint)
            {
                case BillboardConstraint.YAxis:
                    directionToCamera.y = 0;
                    break;
                case BillboardConstraint.XAxis:
                    directionToCamera.x = 0;
                    break;
                case BillboardConstraint.ZAxis:
                    directionToCamera.z = 0;
                    break;
                case BillboardConstraint.Free:
                default:
                    // No constraints, do nothing to the direction vector
                    break;
            }

            if (directionToCamera.sqrMagnitude < 0.0001f)
            {
                return;
            }

            targetRotation = Quaternion.LookRotation(directionToCamera.normalized);
            transform.rotation = targetRotation * Quaternion.Euler(rotationOffset);
        }

#if UNITY_EDITOR
        private Transform GetSceneViewCameraTransform()
        {
            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            {
                return SceneView.lastActiveSceneView.camera.transform;
            }
            return null;
        }
#endif
    }
}