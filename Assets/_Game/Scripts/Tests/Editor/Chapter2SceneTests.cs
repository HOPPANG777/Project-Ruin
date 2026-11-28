//=============================================================================
// File:        Chapter2SceneTests.cs
// Description: Chapter2 씬의 구성(맵·도착 지점)과 SceneCatalog·빌드 설정 등록을 검사합니다.
// History:     11/28/2026 + Created
//=============================================================================
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Chapter1과 같은 스테이지 씬 구성(카메라 + 맵 + 도착 지점)인지 확인한다.</summary>
public sealed class Chapter2SceneTests
{
    private const string ScenePath = "Assets/_Game/Scenes/Chapter/Chapter2.unity";

    [Test]
    public void SceneIsRegisteredAsStageFourAndInBuildSettings()
    {
        SceneCatalogSO catalog = AssetDatabase.LoadAssetAtPath<SceneCatalogSO>("Assets/_Game/Data/SceneCatalog/SceneCatalogSO.asset");
        Assert.That(catalog.TryGetByStageNumber(Ch2Util.StageNumber, out SceneCatalogSO.SceneEntry entry), Is.True);
        Assert.That(entry.SceneName, Is.EqualTo("Chapter2"));
        Assert.That(entry.SceneCategory, Is.EqualTo(SceneCategory.STAGE));
        Assert.That(EditorBuildSettings.scenes.Any(item => item.enabled && item.path == ScenePath), Is.True);
    }

    /// <summary>광선·미로 컨트롤러가 플레이 시작 때 하는 자체 설정 검증을 통과하고, 광선은 기둥을 한 번씩 눌러 풀린다.</summary>
    [Test]
    public void ControllersPassOwnValidationAndEveryBeamSolvesByOnePressPerMiddlePillar()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            GameObject grid = scene.GetRootGameObjects().First(item => item.name == "Grid_2");
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            MazePlatePuzzleController[] mazes = grid.GetComponentsInChildren<MazePlatePuzzleController>(true);
            for (int i = 0; i < mazes.Length; i++)
            {
                bool valid = (bool)typeof(MazePlatePuzzleController).GetMethod("TryValidateSetup", flags).Invoke(mazes[i], new object[] { false });
                Assert.That(valid, Is.True, mazes[i].name);
            }
            Physics2D.SyncTransforms();
            BeamPuzzleController[] beams = grid.GetComponentsInChildren<BeamPuzzleController>(true);
            for (int b = 0; b < beams.Length; b++)
            {
                System.Collections.Generic.List<string> issues = new System.Collections.Generic.List<string>();
                beams[b].CollectValidationIssues(issues);
                Assert.That(issues, Is.Empty, beams[b].name + ": " + string.Join(" / ", issues));
                beams[b].PrepareSceneState();
                SerializedProperty pillars = new SerializedObject(beams[b]).FindProperty("_pillars");
                Assert.That(beams[b].IsClear, Is.False);
                for (int i = 1; i < pillars.arraySize - 1; i++)
                {
                    BeamPillarView pillar = (BeamPillarView)pillars.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.That(beams[b].TryRotatePillar(pillar), Is.True,
                        $"{beams[b].name} 기둥 {i} rotatable={pillar.IsRotatable} ownerOk={pillar.Owner == beams[b]} clear={beams[b].IsClear}");
                }
                Assert.That(beams[b].IsClear, Is.True, beams[b].name + " 모든 중간 기둥을 한 번씩 눌러도 끝 기둥에 닿지 않습니다.");
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void SceneHasOnlyCameraGrid2AndAnArrivalPointOnRealFloor()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            GameObject[] roots = scene.GetRootGameObjects();
            Assert.That(roots.Length, Is.EqualTo(3), string.Join(", ", roots.Select(item => item.name)));
            GameObject grid = roots.First(item => item.name == "Grid_2");
            Assert.That(PrefabUtility.IsAnyPrefabInstanceRoot(grid), Is.True);
            Assert.That(grid.GetComponentsInChildren<UiPuzzleInteractable>(true).Length, Is.EqualTo(10));
            Assert.That(grid.GetComponentsInChildren<MazePlatePuzzleController>(true).Length, Is.EqualTo(4));
            Assert.That(grid.GetComponentsInChildren<BeamPuzzleController>(true).Length, Is.EqualTo(2));

            SpawnPoint spawn = roots.Select(item => item.GetComponent<SpawnPoint>()).First(item => item != null);
            Assert.That(spawn.SpawnPointId, Is.EqualTo(Chapter2SceneBuilder.EntranceId));
            Tilemap village = grid.GetComponentsInChildren<Tilemap>(true).First(item => item.name == "Tilemap2");
            Vector3Int cell = village.WorldToCell(spawn.transform.position);
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Assert.That(village.GetTile(cell + new Vector3Int(x, y, 0)), Is.Not.Null, $"도착 지점 주변 바닥 없음 {x},{y}");
                }
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
