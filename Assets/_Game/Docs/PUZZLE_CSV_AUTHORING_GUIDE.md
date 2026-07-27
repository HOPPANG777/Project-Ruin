# 퍼즐 CSV 작성 가이드

이 문서는 Unity 엔진을 열지 않고 CSV 파일만 수정해 파이프, 라이트, 원형 퍼즐의 배치와 시작 상태를 변경하는 방법을 설명합니다.

일상 작업은 [`Project-Ruin_퍼즐_CSV_작성_가이드.xlsx`](./Project-Ruin_퍼즐_CSV_작성_가이드.xlsx) 에서 표를 채운 뒤 CSV UTF-8로 내보내는 방식을 권장합니다.  
규칙의 이유·오류 원인·협업 범위까지 깊게 보려면 [`PUZZLE_CSV_SYSTEM_DESIGNER_REFERENCE.md`](./PUZZLE_CSV_SYSTEM_DESIGNER_REFERENCE.md) 를 참고하세요.

## 1. 가장 먼저 이해할 흐름

퍼즐 데이터는 다음 순서로 작동합니다.

```text
기획자가 CSV 수정
→ Unity가 다음 실행 때 CSV를 TextAsset으로 가져옴
→ GameDataController가 모든 CSV를 한 번에 읽음
→ PuzzleCsvDefinitionLoader가 값과 참조를 검증함
→ GameDataRepository가 Puzzle ID별 정의를 보관함
→ 각 퍼즐 매니저가 자신의 Puzzle ID를 조회함
→ CSV 내용대로 퍼즐 UI를 생성함
```

따라서 기존처럼 `Stage01.txt`를 수정하거나 Unity Inspector에 타일을 하나씩 입력할 필요가 없습니다.

중요한 의미의 차이는 다음과 같습니다.

- CSV 수정은 Unity가 꺼진 상태에서도 가능합니다.
- 수정한 CSV는 다음에 Unity를 열거나 프로젝트를 실행할 때 자동으로 반영됩니다.
- 이미 만들어 배포한 실행 파일이 외부 CSV를 실시간으로 다시 읽는 방식은 아닙니다.
- 새 프리팹이나 새 씬 배치는 Unity 작업이지만, 이미 연결된 퍼즐의 크기·배치·시작 상태·연동 규칙은 CSV만으로 바꿀 수 있습니다.

## 2. 수정할 파일

퍼즐 데이터는 `Assets/_Game/Data/Csv/` 아래 네 파일만 사용합니다.

| 파일 | 역할 |
| --- | --- |
| `puzzles.csv` | 퍼즐 ID, 퍼즐 종류, 격자 크기 |
| `puzzle_pipe_tiles.csv` | 파이프 퍼즐의 모든 칸과 회전 |
| `puzzle_light_tiles.csv` | 라이트 타일의 시작 상태와 클릭 연동 |
| `puzzle_circle_rings.csv` | 원형 퍼즐의 링 순서, 문양, 회전 단계와 시작 위치 |

네 파일은 부모와 자식 관계입니다.

```text
puzzles.csv의 puzzle_id
├─ puzzle_pipe_tiles.csv의 puzzle_id
├─ puzzle_light_tiles.csv의 puzzle_id
└─ puzzle_circle_rings.csv의 puzzle_id
```

`puzzles.csv`에 없는 ID를 자식 CSV에서 사용하면 로드가 중단됩니다.

## 3. 공통 작성 규칙

1. 첫 번째 줄의 헤더 이름과 순서를 바꾸지 않습니다.
2. 파일 이름을 바꾸지 않습니다.
3. CSV는 UTF-8 형식으로 저장합니다.
4. `puzzle_id`는 영문 대문자로 시작하고 대문자, 숫자, 밑줄만 사용합니다.
5. Boolean 값은 소문자 `true` 또는 `false`만 사용합니다.
6. 행과 열은 0부터 셉니다.
7. 쉼표는 열을 나누므로 한 셀 안의 ID 목록은 `|`로 구분합니다.
8. 같은 ID나 같은 좌표를 두 번 작성하지 않습니다.

예를 들어 3 × 3 격자의 좌표는 다음과 같습니다.

| row \ column | 0 | 1 | 2 |
| ---: | ---: | ---: | ---: |
| 0 | 왼쪽 위 | 위 가운데 | 오른쪽 위 |
| 1 | 왼쪽 가운데 | 정중앙 | 오른쪽 가운데 |
| 2 | 왼쪽 아래 | 아래 가운데 | 오른쪽 아래 |

## 4. puzzles.csv

헤더:

```csv
puzzle_id,puzzle_type,rows,columns
```

