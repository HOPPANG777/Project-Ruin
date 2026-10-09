# 맵에서 직접 조작하는 Beam 퍼즐

**기획자의 배치 순서는 [기획자 사용 안내](기획자_사용안내.md)를 먼저 참고한다.** 개별 프리팹 12개를 제공하며, 한글 Inspector에서 ID·목록·각도·그림을 편집한다. 부품 배치와 목록 연결은 수동으로 한다. 고급 구조 설정은 접어서 표시한다. 아래는 개발자를 위한 세부 설정 설명이다. 기존 영문 필드 이름도 병기했다.

첫 번째 참고 그림에 맞춘 `GridChapter0/BeamPuzzlePoint`의 실제 배치와 정답은 [Chapter0 배치 안내](배치_Chapter0.md)를 참고한다.

기존 `BeamPuzzleController`, `BeamPillarView`, `BeamPropagation`을 월드용으로 전환했다. 팝업, UI Image, 마우스 클릭, 이동/상호작용 입력 잠금은 사용하지 않는다. 파이프·원형·라이트 퍼즐의 공용 팝업 동작은 유지한다. CSV 및 CSV 로더는 만들지 않았다.

## 제공 부품

`Assets/_Game/Prefabs/Puzzle/Beam/`의 **12개 개별 프리팹**을 필요한 위치에 배치한다. 완성된 퍼즐 세트 프리팹은 제공하지 않는다.

| 프리팹 | 용도 / 기본값 |
| --- | --- |
| Beam_관리자 | 명시 참조를 등록할 관리자, 기본 광선 프리팹 연결 |
| Beam_시작 기둥 | 시작 기둥, 출력 1개, 고정 |
| Beam_2방향 기둥 | 2방향 중계, 출력 1개, 회전 가능 |
| Beam_4방향 기둥 | 4방향 중계, 출력 3개, 회전 가능 |
| Beam_8방향 기둥 | 8방향 중계, 출력 7개, 회전 가능 |
| Beam_끝 기둥 | 끝 기둥, 출력 0개, 고정 |
| Beam_초기화 발판 | 플레이어가 새로 밟을 때 한 번 초기화 |
| Beam_광선 표시 | URP 2D Unlit Material이 연결된 광선 LineRenderer |
| Beam_고정 장애물 | 기존 MazeWall의 Variant, 고정 차단 몸체 |
| Beam_출구 장애물 | 기존 MazeWall의 Variant, 완료 시 비활성화할 출구 |
| Beam_밀 수 있는 돌 | 기존 MazePushableStone의 Variant, 기존 밀기 스킬·홀드 동작 재사용 |
| Beam_부술 수 있는 벽 | 기존 MazeWall의 Variant에 기존 BreakableWall 추가 |

`Art/`에 종류별 몸체, 화살표, 발판, 장애물 임시 PNG와 `BeamUnlit.mat`이 있다. 돌은 기존 임시 스프라이트를 그대로 재사용한다. `Tools > Project Ruin > Beam > 개별 부품 프리팹 만들기`는 누락된 에셋만 생성하며 기존 아트나 프리팹과 맵을 덮어쓰지 않는다. 제공 에셋은 이미 생성되어 있어 배치 시 이 메뉴를 실행할 필요가 없다. 이전 영문 파일의 GUID는 한글 파일로 유지했고, Editor 코드에서 경로가 필요하면 `BeamPrefabBuilder.GetPrefabPath("BeamTwoWayPillar")`처럼 기존 부품 키를 사용한다.

## 배치와 관리자 등록

