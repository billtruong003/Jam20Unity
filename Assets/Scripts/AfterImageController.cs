// Path: Assets/Scripts/Visual Effects/AfterImageController.cs

using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("My Indie Game/Visual Effects/After Image Controller")]
public sealed class AfterImageController : MonoBehaviour
{
    #region Enums and Nested Classes

    [Serializable]
    public enum ActivationMode
    {
        OnMovement,
        OnCommand,
        Always
    }

    [System.Serializable]
    public struct ColorSettings
    {
        public enum ColorMode
        {
            Single,
            RandomFromList,
            Gradient
        }

        [EnumToggleButtons]
        public ColorMode Mode;

        [ShowIf("Mode", ColorMode.Single)]
        [ColorUsage(true, true)]
        public Color SingleColor;

        [ShowIf("Mode", ColorMode.RandomFromList)]
        [ColorUsage(true, true)]
        public List<Color> ColorPalette;

        [ShowIf("Mode", ColorMode.Gradient)]
        [GradientUsage(true)]
        public Gradient ColorGradient;
    }

    private class PooledAfterImage
    {
        public readonly GameObject Instance;
        public readonly MeshFilter MeshFilter;
        public readonly Renderer Renderer;
        public float ActivationTimestamp;

        public PooledAfterImage(GameObject instance)
        {
            Instance = instance;
            MeshFilter = instance.GetComponent<MeshFilter>();
            Renderer = instance.GetComponent<Renderer>();
            ActivationTimestamp = 0f;
        }

        public void Activate(Vector3 position, Quaternion rotation)
        {
            Instance.transform.SetPositionAndRotation(position, rotation);
            Instance.SetActive(true);
            ActivationTimestamp = Time.time;
        }

        public void Deactivate()
        {
            if (Instance.activeInHierarchy)
            {
                if (MeshFilter.mesh != null)
                {
                    MeshFilter.mesh.Clear();
                }
                Instance.SetActive(false);
                ActivationTimestamp = 0f;
            }
        }

        public bool IsActive => Instance.activeInHierarchy;
    }

    #endregion

    [Title("After Image Controller", "Manages the creation and fading of after-images.")]
    [InfoBox("This effect is performance-intensive due to mesh baking and combining in real-time. Use judiciously and keep the pool size reasonable.")]

    [BoxGroup("Core Setup")]
    [Required("The character root whose meshes will be copied.")]
    [SerializeField] private GameObject sourceCharacterRoot;

    [BoxGroup("Core Setup")]
    [Required("A prefab with a MeshFilter, MeshRenderer, and a material using a compatible transparent shader.")]
    [AssetsOnly]
    [SerializeField] private GameObject afterImagePrefab;

    [BoxGroup("Core Setup")]
    [Tooltip("The transform to use as the origin (0,0,0) for the combined after-image mesh. Usually the character's root transform.")]
    [SerializeField] private Transform afterImageOrigin;

    [BoxGroup("Activation")]
    [Tooltip("OnMovement: Creates images when moving.\nOnCommand: Creates images only when Trigger() is called.\nAlways: Creates images continuously.")]
    [SerializeField] private ActivationMode activationMode = ActivationMode.OnMovement;

    public ActivationMode Mode
    {
        get => activationMode;
        set => activationMode = value;
    }

    [BoxGroup("Activation")]
    [ShowIf("activationMode", ActivationMode.OnMovement)]
    [Range(0f, 1f)]
    [SerializeField] private float movementThreshold = 0.01f;

    [BoxGroup("Activation")]
    [MinValue(0.001)]
    [SerializeField] private float activationDelay = 0.05f;

    [BoxGroup("Pooling")]
    [Range(1, 50)]
    [SerializeField] private int poolSize = 10;

    [BoxGroup("Appearance")]
    [MinValue(0.001)]
    [Tooltip("Total time for an after-image to fade from newest to oldest before being removed.")]
    [SerializeField] private float fadeDuration = 0.5f;

    [BoxGroup("Appearance")]
    [Tooltip("Name of the float property in the shader that controls transparency (e.g., '_Fade', '_Alpha').")]
    [SerializeField] private string fadeShaderProperty = "_Fade";

    [BoxGroup("Appearance")]
    [Tooltip("Name of the color property in the shader (e.g., '_Color', '_BaseColor').")]
    [SerializeField] private string colorShaderProperty = "_Color";

    [BoxGroup("Appearance")]
    [SerializeField] private ColorSettings colorSettings;

