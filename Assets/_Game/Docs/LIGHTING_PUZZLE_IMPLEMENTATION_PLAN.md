# 조명 퍼즐 시스템 구현 계획

## 1. 문서 목적

이 문서는 `조명 퍼즐 시스템 기획서 (3).docx`의 내용을 현재 Project-Ruin 구조에 맞게 구현하기 위한 기준 문서다.

> 퍼즐 구조물 상호작용 → 조명 퍼즐 창 열기 → 조명 상태 반전 → 모든 조명 ON → 해결 상태 저장 → 문 개방 또는 진행 Flag 기록

구현자는 작업을 시작하기 전에 이 문서와 루트의 [CONVENTIONS.md](../../../CONVENTIONS.md), [폴더 구조 가이드](FOLDER_STRUCTURE_GUIDE.md), [상호작용 제작 가이드](INTERACTION_AUTHORING_GUIDE.md), [진행 상태 시스템 가이드](PROGRESS_SYSTEM_GUIDE.md)를 함께 확인한다.

문서가 충돌할 때는 다음 순서로 판단한다.

1. 사용자가 이번 작업에서 직접 지정한 요구 사항
2. `CONVENTIONS.md`
3. 이 구현 계획
4. 기존 동일 계열 코드
5. 원본 조명 퍼즐 기획서

이 계획은 다음 목표를 동시에 만족해야 한다.

- 처음 보는 사람도 조명 퍼즐의 데이터와 실행 흐름을 이해할 수 있어야 한다.
- 새로운 퍼즐은 C# 수정 없이 CSV 데이터만 추가해 제작할 수 있어야 한다.
- 런타임 중 문자열 파싱, 전체 탐색, 반복 생성과 같은 낭비가 없어야 한다.
- 새 스크립트 수는 최소화하되 서로 다른 책임을 거대한 한 클래스에 몰아넣지 않아야 한다.
- 코드와 주석을 포함한 각 신규 C# 파일은 300줄 이하여야 한다.
- 퍼즐 그래픽을 교체해도 데이터와 게임 규칙은 수정하지 않아야 한다.

---

## 2. 구현 기본 결정

원본 기획서에서 서로 다르거나 확정되지 않은 내용은 MVP에서 다음과 같이 처리한다.

1. 마지막 페이지의 `조명 퍼즐 데이터 작성 예시` 표를 최종 영향 관계 기준으로 사용한다.
2. 5번 조명의 초기 상태는 데이터 표에 적힌 `0`, 즉 OFF를 사용한다.
3. 상호작용 그림에서 5번 조명이 켜진 모습은 예시 이미지 오류로 판단한다.
4. 퍼즐은 실패 상태 없이 `대기 → 진행 → 종료` 순서로만 진행한다.
5. X 버튼으로 닫으면 현재 조작 상태를 저장하지 않고 초기 상태로 되돌린다.
6. 중간 조명 배열은 저장하지 않고 최종 해결 여부만 저장한다.
7. 한번 해결된 퍼즐은 다시 실행할 수 없다.
8. 퍼즐 규칙 데이터와 해결 보상은 분리한다.
9. 공터, 유적 입구, 유적 내부의 차이는 같은 퍼즐 시스템을 사용하되 월드 오브젝트의 해결 결과 설정으로 구분한다.
10. MVP 콘텐츠는 3x3, 4x4, 5x5를 사용한다.
11. 로더는 행과 열의 곱이 25 이하인 직사각형도 처리할 수 있게 작성한다. 별도 분기 없이 가능한 범위이므로 정사각형만 강제하지 않는다.
12. ON/OFF는 CSV에서 각각 `1`과 `0`으로 작성하고 런타임에서는 `bool` 배열로 변환한다.

기획이 변경되면 UI와 클릭 코드에 조건문을 추가하지 않고 CSV 또는 단일 검증 규칙을 수정한다.

---

## 3. 절대 준수 규칙

### 3.1 구조와 의존성

- 새로운 싱글턴을 만들지 않는다.
- 전역 접근은 기존 `GameRoot`만 사용한다.
- 공용 조명 퍼즐 기능은 `LightingPuzzleController`가 담당한다.
- `GameRoot`는 외부에 구현체가 아닌 `ILightingPuzzleService` 계약을 노출한다.
- `GameObject.Find`, `FindObjectOfType`, `FindObjectsByType`를 사용하지 않는다.
- `MonoBehaviour` 참조는 인스펙터 또는 `GameRoot.Init(...)` 흐름으로 주입한다.
- 상속은 사용하지 않고 기존 인터페이스와 구성 방식으로 조립한다.
- 퍼즐 구조물은 기존 `IInteractable`, `IHighlightable`, `ISceneStateContributor`를 재사용한다.
- 플레이어 이동과 상호작용 잠금은 기존 `PlayerMoveController`, `PlayerInteractionController` API를 재사용한다.
- 저장은 기존 `ProgressController`와 Easy Save 3 흐름을 재사용한다.

