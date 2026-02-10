// ============================================================================
// Board.cs - 퍼즐 보드의 핵심 로직 (셀/블록 배열, 매칭, 셔플, 드롭, 스폰)
// ============================================================================
// 설명: row x col 크기의 Cell/Block 2차원 배열을 관리하고, 3매치 판정·제거·재배치·새 블록 생성까지 담당합니다.
// 이유: Stage는 "한 판"의 진입점이고, 실제 격자·규칙은 Board에 모아 단위 테스트와 재사용을 쉽게 합니다.
// ============================================================================
    
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using IntIntKV = System.Collections.Generic.KeyValuePair<int, int>;
    
public class Board
{
    int m_nRow;
    int m_nCol;
    
    public int maxRow { get { return m_nRow; } }
    public int maxCol { get { return m_nCol; } }
    
    Cell[,] m_Cells;
    public Cell[,] cells { get { return m_Cells; } }
    
    Block[,] m_Blocks;
    public Block[,] blocks { get { return m_Blocks; } }
    
    Transform m_Container;
    GameObject m_CellPrefab;
    GameObject m_BlockPrefab;
    StageBuilder m_StageBuilder;
    
    BoardEnumerator m_Enumerator;
    
    public Board(int nRow, int nCol)
        {
            m_nRow = nRow;
            m_nCol = nCol;
            m_Cells = new Cell[nRow, nCol];
            m_Blocks = new Block[nRow, nCol];
            m_Enumerator = new BoardEnumerator(this);
        }
    
        /// <summary>
        /// 스테이지 구성을 완료: 프리팹/컨테이너 저장 → 셔플(3매치 없음) → Cell/Block GameObject 생성 및 배치.
        /// 이유: 게임 시작 시 한 번 호출되어 플레이 가능한 초기 보드를 만듦.
        /// </summary>
        internal void ComposeStage(GameObject cellPrefab, GameObject blockPrefab, Transform container, StageBuilder stageBuilder)
        {
            m_CellPrefab = cellPrefab;
            m_BlockPrefab = blockPrefab;
            m_Container = container;
            m_StageBuilder = stageBuilder;
    
            BoardShuffler shuffler = new BoardShuffler(this, true);
            shuffler.Shuffle();
    
            float initX = CalcInitX(0.5f);
            float initY = CalcInitY(0.5f);
            for (int nRow = 0; nRow < m_nRow; nRow++)
                for (int nCol = 0; nCol < m_nCol; nCol++)
                {
                    Cell cell = m_Cells[nRow, nCol]?.InstantiateCellObj(cellPrefab, container);
                    cell?.Move(initX + nCol, initY + nRow);
    
                    Block block = m_Blocks[nRow, nCol]?.InstantiateBlockObj(blockPrefab, container);
                    block?.Move(initX + nCol, initY + nRow);
                }
        }
    
        /// <summary>
        /// 현재 보드에서 3매치를 찾아 제거합니다. 매치 없으면 matchResult=false로 종료.
        /// 이유: 스와이프 후 한 번만 평가하고, 연쇄 매치는 ActionManager가 반복 호출합니다.
        /// </summary>
        public IEnumerator Evaluate(Returnable<bool> matchResult)
        {
            bool bMatchedBlockFound = UpdateAllBlocksMatchedStatus();
    
            if (bMatchedBlockFound == false)
            {
                matchResult.value = false;
                yield break;
            }
    
            for (int nRow = 0; nRow < m_nRow; nRow++)
                for (int nCol = 0; nCol < m_nCol; nCol++)
                    m_Blocks[nRow, nCol]?.DoEvaluation(m_Enumerator, nRow, nCol);
    
            List<Block> clearBlocks = new List<Block>();
            for (int nRow = 0; nRow < m_nRow; nRow++)
            {
                for (int nCol = 0; nCol < m_nCol; nCol++)
                {
                    Block block = m_Blocks[nRow, nCol];
                    if (block != null && block.status == BlockStatus.CLEAR)
                    {
                        clearBlocks.Add(block);
                        m_Blocks[nRow, nCol] = null;
                    }
                }
            }
    
            clearBlocks.ForEach((block) => block.Destroy());
            yield return new WaitForSeconds(0.2f);
    
            matchResult.value = true;
            yield break;
        }
    
