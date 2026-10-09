# 유적 1·2층 배치와 편집 안내

`Assets/_Game/Prefabs/Map/Grid_1.prefab`에 유적 부품과 문·계단을 추가했다. 작업은 2026-10-08 최종 검증했다. 기존 마을, Point1~5, 마을↔유적 이동, 타일맵·벽·지형·기존 저장 설정은 보존했다. `CONVENTIONS.md`는 수정하지 않았다.

## 배치 찾기

Prefab Mode에서 다음 그룹을 선택하고 Scene 뷰의 Frame Selected를 사용한다.

- `Chapter_1/Chapter1_ruin/Ruin1F_Puzzles`
- `Chapter_1/Chapter1_ruin2f/Ruin2F_Puzzles`

부품의 Transform, Collider 크기, SpriteRenderer와 기존 Beam Inspector 설정을 직접 수정할 수 있다. 아래 좌표는 해당 층 타일맵의 **로컬 좌표**다. 맵 루트의 월드 좌표와 다르다.

| 부품 | 현재 위치 | 데이터 / 작동 | 여는 문 |
| --- | --- | --- | --- |
| a 메인 홀 | (2385, 358.5) | CIRCLE_STAGE_01 + 별자리 설정, 기존 상호작용 키 | 3번 방 입장문 |
| b 왼쪽 접근부 | (2364, 358) | PIPE_STAGE_06, 기존 상호작용 키 | 1번 방 입장문 |
| c 노란 장치 | (2348, 377) | MazePressurePlate, 최초 밟기 | 왼쪽 보상 통로 문 |
| d 오른쪽 접근부 | (2412, 358) | PIPE_STAGE_07, 기존 상호작용 키 | 2번 방 입장문 |
| e 노란 장치 | (2427, 384) | MazePressurePlate, 최초 밟기 | 오른쪽 보상 문 |
| f 노란 장치 | (2367, 403) | MazePressurePlate, 최초 밟기 | 2층 계단 문 |
| g 빛줄기 | 아래 기둥 표 참조 | 기존 끝 기둥 도달 판정 | 2층 노란 장치 접근문 |
| 2층 노란 장치 | (787, -635) | MazePressurePlate, 최초 밟기 | 보상방 문 |

a·b·d의 풀이 순서는 독립적이다. 2층 보상방의 조건은 **g → 접근문 → 노란 발판 → 보상방 문**이다. b·d나 c·e의 완료를 추가 조건으로 검사하지 않는다. 노란 장치에는 별도 미로 완료 판정이나 상호작용 키를 추가하지 않았다.

PIPE_STAGE_06/07과 모든 기존 CSV를 변경하지 않았다. PIPE_STAGE_07은 빈 4번째 행·열을 포함한 원본 4×4다. 새로운 목적지 씬·보상 지급·스킬 획득·미로 벽은 추가하지 않았다.

## a 별자리 편집

1. `a_메인홀_별자리`의 UiPuzzleInteractable에서 **원형 별자리 (선택) / Constellation Settings**를 확인한다.
2. 연결된 `Assets/_Game/Data/Ruin/a_ConstellationSettings.asset`를 선택한다. Inspector의 한글 안내와 오류를 확인한다.
3. **Outer To Inner** 목록은 바깥→안쪽 순서다. 해당 Ring의 Pattern Prefab을 더블클릭해 Prefab Mode로 연다.
4. CirclePattern의 Symbols 목록에서 **Icon / Constellation Id**를 편집한다. Color는 기존 색상 퍼즐용이며 a의 정답에 사용하지 않는다.
5. Icon이 가리키는 기존 Image에서 **Source Image**를 교체한다. RectTransform의 Anchor·Position·Rotation으로 위치와 그림의 각도를 편집한다. 방사 정렬 방향은 링 중심에서 Image의 실제 위치를 향하는 방향이며, Image 자체의 Z 회전은 그림의 기울기다.
6. 문양을 추가할 때 기존 Image를 복제하고 Symbols 목록에 등록한다. 삭제할 때 Image 오브젝트와 Symbols 항목을 함께 제거한다. 코드에 문양 개수를 고정하지 않았다.
7. 설정 에셋으로 돌아와 Answer Id, Alignment Degrees, Initial Rotation Degrees, Rotation Step Degrees를 편집하고 저장한다.

| 바깥부터 | 내부 CSV ring_order | 기본 정답 ID | 방향 | 초기 회전 | 스냅 간격 | 편집 프리팹 |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 3 | SCORPIO (전갈) | 90° / 12시 | -45° | 45° | a_Ring_0_SCORPIO |
| 2 | 2 | VIRGO (처녀) | 90° / 12시 | 135° | 45° | a_Ring_1_VIRGO |
| 3 | 1 | TAURUS (황소) | 90° / 12시 | -90° | 45° | a_Ring_2_TAURUS |
| 4 | 0 | CANCER (게자리) | 90° / 12시 | 45° | 45° | a_Ring_3_CANCER |

