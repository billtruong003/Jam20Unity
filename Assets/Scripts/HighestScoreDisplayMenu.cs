using EchoMage.Core;
using TMPro;
using UnityEngine;

public class HighestScoreDisplayMenu : MonoBehaviour
{
    private void Start()
    {
        TextMeshProUGUI textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            int highestScore = GameSessionManager.Instance.HighestScore;
            textComponent.text = $"HIGHEST SCORE: {highestScore}";
        }
        else
        {
            Debug.LogError("TextMeshProUGUI component not found on HighestScoreDisplayMenu.");
        }
    }
}
