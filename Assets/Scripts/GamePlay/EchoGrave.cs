// Assets/Scripts/Gameplay/EchoGrave.cs

using EchoMage.AI;
using EchoMage.Core;
using EchoMage.Echoes;
using UnityEngine;

namespace EchoMage.World
{
    public class EchoGrave : MonoBehaviour
    {
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private GameObject ghostCompanionPrefab;

        private bool _canInteract = false;
        private EchoData _echoData;

        public void Initialize(EchoData data)
        {
            _echoData = data;
        }

        private void Update()
        {
            if (_canInteract && Input.GetKeyDown(KeyCode.E))
            {
                GameManager.Instance.UIManager.ShowEchoChoice(true);
                // Tạm dừng game để người chơi lựa chọn
                Time.timeScale = 0f;
            }
        }

        public void ChooseCompanion()
        {
            Time.timeScale = 1f;
            Instantiate(ghostCompanionPrefab, transform.position, Quaternion.identity)
                .GetComponent<GhostCompanion>().Initialize(_echoData);

            EchoSystem.ClearEcho();
            Destroy(gameObject);
        }

        public void ChoosePowerBoost()
        {
            Time.timeScale = 1f;
            // TODO: Implement logic for instant power boost
            Debug.Log("Power Boost Chosen! (Not implemented yet)");

            EchoSystem.ClearEcho();
            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                interactionPrompt.SetActive(true);
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