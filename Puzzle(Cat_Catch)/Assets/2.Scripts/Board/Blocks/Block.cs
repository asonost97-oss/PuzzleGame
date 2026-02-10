// ============================================================================
// Block.cs - 퍼즐 게임의 개별 블록 데이터/로직 클래스
// ============================================================================
// 설명: 보드 위 한 칸을 차지하는 블록의 상태, 종류, 매칭 정보를 담당합니다.
// 이유: 게임 로직(데이터)과 화면 표시(Behaviour)를 분리해 유지보수와 테스트를 쉽게 합니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ninez.Quest;

namespace Ninez.Board
{
    public class Block
    {
        //---------------------------------------------------------------------
        // Members (멤버 변수)
        //---------------------------------------------------------------------
        // 현재 블록 상태: NORMAL(평상시), MATCH(3매치됨), CLEAR(제거 예정)
        public BlockStatus status;
        // 블록이 제거될 때 발동하는 효과 타입 (단일 제거, 가로줄, 세로줄 등)
        public BlockQuestType questType;
        // 매칭 형태 (3개, 4개, 5개, T/L형 등) - 퀘스트/특수 블록 판정에 사용
        public MatchType match = MatchType.NONE;
        // 매칭된 개수 (3, 4, 5 등) - 내구도 감소 등에 사용
        public short matchCount;

        //---------------------------------------------------------------------
        // Properties (속성)
        //---------------------------------------------------------------------
        BlockType m_BlockType;
        // 블록 타입: EMPTY(빈칸), BASIC(일반 블록) - 빈칸은 GameObject를 만들지 않음
        public BlockType type
        {
            get { return m_BlockType; }
            set { m_BlockType = value; }
        }

        // 렌더링되는 블록 종류(이미지/색). set 시 화면 갱신(UpdateView)을 호출해 즉시 반영
        protected BlockBreed m_Breed;   //렌더링되는 블럭 캐린터(즉, 이미지 종류)
        public BlockBreed breed
        {
            get { return m_Breed; }
            set
            {
                m_Breed = value;
                m_BlockBehaviour?.UpdateView(true);
            }
        }

        // 이 블록에 연결된 Unity GameObject의 시각/애니메이션 담당 컴포넌트
        protected BlockBehaviour m_BlockBehaviour;
        public BlockBehaviour blockBehaviour
        {
            get { return m_BlockBehaviour; }
            set
            {
                m_BlockBehaviour = value;
                m_BlockBehaviour.SetBlock(this);  // 양방향 참조로 데이터-뷰 동기화
            }
        }

        // 블록 GameObject의 Transform (위치 이동 등에 사용)
        public Transform blockObj { get { return m_BlockBehaviour?.transform; } }

        // 셔플 시 가로/세로 방향으로 같은 종류가 연속된 개수. 3매치가 나지 않도록 배치할 때 사용
        Vector2Int m_vtDuplicate;       // 블럭 젠, Shuffle시에 중복검사에 사용. stage file에서 생성시 (-1, -1)
        public int horzDuplicate
        {
            get { return m_vtDuplicate.x; }
            set { m_vtDuplicate.x = value; }
        }

        //중복 검사시 사용 
        public int vertDuplicate
        {
            get { return m_vtDuplicate.y; }
            set { m_vtDuplicate.y = value; }
        }

        // 내구도: 매칭될 때마다 감소, 0이 되면 제거 (젤리/장애물 등 확장용)
        int m_nDurability;                 //내구도
        public virtual int durability
        {
            get { return m_nDurability; }
            set { m_nDurability = value; }
        }

        // 드롭(낙하) 애니메이션 담당. isMoving으로 코루틴 대기 시 사용
        protected BlockActionBehaviour m_BlockActionBehaviour;

        // 드롭 애니메이션 진행 중인지. Postprocess에서 "모든 블록이 내려올 때까지" 대기할 때 사용
        public bool isMoving
        {
            get
            {
                return blockObj != null && m_BlockActionBehaviour.isMoving;
            }
        }

        // 낙하할 거리(칸 단위) 설정 시 BlockActionBehaviour에 드롭 애니메이션 요청
        public Vector2 dropDistance
        {
            set
            {
                m_BlockActionBehaviour?.MoveDrop(value);
            }
        }

        //---------------------------------------------------------------------
        // Constructor (생성자)
        //---------------------------------------------------------------------

        // 블록 타입에 따라 기본 상태 초기화. BASIC일 때만 화면에 표시됨
        public Block(BlockType blockType)
        {
            m_BlockType = blockType;

            status = BlockStatus.NORMAL;
            questType = BlockQuestType.CLEAR_SIMPLE;
            match = MatchType.NONE;
            m_Breed = BlockBreed.NA;

            m_nDurability = 1;
        }

        //---------------------------------------------------------------------
        // Methods (메서드)
        //---------------------------------------------------------------------

