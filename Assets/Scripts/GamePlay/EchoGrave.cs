using EchoMage.AI;
using EchoMage.Core;
using EchoMage.Echoes;
using EchoMage.Player;
using UnityEngine;
using System.Collections;
using BillUtils.ObjectPooler;

namespace EchoMage.World
{
    public class EchoGrave : MonoBehaviour
    {
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private GameObject ghostCompanionPrefab;
        [SerializeField] private int powerBoostLevels = 3;
        [SerializeField] private float summonDelay = 1.5f;

        [Header("VFX")]
        [Tooltip("Pool ID particle effect khi triệu hồi ghost từ mộ.")]
        [SerializeField] private string summonVFXId = "EchoGraveSummonFX";

        private PlayerEchoData _echoData;
        private bool _canInteract = false;
        private bool _isUsedThisLife = false;
        private GhostCompanion _spawnedGhost;

        // KHÔNG dùng OnEnable/OnDisable để register vì SetActive(false) sẽ unregister
        // → GameManager mất reference → ResetForNewLife không gọi được
        private bool _isRegistered = false;

        public void Initialize(PlayerEchoData data)
        {
            _echoData = data;

            if (!_isRegistered && GameManager.Instance != null)
            {
                GameManager.Instance.RegisterGrave(this);
                _isRegistered = true;
            }
        }

        private void OnDestroy()
        {
            // Chỉ unregister khi thực sự Destroy (absorb) — không phải SetActive(false)
            if (_isRegistered && GameManager.Instance != null)
            {
                GameManager.Instance.UnregisterGrave(this);
                _isRegistered = false;
            }
        }

        /// <summary>
        /// Gọi bởi GameManager khi player hồi sinh.
        /// Bật lại grave nếu đang ẩn (đã summon ở life trước).
        /// Ghost cũ đã bị CleanupAllGhosts() hủy.
        /// </summary>
        public void ResetForNewLife()
        {
            _isUsedThisLife = false;
            _spawnedGhost = null;
            _canInteract = false;

            // Bật lại grave nếu đang ẩn
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }

        private void Update()
        {
            if (!_canInteract || _echoData == null) return;

            bool interactPressed = SettingsManager.Instance != null
                ? SettingsManager.Instance.GetActionDown("Interact")
                : Input.GetKeyDown(KeyCode.E);

            bool absorbPressed = SettingsManager.Instance != null
                ? SettingsManager.Instance.GetActionDown("AbsorbPower")
                : Input.GetKeyDown(KeyCode.Q);

            if (interactPressed && !_isUsedThisLife)
            {
                StartCoroutine(SummonCompanionRoutine());
            }
            else if (absorbPressed && !_isUsedThisLife)
            {
                ChoosePowerBoost();
            }
        }

        private IEnumerator SummonCompanionRoutine()
        {
            _isUsedThisLife = true;
            _canInteract = false;
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);

            // Spawn VFX triệu hồi
            SpawnVFX(transform.position);

            yield return new WaitForSeconds(summonDelay);

            // Spawn ghost companion
            GameObject ghostObj = Instantiate(ghostCompanionPrefab, transform.position, Quaternion.identity);
            _spawnedGhost = ghostObj.GetComponent<GhostCompanion>();
            _spawnedGhost.Initialize(_echoData);

            // Ẩn grave — vẫn nằm trong GameManager._activeGraves (vì register bằng Initialize, không OnEnable)
            // ResetForNewLife() sẽ bật lại khi player chết lần tiếp theo
            gameObject.SetActive(false);
        }

        private void ChoosePowerBoost()
        {
            PlayerStats playerStats = GameManager.Instance.PlayerStats;
            if (playerStats != null)
            {
                for (int i = 0; i < powerBoostLevels; i++)
                {
                    playerStats.Damage += _echoData.Damage * 0.1f;
                    playerStats.AttackCooldown *= 0.95f;
                }
                playerStats.ForceStatsUpdate();
            }

            SpawnVFX(transform.position);

            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);

            // Absorb = hủy vĩnh viễn — OnDestroy unregister khỏi GameManager
            Destroy(gameObject);
        }

        private void SpawnVFX(Vector3 position)
        {
            if (string.IsNullOrEmpty(summonVFXId)) return;
            try
            {
                ObjectPoolManager.Instance.Spawn(summonVFXId, position, Quaternion.identity);
            }
            catch { }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (!_isUsedThisLife && interactionPrompt != null)
                    interactionPrompt.SetActive(true);
                _canInteract = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(false);
                _canInteract = false;
            }
        }
    }
}