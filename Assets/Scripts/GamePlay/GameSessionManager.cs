using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EchoMage.Player;

namespace EchoMage.Core
{
    public enum DeathCause
    {
        HealthDepletion,
        Despair
    }

    /// <summary>
    /// Một entry trong bảng xếp hạng high score.
    /// </summary>
    [Serializable]
    public class HighScoreEntry
    {
        public int Score;
        public string Date;
        public string DeathCause;
        public float PlayTime; // Thời gian chơi (giây)
    }

    [Serializable]
    public class HighScoreBoard
    {
        public List<HighScoreEntry> Entries = new List<HighScoreEntry>();
    }

    public class GameSessionManager : MonoBehaviour
    {
        public static GameSessionManager Instance { get; private set; }

        public event Action<int> OnScoreUpdated;
        public event Action<int> OnHighestScoreUpdated;
        public event Action<DeathCause, int, int> OnGameOver;

        public int CurrentScore { get; private set; }
        public int HighestScore { get; private set; }

        /// <summary>
        /// Bảng xếp hạng top 10 điểm cao nhất.
        /// </summary>
        public List<HighScoreEntry> HighScoreLeaderboard => _highScoreBoard.Entries;

        private const string HighestScoreKey = "HighestScore";
        private const string HighScoreBoardKey = "HighScoreBoard";
        private const int MaxLeaderboardEntries = 10;

        private HighScoreBoard _highScoreBoard = new HighScoreBoard();
        private float _sessionStartTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadHighestScore();
            LoadHighScoreBoard();
        }

        private void Start()
        {
            StartNewGame();
        }

        public void StartNewGame()
        {
            CurrentScore = 0;
            _sessionStartTime = Time.time;
            OnScoreUpdated?.Invoke(CurrentScore);
            OnHighestScoreUpdated?.Invoke(HighestScore);
        }

        /// <summary>
        /// Reset điểm về 0 khi player chọn Continue sau khi chết.
        /// Khác StartNewGame ở chỗ không reset session timer.
        /// </summary>
        public void ResetCurrentScore()
        {
            CurrentScore = 0;
            OnScoreUpdated?.Invoke(CurrentScore);
        }

        public void AddScore(int points)
        {
            if (points <= 0) return;
            CurrentScore += points;
            OnScoreUpdated?.Invoke(CurrentScore);
        }

        public void HandlePlayerDeath(DeathCause cause)
        {
            float playTime = Time.time - _sessionStartTime;
            bool isNewHighScore = CheckAndSetHighestScore();

            // [MỚI] Thêm vào leaderboard
            AddToLeaderboard(CurrentScore, cause, playTime);

            OnGameOver?.Invoke(cause, CurrentScore, HighestScore);
        }

        /// <summary>
        /// Kiểm tra xem điểm hiện tại có đủ vào top 10 không.
        /// </summary>
        public bool IsScoreLeaderboardWorthy(int score)
        {
            if (_highScoreBoard.Entries.Count < MaxLeaderboardEntries) return true;
            return score > _highScoreBoard.Entries.Last().Score;
        }

        /// <summary>
        /// Lấy vị trí xếp hạng của một điểm số (1-indexed, 0 = không vào bảng).
        /// </summary>
        public int GetRank(int score)
        {
            for (int i = 0; i < _highScoreBoard.Entries.Count; i++)
            {
                if (score > _highScoreBoard.Entries[i].Score)
                    return i + 1;
            }

            if (_highScoreBoard.Entries.Count < MaxLeaderboardEntries)
                return _highScoreBoard.Entries.Count + 1;

            return 0;
        }

        /// <summary>
        /// Xóa toàn bộ bảng xếp hạng.
        /// </summary>
        public void ClearLeaderboard()
        {
            _highScoreBoard.Entries.Clear();
            SaveHighScoreBoard();
            HighestScore = 0;
            PlayerPrefs.SetInt(HighestScoreKey, 0);
            PlayerPrefs.Save();
            OnHighestScoreUpdated?.Invoke(0);
        }

        #region Private Methods

        private bool CheckAndSetHighestScore()
        {
            if (CurrentScore > HighestScore)
            {
                HighestScore = CurrentScore;
                SaveHighestScore();
                OnHighestScoreUpdated?.Invoke(HighestScore);
                return true;
            }
            return false;
        }

        private void AddToLeaderboard(int score, DeathCause cause, float playTime)
        {
            var newEntry = new HighScoreEntry
            {
                Score = score,
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                DeathCause = cause.ToString(),
                PlayTime = playTime
            };

            _highScoreBoard.Entries.Add(newEntry);

            // Sắp xếp giảm dần
            _highScoreBoard.Entries = _highScoreBoard.Entries
                .OrderByDescending(e => e.Score)
                .Take(MaxLeaderboardEntries)
                .ToList();

            SaveHighScoreBoard();
        }

        private void LoadHighestScore()
        {
            HighestScore = PlayerPrefs.GetInt(HighestScoreKey, 0);
            OnHighestScoreUpdated?.Invoke(HighestScore);
        }

        private void SaveHighestScore()
        {
            PlayerPrefs.SetInt(HighestScoreKey, HighestScore);
            PlayerPrefs.Save();
        }

        private void LoadHighScoreBoard()
        {
            string json = PlayerPrefs.GetString(HighScoreBoardKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                _highScoreBoard = JsonUtility.FromJson<HighScoreBoard>(json);
            }

            if (_highScoreBoard == null)
            {
                _highScoreBoard = new HighScoreBoard();
            }
        }

        private void SaveHighScoreBoard()
        {
            string json = JsonUtility.ToJson(_highScoreBoard);
            PlayerPrefs.SetString(HighScoreBoardKey, json);
            PlayerPrefs.Save();
        }

        #endregion
    }
}