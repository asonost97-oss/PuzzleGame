using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 매치-3 퍼즐 게임의 핵심 매니저.
/// 그리드 초기화, 블록 스왑/매치 판정/제거/낙하/빈칸 채우기, 입력 처리, 카메라 맞춤을 담당합니다.
/// </summary>
public class Match3Manager : MonoBehaviour
{
    // ========== 그리드 설정 ==========
    /// <summary> 그리드 가로 칸 수 </summary>
    [SerializeField] int width = 8;
    /// <summary> 그리드 세로 칸 수 </summary>
    [SerializeField] int height = 10;
    /// <summary> 한 칸의 월드 크기 (1 = 1유닛) </summary>
    [SerializeField] float cellSize = 1f;
    /// <summary> 그리드 원점(왼쪽 아래) 월드 좌표 </summary>
    [SerializeField] Vector3 originPosition = Vector3.zero;
    /// <summary> true면 디버그 라인·텍스트 표시 </summary>
    [SerializeField] bool debug = true;

    // ========== 블록·이펙트 설정 ==========
    /// <summary> 인스턴스화할 블록 프리팹 (Block 스크립트 + SpriteRenderer 필요) </summary>
    [SerializeField] Block blockPrefab;
    /// <summary> 블록 종류별 데이터 (스프라이트 등). 매치 판정 시 같은 BlockType이 3개 이상이면 매치 </summary>
    [SerializeField] BlockType[] blockTypes;
    /// <summary> 스왑·낙하 애니메이션 재생 시간(초) </summary>
    [SerializeField] float moveDuration = 0.5f;
    /// <summary> 블록 터질 때 재생할 이펙트 프리팹 (선택) </summary>
    [SerializeField] GameObject explosion;

    /// <summary> 입력(클릭/터치 위치·발사)을 읽는 컴포넌트 </summary>
    InputReader inputReader;
    /// <summary> 그리드 자료구조. 각 칸에는 GridObject&lt;Block&gt; (실제 Block 참조) 저장 </summary>
    GridSystem<GridObject<Block>> grid;
    /// <summary> 현재 선택된 블록의 그리드 좌표. 미선택 시 (-1, -1) </summary>
    Vector2Int selectedBlock = Vector2Int.one * -1;

    void Awake()
    {
        inputReader = GetComponent<InputReader>();
    }

    void Start()
    {
        InitializeGrid();
        inputReader.Fire += OnSelectBlock;
    }

    void OnDestroy()
    {
        inputReader.Fire -= OnSelectBlock;
    }

