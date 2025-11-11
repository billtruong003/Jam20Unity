using UnityEngine;
using TMPro;

namespace EchoMage.UI
{
    public class GamePlayUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI currentScoreText;
        [SerializeField] private TextMeshProUGUI highestScoreText;
        [SerializeField] private string scoreFormat = "Score: {0}";
        [SerializeField] private string highestScoreFormat = "Score: {0}";
        public void SetCurrentScore(int score)
        {
            currentScoreText.text = string.Format(scoreFormat, score);
        }

        public void SetHighestScore(int score)
        {
            highestScoreText.text = string.Format(highestScoreFormat, score);
        }
    }
}