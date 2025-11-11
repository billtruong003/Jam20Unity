using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    private const float fpsMeasurePeriod = 0.5f;
    private int m_FpsAccumulator = 0;
    private float m_FpsNextPeriod = 0;
    private int m_CurrentFps;
    private const string display = "{0} FPS";
    [SerializeField] private Color m_Color = Color.white;
    [SerializeField] private TextMeshProUGUI m_Text;
    [SerializeField] private int targetFPS = 120;

    private void Start()
    {
        Application.targetFrameRate = targetFPS;
        m_FpsNextPeriod = Time.realtimeSinceStartup + fpsMeasurePeriod;
        m_Text.color = m_Color;
        m_Text.text = display.Replace("{0}", "0");
    }

    private void Update()
    {
        m_FpsAccumulator++;
        if (Time.realtimeSinceStartup > m_FpsNextPeriod)
        {
            m_CurrentFps = (int)(m_FpsAccumulator / fpsMeasurePeriod);
            m_FpsAccumulator = 0;
            m_FpsNextPeriod += fpsMeasurePeriod;
            m_Text.text = string.Format(display, m_CurrentFps);
        }
    }
}