    /// <summary>
    /// 핵심: 클릭/터치 시 호출됨. 화면 좌표를 그리드 좌표로 바꾼 뒤,
    /// 같은 칸 클릭=선택 해제, 빈 칸/첫 선택=선택, 인접 칸=스왑 후 게임 루프 실행.
    /// </summary>
    void OnSelectBlock()
    {
        var cam = Camera.main;
        var screenPos = inputReader.Selected;
        var worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z)));
        var gridPos = grid.GetXY(worldPos);

        if (!IsValidPosition(gridPos) || IsEmptyPosition(gridPos)) return;

        if (selectedBlock == gridPos)
        {
            DeselectBlock();
        }
        else if (selectedBlock == Vector2Int.one * -1)
        {
            SelectBlock(gridPos);
        }
        else
        {
            if (!IsAdjacent(selectedBlock, gridPos))
            {
                DeselectBlock();
                return;
            }
            StartCoroutine(RunGameLoop(selectedBlock, gridPos));
        }
    }

    /// <summary>
    /// 핵심 게임 루프: 두 칸 스왑 → 매치 찾기 → 없으면 스왑 되돌리고 종료 →
    /// 있으면 매치 블록 제거 → 낙하 반복 → 빈칸 채우기 → 선택 해제.
    /// </summary>
    IEnumerator RunGameLoop(Vector2Int gridPosA, Vector2Int gridPosB)
    {
        yield return StartCoroutine(SwapBlocks(gridPosA, gridPosB));

        List<Vector2Int> matches = FindMatches();
        if (matches.Count == 0)
        {
            yield return StartCoroutine(SwapBlocks(gridPosA, gridPosB));
            DeselectBlock();
            yield break;
        }
        yield return StartCoroutine(ExplodeGems(matches));
        var fallResult = new FallResult();
        do
        {
            fallResult.anyFell = false;
            yield return StartCoroutine(MakeBlocksFall(fallResult));
        } while (fallResult.anyFell);
        yield return StartCoroutine(FillEmptySpots());
        DeselectBlock();
    }

    /// <summary> 그리드에서 빈 칸마다 새 블록을 생성하고, 짧은 대기 후 다음 칸 처리. </summary>
    IEnumerator FillEmptySpots()
    {
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (grid.GetValue(x, y) == null)
                {
                    CreateBlock(x, y);
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
    }

    /// <summary>
    /// 한 프레임 낙하: 각 열에서 빈 칸 위에 있는 블록을 한 칸씩 아래로 이동.
    /// 코루틴이라 out 불가 → FallResult에 anyFell 기록.
    /// </summary>
    IEnumerator MakeBlocksFall(FallResult result)
    {
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (grid.GetValue(x, y) == null)
                {
                    for (var i = y + 1; i < height; i++)
                    {
                        if (grid.GetValue(x, i) != null)
                        {
                            var gem = grid.GetValue(x, i).GetValue();
                            grid.SetValue(x, y, grid.GetValue(x, i));
                            grid.SetValue(x, i, null);
                            yield return StartCoroutine(MoveTo(gem.transform, grid.GetWorldPositionCenter(x, y), moveDuration));
                            result.anyFell = true;
                            break;
                        }
                    }
                }
            }
        }
    }

    /// <summary> MakeGemsFall에서 한 번이라도 낙하했는지 전달하기 위한 래퍼 (이터레이터는 out 불가) </summary>
    class FallResult
    {
        public bool anyFell;
    }

    /// <summary> 매치된 좌표 목록에 있는 블록을 그리드에서 제거하고, 이펙트 재생 후 오브젝트 파괴. </summary>
    IEnumerator ExplodeGems(List<Vector2Int> matches)
    {
        foreach (var match in matches)
        {
            var gem = grid.GetValue(match.x, match.y).GetValue();
            grid.SetValue(match.x, match.y, null);

            ExplodeVFX(match);
            yield return new WaitForSeconds(0.1f);

            Destroy(gem.gameObject, 0.1f);
        }
    }

    /// <summary> 지정 그리드 위치에 폭발 이펙트 인스턴스를 생성하고 일정 시간 후 제거. </summary>
    void ExplodeVFX(Vector2Int match)
    {
        var fx = Instantiate(explosion, transform);
        fx.transform.position = grid.GetWorldPositionCenter(match.x, match.y);
        Destroy(fx, 5f);
    }

    /// <summary>
    /// 핵심: 가로·세로로 연속 3개 이상 같은 BlockType인 칸을 찾아 좌표 집합으로 반환.
    /// </summary>
    List<Vector2Int> FindMatches()
    {
        HashSet<Vector2Int> matches = new();

        // 가로 3연속
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width - 2; x++)
            {
                var blockA = grid.GetValue(x, y);
                var blockB = grid.GetValue(x + 1, y);
                var blockC = grid.GetValue(x + 2, y);

                if (blockA == null || blockB == null || blockC == null) continue;

                if (blockA.GetValue().GetBlockType() == blockB.GetValue().GetBlockType()
                    && blockB.GetValue().GetBlockType() == blockC.GetValue().GetBlockType())
                {
                    matches.Add(new Vector2Int(x, y));
                    matches.Add(new Vector2Int(x + 1, y));
                    matches.Add(new Vector2Int(x + 2, y));
                }
            }
        }

        // 세로 3연속
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height - 2; y++)
            {
                var blockA = grid.GetValue(x, y);
                var blockB = grid.GetValue(x, y + 1);
                var blockC = grid.GetValue(x, y + 2);

                if (blockA == null || blockB == null || blockC == null) continue;

                if (blockA.GetValue().GetBlockType() == blockB.GetValue().GetBlockType()
                    && blockB.GetValue().GetBlockType() == blockC.GetValue().GetBlockType())
                {
                    matches.Add(new Vector2Int(x, y));
                    matches.Add(new Vector2Int(x, y + 1));
                    matches.Add(new Vector2Int(x, y + 2));
                }
            }
        }

        return new List<Vector2Int>(matches);
    }

    /// <summary> 두 그리드 칸의 블록을 데이터 상으로 스왑하고, 시각적으로 두 트랜스폼을 서로 위치로 이동. </summary>
    IEnumerator SwapBlocks(Vector2Int gridPosA, Vector2Int gridPosB)
    {
        var gridObjectA = grid.GetValue(gridPosA.x, gridPosA.y);
        var gridObjectB = grid.GetValue(gridPosB.x, gridPosB.y);
        var targetA = grid.GetWorldPositionCenter(gridPosB.x, gridPosB.y);
        var targetB = grid.GetWorldPositionCenter(gridPosA.x, gridPosA.y);

        grid.SetValue(gridPosA.x, gridPosA.y, gridObjectB);
        grid.SetValue(gridPosB.x, gridPosB.y, gridObjectA);

        yield return StartCoroutine(MoveTwoTransforms(
            gridObjectA.GetValue().transform, targetA,
            gridObjectB.GetValue().transform, targetB,
            moveDuration));
    }

    /// <summary> 한 트랜스폼을 선형 보간으로 목표 위치까지 이동 (코루틴). </summary>
    IEnumerator MoveTo(Transform t, Vector3 target, float duration)
    {
        var start = t.position;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
        {
            t.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        t.position = target;
    }

    /// <summary> 두 트랜스폼을 동시에 각각의 목표 위치로 선형 보간 이동. 스왑 애니메이션용. </summary>
    IEnumerator MoveTwoTransforms(Transform a, Vector3 targetA, Transform b, Vector3 targetB, float duration)
    {
        var startA = a.position;
        var startB = b.position;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = elapsed / duration;
            a.position = Vector3.Lerp(startA, targetA, t);
            b.position = Vector3.Lerp(startB, targetB, t);
            yield return null;
        }
        a.position = targetA;
        b.position = targetB;
    }

    /// <summary> 그리드 생성 및 모든 칸에 블록 배치. blockPrefab·blockTypes 미할당 시 에러 로그 후 리턴. </summary>
    void InitializeGrid()
    {
        if (blockPrefab == null)
        {
            Debug.LogError("Match3Manager: Block Prefab이 할당되지 않았습니다. Inspector에서 Block 프리팹을 넣어주세요. (Assets/3.Prefabs 폴더의 Block 프리팹)");
            return;
        }
        if (blockTypes == null || blockTypes.Length == 0)
        {
            Debug.LogError("Match3Manager: Block Types가 비어 있습니다. Inspector에서 BlockType 에셋들을 넣어주세요. (Resources/BlockConfig 또는 GemType 에셋들)");
            return;
        }

        grid = GridSystem<GridObject<Block>>.VerticalGrid(width, height, cellSize, originPosition, debug);

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                CreateBlock(x, y);
            }
        }
    }

    /// <summary> 지정 그리드 칸에 블록 하나 생성, 랜덤 BlockType 적용 후 그리드에 등록. </summary>
    void CreateBlock(int x, int y)
    {
        var block = Instantiate(blockPrefab, grid.GetWorldPositionCenter(x, y), Quaternion.identity, transform);
        block.SetType(blockTypes[Random.Range(0, blockTypes.Length)]);
        var gridObject = new GridObject<Block>(grid, x, y);
        gridObject.SetValue(block);
        grid.SetValue(x, y, gridObject);
    }

    /// <summary> 선택 해제 (selectedGem = (-1,-1)) </summary>
    void DeselectBlock() => selectedBlock = new Vector2Int(-1, -1);
    /// <summary> 해당 그리드 좌표를 선택 상태로 설정 </summary>
    void SelectBlock(Vector2Int gridPos) => selectedBlock = gridPos;

    /// <summary> 해당 그리드 칸에 블록이 없으면 true </summary>
    bool IsEmptyPosition(Vector2Int gridPosition) => grid.GetValue(gridPosition.x, gridPosition.y) == null;

    /// <summary> 그리드 범위 안의 좌표면 true </summary>
    bool IsValidPosition(Vector2 gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < width && gridPosition.y >= 0 && gridPosition.y < height;
    }

    /// <summary> 두 그리드 좌표가 상하좌우로 인접(거리 1)이면 true. 스왑 허용 조건. </summary>
    bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }
}
