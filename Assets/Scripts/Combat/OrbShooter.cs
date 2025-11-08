// Assets/Scripts/Combat/OrbShooter.cs

using EchoMage.Combat;
using UnityEngine;
using Utilities.Timers;

namespace EchoMage.Player
{
    [AddComponentMenu("EchoMage/Combat/Orb Shooter")]
    public sealed class OrbShooter : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerStats playerStats;

        private const string PROJECTILE_ID = "PlayerProjectile";

        private readonly TimeGate _attackGate = new TimeGate(1f);
        private Camera _mainCamera;
        private Plane _gamePlane;
        private bool _isShooting;

        private void Awake()
        {
            InitializeDependencies();
            InitializeGamePlane();
        }

        private void OnEnable()
        {
            if (playerStats != null)
            {
                playerStats.OnStatsChanged += HandleStatsChanged;
            }
        }

        private void Start()
        {
            HandleStatsChanged();
        }

        private void OnDisable()
        {
            if (playerStats != null)
            {
                playerStats.OnStatsChanged -= HandleStatsChanged;
            }
        }

        private void Update()
        {
            if (!IsConfigurationValid()) return;

            HandleShootingState();
            ProcessShootingAction();
        }

        private void HandleStatsChanged()
        {
            _attackGate.Interval = playerStats.AttackCooldown;
        }

        private void HandleShootingState()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _isShooting = true;
                FireProjectilesImmediately();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _isShooting = false;
            }
        }

        private void ProcessShootingAction()
        {
            if (_isShooting && _attackGate.TryPass())
            {
                FireProjectiles();
            }
        }

        private void FireProjectilesImmediately()
        {
            _attackGate.Reset();
            FireProjectiles();
        }

        private void FireProjectiles()
        {
            Vector3 mousePosition = GetMousePositionOnGamePlane();
            Vector3 baseDirection = mousePosition - transform.position;

            // *** ĐIỂM THAY ĐỔI QUAN TRỌNG ***
            // "Làm phẳng" vector hướng để đảm bảo nó chỉ nằm trên mặt phẳng XZ.
            baseDirection.y = 0;
            baseDirection.Normalize();

            if (baseDirection == Vector3.zero) return; // Tránh lỗi khi hướng là không xác định

            int count = playerStats.ProjectilesPerShot;

            if (count <= 1)
            {
                SpawnSingleProjectile(transform.position, Quaternion.LookRotation(baseDirection));
                return;
            }

            float totalAngle = playerStats.ProjectileSpreadAngle;
            float angleStep = totalAngle / (count - 1);
            float startAngle = -totalAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                float currentAngle = startAngle + (i * angleStep);
                Quaternion rotationOffset = Quaternion.AngleAxis(currentAngle, Vector3.up);
                Vector3 finalDirection = rotationOffset * baseDirection;

                SpawnSingleProjectile(transform.position, Quaternion.LookRotation(finalDirection));
            }
        }

        private void SpawnSingleProjectile(Vector3 position, Quaternion rotation)
        {
            GameObject projectileObject = ObjectPoolManager.Instance.Spawn(PROJECTILE_ID, position, rotation);
            if (projectileObject != null && projectileObject.TryGetComponent<Projectile>(out var projectile))
            {
                projectile.Initialize(playerStats);
            }
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
            return playerStats != null && _mainCamera != null;
        }

        private Vector3 GetMousePositionOnGamePlane()
        {
            Ray cameraRay = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (_gamePlane.Raycast(cameraRay, out float distance))
            {
                return cameraRay.GetPoint(distance);
            }
            return transform.position + transform.forward;
        }
    }
}