using EchoMage.AI;
using EchoMage.Core;
using EchoMage.Echoes;
using EchoMage.Player;
using UnityEngine;

namespace EchoMage.World
{
    public class EchoGrave : MonoBehaviour
    {
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private GameObject ghostCompanionPrefab;
        [SerializeField] private int powerBoostLevels = 3;
        [SerializeField] private LayerMask playerLayer;
        private bool _canInteract = false;
        [SerializeField] private PlayerEchoData _echoData;

        public void Initialize(PlayerEchoData data)
        {
            _echoData = data;
        }

        private void Update()
        {
            if (!_canInteract || _echoData == null) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                ChooseCompanion();
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                ChoosePowerBoost();
            }
        }

        private void ChooseCompanion()
        {
            Instantiate(ghostCompanionPrefab, transform.position, Quaternion.identity)
                .GetComponent<GhostCompanion>().Initialize(_echoData);
            FinalizeChoice();
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
                playerStats.ApplyUpgrade();
            }
            FinalizeChoice();
        }

        private void FinalizeChoice()
        {
            interactionPrompt.SetActive(false);
            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((playerLayer.value & (1 << other.gameObject.layer)) > 0)
            {
                interactionPrompt.SetActive(true);
                _canInteract = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if ((playerLayer.value & (1 << other.gameObject.layer)) > 0)
            {
                interactionPrompt.SetActive(false);
                _canInteract = false;
            }
        }
    }
}