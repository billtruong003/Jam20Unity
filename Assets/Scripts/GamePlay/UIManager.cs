// Assets/Scripts/GamePlay/UIManager.cs

using EchoMage.Player;
using UnityEngine;
using TMPro;

namespace EchoMage.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private UIBillProgress healthBar;
        [SerializeField] private UIBillProgress despairBar;
        [SerializeField] private GameObject gameOverScreen;
        [SerializeField] private TextMeshProUGUI gameOverReasonText;
        [SerializeField] private GameObject echoChoiceScreen;

        [Header("Dependencies")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private DespairSystem despairSystem;

        public DespairSystem GetDespairSystem => despairSystem;

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealthBar;
                playerHealth.OnDeath += HandlePlayerDeath;
            }
            if (despairSystem != null)
            {
                despairSystem.OnDespairChanged += UpdateDespairBar;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealthBar;
                playerHealth.OnDeath -= HandlePlayerDeath;
            }
            if (despairSystem != null)
            {
                despairSystem.OnDespairChanged -= UpdateDespairBar;
            }
        }

        private void Start()
        {
            // Khởi tạo trạng thái ban đầu cho UI
            gameOverScreen.SetActive(false);
            echoChoiceScreen.SetActive(false);
            // Cập nhật giá trị ban đầu nếu cần
            if (playerHealth != null) playerHealth.SendMessage("InitializeHealth", SendMessageOptions.DontRequireReceiver);
            if (despairSystem != null) despairSystem.SendMessage("InitializeDespair", SendMessageOptions.DontRequireReceiver);
        }

        private void UpdateHealthBar(float current, float max)
        {
            healthBar?.SetProgress(current, max);
        }

        private void UpdateDespairBar(float current, float max)
        {
            despairBar?.SetProgress(current, max);
        }

        private void HandlePlayerDeath()
        {
            ShowGameOverScreen("You have been defeated.");
        }

        public void ShowGameOverScreen(string reason)
        {
            gameOverScreen.SetActive(true);
            gameOverReasonText.text = reason;
        }

        public void ShowEchoChoice(bool show)
        {
            echoChoiceScreen.SetActive(show);
        }
    }
}