1. 관리자, 시작·끝 각 하나, 필요한 중간 기둥과 초기화 발판을 맵에 배치한다. 부품은 서로 다른 부모 아래에 있어도 된다. 공통 부모, 거리 기반 탐색, 자식 자동 수집은 없다.
2. 관리자의 `Object Id`를 스테이지 내 고유한 영구 ID로 바꾼다. 템플릿 이름을 그대로 여러 퍼즐에 쓰지 않는다.
3. `Pillars`, `Reset Pads`, `Stones`, `Walls`, `Fixed Obstacles`, `Exit Obstacles`에 **이 퍼즐에 속한 부품만 직접 드래그**한다. 선택적으로 쓰지 않는 돌·벽·고정 장애물·출구 배열은 길이 0으로 둔다. 초기화 발판은 하나 이상 필요하다.
4. 돌과 벽의 `Object Id`는 관리자 내부에서 서로 겹치지 않게 지정한다. 기둥의 `Settings > Id`도 관리자 내부에서 고유해야 한다. 다른 퍼즐에서 기둥 ID를 재사용할 수 있지만 관리자 저장 ID는 중복할 수 없다.
5. 관리자 Inspector의 오류 안내 또는 `Tools > Project Ruin > Beam > 현재 맵 설정 검증`을 확인한다. 같은 부품을 여러 관리자에 등록할 수 없다.
6. `On Solved`에 통로 개방 이후 호출할 보상 등을 연결한다. 실제 최초 해결에서만 호출하며 저장 복원에서는 호출하지 않는다.

관리자/광선 표시/기둥/발판/돌/벽의 부모를 출구로 지정하면 완료 때 퍼즐 자체가 꺼질 수 있으므로 검증에서 차단한다. 출구에는 통로를 막는 독립 몸체 오브젝트만 등록한다. `Beam Parent`는 생성된 광선 표시의 부모일 뿐 다른 부품의 공통 부모가 아니다. 비워 두면 관리자 아래에 생성한다.

## 기둥 각도와 표시 설정

`Settings`는 Unity 객체 참조를 포함하지 않는 `BeamPillarSettings`다. 향후 외부 데이터 공급은 같은 설정 생성자와 초기화 전 `Configure`를 사용할 수 있다.

- `Type`: 시작 / 2방향 / 4방향 / 8방향 / 끝을 선택한다.
- `Is Rotatable`: 시작·끝도 원하면 회전 가능하게 바꿀 수 있다.
- `Initial Rotation Degrees`: **월드 XY 기준** 초기 각도. 0도 오른쪽, 90도 위쪽이다. 부모 회전과 관계없이 표시와 광선이 이 각도를 따른다.
- `Rotation Step Degrees`: 양수 시계방향 회전량. 기본 45도이고 기둥마다 다르게 지정할 수 있다.
- `Output Angles Degrees`: 현재 회전에 더하는 **출력별 독립 로컬 각도**. 예를 들어 4방향 기둥에 `[7, 83, 211]`을 설정할 수 있다. 균등 간격은 필수가 아니다.

시작은 1개 이상, 2방향은 1개, 4방향은 3개, 8방향은 7개, 끝은 0개의 출력을 요구한다. 출력 수를 바꾸면 `Output Arrows` 배열도 같은 수로 구성한다. 기본 프리팹은 이미 연결되어 있다. 화살표는 오른쪽을 향하도록 제작한 Sprite를 사용하고 각 출력 Renderer의 Transform 원점은 기둥 중심에 둔다.

몸체 아트는 `Body Renderer`, 화살표는 `Output Arrows`의 **Sprite만 교체**한다. 발판과 장애물도 자식 SpriteRenderer에서 교체한다. 아트 크기는 표시 자식의 Scale로 조정하고 물리 루트, `Body Collider`, `Receiver Collider`, `Interaction Trigger`를 다시 만들지 않는다. `Direction View`에는 물리 충돌체를 넣지 않는다. 몸체의 연결/끊김 색상과 화살표 색상은 기둥 Inspector에서 바꾼다.

관리자에서 광선 색상·굵기·Z·정렬 레이어·순서를 조정한다. 현재 Quality 설정의 URP에 맞는 `Universal Render Pipeline/2D/Sprite-Unlit-Default` Material을 제공한다. 머티리얼 변경은 `Beam_광선 표시`의 LineRenderer에서 한다.

