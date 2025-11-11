using System;
using UnityEngine;
using EchoMage.Player; // Namespace của PlayerHealth

namespace EchoMage.Core
{
    public enum DeathCause
    {
        HealthDepletion,
        Despair
    }

    public class GameSessionManager : MonoBehaviour
    {
        public static GameSessionManager Instance { get; private set; }

        public event Action<int> OnScoreUpdated;
        public event Action<int> OnHighestScoreUpdated;
        public event Action<DeathCause, int, int> OnGameOver;

        public int CurrentScore { get; private set; }
        public int HighestScore { get; private set; }

        private const string HighestScoreKey = "HighestScore";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Đảm bảo session tồn tại qua các scene

            LoadHighestScore();
        }

        private void Start()
        {
            StartNewGame();
        }

        public void StartNewGame()
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
            CheckAndSetHighestScore();
            OnGameOver?.Invoke(cause, CurrentScore, HighestScore);
        }

        private void CheckAndSetHighestScore()
        {
            if (CurrentScore > HighestScore)
            {
                HighestScore = CurrentScore;
                SaveHighestScore();
                OnHighestScoreUpdated?.Invoke(HighestScore);
            }
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
    }
}