각 프리팹은 기존 CirclePattern_3/2/1/0의 문양 수·그림·배치를 보존한 **편집 시작점**이다. 첫 등록 문양에 기본 정답 ID를 명시했고 나머지 `DRAFT_...` ID와 그림의 최종 대응은 기획자가 구성한다. 그림 의미를 파일 번호나 색으로 자동 추정하는 코드는 없다. 문양 프리팹 자체를 변경하지 않은 기존 원형 퍼즐은 기존 색상 판정을 유지한다.

0°는 3시, 90°는 12시다. 회전 간격은 360°를 정수 단계로 나누어야 하며 초기 회전은 그 간격의 정수배다. 기존 UiCircleComponent의 드래그·회전·스냅을 그대로 사용한다.

필수 ID·Icon·Image 그림 누락과 잘못된 회전 설정은 Inspector에서 오류로 표시한다. 실제 Anchor·링 크기로 정답 방향에 스냅 가능한지도 UI 초기화에서 검사한다. 오류가 있으면 Console에 원인을 남기고 클리어 판정을 중단한다. 설정을 바꾼 뒤에는 별도 검증 세이브로 UI를 다시 열어 확인한다. 이미 완료한 배치는 기존 저장 상태를 유지한다.

## g 빛줄기 편집

`g_빛줄기_관리자`와 개별 `g_기둥_0~6`을 사용한다. 기존 Beam Inspector의 위치·그림·초기 각도·출력 각도·회전 단위를 수정한다.

| 순서 | 기존 부품 | 위치 | 초기 회전 | 출력 각도 |
| --- | --- | --- | --- | --- |
| 0 | 시작 기둥, 고정 | (686, -637) | 0° | 다음 기둥 방향 |
| 1 | 2방향 기둥 | (705, -641) | 45° | 다음 기둥 방향 |
| 2 | 2방향 기둥 | (726, -633) | 45° | 다음 기둥 방향 |
| 3 | 2방향 기둥 | (712, -660) | 45° | 다음 기둥 방향 |
| 4 | 2방향 기둥 | (743, -651) | 45° | 다음 기둥 방향 |
| 5 | 2방향 기둥 | (763, -637) | 45° | 다음 기둥 방향 |
| 6 | 끝 기둥, 고정 | (772, -660) | 0° | 없음 |

출력 각도는 각 다음 기둥까지의 실제 벡터로 설정했다. 현재 해법은 중간 기둥마다 기존 입력을 한 번 눌러 시계방향 45° 회전하는 것이다. 첫 입력에서 회전과 점등이 함께 수행된다. 관리자의 최대 광선 거리는 45, 광선 충돌 마스크는 기존 Interactable/MazeBlocker다. 수신 충돌체·몸체·상호작용 Trigger를 기존 개별 부품 참조로 연결했다.

중앙 고정 장애물은 (735, -641), 현재 몸체 크기는 1.2×18이다. 광선이 이 장애물에 실제로 차단되는 것을 검증했다. 모든 기둥 통과·특정 순서를 강제하는 추가 판정은 없다. 클리어 후 최종 광선이 유지되고 회전·초기화는 기존 규칙대로 거절된다.

청록색 기존 초기화 발판은 (750, -633), 계단 도착은 (755, -635)다. 실제 2×3 플레이어 몸체로 도착 시 발판과 겹치지 않음을 검사했다. 노란 장치와 색·기존 그림이 다르다. 밀기 돌이나 부술 수 있는 벽은 추가하지 않았다.

## 문·계단·보상 표시

닫힌 문은 기존 Beam 출구 장애물 그림과 몸체를 사용한다. 최초 개방과 저장 복원을 구분하며, 열리면 문 오브젝트와 이동 충돌이 함께 꺼진다. 문은 조작대·발판·광선·이동 패드의 부모가 아니다. 최초 결과용 기존 UnityEvent는 복원 시 다시 실행하지 않는다.

1층 계단 감지는 (2413.5, 423.5), 2층 계단 감지는 (765, -623)다. 양쪽 PositionTransitionPad는 서로의 도착 Transform을 참조한다. 도착은 1층 (2395, 400), 2층 (755, -635)이며 기존 StageFlow/SceneTransitionPresenter의 완전 암전 경계에서 이동한다. 별도 페이드나 지연시간을 만들지 않았다. 기존 마을↔유적 입구·도착·복귀 연결은 그대로다.

