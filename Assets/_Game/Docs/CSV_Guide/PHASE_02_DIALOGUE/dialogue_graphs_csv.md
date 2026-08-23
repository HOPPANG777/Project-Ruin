# dialogue_graphs.csv

[README로 돌아가기](../README.md) · [Nodes](dialogue_nodes_csv.md) · [NPC Binding](../PHASE_04_NPC/npc_dialogue_bindings_csv.md)

## 1. 이 CSV의 목적

대화 흐름 한 개의 시작점과 기본 화자·좌우 참가자·배경을 정의합니다. 한 행은 **대화 그래프 한 개**입니다. 현재 3행입니다.

## 2. 언제 작성하는가

선형 대화나 선택지 대화를 새로 만들 때 Node보다 먼저 ID와 기본값을 설계하고, 실제 행 완성은 시작 Node가 준비된 뒤 합니다.

## 3. 정확한 헤더

```csv
graph_id,start_node_id,default_actor_id,left_actor_id,right_actor_id,background_id
```

## 4. 열별 작성 규칙

| 열 이름 | 쉬운 설명 | 필수 여부 | 형식 | 입력 가능 값 | 참조 대상 | 올바른 예 |
| --- | --- | --- | --- | --- | --- | --- |
| `graph_id` | 대화 흐름 고유 ID | 필수 | 시스템 ID | 전체 Graph에서 고유 | Node·NPC·Dialogue 시작 | `DLG_CURATOR_INTRO` |
| `start_node_id` | 처음 보여줄 Node | 필수 | 시스템 ID·동적 참조 | 같은 graph의 Node | `dialogue_nodes.csv` | `NODE_START` |
| `default_actor_id` | Node Actor가 비었을 때 쓸 화자 | 선택 | Actor 동적 참조 | 실제 Actor 또는 빈칸 | `dialogue_actors.csv` | `ACTOR_CURATOR` |
| `left_actor_id` | 왼쪽 참가자 | 선택 | Actor 동적 참조 | 실제 Actor 또는 빈칸 | 화면 배치 | `ACTOR_PLAYER` |
| `right_actor_id` | 오른쪽 참가자 | 선택 | Actor 동적 참조 | 실제 Actor 또는 빈칸 | 화면 배치 | `ACTOR_CURATOR` |
| `background_id` | Graph 기본 배경 | 선택 | 시스템 ID | Unity Catalog ID 또는 빈칸 | `DialogueVisualCatalogSO` | `BG_MUSEUM_HALL` |

## 5. CodeList 및 입력 가능 값

고정 enum은 없습니다. Graph·Node·Actor·Unity Catalog의 동적 목록을 사용합니다. `start_node_id`는 다른 Graph의 같은 이름 Node가 아니라 **같은 `graph_id`** 안의 Node여야 합니다.

## 6. 다른 CSV와의 연결

- 자식: `dialogue_nodes.csv`, 그 아래 `dialogue_choices.csv`
- 참조함: Actor 3열, 시작 Node, Unity Background
- 참조받음: NPC Binding 기본 Graph, NPC Route Graph, Dialogue 실행 요청

Graph를 삭제하면 NPC가 대화를 시작할 수 없고 Route 검증도 실패합니다.

## 7. 작성 순서

1. `DLG_...` Graph ID를 정합니다.
2. 기본·좌·우 Actor를 선택합니다.
3. 배경 ID가 필요하면 Unity 담당자와 합의합니다.
4. 같은 Graph의 Node를 작성합니다.
5. 시작 Node ID를 정확히 입력합니다.
6. NPC Binding/Route에 Graph를 연결합니다.

## 8. 올바른 예

```csv
graph_id,start_node_id,default_actor_id,left_actor_id,right_actor_id,background_id
DLG_CURATOR_INTRO,NODE_START,ACTOR_CURATOR,ACTOR_PLAYER,ACTOR_CURATOR,BG_MUSEUM_HALL
```

## 9. 잘못된 예

| 값 | 이유 |
| --- | --- |
| 없는 `start_node_id` | 같은 Graph에 시작 Node 필요 |
| 없는 Actor ID | Repository 참조 오류 |
| 중복 `graph_id` | 전체 Graph에서 고유해야 함 |
| `background_id=NONE` | 선택값은 빈칸; `NONE`은 배경 고정값이 아님 |

## 10. 자동 또는 Unity 검증 항목

- 필수 ID 형식, Graph ID 중복
- 시작 Node가 같은 Graph에 존재
- Actor 열이 비어 있거나 실제 Actor인지
- Editor 경고: 배경 ID가 Catalog에 없음

## 11. 수정과 삭제 시 주의사항

Graph ID는 Node·Choice·NPC 연결의 부모입니다. 변경 시 모든 연결을 함께 갱신합니다. 배경 Sprite만 바꿀 때는 `background_id`를 유지하고 Catalog의 에셋을 바꾸는 편이 안전합니다.

## 12. 빠른 체크리스트

- [ ] Graph ID가 고유한가?
- [ ] 시작 Node가 같은 Graph에 있는가?
- [ ] Actor ID가 모두 존재하는가?
- [ ] 기본 Actor를 비웠다면 각 Node의 Actor 사용을 검토했는가?
- [ ] 배경 ID를 썼다면 Catalog 연결을 요청했는가?
- [ ] NPC 연결이 필요한 Graph인가?

## 13. 관련 문서와 코드

- [Nodes](dialogue_nodes_csv.md)
- [NPC Binding](../PHASE_04_NPC/npc_dialogue_bindings_csv.md)
- [Dialogue 로더](../../../Scripts/Gameplay/Data/Csv/DialogueCsvDefinitionLoader.cs)
- [실제 CSV](../../../Data/Csv/dialogue_graphs.csv)

