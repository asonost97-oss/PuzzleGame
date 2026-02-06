using UnityEngine;

/// <summary>
/// 그리드 한 칸을 나타내는 래퍼. 해당 칸에 들어 있는 실제 오브젝트(Block 등) 참조를 보관.
/// GridSystem은 GridObject&lt;Block&gt; 배열로 두고, 각 칸의 Block 인스턴스를 GetValue/SetValue로 다룸.
/// </summary>
public class GridObject<T>
{
    GridSystem<GridObject<T>> grid;
    int x;
    int y;
    T block;

    /// <summary> 그리드 참조와 칸 좌표 (x,y) 로 생성. 값은 SetValue로 나중에 설정. </summary>
    public GridObject(GridSystem<GridObject<T>> grid, int x, int y)
    {
        this.grid = grid;
        this.x = x;
        this.y = y;
    }

    /// <summary> 이 칸에 들어갈 오브젝트(Block 등) 참조 설정. </summary>
    public void SetValue(T block)
    {
        this.block = block;
    }

    /// <summary> 이 칸에 들어 있는 오브젝트 참조 반환. 매치 판정·스왑·제거 시 사용. </summary>
    public T GetValue() => block;
}
