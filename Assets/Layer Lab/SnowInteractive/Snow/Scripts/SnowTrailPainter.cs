// Assets/Scenes/SnowInteractive/Snow/Shader/SnowTrailPainter.cs

using UnityEngine;

[DisallowMultipleComponent]
public class SnowTrailPainter : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float trailRadius = 0.5f;

    [SerializeField, Range(0, 1)]
    private float trailStrength = 0.8f;

    [Header("Performance")]
    [Tooltip("Time in seconds between each trail update. Higher values improve performance.")]
    [SerializeField, Min(0.01f)]
    private float updateInterval = 0.033f; // Default to ~30 FPS updates

    private Transform objectTransform;
    private PersistentSnowTrailManager trailManager;
    private float timeSinceLastUpdate;

    private void Awake()
    {
        objectTransform = transform;
    }

    private void Start()
    {
        trailManager = PersistentSnowTrailManager.Instance;
    }

    private void LateUpdate()
    {
        if (trailManager == null)
        {
            return;
        }

        timeSinceLastUpdate += Time.deltaTime;

        if (timeSinceLastUpdate >= updateInterval)
        {
            trailManager.QueueDrawCommand(
                objectTransform.position,
                trailRadius,
                trailStrength
            );
            timeSinceLastUpdate = 0f;
        }
    }
}