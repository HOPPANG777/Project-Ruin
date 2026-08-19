# dialogue_choices.csv

[README로 돌아가기](../README.md) · [Nodes](dialogue_nodes_csv.md) · [CodeList Master](../PHASE_00_FOUNDATION/CODELIST_MASTER.md)

## 1. 이 CSV의 목적

플레이어에게 보여줄 선택지, 표시 순서, 다음 Node, 실행 액션을 정의합니다. 한 행은 **선택지 버튼 한 개**입니다. 현재 4행입니다.

## 2. 언제 작성하는가

Node에서 분기, Quest 조작, Progress Flag 변경, 종료, Shop 열기가 필요할 때 작성합니다. 이 선택지를 가진 Node의 `next_node_id`는 비워야 합니다.

## 3. 정확한 헤더

```csv
graph_id,node_id,choice_order,choice_text_key,next_node_id,action_type,action_id
```

## 4. 열별 작성 규칙

| 열 이름 | 쉬운 설명 | 필수 여부 | 형식 | 입력 가능 값 | 참조 대상 | 올바른 예 |
| --- | --- | --- | --- | --- | --- | --- |
| `graph_id` | 선택지가 속한 Graph | 필수 | Graph 동적 참조 | 실제 Graph | Graph | `DLG_CURATOR_INTRO` |
| `node_id` | 선택지가 붙는 Node | 필수 | 같은 Graph Node | 실제 Node | Node | `NODE_START` |
| `choice_order` | 버튼 표시 순서 | 필수 | 0 이상 정수 | 같은 Node에서 중복 금지 | 정렬 | `0` |
| `choice_text_key` | 버튼 문장 Key | 필수 | Localization Key | 실제 Key | Localization | `dialogue.curator.choice.accept` |
| `next_node_id` | 선택 후 이동할 Node | 조건부 | 같은 Graph Node | 액션에 따라 빈칸 가능/필수 | Node | `NODE_ACCEPTED` |
| `action_type` | 선택 시 실행할 동작 | 필수 | 고정 9종 | 아래 표 | DialogueController | `ACCEPT_QUEST` |
| `action_id` | 액션의 대상 | 조건부 | 시스템 ID·동적 참조 | Flag, Quest, Shop 또는 빈칸 | 액션별 | `QUEST_RESTORE_MURAL` |

## 5. CodeList 및 입력 가능 값

| `action_type` | `action_id` | `next_node_id` |
| --- | --- | --- |
| `NONE` | 빈칸 | 선택 |
| `SET_PROGRESS_FLAG_TRUE` | Flag 시스템 ID 필수 | 선택 |
| `SET_PROGRESS_FLAG_FALSE` | Flag 시스템 ID 필수 | 선택 |
| `ACCEPT_QUEST` | 실제 Quest ID 필수 | 선택 |
| `REFRESH_QUEST_PROGRESS` | 실제 Quest ID 필수 | 선택 |
| `COMPLETE_QUEST` | 실제 Quest ID 필수 | 선택 |
| `CLAIM_QUEST_REWARD` | 실제 Quest ID 필수 | 선택 |
| `END_DIALOGUE` | 빈칸 | 빈칸 |
| `OPEN_SHOP` | 실제 Shop ID 필수 | 빈칸 |

`NONE` 이외의 일반 액션도 `next_node_id`를 비울 수 있으며, 액션 후 대화가 종료됩니다. `OPEN_SHOP`은 대화를 닫고 Shop 요청을 보내므로 다음 Node를 허용하지 않습니다.

## 6. 다른 CSV와의 연결

- 부모: `dialogue_nodes.csv`의 같은 Graph·Node
- 참조함: Localization, 같은 Graph 다음 Node, Quest, Shop
- Progress Flag 액션: 다른 CSV 목록이 아니라 게임플레이 코드와 합의한 시스템 ID

Flag 액션 후에는 진행 중 Quest 목표가 자동으로 다시 평가됩니다.

## 7. 작성 순서

1. 부모 Node의 `next_node_id`를 비웁니다.
2. 버튼 순서를 0부터 정하고 중복되지 않게 작성합니다.
3. 선택지 문장을 Localization에 추가합니다.
4. 9종 중 액션을 선택합니다.
5. 표에 따라 `action_id`와 `next_node_id`를 채우거나 비웁니다.
6. 목적지 Node·Quest·Shop이 실제로 존재하는지 확인합니다.

## 8. 올바른 예

```csv
graph_id,node_id,choice_order,choice_text_key,next_node_id,action_type,action_id
DLG_CURATOR_INTRO,NODE_START,0,dialogue.curator.choice.accept,NODE_ACCEPTED,ACCEPT_QUEST,QUEST_RESTORE_MURAL
DLG_CURATOR_INTRO,NODE_START,1,dialogue.curator.choice.decline,,END_DIALOGUE,
```

## 9. 잘못된 예

| 값 | 이유 |
| --- | --- |
| `action_type=SET_PROGRESS_FLAG` | 실제 9종에 없음 |
| `NONE` + `action_id=QUEST_X` | NONE은 대상 금지 |
| `END_DIALOGUE` + 다음 Node | 종료 액션은 두 대상 모두 빈칸 |
| `OPEN_SHOP` + 없는 Shop | Repository 참조 오류 |
| 같은 Node에 `choice_order=0` 두 개 | 중복 순서 |
| 다른 Graph의 다음 Node | 같은 Graph 안에서만 이동 가능 |

## 10. 자동 또는 Unity 검증 항목

- 부모 Node 존재, 같은 Node 내 순서 중복
- 문장 Key·다음 Node 존재
- 정확한 9종 액션
- NONE/END/OPEN_SHOP의 금지 필드
- Quest 액션의 Quest 존재, OPEN_SHOP의 Shop 존재
- 부모 Node에 기본 `next_node_id`가 없는지

## 11. 수정과 삭제 시 주의사항

액션 종류를 바꿀 때 `action_id`만 남겨 두는 실수가 가장 위험합니다. Quest·Shop ID 변경 시 Choice를 함께 수정합니다. `choice_order`를 바꾸면 버튼 표시 순서가 달라지지만 저장 ID로 사용되지는 않습니다.

## 12. 빠른 체크리스트

- [ ] 부모 Graph·Node가 존재하는가?
- [ ] 같은 Node의 순서가 중복되지 않는가?
- [ ] 선택지 Key가 Localization에 있는가?
- [ ] 액션이 정확한 9종 중 하나인가?
- [ ] 액션별 `action_id` 조합이 맞는가?
- [ ] END/OPEN_SHOP의 다음 Node가 비어 있는가?
- [ ] 부모 Node의 기본 다음 Node가 비어 있는가?

## 13. 관련 문서와 코드

- [Nodes](dialogue_nodes_csv.md)
- [대화 작업 흐름](../PHASE_08_WORKFLOW_QA/DESIGNER_WORKFLOWS.md#3-새-대화)
- [Dialogue 로더](../../../Scripts/Gameplay/Data/Csv/DialogueCsvDefinitionLoader.cs)
- [Repository 액션 검증](../../../Scripts/Gameplay/Data/Repository/GameDataRepository.cs)
- [Dialogue 실행](../../../Scripts/Gameplay/Dialogue/DialogueController.cs)
- [실제 CSV](../../../Data/Csv/dialogue_choices.csv)
