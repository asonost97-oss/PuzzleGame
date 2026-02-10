// ============================================================================
// BlockActionBehaviour.cs - 블록의 드롭(낙하) 애니메이션
// ============================================================================
// 설명: 매칭 제거 후 빈 칸으로 블록이 떨어질 때, 부드럽게 이동하는 연출을 담당합니다.
// 이유: 여러 칸 연속 낙하 시 큐로 순차 재생하고, 낙하 거리에 따라 속도를 달리 해 자연스럽게 보이게 합니다.
// ============================================================================

//#define SELF_DROP

using System.Collections;
using System.Collections.Generic;
using Ninez.Scriptable;
using UnityEngine;

namespace Ninez.Board
{
	/// <summary>Block GameObject의 이동 애니메이션 담당. Drop(낙하), 추후 Focus/Landing 등 확장 가능</summary>
	public class BlockActionBehaviour : MonoBehaviour
	{
        [SerializeField] BlockConfig m_BlockConfig;
        /// <summary>드롭 애니메이션 진행 중 여부. Stage.WaitForDropping에서 대기 판단에 사용</summary>
        public bool isMoving { get; set; }

        /// <summary>낙하 요청이 여러 번 들어올 수 있으므로 큐로 순서대로 처리</summary>
		Queue<Vector3> m_MovementQueue = new Queue<Vector3>();    // x,y = 이동량, z = 가속도 등 확장용

		/// <summary>
		/// 주어진 거리(칸 단위)만큼 아래/옆으로 이동하는 드롭 요청을 큐에 넣고, 재생 중이 아니면 코루틴 시작.
		/// 이유: Board.ArrangeBlocksAfterClean 등에서 한 블록에 여러 칸 낙하를 한 번에 요청할 수 있어 큐가 필요합니다.
		/// </summary>
		public void MoveDrop(Vector2 vtDropDistance)
		{
			m_MovementQueue.Enqueue(new Vector3(vtDropDistance.x, vtDropDistance.y, 1));

			if (!isMoving)
                StartCoroutine(DoActionMoveDrop());
		}

        /// <summary>큐에 쌓인 낙하를 하나씩 재생. 낙하 칸 수에 따라 dropSpeed로 duration 결정</summary>
        IEnumerator DoActionMoveDrop(float acc = 1.0f)
        {
            isMoving = true;

            while (m_MovementQueue.Count > 0)
            {
                Vector2 vtDestination = m_MovementQueue.Dequeue();
                int dropIndex = System.Math.Min(9, System.Math.Max(1, (int)Mathf.Abs(vtDestination.y)));
                float duration = m_BlockConfig.dropSpeed[dropIndex - 1];
                yield return CoStartDropSmooth(vtDestination, duration * acc);
            }

            isMoving = false;
            yield break;
        }

        /// <summary>한 번의 낙하를 duration 동안 선형 보간으로 이동</summary>
        IEnumerator CoStartDropSmooth(Vector2 vtDropDistance, float duration)
        {
            Vector2 to = new Vector3(transform.position.x + vtDropDistance.x, transform.position.y - vtDropDistance.y);
            yield return Action2D.MoveTo(transform, to, duration);
        }
    }
}