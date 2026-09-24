//=============================================================================
// File:        CoreBootstrapModule.cs
//
// Description: 게임 데이터와 Progress 저장 복구 참조 및 초기화를 소유한다.
//
// History:     09/23/2026 + GameRoot Core 책임 분리 -- Codex
//
//=============================================================================

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>게임 데이터 준비와 Progress 저장 복구 단계를 소유하는 직렬화 모듈입니다.</summary>
[Serializable]
internal sealed class CoreBootstrapModule
{
    [Tooltip("CSV TextAsset을 읽어 게임 데이터 저장소를 생성하는 컨트롤러입니다.")]
    [SerializeField] private GameDataController _gameDataController;

    [Tooltip("게임 진행 상태와 플래그를 보관하는 컨트롤러입니다.")]
    [SerializeField] private ProgressController _progressController;

    /// <summary>CSV에서 초기화된 읽기 전용 게임 데이터 저장소입니다.</summary>
    internal IGameDataRepository GameData => _gameDataController != null
        ? _gameDataController.Repository
        : null;

    /// <summary>CSV 문장 Key를 현재 언어 문자열로 변환하는 계약입니다.</summary>
    internal ILocalizationService Localization => _gameDataController;

    /// <summary>게임 진행 상태 시스템의 외부 공개 계약입니다.</summary>
    internal IProgressService Progress => _progressController;

    /// <summary>CSV 게임 데이터를 가장 먼저 검증하고 저장소를 생성합니다.</summary>
    /// <returns>게임 데이터 저장소를 생성했으면 true입니다.</returns>
    internal bool TryInitializeGameData()
    {
        if (_gameDataController == null)
        {
            Debug.LogError("[CoreBootstrapModule] GameDataController가 연결되지 않았습니다.");
            return false;
        }

        if (_gameDataController.TryInit(out GameDataCsvLoadError loadError))
        {
            return true;
        }

        string errorMessage = loadError != null
            ? loadError.ToString()
            : "알 수 없는 CSV 게임 데이터 초기화 오류입니다.";
        Debug.LogError($"[CoreBootstrapModule] 게임 데이터 초기화에 실패했습니다. {errorMessage}");
        return false;
    }

    /// <summary>ES3 Progress 저장 데이터를 불러옵니다.</summary>
    /// <param name="cancellationToken">GameRoot 생명주기와 연결된 취소 토큰입니다.</param>
    /// <returns>Progress 참조가 있고 저장 복구에 성공하면 true입니다.</returns>
    internal async UniTask<bool> LoadProgressAsync(CancellationToken cancellationToken)
    {
        if (_progressController == null)
        {
            Debug.LogError("[CoreBootstrapModule] ProgressController가 연결되지 않았습니다.");
            return false;
        }

        return await _progressController.LoadAsync(cancellationToken);
    }
}