| 열 | 의미 | 허용 값 |
| --- | --- | --- |
| `puzzle_id` | 퍼즐 고유 ID | 예: `PIPE_STAGE_01` |
| `puzzle_type` | 퍼즐 종류 | `PIPE`, `LIGHT`, `CIRCLE` |
| `rows` | 격자 행 개수 | PIPE/LIGHT는 1~100, CIRCLE은 0 |
| `columns` | 격자 열 개수 | PIPE/LIGHT는 1~100, CIRCLE은 0 |

예시:

```csv
puzzle_id,puzzle_type,rows,columns
PIPE_STAGE_01,PIPE,4,4
LIGHT_STAGE_01,LIGHT,3,3
CIRCLE_STAGE_01,CIRCLE,0,0
```

원형 퍼즐은 격자를 사용하지 않으므로 `rows`와 `columns`를 모두 0으로 작성합니다.

## 5. puzzle_pipe_tiles.csv

헤더:

```csv
puzzle_id,row,column,tile_type,rotation
```

| 열 | 의미 | 허용 값 |
| --- | --- | --- |
| `puzzle_id` | 부모 파이프 퍼즐 ID | `puzzles.csv`에 존재하는 PIPE ID |
| `row` | 타일 행 | 0부터 `rows - 1` |
| `column` | 타일 열 | 0부터 `columns - 1` |
| `tile_type` | 타일 모양 | 아래 표 참고 |
| `rotation` | 시계 방향 90도 회전 횟수 | 0, 1, 2, 3 |

타일 종류:

| 값 | 의미 |
| --- | --- |
| `EMPTY` | 파이프를 옮길 수 있는 빈 칸 |
| `START` | 물이 시작되는 칸 |
| `END` | 물이 도착해야 하는 칸 |
| `STRAIGHT` | 직선 파이프 |
| `CORNER` | 꺾인 파이프 |
| `T_JUNCTION` | 세 방향 T자 파이프 |
| `CROSS` | 네 방향 십자 파이프 |

예시:

```csv
puzzle_id,row,column,tile_type,rotation
PIPE_STAGE_01,0,0,START,1
PIPE_STAGE_01,0,1,STRAIGHT,1
PIPE_STAGE_01,0,2,CORNER,0
PIPE_STAGE_01,0,3,EMPTY,0
```

필수 조건:

- `rows × columns`만큼 모든 좌표를 정확히 한 번씩 작성해야 합니다.
- `START`와 `END`는 각각 정확히 한 개여야 합니다.
- 회전값 1은 90도, 2는 180도, 3은 270도를 의미합니다.

## 6. puzzle_light_tiles.csv

헤더:

```csv
puzzle_id,tile_id,row,column,is_on,affected_tile_ids
```

| 열 | 의미 | 예시 |
| --- | --- | --- |
| `puzzle_id` | 부모 라이트 퍼즐 ID | `LIGHT_STAGE_01` |
| `tile_id` | 퍼즐 안에서 유일한 1 이상의 번호 | `5` |
| `row` | 타일 행 | `1` |
| `column` | 타일 열 | `1` |
| `is_on` | 처음 열었을 때 켜짐 여부 | `true` 또는 `false` |
| `affected_tile_ids` | 클릭 시 함께 반전될 타일 ID | `2|4|5|6|8` |

예시:

```csv
puzzle_id,tile_id,row,column,is_on,affected_tile_ids
LIGHT_STAGE_01,1,0,0,false,1|2|4
LIGHT_STAGE_01,2,0,1,false,2|3|5
LIGHT_STAGE_01,3,0,2,true,3|6
```

`affected_tile_ids`는 클릭 결과를 그대로 적는 칸입니다. `1|2|4`는 1번 타일을 클릭했을 때 1번, 2번, 4번 타일의 켜짐 상태가 모두 반전된다는 뜻입니다.

필수 조건:

- `rows × columns`만큼 모든 좌표를 정확히 한 번씩 작성해야 합니다.
- `tile_id`는 같은 퍼즐 안에서 중복될 수 없습니다.
- `affected_tile_ids`에 적은 모든 ID가 같은 퍼즐 안에 실제로 존재해야 합니다.
- 같은 영향 ID를 한 셀 안에 두 번 적을 수 없습니다.

## 7. puzzle_circle_rings.csv

헤더:

```csv
puzzle_id,ring_order,pattern_index,segments,initial_offset
```

