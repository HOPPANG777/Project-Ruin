# quest_consume_items.csv 작성 가이드

## 1. 목적

퀘스트 완료/보상 수령 시 보유 여부를 검사하고 실제로 차감할 아이템을 정의한다.

## 2. 언제 작성하는가

퀘스트 완료에 아이템 제출이 필요한 경우, 부모 퀘스트와 아이템을 만든 뒤 작성한다. 제출물이 없으면 헤더만 유지한다.

## 3. 정확한 헤더

```csv
quest_id,consume_order,item_id,item_amount
```

## 4. 열 규칙

| 열 | 필수 | 형식/검증 |
|---|---:|---|
| `quest_id` | 예 | 존재하는 퀘스트 ID |
| `consume_order` | 예 | 0 이상 정수, 같은 퀘스트 안에서 유일 |
| `item_id` | 예 | 존재하는 아이템 ID, 같은 퀘스트 안에서 유일 |
| `item_amount` | 예 | 1 이상 정수 |

## 5. CodeList

고정 enum은 없다. 순서는 0 이상 정수이며 `0,1,2...` 연속값을 권장한다.

## 6. 연결 관계

`quests.csv`와 `items.csv`를 참조한다. 소비 행이 하나라도 있으면 목표 플래그만 충족되어도 자동 완료되지 않고, 완료/보상 수령 시 보유 수량 검사와 차감이 수행된다.

## 7. 작성 순서

1. 제출할 아이템과 수량을 확정한다.
2. 퀘스트별로 순서를 정한다.
3. 같은 아이템을 한 퀘스트에 중복하지 않는다.
4. 부족/충분 수량 두 경우를 플레이한다.
5. 보상 수령 후 정확히 차감됐는지 확인한다.

## 8. 유효 예시

```csv
QUEST_REPAIR_GATE,0,ITEM_PART,3
```

## 9. 잘못된 예시

| 행/값 | 이유 |
|---|---|
| `item_amount=0` | 양의 수량이 아님 |
| 같은 퀘스트에 `ITEM_PART` 두 행 | 아이템 중복 |
| 존재하지 않는 `ITEM_UNKNOWN` | 저장소 참조 오류 |
| 같은 퀘스트의 같은 `consume_order` | 순서 중복 |

## 10. 자동/Unity 검증

로더가 정수/ID를, 저장소가 부모·아이템 참조와 순서·아이템 중복을 검사한다. 런타임은 완료/수령 시 전체 수량을 확인한 뒤 소비한다. UI 피드백과 저장 복원은 PlayMode에서 확인한다.

## 11. 수정·삭제 주의

진행 중인 퀘스트의 수량을 올리면 사용자가 새 요구량을 맞춰야 한다. 행을 삭제하면 다음 로드부터 소비 없이 완료될 수 있다. 아이템 ID 변경은 인벤토리 저장 데이터에도 영향을 준다.

## 12. 빠른 체크리스트

- [ ] 부모 퀘스트와 아이템이 존재한다.
- [ ] 수량은 1 이상이다.
- [ ] 순서와 아이템이 퀘스트 안에서 유일하다.
- [ ] 부족/충분/수령 후 차감을 확인했다.
- [ ] 목표만 충족된 시점의 상태도 확인했다.

## 13. 관련 문서/코드

- [quests.csv](quests_csv.md)
- [items.csv](../PHASE_01_LOCALIZATION_ITEMS/items_csv.md)
- [검증/문제 해결](../PHASE_08_WORKFLOW_QA/VALIDATION_AND_TROUBLESHOOTING.md)
- [QuestCsvDefinitionLoader.cs](../../../Scripts/Gameplay/Data/Csv/QuestCsvDefinitionLoader.cs)
- [QuestController.cs](../../../Scripts/Gameplay/Quests/QuestController.cs)
- [원본 CSV](../../../Data/Csv/quest_consume_items.csv)
