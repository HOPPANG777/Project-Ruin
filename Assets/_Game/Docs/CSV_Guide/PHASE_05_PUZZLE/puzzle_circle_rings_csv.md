# puzzle_circle_rings.csv 작성 가이드

## 1. 목적

CIRCLE 퍼즐의 링 순서, 패턴 프리팹 인덱스, 세그먼트 수, 초기 회전 오프셋을 정의한다.

## 2. 언제 작성하는가

부모 CIRCLE 퍼즐을 `rows=0,columns=0`으로 만든 뒤 작성한다.

## 3. 정확한 헤더

```csv
puzzle_id,ring_order,pattern_index,segments,initial_offset
```

## 4. 열 규칙

| 열 | 필수 | 형식/검증 |
|---|---:|---|
| `puzzle_id` | 예 | 존재하며 CIRCLE인 퍼즐 |
| `ring_order` | 예 | 0부터 시작해 빠짐없이 연속 |
| `pattern_index` | 예 | 0 이상 정수; 런타임 프리팹 배열 범위 안 |
| `segments` | 예 | 2 이상 정수 |
| `initial_offset` | 예 | 0이 아닌 정수, `-segments < 값 < segments` |

퍼즐마다 링이 최소 하나 있어야 한다.

## 5. CodeList

고정 문자열 enum은 없다. 현재 `All.unity`의 Circle 관리자에서 확인된 링 프리팹 배열은 1개이므로 현재 확정 사용 가능 `pattern_index`는 `0`뿐이다. 장면 배열이 바뀌면 범위도 바뀐다.

## 6. 연결 관계

`puzzles.csv`의 CIRCLE 부모와 장면의 Circle 퍼즐 관리자 `_ringPrefabs` 배열을 함께 참조한다. CSV 로더는 `pattern_index`가 음수가 아닌지만 검사하고 실제 배열 상한은 런타임 구성에 달려 있다.

## 7. 작성 순서

1. 사용할 프리팹 배열 인덱스를 Unity에서 확인한다.
2. 링 수와 세그먼트를 정한다.
3. `ring_order`를 0부터 연속 배정한다.
4. 각 링에 유효한 비영점 오프셋을 준다.
5. 스폰과 정렬/클리어를 PlayMode에서 확인한다.

## 8. 유효 예시

```csv
PUZZLE_CIRCLE_A,0,0,8,1
PUZZLE_CIRCLE_A,1,0,8,-2
```

## 9. 잘못된 예시

| 값 | 이유 |
|---|---|
| 순서 `0,2` | 연속 순서 위반 |
| `segments=1` | 최소 2 |
| `initial_offset=0` | 초기 오프셋은 0 불가 |
| segments 8, offset 8/-8 | 엄격한 범위 밖 |
| 현재 `pattern_index=1` | 확인된 프리팹 배열 범위 밖 |

## 10. 자동/Unity 검증

저장소가 부모 종류, 연속 순서, 숫자 범위와 최소 한 링을 검사한다. 프리팹 배열 상한과 실제 시각 패턴/해결 가능성은 Unity에서 확인한다.

## 11. 수정·삭제 주의

중간 링 삭제 시 뒤의 `ring_order`를 당겨야 한다. Unity 배열 재정렬은 같은 숫자가 다른 프리팹을 가리키게 만들 수 있으므로 CSV와 장면을 함께 검토한다.

## 12. 빠른 체크리스트

- [ ] 순서가 0부터 연속이다.
- [ ] 패턴 인덱스가 장면 배열 범위 안이다.
- [ ] 세그먼트는 2 이상이다.
- [ ] 오프셋은 0이 아니며 범위 안이다.
- [ ] 실제 프리팹 생성과 클리어를 확인했다.

## 13. 관련 문서/코드

- [puzzles.csv](puzzles_csv.md)
- [가이드 정정](../PHASE_00_FOUNDATION/EXISTING_GUIDE_CORRECTIONS.md)
- [PuzzleCsvDefinitionLoader.cs](../../../Scripts/Gameplay/Data/Csv/PuzzleCsvDefinitionLoader.cs)
- [CirclePuzzleManager.cs](../../../Scripts/Puzzle/Circle/CirclePuzzleManager.cs)
- [원본 CSV](../../../Data/Csv/puzzle_circle_rings.csv)
