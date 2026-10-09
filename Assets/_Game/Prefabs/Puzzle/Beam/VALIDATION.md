# Beam 구현 검증 결과

검증일: 2026-10-06 (Asia/Seoul)
Unity: 6000.3.14f1 / Input System 1.19.0 / URP 17.3.0

## BeamPuzzlePoint의 실제 맵 배치

2026-10-06 참고 그림의 배치를 `Assets/_Game/Prefabs/Map/GridChapter0.prefab`의 기존 `BeamPuzzlePoint` 아래에 반영했다. 기존 개별 프리팹을 연결한 기둥 7개(시작 1, 중계 5, 끝 1), 고정 장애물 3개로 만든 두 덩어리, 초기화 발판 1개, 오른쪽 출구 장애물 1개, 관리자 1개를 추가했다. 저장 ID는 `CH00_BEAM_ROOM_01`이다. 별도 설치 메뉴나 완성 퍼즐 프리팹은 추가하지 않았다.

최신 맵의 기존 오브젝트 데이터를 보존하고 marker의 자식 연결과 신규 직렬화 오브젝트만 추가했다. 작업 중 저장된 그림 정렬·기존 미로 설정·삭제 내용을 유지한 배치로 다시 검증했다. 맵 GUID와 기존 미로 문은 유지했다.

- **BeamChapter0LayoutTests 3 / 3, BeamPuzzleTests 36 / 36: 총 39 / 39 통과.**
- 실제 참조, 개별 프리팹 연결, 그림과 장애물 충돌 크기 일치, 정답 전체 연결, 초기화, 완료 잠금·출구 개방·최종 광선 6개 유지를 확인했다.
- 기존 플레이어의 2×3 몸체로 서쪽 미로 문을 지나 Beam 방에 들어온 위치부터 통행을 검사했다. 중간 기둥 5개 모두 상호작용 범위 안까지 접근하고, 발판에 도달하고, 출구를 연 뒤 오른쪽 통로로 이동할 수 있다.
- `Temp/BeamPlace` 복사본에서 Unity 에셋 로드·코드 컴파일·물리 검사를 수행했다. 검증 복사본과 배치용 임시 코드는 종료 후 정리했다.
- 최종 로그/XML: `Logs/BeamValidation/placement-latest-tests.log`, `placement-latest-results.xml`.
- 좌표 기반 정답 배치도: `Logs/BeamValidation/Beam-room-placement.png`. Game 화면 캡처는 아니며 수동 키보드 플레이와 실제 카메라 렌더링은 미확인이다.

부품 위치와 기둥별 정답은 [Chapter0 배치 안내](배치_Chapter0.md)를 참고한다. 아래는 앞선 프리팹 제작과 런타임 전환 기록이다.

## 기획용 개별 프리팹 후속 정리

2026-10-06 후속 요청에 맞춰 12개 프리팹의 파일·Hierarchy 이름을 한글로 정리했다. 기존 GUID를 모두 유지했고, 기둥·발판·벽에서는 설정 컴포넌트를 먼저 표시한다. 기존 돌 Variant의 상속 컴포넌트 순서는 유지한다. 수신·몸체·Trigger·Sprite·Material 참조와 기본 동작은 유지했다.

관리자는 수동 부품 목록을 필수/선택으로 나눠 표시한다. 기둥은 ID·초기 각도·시계방향 회전 단위·출력 각도·그림을 먼저 표시하고 구조 참조는 고급 항목으로 접었다. 발판·Beam 돌·Beam 벽도 그림 교체를 바로 노출한다. 기존 일반 돌·벽에서는 기본 Inspector를 유지한다. 부품 자동 배치·등록·장착 버튼은 추가하지 않았다. 기획자용 수동 배치 예제와 문제 해결 안내는 `기획자_사용안내.md`에 있다.

| 후속 검증 | 결과 |
| --- | --- |
| 최신 런타임 / Editor 코드 컴파일 | 모두 통과 |
| Unity 실제 프리팹 저장 및 한글 파일 이름 | 12개 성공 |
| 기존 GUID 유지 | 12개 모두 원본과 일치 |
| 한글 프리팹의 BeamPuzzleTests | **36 / 36 통과** |
| 스프라이트·머티리얼·충돌체 참조 / 돌·벽 Variant | 기존 Beam 프리팹 검사 통과 |

