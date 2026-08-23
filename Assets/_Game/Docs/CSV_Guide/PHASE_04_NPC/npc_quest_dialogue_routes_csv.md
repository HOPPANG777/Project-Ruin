# npc_quest_dialogue_routes.csv 작성 가이드

## 1. 목적

NPC가 특정 퀘스트 상태일 때 기본 대화 대신 사용할 그래프를 우선순위 순서로 연결한다.

## 2. 언제 작성하는가

NPC 기본 바인딩과 퀘스트/대화 그래프를 만든 뒤, 퀘스트 상태별 대화 분기가 필요할 때 작성한다.

## 3. 정확한 헤더

```csv
npc_id,route_order,quest_id,quest_state,graph_id
```

## 4. 열 규칙

| 열 | 필수 | 형식/검증 |
|---|---:|---|
| `npc_id` | 예 | 바인딩이 존재하는 NPC ID |
| `route_order` | 예 | 0 이상 정수, NPC 안에서 유일 |
| `quest_id` | 예 | 존재하는 퀘스트 ID |
| `quest_state` | 예 | 아래 4개 값 중 하나 |
| `graph_id` | 예 | 존재하는 그래프 ID |

## 5. CodeList

`quest_state`: `QUEST_NOT_STARTED`, `QUEST_IN_PROGRESS`, `QUEST_COMPLETED`, `QUEST_REWARDED`.

## 6. 연결 관계

`npc_dialogue_bindings.csv`, `quests.csv`, `dialogue_graphs.csv`를 참조한다. 라우트는 NPC별 `route_order` 오름차순으로 평가되므로 여러 조건이 동시에 맞을 수 있다면 먼저 와야 할 규칙을 낮은 숫자로 둔다.

## 7. 작성 순서

1. NPC 기본 바인딩을 만든다.
2. 상태별 그래프를 만든다.
3. 가장 우선할 상태부터 `0,1,2...`를 배정한다.
4. 모든 목표 상태를 직접 만든 뒤 NPC와 대화한다.
5. 일치하지 않을 때 기본 그래프로 돌아가는지 확인한다.

## 8. 유효 예시

```csv
NPC_ENGINEER,0,QUEST_REPAIR_GATE,QUEST_IN_PROGRESS,DIALOGUE_ENGINEER_REPAIRING
NPC_ENGINEER,1,QUEST_REPAIR_GATE,QUEST_COMPLETED,DIALOGUE_ENGINEER_COMPLETE
```

## 9. 잘못된 예시

| 값 | 이유 |
|---|---|
| `quest_state=COMPLETE` | 고정 CodeList에 없음 |
| 바인딩 없는 `NPC_UNKNOWN` | 저장소 참조 오류 |
| 같은 NPC의 `route_order=0` 두 행 | 순서 중복 |
| 존재하지 않는 퀘스트/그래프 | 저장소 구성 실패 |

## 10. 자동/Unity 검증

로더는 숫자와 CodeList를, 저장소는 바인딩·퀘스트·그래프 참조 및 NPC별 순서 중복을 검사한다. 실제 우선순위와 기본 그래프 폴백은 PlayMode에서 상태별로 확인한다.

## 11. 수정·삭제 주의

순서 변경은 선택되는 대화를 바꿀 수 있다. 마지막 상태 라우트를 삭제해도 오류가 아니라 기본 그래프가 사용될 수 있으므로 의도치 않은 폴백을 점검한다.

## 12. 빠른 체크리스트

- [ ] NPC 기본 바인딩이 존재한다.
- [ ] 상태 문자열이 정확하다.
- [ ] 순서가 NPC 안에서 유일하고 우선순위와 일치한다.
- [ ] 퀘스트와 그래프가 존재한다.
- [ ] 각 상태와 기본 폴백을 플레이했다.

## 13. 관련 문서/코드

- [NPC 기본 바인딩](npc_dialogue_bindings_csv.md)
- [퀘스트](../PHASE_03_QUEST/quests_csv.md)
- [디자이너 워크플로](../PHASE_08_WORKFLOW_QA/DESIGNER_WORKFLOWS.md)
- [GameDataRepository.cs](../../../Scripts/Gameplay/Data/Repository/GameDataRepository.cs)
- [원본 CSV](../../../Data/Csv/npc_quest_dialogue_routes.csv)
