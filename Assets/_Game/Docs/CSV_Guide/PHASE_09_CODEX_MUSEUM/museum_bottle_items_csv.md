# museum_bottle_items.csv

[도감·박물관 가이드](README.md) · [술병](museum_bottles_csv.md) · [도감 항목](codex_entries_csv.md)

> 상태: **Phase 5 런타임·자동 검증 연결 완료**. 실제 술 목록은 아직 확정되지 않아 CSV는 헤더만 유지합니다.

## 1. 이 CSV의 목적

각 술병이 인정하는 술 종류와 채움 기여도를 정의합니다. 한 행은 **특정 술병에 넣을 수 있는 술 한 종류**입니다. 같은 술 종류는 전역 한 번만 인정하며, 투입하면 인벤토리에서 1개가 소비됩니다.

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

## 5. 입력 가능 값과 교차 참조

- `alcohol_item_id`는 `items.csv`에서 `item_type=ITEM_ALCOHOL_COLLECTIBLE`인 동적 목록입니다.
- 해당 Item은 `codex_entries.csv`의 `ALCOHOL` 항목으로 한 번 등록되어야 합니다.
- Item의 도감 `chapter`와 부모 술병 `chapter`가 같아야 합니다.
- 같은 술병 안에서 `item_order`와 술 ID는 각각 중복할 수 없습니다.
- 한 술 종류를 여러 술병에 등록하지 않습니다.
- `contribution_value` 기본 권장값은 `1`입니다. 서로 다른 가중치가 확정될 때만 다른 양의 정수를 사용합니다.
- 진행률은 `투입한 기여도 합 / 전체 기여도 합`이며 최대 1로 제한합니다.

## 6. 런타임 동작

- 선택 UI에는 현재 보유 중이고, 이 병에 허용되며, 아직 투입하지 않은 술만 표시합니다.
- 버튼을 누르면 상태 변경 직전에 보유량·허용 목록·중복·완성 여부를 다시 확인합니다.
- 일반 투입은 Item 1개를 소비한 뒤 인정 상태를 저장합니다.
- 마지막 투입에 보상이 있으면 술 1개 제거와 보상 추가를 원자 교환합니다.
- 보상 추가가 불가능하면 술과 완료 상태를 변경하지 않아 다시 시도할 수 있습니다.
- 완성 후 추가 투입과 같은 술의 중복 투입은 금지합니다.
- 빠른 재입력 중 두 번째 요청은 `MUSEUM_BOTTLE_FAILURE_BUSY`로 거부합니다.

## 7. 작성 순서

1. 술 Item과 ALCOHOL 도감 Entry를 만듭니다.
2. 같은 챕터의 부모 술병을 확인합니다.
3. UI 표시 순서를 0부터 정합니다.
4. 기본적으로 기여도를 모두 1로 작성합니다.
5. 술병 안팎의 중복과 챕터 일치를 확인합니다.
6. 전체 기여도 합과 UI·씬 채움 표현을 확인합니다.

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
| 부모와 다른 챕터의 술 | 도감 챕터 불일치 |
| 같은 술을 두 병에 등록 | 술 종류는 전역 한 번만 인정 |
| 같은 병에 같은 술 두 행 | 중복 투입 정의 |
| `contribution_value=0` | 1 이상이어야 함 |
| `contribution_value=10%` | 정수 열에 퍼센트 문자열 사용 |

## 10. 자동 또는 Unity 검증 항목

`MuseumBottleDataRepository`는 헤더, 부모 존재, 순서·술 중복, Item 존재·ItemType, ALCOHOL 도감 Entry, 챕터 일치, 전역 술 중복, 기여도 1 이상, 부모별 자식 1개 이상을 검사합니다. 런타임 `MuseumBottleController`는 보유량, 기존 투입, 완성, 보상 수용 가능 여부와 빠른 재입력을 상태 변경 직전에 다시 검사합니다.

실행 메뉴: `Tools > Project Ruin > Museum > Validate Phase 5 Data`

## 11. 수정과 삭제 시 주의사항

플레이어가 술을 투입한 뒤 목록을 바꾸면 현재 CSV에 남은 인정 술만 복원되고 완성률이 다시 계산됩니다. 출시 후 행 삭제·이동에는 저장 마이그레이션을 검토합니다. 기여도 변경은 인정된 종류를 유지하고 표시 비율만 다시 계산합니다.

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
