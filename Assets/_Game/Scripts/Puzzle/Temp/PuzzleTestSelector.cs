using UnityEngine;
using UnityEngine.InputSystem;

public class PuzzleTestSelector : MonoBehaviour
{
    [System.Serializable]
    public struct PuzzleUiEntry
    {
        public string puzzleName;
        public UiPuzzlePopup puzzlePopup;
        public string puzzleId;
        [Range(0, 3)] public int chapterIndex;
    }

    [Header("테스트할 UI 팝업 퍼즐 리스트")]
    [SerializeField] private PuzzleUiEntry[] puzzles;

    [Header("시작 시 활성화할 퍼즐 인덱스")]
    [SerializeField] private int startIndex = -1;

    [Header("클리어 시 행동 설정")]
    [SerializeField] private bool hideOnClear = false;

    private int currentIndex = -1;
    private int pendingIndex = -1;
    private bool isCurrentPuzzleClearedProcessed = false;
    private BasePuzzleManager currentActiveManager;
    private PuzzleUiRoot puzzleUiRoot;

    private void Awake()
    {
        puzzleUiRoot = GetComponent<PuzzleUiRoot>();
    }

    private void Start()
    {
        if (!Application.isEditor)
        {
            enabled = false;
            return;
        }

        for (int i = 0; i < puzzles.Length; i++)
        {
            if (puzzles[i].puzzlePopup != null)
            {
                puzzles[i].puzzlePopup.ClosePopup();
            }
        }

        if (puzzles.Length > 0 && startIndex >= 0 && startIndex < puzzles.Length)
        {
            SelectPuzzle(startIndex);
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (currentIndex >= 0
            && (puzzles[currentIndex].puzzlePopup == null
                || !puzzles[currentIndex].puzzlePopup.IsOpen))
        {
            currentIndex = -1;
            currentActiveManager = null;
        }

        if (puzzleUiRoot != null && puzzleUiRoot.IsPuzzleOpen) return;

        if (pendingIndex >= 0
            && GameRoot.TryGetInstance(out GameRoot root)
            && root.GameData != null)
        {
            int index = pendingIndex;
            pendingIndex = -1;
            SelectPuzzle(index);
        }

        // 1번 키: 파이프 퍼즐 (배열의 0번째 항목)
        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) SelectPuzzle(0);

        // 2번 키: 라이트 퍼즐 (배열의 1번째 항목)
        if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) SelectPuzzle(1);

        // --- [핵심 추가] 3번 키: 서클(원) 퍼즐 (배열의 2번째 항목) ---
        if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) SelectPuzzle(2);

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetCurrentPuzzle();
        }

        if (currentIndex != -1 && !isCurrentPuzzleClearedProcessed && currentActiveManager != null)
        {
            if (currentActiveManager.IsClear)
            {
                HandlePuzzleClearAutomation();
            }
        }
    }

    public void SelectPuzzle(int index)
    {
        if (index < 0 || index >= puzzles.Length) return;

        var targetPopup = puzzles[index].puzzlePopup;
        if (targetPopup == null || targetPopup.PuzzleManager == null)
        {
            Debug.LogError($"[PuzzleTestSelector] {index + 1}번 퍼즐 팝업 또는 매니저가 연결되지 않았습니다.", this);
            return;
        }

        if (currentIndex == index)
        {
            pendingIndex = -1;
            targetPopup.ClosePopup();
            currentIndex = -1;
            currentActiveManager = null;
            return;
        }

        if (currentIndex != -1 && puzzles[currentIndex].puzzlePopup != null)
        {
            puzzles[currentIndex].puzzlePopup.ClosePopup();
        }

        currentIndex = -1;
        currentActiveManager = null;
        BasePuzzleManager manager = targetPopup.PuzzleManager;
        if (manager.RequiresGameData
            && (!GameRoot.TryGetInstance(out GameRoot root) || root.GameData == null))
        {
            pendingIndex = index;
            Debug.Log("[PuzzleTestSelector] 게임 데이터 준비 후 퍼즐 테스트를 엽니다.", this);
            return;
        }

        pendingIndex = -1;
        if (!string.IsNullOrWhiteSpace(puzzles[index].puzzleId)
            && !manager.TrySetPuzzleId(puzzles[index].puzzleId))
        {
            return;
        }

        manager.SetChapterIndex(puzzles[index].chapterIndex);
        targetPopup.OpenPuzzlePopup();
        if (!targetPopup.IsOpen) return;

        currentIndex = index;
        isCurrentPuzzleClearedProcessed = false;
        currentActiveManager = manager;
    }

    private void ResetCurrentPuzzle()
    {
        if (currentIndex != -1 && puzzles[currentIndex].puzzlePopup != null)
        {
            isCurrentPuzzleClearedProcessed = false;
            puzzles[currentIndex].puzzlePopup.OpenPuzzlePopup();
        }
    }

    private void HandlePuzzleClearAutomation()
    {
        isCurrentPuzzleClearedProcessed = true;
        var entry = puzzles[currentIndex];

        if (hideOnClear)
        {
            if (entry.puzzlePopup != null) entry.puzzlePopup.ClosePopup();
        }
        else
        {
            if (currentActiveManager != null) currentActiveManager.enabled = false;
        }
    }
}