        /// <summary>
        /// 보드 전체를 돌며 각 칸에서 가로/세로 3매치 여부를 검사하고, 매치된 블록에 MATCH 상태를 붙입니다.
        /// 이유: Evaluate 진입 시 "지금 보드에 3매치가 있는지"를 한 번에 계산하기 위함.
        /// </summary>
        public bool UpdateAllBlocksMatchedStatus()
        {
            List<Block> matchedBlockList = new List<Block>();
            int nCount = 0;
            for (int nRow = 0; nRow < m_nRow; nRow++)
            {
                for (int nCol = 0; nCol < m_nCol; nCol++)
                {
                    if (EvalBlocksIfMatched(nRow, nCol, matchedBlockList))
                    {
                        nCount++;
                    }
                }
            }
    
            return nCount > 0;
        }
    
        /// <summary>
        /// (nRow, nCol)을 포함한 가로/세로 3매치가 있으면 해당 블록들에 MATCH 상태를 붙입니다.
        /// matchedBlockList는 재사용 리스트로 전달해 매 프레임 GC를 줄이기 위함.
        /// </summary>
        public bool EvalBlocksIfMatched(int nRow, int nCol, List<Block> matchedBlockList)
        {
            bool bFound = false;
    
            Block baseBlock = m_Blocks[nRow, nCol];
            if (baseBlock == null)
                return false;
    
            if (baseBlock.match != MatchType.NONE || !baseBlock.IsValidate() || m_Cells[nRow, nCol].IsObstracle())
                return false;
    
            //검사하는 자신을 매칭 리스트에 우선 보관한다.
            matchedBlockList.Add(baseBlock);
    
            //1. 가로 블럭 검색
            Block block;
    
            //1.1 오른쪽 방향
            for (int i = nCol + 1; i < m_nCol; i++)
            {
                block = m_Blocks[nRow, i];
                if (!block.IsSafeEqual(baseBlock))
                    break;
    
                matchedBlockList.Add(block);
            }
    
            //1.2 왼쪽 방향
            for (int i = nCol - 1; i >= 0; i--)
            {
                block = m_Blocks[nRow, i];
                if (!block.IsSafeEqual(baseBlock))
                    break;
    
                matchedBlockList.Insert(0, block);
            }
    
            //1.3 매치된 상태인지 판단한다
            //    기준 블럭(baseBlock)을 제외하고 좌우에 2개이상이면 기준블럭 포함해서 3개이상 매치되는 경우로 판단할 수 있다
            if (matchedBlockList.Count >= 3)
            {
                SetBlockStatusMatched(matchedBlockList, true);
                bFound = true;
            }
    
            matchedBlockList.Clear();
    
            //2. 세로 블럭 검색
            matchedBlockList.Add(baseBlock);
    
            //2.1 위쪽 검색
            for (int i = nRow + 1; i < m_nRow; i++)
            {
                block = m_Blocks[i, nCol];
                if (!block.IsSafeEqual(baseBlock))
                    break;
    
                matchedBlockList.Add(block);
            }
    
            //2.2 아래쪽 검색
            for (int i = nRow - 1; i >= 0; i--)
            {
                block = m_Blocks[i, nCol];
                if (!block.IsSafeEqual(baseBlock))
                    break;
    
                matchedBlockList.Insert(0, block);
            }
    
            //2.3 매치된 상태인지 판단한다
            //    기준 블럭(baseBlock)을 제외하고 상하에 2개이상이면 기준블럭 포함해서 3개이상 매치되는 경우로 판단할 수 있다
            if (matchedBlockList.Count >= 3)
            {
                SetBlockStatusMatched(matchedBlockList, false);
                bFound = true;
            }
    
            //계산위해 리스트에 저장한 블럭 제거
            matchedBlockList.Clear();
    
            return bFound;
        }
    
        /// <summary>매치된 블록 리스트 전체에 MATCH 상태와 매치 개수(3/4/5)를 기록</summary>
        void SetBlockStatusMatched(List<Block> blockList, bool bHorz)
        {
            int nMatchCount = blockList.Count;
            blockList.ForEach(block => block.UpdateBlockStatusMatched((MatchType)nMatchCount));
        }
    
