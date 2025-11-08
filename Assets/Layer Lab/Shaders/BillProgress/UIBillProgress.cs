using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

[ExecuteInEditMode]
[RequireComponent(typeof(Image))]
[HideMonoScript]
public class UIBillProgress : SerializedMonoBehaviour
{
    [Title("UI Component Reference")]
    [InfoBox("Component Image sẽ được tự động lấy. Hãy chắc chắn rằng bạn đã đặt 'Image Type' thành 'Filled' trong Inspector.")]
    [SerializeField, ReadOnly]
    private Image progressImage;

    private void Awake()
    {
        Initialize();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (progressImage == null)
        {
            progressImage = GetComponent<Image>();
        }
    }
#endif

    private void Initialize()
    {
        if (progressImage == null)
        {
            progressImage = GetComponent<Image>();
        }
    }

    public void SetProgress(float current, float max)
    {
        if (max <= 0)
        {
            SetNormalizedProgress(0);
            return;
        }
        SetNormalizedProgress(current / max);
    }

    public void SetNormalizedProgress(float normalizedValue)
    {
        if (progressImage == null)
        {
            Initialize();
        }

        float clampedValue = Mathf.Clamp01(normalizedValue);
        progressImage.fillAmount = clampedValue;
    }

    public float GetCurrentFill()
    {
        return progressImage != null ? progressImage.fillAmount : 0;
    }
}