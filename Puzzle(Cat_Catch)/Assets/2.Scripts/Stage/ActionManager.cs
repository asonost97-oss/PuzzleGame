// ============================================================================
// ActionManager.cs - 플레이어 스와이프 액션 및 보드 평가 연쇄 처리
// ============================================================================
// 설명: 스와이프 요청을 받아 Stage에 스와이프·평가·후처리를 요청하고, 매치가 없으면 스와이프를 되돌립니다. 한 번에 하나의 액션만 실행되도록 m_bRunning으로 보호합니다.
// 이유: 코루틴은 MonoBehaviour에서만 시작할 수 있으므로, Container에서 MonoBehaviour를 얻어 코루틴을 실행하는 역할을 이 클래스가 담당합니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionManager
{
    Transform m_Container;
    Stage m_Stage;
    MonoBehaviour m_MonoBehaviour;

    bool m_bRunning;

    public ActionManager(Transform container, Stage stage)
    {
        m_Container = container;
        m_Stage = stage;
        m_MonoBehaviour = container.gameObject.GetComponent<MonoBehaviour>();
    }

    public Coroutine StartCoroutine(IEnumerator routine)
    {
        return m_MonoBehaviour.StartCoroutine(routine);
    }

    public void DoSwipeAction(int nRow, int nCol, Swipe swipeDir)
    {
        Debug.Assert(nRow >= 0 && nRow < m_Stage.maxRow && nCol >= 0 && nCol < m_Stage.maxCol);

        if (m_Stage.IsValideSwipe(nRow, nCol, swipeDir))
            StartCoroutine(CoDoSwipeAction(nRow, nCol, swipeDir));
    }

    IEnumerator CoDoSwipeAction(int nRow, int nCol, Swipe swipeDir)
    {
        if (!m_bRunning)
        {
            m_bRunning = true;

            Returnable<bool> bSwipedBlock = new Returnable<bool>(false);
            yield return m_Stage.CoDoSwipeAction(nRow, nCol, swipeDir, bSwipedBlock);

            if (bSwipedBlock.value)
            {
                Returnable<bool> bMatchBlock = new Returnable<bool>(false);
                yield return EvaluateBoard(bMatchBlock);

                if (!bMatchBlock.value)
                    yield return m_Stage.CoDoSwipeAction(nRow, nCol, swipeDir, bSwipedBlock);
            }

            m_bRunning = false;
        }
        yield break;
    }

    IEnumerator EvaluateBoard(Returnable<bool> matchResult)
    {
        while (true)
        {
            Returnable<bool> bBlockMatched = new Returnable<bool>(false);
            yield return StartCoroutine(m_Stage.Evaluate(bBlockMatched));

            if (bBlockMatched.value)
            {
                matchResult.value = true;
                yield return StartCoroutine(m_Stage.PostprocessAfterEvaluate());
            }
            else
                break;
        }

        yield break;
    }
}