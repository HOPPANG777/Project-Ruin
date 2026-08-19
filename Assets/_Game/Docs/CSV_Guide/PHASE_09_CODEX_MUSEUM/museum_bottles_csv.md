# museum_bottles.csv

[도감·박물관 가이드](README.md) · [술병 인정 목록](museum_bottle_items_csv.md) · [items.csv](../PHASE_01_LOCALIZATION_ITEMS/items_csv.md)

> 상태: 계획 계약. 정확한 술과 보상 콘텐츠는 아직 확정되지 않았으며 이 파일과 런타임 로더도 아직 없습니다.

## 1. 이 CSV의 목적

박물관에 놓이는 챕터별 술병과 완성 보상을 정의합니다. 한 행은 **술병 한 개**입니다. 실제로 투입된 술, 완성 여부, 보상 수령 여부는 저장 상태이며 이 파일에 쓰지 않습니다.

## 2. 언제 작성하는가

박물관의 술병 수, 담당 챕터, 표시 이름 또는 완성 보상을 정할 때 작성합니다. 현재 정책은 박물관 전체 3개, 챕터당 1개입니다.

## 3. 정확한 헤더

```csv
bottle_id,chapter,display_name_key,completion_reward_item_id,completion_reward_amount
```

## 4. 열별 작성 규칙

| 열 이름 | 쉬운 설명 | 필수 여부 | 형식 | 입력 가능 값 | 참조 대상 | 올바른 예 |
| --- | --- | --- | --- | --- | --- | --- |
| `bottle_id` | 술병과 저장 상태를 찾는 ID | 필수 | 시스템 ID | 전체 술병에서 고유 | 자식 목록·씬·저장 | `BOTTLE_EXAMPLE_CH1` |
| `chapter` | 이 술병이 받는 술의 챕터 | 필수 | 0 이상 정수 | 술병끼리 중복 금지 | 도감 술 그룹 | `1` |
| `display_name_key` | 선택 UI에 표시할 술병 이름 Key | 필수 | Localization Key | 실제 `text_key` | `localization.csv` | `museum.bottle.example_ch1.name` |
| `completion_reward_item_id` | 처음 완성했을 때 지급할 Item ID | 조건부 | 시스템 ID | 빈칸 또는 실제 Item ID | `items.csv` | `ITEM_EXAMPLE_REWARD` |
| `completion_reward_amount` | 완성 보상 수량 | 조건부 | 1 이상 정수 | Item ID와 함께 입력 | Inventory | `1` |

보상 두 열은 함께 입력하거나 함께 비웁니다. 콘텐츠가 미확정이면 가짜 ID를 쓰지 말고 둘 다 비웁니다. 빈 보상은 계획 중 허용되지만 출시 준비 검증에서는 경고 대상입니다.

## 5. CodeList 및 입력 가능 값

- 고정 enum은 없습니다.
- `bottle_id`: 시스템 ID, 전역 고유입니다.
- `chapter`: 0 이상이며 다른 술병과 중복할 수 없습니다.
- 보상 조합:

| 상태 | `completion_reward_item_id` | `completion_reward_amount` |
| --- | --- | --- |
| 보상 확정 | 실제 Item ID | 1 이상 |
| 아직 미확정 | 빈칸 | 빈칸 |
| 한쪽만 입력 | 오류 | 오류 |

- 현재 승인된 콘텐츠 수는 전체 3행입니다. 정확한 챕터 번호는 기획 확정값을 사용합니다.

## 6. 다른 CSV와의 연결

- 참조함: 이름 → `localization.csv`, 보상 → `items.csv`
- 참조받음: `museum_bottle_items.bottle_id`
- 씬: 술병 상호작용 컴포넌트의 `bottle_id`와 정확히 일치해야 합니다.
- 저장: 인정된 술 목록, 완료, 보상 지급 완료는 각각 분리합니다.

## 7. 작성 순서

1. 3개 술병의 챕터를 확정합니다.
2. 고유한 `BOTTLE_...` ID와 이름 Key를 만듭니다.
3. 술병 행을 작성합니다.
4. 챕터에 맞는 인정 술 자식 행을 작성합니다.
5. 보상이 확정됐을 때만 Item ID와 수량을 함께 입력합니다.
6. Unity 씬의 술병 컴포넌트 ID를 연결합니다.

## 8. 올바른 예

아래는 형식 설명용이며 실제 콘텐츠가 아닙니다.

```csv
bottle_id,chapter,display_name_key,completion_reward_item_id,completion_reward_amount
BOTTLE_EXAMPLE_CH1,1,museum.bottle.example_ch1.name,,
BOTTLE_EXAMPLE_CH2,2,museum.bottle.example_ch2.name,ITEM_EXAMPLE_REWARD,1
```

첫 행은 보상 미확정 상태를 올바르게 비운 예입니다.

## 9. 잘못된 예

| 잘못된 값 | 이유 |
| --- | --- |
| `completion_reward_item_id=TODO` | 가짜 시스템 ID를 실데이터로 쓰면 안 됨 |
| Item ID만 있고 수량 빈칸 | 조건부 쌍 위반 |
| 수량 `0` | 보상 수량은 1 이상 |
| 같은 챕터 술병 두 행 | 현재 챕터당 1개 정책 위반 |
| 술병 4행 | 현재 박물관 전체 3개 정책 위반 |

## 10. 자동 또는 Unity 검증 항목

Phase 5 구현 시 정확한 헤더, ID·챕터 중복, 이름 Key, 보상 조건부 쌍, 보상 Item 존재·수량, 전체 3행, 각 술병의 자식 1개 이상을 검사합니다. 보상 빈칸은 개발 중 허용하되 콘텐츠 완료 검증에서 경고합니다. Unity에서는 모든 `bottle_id`의 씬 연결을 확인합니다. 현재 자동 검증은 없습니다.

## 11. 수정과 삭제 시 주의사항

`bottle_id`는 이미 투입한 술과 보상 수령 상태의 저장 Key입니다. 출시 후 변경하면 진행을 잃을 수 있습니다. 챕터를 바꾸면 기존 투입 목록과 새 허용 목록이 충돌할 수 있으므로 저장 마이그레이션이 필요합니다. 완성 보상을 바꿀 때는 이미 보상을 받은 저장에 소급 지급하지 않도록 정책을 정합니다.

## 12. 빠른 체크리스트

- [ ] 정확한 5열 헤더인가?
- [ ] 전체 3개이고 챕터가 서로 다른가?
- [ ] 이름 Key가 실제로 존재하는가?
- [ ] 보상 두 열을 함께 입력하거나 함께 비웠는가?
- [ ] 각 술병에 인정 술이 한 개 이상 연결되는가?
- [ ] 씬 술병 ID와 정확히 일치하는가?

## 13. 관련 문서와 코드

- [술병 인정 목록](museum_bottle_items_csv.md)
- [items.csv](../PHASE_01_LOCALIZATION_ITEMS/items_csv.md)
- [Progress 저장 가이드](../../ES3_PROGRESS_SAVE_GUIDE.md)
- [검증과 문제 해결](../PHASE_08_WORKFLOW_QA/VALIDATION_AND_TROUBLESHOOTING.md)

