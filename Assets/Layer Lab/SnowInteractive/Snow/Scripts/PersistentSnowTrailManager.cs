using UnityEngine;
using System.Collections.Generic;

public struct SnowTrailDrawCommand
{
    public Vector3 worldPosition;
    public float radius;
    public float strength;
}

[DisallowMultipleComponent]
public class PersistentSnowTrailManager : MonoBehaviour
{
    public static PersistentSnowTrailManager Instance { get; private set; }

    [Header("Trail Area Configuration")]
    [SerializeField] private Vector3 trailAreaCenter = Vector3.zero;
    [SerializeField] private float trailAreaSize = 50f;

    [Header("Core Settings")]
    [SerializeField] private RenderTexture snowCanvasRT;
    [SerializeField] private Material trailBlitMaterial;
    [SerializeField] private Material snowEffectsMaterial;

    [Header("Effects Control")]
    [SerializeField, Range(0f, 0.5f)] private float healingRate = 0.01f;
    [SerializeField, Range(0.01f, 1.0f)] private float smoothingLerpFactor = 0.1f;

    [Header("Compute Shader Optimization")]
    [SerializeField] private bool useComputeShader = false;
    [SerializeField] private ComputeShader snowTrailComputeShader;

    private RenderTexture tempRT1;
    private RenderTexture tempRT2;

    private readonly List<SnowTrailDrawCommand> drawQueue = new List<SnowTrailDrawCommand>(32);
    private readonly List<Vector4> drawCommandData = new List<Vector4>(32);
    private ComputeBuffer drawCommandComputeBuffer;
    private int snowTrailComputeKernel;

