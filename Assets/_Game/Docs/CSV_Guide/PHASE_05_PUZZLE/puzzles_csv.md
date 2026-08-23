# puzzles.csv 작성 가이드

## 1. 목적

퍼즐의 최상위 ID, 종류, 보드 크기를 선언한다.

## 2. 언제 작성하는가

세부 타일/링 행보다 먼저 작성한다. 퍼즐 시스템을 사용할 때 최소 한 행이 필요하다.

## 3. 정확한 헤더

```csv
puzzle_id,puzzle_type,rows,columns
```

## 4. 열 규칙

| 열 | 필수 | 형식/검증 |
|---|---:|---|
| `puzzle_id` | 예 | 시스템 ID, 전역 유일 |
| `puzzle_type` | 예 | `PIPE`, `LIGHT`, `CIRCLE` |
| `rows` | 예 | PIPE/LIGHT는 1~100, CIRCLE은 0 |
| `columns` | 예 | PIPE/LIGHT는 1~100, CIRCLE은 0 |

## 5. CodeList

`puzzle_type`: `PIPE`, `LIGHT`, `CIRCLE`. 숫자는 부호 없는 정수다.

## 6. 연결 관계

종류에 맞는 세부 CSV 한 종류만 자식으로 사용한다. PIPE는 `puzzle_pipe_tiles.csv`, LIGHT는 `puzzle_light_tiles.csv`, CIRCLE은 `puzzle_circle_rings.csv`다. 네 퍼즐 TextAsset은 런타임 필수 입력이다.

## 7. 작성 순서

1. 퍼즐 종류와 ID를 정한다.
2. 그리드형이면 행/열 수를 확정한다.
3. 종류에 맞는 세부 CSV를 작성한다.
4. 저장소 검증을 통과시킨다.
5. 장면 프리팹 수용 범위와 실제 해결 가능성을 플레이 검증한다.

## 8. 유효 예시

```csv
PUZZLE_PIPE_A,PIPE,4,4
PUZZLE_LIGHT_A,LIGHT,3,3
PUZZLE_CIRCLE_A,CIRCLE,0,0
```

## 9. 잘못된 예시

| 값 | 이유 |
|---|---|
| `puzzle_type=LIGHTS` | CodeList에 없음 |
| `PIPE,0,4` | 그리드 크기는 1~100 |
| `CIRCLE,1,1` | CIRCLE은 두 값 모두 0 |
| 같은 `puzzle_id` 두 행 | 전역 중복 |

## 10. 자동/Unity 검증

로더가 종류/범위를, 저장소가 ID 중복·종류별 자식 구조를 검사한다. 해결 가능성은 자동 검증하지 않는다. 각 퍼즐을 열고 조작해 실제 클리어 이벤트까지 확인한다.

## 11. 수정·삭제 주의

크기 변경 시 기존 모든 타일 좌표와 개수가 새 `rows*columns`에 맞아야 한다. 종류 변경은 기존 세부 행을 제거하고 새 종류의 자식을 완전하게 만들어야 한다. ID 변경은 장면/상호작용/저장 키에 영향을 줄 수 있다.

## 12. 빠른 체크리스트

- [ ] 종류가 세 값 중 하나다.
- [ ] 크기가 종류별 범위에 맞다.
- [ ] 종류에 맞는 자식 데이터만 있다.
- [ ] 모든 좌표/링 검증을 통과한다.
- [ ] 실제 해결 가능성을 플레이했다.

## 13. 관련 문서/코드

- [퍼즐 가독성 문서](LIGHT_PUZZLE_READABLE_MAPPING.md)
- [검증/문제 해결](../PHASE_08_WORKFLOW_QA/VALIDATION_AND_TROUBLESHOOTING.md)
- [PuzzleCsvDefinitionLoader.cs](../../../Scripts/Gameplay/Data/Csv/PuzzleCsvDefinitionLoader.cs)
- [GameDataRepository.cs](../../../Scripts/Gameplay/Data/Repository/GameDataRepository.cs)
- [원본 CSV](../../../Data/Csv/puzzles.csv)
