// Assets/Scripts/GamePlay/GameManager.cs (Cập nhật)

using EchoMage.Echoes;
using EchoMage.Player;
using EchoMage.UI;
using EchoMage.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoMage.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Core References")]
        public Transform PlayerTransform;
        public PlayerStats PlayerStats;
        public UIManager UIManager;


        [Header("Gameplay State")]
        public float WorldThreatLevel = 1f;

        [Header("Echo System")]
        [SerializeField] private GameObject echoGravePrefab;

        private bool _isGameOver = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SpawnEchoGraveIfNeeded();
        }

        public void EndGame(string reason)
        {
            if (_isGameOver) return;
            _isGameOver = true;

            EchoSystem.SaveEcho(PlayerStats, PlayerTransform.position);
            UIManager.ShowGameOverScreen(reason);

            // Tự động restart sau 5 giây
            Invoke(nameof(RestartGame), 5f);
        }

        private void SpawnEchoGraveIfNeeded()
        {
            EchoData data = EchoSystem.LoadEcho();
            if (data != null)
            {
                GameObject graveInstance = Instantiate(echoGravePrefab, data.DeathPosition, Quaternion.identity);
                graveInstance.GetComponent<EchoGrave>().Initialize(data);
            }
        }

        private void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}