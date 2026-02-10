// ============================================================================
// BlockFactory.cs - Block 인스턴스 생성
// ============================================================================
// 설명: BlockType에 맞는 Block을 만들고, BASIC일 때는 랜덤 breed를 부여합니다.
// 이유: 생성 로직을 한 곳에 모아 스테이지/난이도별로 다른 생성 정책을 넣기 쉽게 합니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BlockFactory
{
    /// <summary>
    /// 지정 타입의 Block 생성. BASIC이면 breed 0~5 중 랜덤, EMPTY면 NA.
    /// </summary>
    public static Block SpawnBlock(BlockType blockType)
    {
        Block block = new Block(blockType);

        if (blockType == BlockType.BASIC)
            block.breed = (BlockBreed)UnityEngine.Random.Range(0, 6);
        else if (blockType == BlockType.EMPTY)
            block.breed = BlockBreed.NA;

        return block;
    }
}