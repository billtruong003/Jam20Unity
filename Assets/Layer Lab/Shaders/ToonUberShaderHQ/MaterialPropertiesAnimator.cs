using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public class MaterialPropertyAnimator : MonoBehaviour
{
    public enum Waveform
    {
        Sine,
        Triangle,
        Sawtooth
    }

    [Header("Animation Target")]
    [Tooltip("The exact name of the shader property to animate. E.g., _DissolveThreshold")]
    [SerializeField] private string propertyToAnimate = "_DissolveThreshold";

    [Header("Animation Parameters")]
    [Tooltip("The shape of the animation wave over time.")]
    [SerializeField] private Waveform waveform = Waveform.Sine;

    [Tooltip("The speed of the animation cycle.")]
    [SerializeField] private float animationSpeed = 0.5f;

    [Tooltip("The minimum value the property will animate to.")]
    [SerializeField] private float minimumValue = -0.2f;

    [Tooltip("The maximum value the property will animate to.")]
    [SerializeField] private float maximumValue = 1.2f;

    [Header("Advanced Settings")]
    [Tooltip("An offset added to the time, useful for variation across multiple objects.")]
    [SerializeField] private float timeOffset = 0.0f;

    [Tooltip("Applies the property block to all material slots on the renderer.")]
    [SerializeField] private bool affectAllMaterialSlots = true;


    private Renderer objectRenderer;
    private MaterialPropertyBlock propertyBlock;
    private int propertyID;

    private void Awake()
    {
        Initialize();
    }

    private void OnValidate()
    {
        InitializeShaderProperty();
    }

    private void Update()
    {
        UpdateAnimation();
    }

    private void Initialize()
    {
        objectRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        InitializeShaderProperty();
    }

    private void InitializeShaderProperty()
    {
        if (!string.IsNullOrEmpty(propertyToAnimate))
        {
            propertyID = Shader.PropertyToID(propertyToAnimate);
        }
    }

    private void UpdateAnimation()
    {
        if (objectRenderer == null || propertyBlock == null) return;

        float animatedValue = CalculateAnimatedValue();
        ApplyValueToMaterial(animatedValue);
    }

    private float CalculateAnimatedValue()
    {
        float time = (Time.time * animationSpeed) + timeOffset;
        float normalizedValue = GetNormalizedWaveformValue(time);
        return Mathf.Lerp(minimumValue, maximumValue, normalizedValue);
    }

    private float GetNormalizedWaveformValue(float time)
    {
        switch (waveform)
        {
            case Waveform.Sine:
                return (Mathf.Sin(time) + 1.0f) * 0.5f;
            case Waveform.Triangle:
                return Mathf.PingPong(time, 1.0f);
            case Waveform.Sawtooth:
                return time % 1.0f;
            default:
                return 0.5f;
        }
    }

    private void ApplyValueToMaterial(float value)
    {
        objectRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(propertyID, value);

        if (affectAllMaterialSlots)
        {
            for (int i = 0; i < objectRenderer.sharedMaterials.Length; i++)
            {
                objectRenderer.SetPropertyBlock(propertyBlock, i);
            }
        }
        else
        {
            objectRenderer.SetPropertyBlock(propertyBlock, 0);
        }
    }
}