        /// <summary>
        /// 매칭 제거 후 빈 칸을 위쪽 블록이 떨어져 채우도록 보드 배열을 갱신하고, 드롭 애니메이션을 요청합니다.
        /// 열 단위로 아래부터 빈 칸을 채우며, dropDistance와 movingBlocks에 기록해 Postprocess에서 대기할 수 있게 합니다.
        /// </summary>
        public IEnumerator ArrangeBlocksAfterClean(List<IntIntKV> unfilledBlocks, List<Block> movingBlocks)
        {
            SortedList<int, int> emptyBlocks = new SortedList<int, int>();
            List<IntIntKV> emptyRemainBlocks = new List<IntIntKV>();
    
            for (int nCol = 0; nCol < m_nCol; nCol++)
            {
                emptyBlocks.Clear();
    
                //1.같은 열(col)에 빈 블럭을 수집한다.
                //  현재 col의 다른 row의 비어있는 븝럭 인덱스를 수집한다. sortedList이므로 첫번째 노드가 가장 아래쪽 블럭 위치이다
                for (int nRow = 0; nRow < m_nRow; nRow++)
                {
                    if (CanBlockBeAllocatable(nRow, nCol))
                        emptyBlocks.Add(nRow, nRow);
                }
    
                //아래쪽에 비었는 블럭이 없는 경	
                if (emptyBlocks.Count == 0)
                    continue;
    
                //2. 이동이 가능한 블럭을 비어있는 하단 위치로 이동한다.
    
                //2.1 가장 아래쪽부터 비어있는 블럭을 처리한다
                IntIntKV first = emptyBlocks.First();
    
                //2.2 비어있는 블럭 위쪽 방향으로 이동 가능한 블럭을 탐색하면서 빈 블럭을 채워나간다
                for (int nRow = first.Value + 1; nRow < m_nRow; nRow++)
                {
                    Block block = m_Blocks[nRow, nCol];
    
                    //2.2.1 이동 가능한 아이템이 아닌 경우 pass
                    if (block == null || m_Cells[nRow, nCol].type == CellType.EMPTY) //TODO EMPTY를 직접체크하지 않고 이러한 부류를 함수로 체크
                        continue;
    
                    //2.2.2 드롭을 방해하는 cell이 있는 경우 해당 cell 아래쪽은 비워둔다 (직전방향 드롭하지 않음) 
                    //      아래쪽 비어있는 블럭은 이동가능한 목록에서 제거한다
    
                    //2.2.3 이동이 필요한 블럭 발견
                    block.dropDistance = new Vector2(0, nRow - first.Value);    //GameObject 애니메이션 이동
                    movingBlocks.Add(block);
    
                    //2.2.4 빈 공간으로 이동
                    Debug.Assert(m_Cells[first.Value, nCol].IsObstracle() == false, $"{m_Cells[first.Value, nCol]}");
                    m_Blocks[first.Value, nCol] = block;        // 이동될 위치로 Board에서 저장된 위치 이동
    
                    //2.2.5 다른 곳으로 이동했음으로 현재 위치는 비워둔다
                    m_Blocks[nRow, nCol] = null;
    
                    //2.2.6 비어있는 블럭 리스트에서 사용된 첫번째 노드(first)를 삭제한다
                    emptyBlocks.RemoveAt(0);
    
                    //2.2.7 현재 위치의 블럭이 다른 위치로 이동했으므로 현재 위치가 비어있게 된다.
                    //그러므로 비어있는 블럭을 보관하는 emptyBolocks에 추가한다
                    emptyBlocks.Add(nRow, nRow);
    
                    //2.2.8 다음(Next) 비어었는 블럭을 처리하도록 기준을 변경한다
                    first = emptyBlocks.First();
                    nRow = first.Value; //Note : 빈곳 바로 위부터 처리하도록 위치 조정, for 문에서 nRow++ 하기 때문에 +1을 하지 않는다
                }
            }
    
            yield return null;
    
            //드롭으로 채워지지 않는 블럭이 있는 경우(왼쪽 아래 순으로 들어있음)
            if (emptyRemainBlocks.Count > 0)
            {
                unfilledBlocks.AddRange(emptyRemainBlocks);
            }
    
            yield break;
        }
    
