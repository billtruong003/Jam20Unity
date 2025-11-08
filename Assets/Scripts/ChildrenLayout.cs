using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Đề xuất 1: Đổi tên enum để ngắn gọn và giải quyết trực tiếp lỗi trong các thuộc tính [ShowIf]
public enum LayoutShapeChild
{
    Grid,
    Circle,
    Cylinder,
    Triangle,
    Pyramid,
    Sphere,
    Spiral,
    Wave
}

[ExecuteInEditMode]
public class ChildrenLayout : MonoBehaviour
{
    private const float TAU = 2 * Mathf.PI;

    [Title("Layout Configuration", bold: true)]
    [EnumToggleButtons]
    [OnValueChanged(nameof(SetDirty))]
    public LayoutShapeChild Shape;

    [InfoBox("This component requires child objects to arrange.", InfoMessageType.Warning, "ShowIfNoChildren")]
    private bool ShowIfNoChildren => transform.childCount == 0;
    private bool HasChildren => transform.childCount > 0;

    //================================================================================
    // Shape Parameters
    //================================================================================

    #region Shape Parameters
    [BoxGroup("Shape Parameters", ShowLabel = false)]
    [ShowIf("Shape", LayoutShapeChild.Grid)]
    [OnValueChanged(nameof(SetDirty))]
    public Vector3Int GridSize = new Vector3Int(5, 5, 5);