## 레이어, 충돌과 플레이어 입력

기존 레이어와 Physics2D 충돌 행렬은 변경하지 않았다.

- 기둥 몸체/수신 및 장애물: 기존 `MazeBlocker`(9).
- 주변 상호작용 Trigger: 기존 `Interactable`(8).
- 초기화 발판 Trigger: `Ignore Raycast`(2).
- 관리자 광선 Mask 기본값: `Interactable | MazeBlocker`(768). 새로운 레이어로 옮기면 수신과 모든 장애물 몸체를 Mask에 포함한다.
- 돌 Variant의 이동 차단 Mask는 기존 `MazeBlocker | MazeHazard`에 `Interactable`을 추가해 기존 월드와 기둥을 함께 검사한다.

기둥 루트는 Static Rigidbody2D, 비 Trigger 몸체 충돌체와 BeamPillarView를 갖는다. 자식의 주변 Trigger는 루트 Rigidbody에 붙어 기존 `PlayerInteractionController`가 `attachedRigidbody`에서 `IInteractable`을 찾는다. 기본 몸체와 수신 참조는 같은 Collider를 쓴다. 수신면을 따로 지정해도 이 관리자의 몸체는 수신면 앞에서 광선을 잘못 차단하지 않으며, 지정한 수신면만 중계 대상을 활성화한다. 다른 관리자의 기둥은 차단 대상일 뿐 이 퍼즐에서 활성화되지 않는다.

광선 계산은 일반 Trigger, 발판 Trigger, PlayerInteractionController/UnitPlayer가 있는 플레이어 몸체를 제외한다. 따라서 상호작용 Trigger의 레이어가 광선 Mask에 있어도 차단하지 않는다. Trigger를 `Receiver Collider`나 고정 장애물 몸체로 잘못 등록한 설정은 검증한다. 완료 후에도 기둥 몸체/수신 Collider는 계속 켜져 있다.

플레이어의 기존 `Interact Action`을 그대로 연결하고 `Interaction Layers`에 `Interactable`을 포함한다. 코드에서 키를 하드코딩하지 않는다. `WasPressedThisFrame()` 경로로 입력 한 번에 한 단계만 회전하므로 홀드해도 반복 회전하지 않는다. 플레이어 Rigidbody, 이동 입력, 기존 상호작용 반경과 Physics2D 충돌 설정을 정상 연결해야 한다.

초기화 발판은 PlayerInteractionController가 있는 플레이어를 감지한다. 플레이어 몸체가 Trigger 진입 이벤트를 발생시키도록 Rigidbody2D와 해당 레이어 접촉 설정을 유지한다. 여러 몸체 Collider도 진입당 한 번만 초기화하고, 모두 나갔다 다시 들어오면 다시 초기화한다.

## 돌과 벽 스킬

돌은 기존 `MAZE_PUSH_STONE` SkillDataSO와 기존 스킬 획득 조건을 유지한다. 이 돌 스킬도 사용하는 GameRoot의 SkillController 목록에 등록하고 기존 획득 흐름으로 획득해야 한다. 벽 Variant에는 제공한 `BeamBreakSkill.asset`(`BEAM_BREAK_WALL`, 누르기·장애물 파괴)을 기본 연결했다. 기존 프로젝트에는 재사용할 벽 부수기 SkillDataSO/프리팹이 없어서 기존 벽 컴포넌트와 MazeWall Variant를 조합했다. 실제 게임에서는 기존 벽 스킬을 연결하거나 이 에셋을 GameRoot의 SkillController 목록에 등록하고 기존 획득 흐름으로 지급해야 한다. 자동으로 스킬을 지급하지 않는다.

