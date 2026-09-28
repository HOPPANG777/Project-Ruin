# UI 퍼즐 프리팹 사용법

## 맵에 퍼즐 놓기

1. 아래 세 프리팹 중 하나를 맵에 끌어 놓습니다.
   - `PipePuzzleInteractable.prefab`
   - `LightPuzzleInteractable.prefab`
   - `CirclePuzzleInteractable.prefab`
2. 프리팹 인스턴스 아래에 실제 맵 오브젝트의 SpriteRenderer를 자식으로 넣습니다.
3. Collider 크기를 오브젝트 크기에 맞춥니다.
4. 다른 CSV 퍼즐을 사용할 때는 `UiPuzzleInteractable > Puzzle Id`만 바꿉니다.
   배경과 조각의 챕터를 바꾸려면 `Chapter Index`를 0~3으로 설정합니다.
5. 클리어 후 문 열기나 보상은 `On Puzzle Cleared` 이벤트에 연결합니다.

## 공용 UI

`PuzzleUiRoot.prefab`은 파이프, 라이트, 원형 UI를 모두 포함합니다.
생성 도구가 이 프리팹을 `GameRoot.prefab` 아래에 자동으로 설치하므로,
정식 GameRoot 프리팹을 사용하는 씬에서는 별도 UI 연결이 필요하지 않습니다.

## 원형 퍼즐 정답과 새 퍼즐 설정

- 모든 링에서 같은 색 문양을 위쪽 세모 아래에 일렬로 맞추면 클리어됩니다. 다른 방향에 맞추면 클리어되지 않으며, 문양의 모양은 달라도 됩니다.
- 현재 가장 안쪽 링은 노란색 하나이므로 노란색으로 맞춥니다. 바깥 링의 노란색 두 개 중 어느 것을 사용해도 됩니다.
- 새 퍼즐은 `puzzles.csv`와 `puzzle_circle_rings.csv`에 ID와 링 구성을 등록하고 맵 오브젝트의 `Puzzle Id`에 연결합니다. `initial_offset`은 시작 회전값이며 정답값이 아닙니다.
- 새 문양 프리팹에는 `CirclePattern`을 추가하고 `Symbols`에 각 문양의 RectTransform과 색을 연결합니다. 모든 링에 공통 색이 있고 회전 단계(`segments`)로 세모 아래에 맞출 수 있어야 합니다. 현재 문양 구성은 `segments=8`을 사용합니다.
- 공용 프리팹의 `Target Pointer`에는 세모가 연결되어 있습니다. 별도 UI도 세모를 연결하면 그 방향을 사용하며, 연결하지 않으면 링 부모의 위쪽을 기준으로 판정합니다.
- 숫자키 테스트와 맵 오브젝트는 같은 `UiCirclePuzzleManager` 판정을 사용합니다. 개별 UI를 만들 때도 이 매니저의 `Ring Prefabs`에 사용할 문양 프리팹을 모두 등록해야 합니다.

## 씬 실행

Unity 에디터의 Play 모드에서는 Game 화면에 포커스를 준 뒤 숫자 `1`·`2`·`3`으로 파이프·라이트·원형 퍼즐을 열고, `R`로 현재 퍼즐을 다시 시작합니다. 같은 숫자를 다시 누르면 팝업을 닫습니다. 테스트용 퍼즐 ID와 챕터는 `PuzzleUiRoot > PuzzleTestSelector`에서 설정합니다.

- Player에 `PlayerInteractionController`가 있어야 합니다.
- PlayerInteractionController의 Interaction Layers에 `Interactable`이 포함되어야 합니다.
- 맵을 직접 실행하려면 정식 `GameRoot.prefab`이 씬에 있어야 합니다.
