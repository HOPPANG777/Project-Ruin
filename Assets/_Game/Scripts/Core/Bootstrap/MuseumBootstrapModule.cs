//=============================================================================
// File:        MuseumBootstrapModule.cs
//
// Description: 박물관 Controller 참조와 의존 단계별 초기화를 소유한다.
//
// History:     09/23/2026 + GameRoot Museum 책임 분리 -- Codex
//
//=============================================================================

using System;
using UnityEngine;

/// <summary>벽화, 술병, 유물 전시, 최종 보상 방 조립을 소유합니다.</summary>
[Serializable]
internal sealed class MuseumBootstrapModule
{
    [Tooltip("박물관 벽화 개봉 상태와 제단 이동 정의를 관리하는 컨트롤러입니다.")]
    [SerializeField] private MuseumController _museumController;

    [Tooltip("챕터별 술병 진행과 투입 보상을 관리하는 컨트롤러입니다.")]
    [SerializeField] private MuseumBottleController _museumBottleController;

    [Tooltip("유물 전시대별 배치와 박물관 마일스톤을 관리하는 컨트롤러입니다.")]
    [SerializeField] private MuseumRelicDisplayController _museumRelicDisplayController;

    [Tooltip("도감 영구 발견에 따른 최종 방 개방과 방 보상 수령을 연결합니다.")]
    [SerializeField] private MuseumRewardRoomController _museumRewardRoomController;

    /// <summary>박물관 벽화와 제단 목적지 시스템의 외부 공개 계약입니다.</summary>
    internal IMuseumService Museum => _museumController;
    /// <summary>술병 진행과 투입 시스템의 외부 공개 계약입니다.</summary>
    internal IMuseumBottleService MuseumBottles => _museumBottleController;
    /// <summary>유물 전시대와 마일스톤 시스템의 외부 공개 계약입니다.</summary>
    internal IMuseumRelicDisplayService MuseumRelicDisplays => _museumRelicDisplayController;
    /// <summary>최종 보상 방 개방과 보상 수령 시스템의 외부 공개 계약입니다.</summary>
    internal IMuseumRewardRoomService MuseumRewardRoom => _museumRewardRoomController;

    /// <summary>박물관 CSV와 Progress·기술 반환 상태를 연결합니다.</summary>
    internal bool TryInitializeMuseum(
        IGameDataRepository gameData,
        IProgressService progress,
        IRelicRestorationService restoration)
    {
        if (_museumController == null)
        {
            Debug.LogWarning("[MuseumBootstrapModule] MuseumController가 연결되지 않았습니다.");
            return true;
        }

        if (_museumController.TryInit(
                gameData, progress, restoration, out GameDataCsvLoadError loadError))
        {
            return true;
        }

        Debug.LogError($"[MuseumBootstrapModule] 박물관 데이터 초기화에 실패했습니다. {loadError}");
        return false;
    }

    /// <summary>인벤토리 복구 후 술병 CSV와 저장 상태를 연결합니다.</summary>
    internal bool TryInitializeBottles(
        IGameDataRepository gameData,
        IInventoryService inventory,
        IProgressService progress)
    {
        if (_museumBottleController == null)
        {
            Debug.LogWarning("[MuseumBootstrapModule] MuseumBottleController가 연결되지 않았습니다.");
            return true;
        }

        if (_museumBottleController.TryInit(
                gameData, inventory, progress, out GameDataCsvLoadError loadError))
        {
            return true;
        }

        Debug.LogError($"[MuseumBootstrapModule] 술병 데이터 초기화에 실패했습니다. {loadError}");
        return false;
    }

    /// <summary>인벤토리 복구 후 유물 전시 CSV와 저장 상태를 연결합니다.</summary>
    internal bool TryInitializeRelicDisplays(
        IGameDataRepository gameData,
        IInventoryService inventory,
        IProgressService progress)
    {
        if (_museumRelicDisplayController == null)
        {
            Debug.LogWarning(
                "[MuseumBootstrapModule] MuseumRelicDisplayController가 연결되지 않았습니다.");
            return true;
        }

        if (_museumRelicDisplayController.TryInit(
                gameData, inventory, progress, out GameDataCsvLoadError loadError))
        {
            return true;
        }

        Debug.LogError($"[MuseumBootstrapModule] 유물 전시 데이터 초기화에 실패했습니다. {loadError}");
        return false;
    }

    /// <summary>Codex 구버전 동기화 뒤 최종 보상 방 개방 상태를 평가합니다.</summary>
    internal bool TryInitializeRewardRoom(
        ICodexDataRepository data,
        ICodexService codex,
        IProgressService progress)
    {
        if (_museumRewardRoomController == null)
        {
            return true;
        }

        if (_museumRewardRoomController.Init(
                data, codex, progress, _museumRelicDisplayController))
        {
            return true;
        }

        Debug.LogError("[MuseumBootstrapModule] 최종 보상 방 초기화에 실패했습니다.");
        return false;
    }
}