열려 있는 원본 편집기를 유지하고 `Temp/BeamDesigner` 독립 복사본에서 검사한 뒤 검증된 프리팹만 원본에 반영했다. 로그는 `Logs/BeamValidation/designer-compile.log`, `designer-prefabs-final.log`, `designer-final-tests.log`, `designer-final-results.xml`이며 GUID 대응은 `designer-prefab-guids.json`이다. 검증 복사본은 종료 후 정리했다. 이 후속 검증에서는 전체 회귀를 재실행하지 않았다.

배치 모드에서 참조와 동작을 검사했다. 실제 화면에서 한글 Inspector의 배치·글자 잘림, 사용자의 맵 Game 화면과 수동 키보드 플레이는 확인하지 않았다. 아래는 앞선 런타임 전환 작업의 검증 기록이다.

원본 프로젝트는 열려 있는 편집기의 잠금이 있어 `Temp/BeamValidationProject`의 독립 복사본에서 Unity 배치 모드로 생성·검증했다. 생성된 12개 프리팹, 임시 PNG, URP 머티리얼과 참조되는 `.meta`를 원본으로 복사했다. 기존 맵 배치, 입력 액션, 레이어 이름과 Physics2D 충돌 행렬은 변경하지 않았다. 사용자 수정 중인 PlayerInteractionController 및 다른 월드/맵 변경은 보존했다.

## 최종 검증

| 항목 | 결과 |
| --- | --- |
| 런타임 Assembly-CSharp 컴파일 | 통과 |
| Editor 및 테스트 Assembly-CSharp-Editor 컴파일 | 통과 |
| Unity 실제 에셋 생성/Import | 12개 프리팹 생성 성공 |
| BeamPuzzleTests | **36 / 36 통과** |
| 기존 MazePlatePuzzleTests | **11 / 11 통과** |
| 기존 CirclePuzzleTests | 11 / 12 통과; 아래 기존 테스트 Assert 1건 |
| 프리팹 스크립트·Sprite·Material·Collider 참조 | Unity에서 실제 프리팹 로드 및 검증 통과 |
| 돌·벽·장애물 원본 재사용 | 기존 Maze 프리팹 Variant 확인 |
| 원본 프로젝트 외부 에셋 GUID | 참조 확인; 머티리얼 Shader GUID는 실제 URP PackageCache와 일치 |
| git diff --check | 통과 |

최종 관련 테스트 묶음은 총 59건 중 58건 통과, 원형 테스트 Assert 1건 실패다. 로그/XML은 프로젝트 `Logs/BeamValidation/`에 있다.

- `final-results.xml`, `final-tests.log`: 최종 Beam·미로·원형 묶음.
- `beam-results.xml`, `beam-tests.log`: Beam 단독 전체 검증.
- `regression-results.xml`, `regression-tests.log`: 전체 회귀 감사.
- `circle-isolated-results.xml`: 원형 Assert를 Beam 검사 없이 독립 재현.
- `build-isolated.log`: Unity 실제 프리팹 생성/Import.
- `compile.log`: 현재 소스 전체 컴파일.

## 확인한 Beam 동작

- 최초 회전 가능 기둥 입력에서 회전과 점등을 함께 처리하고 고정 기둥 입력은 무시한다.
- 실제 Play Mode에서 기존 PlayerInteractionController가 자식 Trigger의 attachedRigidbody에서 기둥을 찾으며, 기존 InputActionReference의 누르기 입력을 전달한다. 홀드 중 반복 회전하지 않고 해제 후 다음 누르기는 회전한다. 포커스 없는 배치 환경 때문에 테스트 안에서만 Manual 입력 주기와 IgnoreFocus를 설정하고 모두 복원한다. 게임 입력 코드에는 새 키나 입력 설정을 추가하지 않았다.
- 기둥별 회전 단위, 임의 출력 각도, 화살표 회전과 항상 켜진 표시를 확인한다.
- 분기, 순환, 중복 진입, 최근접 장애물 차단, 플레이어/일반 Trigger 제외, 다른 퍼즐 기둥의 중계 제외를 확인한다.
- 돌 위치 변경과 벽 파괴에 따라 활성 경로가 추가 입력 없이 갱신된다.
- 기존 SkillController 등록/획득 조건, 돌 홀드, 벽 준비/이동 취소 조건을 재사용하고 완료 시 조작을 잠근다.
- 초기화 발판은 플레이어 복수 Collider가 머무는 동안 재초기화하지 않고 모두 나간 후 새 진입에 다시 작동한다.
- 초기화가 기둥, 돌 위치/회전, 벽, 연결 표시, 진행과 점등을 원래 상태로 복귀시킨다.
- 초기 배치 캡처는 복원 후 Awake를 호출해도 덮어쓰지 않는다. 일반 미로 돌의 기존 Rigidbody 기준 초기화도 보존했다.
- 진행 저장/복원과 초기화 이후 재저장, 전체 부품 배열 재정렬의 ID 대응을 확인한다.
- 실제 ProgressController.SaveAsync/LoadAsync가 고유한 테스트 ES3 파일로 최신 씬 상태를 수집하고 복원한다. 사용자 세이브는 사용하지 않았다.
- 실제 StageFlow Capture/Restore 이벤트 연결을 확인하고 벽의 독립 저장값과 Hierarchy 순서를 바꿔도 Beam 소유 상태로 복원한다.
- 완료 후 광선 유지, 출구 개방, 기둥 충돌체 유지, 돌/벽/발판 잠금, 이벤트 1회 처리를 확인한다.
- 출구를 열면 경로가 달라지는 다른 분기도 최종 선분 그대로 유지·복원하며 복원에서 완료 이벤트를 다시 호출하지 않는다.
- 서로 다른 부모와 두 퍼즐의 독립 조작, 공유 참조/ID/잘못된 Mask/출구 부모 검증을 확인한다.

