# quest_rewards.csv 작성 가이드

## 1. 목적

퀘스트 보상 수령 시 적용할 진행 플래그와 아이템 지급을 정의한다. 한 행에 둘을 함께 둘 수 있다.

## 2. 언제 작성하는가

부모 퀘스트, 설명 문구, 지급 아이템을 확정한 뒤 작성한다.

## 3. 정확한 헤더

```csv
quest_id,reward_order,reward_id,description_text_key,progress_flag_id,flag_value,item_id,item_amount
```

## 4. 열 규칙

| 열 | 필수 | 형식/검증 |
|---|---:|---|
| `quest_id` | 예 | 존재하는 퀘스트 ID |
| `reward_order` | 예 | 0 이상 정수, 퀘스트 안에서 유일 |
| `reward_id` | 예 | 시스템 ID, 퀘스트 안에서 유일 |
| `description_text_key` | 예 | 존재하는 문구 키 |
| `progress_flag_id` | 조건부 | 비우거나 시스템 ID |
| `flag_value` | 조건부 | 플래그 ID와 함께 비우거나 `true`/`false` |
| `item_id` | 조건부 | 비우거나 존재하는 아이템 ID |
| `item_amount` | 조건부 | 아이템 ID와 함께 비우거나 1 이상 정수 |

플래그 쌍 또는 아이템 쌍 중 적어도 하나가 완전해야 한다. 두 쌍 모두 사용 가능하다.

## 5. CodeList

Boolean은 `true`/`false`; 순서는 0 이상 정수다. 보상 종류를 나타내는 별도 enum은 없다.

## 6. 연결 관계

부모는 `quests.csv`; 문구는 `localization.csv`; 아이템은 `items.csv`다. 플래그는 진행 시스템과 이름 계약을 공유한다.

현재 로더는 플래그 ID에 대문자 시스템 ID를 요구한다. `QuestController`에 `skill.acquired.` 접두사를 해석하는 분기가 있어도 점/소문자 값은 유효 CSV로 로드되지 않으므로 현재 데이터에는 작성하지 않는다.

## 7. 작성 순서

1. 보상 설명을 등록한다.
2. 플래그/아이템 중 적용할 보상을 선택한다.
3. 선택한 쌍의 두 열을 모두 채운다.
4. 퀘스트별 순서와 보상 ID 중복을 확인한다.
5. 보상 수령 후 상태와 인벤토리를 확인한다.

## 8. 유효 예시

```csv
QUEST_REPAIR_GATE,0,OPEN_GATE,quest.repair_gate.reward,GATE_OPEN,true,,
QUEST_REPAIR_GATE,1,GIVE_PARTS,quest.repair_gate.reward.parts,,,ITEM_PART,3
```

## 9. 잘못된 예시

| 값 | 이유 |
|---|---|
| 플래그/아이템 네 열 모두 빈 값 | 보상 효과가 없음 |
| `progress_flag_id=GATE_OPEN`, `flag_value=` | 플래그 쌍 불완전 |
| `item_id=ITEM_PART`, `item_amount=0` | 수량은 1 이상 |
| `progress_flag_id=skill.acquired.DASH` | 현재 로더의 시스템 ID 규칙 위반 |

## 10. 자동/Unity 검증

로더는 조건부 쌍과 형식을, 저장소는 부모·문구·아이템 참조 및 퀘스트 내부 중복을 검사한다. 실제 수령은 `ClaimQuestReward` 흐름에서 플래그와 아이템을 적용하고 저장을 요청하므로 PlayMode에서 중복 수령 방지도 확인한다.

## 11. 수정·삭제 주의

이미 수령된 보상을 바꿔도 기존 저장 데이터에 자동 소급되지 않는다. `reward_id`는 퀘스트 내부 계약이므로 다른 참조/분석 데이터와 함께 변경한다.

## 12. 빠른 체크리스트

- [ ] 최소 한 보상 쌍이 완전하다.
- [ ] 수량은 1 이상이다.
- [ ] 아이템과 문구가 존재한다.
- [ ] 순서/ID가 퀘스트 안에서 유일하다.
- [ ] 플래그는 대문자 시스템 ID다.

## 13. 관련 문서/코드

- [items.csv](../PHASE_01_LOCALIZATION_ITEMS/items_csv.md)
- [quests.csv](quests_csv.md)
- [알려진 가이드 정정](../PHASE_00_FOUNDATION/EXISTING_GUIDE_CORRECTIONS.md)
- [QuestCsvDefinitionLoader.cs](../../../Scripts/Gameplay/Data/Csv/QuestCsvDefinitionLoader.cs)
- [QuestController.cs](../../../Scripts/Gameplay/Quests/QuestController.cs)
- [원본 CSV](../../../Data/Csv/quest_rewards.csv)
