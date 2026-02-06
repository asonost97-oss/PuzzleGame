using UnityEngine;

/// <summary>
/// 블록 종류별 데이터 (ScriptableObject). Create → Match3 → BlockType 으로 에셋 생성.
/// 매치3에서는 같은 BlockType이 3개 이상 연속되면 매치로 처리.
/// </summary>
[CreateAssetMenu(fileName = "BlockType", menuName = "Match3/BlockType")]
public class BlockType : ScriptableObject
{
    /// <summary> 이 종류의 블록에 표시할 스프라이트. Block.SetType() 시 SpriteRenderer에 적용됨. </summary>
    public Sprite sprite;
}
