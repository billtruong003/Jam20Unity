using EchoMage.AI;
using EchoMage.Core;
using EchoMage.Echoes;
using EchoMage.Player;
using UnityEngine;
using System.Collections;
using EchoMage.Loot;
using EchoMage.Loot.Effects;

namespace EchoMage.World
{
    public class EchoGrave : MonoBehaviour
    {
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private GameObject ghostCompanionPrefab;
        [SerializeField] private int powerBoostLevels = 3;
        [SerializeField] private float summonDelay = 1.5f;

        private PlayerEchoData _echoData;
        private bool _canInteract = false;
        private bool _isUsedThisLife = false;

        // [MỚI] Track ghost đã triệu hồi từ mộ này
        // Khi player chết → GameManager.CleanupAllGhosts() hủy ghost
        // → ResetForNewLife() cho phép triệu hồi lại
        private GhostCompanion _spawnedGhost;

        private void OnEnable() => GameManager.Instance.RegisterGrave(this);
        private void OnDisable() => GameManager.Instance.UnregisterGrave(this);

        public void Initialize(PlayerEchoData data)
        {
            _echoData = data;
        }

        /// <summary>
        /// Gọi bởi GameManager khi player hồi sinh.
        /// Reset trạng thái cho phép tương tác lại trong life mới.
        /// Ghost cũ đã bị GameManager.CleanupAllGhosts() hủy trước đó.
        /// </summary>
        public void ResetForNewLife()
        {
            _isUsedThisLife = false;
            _spawnedGhost = null; // Ghost cũ đã bị hủy bởi GameManager

            // Nếu player đang đứng gần mộ → hiện lại prompt
            // (không cần vì OnTriggerEnter sẽ gọi lại khi player mới spawn)
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
            interactionPrompt.SetActive(false);
            _canInteract = false;

            yield return new WaitForSeconds(summonDelay);

            // [FIX] Track ghost đã spawn
            GameObject ghostObj = Instantiate(ghostCompanionPrefab, transform.position, Quaternion.identity);
            _spawnedGhost = ghostObj.GetComponent<GhostCompanion>();
            _spawnedGhost.Initialize(_echoData);

            // Mộ vẫn tồn tại — nhưng không cho tương tác nữa trong life này
        }

        StatUpgradeData statUpgradeData = new();
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
            FinalizeChoice();
        }

        private void FinalizeChoice()
        {
            interactionPrompt.SetActive(false);
            Destroy(gameObject); // Hủy mộ khi hấp thụ
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (!_isUsedThisLife)
                {
                    interactionPrompt.SetActive(true);
                }
                _canInteract = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                interactionPrompt.SetActive(false);
                _canInteract = false;
            }
        }
    }
}