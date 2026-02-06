using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 2D 그리드 자료구조. 각 칸에 값 T를 저장하고, 그리드 좌표 ↔ 월드 좌표 변환을 제공.
/// Vertical: X-Y 평면(2D 게임), Horizontal: X-Z 평면(3D 바닥) 지원.
/// </summary>
public class GridSystem<T>
{
    readonly int width;
    readonly int height;
    readonly float cellSize;
    readonly Vector3 origin;
    readonly T[,] gridArray;
    readonly CoordinateConverter coordinateConverter;

    /// <summary> 칸 값이 바뀔 때 (x, y, 새 값) 로 전달되는 이벤트 </summary>
    public event Action<int, int, T> OnValueChangeEvent;

    /// <summary> X-Y 평면용 그리드 생성 (2D 매치3 등) </summary>
    public static GridSystem<T> VerticalGrid(int width, int height, float cellSize, Vector3 origin, bool debug = false)
    {
        return new GridSystem<T>(width, height, cellSize, origin, new VerticalConverter(), debug);
    }

    /// <summary> X-Z 평면용 그리드 생성 (3D 바닥 그리드 등) </summary>
    public static GridSystem<T> HorizontalGrid(int width, int height, float cellSize, Vector3 origin, bool debug = false)
    {
        return new GridSystem<T>(width, height, cellSize, origin, new HorizontalConverter(), debug);
    }

    public GridSystem(int width, int height, float cellSize, Vector3 origin, CoordinateConverter coordinateConverter, bool debug)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.origin = origin;
        this.coordinateConverter = coordinateConverter ?? new VerticalConverter();

        gridArray = new T[width, height];

        if (debug)
        {
            DrawDebugLines();
        }
    }

    /// <summary> 월드 좌표에 해당하는 칸에 값 설정 </summary>
    public void SetValue(Vector3 worldPosition, T value)
    {
        Vector2Int pos = coordinateConverter.WorldToGrid(worldPosition, cellSize, origin);
        SetValue(pos.x, pos.y, value);
    }

    /// <summary> (x,y) 칸에 값 설정. 범위 밖이면 무시. </summary>
    public void SetValue(int x, int y, T value)
    {
        if (IsValid(x, y))
        {
            gridArray[x, y] = value;
            OnValueChangeEvent?.Invoke(x, y, value);
        }
    }

    /// <summary> 월드 좌표에 해당하는 칸의 값 반환 </summary>
    public T GetValue(Vector3 worldPosition)
    {
        Vector2Int pos = GetXY(worldPosition);
        return GetValue(pos.x, pos.y);
    }

    /// <summary> (x,y) 칸의 값 반환. 범위 밖이면 default(T). </summary>
    public T GetValue(int x, int y)
    {
        return IsValid(x, y) ? gridArray[x, y] : default;
    }

    bool IsValid(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

    /// <summary> 핵심: 월드 좌표를 그리드 칸 인덱스 (x,y)로 변환. 클릭 위치 → 선택 칸 계산에 사용. </summary>
    public Vector2Int GetXY(Vector3 worldPosition) => coordinateConverter.WorldToGrid(worldPosition, cellSize, origin);

    /// <summary> 핵심: 그리드 칸 (x,y)의 중심 월드 좌표. 블록 배치·이동 목표 위치에 사용. </summary>
    public Vector3 GetWorldPositionCenter(int x, int y) => coordinateConverter.GridToWorldCenter(x, y, cellSize, origin);

    Vector3 GetWorldPosition(int x, int y) => coordinateConverter.GridToWorld(x, y, cellSize, origin);

    /// <summary> 디버그 시 그리드 라인과 칸 좌표 텍스트를 씬에 그림. </summary>
    void DrawDebugLines()
    {
        const float duration = 100f;
        var parent = new GameObject("Debugging");

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CreateWorldText(parent, x + "," + y, GetWorldPositionCenter(x, y), coordinateConverter.Forward);
                Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1), Color.white, duration);
                Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y), Color.white, duration);
            }
        }

        Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, duration);
        Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, duration);
    }

    TextMeshPro CreateWorldText(GameObject parent, string text, Vector3 position, Vector3 dir,
        int fontSize = 2, Color color = default, TextAlignmentOptions textAnchor = TextAlignmentOptions.Center, int sortingOrder = 0)
    {
        GameObject gameObject = new GameObject("DebugText_" + text, typeof(TextMeshPro));
        gameObject.transform.SetParent(parent.transform);
        gameObject.transform.position = position;
        gameObject.transform.forward = dir;

        TextMeshPro textMeshPro = gameObject.GetComponent<TextMeshPro>();
        textMeshPro.text = text;
        textMeshPro.fontSize = fontSize;
        textMeshPro.color = color == default ? Color.white : color;
        textMeshPro.alignment = textAnchor;
        var renderer = gameObject.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sortingOrder = sortingOrder;

        return textMeshPro;
    }

    /// <summary> 그리드 ↔ 월드 좌표 변환 규칙 (Vertical: X-Y, Horizontal: X-Z) </summary>
    public abstract class CoordinateConverter
    {
        public abstract Vector3 GridToWorld(int x, int y, float cellSize, Vector3 origin);
        public abstract Vector3 GridToWorldCenter(int x, int y, float cellSize, Vector3 origin);
        public abstract Vector2Int WorldToGrid(Vector3 worldPosition, float cellSize, Vector3 origin);
        public abstract Vector3 Forward { get; }
    }

    /// <summary> X-Y 평면용 변환. 셀 (x,y) → 월드 (x*cellSize, y*cellSize) 기준. </summary>
    public class VerticalConverter : CoordinateConverter
    {
        public override Vector3 GridToWorld(int x, int y, float cellSize, Vector3 origin)
        {
            return new Vector3(x, y, 0) * cellSize + origin;
        }

        public override Vector3 GridToWorldCenter(int x, int y, float cellSize, Vector3 origin)
        {
            return new Vector3(x * cellSize + cellSize * 0.5f, y * cellSize + cellSize * 0.5f, 0) + origin;
        }

        public override Vector2Int WorldToGrid(Vector3 worldPosition, float cellSize, Vector3 origin)
        {
            Vector3 gridPosition = (worldPosition - origin) / cellSize;
            var x = Mathf.FloorToInt(gridPosition.x);
            var y = Mathf.FloorToInt(gridPosition.y);
            return new Vector2Int(x, y);
        }

        public override Vector3 Forward => Vector3.forward;
    }

    /// <summary> X-Z 평면용 변환. 셀 (x,y) → 월드 (x*cellSize, 0, y*cellSize) 기준. </summary>
    public class HorizontalConverter : CoordinateConverter
    {
        public override Vector3 GridToWorldCenter(int x, int y, float cellSize, Vector3 origin)
        {
            return new Vector3(x * cellSize + cellSize * 0.5f, 0, y * cellSize + cellSize * 0.5f) + origin;
        }

        public override Vector3 GridToWorld(int x, int y, float cellSize, Vector3 origin)
        {
            return new Vector3(x, 0, y) * cellSize + origin;
        }

        public override Vector2Int WorldToGrid(Vector3 worldPosition, float cellSize, Vector3 origin)
        {
            Vector3 gridPosition = (worldPosition - origin) / cellSize;
            var x = Mathf.FloorToInt(gridPosition.x);
            var y = Mathf.FloorToInt(gridPosition.z);
            return new Vector2Int(x, y);
        }

        public override Vector3 Forward => -Vector3.up;
    }
}
