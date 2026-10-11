# 미로 퍼즐 맵 배치

## 개별 컨트롤러와 두 종류 발판

- `MazePlatePuzzleController.prefab`: 맵에 배치하는 퍼즐 컨트롤러입니다.
- `MazePlayerPlate.prefab`: 플레이어가 한 번 밟으면 눌림이 유지됩니다. 바위에는 반응하지 않습니다.
- `MazeStonePlate.prefab`: 바위 몸체가 올라가 있는 동안 눌립니다. 플레이어와 바위 상호작용 센서에는 반응하지 않습니다.

기획자가 각 프리팹을 맵에 직접 배치하고 Inspector에서 연결합니다.

1. 컨트롤러의 `Plates`에 클리어 판정에 사용할 발판을 직접 등록합니다.
2. 바위를 사용하는 퍼즐은 `Stones`에 초기화 및 완료 시 잠글 바위를 직접 등록합니다.
   플레이어 발판만 사용하는 퍼즐은 비워 둡니다.
3. 문과 클리어 연출은 아래의 결과 오브젝트와 `On Solved`에 직접 연결합니다.
4. **새 퍼즐 ID 만들기 (복제 후 사용)**를 눌러 퍼즐 고유 ID를 만듭니다.
   직접 등록한 돌의 ID도 갱신하며, 지정한 부품 목록은 바꾸지 않습니다.

발판과 돌의 부모 위치는 자유롭게 구성합니다. 자식으로 넣어도 등록 목록에는 자동으로 추가되지 않습니다.
시작 / 초기화 트리거와 불 / 가시 함정은 각 Inspector의 `Puzzle`에 사용할 컨트롤러를 직접 연결합니다.

`Begin On Plate Press`는 기본으로 켜져 있어 시작 트리거 없이 첫 발판 입력으로 진행됩니다.
모든 등록 발판의 눌림 조건을 충족하면 한 번 클리어되며 `On Solved`가 실행됩니다.
플레이어용 발판 여러 개는 차례로 밟아도 됩니다. 바위용 발판은 완료 시점에 바위가 올라가 있어야 합니다.
문은 `Unsolved Result Object` / `Solved Result Object`에 연결하고, 이벤트만 사용할 때는 비워도 됩니다.
클리어 상태는 기존 씬 상태 저장 시스템으로 저장되며, 복원할 때 보상 이벤트를 다시 실행하지 않습니다.

기존 `MazePuzzlePlate.prefab`은 플레이어와 바위 모두 점유 중에만 반응하는 공용 발판으로 유지합니다.
기존 배치는 Inspector의 `Mode`에서 플레이어 또는 바위 전용으로 바꿀 수 있습니다.
`GridChapter0`의 `MazeController`와 `MazeController (1)`은 모두 플레이어 일회성 발판으로 설정되어 있습니다.

## 테스트용 전체 구성

`TestMazePlatePuzzle.prefab`을 씬에 끌어 놓습니다. 루트에 `MazePlatePuzzleController`가 있고,
발판 2개, 돌 2개, 시작 / 초기화 트리거, 닫힌 문 / 열린 문 표시가 연결되어 있습니다.
게임 시작 시 새로 생성되는 방식이 아니라, 맵에 배치한 인스턴스가 동작합니다.

1. 루트 Inspector의 **새 퍼즐 ID 만들기 (복제 후 사용)** 버튼으로 퍼즐별 고유 ID를 만듭니다.
   씬에 새로 배치하거나 복제할 때 사용하세요. 저장된 완료 상태가 이 ID를 사용하므로,
   출시 후 기존 퍼즐의 ID는 유지합니다. 돌 ID와 Puzzle Id도 함께 갱신됩니다.
2. `Plate_A/B`, `Stone_A/B`, `BeginTrigger`, `ResetTrigger`, `GateClosed/Open` 위치를 맵에 맞게 옮깁니다.
   두 문 표시의 위치는 동일하게 유지합니다. 돌의 시작 위치는 플레이 시작 시 기록됩니다.
3. 발판 / 돌을 추가하거나 삭제하면 컨트롤러의 `Plates` / `Stones` 목록을 직접 수정합니다.
   트리거 / 함정의 `Puzzle` 참조도 각각 직접 연결합니다.
4. 문을 교체하면 `Unsolved Result Object`와 `Solved Result Object`를 새 오브젝트에 연결합니다.
   처음 해결할 때 실행할 보상이나 연출은 `On Solved`에 연결합니다.

플레이어가 파란 `BeginTrigger`에 들어가면 퍼즐이 시작됩니다. 두 발판이 동시에 눌리면 문이 열립니다.
노란 `ResetTrigger`에 들어가면 진행 중인 퍼즐의 돌과 발판이 초기화됩니다.
초기화한 뒤 다시 시작하려면 `BeginTrigger`에 재진입합니다.

## 벽과 함정

- 개별 벽은 `MazeWall.prefab`을 배치합니다.
- 벽 타일맵은 바닥과 별도로 구성하고 `TilemapCollider2D`를 붙입니다.
  벽 Tile의 Collider Type은 `Grid` 또는 `Sprite`, 벽 오브젝트의 Layer는 `MazeBlocker`로 설정합니다.
  `TilemapRenderer`는 Sorting Layer `Object`, Order `0`, Mode `Individual`로 맞춥니다.
- 플레이어 루트는 발의 위치이며 그림은 `Visual` 자식에 있습니다. 벽과 플레이어는 같은 `Object / 0`에서 Y좌표로 정렬됩니다.
  바닥은 `TileMap` 레이어로 두고, 기둥처럼 키가 큰 물체는 바닥 충돌 영역 위치에 `SortingGroup`을 둡니다.
  `GridChapter0`의 기둥은 `Depth - ...` 부모를 이동하면 충돌 영역과 그림이 함께 이동합니다.
- 불 / 가시 함정은 `MazeFireEmitter` / `MazeSpikeTrap` 프리팹을 배치하고 `Puzzle`에 컨트롤러를 직접 연결합니다.
- 사망 시 복구가 필요하면 `MazePuzzleRecovery`를 별도로 추가하고 퍼즐, 체크포인트와 씬 플레이어 참조를 연결합니다.

## 실행 조건

플레이어에는 `UnitPlayer`, `Rigidbody2D`, `Collider2D`, `PlayerMoveController`,
`PlayerInteractionController`가 필요합니다. Interaction Layers에 `Interactable`을 포함합니다.
돌은 상호작용한 플레이어에게서 이동 정보를 받으므로 프리팹에 씬 플레이어를 직접 연결하지 않아도 됩니다.
직접 홀드 API를 호출하는 별도 시스템에서는 이동 컨트롤러를 연결하거나 상호작용 컨텍스트를 전달합니다.

정식 `GameRoot.prefab`을 사용하는 씬에서 플레이어가 `MazePushSkill`을 획득한 뒤
상호작용 입력을 누른 채 돌 방향으로 이동하면 돌을 밀 수 있습니다.
