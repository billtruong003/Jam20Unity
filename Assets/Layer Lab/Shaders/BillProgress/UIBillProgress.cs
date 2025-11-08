using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(Image))]
public class UIBillProgress : MonoBehaviour
{
    // Tên thuộc tính trong shader, phải khớp chính xác
    private const string FILL_AMOUNT_PROPERTY_NAME = "_FillAmount";

    private Image _targetImage;
    private Material _materialInstance;
    private int _fillAmountID;

    private void Awake()
    {
        Initialize();
    }

    // Được gọi khi script được tải hoặc một giá trị thay đổi trong Inspector
    private void OnEnable()
    {
        Initialize();
    }

#if UNITY_EDITOR
    // Đảm bảo UI cập nhật trong Editor khi thay đổi giá trị
    private void OnDidApplyAnimationProperties()
    {
        Initialize();
    }
#endif

    private void Initialize()
    {
        _targetImage = GetComponent<Image>();

        // Tạo một instance của material để không làm thay đổi asset gốc
        // Điều này rất quan trọng khi có nhiều thanh progress dùng cùng một material
        if (Application.isPlaying)
        {
            _materialInstance = Instantiate(_targetImage.material);
            _targetImage.material = _materialInstance;
        }
        else
        {
            // Trong Editor, chúng ta có thể làm việc trực tiếp với sharedMaterial
            _materialInstance = _targetImage.material;
        }

        // Cache lại ID của property để tối ưu hiệu năng
        _fillAmountID = Shader.PropertyToID(FILL_AMOUNT_PROPERTY_NAME);
    }

    /// <summary>
    /// Đặt tiến trình dựa trên giá trị hiện tại và tối đa.
    /// </summary>
    public void SetProgress(float current, float max)
    {
        float normalizedValue = (max > 0) ? current / max : 0f;
        SetNormalizedProgress(normalizedValue);
    }

    /// <summary>
    /// Đặt tiến trình dựa trên một giá trị đã được chuẩn hóa (0 đến 1).
    /// </summary>
    public void SetNormalizedProgress(float normalizedValue)
    {
        if (_materialInstance == null)
        {
            Initialize();
        }

        float clampedValue = Mathf.Clamp01(normalizedValue);
        _materialInstance.SetFloat(_fillAmountID, clampedValue);
    }
}