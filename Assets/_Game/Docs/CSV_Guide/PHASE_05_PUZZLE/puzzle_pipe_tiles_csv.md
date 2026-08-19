# puzzle_pipe_tiles.csv 작성 가이드

## 1. 목적

PIPE 퍼즐의 모든 좌표에 초기 타일 종류와 시계 방향 90도 회전 횟수를 배치한다.

## 2. 언제 작성하는가

부모 PIPE 퍼즐의 행/열 크기를 확정한 뒤 작성한다.

## 3. 정확한 헤더

```csv
puzzle_id,row,column,tile_type,rotation
```

## 4. 열 규칙

| 열 | 필수 | 형식/검증 |
|---|---:|---|
| `puzzle_id` | 예 | 존재하며 종류가 PIPE인 퍼즐 |
| `row` | 예 | 0 이상, 부모 `rows` 미만 |
| `column` | 예 | 0 이상, 부모 `columns` 미만 |
| `tile_type` | 예 | 7개 CodeList 중 하나 |
| `rotation` | 예 | `0`,`1`,`2`,`3` |

모든 좌표가 정확히 한 번 있어야 하며 행 수는 `rows*columns`이다. START와 END는 각각 정확히 하나다.

## 5. CodeList

`EMPTY`, `START`, `END`, `STRAIGHT`, `CORNER`, `T_JUNCTION`, `CROSS`.

기본 연결 방향: START/END=위, STRAIGHT=위+아래, CORNER=왼쪽+아래, T_JUNCTION=왼쪽+오른쪽+아래, CROSS=전 방향. `rotation` 1 증가마다 연결을 시계 방향 90도 돌린다.

| 종류 | rot 0 | rot 1 | rot 2 | rot 3 |
|---|---|---|---|---|
| START/END | 위 | 오른쪽 | 아래 | 왼쪽 |
| STRAIGHT | 세로 | 가로 | 세로 | 가로 |
| CORNER | 왼+아래 | 위+왼 | 위+오른 | 오른+아래 |
| T_JUNCTION | 왼+오른+아래 | 위+왼+아래 | 위+왼+오른 | 위+오른+아래 |
| CROSS | 전 방향 | 전 방향 | 전 방향 | 전 방향 |

## 6. 연결 관계

`puzzles.csv`의 PIPE 부모를 참조한다. 타일 연결은 인접 좌표의 반대 방향 연결과 맞아야 한다. 플레이어는 EMPTY/START/END를 회전시킬 수 없지만 CSV의 초기 회전은 이 타일에도 적용된다.

## 7. 작성 순서

1. 0 기반 좌표 표를 만든다.
2. 모든 칸에 타일 종류를 배치한다.
3. START/END를 각각 하나만 둔다.
4. 목표 연결 방향에서 회전값을 역산한다.
5. `rows*columns`, 좌표 중복/누락을 확인한다.
6. 초기 상태와 해결 경로를 PlayMode에서 확인한다.

## 8. 유효 예시

```csv
PUZZLE_PIPE_A,0,0,START,1
PUZZLE_PIPE_A,0,1,STRAIGHT,1
PUZZLE_PIPE_A,0,2,END,3
PUZZLE_PIPE_A,1,0,EMPTY,0
```

## 9. 잘못된 예시

| 값 | 이유 |
|---|---|
| `rotation=4` | 허용 범위 초과 |
| 4x4 보드에 `row=4` | 유효 좌표는 0~3 |
| START 두 개 또는 END 없음 | 개수 규칙 위반 |
| `T_JUNCTION` rot 0을 위+왼+오른으로 해석 | 실제 기본은 왼+오른+아래 |
| EMPTY 행 생략 | 모든 좌표 행이 필요함 |

## 10. 자동/Unity 검증

저장소가 부모 종류, 좌표 범위/중복/완전성, 종류/회전, START/END 개수를 검사한다. 연결 경로의 해결 가능성은 자동 검증하지 않으므로 플레이 검증한다.

## 11. 수정·삭제 주의

행 하나를 삭제하면 보드 완전성 오류가 난다. 크기 변경은 전 좌표를 다시 검토한다. 회전 기준은 오래된 문서의 T자 표보다 현재 `PipeData` 구현을 우선한다.

## 12. 빠른 체크리스트

- [ ] 좌표는 0 기반이며 전 칸이 한 번씩 있다.
- [ ] START/END가 각각 하나다.
- [ ] 회전은 0~3이다.
- [ ] 현재 방향표로 T자와 코너를 확인했다.
- [ ] 해결 가능성을 직접 플레이했다.

## 13. 관련 문서/코드

- [puzzles.csv](puzzles_csv.md)
- [가이드 정정](../PHASE_00_FOUNDATION/EXISTING_GUIDE_CORRECTIONS.md)
- [PuzzleCsvDefinitionLoader.cs](../../../Scripts/Gameplay/Data/Csv/PuzzleCsvDefinitionLoader.cs)
- [PipeData.cs](../../../Scripts/Puzzle/Square/PipeData.cs)
- [원본 CSV](../../../Data/Csv/puzzle_pipe_tiles.csv)
