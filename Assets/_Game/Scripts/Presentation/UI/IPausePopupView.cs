//=============================================================================
// File: IPausePopupView.cs
// Description: 외부 입력 라우터가 주입받는 팝업 표시 계약이다.
// History: 10/16/2026 + Created -- Codex
//=============================================================================
using UnityEngine.Events;

/// <summary>게임 기능에 의존하지 않는 일시정지 UI 진입점입니다.</summary>
public interface IPausePopupView
{
    /// <summary>전체 팝업이 열려 있는지 반환합니다.</summary>
    bool IsOpen { get; }
    /// <summary>전체 표시 상태 변경 이벤트입니다.</summary>
    UnityEvent<bool> VisibilityChanged { get; }
    /// <summary>실제 종료 처리 연결 지점입니다.</summary>
    UnityEvent ExitRequested { get; }
    /// <summary>외부 Cancel 입력을 처리합니다.</summary>
    void HandleCancel();
    /// <summary>메뉴를 엽니다.</summary>
    void OpenPauseMenu();
    /// <summary>전체 팝업을 닫습니다.</summary>
    void CloseAll();
    /// <summary>외부 HUD 버튼의 열기·닫기를 처리합니다.</summary>
    void TogglePauseMenu();
}
