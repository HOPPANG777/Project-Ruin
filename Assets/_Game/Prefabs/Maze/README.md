# 미로 퍼즐 맵 배치

`MazePlatePuzzle.prefab`을 씬에 끌어 놓습니다. 루트에 `MazePlatePuzzleController`가 있고,
발판 2개, 돌 2개, 시작 / 초기화 트리거, 닫힌 문 / 열린 문 표시가 연결되어 있습니다.
게임 시작 시 새로 생성되는 방식이 아니라, 맵에 배치한 인스턴스가 동작합니다.

1. 루트 Inspector의 **새 퍼즐 ID 만들기 (복제 후 사용)** 버튼으로 퍼즐별 고유 ID를 만듭니다.
   씬에 새로 배치하거나 복제할 때 사용하세요. 저장된 완료 상태가 이 ID를 사용하므로,
   출시 후 기존 퍼즐의 ID는 유지합니다. 돌 ID와 Puzzle Id도 함께 갱신됩니다.
2. `Plate_A/B`, `Stone_A/B`, `BeginTrigger`, `ResetTrigger`, `GateClosed/Open` 위치를 맵에 맞게 옮깁니다.
   두 문 표시의 위치는 동일하게 유지합니다. 돌의 시작 위치는 플레이 시작 시 기록됩니다.
3. 발판 / 돌을 추가하거나 삭제한 뒤 루트의 **자식 발판 / 돌 / 트리거 / 함정 연결** 버튼을 누릅니다.
   루트 아래에 있는 이 퍼즐 소속 부품만 연결하며, 중첩된 다른 미로 퍼즐은 제외합니다.
4. 문을 교체하면 `Unsolved Result Object`와 `Solved Result Object`를 새 오브젝트에 연결합니다.
   처음 해결할 때 실행할 보상이나 연출은 `On Solved`에 연결합니다.

플레이어가 파란 `BeginTrigger`에 들어가면 퍼즐이 시작됩니다. 두 발판이 동시에 눌리면 문이 열립니다.
노란 `ResetTrigger`에 들어가면 진행 중인 퍼즐의 돌과 발판이 초기화됩니다.
초기화한 뒤 다시 시작하려면 `BeginTrigger`에 재진입합니다.

## 벽과 함정

- 개별 벽은 `MazeWall.prefab`을 배치합니다.
- 벽 타일맵은 루트 Grid를 선택한 뒤 Hierarchy 우클릭 → `Project Ruin / Maze / Wall Tilemap`으로 만듭니다.
  Tile의 Collider Type은 `Grid` 또는 `Sprite`로 설정합니다.
- 불 / 가시 함정은 `MazeFireEmitter` / `MazeSpikeTrap` 프리팹을 루트 아래에 배치하고 자식 연결 버튼을 누릅니다.
- 사망 시 복구가 필요하면 `MazePuzzleRecovery`를 별도로 추가하고 퍼즐, 체크포인트와 씬 플레이어 참조를 연결합니다.

## 실행 조건

플레이어에는 `UnitPlayer`, `Rigidbody2D`, `Collider2D`, `PlayerMoveController`,
`PlayerInteractionController`가 필요합니다. Interaction Layers에 `Interactable`을 포함합니다.
돌은 상호작용한 플레이어에게서 이동 정보를 받으므로 프리팹에 씬 플레이어를 직접 연결하지 않아도 됩니다.
직접 홀드 API를 호출하는 별도 시스템에서는 이동 컨트롤러를 연결하거나 상호작용 컨텍스트를 전달합니다.

정식 `GameRoot.prefab`을 사용하는 씬에서 플레이어가 `MazePushSkill`을 획득한 뒤
상호작용 입력을 누른 채 돌 방향으로 이동하면 돌을 밀 수 있습니다.

프리팹이 삭제된 경우 `Tools / Project Ruin / Maze / Create Plate Puzzle Prefab`으로 기본 구성을 다시 생성할 수 있습니다.
이미 프리팹이 있으면 이 메뉴는 해당 자산을 선택합니다.
