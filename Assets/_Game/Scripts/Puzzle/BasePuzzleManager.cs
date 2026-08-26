using System;
using UnityEngine;

//=============================================================================
// File:        BasePuzzleManager.cs
//
// Description: 퍼즐 ID와 게임 데이터 저장소를 보관하고 퍼즐 공통 생명주기를 제공한다.
//
// History:     07/26/2026 + CSV 데이터 주입 계약 추가 -- VicZero
//
//=============================================================================
public abstract class BasePuzzleManager : MonoBehaviour
{
    [Header("Puzzle Data")]
    [Tooltip("puzzles.csv의 puzzle_id와 정확히 일치해야 하는 퍼즐 ID입니다.")]
    [SerializeField]
    private string _puzzleId;

    [Header("Puzzle Layout")]
    [Tooltip("UI 퍼즐 구성 요소 사이의 간격입니다.")]
    [SerializeField]
    private float _itemSpacing = 1.2f;

    private IGameDataRepository _gameDataRepository;

    /// <summary>퍼즐이 클리어됐을 때 한 번 호출되는 이벤트입니다.</summary>
    public event Action OnPuzzleClearedEvent;

    /// <summary>현재 퍼즐이 클리어됐는지 여부입니다.</summary>
    public bool IsClear { get; protected set; }

    /// <summary>현재 매니저가 조회할 CSV 퍼즐 ID입니다.</summary>
    protected string PuzzleId => _puzzleId;

    /// <summary>팝업을 열기 전에 월드 오브젝트가 사용할 CSV 퍼즐 ID를 지정합니다.</summary>
    public bool TrySetPuzzleId(string puzzleId)
    {
        if (string.IsNullOrWhiteSpace(puzzleId))
        {
            Debug.LogError($"[{GetType().Name}] 설정할 Puzzle ID가 비어 있습니다.");
            return false;
        }

        _puzzleId = puzzleId.Trim();
        return true;
    }

    /// <summary>퍼즐 구성 요소 사이의 UI 간격입니다.</summary>
    protected float ItemSpacing => _itemSpacing;

    /// <summary>퍼즐 초기화에 CSV 게임 데이터가 필요한지 여부입니다.</summary>
    public virtual bool RequiresGameData => true;

    /// <summary>CSV 게임 데이터 저장소 의존성을 주입합니다.</summary>
    public void Init(IGameDataRepository gameDataRepository)
    {
        _gameDataRepository = gameDataRepository;
    }

    /// <summary>주입된 CSV 데이터로 퍼즐 오브젝트를 다시 생성합니다.</summary>
    public abstract void InitializePuzzle();

    /// <summary>현재 퍼즐 상태가 정답인지 검사합니다.</summary>
    public abstract void CheckClearCondition();

    /// <summary>퍼즐 종류와 ID가 일치하는 검증된 Definition을 조회합니다.</summary>
    protected bool TryGetPuzzleDefinition(
        PuzzleType expectedPuzzleType,
        out PuzzleDefinition puzzleDefinition)
    {
        puzzleDefinition = null;

        if (_gameDataRepository == null)
        {
            Debug.LogError($"[{GetType().Name}] 게임 데이터 저장소가 주입되지 않았습니다.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_puzzleId))
        {
            Debug.LogError($"[{GetType().Name}] puzzles.csv에서 조회할 Puzzle ID가 비어 있습니다.");
            return false;
        }

        if (!_gameDataRepository.TryGetPuzzle(_puzzleId, out puzzleDefinition))
        {
            Debug.LogError($"[{GetType().Name}] puzzles.csv에 Puzzle ID가 없습니다. id={_puzzleId}");
            return false;
        }

        if (puzzleDefinition.PuzzleType == expectedPuzzleType)
        {
            return true;
        }

        Debug.LogError(
            $"[{GetType().Name}] 매니저와 CSV 퍼즐 종류가 다릅니다. "
            + $"id={_puzzleId}, expected={expectedPuzzleType}, actual={puzzleDefinition.PuzzleType}");
        puzzleDefinition = null;
        return false;
    }

    /// <summary>퍼즐 종류별 클리어 처리를 실행합니다.</summary>
    protected abstract void OnPuzzleClear();

    /// <summary>팝업과 외부 시스템에 퍼즐 클리어를 알립니다.</summary>
    protected void TriggerClearEvent()
    {
        OnPuzzleClearedEvent?.Invoke();
    }
}