### 3.2 코드 크기와 파일 수

- 조명 퍼즐 신규 런타임 C#은 최대 4개다.
- 자동 테스트 C#은 최대 1개다.
- 신규 C# 파일은 파일 헤더, 주석, 빈 줄을 포함한 실제 파일 기준 300줄 이하로 유지한다.
- 권장 목표는 파일당 250줄 이하다.
- 250줄을 넘으면 중복 조건과 불필요한 추상화를 먼저 제거한다.
- 300줄을 맞추기 위해 의미 없는 한 줄 래퍼나 DTO 파일을 추가하지 않는다.
- 작은 데이터 타입과 열거형은 가장 관련 있는 파일 안에 둔다.
- 타일마다 별도의 사용자 정의 MonoBehaviour를 만들지 않는다.
- 새 `Manager`, 새 Save 시스템, 새 입력 시스템을 만들지 않는다.
- 300줄을 넘겨야만 구현할 수 있다고 판단되면 구현을 중단하고 구조를 다시 검토한다.

### 3.3 가독성

- 한 메서드는 한 가지 판단 또는 한 가지 상태 변경만 수행한다.
- 중첩 조건문보다 가드절을 사용한다.
- 의미 없는 약어와 매직 스트링을 사용하지 않는다.
- 실패 가능한 조회와 실행은 `TryXxx` 패턴을 우선한다.
- 공개 API는 XML 문서화 주석을 작성한다.
- 주석은 코드가 무엇을 하는지가 아니라 해당 제약과 판단 이유를 설명한다.
- CSV 열 이름과 C# 프로퍼티 이름은 역할을 바로 알 수 있게 작성한다.

### 3.4 성능

- 조명 퍼즐 전용 `Update()`를 만들지 않는다.
- Coroutine을 사용하지 않는다.
- 퍼즐 로직을 위해 UniTask를 사용하지 않는다.
- 클릭 처리 경로에서 LINQ를 사용하지 않는다.
- CSV는 초기화 시 한 번만 파싱한다.
- 문자열 ID를 클릭할 때마다 해석하지 않는다.
- 초기화 후 타일 상태는 `bool[]`, 영향 관계는 `int[][]` 또는 동등한 배열 구조로 관리한다.
- 타일 클릭 시 영향받는 타일만 갱신한다.
- UI를 열고 닫을 때 버튼을 매번 `Instantiate/Destroy`하지 않는다.
- 최대 25개의 버튼을 생성 후 재사용한다.
- 버튼 이벤트는 버튼 생성 시 한 번 연결하고 재사용한다.
- 반복 실행 경로에서 임시 List, 배열, Delegate를 생성하지 않는다.
- DOTween은 추후 시각 연출에만 사용하고 해결 판정 기준으로 사용하지 않는다.

---

## 4. CSV 3개 구조

CSV 파일은 기존 데이터 디렉터리에 저장한다.

```text
Assets/_Game/Data/Csv/
├── lighting_puzzles.csv
├── lighting_puzzle_tiles.csv
└── lighting_puzzle_effects.csv
```

### 4.1 `lighting_puzzles.csv`

퍼즐의 고유 ID와 크기를 정의한다.

```csv
puzzle_id,rows,columns
LIGHT_RUIN_01,3,3
LIGHT_RUIN_02,4,4
LIGHT_RUIN_03,5,5
```

| 열 | 형식 | 의미 |
| --- | --- | --- |
| `puzzle_id` | 문자열 | 퍼즐 정의 고유 ID |
| `rows` | 양의 정수 | 세로 타일 수 |
| `columns` | 양의 정수 | 가로 타일 수 |

규칙:

- `puzzle_id`는 전체 조명 퍼즐에서 유일해야 한다.
- ID는 대문자 `SCREAMING_SNAKE_CASE`를 사용한다.
- `rows × columns`는 25를 초과하지 않는다.
- 보상, 문, 씬 오브젝트 ID는 이 파일에 넣지 않는다.

### 4.2 `lighting_puzzle_tiles.csv`

각 타일의 초기 상태를 정의한다.