    private readonly List<PooledAfterImage> _pool = new List<PooledAfterImage>();
    private readonly List<PooledAfterImage> _activeImages = new List<PooledAfterImage>();
    private GameObject _poolParent;
    private SkinnedMeshRenderer[] _sourceSkinnedRenderers;
    private MeshRenderer[] _sourceMeshRenderers;
    private MeshFilter[] _sourceMeshFilters;
    private CombineInstance[] _combineInstances;
    private float _activationTimer;
    private Vector3 _previousPosition;
    private bool _isInitialized;
    private MaterialPropertyBlock _matBlock;

    public void SwitchModeMovement() => activationMode = ActivationMode.OnMovement;
    public void SwitchModeCommand() => activationMode = ActivationMode.OnCommand;
    public void SwitchModeAlways() => activationMode = ActivationMode.Always;
    public void SetRoot(GameObject root) => sourceCharacterRoot = root;
    public void SetOrigin(Transform origin) => afterImageOrigin = origin;

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        if (!_isInitialized) return;
        _previousPosition = afterImageOrigin.position;
    }

    private void OnDisable()
    {
        DeactivateAllImages();
    }

    private void OnDestroy()
    {
        CleanupPool();
    }

    private void LateUpdate()
    {
        if (!_isInitialized) return;

        HandleActivationTimer();

        if (ShouldCreateAfterImage())
        {
            StartCoroutine(CreateAfterImageAtFrameEnd());
            _activationTimer = activationDelay;
        }

        UpdateActiveImages();
    }

    [Button("Trigger Effect", ButtonSizes.Medium)]
    [ShowIf("activationMode", ActivationMode.OnCommand)]
    public void TriggerFX()
    {
        if (activationMode != ActivationMode.OnCommand || !_isInitialized) return;
        StartCoroutine(CreateAfterImageAtFrameEnd());
    }

    private void Initialize()
    {
        if (!ValidateSetup()) return;

        _matBlock = new MaterialPropertyBlock();
        InitializePool();
        CollectSourceRenderers();

        _previousPosition = afterImageOrigin.position;
        _isInitialized = true;
    }

    private bool ValidateSetup()
    {
        if (sourceCharacterRoot == null || afterImagePrefab == null || afterImageOrigin == null)
        {
            Debug.LogError($"[{nameof(AfterImageController)}] Core setup fields are not assigned. Disabling component.", this);
            enabled = false;
            return false;
        }
        return true;
    }

    private void InitializePool()
    {
        if (_poolParent != null) CleanupPool();

        _poolParent = new GameObject($"{sourceCharacterRoot.name}_AfterImagePool");
        _poolParent.transform.SetParent(this.transform);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject instance = Instantiate(afterImagePrefab, _poolParent.transform);
            instance.SetActive(false);
            _pool.Add(new PooledAfterImage(instance));
        }
    }

    private void CollectSourceRenderers()
    {
        _sourceSkinnedRenderers = sourceCharacterRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
        _sourceMeshRenderers = sourceCharacterRoot.GetComponentsInChildren<MeshRenderer>();
        _sourceMeshFilters = new MeshFilter[_sourceMeshRenderers.Length];

        for (int i = 0; i < _sourceMeshRenderers.Length; i++)
        {
            _sourceMeshFilters[i] = _sourceMeshRenderers[i].GetComponent<MeshFilter>();
        }

        _combineInstances = new CombineInstance[_sourceSkinnedRenderers.Length + _sourceMeshRenderers.Length];
    }

    private void CleanupPool()
    {
        if (_poolParent != null)
        {
            Destroy(_poolParent);
        }
        _pool.Clear();
        _activeImages.Clear();
    }

    private IEnumerator CreateAfterImageAtFrameEnd()
    {
        yield return new WaitForEndOfFrame();

        PooledAfterImage image = GetAvailableImageFromPool();
        if (image == null) yield break;

        BakeAndCombineMeshes(image.MeshFilter);
        ApplyMaterialProperties(image.Renderer);

        _activeImages.Add(image);
        image.Activate(afterImageOrigin.position, afterImageOrigin.rotation);
    }

    private void HandleActivationTimer()
    {
        if (_activationTimer > 0)
        {
            _activationTimer -= Time.deltaTime;
        }
    }

    private bool ShouldCreateAfterImage()
    {
        if (_activationTimer > 0f) return false;

        switch (activationMode)
        {
            case ActivationMode.OnMovement:
                float movementSqrMagnitude = (afterImageOrigin.position - _previousPosition).sqrMagnitude;
                _previousPosition = afterImageOrigin.position;
                return movementSqrMagnitude > (movementThreshold * movementThreshold);
            case ActivationMode.Always:
                return true;
            case ActivationMode.OnCommand:
            default:
                return false;
        }
    }

    private void BakeAndCombineMeshes(MeshFilter targetFilter)
    {
        Matrix4x4 matrix = afterImageOrigin.worldToLocalMatrix;
        var tempMeshes = new List<Mesh>(_sourceSkinnedRenderers.Length);

        for (int i = 0; i < _sourceSkinnedRenderers.Length; i++)
        {
            Mesh bakedMesh = new Mesh();
            _sourceSkinnedRenderers[i].BakeMesh(bakedMesh);
            tempMeshes.Add(bakedMesh);
            _combineInstances[i].mesh = bakedMesh;
            _combineInstances[i].transform = matrix * _sourceSkinnedRenderers[i].localToWorldMatrix;
        }

        for (int i = 0; i < _sourceMeshRenderers.Length; i++)
        {
            int combineIndex = _sourceSkinnedRenderers.Length + i;
            _combineInstances[combineIndex].mesh = _sourceMeshFilters[i].sharedMesh;
            _combineInstances[combineIndex].transform = matrix * _sourceMeshRenderers[i].transform.localToWorldMatrix;
        }

        if (targetFilter.mesh == null) targetFilter.mesh = new Mesh();
        targetFilter.mesh.Clear();
        targetFilter.mesh.CombineMeshes(_combineInstances, true, true);

        foreach (var mesh in tempMeshes)
        {
            Destroy(mesh);
        }
    }

    private void ApplyMaterialProperties(Renderer targetRenderer)
    {
        targetRenderer.GetPropertyBlock(_matBlock);

        Color finalColor = GetColorFromSettings();
        _matBlock.SetColor(colorShaderProperty, finalColor);
        _matBlock.SetFloat(fadeShaderProperty, 1f);

        targetRenderer.SetPropertyBlock(_matBlock);
    }

    private void UpdateActiveImages()
    {
        if (_activeImages.Count == 0) return;

        while (_activeImages.Count > 0 && (Time.time - _activeImages[0].ActivationTimestamp) > fadeDuration)
        {
            _activeImages[0].Deactivate();
            _activeImages.RemoveAt(0);
        }

        for (int i = 0; i < _activeImages.Count; i++)
        {
            PooledAfterImage image = _activeImages[i];

            // --- ĐÂY LÀ THAY ĐỔI DUY NHẤT ---
            // Đảo ngược giá trị fade: bóng cũ nhất (i=0) sẽ có giá trị fade cao nhất (mờ nhất).
            // Bóng mới nhất (i = count-1) sẽ có giá trị fade thấp nhất (rõ nhất).
            float fadeValue = 1.0f - ((_activeImages.Count <= 1)
                ? 1.0f
                : (float)i / (_activeImages.Count - 1));

            image.Renderer.GetPropertyBlock(_matBlock);
            _matBlock.SetFloat(fadeShaderProperty, fadeValue);
            image.Renderer.SetPropertyBlock(_matBlock);
        }
    }

    private PooledAfterImage GetAvailableImageFromPool()
    {
        foreach (var image in _pool)
        {
            if (!image.IsActive) return image;
        }

        if (_activeImages.Count > 0)
        {
            PooledAfterImage oldestImage = _activeImages[0];
            _activeImages.RemoveAt(0);
            oldestImage.Deactivate();
            return oldestImage;
        }

        Debug.LogWarning("After Image Pool is full and no active images could be recycled. Consider increasing pool size.");
        return null;
    }

    private void DeactivateAllImages()
    {
        if (_pool == null) return;
        foreach (var image in _pool)
        {
            if (image != null && image.IsActive)
            {
                image.Deactivate();
            }
        }
        _activeImages.Clear();
    }

    private Color GetColorFromSettings()
    {
        switch (colorSettings.Mode)
        {
            case ColorSettings.ColorMode.Single:
                return colorSettings.SingleColor;
            case ColorSettings.ColorMode.RandomFromList:
                if (colorSettings.ColorPalette == null || colorSettings.ColorPalette.Count == 0)
                {
                    return Color.white;
                }
                return colorSettings.ColorPalette[UnityEngine.Random.Range(0, colorSettings.ColorPalette.Count)];
            case ColorSettings.ColorMode.Gradient:
                return colorSettings.ColorGradient.Evaluate(UnityEngine.Random.value);
            default:
                return Color.white;
        }
    }
}