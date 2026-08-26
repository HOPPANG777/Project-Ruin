# UI 퍼즐 프리팹 사용법

## 맵에 퍼즐 놓기

1. 아래 세 프리팹 중 하나를 맵에 끌어 놓습니다.
   - `PipePuzzleInteractable.prefab`
   - `LightPuzzleInteractable.prefab`
   - `CirclePuzzleInteractable.prefab`
2. 프리팹 인스턴스 아래에 실제 맵 오브젝트의 SpriteRenderer를 자식으로 넣습니다.
3. Collider 크기를 오브젝트 크기에 맞춥니다.
4. 다른 CSV 퍼즐을 사용할 때는 `UiPuzzleInteractable > Puzzle Id`만 바꿉니다.
5. 클리어 후 문 열기나 보상은 `On Puzzle Cleared` 이벤트에 연결합니다.

## 공용 UI

`PuzzleUiRoot.prefab`은 파이프, 라이트, 원형 UI를 모두 포함합니다.
생성 도구가 이 프리팹을 `GameRoot.prefab` 아래에 자동으로 설치하므로,
정식 GameRoot 프리팹을 사용하는 씬에서는 별도 UI 연결이 필요하지 않습니다.

## 씬 체크

- Player에 `PlayerInteractionController`가 있어야 합니다.
- PlayerInteractionController의 Interaction Layers에 `Interactable`이 포함되어야 합니다.
- 맵을 직접 실행하려면 정식 `GameRoot.prefab`이 씬에 있어야 합니다.