```csv
puzzle_id,tile_id,initial_state
LIGHT_RUIN_01,1,0
LIGHT_RUIN_01,2,0
LIGHT_RUIN_01,3,1
LIGHT_RUIN_01,4,0
LIGHT_RUIN_01,5,0
LIGHT_RUIN_01,6,0
LIGHT_RUIN_01,7,1
LIGHT_RUIN_01,8,0
LIGHT_RUIN_01,9,0
```

| 열 | 형식 | 의미 |
| --- | --- | --- |
| `puzzle_id` | 문자열 | 소속 퍼즐 ID |
| `tile_id` | 양의 정수 | 퍼즐 내부 타일 ID |
| `initial_state` | `0` 또는 `1` | 시작 시 OFF 또는 ON |

규칙:

- 타일 ID는 왼쪽 위부터 오른쪽 방향으로 부여한다.
- 3x3 퍼즐은 `1~9`, 4x4는 `1~16`, 5x5는 `1~25`를 사용한다.
- 한 퍼즐 안에서 타일 ID는 중복되거나 빠지면 안 된다.
- `initial_state`는 반드시 `0` 또는 `1`이어야 한다.

3x3 위치 예시:

```text
1 2 3
4 5 6
7 8 9
```

### 4.3 `lighting_puzzle_effects.csv`

타일을 클릭했을 때 함께 반전되는 타일 관계를 정의한다.

```csv
puzzle_id,source_tile_id,target_tile_id
LIGHT_RUIN_01,1,1
LIGHT_RUIN_01,1,2
LIGHT_RUIN_01,1,4
LIGHT_RUIN_01,2,2
LIGHT_RUIN_01,2,3
LIGHT_RUIN_01,2,5
```

| 열 | 형식 | 의미 |
| --- | --- | --- |
| `puzzle_id` | 문자열 | 소속 퍼즐 ID |
| `source_tile_id` | 양의 정수 | 플레이어가 클릭한 타일 |
| `target_tile_id` | 양의 정수 | 상태가 반전되는 타일 |

규칙:

- 하나의 영향 관계를 한 행에 작성한다.
- 같은 `source → target` 관계를 중복 작성하지 않는다.
- 원본과 대상 타일은 해당 퍼즐에 실제로 존재해야 한다.
- 클릭한 타일 자신의 상태도 바뀌므로 모든 `source_tile_id`는 자신의 ID를 대상으로 포함해야 한다.

예:

```text
LIGHT_RUIN_01,1,1
LIGHT_RUIN_01,1,2
LIGHT_RUIN_01,1,4
```

위 세 행의 의미:

> LIGHT_RUIN_01에서 1번 타일을 클릭하면 1번, 2번, 4번 타일의 ON/OFF 상태가 반전된다.

---

## 5. MVP 기준 데이터

원본 기획서 마지막 요약표를 기준으로 첫 번째 3x3 퍼즐을 작성한다.

### 5.1 초기 상태

```text
ID:      1 2 3 4 5 6 7 8 9
상태:    0 0 1 0 0 0 1 0 0
```

### 5.2 영향 관계

| 클릭 타일 | 반전되는 타일 |
| ---: | --- |
| 1 | 1, 2, 4 |
| 2 | 2, 3, 5 |
| 3 | 3, 6 |
| 4 | 1, 4, 5, 7 |
| 5 | 2, 4, 5, 6, 8 |
| 6 | 3, 5, 6, 9 |
| 7 | 4, 7, 8 |
| 8 | 5, 7, 8, 9 |
| 9 | 6, 8, 9 |

위 데이터가 실제로 해결 가능한지는 자동 검증으로 확인한다. 해결 불가능하면 콘텐츠 오류로 처리하고 게임 초기화를 중단한다.

---

## 6. 최소 스크립트 구조

신규 런타임 C#은 다음 4개만 사용한다.

```text
Assets/_Game/Scripts/
├── Gameplay/Puzzles/Lighting/
│   ├── LightingPuzzleCsvLoader.cs
│   ├── LightingPuzzleController.cs
│   └── LightingPuzzleInteractable.cs
├── Presentation/UI/Puzzles/Lighting/
│   └── LightingPuzzleView.cs
└── Tests/Editor/
    └── LightingPuzzleTests.cs
```

### 6.1 책임과 줄 수 예산