초록 보상 표시는 c (2348, 386.5), e (2435.5, 384), f (2412, 405), 2층 (665, -658)이다. 파란 최종 보상 표시는 2층 (801, -635)다. 기존 맵 Sprite를 사용한 Transform/SpriteRenderer만 있으며 충돌·획득·지급 컴포넌트는 없다. 기획자가 기존 그림으로 교체하거나 표시 크기를 조정할 수 있다.

## 저장

실제 SceneCatalog의 Chapter1 StageNumber는 **3**, UI ChapterIndex는 **1**이다. 기존 GameRoot의 SceneFlowBootstrapModule → UiPuzzleStateController 주입을 재사용했다. GameRoot·Bootstrap·카탈로그·ProgressController·SceneStateBridge는 이번 작업에서 변경하지 않았다.

| 배치 | 데이터 ID와 독립된 저장 ID |
| --- | --- |
| a / b / d | CH1_RUIN_1F_A / CH1_RUIN_1F_B / CH1_RUIN_1F_D |
| c / e / f | CH1_RUIN_C_PLATE / CH1_RUIN_E_PLATE / CH1_RUIN_F_PLATE |
| g | CH1_RUIN_2F_G |
| 2층 노란 발판 | CH1_RUIN_2_PLATE |

처음 클리어·작동하면 contributor 상태를 먼저 Progress 메모리에 기록하고 기존 SaveAsync를 요청한다. 저장 중 추가 완료도 기존 Dirty/version 처리와 연결 객체의 요청 플래그로 이어서 저장한다. 실패·취소는 성공으로 보고하지 않는다.

미완료 g의 회전·점등은 기존 Beam JSON으로 Capture/Apply한다. SceneStateBridge와 함께 기존 ProgressController.BeforeSaveRequested에서 연결된 배치의 StageNumber 3으로 상태를 수집한다. Chapter1 씬을 직접 실행해 StageFlow의 현재 번호가 아직 -1인 경우에도 기존 저장 요청에 진행 상태가 포함된다. 회전 입력마다 파일 저장을 추가하지 않았다. 완료 g의 최종 선분과 잠금도 같은 형식으로 복원한다.

발판은 Awake 전 복원 준비와 false/true 최종 상태 복원을 지원한다. 반복 Init/Load 및 연결 객체·배치 파괴에서 이전 구독을 해제한다. 저장 ID를 변경하면 기존 배치 저장과 연결되지 않으므로, 이름이나 위치를 바꾼다는 이유만으로 ID를 바꾸지 않는다. 새로운 배치를 복제할 때는 별도 ID를 지정한다.

## 실제 검증과 남은 제한

Unity 6000.3.14f1의 분리된 프로젝트 복사본에서 **101개 검사 중 97개 통과, 4개 실패**했다. 최종 XML은 `Logs/RuinValidation/reviewed-results.xml`, 로그는 `reviewed-tests.log`, 실제 ES3 증거 파일 20개와 보존 감사 결과는 `ES3Evidence/`, `final-audit.json`에 있다. 테스트 파일·에셋이 최종 작업 파일과 바이트 단위로 같은지 확인했다.

통과한 항목:

- a의 기본 ID·12시 판정, 바깥↔안쪽 순서, 문양 수·ID·그림·위치·각도·정답·방향·초기값·스냅 간격 편집 후 실제 기존 드래그 해결.
- 잘못된 정답·참조·회전·도달 불가능 방향 거부, 기존 원형 색상 판정과 다른 기존 UI 퍼즐 회귀.
- b·d 원본 CSV의 실제 드래그·회전 해법, 공용 UI의 기존 Interact 진입점과 챕터 표시.
- 입장문 독립 연결, 실제 플레이어 Trigger의 노란 발판 최초 한 번 작동, 반복 복원의 무효과.
- g의 7개 부품 경로·중앙 장애물 차단·실제 초기화 발판·완료 후 조작 잠금과 모든 중간 기둥 입력 범위까지의 통행.
- g 접근문과 노란 발판 보상문 분리, b·d/c·e가 미완료인 상태의 2층 문 흐름.
- 실제 StageFlow/SceneTransitionPresenter에서 alpha=1 이후 이동, 화면 복귀, 양층 도착 충돌 및 자동 재이동 방지.
- 실제 ES3 즉시 저장·연속 완료·파일 재로드·재방문·미완료 g 진행 복원(직접 씬 실행의 StageFlow 번호 -1 포함), 취소·쓰기 실패, 재초기화·파괴의 구독 정리.
- 현재 지형에서 a/b/d 입장→각 노란 장치, e 장치→보상, 2층 접근·보상 통로의 닫힘/열림 통행.

통과하지 못한 통행은 그대로 남겼다:

