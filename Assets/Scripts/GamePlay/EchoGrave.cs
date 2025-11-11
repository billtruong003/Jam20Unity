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

        private void OnEnable() => GameManager.Instance.RegisterGrave(this);
        private void OnDisable() => GameManager.Instance.UnregisterGrave(this);

        public void Initialize(PlayerEchoData data)
        {
            _echoData = data;
        }

        public void ResetForNewLife()
        {
            _isUsedThisLife = false;
        }

        private void Update()
        {
            if (!_canInteract || _echoData == null) return;

            if (Input.GetKeyDown(KeyCode.E) && !_isUsedThisLife)
            {
                StartCoroutine(SummonCompanionRoutine());
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                ChoosePowerBoost();
            }
        }

        private IEnumerator SummonCompanionRoutine()
        {
            _isUsedThisLife = true;
            interactionPrompt.SetActive(false);
            _canInteract = false;

            // Thêm hiệu ứng triệu hồi tại đây nếu muốn

            yield return new WaitForSeconds(summonDelay);

            Instantiate(ghostCompanionPrefab, transform.position, Quaternion.identity)
                .GetComponent<GhostCompanion>().Initialize(_echoData);

            // Sau khi triệu hồi, không hủy mộ nhưng không cho tương tác lại
        }

        StatUpgradeData statUpgradeData = new();
        private void ChoosePowerBoost()
        {
            PlayerStats playerStats = GameManager.Instance.PlayerStats;
            if (playerStats != null)
            {
                for (int i = 0; i < powerBoostLevels; i++)
                {
                    // Thay đổi chỉ số trực tiếp
                    playerStats.Damage += _echoData.Damage * 0.1f;
                    playerStats.AttackCooldown *= 0.95f;
                }

                // Gọi phương thức mới, rõ ràng hơn để cập nhật các hệ thống khác (UI, OrbShooter,...)
                playerStats.ForceStatsUpdate();
            }
            FinalizeChoice();
        }

        private void FinalizeChoice()
        {
            interactionPrompt.SetActive(false);
            Destroy(gameObject); // Chỉ hủy mộ khi hấp thụ
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