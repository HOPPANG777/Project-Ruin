# 챕터 1 마을 배치·저장 검증 기록

Grid_1 프리팹에 네 UI 퍼즐, Point1~4의 문 충돌체, 이동 패드 다섯 개를 반영했다. 저장·복원과 패드 연결은 검증됐지만, **Point1과 Point3의 기존 지형 통로는 현재 플레이어 충돌체로 통과하지 못한다. 따라서 전체 통행 완료로 보고하지 않는다.**

## 반영한 연결

| 문 | 조작대 데이터 | UI 챕터 | 패드 |
| --- | --- | --- | --- |
| Point1 | Pipe / PIPE_STAGE_03 | 1 | 같은 Grid_1의 Chapter1_ruin 입구로 Position 이동 |
| Point2 | Pipe / PIPE_STAGE_04 | 1 | Scene 이동, 목적지 미정·감지 충돌체 비활성 |
| Point3 | Light / LIGHT_STAGE_01 | 1 | Scene 이동, 목적지 미정·감지 충돌체 비활성 |
| Point4 | Pipe / PIPE_STAGE_05 | 1 | Scene 이동, 목적지 미정·감지 충돌체 비활성 |

SceneCatalog에 이미 등록된 Chapter1의 저장 스테이지 번호는 **3**이다. UI Chapter Index 1과 구분하여 사용했다. SceneCatalog 및 Build Settings는 작업 시작 상태를 보존했다.

Point1~4의 이름·위치는 유지했고 기존 문 스프라이트와 동일한 크기·회전의 solid BoxCollider2D를 연결했다. 문만 별도의 오브젝트로 비활성화하므로 조작대와 패드는 남는다. Point5와 기존 주변 오브젝트·타일 데이터는 보존했다.

## 저장 연결

- 배치 ID는 `CH1_VILLAGE_POINT_1`부터 `CH1_VILLAGE_POINT_4`까지 프리팹에 고정했다. CSV 데이터 ID와 별개이며 런타임 ID 생성은 없다.
- `UiPuzzleSceneState`는 기존 ISceneStateContributor로 완료 여부만 Capture/Apply하고 자기 문을 연결한다. 미완성 조각 배치는 저장하지 않는다.
- `UiPuzzleStateController`는 기존 SceneFlowBootstrapModule.Init에서 Progress 서비스를 주입받는다. 초기 씬, 이후 로드된 씬, 비활성 배치를 연결하고 Progress.StateLoaded 이후 복원한다. 발견은 이 초기화 경로에만 있다.
- 최초 클리어 시 완료 여부를 Progress 메모리에 먼저 기록하고 곧바로 기존 SaveAsync를 요청한다. 같은 연결에서 연속 요청을 묶으며, 파일 응답이 실패하면 성공으로 표시하지 않고 메모리·Dirty를 유지한다. 다음 기존 저장 또는 다음 클리어 요청이 재저장할 수 있다.
- 최초 클리어 이벤트와 RestoreSolvedState/ApplyState를 분리했다. 반복 복원은 클리어 이벤트, 보상, 개방 연출, 저장 요청을 발생시키지 않는다. 저장 초기화 전에는 해당 조작대의 Interact를 차단한다.
- 기존 ProgressController, SceneStateBridge, ES3 파일 경로 및 공용 PuzzleUiRoot를 재사용했다. 새 저장 파일 체계나 싱글턴·인터페이스는 추가하지 않았다.

## 임시 배치 조정

아래 좌표는 `Grid_1/Chapter_1` 기준이며 Grid_1 자체의 원래 위치와 무관하다. 새 조작대·패드는 `Chapter1VillagePuzzles` 아래에 있다. 문 크기를 조정할 때 SpriteRenderer.size와 BoxCollider2D.size를 함께 맞춘다.

| 항목 | 조작대 (x, y) | 문 크기 / 회전 | 출발 패드 (x, y) |
| --- | --- | --- | --- |
| Point1 | (464.6, 50.0) | 7 × 1.5 / 90° | (454.6, 50.6) |
| Point2 | (612.2, 40.8) | 8 × 1.5 / 0° | (612.2, 49.8) |
| Point3 | (655.8, -36.0) | 9 × 1.5 / 45° | (668.8, -48.0) |
| Point4 | (600.8, -80.6) | 7 × 1.5 / 90° | (609.8, -80.6) |

