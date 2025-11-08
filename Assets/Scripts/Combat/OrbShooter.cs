using EchoMage.Combat;
using UnityEngine;
using Utilities.Timers;

namespace EchoMage.Player
{
    public sealed class OrbShooter : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerStats playerStats;

        private const string PROJECTILE_ID = "PlayerProjectile";
        private readonly TimeGate _attackGate = new TimeGate(1f);
        private Camera _mainCamera;
        private Plane _gamePlane;

        private void Awake()
        {
            InitializeDependencies();
            InitializeGamePlane();
        }

        private void OnEnable() => playerStats.OnStatsChanged += HandleStatsChanged;
        private void OnDisable() => playerStats.OnStatsChanged -= HandleStatsChanged;
        private void Start() => HandleStatsChanged();
        private void Update() => HandleShootingInput();

        private void HandleStatsChanged() => _attackGate.Interval = playerStats.AttackCooldown;

        private void HandleShootingInput()
        {
            if (Input.GetMouseButton(0) && _attackGate.TryPass())
            {
                FireProjectiles();
            }
        }

        private void FireProjectiles()
        {
            Vector3 mousePosition = GetMousePositionOnGamePlane();
            Vector3 firePosition = transform.position;

            Vector3 baseDirection = mousePosition - firePosition;
            baseDirection.y = 0;
            baseDirection.Normalize();

            if (baseDirection == Vector3.zero) return;

            int count = playerStats.ProjectilesPerShot;
            if (count <= 1)
            {
                SpawnSingleProjectile(firePosition, Quaternion.LookRotation(baseDirection));
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
                SpawnSingleProjectile(firePosition, Quaternion.LookRotation(finalDirection));
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

        private void InitializeDependencies() => _mainCamera = Camera.main;
        private void InitializeGamePlane() => _gamePlane = new Plane(Vector3.up, Vector3.zero);

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