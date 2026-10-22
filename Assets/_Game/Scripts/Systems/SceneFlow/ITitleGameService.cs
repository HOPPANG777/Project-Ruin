//=============================================================================
// File: ITitleGameService.cs
// Description: 타이틀 UI와 게임 진입 처리의 경계이다.
// History: 10/16/2026 + Created -- Codex
//=============================================================================
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>타이틀 표시부가 구현체 대신 주입받는 게임 진입 계약입니다.</summary>
public interface ITitleGameService
{
    /// <summary>현재 불러올 수 있는 저장이 있는지 반환합니다.</summary>
    bool HasSavedData { get; }
    /// <summary>진행 상태를 준비하고 시작 챕터로 진입합니다.</summary>
    UniTask<bool> TryEnterAsync(bool newGame, CancellationToken cancellationToken);
    /// <summary>애플리케이션 종료를 요청합니다.</summary>
    void Quit();
}