        /// <summary>
        /// ArrangeBlocksAfterClean 이후에도 남은 빈 칸(맨 위 등)에 새 블록을 생성하고 위에서 드롭시킵니다.
        /// 이유: 한 번에 한 열씩 처리해, 각 열의 맨 위 빈 칸들을 새 블록으로 채웁니다.
        /// </summary>
        public IEnumerator SpawnBlocksAfterClean(List<Block> movingBlocks)
        {
            for (int nCol = 0; nCol < m_nCol; nCol++)
            {
                for (int nRow = 0; nRow < m_nRow; nRow++)
                {
                    //비어있는 블럭이 있는 경우, 상위 열은 모두 비어있거나, 장애물로 인해서 남아있음.
                    if (m_Blocks[nRow, nCol] == null)
                    {
                        int nTopRow = nRow;
                        int nSpawnBaseY = 0;
    
                        for (int y = nTopRow; y < m_nRow; y++)
                        {
                            if (m_Blocks[y, nCol] != null || !CanBlockBeAllocatable(y, nCol))
                                continue;
    
                            Block block = SpawnBlockWithDrop(y, nCol, nSpawnBaseY, nCol);
                            if (block != null)
                                movingBlocks.Add(block);
    
                            nSpawnBaseY++;
                        }
    
                        break;
                    }
                }
            }
    
            yield return null;
        }
    
        /// <summary>
        /// 새 블록을 보드 위쪽(화면 밖)에 생성한 뒤 (nRow, nCol)까지 드롭시키고 보드 배열에 등록.
        /// nSpawnedRow/nSpawnedCol은 생성 시점의 논리 좌표로, 드롭 거리 계산에 사용됩니다.
        /// </summary>
        Block SpawnBlockWithDrop(int nRow, int nCol, int nSpawnedRow, int nSpawnedCol)
        {
            float fInitX = CalcInitX(Constants.BLOCK_ORG);
            float fInitY = CalcInitY(Constants.BLOCK_ORG) + m_nRow;
    
            Block block = m_StageBuilder.SpawnBlock().InstantiateBlockObj(m_BlockPrefab, m_Container);
            if (block != null)
            {
                m_Blocks[nRow, nCol] = block;
                block.Move(fInitX + (float)nSpawnedCol, fInitY + (float)(nSpawnedRow));
                block.dropDistance = new Vector2(nSpawnedCol - nCol, m_nRow + (nSpawnedRow - nRow));
            }
    
            return block;
        }
    
    
        /// <summary>보드 왼쪽 끝의 X 좌표(월드). offset으로 셀 중심 정렬(0.5) 등 사용</summary>
        public float CalcInitX(float offset = 0)
        {
            return -m_nCol / 2.0f + offset;
        }
    
        /// <summary>보드 아래쪽 끝의 Y 좌표(월드). Unity 2D는 y 아래가 작은 값</summary>
        public float CalcInitY(float offset = 0)
        {
            return -m_nRow / 2.0f + offset;
        }
    
        /// <summary>해당 칸의 셀이 블록 이동 가능 타입이면 true. 셔플/스와이프 가능 여부에 사용</summary>
        public bool CanShuffle(int nRow, int nCol, bool bLoading)
        {
            if (!m_Cells[nRow, nCol].type.IsBlockMovableType())
                return false;
    
            return true;
        }
    
        /// <summary>블록 breed를 notAllowedBreed를 제외한 랜덤으로 변경. 셔플 시 3매치가 안 나도록 할 때 사용</summary>
        public void ChangeBlock(Block block, BlockBreed notAllowedBreed)
        {
            BlockBreed genBreed;
    
            while (true)
            {
                genBreed = (BlockBreed)UnityEngine.Random.Range(0, 6); //TODO 스테이지파일에서 Spawn 정책을 이용해야함
    
                if (notAllowedBreed == genBreed)
                    continue;
    
                break;
            }
    
            block.breed = genBreed;
        }
    
        /// <summary>해당 칸이 스와이프(블록 교환) 가능한 셀인지</summary>
        public bool IsSwipeable(int nRow, int nCol)
        {
            return m_Cells[nRow, nCol].type.IsBlockMovableType();
        }
    
        /// <summary>블록이 (nRow, nCol)에 배치 가능한지: 셀이 배치 가능 타입이고, 그 칸이 비어 있어야 함</summary>
        bool CanBlockBeAllocatable(int nRow, int nCol)
        {
            if (!m_Cells[nRow, nCol].type.IsBlockAllocatableType())
                return false;
    
            return m_Blocks[nRow, nCol] == null;
        }
    }