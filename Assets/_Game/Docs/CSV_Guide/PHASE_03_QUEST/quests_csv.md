# quests.csv 작성 가이드

## 1. 목적

퀘스트의 최상위 식별자, 표시 문구, 담당 NPC를 선언한다. 목표·보상·소비 아이템은 모두 `quest_id`로 이 파일을 참조한다.

## 2. 언제 작성하는가

로컬라이제이션 키를 만든 뒤, 목표/보상/소비 행을 만들기 전에 작성한다.

## 3. 정확한 헤더

```csv
quest_id,title_text_key,description_text_key,owner_npc_id
```

## 4. 열 규칙

| 열 | 필수 | 형식/검증 |
|---|---:|---|
| `quest_id` | 예 | 전역 유일 시스템 ID |
| `title_text_key` | 예 | 존재하는 로컬라이제이션 키 |
| `description_text_key` | 예 | 존재하는 로컬라이제이션 키 |
| `owner_npc_id` | 아니요 | 비우거나 시스템 ID. 현재 저장소는 NPC 실재 여부를 검증하지 않음 |

## 5. CodeList

고정 목록은 없다. `quest_id`와 `owner_npc_id`는 `^[A-Z][A-Z0-9_]*$`, 문구 키는 `^[a-z][a-z0-9._]*$`를 따른다.

## 6. 연결 관계

- 부모: `localization.csv`
- 자식: `quest_objectives.csv`, `quest_rewards.csv`, `quest_consume_items.csv`
- 선택 연결: `npc_quest_dialogue_routes.csv`, 대화 선택의 퀘스트 액션

`owner_npc_id`는 형식만 자동 검증되므로 실제 NPC/상호작용 대상과의 일치는 디자이너가 확인한다.

## 7. 작성 순서

1. 퀘스트 ID를 확정한다.
2. 제목/설명 로컬라이제이션을 등록한다.
3. 담당 NPC가 있으면 ID를 입력한다.
4. 목표, 보상, 소비 아이템을 작성한다.
5. NPC 라우트와 대화 액션을 연결한다.

## 8. 유효 예시

```csv
QUEST_REPAIR_GATE,quest.repair_gate.title,quest.repair_gate.description,NPC_ENGINEER
```

## 9. 잘못된 예시

| 행 | 이유 |
|---|---|
| `quest_repair,...` | 시스템 ID가 대문자로 시작하지 않음 |
| `QUEST_A,missing.title,...` | 문구 키가 `localization.csv`에 없으면 저장소 구성 실패 |
| `QUEST_A,...,npc.engineer` | `owner_npc_id`가 시스템 ID 형식이 아님 |

## 10. 자동/Unity 검증

CSV 로더는 헤더·필수값·ID 형식을, 저장소는 퀘스트 ID 중복과 로컬라이제이션 참조를 검사한다. `owner_npc_id` 실재 여부와 플레이 흐름은 Editor/PlayMode에서 별도 확인한다.

## 11. 수정·삭제 주의

`quest_id` 변경은 모든 자식 CSV, NPC 라우트, 대화 액션과 런타임 퀘스트 상태 키에 영향을 준다. 출시 후 변경은 기존 저장 데이터와의 호환 계획 없이는 하지 않는다.

## 12. 빠른 체크리스트

- [ ] 헤더가 정확하다.
- [ ] ID가 대문자 시스템 ID이며 중복되지 않는다.
- [ ] 제목/설명 키가 존재한다.
- [ ] 자식 행과 대화/NPC 참조를 함께 확인했다.
- [ ] 담당 NPC를 실제 장면에서 확인했다.

## 13. 관련 문서/코드

- [공통 규칙](../PHASE_00_FOUNDATION/COMMON_AUTHORING_RULES.md)
- [ID 변경 영향](../PHASE_07_REFERENCE/ID_CHANGE_IMPACT_MAP.md)
- [QuestCsvDefinitionLoader.cs](../../../Scripts/Gameplay/Data/Csv/QuestCsvDefinitionLoader.cs)
- [QuestController.cs](../../../Scripts/Gameplay/Quests/QuestController.cs)
- [원본 CSV](../../../Data/Csv/quests.csv)
