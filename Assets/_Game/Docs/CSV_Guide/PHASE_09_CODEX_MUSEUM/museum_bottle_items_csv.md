# museum_bottle_items.csv

[도감·박물관 가이드](README.md) · [술병](museum_bottles_csv.md) · [도감 항목](codex_entries_csv.md)

> 상태: 계획 계약. 실제 술 목록은 아직 확정되지 않았습니다.

## 1. 이 CSV의 목적

각 술병이 인정하는 술 종류와 채움 기여도를 정의합니다. 한 행은 **특정 술병에 넣을 수 있는 술 한 종류**입니다. 같은 술 종류는 한 번만 인정하며, 투입하면 인벤토리에서 1개가 소비됩니다.

## 2. 언제 작성하는가

술 Item과 술병을 만든 뒤 챕터별 요구 목록과 시각적 채움 비율을 정할 때 작성합니다. 현재 예상은 챕터별 약 8~10종이지만 실제 행 수는 확정된 콘텐츠를 따릅니다.

## 3. 정확한 헤더

```csv
bottle_id,item_order,alcohol_item_id,contribution_value
```

## 4. 열별 작성 규칙

| 열 이름 | 쉬운 설명 | 필수 여부 | 형식 | 입력 가능 값 | 참조 대상 | 올바른 예 |
| --- | --- | --- | --- | --- | --- | --- |
| `bottle_id` | 이 술을 받을 부모 술병 | 필수 | 시스템 ID | 실제 Bottle ID | `museum_bottles.csv` | `BOTTLE_EXAMPLE_CH1` |
| `item_order` | 술병 선택 UI의 표시 순서 | 필수 | 0 이상 정수 | 같은 술병에서 중복 금지 | UI 정렬 | `0` |
| `alcohol_item_id` | 투입 가능한 완성 술 Item ID | 필수 | 시스템 ID | 실제 술 수집 Item | `items.csv`·도감 | `ITEM_EXAMPLE_ALCOHOL` |
| `contribution_value` | 이 술이 채우는 상대 기여도 | 필수 | 1 이상 정수 | 기본 권장 `1` | 술병 채움 표시 | `1` |

## 5. CodeList 및 입력 가능 값

- `alcohol_item_id`는 `items.csv`에서 `item_type=ITEM_ALCOHOL_COLLECTIBLE`인 동적 목록입니다.
- 해당 Item은 `codex_entries.csv`의 `ALCOHOL` 항목으로도 한 번 등록되어야 합니다.
- Item의 도감 `chapter`와 부모 술병 `chapter`가 같아야 합니다.
- 같은 술병 안에서 순서와 술 ID는 각각 중복할 수 없습니다.
- 한 술 종류를 여러 술병에 등록하지 않습니다.
- `contribution_value` 기본값은 모두 `1`입니다. 서로 다른 가중치가 기획으로 확정될 때만 다른 양의 정수를 사용합니다.
- 진행률은 `투입한 기여도 합 / 전체 기여도 합`으로 계산하며 CSV에 퍼센트 문자열을 쓰지 않습니다.

## 6. 다른 CSV와의 연결

- 부모: `museum_bottles.bottle_id`
- 참조함: `alcohol_item_id` → `items.csv`, 같은 Item의 `codex_entries` 술 항목
- 상태: 투입 성공 시 Item 1개 소비 + 해당 술 인정 상태 저장
- 완료: 부모에 등록된 모든 고유 술이 인정되면 완료하며, 완성 후 추가 투입은 금지합니다.

## 7. 작성 순서

1. 술 Item과 도감 Entry를 만듭니다.
2. 같은 챕터의 부모 술병을 확인합니다.
3. UI 표시 순서를 0부터 정합니다.
4. 기본적으로 기여도를 모두 1로 작성합니다.
5. 술병 안팎의 중복과 챕터 일치를 확인합니다.
6. 전체 기여도 합과 UI 채움 미리보기를 확인합니다.

## 8. 올바른 예

아래는 형식 설명용이며 실제 술 목록이 아닙니다.

```csv
bottle_id,item_order,alcohol_item_id,contribution_value
BOTTLE_EXAMPLE_CH1,0,ITEM_EXAMPLE_ALCOHOL_A,1
BOTTLE_EXAMPLE_CH1,1,ITEM_EXAMPLE_ALCOHOL_B,1
```

## 9. 잘못된 예

| 잘못된 값 | 이유 |
| --- | --- |
| 유물 또는 술 도구 Item ID | `ITEM_ALCOHOL_COLLECTIBLE`만 허용 |
| 부모와 다른 챕터의 술 | 챕터 불일치 |
| 같은 술을 두 병에 등록 | 술 종류는 전역 한 번만 인정 |
| 같은 병에 같은 술 두 행 | 중복 투입 정의 |
| `contribution_value=0` | 1 이상이어야 함 |
| `contribution_value=10%` | 정수 열에 퍼센트 문자열 사용 |

## 10. 자동 또는 Unity 검증 항목

Phase 5 구현 시 헤더, 부모 존재, 순서·술 중복, Item 존재·ItemType, 도감 Entry 존재, 챕터 일치, 전역 술 중복, 기여도 1 이상, 부모별 자식 1개 이상을 검사합니다. 런타임은 보유량, 기존 투입 여부, 완성 여부를 상태 변경 직전에 다시 검사해야 합니다. 현재 자동 검증은 없습니다.

## 11. 수정과 삭제 시 주의사항

이미 플레이어가 술을 투입한 뒤 목록을 바꾸면 완성률과 완료 여부가 달라질 수 있습니다. 출시 후 행 삭제·이동은 기존 저장의 인정 Item ID를 어떻게 처리할지 마이그레이션이 필요합니다. 기여도를 바꿔도 이미 인정된 종류 자체는 유지하고 진행률만 다시 계산하는 정책을 권장합니다.

## 12. 빠른 체크리스트

- [ ] 정확한 4열 헤더인가?
- [ ] 부모 술병과 술 Item이 실제로 존재하는가?
- [ ] 술 ItemType과 도감 카테고리가 맞는가?
- [ ] 챕터가 부모 술병과 같은가?
- [ ] 순서와 술 ID가 중복되지 않는가?
- [ ] 기여도가 1 이상이며 특별한 이유가 없으면 모두 1인가?

## 13. 관련 문서와 코드

- [술병 CSV](museum_bottles_csv.md)
- [도감 Entry CSV](codex_entries_csv.md)
- [items.csv](../PHASE_01_LOCALIZATION_ITEMS/items_csv.md)
- [Inventory 작성 가이드](../../INVENTORY_SYSTEM_AUTHORING_GUIDE.md)

