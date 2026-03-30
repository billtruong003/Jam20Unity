using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace EchoMage.Core
{
    /// <summary>
    /// UI hiển thị bảng xếp hạng High Score.
    /// Gắn vào một panel trong Game Over screen hoặc Main Menu.
    /// </summary>
    public class HighScoreUI : MonoBehaviour
    {
        [Header("Leaderboard")]
        [SerializeField] private Transform leaderboardListParent;
        [SerializeField] private GameObject leaderboardEntryPrefab;

        [Header("Current Game Info")]
        [SerializeField] private TextMeshProUGUI currentRankText;

        [Header("Buttons")]
        [SerializeField] private UnityEngine.UI.Button clearButton;

        private void OnEnable()
        {
            RefreshLeaderboard();
        }

        private void Start()
        {
            if (clearButton != null)
            {
                clearButton.onClick.AddListener(OnClearLeaderboard);
            }
        }

        /// <summary>
        /// Cập nhật bảng xếp hạng.
        /// </summary>
        public void RefreshLeaderboard()
        {
            if (leaderboardListParent == null || leaderboardEntryPrefab == null) return;
            if (GameSessionManager.Instance == null) return;

            // Xóa entries cũ
            foreach (Transform child in leaderboardListParent)
            {
                Destroy(child.gameObject);
            }

            List<HighScoreEntry> entries = GameSessionManager.Instance.HighScoreLeaderboard;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderboardListParent);

                // Giả sử prefab có các TextMeshProUGUI children
                TextMeshProUGUI rankLabel = entryObj.transform.Find("RankLabel")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI scoreLabel = entryObj.transform.Find("ScoreLabel")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI dateLabel = entryObj.transform.Find("DateLabel")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI timeLabel = entryObj.transform.Find("TimeLabel")?.GetComponent<TextMeshProUGUI>();

                if (rankLabel != null) rankLabel.text = $"#{i + 1}";
                if (scoreLabel != null) scoreLabel.text = entry.Score.ToString("N0");
                if (dateLabel != null) dateLabel.text = entry.Date;
                if (timeLabel != null)
                {
                    int minutes = Mathf.FloorToInt(entry.PlayTime / 60f);
                    int seconds = Mathf.FloorToInt(entry.PlayTime % 60f);
                    timeLabel.text = $"{minutes}m {seconds}s";
                }

                // Highlight current game score
                bool isCurrentGame = (entry.Score == GameSessionManager.Instance.CurrentScore
                    && i == GameSessionManager.Instance.GetRank(entry.Score) - 1);

                if (isCurrentGame)
                {
                    // Có thể thay đổi màu hoặc thêm highlight
                    var bg = entryObj.GetComponent<UnityEngine.UI.Image>();
                    if (bg != null) bg.color = new Color(1f, 0.85f, 0.3f, 0.3f); // Highlight vàng
                }
            }

            // Hiển thị rank hiện tại
            if (currentRankText != null)
            {
                int rank = GameSessionManager.Instance.GetRank(GameSessionManager.Instance.CurrentScore);
                currentRankText.text = rank > 0
                    ? $"Your Rank: #{rank}"
                    : "Not in Top 10";
            }
        }

        private void OnClearLeaderboard()
        {
            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.ClearLeaderboard();
                RefreshLeaderboard();
            }
        }
    }
}