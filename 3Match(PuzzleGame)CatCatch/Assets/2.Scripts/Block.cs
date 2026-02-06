using UnityEngine;

/// <summary>
/// 그리드 위에 놓이는 블록(젬) 하나를 나타내는 컴포넌트.
/// BlockType에 따라 스프라이트가 바뀌고, 매치 판정 시 GetBlockType()으로 종류 비교.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Block : MonoBehaviour
{
    /// <summary> 이 블록의 종류(스프라이트·색 등). SetType으로 설정, 매치 시 비교에 사용. </summary>
    public BlockType type;

    /// <summary> 블록 종류 설정 및 스프라이트 적용. 새 블록 생성 시 Match3Manager에서 호출. </summary>
    public void SetType(BlockType type)
    {
        this.type = type;
        GetComponent<SpriteRenderer>().sprite = type.sprite;
    }

    /// <summary> 현재 블록 종류 반환. FindMatches에서 3연속 같은 종류인지 비교할 때 사용. </summary>
    public BlockType GetBlockType() => type;
}
