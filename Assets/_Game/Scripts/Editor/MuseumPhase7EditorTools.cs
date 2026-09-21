//=============================================================================
// File:        MuseumPhase7EditorTools.cs
// Description: 최종 보상 방의 GameRoot 연결과 읽기 전용 CSV·씬·빌드 검증
// History:     08/31/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class MuseumPhase7EditorTools
{
    private const string ROOT_PATH = "Assets/_Game/Prefabs/GameRoot.prefab";

    //=============================================================================
    // IntegrateFromMenu
    // Description: 기존 GameRoot 프리팹에 누락된 Controller만 추가·연결합니다.
    // Parameters: None.
    // Return: void
    //=============================================================================
    [MenuItem("Tools/Project Ruin/Museum/Integrate Phase 7 Into GameRoot")]
    internal static void IntegrateFromMenu()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROOT_PATH);
        if (prefab == null || prefab.GetComponent<GameRoot>() == null)
            throw new BuildFailedException("[MuseumPhase7] GameRoot 프리팹이 없습니다.");
        var existing = prefab.GetComponent<MuseumRewardRoomController>();
        if (existing != null && ReferenceEquals(prefab.GetComponent<GameRoot>().MuseumRewardRoom, existing))
        {
            Debug.Log("[MuseumPhase7] GameRoot 연결 유지: 변경 없음.");
            return;
        }
        GameObject contents = PrefabUtility.LoadPrefabContents(ROOT_PATH);
        try
        {
            var controller = contents.GetComponent<MuseumRewardRoomController>();
            if (controller == null) controller = contents.AddComponent<MuseumRewardRoomController>();
            CodexUiElementFactory.SetReference(contents.GetComponent<GameRoot>(),
                "_museumRewardRoomController", controller);
            PrefabUtility.SaveAsPrefabAsset(contents, ROOT_PATH);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        Debug.Log("[MuseumPhase7] GameRoot 연결 완료. 미확정 방 콘텐츠는 생성하지 않았습니다.");
    }

    //=============================================================================
    // ValidateFromMenu / TryValidate
    // Description: CSV와 프리팹·Preview Scene만 검사하며 Progress는 초기화하지 않습니다.
    // Parameters: None / out string message
    // Return: void / bool
    //=============================================================================
    [MenuItem("Tools/Project Ruin/Museum/Validate Phase 7 Data")]
    internal static void ValidateFromMenu()
    {
        if (!TryValidate(out string message)) throw new BuildFailedException($"[MuseumPhase7] {message}");
        Debug.Log($"[MuseumPhase7] {message}");
    }

    internal static bool TryValidate(out string message)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROOT_PATH);
        GameRoot root = prefab != null ? prefab.GetComponent<GameRoot>() : null;
        if (root == null || root.MuseumRewardRoom == null || root.Codex == null
            || root.Progress == null || root.MuseumRelicDisplays == null || root.Museum == null
            || !ReferenceEquals(root.MuseumRewardRoom, prefab.GetComponent<MuseumRewardRoomController>()))
            return Fail("GameRoot의 도감·Progress·전시·벽화·최종 방 연결이 누락됐습니다.", out message);
        GameDataController data = prefab.GetComponentInChildren<GameDataController>(true);
        if (data == null) return Fail("GameDataController가 없습니다.", out message);
        if (!data.TryInit(out var dataError)) return Fail($"게임 CSV 오류: {dataError}", out message);
        GameDataCsvFile rewardCsv = ReadCsv((Object)root.MuseumRelicDisplays, "_museumRelicMilestonesCsv");
        if (rewardCsv == null) return Fail("전시 마일스톤 CSV 연결이 없습니다.", out message);
        if (!MuseumRelicMilestoneCsvLoader.TryLoadRepository(rewardCsv, data.Repository,
                out var rewards, out var rewardError)) return Fail($"보상 CSV 오류: {rewardError}", out message);
        GameDataCsvFile muralCsv = ReadCsv((Object)root.Museum, "_museumMuralsCsv");
        if (muralCsv == null) return Fail("벽화 CSV 연결이 없습니다.", out message);
        if (!MuseumCsvDefinitionLoader.TryLoad(muralCsv, null, out var murals, out _, out var muralError))
            return Fail($"벽화 CSV 오류: {muralError}", out message);
        var byId = new Dictionary<string, MuseumMuralDefinition>();
        foreach (var mural in murals)
        {
            if (byId.ContainsKey(mural.MuralId)
                || !data.Repository.CodexData.TryGetTechnology(mural.TechnologyId, out _))
                return Fail($"벽화 ID 중복 또는 기술 참조 오류: {mural.MuralId}", out message);
            byId.Add(mural.MuralId, mural);
        }
        if (!TryInspectScenes(rewards, byId, out int roomCount, out message)) return false;
        int relics = 0;
        foreach (var entry in data.Repository.CodexData.CodexEntries)
            if (entry.Category == CodexCategory.CODEX_CATEGORY_RELIC) relics++;
        bool ready = roomCount == 1 && rewards.Milestones.Count == 2 && relics > 0;
        message = (ready ? "기반·콘텐츠 연결 검증 통과" : "기반 검증 통과 / 콘텐츠 미완성")
            + $": relics={relics}, milestones={rewards.Milestones.Count}, rooms={roomCount}";
        return true;
    }

    // 문과 보검의 Renderer만 바꾸므로 씬 루트나 기존 벽화 활성 상태를 덮어쓰지 않습니다.
    private static bool TryInspectScenes(MuseumRelicMilestoneRepository rewards,
        Dictionary<string, MuseumMuralDefinition> murals, out int roomCount, out string message)
    {
        roomCount = 0;
        message = string.Empty;
        int configuredLayers = 0;
        var roomLayers = new List<Vector2Int>();
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Game" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                var rooms = new List<MuseumRewardRoom>();
                var sceneMurals = new List<MuseumMural>();
                foreach (GameObject sceneRoot in scene.GetRootGameObjects())
                {
                    rooms.AddRange(sceneRoot.GetComponentsInChildren<MuseumRewardRoom>(true));
                    sceneMurals.AddRange(sceneRoot.GetComponentsInChildren<MuseumMural>(true));
                    foreach (var player in sceneRoot.GetComponentsInChildren<PlayerInteractionController>(true))
                        configuredLayers |= new SerializedObject(player).FindProperty("_interactionLayers").intValue;
                }
                roomCount += rooms.Count;
                if (roomCount > 1) return Fail("최종 보상 방은 전체 씬에서 하나여야 합니다.", out message);
                foreach (MuseumRewardRoom room in rooms)
                {
                    var bounds = (BoxCollider2D)new SerializedObject(room)
                        .FindProperty("_roomBounds").objectReferenceValue;
                    roomLayers.Add(new Vector2Int(room.gameObject.layer, bounds != null ? bounds.gameObject.layer : -1));
                    if (!TryValidateRoom(room, rewards, murals, out message))
                    {
                        message = $"{path}: {message}";
                        return false;
                    }
                }
                foreach (MuseumMural mural in sceneMurals)
                    if (murals.TryGetValue(mural.MuralId, out var definition) && definition.Chapter == 3
                        && (rooms.Count != 1 || rooms[0].Chapter3Mural != mural))
                        return Fail($"{path}: 챕터 3 벽화는 최종 보상 방에 연결해야 합니다.", out message);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        foreach (var layers in roomLayers)
            if (!TryValidateInteractionLayers(layers.x, layers.y, configuredLayers, out message)) return false;
        return true;
    }

    // 프로젝트 씬의 실제 플레이어 마스크를 사용하며 레이어 번호를 고정하지 않습니다.
    internal static bool TryValidateInteractionLayers(int pickupLayer, int boundsLayer,
        int configuredLayers, out string message)
    {
        if (pickupLayer < 0 || pickupLayer > 31 || (configuredLayers & (1 << pickupLayer)) == 0)
            return Fail("보검 레이어를 탐지하는 플레이어 Interaction Layers 설정이 없습니다.", out message);
        if (boundsLayer < 0 || boundsLayer > 31 || (configuredLayers & (1 << boundsLayer)) != 0)
            return Fail("방 범위 Trigger는 플레이어 상호작용 탐지 레이어에서 제외해야 합니다.", out message);
        message = string.Empty;
        return true;
    }

    // 테스트에서도 같은 순수 참조 검사 경로를 사용합니다.
    internal static bool TryValidateRoom(MuseumRewardRoom room,
        MuseumRelicMilestoneRepository rewards, Dictionary<string, MuseumMuralDefinition> murals,
        out string message)
    {
        if (!room.TryValidateLayout(out message)) return false;
        if (!rewards.TryGet(room.RewardMilestoneId, out var reward)
            || reward.RewardMode != MuseumRelicRewardMode.MUSEUM_RELIC_REWARD_ENABLE_ROOM_PICKUP)
            return Fail("보상 ID는 ENABLE_ROOM_PICKUP 행이어야 합니다.", out message);
        if (!murals.TryGetValue(room.Chapter3Mural.MuralId, out var mural) || mural.Chapter != 3)
            return Fail("최종 보상 방 벽화의 CSV chapter는 3이어야 합니다.", out message);
        return true;
    }

    private static GameDataCsvFile ReadCsv(Object component, string field)
    {
        var csv = new SerializedObject(component).FindProperty(field)?.objectReferenceValue as TextAsset;
        return csv != null ? new GameDataCsvFile(csv.name, csv.text) : null;
    }

    private static bool Fail(string value, out string message)
    {
        message = value;
        return false;
    }
}

internal sealed class MuseumPhase7BuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 27;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (!MuseumPhase7EditorTools.TryValidate(out string message))
            throw new BuildFailedException($"[MuseumPhase7] {message}");
    }
}