- 유적 복귀 패드: (944.2, -59.8). Chapter1_ruin 타일 좌표로 (2381.5, 343.5), 기존 하단 입구 바로 안쪽이다.
- 유적 도착 지점: (950.2, -59.8). 복귀 패드에서 6 떨어진 기존 바닥이다.
- 마을 복귀 도착 지점: (450.6, 50.6). 출발 패드에서 4 떨어진 기존 바닥이다.
- 두 Position 패드는 상대 패드의 도착 Transform을 참조한다. 도착 지점은 실제 2×3 플레이어 범위에서도 벽·감지 충돌체와 겹치지 않는다.
- Point2~4는 향후 실제 목적지의 `_destinationStageNumber`, `_targetSpawnPointId`를 지정한 다음 해당 인스턴스의 BoxCollider2D.enabled를 켜야 한다. 지금은 -1/빈 ID/비활성 충돌체를 유지한다. 공유 원본 프리팹은 보존했다.

## 검증 결과와 부족 사항

**데이터 계산:** PIPE_STAGE_04 원본의 고정 끝 방향으로 가능한 단순 연결 경로가 없음을 확인했다. 승인된 END 회전 1에서는 경로 (0,0) → (0,1) → (1,1) → (2,1) → (2,0)가 가능하다. 기존 인접 빈 칸 이동 6회와 허용 회전으로 재현했다. LIGHT_STAGE_01은 원본 그대로 2, 3, 4, 8번 타일 클릭으로 해결된다.

**Unity 자동 테스트:** Unity 6000.3.14f1의 격리된 프로젝트 복사본에서 관련 기존 테스트와 신규 검증 **107개 중 105개 통과, 2개 실패**. 원본 에디터는 종료하지 않았다.

마지막 foreach 제거 뒤 CirclePuzzleTests만 다시 실행하여 12개 모두 통과했다. 검증용 프로젝트 복사본은 정리했고 결과 XML·로그·ES3 증거는 보존했다.

통과한 검증:

- 세 파이프와 라이트를 실제 기존 UI 매니저의 드래그·회전·클릭으로 해결.
- 네 조작대의 기존 Interact → 공용 PuzzleUiRoot → 해당 팝업·데이터 ID·Chapter Index 1 연결.
- 다른 퍼즐 클리어와 관계없는 독립 완료, 자기 문만 비활성화, 완료 후 상호작용 충돌체 차단.
- 클리어 직후 실제 ES3 파일 생성, 연속 요청, 새 Progress에서 파일 재로드, 씬 배치 재생성, 비활성 상태의 Awake 이전 복원, 반복 복원·중복 이벤트 방지, 같은 데이터의 서로 다른 저장 ID.
- 실제 쓰기 실패 시 false 결과와 Dirty·완료 상태 유지, 다음 클리어 저장에서 두 상태의 파일 복구.
- Position 패드의 실제 플레이 모드 트리거로 왕복, 도착 후 자동 왕복 없음, 도착 범위의 벽·감지 충돌체 비중첩.
- 미정 Scene 패드 위에서 전환·페이드 호출·런타임 경고 없음. Point2와 Point4의 문을 연 뒤 실제 플레이어 크기로 통행 가능.
- 프리팹 원본 참조, 문 표시/충돌 크기 일치, 상호작용 레이어 8, 공용 UI와 Bootstrap 직렬화 연결.

남은 실패:

- `EachClosedDoorBlocksItsPassageAndOpenPassageHasNoSolidCollision("PIPE_STAGE_03")`: Point1의 문 제거 후에도 조작대와 패드 사이의 기존 목 부분을 통과하지 못함.
- 같은 검사 `LIGHT_STAGE_01`: Point3의 문 제거 후에도 기존 굽은 목 부분을 통과하지 못함.
- 양쪽 조작대·패드 중심은 플레이어 크기로 벽과 겹치지 않는다. 문제는 그 사이의 기존 지형 충돌이다. Point1은 x=458~462, y=47~55 범위의 실제 충돌 검사에서 2.02×3.02 크기의 통과 가능한 중심이 없었다.
- 통행 검사는 기존 2×3 플레이어에 작은 여유를 더한 2.02×3.02 크기, 0.25 간격으로 문 양측과 주변 6 범위의 경로를 탐색했다. 좁은 목 부분의 지형 충돌 범위 또는 플레이어 충돌 크기를 별도 조정해야 한다. 이번 작업에서는 기존 타일·벽·플레이어 설정을 임의 변경하지 않았다.

**플레이 검증의 범위:** 자동 테스트 안에서 Unity 플레이 모드·실제 물리 트리거와 Interact 경로를 실행했다. 패드의 페이드 서비스는 호출 기록 및 암전 콜백 대역을 사용했다. 사람이 원본 씬에서 키보드·마우스로 플레이하는 검증과 실제 화면 페이드·임시 배치의 시각 QA는 수행하지 않았다. 전체 통행을 통과했다고 표현하지 않는다.

검증 XML·로그·실제 ES3 증거 파일은 `Logs/Chapter1Validation/`에 있다. `final-results.xml`에 두 통행 실패를 그대로 남겼다. 작업 시작 파일의 해시 비교로 CONVENTIONS.md, 기존 GUID/.meta, 다른 맵·설정 및 기존 미커밋 변경 보존을 확인했다.

## 변경 파일과 줄 수

기존 파일 수정:

- `Assets/_Game/Prefabs/Map/Grid_1.prefab`: 기존 Point1~4 표시/충돌 및 기존 퍼즐·패드의 nested prefab 인스턴스 연결.
- `Assets/_Game/Prefabs/GameRoot.prefab`: 저장 연결 컨트롤러 및 기존 Module 참조.
- `Assets/_Game/Data/Csv/puzzle_pipe_tiles.csv`: 승인된 PIPE_STAGE_04 끝 타일의 회전 한 값만 변경.
- `Assets/_Game/Scripts/Core/Bootstrap/SceneFlowBootstrapModule.cs` (60줄): 기존 Init에서 저장 연결 주입.
- `Assets/_Game/Scripts/Puzzle/Interaction/UiPuzzleInteractable.cs` (149줄): 최초 클리어 이벤트와 무효과 복원 API, 초기화 전 상호작용 차단.
- `Assets/_Game/Scripts/Tests/Editor/CirclePuzzleTests.cs` (137줄): 수동 Awake의 SendMessage가 발생시키던 EditMode Assert를 Reflection 호출로 검증하고, 변경 대상 파일의 기존 foreach를 for로 전환.

신규 런타임 클래스:

| 파일 | 책임 | 파일 줄 수 |
| --- | --- | --- |
| `UiPuzzleSceneState.cs` | 배치 ID·완료 상태 contributor와 자기 문 연결 | 106 |
| `UiPuzzleStateController.cs` | Bootstrap 주입, 씬 배치 발견·복원, 즉시 저장 요청 중개 | 113 |

신규 테스트 파일과 .meta:

| 파일 | 검증 책임 | 파일 줄 수 |
| --- | --- | --- |
| `Chapter1PuzzleDataTests.cs` | 실제 CSV와 기존 UI 조작 해법 | 99 |
| `Chapter1VillageLayoutTests.cs` | 맵 구성, 독립 개방, 실제 크기의 통행·도착 범위 | 190 |
| `UiPuzzlePersistenceTests.cs` | 실제 ES3 저장·재로드·실패·초기화 순서 | 191 |
| `Chapter1PadRuntimeTests.cs` | 실제 트리거 및 공용 UI 연결; PadFlowProbe는 테스트용 전환 호출 기록 | 165 |

신규 런타임 두 파일의 .meta도 추가했다. 모든 신규·변경 C# 파일은 200줄 이내이며 foreach를 포함하지 않는다. 기존 긴 ProgressController/SceneStateBridge는 이번 작업에서 수정하지 않았다.