| 열 | 의미 | 허용 값 |
| --- | --- | --- |
| `puzzle_id` | 부모 원형 퍼즐 ID | `CIRCLE_STAGE_01` |
| `ring_order` | 안쪽부터 세는 링 순서 | 0부터 빈 번호 없이 연속 |
| `pattern_index` | 매니저에 연결된 문양 프리팹 번호 | 0 이상의 정수 |
| `segments` | 한 바퀴를 나눌 회전 단계 수 | 2 이상의 정수 |
| `initial_offset` | 정답 0에서 떨어진 시작 단계 | 0이 아닌 정수, 절댓값은 segments보다 작음 |

예시:

```csv
puzzle_id,ring_order,pattern_index,segments,initial_offset
CIRCLE_STAGE_01,0,0,8,1
CIRCLE_STAGE_01,1,0,8,-2
```

`segments`가 8이면 한 단계는 45도입니다. `initial_offset`이 1이면 45도, -2이면 반대 방향 90도만큼 정답에서 벗어나 시작합니다.

`pattern_index`는 Unity에 미리 연결된 문양 프리팹 목록의 번호입니다. 기존 번호를 선택하는 것은 CSV만으로 가능하지만, 완전히 새로운 그림 프리팹을 목록에 추가하는 작업은 Unity 설정이 필요합니다.

## 8. 기획자 권장 작업 순서

1. Unity와 Excel에서 해당 CSV를 동시에 열어두지 않습니다.
2. 수정할 CSV의 백업 복사본을 만듭니다.
3. `puzzles.csv`의 기존 Puzzle ID와 크기를 확인합니다.
4. 해당 종류의 자식 CSV에서 같은 Puzzle ID 행을 수정합니다.
5. CSV 형식으로 저장합니다. Excel 경고가 나오면 CSV 형식 유지를 선택합니다.
6. Unity를 엽니다.
7. Console에 `[GameDataEditorValidator]` 오류가 없는지 확인합니다.
8. `All` 씬에서 1, 2, 3 키로 파이프, 라이트, 원형 퍼즐을 각각 확인합니다.

## 9. 오류가 났을 때 읽는 법

오류 예시:

```text
[GameDataCsvLoader] source=puzzle_light_tiles, record=4, line=4,
column=affected_tile_ids, message=존재하지 않는 라이트 타일 ID를 참조합니다.
```

| 항목 | 의미 |
| --- | --- |
| `source` | 문제가 있는 CSV 파일 |
| `record` | 헤더를 포함한 CSV 레코드 위치 |
| `line` | 실제 텍스트 줄 번호 |
| `column` | 문제가 있는 열 이름 |
| `message` | 고쳐야 하는 이유 |

오류가 하나라도 있으면 잘못된 일부 데이터만 억지로 실행하지 않고 전체 게임 데이터 초기화를 중단합니다. 잘못된 퍼즐을 조용히 생성하는 것보다 기획자가 정확한 행을 고칠 수 있게 하는 것이 목적입니다.

## 10. 자주 하는 실수

| 실수 | 결과 | 해결 |
| --- | --- | --- |
| 헤더 이름 변경 | 파일 전체 로드 실패 | 첫 줄을 원래대로 복구 |
| `True`, `FALSE`, `1` 사용 | Boolean 오류 | `true`, `false` 사용 |
| LIGHT 퍼즐에 PIPE 자식 행 작성 | 부모 종류 불일치 | 올바른 자식 CSV로 이동 |
| 같은 좌표 두 번 작성 | 중복 좌표 오류 | 중복 행 삭제 또는 좌표 수정 |
| 격자 칸 하나 누락 | 타일 수 오류 | 모든 좌표가 있는지 확인 |
| 라이트 영향 ID에 없는 타일 입력 | 참조 오류 | 실제 `tile_id`로 수정 |
| 파이프 START 두 개 입력 | 시작 타일 개수 오류 | START를 한 개만 유지 |
| 원형 `initial_offset`을 0으로 입력 | 처음부터 정답 상태 | 0이 아닌 값 사용 |
| 쉼표로 영향 ID 구분 | CSV 열 개수 오류 | 쉼표 대신 `|` 사용 |

## 11. CSV만으로 가능한 것과 Unity가 필요한 것

| 작업 | CSV만으로 가능 |
| --- | ---: |
| 기존 파이프 퍼즐 크기와 전체 배치 변경 | 가능 |
| 파이프 시작 회전 변경 | 가능 |
| 라이트 시작 상태와 클릭 연동 변경 | 가능 |
| 원형 링 개수, 단계, 시작 회전 변경 | 가능 |
| 미리 연결된 원형 문양 선택 | 가능 |
| 새 그림 프리팹 제작과 문양 목록 등록 | Unity 필요 |
| 새 팝업을 씬에 배치하고 새 Puzzle ID 연결 | Unity 필요 |

이 경계를 기억하면 기획 데이터와 Unity 에셋 설정을 혼동하지 않고 작업할 수 있습니다.
