# npc_dialogue_bindings.csv 작성 가이드

## 1. 목적

상호작용 NPC에 기본 대화 그래프와 선택적 배우 오버라이드를 연결한다.

## 2. 언제 작성하는가

대화 그래프를 완성한 뒤 NPC가 대화를 시작해야 할 때 작성한다. 런타임 입력은 선택 사항이므로 기능을 쓰지 않으면 헤더만 유지할 수 있다.

## 3. 정확한 헤더

```csv
npc_id,default_graph_id,actor_override_id
```

## 4. 열 규칙

| 열 | 필수 | 형식/검증 |
|---|---:|---|
| `npc_id` | 예 | 시스템 ID, 전역 유일 바인딩 |
| `default_graph_id` | 예 | 존재하는 대화 그래프 ID |
| `actor_override_id` | 아니요 | 비우거나 존재하는 배우 ID |

## 5. CodeList

고정 enum은 없다. 세 ID 모두 시스템 ID 형식을 사용한다.

## 6. 연결 관계

`dialogue_graphs.csv`와 선택적으로 `dialogue_actors.csv`를 참조한다. `npc_quest_dialogue_routes.csv`의 `npc_id`는 반드시 이 파일에 바인딩이 있어야 한다. 실제 장면의 NPC 식별자와의 일치는 PlayMode 확인 대상이다.

## 7. 작성 순서

1. NPC의 실제 ID를 확인한다.
2. 항상 사용할 기본 그래프를 정한다.
3. 필요할 때만 배우 오버라이드를 넣는다.
4. 상태별 그래프가 필요하면 라우트 CSV를 추가한다.
5. 해당 NPC와 상호작용한다.

## 8. 유효 예시

```csv
NPC_ENGINEER,DIALOGUE_ENGINEER_DEFAULT,ACTOR_ENGINEER
```

## 9. 잘못된 예시

| 행/값 | 이유 |
|---|---|
| 동일 `NPC_ENGINEER` 두 행 | NPC 바인딩은 전역 유일 |
| `default_graph_id=DIALOGUE_MISSING` | 존재하지 않는 그래프 |
| `actor_override_id=ACTOR_MISSING` | 존재하지 않는 배우 |

## 10. 자동/Unity 검증

로더가 헤더와 ID 형식을, 저장소가 그래프/배우 참조 및 NPC 중복을 검사한다. 이 TextAsset 자체가 미연결이면 선택 입력으로 취급되어 로딩이 계속되므로, `GameRoot` 연결과 실제 NPC 상호작용을 별도로 확인한다.

## 11. 수정·삭제 주의

바인딩 삭제 시 해당 NPC는 기본 대화를 찾지 못한다. `npc_id` 변경은 모든 상태 라우트와 장면의 NPC ID를 함께 바꿔야 한다.

## 12. 빠른 체크리스트

- [ ] NPC ID가 바인딩에서 유일하다.
- [ ] 기본 그래프가 존재한다.
- [ ] 오버라이드 배우가 존재하거나 비어 있다.
- [ ] 상태 라우트의 NPC가 이 파일에 있다.
- [ ] GameRoot 연결과 실제 상호작용을 확인했다.

## 13. 관련 문서/코드

- [대화 그래프](../PHASE_02_DIALOGUE/dialogue_graphs_csv.md)
- [NPC 상태 라우트](npc_quest_dialogue_routes_csv.md)
- [GameDataController.cs](../../../Scripts/Gameplay/Data/GameDataController.cs)
- [GameDataRepository.cs](../../../Scripts/Gameplay/Data/Repository/GameDataRepository.cs)
- [원본 CSV](../../../Data/Csv/npc_dialogue_bindings.csv)