| 파일 | 단일 책임 | 목표 | 절대 상한 |
| --- | --- | ---: | ---: |
| `LightingPuzzleCsvLoader.cs` | CSV 3개 파싱, 검증, 실행용 정의 생성 | 240줄 | 300줄 |
| `LightingPuzzleController.cs` | 퍼즐 상태, 클릭, 초기화, 해결 흐름 | 240줄 | 300줄 |
| `LightingPuzzleInteractable.cs` | 월드 상호작용, 해결 결과, 저장 연결 | 180줄 | 250줄 |
| `LightingPuzzleView.cs` | 팝업, 버튼 풀, ON/OFF 시각 갱신 | 200줄 | 280줄 |
| `LightingPuzzleTests.cs` | CSV, 퍼즐 계산, 줄 수 규칙 자동 검증 | 240줄 | 300줄 |

작은 타입은 다음처럼 배치한다.

- `ILightingPuzzleService`, 퍼즐 상태 enum, 실행용 퍼즐 정의는 관련 파일 내부에 둔다.
- 저장용 `isSolved` 상태 타입은 `LightingPuzzleInteractable.cs` 내부에 둔다.
- 별도 `LightingPuzzleTileView.cs`는 만들지 않는다.
- Unity `Button.targetGraphic` 또는 연결된 `Image`로 타일 상태를 표시한다.

### 6.2 수정할 기존 파일

| 기존 파일 | 최소 수정 내용 |
| --- | --- |
| `GameRoot.cs` | 조명 퍼즐 Controller 참조, Service 공개, 초기화 추가 |
| `GameRoot.prefab` | Controller와 View 직렬화 참조 연결 |
| `GameDataBuildPreprocessor.cs` | 조명 퍼즐 CSV 검증 실패 시 빌드 중단 연결 |

다음 파일은 수정하지 않고 기존 공개 API만 사용한다.

- `PlayerInteractionController.cs`
- `PlayerMoveController.cs`
- `ProgressController.cs`
- `SceneStateBridge.cs`
- 기존 `CsvParser.cs`
- 기존 `CsvDefinitionReadUtility.cs`

기존 작업 트리의 사용자 변경은 덮어쓰거나 되돌리지 않는다. 같은 파일을 수정해야 할 때는 현재 변경을 기준으로 최소 패치만 적용한다.

---

## 7. 클래스별 상세 책임

### 7.1 `LightingPuzzleCsvLoader`

담당:

- 기존 `CsvParser`와 `CsvDefinitionReadUtility`를 사용해 CSV 3개를 읽는다.
- CSV의 1부터 시작하는 타일 ID를 0부터 시작하는 배열 인덱스로 한 번 변환한다.
- 퍼즐별 `bool[] initialStates`를 만든다.
- 퍼즐별 `int[][] affectedTileIndices`를 만든다.
- 퍼즐 ID Dictionary를 한 번 생성한다.
- 모든 구조 검증과 해결 가능성 검증을 수행한다.
- 성공 시 읽기 전용 실행 정의를 반환한다.
- 실패 시 파일명, 행, 열, 원인을 포함한 오류를 반환한다.

금지:

- Unity UI 참조
- 플레이어 입력 처리
- 진행 상태 저장
- 클릭 시 CSV 문자열 재파싱

### 7.2 `LightingPuzzleController`

담당:

- `GameRoot`에서 `Init(...)`을 호출해 초기화한다.
- CSV TextAsset 3개를 직렬화 필드로 받는다.
- 동시에 하나의 퍼즐만 열리도록 한다.
- 퍼즐 정의의 초기 상태를 작업용 `bool[]`에 복사한다.
- 타일 클릭 시 미리 계산된 영향 배열만 순회한다.
- 상태가 바뀐 타일만 View에 전달한다.
- 매 클릭 후 모든 타일이 ON인지 검사한다.
- X 버튼 시 상태를 버리고 UI를 닫는다.
- 해결 시 UI를 닫고 요청자에게 결과를 한 번 전달한다.
- UI가 열릴 때 플레이어 이동과 상호작용을 잠근다.
- 닫기, 해결, 비활성화, 예외 경로에서 입력 잠금을 반드시 복원한다.

상태:

```text
LIGHTING_PUZZLE_WAITING = 0
LIGHTING_PUZZLE_IN_PROGRESS
LIGHTING_PUZZLE_FINISHED
```

금지:

- 월드 문이나 보상 구현체 직접 참조
- 타일별 GameObject 탐색
- 프레임 기반 해결 검사
- DOTween 완료 콜백을 게임 상태 기준으로 사용

### 7.3 `LightingPuzzleView`

담당:

- 팝업 루트 표시와 숨김
- X 버튼 입력 전달
- `GridLayoutGroup` 열 수와 셀 크기 적용
- 필요한 버튼 수 확보
- 생성한 버튼 재사용
- 각 버튼의 클릭 인덱스를 Controller에 전달
- ON/OFF Sprite 또는 색상 반영

View는 해결 조건을 판단하지 않는다. `bool` 상태를 받아 표시만 한다.

금지:

- CSV 조회
- Progress Flag 기록
- 퍼즐 해결 판정
- 버튼을 열 때마다 전부 파괴하고 재생성

### 7.4 `LightingPuzzleInteractable`

구현 인터페이스:

```text
IInteractable
IHighlightable
ISceneStateContributor
```

직렬화 설정:

```text
Object Id
Puzzle Id
Solved Flag Id (선택)
Highlight Object
Solved Visual Root 또는 Animator (선택)
On Solved State Applied UnityEvent (선택)
```

담당:

- 플레이어 상호작용 시 조명 퍼즐 Service에 `puzzle_id` 실행을 요청한다.
- 이미 해결되었으면 다시 열지 않는다.
- 해결 콜백을 한 번만 처리한다.
- 해결 상태를 `ISceneStateContributor`로 저장하고 복원한다.
- 선택적 Progress Flag를 기록한다.
- 퀘스트 목표 갱신이 필요하면 기존 Quest Service를 호출한다.
- 문 열림처럼 반복 적용해도 안전한 월드 상태를 복원한다.

UnityEvent에는 반복 지급될 수 있는 아이템 보상을 직접 연결하지 않는다. 복원 시에도 호출될 수 있으므로 문 열림, 오브젝트 표시 변경처럼 반복 적용해도 결과가 같은 작업만 연결한다.

---

## 8. 실행 흐름

### 8.1 게임 초기화

```text
GameRoot.Start
→ LightingPuzzleController.Init
→ CSV 3개 파싱
→ 구조와 참조 검증
→ 해결 가능성 검증
→ 퍼즐 ID Dictionary 생성
→ 초기화 성공
```

CSV가 잘못되면 오류를 무시하고 진행하지 않는다. 파일명과 행을 로그에 출력하고 조명 퍼즐 기능 초기화를 실패 처리한다.

### 8.2 퍼즐 열기

```text
플레이어가 퍼즐 구조물 근처에서 상호작용
→ LightingPuzzleInteractable.Interact
→ 이미 해결됐는지 검사
→ ILightingPuzzleService.TryOpen 호출
→ puzzle_id 조회
→ 초기 상태를 작업 배열에 복사
→ 버튼 풀 활성화 및 상태 표시
→ 플레이어 이동과 상호작용 잠금
→ 진행 상태 전환
```

### 8.3 타일 클릭

```text
버튼 클릭
→ 클릭 인덱스 전달
→ 영향 대상 int[] 조회
→ 각 대상 bool 상태 반전
→ 변경된 타일만 View 갱신
→ 모든 상태가 true인지 검사
```

상태 반전:

```text
false → true
true  → false
```

### 8.4 X 버튼

```text
X 버튼 클릭
→ 작업 배열 폐기
→ 팝업 닫기
→ 플레이어 조작 복원
→ 대기 상태 전환
```

중간 진행은 저장하지 않는다.

### 8.5 해결

```text
모든 조명이 ON
→ 추가 클릭 차단
→ 팝업 닫기
→ 플레이어 조작 복원
→ Interactable 해결 콜백
→ isSolved = true
→ 선택적 Progress Flag 기록
→ 문 또는 월드 상태 적용
→ 종료 상태 유지
```

---

## 9. 저장과 결과 처리

### 9.1 저장 데이터

```json
{
  "isSolved": true
}
```

저장하지 않는 값:

- 현재 ON/OFF 배열
- 클릭 횟수
- 퍼즐 창 열림 여부
- 중간 애니메이션 상태

### 9.2 저장 방식 선택

| 목적 | 방식 |
| --- | --- |
| 같은 씬 재방문 시 해결 상태 유지 | `ISceneStateContributor` |
| 다른 씬, 대화, 퀘스트에서 해결 여부 확인 | Progress Flag |
| 두 조건 모두 필요 | 두 방식 함께 사용 |

### 9.3 결과 연결

| 설치 장소 | 처리 |
| --- | --- |
| 공터 | 해결 상태만 저장, 별도 보상 없음 |
| 유적 입구 | 해결 상태 저장 후 입구 개방 |
| 유적 내부 | 해결 상태 저장 후 다음 문 개방 |

