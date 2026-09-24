//=============================================================================
// File:        MuseumPhase8EditorValidator.cs
// Description: 기존 CSV·박물관 검사와 원본 에셋 무결성을 묶는 읽기 전용 최종 검증
// History:     08/31/2026 + Created -- Codex
//              09/23/2026 + Bootstrap Module 직렬화 및 Nested Prefab 검증 반영 -- Codex
//=============================================================================
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class MuseumPhase8EditorValidator
{
    internal const string ROOT_PATH = "Assets/_Game/Prefabs/GameRoot.prefab";
    private delegate bool Validation(out string message);

    //=============================================================================
    // ValidateFromMenu
    // Description: 모든 검사 결과를 모으며 원본 참조 오류를 전체 성공으로 숨기지 않습니다.
    // Parameters: None.
    // Return: void — 하나라도 실패하면 배치 실행도 실패합니다.
    //=============================================================================
    [MenuItem("Tools/Project Ruin/Museum/Validate Phase 8 Integration")]
    internal static void ValidateFromMenu()
    {
        var failures = new List<string>();
        var report = GameDataEditorValidator.Validate();
        GameDataEditorValidator.LogReport(report, true);
        if (report.ErrorCount > 0) failures.Add($"CSV·Localization 오류: {report.ErrorCount}");
        if (!TryValidateRoot(AssetDatabase.LoadAssetAtPath<GameObject>(ROOT_PATH), out string message))
            failures.Add(message);
        Run("Phase 4", MuseumPhase4EditorValidator.TryValidate, failures);
        Run("Phase 5", MuseumPhase5EditorValidator.TryValidate, failures);
        Run("Phase 6", MuseumPhase6EditorValidator.TryValidate, failures);
        Run("Phase 7", MuseumPhase7EditorTools.TryValidate, failures);
        InspectAssets("t:Scene", failures);
        InspectAssets("t:Prefab", failures);
        InspectRuntimeSources(failures);
        if (failures.Count > 0)
            throw new BuildFailedException("[MuseumPhase8] 통합 검증 실패: " + string.Join(" / ", failures));
        Debug.Log("[MuseumPhase8] 통합 연결 검증 통과. 콘텐츠 완성 여부는 각 단계의 개수 결과를 확인하세요.");
    }

    //=============================================================================
    // TryValidateRoot
    // Description: GameRoot 직렬화 연결이 같은 계층의 단일 Controller를 가리키는지 검사합니다.
    // Parameters: GameObject prefab, out string message
    // Return: bool
    //=============================================================================
    internal static bool TryValidateRoot(GameObject prefab, out string message)
    {
        message = "GameRoot 또는 Controller 연결이 없습니다.";
        if (prefab == null) return false;
        var roots = prefab.GetComponentsInChildren<GameRoot>(true);
        if (roots.Length != 1 || roots[0].gameObject != prefab
            || !roots[0].enabled || !prefab.activeSelf) return false;
        string[] fields = {
            "_coreModule._gameDataController",
            "_coreModule._progressController",
            "_gameplayModule._inventoryController",
            "_gameplayModule._relicRestorationController",
            "_gameplayModule._codexController",
            "_museumModule._museumController",
            "_museumModule._museumBottleController",
            "_museumModule._museumRelicDisplayController",
            "_museumModule._museumRewardRoomController",
            "_uiModule._museumBottlePresenter",
            "_uiModule._museumRelicPresenter",
            "_uiModule._inventoryPresenter",
            "_sceneFlowModule._stageFlowController",
            "_uiModule._codexNavigationPresenter"
        };
        var serialized = new SerializedObject(roots[0]);
        for (int i = 0; i < fields.Length; i++)
        {
            var value = serialized.FindProperty(fields[i])?.objectReferenceValue as MonoBehaviour;
            if (value == null || !value.enabled || !value.gameObject.activeSelf
                || !value.transform.IsChildOf(prefab.transform)
                || prefab.GetComponentsInChildren(value.GetType(), true).Length != 1)
            {
                message = $"GameRoot 참조 누락·중복·비활성·외부 연결: {fields[i]}";
                return false;
            }
            for (Transform parent = value.transform; parent != null
                && parent != prefab.transform; parent = parent.parent)
            {
                if (parent.gameObject.activeSelf) continue;
                message = $"GameRoot Controller의 부모가 비활성입니다: {fields[i]}";
                return false;
            }
        }
        message = "Museum Controller가 기능별 Nested Prefab 경계를 벗어났습니다.";
        Transform museumModule = prefab.transform.Find("MuseumModule");
        if (museumModule == null
            || prefab.GetComponents<MuseumController>().Length != 0
            || prefab.GetComponents<MuseumBottleController>().Length != 0
            || prefab.GetComponents<MuseumRelicDisplayController>().Length != 0
            || prefab.GetComponents<MuseumRewardRoomController>().Length != 0)
        {
            return false;
        }
        if (!IsMuseumModulePrefabBoundary(museumModule)) return false;
        message = "GameRoot의 도감·인벤토리 UI 교차 참조가 누락되거나 다른 대상을 가리킵니다.";
        if (!GameUiPrefabIntegrator.IsConnected(prefab)) return false;
        message = "GameRoot 단일 소유와 필수 직렬화 연결 확인";
        return true;
    }

    //=============================================================================
    // IsMuseumModulePrefabBoundary
    // Description: 에셋과 일반 Instantiate 복제본 모두에서 원본 GameRoot의 중첩 경계를 확인합니다.
    // Parameters: Transform museumModule
    // Return: bool
    //=============================================================================
    private static bool IsMuseumModulePrefabBoundary(Transform museumModule)
    {
        GameObject museumSource =
            PrefabUtility.GetCorrespondingObjectFromOriginalSource(museumModule.gameObject);
        if (museumSource != null
            && AssetDatabase.GetAssetPath(museumSource)
                == MuseumBootstrapPrefabUtility.PREFAB_PATH)
        {
            return true;
        }

        // Object.Instantiate로 만든 검증 복제본은 Prefab 연결 정보를 유지하지 않습니다.
        // 이때는 변경 가능한 복제본의 구성 검증과 별개로 원본 GameRoot 에셋의 경계를 확인합니다.
        GameObject rootAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ROOT_PATH);
        Transform assetModule = rootAsset != null ? rootAsset.transform.Find("MuseumModule") : null;
        GameObject assetSource = assetModule != null
            ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(assetModule.gameObject)
            : null;
        return assetSource != null
            && AssetDatabase.GetAssetPath(assetSource)
                == MuseumBootstrapPrefabUtility.PREFAB_PATH;
    }

    //=============================================================================
    // TryValidateComponentReferences
    // Description: Unity가 Preview 로드 중 제거할 수 있는 끊어진 로컬 컴포넌트 참조를 원문에서 찾습니다.
    // Parameters: string yaml, out string missingId
    // Return: bool — 일반 GUID 참조나 씬 전체 의미 검증을 대신하지 않습니다.
    //=============================================================================
    internal static bool TryValidateComponentReferences(string yaml, out string missingId)
    {
        var ids = new HashSet<string>();
        foreach (Match match in Regex.Matches(yaml, @"^--- !u!\d+ &(-?\d+)", RegexOptions.Multiline))
            ids.Add(match.Groups[1].Value);
        foreach (Match match in Regex.Matches(yaml, @"^\s*- component: \{fileID: (-?\d+)\}", RegexOptions.Multiline))
        {
            string id = match.Groups[1].Value;
            if (ids.Contains(id)) continue;
            missingId = id;
            return false;
        }
        missingId = string.Empty;
        return true;
    }

    private static void Run(string label, Validation validate, List<string> failures)
    {
        if (!validate(out string message)) failures.Add($"{label}: {message}");
        else Debug.Log($"[MuseumPhase8] {label}: {message}");
    }

    // 대상 런타임의 금지 API 재유입을 검사합니다. 기존 공용 시스템 전체를 리팩터링하지 않습니다.
    internal static bool HasForbiddenRuntimeCall(string source) =>
        Regex.IsMatch(source, @"\b(?:FindFirstObjectByType|FindAnyObjectByType|FindObjectOfType|FindObjectsOfType|FindObjectsByType|StartCoroutine)\s*(?:<[^>]+>)?\s*\(|\bGameObject\s*\.\s*Find\s*\(");

    private static void InspectRuntimeSources(List<string> failures)
    {
        string[] files = Directory.GetFiles("Assets/_Game/Scripts", "*.cs", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string path = files[i].Replace('\\', '/');
            if (!path.Contains("/Gameplay/Codex/") && !path.Contains("/Gameplay/Museum/")
                && !path.Contains("/Presentation/UI/Museum/") && !path.Contains("/Presentation/UI/Codex"))
                continue;
            if (HasForbiddenRuntimeCall(File.ReadAllText(path)))
                failures.Add($"{path}: 금지된 전역 탐색 또는 런타임 Coroutine 호출");
        }
    }

    private static void InspectAssets(string filter, List<string> failures)
    {
        string[] guids = AssetDatabase.FindAssets(filter, new[] { "Assets/_Game" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!TryValidateComponentReferences(File.ReadAllText(path), out string id))
            {
                failures.Add($"{path}: 존재하지 않는 Component fileID={id}");
                continue;
            }
            if (filter != "t:Scene") continue;
            var scene = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                int count = 0;
                foreach (GameObject sceneRoot in scene.GetRootGameObjects())
                {
                    var roots = sceneRoot.GetComponentsInChildren<GameRoot>(true);
                    count += roots.Length;
                    for (int r = 0; r < roots.Length; r++)
                        if (!TryValidateRoot(roots[r].gameObject, out string message))
                            failures.Add($"{path}: {message}");
                }
                if (count > 1) failures.Add($"{path}: GameRoot 중복 {count}개");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
