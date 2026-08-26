# museum_bottles.csv

[도감·박물관 가이드](README.md) · [술병 인정 목록](museum_bottle_items_csv.md) · [items.csv](../PHASE_01_LOCALIZATION_ITEMS/items_csv.md)

> 상태: **Phase 5 런타임·자동 검증 연결 완료**. 정확한 술과 보상 콘텐츠는 아직 확정되지 않아 실제 CSV는 헤더만 유지합니다.

## 1. 이 CSV의 목적

박물관에 놓이는 챕터별 술병과 완성 보상을 정의합니다. 한 행은 **술병 한 개**입니다. 실제로 투입된 술, 완성 여부, 보상 수령 여부는 저장 상태이며 이 파일에 쓰지 않습니다.

## 2. 언제 작성하는가

박물관의 술병 수, 담당 챕터, 표시 이름 또는 완성 보상을 정할 때 작성합니다. 현재 정책은 박물관 전체 3개, 챕터당 1개입니다. 일부 병만 먼저 작성하면 런타임 검증이 실패하므로 3개를 한 번에 작성합니다.

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

보상 두 열은 함께 입력하거나 함께 비웁니다. 콘텐츠가 미확정이면 가짜 ID를 쓰지 말고 둘 다 비웁니다. 보상이 없는 상태로 완성된 저장에는 나중에 보상 행을 바꾸더라도 소급 지급하지 않습니다.

## 5. 입력 가능 값

- `bottle_id`: 시스템 ID이며 전역 고유입니다.
- `chapter`: 0 이상이며 다른 술병과 중복할 수 없습니다.
- 현재 승인된 콘텐츠 수는 전체 3행입니다. 정확한 챕터 번호는 기획 확정값을 사용합니다.

| 상태 | `completion_reward_item_id` | `completion_reward_amount` |
| --- | --- | --- |
| 보상 확정 | 실제 Item ID | 1 이상 |
| 아직 미확정 | 빈칸 | 빈칸 |
| 한쪽만 입력 | 오류 | 오류 |

## 6. 다른 CSV·Unity·저장과의 연결

- 이름 → `localization.csv`, 보상 → `items.csv`
- 자식 → `museum_bottle_items.bottle_id`
- 씬 → `MuseumBottleInteractable.BottleId`와 정확히 일치
- 저장 → `museum.bottles.v1`에 인정 술, 완료, 보상 지급 완료를 분리 저장
- UI → `MuseumBottlePresenter`가 이름과 현재 투입 가능한 보유 술만 표시

## 7. 작성 순서

1. 3개 술병의 챕터를 확정합니다.
2. 고유한 `BOTTLE_...` ID와 이름 Key를 만듭니다.
3. 3개 술병 행을 함께 작성합니다.
4. 각 병에 챕터가 같은 인정 술 자식 행을 한 개 이상 작성합니다.
5. 보상이 확정됐을 때만 Item ID와 수량을 함께 입력합니다.
6. Unity 씬의 술병 컴포넌트 ID와 액체 채움 Transform을 연결합니다.

## 8. 올바른 예

아래는 형식 설명용이며 실제 콘텐츠가 아닙니다.

```csv
bottle_id,chapter,display_name_key,completion_reward_item_id,completion_reward_amount
BOTTLE_EXAMPLE_CH1,1,museum.bottle.example_ch1.name,,
BOTTLE_EXAMPLE_CH2,2,museum.bottle.example_ch2.name,ITEM_EXAMPLE_REWARD,1
```

첫 행은 보상 미확정 상태를 올바르게 비운 예입니다. 실제 작성 시에는 승인 정책대로 총 3행을 작성해야 합니다.

## 9. 잘못된 예

| 잘못된 값 | 이유 |
| --- | --- |
| `completion_reward_item_id=TODO` | 가짜 시스템 ID를 실데이터로 쓰면 안 됨 |
| Item ID만 있고 수량 빈칸 | 조건부 쌍 위반 |
| 수량 `0` | 보상 수량은 1 이상 |
| 같은 챕터 술병 두 행 | 챕터당 1개 정책 위반 |
| 술병 1·2·4행 | 헤더 전용 0행 또는 정확히 3행만 허용 |

## 10. 자동 또는 Unity 검증 항목

`MuseumBottleDataRepository`는 정확한 헤더, ID·챕터 중복, 이름 Key, 보상 조건부 쌍, 보상 Item 존재·수량, 0행 또는 전체 3행, 각 술병의 자식 1개 이상을 검사합니다. `MuseumPhase5EditorValidator`는 GameRoot·UI·CSV 연결과 모든 `bottle_id`의 Scene 컴포넌트 1:1 배치를 빌드 전에 검사합니다.

실행 메뉴: `Tools > Project Ruin > Museum > Validate Phase 5 Data`

## 11. 수정과 삭제 시 주의사항

`bottle_id`는 이미 투입한 술과 보상 수령 상태의 저장 Key입니다. 출시 후 변경하면 진행을 잃을 수 있습니다. 챕터나 요구 목록을 바꾸면 현재 CSV에 남아 있는 인정 술만 복원하고 완료율을 다시 계산하므로 저장 마이그레이션을 검토해야 합니다.

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