퍼즐 데이터는 문을 직접 알지 않는다. 월드 구조물이 해결 결과를 연결한다.

---

## 10. 데이터 검증 규칙

### 10.1 구조 검증

- 필수 CSV TextAsset 3개가 연결되어 있어야 한다.
- 필수 헤더가 정확히 존재해야 한다.
- 빈 행과 공백 ID를 허용하지 않는다.
- 퍼즐 ID는 중복되면 안 된다.
- 행과 열은 양수여야 한다.
- 전체 타일 수는 25 이하이어야 한다.
- `rows × columns`와 등록된 타일 수가 같아야 한다.
- 타일 ID는 1부터 전체 타일 수까지 끊김 없이 존재해야 한다.
- 초기 상태는 0 또는 1이어야 한다.
- 모든 영향 원본과 대상 ID가 실제 타일이어야 한다.
- 같은 영향 관계를 두 번 작성하면 안 된다.
- 각 타일은 최소 하나 이상의 영향 대상을 가져야 한다.
- 각 타일은 자기 자신을 영향 대상으로 포함해야 한다.

### 10.2 해결 가능성 검증

조명 퍼즐은 클릭할 때 상태를 두 값 사이에서 반전시키므로 0과 1을 사용하는 선형 문제로 검사할 수 있다.

구현 세부 용어를 몰라도 작업자는 다음만 이해하면 된다.

> 로더는 초기 상태와 영향 관계를 바탕으로 모든 조명을 켤 수 있는 클릭 조합이 존재하는지 확인한다. 조합이 없으면 해당 CSV는 잘못된 퍼즐 데이터다.

검증은 초기화 또는 빌드 전에 한 번만 수행하며 플레이 중 클릭 성능에는 영향을 주지 않는다.

### 10.3 오류 메시지 기준

좋은 오류 예:

```text
[LightingPuzzleCsvLoader] source=lighting_puzzle_effects.csv,
record=12, column=target_tile_id,
존재하지 않는 타일입니다. puzzle=LIGHT_RUIN_01, tile=10
```

나쁜 오류 예:

```text
퍼즐 데이터 오류
```

---

## 11. UI와 프리팹 계획

### 11.1 UI 구조

```text
LightingPuzzlePanel
├── Background
├── Title
├── CloseButton
└── TileGrid
```

`TileGrid`에는 `GridLayoutGroup`을 사용한다.

타일 버튼 프리팹:

```text
LightingPuzzleTileButton
├── Button
└── Image 또는 Button.targetGraphic
```

별도 타일 스크립트는 붙이지 않는다.

### 11.2 임시 그래픽

실제 그래픽 리소스가 준비되기 전에는 다음 중 하나를 사용한다.

- ON: 밝은 노란색
- OFF: 어두운 회색

실제 리소스 적용 시 `LightingPuzzleView`의 Sprite 또는 색상 직렬화 참조만 교체한다.

### 11.3 입력 잠금

퍼즐 창이 열리면:

- 플레이어 이동 금지
- 월드 상호작용 탐지와 입력 금지
- 퍼즐 UI 입력만 허용

퍼즐 창이 닫히면 이유와 관계없이 조작을 복원한다.

- X 버튼
- 퍼즐 해결
- Controller 비활성화
- 초기화 실패
- 예외 또는 조기 종료

---

## 12. 구현 단계

### Phase 1. 데이터 기반

- [ ] CSV 파일 3개 생성
- [ ] 첫 번째 3x3 기준 데이터 입력
- [ ] 기존 CSV 파서와 읽기 유틸리티 재사용 확인
- [ ] `LightingPuzzleCsvLoader` 작성
- [ ] 구조 검증 작성
- [ ] 해결 가능성 검증 작성
- [ ] 정상/오류 데이터 자동 테스트 작성

완료 기준:

- 올바른 CSV는 실행용 배열 정의로 변환된다.
- 잘못된 CSV는 파일명과 행을 포함한 오류로 거부된다.
- 클릭 중에는 CSV 문자열을 사용하지 않는다.

### Phase 2. 퍼즐 순수 로직

- [ ] `ILightingPuzzleService` 계약 작성
- [ ] `LightingPuzzleController` 작성
- [ ] 초기 상태 복사 구현
- [ ] 영향 대상 반전 구현
- [ ] 모든 타일 ON 검사 구현
- [ ] X 버튼 초기화 구현
- [ ] 중복 실행과 중복 해결 방지 구현

완료 기준:

- Unity UI 없이도 자동 테스트에서 클릭, 초기화, 해결 판정이 검증된다.

### Phase 3. UI

- [ ] `LightingPuzzleView` 작성
- [ ] 패널과 타일 버튼 프리팹 생성
- [ ] 버튼 풀 구현
- [ ] 3x3, 4x4, 5x5 자동 배치 확인
- [ ] ON/OFF 임시 시각 적용
- [ ] X 버튼 연결
- [ ] 플레이어 조작 잠금과 복원 연결

완료 기준:

- 퍼즐 크기가 달라도 동일한 View와 버튼 프리팹을 사용한다.
- 패널을 반복해서 열어도 버튼 GameObject 수가 계속 증가하지 않는다.

### Phase 4. 월드 상호작용과 저장

- [ ] `LightingPuzzleInteractable` 작성
- [ ] `IInteractable` 연결
- [ ] `IHighlightable` 연결
- [ ] `ISceneStateContributor` 연결
- [ ] 해결 후 재실행 차단
- [ ] 선택적 Progress Flag 처리
- [ ] 문 또는 월드 상태 이벤트 연결
- [ ] 씬 왕복 후 해결 상태 복원 확인

완료 기준:

- 미해결 구조물만 퍼즐을 열 수 있다.
- 해결 후 재접근해도 퍼즐 UI가 열리지 않는다.
- 씬을 나갔다 돌아와도 해결 상태와 문 상태가 유지된다.

### Phase 5. 통합 검증과 정리

- [ ] `GameRoot` 초기화와 Service 노출 연결
- [ ] 빌드 전 CSV 검증 연결
- [ ] 신규 C# 파일 줄 수 자동 검사
- [ ] `dotnet build` 통과
- [ ] Unity Editor 테스트 통과
- [ ] 플레이 모드 검수 완료
- [ ] 문서와 실제 CSV 헤더 일치 확인
- [ ] 불필요한 로그와 임시 코드 제거

---

## 13. 자동 테스트 계획

자동 테스트는 `LightingPuzzleTests.cs` 하나에 모으고 300줄 이하를 유지한다.

### 13.1 CSV 테스트

- [ ] 정상적인 3x3 CSV를 읽는다.
- [ ] 여러 퍼즐 ID를 동시에 읽는다.
- [ ] 필수 CSV 누락을 거부한다.
- [ ] 필수 헤더 누락을 거부한다.
- [ ] 중복 퍼즐 ID를 거부한다.
- [ ] 중복 타일 ID를 거부한다.
- [ ] 빠진 타일 ID를 거부한다.
- [ ] 0과 1 이외의 초기 상태를 거부한다.
- [ ] 존재하지 않는 영향 원본과 대상을 거부한다.
- [ ] 중복 영향 관계를 거부한다.
- [ ] 자기 자신 영향이 빠진 타일을 거부한다.
- [ ] 해결 불가능한 퍼즐을 거부한다.

### 13.2 로직 테스트

- [ ] OFF 타일 클릭 시 ON으로 바뀐다.
- [ ] ON 타일 클릭 시 OFF로 바뀐다.
- [ ] 지정된 영향 대상만 바뀐다.
- [ ] X 버튼 처리 시 초기 상태로 돌아간다.
- [ ] 모든 타일이 ON일 때만 해결된다.
- [ ] 해결 콜백은 한 번만 실행된다.
- [ ] 해결 후 다시 열리지 않는다.

### 13.3 구조 테스트

- [ ] 신규 조명 퍼즐 C# 파일이 300줄 이하인지 검사한다.
- [ ] 금지 API 문자열이 없는지 검사한다.
  - `StartCoroutine`
  - `GameObject.Find`
  - `FindObjectOfType`
  - `FindObjectsByType`
- [ ] 런타임 조명 퍼즐 스크립트가 4개를 넘지 않는지 검사한다.

---

## 14. 플레이 모드 검수

### 상호작용

- [ ] 퍼즐 구조물에 Collider2D가 있다.
- [ ] 구조물 Layer가 Interaction으로 설정되어 있다.
- [ ] 가까이 가면 하이라이트가 켜진다.
- [ ] 범위를 벗어나면 하이라이트가 꺼진다.
- [ ] 상호작용 입력으로 퍼즐 창이 열린다.

### 퍼즐 진행

