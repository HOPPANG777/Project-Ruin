# codex_entries.csv

[도감·박물관 가이드](README.md) · [술 재료](codex_ingredients_csv.md) · [기술](technologies_csv.md)

> 상태: 계획 계약. 이 파일과 런타임 로더는 아직 존재하지 않습니다.

## 1. 이 CSV의 목적

도감에 어떤 유물, 술, 기술을 어떤 순서로 표시할지 정의합니다. 한 행은 **도감 항목 한 개**입니다. 이름·설명·아이콘은 원본 Item 또는 Technology에서 가져오므로 중복 작성하지 않습니다.

## 2. 언제 작성하는가

새 유물·술·기술을 도감에 노출하거나 카테고리, 표시 순서, 챕터 그룹을 바꿀 때 작성합니다. 아이템 또는 기술 원본을 먼저 만들어야 합니다.

## 3. 정확한 헤더

```csv
entry_id,category,display_order,source_id,chapter
```

## 4. 열별 작성 규칙

| 열 이름 | 쉬운 설명 | 필수 여부 | 형식 | 입력 가능 값 | 참조 대상 | 올바른 예 |
| --- | --- | --- | --- | --- | --- | --- |
| `entry_id` | 도감 항목 자체의 고유 ID | 필수 | 시스템 ID | 전체 도감에서 고유 | 재료·도감 조회 | `CODEX_EXAMPLE_RELIC` |
| `category` | 도감에서 표시할 분류 | 필수 | 고정 CodeList | `RELIC`, `ALCOHOL`, `TECHNOLOGY` | UI 레이아웃·검증 | `RELIC` |
| `display_order` | 카테고리 안의 표시 순서 | 필수 | 0 이상 정수 | 같은 카테고리에서 중복 금지 | UI 정렬 | `0` |
| `source_id` | 실제 표시 정보를 가져올 원본 ID | 필수 | 시스템 ID | 카테고리별 동적 목록 | `items.csv` 또는 `technologies.csv` | `ITEM_EXAMPLE_RELIC` |
| `chapter` | 획득·표시 그룹 챕터 | 필수 | 0 이상 정수 | 원본 챕터 정책과 일치 | 필터·박물관 연결 | `1` |

## 5. CodeList 및 입력 가능 값

| `category` | `source_id` 대상 | 추가 검증 | 화면 위치 |
| --- | --- | --- | --- |
| `RELIC` | `items.csv.item_id` | `item_type=ITEM_RELIC_COLLECTIBLE` | 왼쪽 1P 유물 |
| `ALCOHOL` | `items.csv.item_id` | `item_type=ITEM_ALCOHOL_COLLECTIBLE` | 왼쪽 1P 술 |
| `TECHNOLOGY` | `technologies.csv.technology_id` | 기술이 실제로 존재 | 오른쪽 2P 기술 |

- `display_order`는 같은 카테고리 안에서 고유해야 하며 0부터 연속 작성을 권장합니다.
- 동일한 `source_id`를 같은 카테고리에 두 번 등록할 수 없습니다.
- `ALCOHOL` 항목은 `codex_ingredients.csv`에 한 개 이상의 재료가 있어야 합니다.
- 잠금 이름 `???`와 잠금 설명 `?`는 UI 공통 정책이므로 CSV 열이 아닙니다.

## 6. 다른 CSV와의 연결

- 참조함: 유물·술 → `items.csv`, 기술 → `technologies.csv`
- 참조받음: `codex_ingredients.entry_id`
- 상태 연결: 유물·술은 `source_id` 최초 획득 Flag, 기술은 기존 기술 획득 상태
- 박물관: 유물 전체 종류 수와 발견 완료 조건은 `category=RELIC` 행에서 계산합니다.

## 7. 작성 순서

1. Item 또는 Technology 원본을 먼저 작성합니다.
2. 고유한 `CODEX_...` ID를 만듭니다.
3. 원본 타입에 맞는 카테고리를 선택합니다.
4. 카테고리 안에서 표시 순서를 정합니다.
5. 획득 챕터를 입력합니다.
6. 술 항목이면 재료 자식 행을 작성합니다.
7. 카테고리별 중복 순서와 중복 원본을 검사합니다.

## 8. 올바른 예

아래 ID는 문서 형식 설명용이며 현재 실제 CSV에는 없습니다.

```csv
entry_id,category,display_order,source_id,chapter
CODEX_EXAMPLE_RELIC,RELIC,0,ITEM_EXAMPLE_RELIC,1
CODEX_EXAMPLE_ALCOHOL,ALCOHOL,0,ITEM_EXAMPLE_ALCOHOL,1
CODEX_EXAMPLE_TECH,TECHNOLOGY,0,TECH_EXAMPLE_GLIDE,1
```

## 9. 잘못된 예

| 잘못된 값 | 이유 |
| --- | --- |
| `category=ITEM_RELIC` | 허용 CodeList가 아님 |
| `RELIC`인데 술 Item ID | 카테고리와 ItemType 불일치 |
| `TECHNOLOGY`인데 Item ID | 기술 CSV를 참조해야 함 |
| 같은 카테고리의 `display_order=0` 두 행 | 표시 순서 중복 |
| 같은 술 `source_id` 두 행 | 동일 원본 중복 연결 |
| `chapter=-1` | 0 이상이어야 함 |

## 10. 자동 또는 Unity 검증 항목

Phase 1 구현 시 정확한 헤더, 필수값, ID 형식·중복, 카테고리 3종, 표시 순서, 원본 존재, ItemType 일치, 동일 원본 중복, 술 재료 존재를 오류로 검사합니다. `icon_id`는 원본 데이터의 Unity Catalog 연결을 경고로 검사합니다. 현재는 자동 검증이 없습니다.

## 11. 수정과 삭제 시 주의사항

`entry_id`는 술 재료의 부모이므로 변경 시 모든 자식 행을 함께 바꿉니다. 발견 저장 Key는 유물·술의 안정적인 `source_id`를 사용하고 `entry_id`와 표시 이름을 저장 Key로 중복 사용하지 않습니다. 유물 행을 삭제하면 “모든 유물 발견/전시”의 전체 개수와 최종 방 조건이 바뀝니다. 원본 Item을 삭제하기 전에 이 파일의 참조를 먼저 정리합니다.

## 12. 빠른 체크리스트

- [ ] 정확한 5열 헤더인가?
- [ ] Entry ID와 카테고리별 순서가 고유한가?
- [ ] 카테고리가 3종 중 하나인가?
- [ ] 원본 ID가 존재하고 ItemType이 맞는가?
- [ ] 술 항목에 재료가 한 개 이상 연결되는가?
- [ ] 챕터가 0 이상인가?
- [ ] 유물 총종류 변경이 최종 조건에 미치는 영향을 확인했는가?

## 13. 관련 문서와 코드

- [items.csv](../PHASE_01_LOCALIZATION_ITEMS/items_csv.md)
- [술 재료 CSV](codex_ingredients_csv.md)
- [기술 CSV](technologies_csv.md)
- [교차 참조 지도](../PHASE_07_REFERENCE/CROSS_REFERENCE_MAP.md)
- [Progress 저장 가이드](../../ES3_PROGRESS_SAVE_GUIDE.md)
