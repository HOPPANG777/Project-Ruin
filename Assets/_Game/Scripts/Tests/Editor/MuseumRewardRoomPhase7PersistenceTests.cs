//=============================================================================
// File:        MuseumRewardRoomPhase7PersistenceTests.cs
// Description: 사용자 세이브와 분리된 실제 ES3 파일에서 방·아이템·수령 상태를 재로드한다.
// History:     08/31/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed partial class MuseumRewardRoomPhase7SceneTests
{
    [UnityTest]
    public IEnumerator RealEs3RoundTripRestoresDoorExhibitsInventoryAndSingleClaim()
    {
        // Unity 프로젝트 Logs 아래 고유 파일만 사용합니다. 기본 플레이어 저장 경로는 건드리지 않습니다.
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Phase7Validation"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "test-save-" + Guid.NewGuid().ToString("N") + ".es3");
        Assert.That(File.Exists(path), Is.False);
        ProgressController progress = CreateFileProgress(path);
        yield return progress.LoadAsync().ToCoroutine(loaded => Assert.That(loaded, Is.True));
        CreateRealServices(progress);
        DiscoverAllReal();
        yield return FlushProgress(progress);
        for (int i = 1; i <= 20; i++)
        {
            Assert.That(_displays.TryPlace("P" + i, RelicId(i), out _), Is.True);
            yield return FlushProgress(progress);
        }
        Assert.That(File.Exists(path), Is.True);
        Assert.That(new FileInfo(path).Length, Is.GreaterThan(0));
        Assert.That(_realRoom.TryGetRewardStatus("TEST_ALL", out var before), Is.True);
        Assert.That(before.IsClaimed, Is.False);
        Object.DestroyImmediate(_servicesRoot);
        Object.DestroyImmediate(progress.gameObject);

        // 새 Progress와 모든 새 도메인 Controller로 파일을 다시 읽습니다.
        progress = CreateFileProgress(path);
        yield return progress.LoadAsync().ToCoroutine(loaded => Assert.That(loaded, Is.True));
        CreateRealServices(progress);
        Assert.That(_realRoom.IsOpen, Is.True);
        Assert.That(_door.enabled, Is.False);
        Assert.That(_displays.DisplayedRelicCount, Is.EqualTo(20));
        Assert.That(_inventory.GetItemCount(RelicId(1)), Is.Zero);
        Assert.That(_inventory.GetItemCount("TEST_BONUS"), Is.EqualTo(1));
        _view.SetInteractionContext(_player, null);
        Assert.That(_view.TryInteract(out _), Is.True);
        yield return FlushProgress(progress);
        Assert.That(_inventory.GetItemCount("TEST_SWORD"), Is.EqualTo(1));
        Object.DestroyImmediate(_servicesRoot);
        Object.DestroyImmediate(progress.gameObject);

        // 수령 후 저장도 새 인스턴스에서 복원하며 아이템·수령 상태가 함께 유지돼야 합니다.
        progress = CreateFileProgress(path);
        yield return progress.LoadAsync().ToCoroutine(loaded => Assert.That(loaded, Is.True));
        CreateRealServices(progress);
        Assert.That(_inventory.GetItemCount("TEST_SWORD"), Is.EqualTo(1));
        Assert.That(_realRoom.TryGetRewardStatus("TEST_ALL", out var claimed), Is.True);
        Assert.That(claimed.IsClaimed, Is.True);
        Assert.That(_reward.enabled, Is.False);
        Assert.That(_inventory.TryConsumeItem("TEST_SWORD", 1), Is.True);
        yield return FlushProgress(progress);
        Object.DestroyImmediate(_servicesRoot);
        Object.DestroyImmediate(progress.gameObject);

        progress = CreateFileProgress(path);
        yield return progress.LoadAsync().ToCoroutine(loaded => Assert.That(loaded, Is.True));
        CreateRealServices(progress);
        Assert.That(_realRoom.IsOpen, Is.True);
        Assert.That(_inventory.GetItemCount("TEST_SWORD"), Is.Zero);
        _view.SetInteractionContext(_player, null);
        Assert.That(_view.TryInteract(out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_MILESTONE_ALREADY_CLAIMED));
        // 검증 근거인 테스트 파일은 gitignore된 Logs에 남깁니다. 사용자 파일 삭제는 없습니다.
    }

    private ProgressController CreateFileProgress(string path)
    {
        var progress = Child("FileProgress").AddComponent<ProgressController>();
        Set(progress, "_saveFilePath", path);
        return progress;
    }

    // 도메인의 자동 저장이 진행 중이면 완료될 때까지 기다린 후 최신 Dirty까지 저장합니다.
    private static IEnumerator FlushProgress(ProgressController progress)
    {
        bool saved = false;
        float deadline = Time.realtimeSinceStartup + 10;
        while (!saved && Time.realtimeSinceStartup < deadline)
        {
            yield return progress.SaveAsync().ToCoroutine(result => saved = result);
            if (!saved) yield return null;
        }
        Assert.That(saved, Is.True, "ES3 저장이 제한 시간 내에 완료돼야 합니다.");
        Assert.That(progress.IsDirty, Is.False);
    }
}