    [BoxGroup("Shape Parameters")]
    [ShowIf("@this.Shape == LayoutShapeChild.Circle || this.Shape == LayoutShapeChild.Cylinder")]
    [MinValue(0f)]
    [OnValueChanged(nameof(SetDirty))]
    public float Radius = 5f;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShapeChild.Cylinder)]
    [MinValue(1)]
    [OnValueChanged(nameof(SetDirty))]
    public int Layers = 5;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShapeChild.Cylinder)]
    [OnValueChanged(nameof(SetDirty))]
    public float LayerHeight = 1f;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShapeChild.Triangle)]
    [MinValue(1)]
    [OnValueChanged(nameof(SetDirty))]
    public int TriangleBaseWidth = 7;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShapeChild.Pyramid)]
    [MinValue(2)]
    [OnValueChanged(nameof(SetDirty))]
    public int PyramidBaseSize = 5;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShapeChild.Pyramid)]
    [MinValue(1)]
    [OnValueChanged(nameof(SetDirty))]
    public int PyramidHeight = 5;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShapeChild.Sphere)]
    [MinValue(1f)]
    [OnValueChanged(nameof(SetDirty))]
    public float SphereRadius = 5f;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShapeChild.Sphere)]
    [Range(3, 100)]
    [OnValueChanged(nameof(SetDirty))]
    public int LatitudeDivisions = 12;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShapeChild.Sphere)]
    [Range(3, 100)]
    [OnValueChanged(nameof(SetDirty))]
    public int LongitudeDivisions = 24;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShapeChild.Spiral)]
    [OnValueChanged(nameof(SetDirty))]
    public float StartRadius = 1f;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShapeChild.Spiral)]
    [OnValueChanged(nameof(SetDirty))]
    public float EndRadius = 10f;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShapeChild.Spiral)]
    [OnValueChanged(nameof(SetDirty))]
    public float SpiralHeight = 5f;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShapeChild.Spiral)]
    [OnValueChanged(nameof(SetDirty))]
    public int Rotations = 3;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShapeChild.Wave)]
    [OnValueChanged(nameof(SetDirty))]
    public Vector2Int WaveGridSize = new Vector2Int(10, 10);

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShapeChild.Wave)]
    [OnValueChanged(nameof(SetDirty))]
    public float Amplitude = 1f;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShapeChild.Wave)]
    [OnValueChanged(nameof(SetDirty))]
    public float Frequency = 0.5f;

    [Title("Shared Parameters")]
    [BoxGroup("Shape Parameters")]
    [OnValueChanged(nameof(SetDirty))]
    public Vector3 Spacing = Vector3.one;
    #endregion

    //================================================================================
    // Actions
    //================================================================================

    [Title("Actions", Bold = true)]
    [Button(ButtonSizes.Large, Name = "Apply Layout")]
    [GUIColor(0.4f, 0.8f, 1f)]
    [EnableIf(nameof(HasChildren))]
    private void ApplyLayout()
    {
        List<Vector3> targetPositions = CalculateTargetPositions();
        ApplyPositionsToChildren(targetPositions);
    }

    //================================================================================
    // Core Logic
    //================================================================================

    private List<Vector3> CalculateTargetPositions()
    {
        int childCount = transform.childCount;
        var positions = new List<Vector3>(childCount);

        switch (Shape)
        {
            case LayoutShapeChild.Grid: FillGridPositions(positions, childCount); break;
            case LayoutShapeChild.Circle: FillCirclePositions(positions, childCount); break;
            case LayoutShapeChild.Cylinder: FillCylinderPositions(positions, childCount); break;
            case LayoutShapeChild.Triangle: FillTrianglePositions(positions); break;
            case LayoutShapeChild.Pyramid: FillPyramidPositions(positions, childCount); break;
            case LayoutShapeChild.Sphere: FillSpherePositions(positions); break;
            case LayoutShapeChild.Spiral: FillSpiralPositions(positions, childCount); break;
            case LayoutShapeChild.Wave: FillWavePositions(positions, childCount); break;
        }
        return positions;
    }

    private void ApplyPositionsToChildren(List<Vector3> positions)
    {
        int childCount = transform.childCount;
        int positionCount = positions.Count;

        for (int i = 0; i < childCount; i++)
        {
            if (i >= positionCount) break;

            Transform child = transform.GetChild(i);
#if UNITY_EDITOR
            Undo.RecordObject(child, "Apply Children Layout");
#endif
            child.localPosition = positions[i];
        }
    }

    //================================================================================
    // Position Calculation Methods
    //================================================================================

    private void FillGridPositions(List<Vector3> positions, int count)
    {
        if (GridSize.x <= 0 || GridSize.z <= 0) return;

        int totalInLayer = GridSize.x * GridSize.z;
        Vector3 offset = new Vector3(
            -(GridSize.x - 1) * Spacing.x * 0.5f,
            0,
            -(GridSize.z - 1) * Spacing.z * 0.5f
        );

        for (int i = 0; i < count; i++)
        {
            int layerIndex = i / totalInLayer;
            int indexInLayer = i % totalInLayer;
            int x = indexInLayer % GridSize.x;
            int z = indexInLayer / GridSize.x;

            Vector3 pos = new Vector3(x * Spacing.x, layerIndex * Spacing.y, z * Spacing.z) + offset;
            positions.Add(pos);
        }
    }

    private void FillCirclePositions(List<Vector3> positions, int count)
    {
        if (count == 0) return;
        float angleStep = TAU / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep;
            float x = Mathf.Cos(angle) * Radius;
            float z = Mathf.Sin(angle) * Radius;
            positions.Add(new Vector3(x, 0, z));
        }
    }

    private void FillCylinderPositions(List<Vector3> positions, int count)
    {
        if (Layers <= 0 || count == 0) return;
        int objectsPerLayer = Mathf.CeilToInt((float)count / Layers);
        if (objectsPerLayer == 0) return;

        float angleStep = TAU / objectsPerLayer;

        for (int i = 0; i < count; i++)
        {
            int layer = i / objectsPerLayer;
            int indexInLayer = i % objectsPerLayer;

            float angle = indexInLayer * angleStep;
            float x = Mathf.Cos(angle) * Radius;
            float z = Mathf.Sin(angle) * Radius;
            float y = layer * LayerHeight;
            positions.Add(new Vector3(x, y, z));
        }
    }

    private void FillTrianglePositions(List<Vector3> positions)
    {
        int childCount = transform.childCount;
        int placedCount = 0;
        for (int y = 0; y < TriangleBaseWidth && placedCount < childCount; y++)
        {
            int rowWidth = TriangleBaseWidth - y;
            for (int x = 0; x < rowWidth && placedCount < childCount; x++)
            {
                float xPos = (x - (rowWidth - 1) * 0.5f) * Spacing.x;
                float zPos = y * Spacing.z;
                positions.Add(new Vector3(xPos, 0, zPos));
                placedCount++;
            }
        }
    }

    private void FillPyramidPositions(List<Vector3> positions, int count)
    {
        int placedCount = 0;
        for (int y = 0; y < PyramidHeight && placedCount < count; y++)
        {
            float ratio = (float)(PyramidHeight - y) / PyramidHeight;
            int layerSize = Mathf.Max(1, Mathf.CeilToInt(PyramidBaseSize * ratio));
            float offset = (layerSize - 1) * 0.5f;

            for (int z = 0; z < layerSize && placedCount < count; z++)
            {
                for (int x = 0; x < layerSize && placedCount < count; x++)
                {
                    Vector3 pos = new Vector3((x - offset) * Spacing.x, y * Spacing.y, (z - offset) * Spacing.z);
                    positions.Add(pos);
                    placedCount++;
                }
            }
        }
    }

    private void FillSpherePositions(List<Vector3> positions)
    {
        int childCount = transform.childCount;
        int placedCount = 0;
        for (int i = 0; i <= LatitudeDivisions && placedCount < childCount; i++)
        {
            float latAngle = Mathf.PI * i / LatitudeDivisions;
            int currentLongDivisions = (i == 0 || i == LatitudeDivisions) ? 1 : LongitudeDivisions;
            float lonAngleStep = TAU / currentLongDivisions;

            for (int j = 0; j < currentLongDivisions && placedCount < childCount; j++)
            {
                float lonAngle = j * lonAngleStep;
                float x = SphereRadius * Mathf.Sin(latAngle) * Mathf.Cos(lonAngle);
                float y = SphereRadius * Mathf.Cos(latAngle);
                float z = SphereRadius * Mathf.Sin(latAngle) * Mathf.Sin(lonAngle);
                positions.Add(new Vector3(x, y, z));
                placedCount++;
            }
        }
    }

    private void FillSpiralPositions(List<Vector3> positions, int count)
    {
        if (count == 0) return;
        float totalAngle = Rotations * TAU;

        for (int i = 0; i < count; i++)
        {
            float progress = (count == 1) ? 0 : (float)i / (count - 1);
            float radius = Mathf.Lerp(StartRadius, EndRadius, progress);
            float angle = progress * totalAngle;
            float height = progress * SpiralHeight;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            positions.Add(new Vector3(x, height, z));
        }
    }

    private void FillWavePositions(List<Vector3> positions, int count)
    {
        if (WaveGridSize.x <= 0) return;
        Vector3 offset = new Vector3(-(WaveGridSize.x - 1) * Spacing.x * 0.5f, 0, -(WaveGridSize.y - 1) * Spacing.z * 0.5f);

        for (int i = 0; i < count; i++)
        {
            int xGrid = i % WaveGridSize.x;
            int zGrid = i / WaveGridSize.x;

            float xPos = xGrid * Spacing.x + offset.x;
            float zPos = zGrid * Spacing.z + offset.z;
            float yPos = Amplitude * Mathf.Sin(Frequency * (xPos + zPos));
            positions.Add(new Vector3(xPos, yPos, zPos));
        }
    }

    //================================================================================
    // Editor-specific Methods
    //================================================================================

    private void SetDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
        }
#endif
    }
}