| 구간 | 확인한 제한 |
| --- | --- |
| c 장치 → 왼쪽 보상 표시 | 문을 꺼도 현재 좁은 연결 통로를 실제 2×3 몸체로 통과하지 못한다. 보상 표시 중심은 충돌 없는 상부 공간으로 조정했다. |
| f 장치 → 1층 계단 감지 | 문을 꺼도 현재 계단의 경사진 연결 구간을 실제 몸체로 통과하지 못한다. 감지 중심은 몸체가 겹치지 않는 계단 상부 공간으로 조정했다. |
| 기존 마을 Point1 / Point3 | 이전 마을 작업에서 확인된 두 통행 실패가 동일하게 남아 있다. |

PD가 아직 작성 중인 c·e·f 미로의 최종 벽과 접근 동선은 미완성이다. 현재 빈 방 통행 검사가 최종 미로 통과를 보증하지 않는다. c/f 연결 통행과 완성된 미로는 PD 지형 작업 후 재검증해야 한다. 이 제한을 해결하려고 기존 벽·타일·지형·플레이어 몸체를 변경하지 않았다.

자동 플레이 모드에서 실제 물리·UI 조작 API·페이드 alpha·ES3 I/O를 검증했다. 사람이 원본 Chapter1 씬에서 키보드·마우스로 플레이하는 검증과 화면 렌더의 시각 QA는 수행하지 않았다. Profiler CPU/GC 수치는 측정하지 않았다. 새 런타임 상태 연결에는 Update/FixedUpdate/LateUpdate나 회전별 파일 저장을 추가하지 않았다.

## 변경 파일과 줄 수

기존 파일 수정:

- `Assets/_Game/Prefabs/Map/Grid_1.prefab`: 두 층 Transform의 자식 그룹 참조와 신규 부품 133개 직렬화 블록만 추가했다. 다른 모든 기존 직렬화 블록은 보존했다.
- `Assets/_Game/Scripts/Puzzle/Circle/CirclePuzzleManager.cs` — 200줄: 선택적 별자리 설정과 기존 색상 판정.
- `Assets/_Game/Scripts/Puzzle/Circle/CirclePattern.cs` — 108줄: 명시적 ID·그림 참조 및 실제 스냅 도달 검사.
- `Assets/_Game/Scripts/Puzzle/Interaction/UiPuzzleInteractable.cs` — 162줄: 배치의 별자리 설정을 기존 공용 UI에 전달.
- `Assets/_Game/Scripts/Puzzle/Interaction/UiPuzzleSceneState.cs` — 111줄: 저장 연결 파괴 시 기존 완료 구독 해제.
- `Assets/_Game/Scripts/Puzzle/Interaction/UiPuzzleStateController.cs` — 167줄: 기존 Beam/발판 최초 이벤트·복원·저장 요청 및 직접 씬 실행의 상태 수집 연결.
- `Assets/_Game/Scripts/Gameplay/World/MazePressurePlate.cs` — 186줄: 최초 작동 이벤트, Awake 전 준비, 무효과 최종 상태 복원.
- `Assets/_Game/Scripts/Tests/Editor/Chapter1VillageLayoutTests.cs` — 190줄: 마을 검사 범위를 기존 마을 그룹에 한정했다.

신규 클래스와 각 .meta:

| 파일 | 줄 수 |
| --- | --- |
| Scripts/Puzzle/Circle/CircleConstellationSettingsSO.cs | 104 |
| Scripts/Puzzle/Interaction/WorldPuzzleSceneState.cs | 86 |
| Scripts/Editor/CircleConstellationSettingsEditor.cs | 32 |
| Scripts/Tests/Editor/RuinPuzzleDataTests.cs | 195 |
| Scripts/Tests/Editor/RuinLayoutTests.cs | 164 |
| Scripts/Tests/Editor/RuinPersistenceTests.cs | 154 |
| Scripts/Tests/Editor/RuinRuntimeTests.cs | 168 |
| Scripts/Tests/Editor/RuinAccessTests.cs | 129 |

위 표의 Scripts 경로는 `Assets/_Game/` 아래다. 신규 에셋은 `Assets/_Game/Data/Ruin/a_ConstellationSettings.asset`, `a_Ring_0_SCORPIO.prefab`, `a_Ring_1_VIRGO.prefab`, `a_Ring_2_TAURUS.prefab`, `a_Ring_3_CANCER.prefab`과 각각의 .meta, Ruin 폴더 .meta다. 신규 그림은 없다.

모든 신규·수정 C# 파일과 클래스는 200줄 이내이며 **foreach를 사용하지 않는다**. 기존 긴 Beam·Progress·SceneStateBridge 및 관련 없는 시스템은 수정하지 않았다. 보존 감사에서 기존 GUID/.meta·CSV·마을·씬·지형과 다른 사용자 변경의 유지도 확인했다.