- [ ] 초기 조명 상태가 CSV와 같다.
- [ ] 각 타일의 영향 관계가 CSV와 같다.
- [ ] ON 타일도 클릭하면 OFF가 된다.
- [ ] 퍼즐 중 캐릭터가 이동하지 않는다.
- [ ] 퍼즐 중 다른 월드 오브젝트와 상호작용할 수 없다.
- [ ] X 버튼을 누르면 진행 상태가 저장되지 않는다.
- [ ] 다시 열면 초기 상태에서 시작한다.

### 해결

- [ ] 모든 조명이 ON일 때 즉시 해결된다.
- [ ] 해결 후 UI가 닫힌다.
- [ ] 플레이어 조작이 복원된다.
- [ ] 해결 결과가 한 번만 적용된다.
- [ ] 해결된 구조물은 다시 활성화되지 않는다.
- [ ] 공터 퍼즐은 별도 보상 없이 종료된다.
- [ ] 유적 입구 퍼즐은 입구를 연다.
- [ ] 유적 내부 퍼즐은 다음 문을 연다.

### 저장

- [ ] 씬을 나갔다 돌아와도 해결 상태가 유지된다.
- [ ] 해결된 문 또는 월드 오브젝트의 시각 상태가 복원된다.
- [ ] Progress Flag를 사용하는 퍼즐은 다른 씬과 퀘스트에서 조회된다.
- [ ] 중간 조명 상태는 저장되지 않는다.

### 반복과 안정성

- [ ] 퍼즐을 열고 닫는 동작을 반복해도 버튼 수가 증가하지 않는다.
- [ ] 퍼즐을 반복해도 중복 버튼 이벤트가 발생하지 않는다.
- [ ] Controller가 비활성화되어도 플레이어 조작 잠금이 남지 않는다.
- [ ] 서로 다른 `puzzle_id`가 같은 시스템에서 정상 작동한다.

---

## 15. 완료 정의

다음 조건을 모두 만족해야 조명 퍼즐 MVP 구현 완료로 판단한다.

- [ ] CSV 3개만 수정해 새로운 퍼즐을 추가할 수 있다.
- [ ] 3x3, 4x4, 5x5 퍼즐이 같은 코드로 작동한다.
- [ ] 구조적으로 올바르지만 해결 불가능한 데이터도 자동 검출한다.
- [ ] 클릭, 닫기, 해결 상태가 기획서 규칙과 일치한다.
- [ ] 해결 전 중간 상태는 저장되지 않는다.
- [ ] 최종 해결 상태는 씬 왕복 후에도 유지된다.
- [ ] 해결 후 재실행할 수 없다.
- [ ] 그래픽 교체 시 CSV와 퍼즐 로직을 수정하지 않는다.
- [ ] 신규 런타임 C#은 4개 이하이다.
- [ ] 자동 테스트 C#은 1개 이하이다.
- [ ] 모든 신규 C# 파일은 실제 파일 기준 300줄 이하이다.
- [ ] 새로운 싱글턴과 Manager가 없다.
- [ ] 조명 퍼즐 전용 Update와 Coroutine이 없다.
- [ ] 클릭 처리 중 의도적인 힙 할당이 없다.
- [ ] `dotnet build`와 관련 자동 테스트가 통과한다.
- [ ] 사용자 작업 중인 기존 변경을 덮어쓰거나 되돌리지 않았다.

---

## 16. 작업 시작 전 체크리스트

구현을 시작하는 작업자는 다음을 먼저 확인한다.

- [ ] 현재 브랜치와 작업 트리 상태를 확인했다.
- [ ] 다른 작업자의 수정 파일을 확인했다.
- [ ] `CONVENTIONS.md`를 다시 읽었다.
- [ ] 기존 CSV 파서와 오류 형식을 확인했다.
- [ ] `GameRoot`의 현재 초기화 순서를 확인했다.
- [ ] `PlayerMoveController`와 `PlayerInteractionController`의 잠금 API를 확인했다.
- [ ] `ISceneStateContributor` 저장·복원 흐름을 확인했다.
- [ ] 원본 기획서 예시가 아니라 이 문서의 MVP 기준 데이터를 사용한다.
- [ ] 먼저 데이터와 순수 로직 테스트를 완성한 뒤 UI를 연결한다.

권장 작업 순서:

```text
CSV와 테스트
→ CSV Loader
→ 순수 클릭 로직
→ UI
→ 월드 상호작용
→ 저장과 문 개방
→ 통합 테스트
→ 그래픽 연출
```

처음부터 UI, 저장, 보상, 애니메이션을 동시에 연결하지 않는다. 가장 작은 검증 가능한 단계부터 완료하고 다음 단계로 진행한다.
