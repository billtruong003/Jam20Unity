using UnityEngine;
using TMPro;
using EchoMage.Core;

namespace EchoMage.UI
{
    public class GamePlayUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI currentScoreText;
        [SerializeField] private TextMeshProUGUI highestScoreText;
        [SerializeField] private string scoreFormat = "Score: {0}";
        [SerializeField] private string highestScoreFormat = "Best: {0}";

        private void Start()
        {
            // [FIX] Tự pull giá trị ban đầu từ GameSessionManager
            // UIManager fire event trong Awake/Start TRƯỚC khi GamePlayUI sẵn sàng
            // → UI hiện stale/0 cho đến khi có event mới
            if (GameSessionManager.Instance != null)
            {
                SetCurrentScore(GameSessionManager.Instance.CurrentScore);
                SetHighestScore(GameSessionManager.Instance.HighestScore);
            }
        }

        public void SetCurrentScore(int score)
        {
            if (currentScoreText != null)
                currentScoreText.text = string.Format(scoreFormat, score);
        }

        public void SetHighestScore(int score)
        {
            if (highestScoreText != null)
                highestScoreText.text = string.Format(highestScoreFormat, score);
        }
    }
}