벽은 기존 준비 시간과 이동 취소 조건을 유지하며 상호작용한 플레이어 이동 컨텍스트를 받는다. 활성 광선은 미완료 동안 LateUpdate에서 재계산하므로 돌이 이동하거나 벽이 부서지면 추가 기둥 입력 없이 갱신된다. 완료는 등록 돌의 홀드와 벽의 준비를 취소하고 잠근다. 돌 Rigidbody도 고정하여 플레이어 접촉으로 밀리지 않게 한다. Beam에 등록하지 않은 기존 돌·벽의 스킬 동작과 저장은 유지한다.

## 초기화, 완료, 저장

광선은 처음 꺼져 있다. 회전 가능한 기둥의 첫 입력이 **한 단계 회전과 점등을 함께** 수행한다. 발판 초기화는 모든 기둥 초기 각도, 돌의 씬 원래 월드 위치/회전, 벽의 초기 상태, 연결 색상, 진행 상태를 복원하고 소등한다. 저장 상태를 불러온 후에도 이 원래 스냅샷은 덮어쓰지 않는다.

끝에 도달하면 최종 선분을 고정하고 등록한 출구를 비활성화한다. 기둥·돌·벽·발판을 통한 추가 조작은 막는다. 광선 계산은 순환/중복 진입에서도 각 기둥의 출력을 한 번만 처리한다.

`ISceneStateContributor`의 버전 1 JSON은 진행 상태, 점등, 기둥 ID별 회전/연결, 돌 ID별 월드 위치/회전, 벽 ID별 파괴, 완료 상태의 최종 선분을 저장한다. 배열을 재정렬해도 ID로 복원한다. 맵을 변경해 저장된 ID가 없어지면 해당 항목은 무시하고, 새 부품은 원래 배치 상태를 유지한다. 저장 ID를 바꾸면 이전 저장과 연결되지 않으므로 운영 중인 ID는 유지한다.

`SceneStateBridge`는 기존 StageFlow Capture/Restore를 유지하며 부품 소유권 준비를 모든 Apply보다 먼저 수행한다. Beam에 등록한 벽은 독립 벽 저장에서 제외되며 과거 벽의 독립 저장값도 무시한다. 따라서 Hierarchy/Awake 순서와 무관하게 Beam 상태가 유일한 소유자다. 일반 `ProgressController.SaveAsync()`도 Dirty 검사 **전에** 현재 스테이지 contributor를 수집한다. 실제 `LoadAsync()` 이후에는 현재 스테이지 상태를 복원한다. GameRoot의 SceneFlowBootstrapModule에 SceneStateBridge를 연결하고 StageFlow의 CurrentStageNumber가 유효해야 한다. 사용자 맵이나 GameRoot 참조는 이 작업에서 변경하지 않았다.

초기화 직후 Capture도 빈 문자열 대신 명시적인 대기 JSON을 반환하여 SceneStateBridge가 이전 진행값을 덮어쓴다. 완료 상태 Apply는 보상/완료 이벤트를 호출하지 않는다. 미완료 Apply의 경로 계산도 클리어 이벤트를 억제한다. 이후 정상 월드 갱신에서 실제 해결 여부를 판단한다.

기존 `true` 저장은 완료·출구·잠금을 복원하고 현재 씬 초기 배치로 광선을 재구성한다. 과거 형식에는 회전/돌/벽/최종 경로가 없으므로 과거 정답 경로를 정확히 복원할 수 없다. `false`는 초기 대기로 취급한다. 다음 저장부터 전체 JSON이 기록된다. 잘못된 버전, 중복 저장 항목 ID, NaN/Infinity 상태는 적용 전에 거부한다.

## 검증

Unity Test Runner의 EditMode에서 `BeamPuzzleTests`를 실행한다. 테스트는 제공 프리팹을 사용하며 실제 ES3 검증은 고유한 `Logs/BeamValidation/` 임시 파일만 사용한다. 테스트를 위해 기존 맵 배치를 바꾸지 않는다. 자동 검증 결과와 실행 환경 제한은 같은 폴더의 `VALIDATION.md`에 기록한다.
