using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Renderer))]
public class BillProgress : MonoBehaviour
{
    [Title("Shader Property Name")]
    [SerializeField] private string fillAmountPropertyName = "_FillAmount";

    private Renderer objectRenderer;
    private MaterialPropertyBlock propertyBlock;
    private int fillAmountPropertyID;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        objectRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        fillAmountPropertyID = Shader.PropertyToID(fillAmountPropertyName);
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
        if (propertyBlock == null)
        {
            // Re-initialize in case of domain reload in editor
            Initialize();
        }

        float clampedValue = Mathf.Clamp01(normalizedValue);
        objectRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(fillAmountPropertyID, clampedValue);
        objectRenderer.SetPropertyBlock(propertyBlock);
    }
}