        /// <summary>
        /// 블럭을 디스플레이하는 GameObject를 생성한다. 출력이 필요한 경우에만 생성한다.
        /// 이유: EMPTY 블록은 화면에 그리지 않아 성능과 계층 구조를 단순하게 유지.
        /// </summary>
        /// <param name="blockPrefab">블록 프리팹</param>
        /// <param name="containerObj">부모 Transform(보드)</param>
        /// <returns>비어있는 블럭이면 null, 유효하면 this</returns>
        internal Block InstantiateBlockObj(GameObject blockPrefab, Transform containerObj)
        {
            if (IsValidate() == false)
                return null;

            GameObject newObj = Object.Instantiate(blockPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            newObj.transform.parent = containerObj;

            this.blockBehaviour = newObj.transform.GetComponent<BlockBehaviour>();
            m_BlockActionBehaviour = newObj.transform.GetComponent<BlockActionBehaviour>();

            return this;
        }

        /// <summary>
        /// 매칭된 블록에 대해 게임 규칙을 적용 (내구도 감소, CLEAR 처리 등).
        /// 이유: Board.Evaluate에서 모든 칸을 순회하며 호출해, 매치된 블록만 최종 CLEAR로 만듦.
        /// </summary>
        /// <returns>특수 블록 처리 필요 시 true, 일반 처리만 하면 false</returns>
        public bool DoEvaluation(BoardEnumerator boardEnumerator, int nRow, int nCol)
        {
            Debug.Assert(boardEnumerator != null, $"({nRow},{nCol})");

            if (!IsEvaluatable())
                return false;

            if (status == BlockStatus.MATCH)
            {
                // 단순 제거 또는 케이지 셀인 경우 내구도만 감소
                if (questType == BlockQuestType.CLEAR_SIMPLE || boardEnumerator.IsCageTypeCell(nRow, nCol)) //TODO cagetype cell 조건이 필요한가? 
                {
                    Debug.Assert(m_nDurability > 0, $"durability is zero : {m_nDurability}");
                    durability--;
                }
                else // 가로/세로/원형 등 특수 블록은 별도 처리
                {
                    return true;
                }

                if (m_nDurability == 0)
                {
                    status = BlockStatus.CLEAR;
                    return false;
                }
            }

            // 아직 매치가 아니거나 처리 후: 상태 초기화
            status = BlockStatus.NORMAL;
            match = MatchType.NONE;
            matchCount = 0;

            return false;
        }

        /// <summary>
        /// 이 블록을 "매칭됨" 상태로 표시하고, 매치 타입/개수를 기록.
        /// bAccumulate: 가로+세로 동시 매치처럼 여러 매치를 합칠 때 true.
        /// </summary>
        public void UpdateBlockStatusMatched(MatchType matchType, bool bAccumulate = true)
        {
            this.status = BlockStatus.MATCH;

            if (match == MatchType.NONE)
            {
                this.match = matchType;
            }
            else
            {
                this.match = bAccumulate ? match.Add(matchType) : matchType; //match + matchType
            }

            matchCount = (short)matchType;
        }

        /// <summary>지정된 월드 좌표로 블록 GameObject를 즉시 이동 (셔플/배치 시 사용)</summary>
        internal void Move(float x, float y)
        {
            blockBehaviour.transform.position = new Vector3(x, y);
        }

        /// <summary>지정 시간 동안 목표 위치로 이동하는 코루틴 실행 (스와이프 애니메이션용)</summary>
        public void MoveTo(Vector3 to, float duration)
        {
            m_BlockBehaviour.StartCoroutine(Action2D.MoveTo(blockObj, to, duration));
        }

        /// <summary>블록 제거 연출(축소+파티클) 후 GameObject 제거. Board.Evaluate에서 CLEAR 블록에 호출</summary>
        public virtual void Destroy()
        {
            Debug.Assert(blockObj != null, $"{match}");
            blockBehaviour.DoActionClear();
        }

        /// <summary>EMPTY가 아니면 true. GameObject 생성 여부 판단에 사용</summary>
        public bool IsValidate()
        {
            return type != BlockType.EMPTY;
        }

        /// <summary>셔플 전에 가로/세로 중복 카운트 초기화. 셔플 알고리즘이 다시 계산함</summary>
        public void ResetDuplicationInfo()
        {
            m_vtDuplicate.x = 0;
            m_vtDuplicate.y = 0;
        }

        /// <summary>같은 종류(breed)의 매칭 가능 블록이면 true. 3매치 판정에 사용</summary>
        public bool IsEqual(Block target)
        {
            if (IsMatchableBlock() && this.breed == target.breed)
                return true;
            return false;
        }

        /// <summary>3매치로 제거 가능한 블록인지 (EMPTY가 아니면 true)</summary>
        public bool IsMatchableBlock()
        {
            return !(type == BlockType.EMPTY);
        }

        /// <summary>기준 블록과 스와이프(교환) 가능한지. 확장 시 특수 블록별 제한 가능</summary>
        public bool IsSwipeable(Block baseBlock)
        {
            return true;
        }

        /// <summary>DoEvaluation 대상인지. 이미 CLEAR였거나 매칭 불가 블록이면 false</summary>
        public bool IsEvaluatable()
        {
            if (status == BlockStatus.CLEAR || !IsMatchableBlock())
                return false;
            return true;
        }
    }
}