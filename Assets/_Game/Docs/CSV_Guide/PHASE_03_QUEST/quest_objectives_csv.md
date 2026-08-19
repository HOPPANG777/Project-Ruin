# quest_objectives.csv 작성 가이드

## 1. 목적

퀘스트 완료 조건을 진행 플래그의 기대 Boolean 값으로 정의한다.

## 2. 언제 작성하는가

부모 퀘스트와 설명 문구를 만든 뒤 작성한다. 목표가 없는 퀘스트는 헤더만 둘 수 있지만, 런타임에서는 목표 조건이 즉시 충족된 것으로 평가될 수 있으므로 의도 확인이 필수다.

## 3. 정확한 헤더

```csv
quest_id,objective_order,objective_id,description_text_key,required_progress_flag_id,required_flag_value
```

## 4. 열 규칙

| 열 | 필수 | 형식/검증 |
|---|---:|---|
| `quest_id` | 예 | 존재하는 퀘스트 ID |
| `objective_order` | 예 | 0 이상 정수, 같은 퀘스트 안에서 유일 |
| `objective_id` | 예 | 시스템 ID, 같은 퀘스트 안에서 유일 |
| `description_text_key` | 예 | 존재하는 로컬라이제이션 키 |
| `required_progress_flag_id` | 예 | 시스템 ID |
| `required_flag_value` | 예 | 정확히 `true` 또는 `false` |

## 5. CodeList

Boolean은 소문자 `true`, `false`만 허용한다. 순서는 고정 목록이 아니라 0 이상 정수이며, 가독성을 위해 `0,1,2...` 연속값을 권장한다.

## 6. 연결 관계

`quests.csv`와 `localization.csv`를 참조한다. `required_progress_flag_id`는 진행 시스템이 실제로 설정하는 플래그 이름과 정확히 같아야 한다. 로더 규칙상 대문자 시스템 ID만 유효하다.

## 7. 작성 순서

1. 게임플레이가 설정할 진행 플래그를 확정한다.
2. 목표 설명 키를 등록한다.
3. 부모 퀘스트별로 0부터 순서를 배정한다.
4. 기대값을 `true`/`false`로 입력한다.
5. 플래그를 실제로 바꾸는 상호작용/대화를 플레이 검증한다.

## 8. 유효 예시

```csv
QUEST_REPAIR_GATE,0,REPAIR_PANEL,quest.repair_gate.objective.repair,PANEL_REPAIRED,true
```

## 9. 잘못된 예시

| 행/값 | 이유 |
|---|---|
| `objective_order=-1` | 음수 불가 |
| `required_flag_value=True` | 대문자 Boolean 불가 |
| `skill.acquired.DASH` | 점/소문자를 포함하여 현재 시스템 ID 로더를 통과하지 못함 |
| 동일 퀘스트의 같은 `objective_order` | 저장소 중복 오류 |

## 10. 자동/Unity 검증

로더가 정수·Boolean·ID를 검사하고, 저장소가 부모/문구 참조와 퀘스트 내부 순서·ID 중복을 검사한다. 해당 플래그가 실제 플레이에서 바뀌는지는 자동 CSV 검증 범위가 아니므로 PlayMode에서 확인한다.

## 11. 수정·삭제 주의

플래그 ID나 기대값을 바꾸면 이미 진행 중인 퀘스트의 판정이 달라진다. 모든 목표 삭제는 의도치 않은 즉시 충족을 만들 수 있다.

## 12. 빠른 체크리스트

- [ ] 부모 퀘스트와 설명 키가 존재한다.
- [ ] 순서와 목표 ID가 퀘스트 안에서 유일하다.
- [ ] 플래그는 대문자 시스템 ID다.
- [ ] Boolean은 소문자다.
- [ ] 실제 플레이가 같은 플래그를 설정한다.

## 13. 관련 문서/코드

- [quests.csv](quests_csv.md)
- [CodeList](../PHASE_00_FOUNDATION/CODELIST_MASTER.md)
- [알려진 가이드 정정](../PHASE_00_FOUNDATION/EXISTING_GUIDE_CORRECTIONS.md)
- [QuestCsvDefinitionLoader.cs](../../../Scripts/Gameplay/Data/Csv/QuestCsvDefinitionLoader.cs)
- [QuestController.cs](../../../Scripts/Gameplay/Quests/QuestController.cs)
- [원본 CSV](../../../Data/Csv/quest_objectives.csv)
