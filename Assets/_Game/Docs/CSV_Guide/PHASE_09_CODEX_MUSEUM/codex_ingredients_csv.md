# codex_ingredients.csv

[도감·박물관 가이드](README.md) · [도감 항목](codex_entries_csv.md) · [items.csv](../PHASE_01_LOCALIZATION_ITEMS/items_csv.md)

> 상태: 계획 계약. 이 파일과 런타임 로더는 아직 존재하지 않습니다.

## 1. 이 CSV의 목적

술 도감 한 항목에 표시할 제조 재료와 재료 위치를 정의합니다. 한 행은 **술 한 종류의 재료 한 개**입니다. 술 하나에 재료가 여러 개이므로 부모·자식 CSV로 분리합니다.

## 2. 언제 작성하는가

술 도감 항목을 만든 뒤 제조 재료, 표시 순서 또는 획득 위치 안내를 작성할 때 사용합니다. 재료 Item과 위치 Localization Key를 먼저 준비합니다.

## 3. 정확한 헤더

```csv
entry_id,ingredient_order,item_id,location_text_key
```

## 4. 열별 작성 규칙

| 열 이름 | 쉬운 설명 | 필수 여부 | 형식 | 입력 가능 값 | 참조 대상 | 올바른 예 |
| --- | --- | --- | --- | --- | --- | --- |
| `entry_id` | 재료를 표시할 술 도감 항목 | 필수 | 시스템 ID | `category=ALCOHOL`인 실제 Entry | `codex_entries.csv` | `CODEX_EXAMPLE_ALCOHOL` |
| `ingredient_order` | 술 행 안의 재료 표시 순서 | 필수 | 0 이상 정수 | 같은 Entry에서 중복 금지 | UI 정렬 | `0` |
| `item_id` | 재료 아이템 ID | 필수 | 시스템 ID | 실제 `items.csv.item_id` | Item 이름·설명·아이콘 | `ITEM_EXAMPLE_HERB` |
| `location_text_key` | 재료 위치 안내 문장 Key | 필수 | Localization Key | 실제 `text_key` | `localization.csv` | `item.example_herb.location` |

## 5. CodeList 및 입력 가능 값

- 고정 enum은 없습니다.
- `entry_id`: `codex_entries.csv` 중 `ALCOHOL` 카테고리의 동적 목록입니다.
- `item_id`: 실제 Item ID 목록입니다. 재료의 세부 ItemType은 제작 시스템 정책과 맞아야 하며, `ITEM_ALCOHOL_TOOL`만 허용한다는 식으로 임의 제한하지 않습니다.
- `ingredient_order`: 같은 술 안에서 고유하며 0부터 연속 작성을 권장합니다.
- 같은 술에 같은 `item_id`를 두 번 등록할 수 없습니다.

## 6. 다른 CSV와의 연결

- 부모: `codex_entries.entry_id`(`category=ALCOHOL`)
- 참조함: `item_id` → `items.csv`, `location_text_key` → `localization.csv`
- 표시: 재료 이름·설명·아이콘은 Item 원본에서 가져오고, 이 파일은 위치 안내만 추가합니다.
- 잠금 정책: 술을 아직 발견하지 않아도 재료와 위치는 표시합니다. 술 이미지만 잠금 처리합니다.

## 7. 작성 순서

1. 술 Item과 `ALCOHOL` 도감 Entry를 만듭니다.
2. 재료 Item을 만듭니다.
3. 위치 안내 Localization 행을 만듭니다.
4. 재료를 0부터 읽기 순서대로 작성합니다.
5. 중복 재료와 중복 순서를 확인합니다.

## 8. 올바른 예

아래는 형식 설명용이며 실제 제조법이 아닙니다.

```csv
entry_id,ingredient_order,item_id,location_text_key
CODEX_EXAMPLE_ALCOHOL,0,ITEM_EXAMPLE_HERB,item.example_herb.location
CODEX_EXAMPLE_ALCOHOL,1,ITEM_EXAMPLE_WATER,item.example_water.location
```

## 9. 잘못된 예

| 잘못된 값 | 이유 |
| --- | --- |
| 유물 Entry ID | 술 카테고리만 부모가 될 수 있음 |
| 같은 Entry의 순서 `0` 두 행 | 표시 순서 중복 |
| 같은 Item을 두 번 등록 | 중복 재료 |
| 존재하지 않는 Item ID | Item 참조 오류 |
| `location_text_key=미정` | Localization Key가 아니며 미확정 표기 방식도 아님 |

## 10. 자동 또는 Unity 검증 항목

Phase 1 구현 시 정확한 4열 헤더, 필수값, 부모 존재와 카테고리, 순서 범위·중복, Item 존재, 재료 중복, 위치 Key 존재, 모든 술 Entry의 재료 1개 이상을 검사합니다. Item 아이콘 누락은 Unity Catalog 경고로 구분합니다. 현재는 이 CSV용 자동 검증이 없습니다.

## 11. 수정과 삭제 시 주의사항

재료를 삭제하면 도감 표시 제조법이 즉시 바뀝니다. 실제 제작 시스템에 별도 제조법 데이터가 있다면 두 데이터의 의도가 어긋나지 않는지 함께 확인합니다. 마지막 재료 행을 삭제해 술 Entry가 빈 재료 목록이 되면 오류입니다.

## 12. 빠른 체크리스트

- [ ] 정확한 4열 헤더인가?
- [ ] 부모가 실제 `ALCOHOL` 도감 Entry인가?
- [ ] 순서가 중복되지 않고 0부터 읽기 쉬운가?
- [ ] 재료 Item과 위치 Key가 존재하는가?
- [ ] 같은 재료를 중복 등록하지 않았는가?
- [ ] 모든 술 Entry에 재료가 한 개 이상 있는가?

## 13. 관련 문서와 코드

- [도감 Entry CSV](codex_entries_csv.md)
- [items.csv](../PHASE_01_LOCALIZATION_ITEMS/items_csv.md)
- [localization.csv](../PHASE_01_LOCALIZATION_ITEMS/localization_csv.md)
- [공통 작성 규칙](../PHASE_00_FOUNDATION/COMMON_AUTHORING_RULES.md)