## 전체 회귀 감사에서 발견한 범위 밖 실패

초기 전체 EditMode 실행은 **276건 중 261건 통과, 15건 실패**였다. 그중 기존 미로 돌 초기화 1건은 이번 확장의 영향으로 확인해 수정했고, 최종 미로 11건을 모두 다시 통과했다. 아래 14건은 Beam 범위 밖 테스트/현재 에셋의 문제로 기록하고 관련 UI, 맵 또는 플레이어 코드를 추가 수정하지 않았다.

| 테스트 | 건수 | 관찰된 실패 |
| --- | --- | --- |
| CirclePuzzleTests.ClearEventCanCompleteAWorldPuzzleOnce | 1 | 기존 테스트의 SendMessage("Awake")에서 Unity ShouldRunBehaviour Assert. 해당 테스트만 실행해도 동일하게 재현 |
| CodexBootstrapPhase8Tests.StartAndSceneReplacementBindWithoutSearching | 1 | 테스트 플레이어에 Rigidbody2D가 없어 기존 PlayerMoveController가 오류 보고 |
| CodexNavigationPhase3Tests | 6 | 동일한 테스트 플레이어 Rigidbody2D 누락 |
| MuseumRelicPhase6PrefabTests.DedicatedUiBlocksInventoryAndRestoresPlayerInput | 2 | 동일한 테스트 플레이어 Rigidbody2D 누락 |
| CodexMuseumInteractionPhase8Tests.ScrollEventMovesRealContentAndReopeningReusesSlots | 1 | 슬롯 기대값 24, 현재 결과 30 |
| CodexUiPhase2PrefabTests.PoolReusesViewsAfterCountShrinksAndGrows | 1 | 슬롯 기대값 5, 현재 결과 30 |
| CodexUiPhase2PrefabTests.RepeatedInitRegistersTabEventOnce | 1 | 현재 UI 프리팹에 기대한 BookRoot/LeftPage/RelicTab 참조 없음 |
| MazeSceneIntegrationTests.VzSceneContainsConnectedMazeDemo | 1 | 테스트가 요구하는 Assets/_Game/Scenes/VZScene.unity 경로 없음 |

전체 회귀를 전부 통과했다고 보고하지 않는다. 최종 수정 후 전체 276건을 다시 실행한 결과는 아니며, 영향을 받은 Beam·미로 및 공용 원형 묶음 59건을 재검증했다. 기존 저장 관련 CodexMuseumPhase8Tests 14건, MuseumRewardRoomPhase7SceneTests 21건, MuseumRelicPhase6Tests 23건, SkillSystemTests 7건, 미로 위험/발판/회복 테스트는 전체 감사에서 통과했다.

## 실제 화면에서 미확인한 항목

배치 모드의 물리·플레이 입력과 에셋 참조는 확인했으나, 사용자가 배치한 실제 맵의 Game 화면에서 GPU 렌더링, 카메라 정렬, 실제 키보드로 걸어 다니는 수동 플레이는 확인하지 않았다. Unity에서 해당 맵에 부품을 배치하고 참조/고유 ID/필요 스킬을 등록한 뒤 확인해야 한다. 임시 PNG 자체는 확인했다.

기존 bool 완료 저장은 과거 회전·돌·벽·최종 경로가 없어서 현재 초기 배치로 표시를 재구성하는 한계가 있다. 상세 배치, 각도, 레이어, 스프라이트 교체, 스킬 및 저장 설정은 README.md를 참고한다.
