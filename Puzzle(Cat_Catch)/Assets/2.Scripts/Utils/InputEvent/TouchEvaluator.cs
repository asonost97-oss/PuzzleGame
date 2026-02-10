// ============================================================================
// TouchEvaluator.cs - 드래그 방향(Swipe) 열거형 및 방향·각도 계산
// ============================================================================
// 설명: 드래그 시작/끝 좌표로 각도를 구하고, 45도 단위로 RIGHT/UP/LEFT/DOWN을 반환합니다. SwipeDirMethod는 스와이프 시 "인접 칸 row/col 오프셋"을 줍니다.
// 이유: 짧은 드래그는 NA로 무시하고, 방향만 정수로 넘기면 Stage에서 인덱스 계산이 단순해집니다.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Swipe
{
    NA = -1,
    RIGHT = 0,
    UP = 1,
    LEFT = 2,
    DOWN = 3
}

/// <summary>Swipe 방향에 따른 row/col 델타. Stage에서 스와이프 대상 칸 계산 시 사용</summary>
public static class SwipeDirMethod
{
    public static int GetTargetRow(this Swipe swipeDir)
    {
        switch (swipeDir)
        {
            case Swipe.DOWN: return -1;
            case Swipe.UP: return 1;
            default: return 0;
        }
    }

    public static int GetTargetCol(this Swipe swipeDir)
    {
        switch (swipeDir)
        {
            case Swipe.LEFT: return -1;
            case Swipe.RIGHT: return 1;
            default: return 0;
        }
    }
}

public static class TouchEvaluator
{
    /// <summary>드래그 각도를 90도 구간으로 나누어 Swipe 반환. 너무 짧으면 NA. 각도: RIGHT 0~45, UP 45~135 등</summary>
    public static Swipe EvalSwipeDir(Vector2 vtStart, Vector2 vtEnd)
    {
        float angle = EvalDragAngle(vtStart, vtEnd);
        if (angle < 0) return Swipe.NA;

        int swipe = (((int)angle + 45) % 360) / 90;
        switch (swipe)
        {
            case 0: return Swipe.RIGHT;
            case 1: return Swipe.UP;
            case 2: return Swipe.LEFT;
            case 3: return Swipe.DOWN;
        }
        return Swipe.NA;
    }

    /// <summary>시작→끝 벡터의 각도(도). 드래그 거리가 너무 짧으면 -1 반환</summary>
    static float EvalDragAngle(Vector2 vtStart, Vector2 vtEnd)
    {
        Vector2 dragDirection = vtEnd - vtStart;
        if (dragDirection.magnitude <= 0.2f)
            return -1f;

        float aimAngle = Mathf.Atan2(dragDirection.y, dragDirection.x);
        if (aimAngle < 0f)
        {
            aimAngle = Mathf.PI * 2 + aimAngle;
        }

        return aimAngle * Mathf.Rad2Deg;
    }
}