    private static readonly int GlobalEffectRT_ID = Shader.PropertyToID("_GlobalEffectRT");
    private static readonly int InteractorPosition_ID = Shader.PropertyToID("_InteractorPosition");
    private static readonly int OrthographicCamSize_ID = Shader.PropertyToID("_OrthographicCamSize");
    private static readonly int BrushCenterUV_ID = Shader.PropertyToID("_BrushCenterUV");
    private static readonly int BrushRadius_ID = Shader.PropertyToID("_BrushRadius");
    private static readonly int BrushStrength_ID = Shader.PropertyToID("_BrushStrength");
    private static readonly int PreviousFrameRT_ID = Shader.PropertyToID("_PreviousFrameRT");
    private static readonly int HealingRate_ID = Shader.PropertyToID("_HealingRate");
    private static readonly int DeltaTime_ID = Shader.PropertyToID("_DeltaTime");
    private static readonly int SmoothingLerpFactor_ID = Shader.PropertyToID("_SmoothingLerpFactor");
    private static readonly int DrawCommandsBuffer_ID = Shader.PropertyToID("_DrawCommands");
    private static readonly int DrawCommandCount_ID = Shader.PropertyToID("_DrawCommandCount");
    private static readonly int TrailAreaParams_ID = Shader.PropertyToID("_TrailAreaParams");
    private static readonly int TextureSize_ID = Shader.PropertyToID("_TextureSize");
    private static readonly int Result_ID = Shader.PropertyToID("Result");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        InitializeResources();
    }

    private void LateUpdate()
    {
        if (drawQueue.Count > 0)
        {
            if (useComputeShader)
            {
                ExecuteComputeShaderPass();
            }
            else
            {
                ExecuteBlitPass();
            }
        }
        else
        {
            ApplyEffectsOnly();
        }

        drawQueue.Clear();
    }

    private void OnDisable()
    {
        ReleaseResources();
    }

    public void QueueDrawCommand(Vector3 worldPosition, float radius, float strength)
    {
        drawQueue.Add(new SnowTrailDrawCommand
        {
            worldPosition = worldPosition,
            radius = radius,
            strength = strength
        });
    }

    private void InitializeResources()
    {
        if (snowCanvasRT == null) return;

        RenderTextureDescriptor descriptor = snowCanvasRT.descriptor;

        // tempRT2 never needs random write access.
        tempRT2 = new RenderTexture(descriptor);

        // tempRT1 only needs random write if we use the compute shader.
        if (useComputeShader && snowTrailComputeShader != null)
        {
            descriptor.enableRandomWrite = true;
            tempRT1 = new RenderTexture(descriptor);

            snowTrailComputeKernel = snowTrailComputeShader.FindKernel("ProcessSnowTrails");
            drawCommandComputeBuffer = new ComputeBuffer(32, sizeof(float) * 4, ComputeBufferType.Structured);
            snowTrailComputeShader.SetVector(TextureSize_ID, new Vector2(snowCanvasRT.width, snowCanvasRT.height));
        }
        else
        {
            tempRT1 = new RenderTexture(descriptor);
        }

        Graphics.Blit(Texture2D.blackTexture, snowCanvasRT);
        Graphics.Blit(Texture2D.blackTexture, tempRT1);
        Graphics.Blit(Texture2D.blackTexture, tempRT2);

        Shader.SetGlobalTexture(GlobalEffectRT_ID, snowCanvasRT);
        Shader.SetGlobalVector(InteractorPosition_ID, new Vector4(trailAreaCenter.x, trailAreaCenter.y, trailAreaCenter.z, 0));
        Shader.SetGlobalFloat(OrthographicCamSize_ID, trailAreaSize / 2f);
    }

    private void ReleaseResources()
    {
        tempRT1?.Release();
        tempRT2?.Release();
        drawCommandComputeBuffer?.Release();

        tempRT1 = null;
        tempRT2 = null;
        drawCommandComputeBuffer = null;

        Shader.SetGlobalTexture(GlobalEffectRT_ID, null);
    }

    private void ApplyEffectsOnly()
    {
        Graphics.Blit(snowCanvasRT, tempRT1);
        ApplyHealing(tempRT1, tempRT2);
        ApplySmoothing(tempRT2, snowCanvasRT);
    }

    private void ExecuteBlitPass()
    {
        if (snowEffectsMaterial == null || trailBlitMaterial == null) return;

        Graphics.Blit(snowCanvasRT, tempRT1);
        ApplyHealing(tempRT1, tempRT2);
        DrawTrailsWithBlit(tempRT2, tempRT1);
        ApplySmoothing(tempRT1, snowCanvasRT);
    }

    private void ApplyHealing(RenderTexture source, RenderTexture destination)
    {
        snowEffectsMaterial.SetFloat(HealingRate_ID, healingRate);
        snowEffectsMaterial.SetFloat(DeltaTime_ID, Time.deltaTime);
        Graphics.Blit(source, destination, snowEffectsMaterial, 0);
    }

    private void DrawTrailsWithBlit(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination);
        foreach (var command in drawQueue)
        {
            DrawCommandToTexture(command, destination);
        }
    }

    private void ApplySmoothing(RenderTexture source, RenderTexture destination)
    {
        snowEffectsMaterial.SetTexture(PreviousFrameRT_ID, snowCanvasRT);
        snowEffectsMaterial.SetFloat(SmoothingLerpFactor_ID, smoothingLerpFactor);
        Graphics.Blit(source, destination, snowEffectsMaterial, 1);
    }

    private void DrawCommandToTexture(SnowTrailDrawCommand command, RenderTexture target)
    {
        float uvX = (command.worldPosition.x - trailAreaCenter.x) / trailAreaSize + 0.5f;
        float uvY = (command.worldPosition.z - trailAreaCenter.z) / trailAreaSize + 0.5f;
        float uvRadius = command.radius / trailAreaSize;

        trailBlitMaterial.SetVector(BrushCenterUV_ID, new Vector4(uvX, uvY, 0, 0));
        trailBlitMaterial.SetFloat(BrushRadius_ID, uvRadius);
        trailBlitMaterial.SetFloat(BrushStrength_ID, command.strength);

        Graphics.Blit(target, tempRT2);
        Graphics.Blit(tempRT2, target, trailBlitMaterial);
    }

    private void ExecuteComputeShaderPass()
    {
        if (snowTrailComputeShader == null || drawCommandComputeBuffer == null) return;

        PrepareComputeBuffer();

        snowTrailComputeShader.SetTexture(snowTrailComputeKernel, PreviousFrameRT_ID, snowCanvasRT);
        snowTrailComputeShader.SetTexture(snowTrailComputeKernel, Result_ID, tempRT1);
        snowTrailComputeShader.SetFloat(HealingRate_ID, healingRate);
        snowTrailComputeShader.SetFloat(DeltaTime_ID, Time.deltaTime);
        snowTrailComputeShader.SetBuffer(snowTrailComputeKernel, DrawCommandsBuffer_ID, drawCommandComputeBuffer);
        snowTrailComputeShader.SetInt(DrawCommandCount_ID, drawQueue.Count);
        snowTrailComputeShader.SetVector(TrailAreaParams_ID, new Vector4(trailAreaCenter.x, trailAreaCenter.z, trailAreaSize, 0));

        int threadGroupsX = Mathf.CeilToInt(tempRT1.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(tempRT1.height / 8.0f);
        snowTrailComputeShader.Dispatch(snowTrailComputeKernel, threadGroupsX, threadGroupsY, 1);

        ApplySmoothing(tempRT1, snowCanvasRT);
    }

    private void PrepareComputeBuffer()
    {
        drawCommandData.Clear();
        for (int i = 0; i < drawQueue.Count; i++)
        {
            var command = drawQueue[i];
            drawCommandData.Add(new Vector4(command.worldPosition.x, command.worldPosition.z, command.radius, command.strength));
        }
        drawCommandComputeBuffer.SetData(drawCommandData);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f);
        Gizmos.DrawWireCube(trailAreaCenter, new Vector3(trailAreaSize, 1f, trailAreaSize));
    }
}