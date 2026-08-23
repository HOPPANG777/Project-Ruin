# dialogue_nodes.csv

[README로 돌아가기](../README.md) · [Graphs](dialogue_graphs_csv.md) · [Choices](dialogue_choices_csv.md)

## 1. 이 CSV의 목적

대화 한 장면의 화자, 표정, 배경, 본문 문장, 다음 흐름을 정의합니다. 한 행은 **대사 Node 한 개**입니다. 현재 4행입니다.

## 2. 언제 작성하는가

Graph를 설계한 뒤 대사 순서대로 작성합니다. 선택지가 붙을 Node는 `next_node_id`를 비워 두고 Choice 행으로 분기합니다.

## 3. 정확한 헤더

```csv
graph_id,node_id,actor_id,portrait_id,background_id,body_text_key,next_node_id
```

## 4. 열별 작성 규칙

| 열 이름 | 쉬운 설명 | 필수 여부 | 형식 | 입력 가능 값 | 참조 대상 | 올바른 예 |
| --- | --- | --- | --- | --- | --- | --- |
| `graph_id` | 소속 대화 흐름 | 필수 | Graph 동적 참조 | 실제 Graph | `dialogue_graphs.csv` | `DLG_CURATOR_INTRO` |
| `node_id` | 같은 Graph 안의 장면 ID | 필수 | 시스템 ID | 같은 Graph에서 고유 | Graph 시작·Node·Choice | `NODE_START` |
| `actor_id` | 이 대사의 화자 | 선택 | Actor 동적 참조 | Actor 또는 빈칸 | 빈칸이면 Graph 기본 Actor | `ACTOR_CURATOR` |
| `portrait_id` | 실제 화자의 표정 | 선택·조건부 | 시스템 ID·동적 참조 | 해당 화자의 Portrait | `dialogue_portraits.csv` | `WORRIED` |
| `background_id` | 이 Node에서 덮어쓸 배경 | 선택 | 시스템 ID | Catalog ID 또는 빈칸 | Unity Catalog | `BG_CLOSEUP` |
| `body_text_key` | 화면에 보일 대사 Key | 필수 | Localization Key | 실제 Key | `localization.csv` | `dialogue.curator.intro.001` |
| `next_node_id` | 선택지가 없을 때 다음 Node | 선택·조건부 | 같은 Graph Node | 실제 Node 또는 빈칸 | 비면 대화 종료 | `NODE_ACCEPTED` |

`portrait_id`를 쓰려면 실제 화자가 결정되어야 합니다. Node `actor_id`가 비면 Graph `default_actor_id`가 실제 화자입니다. 둘 다 비어 있는데 Portrait만 쓰면 참조 오류입니다.

## 5. CodeList 및 입력 가능 값

고정 enum은 없습니다. 모든 참조는 Graph·Actor·Portrait·Localization의 동적 목록입니다.

`next_node_id` 규칙:

- Choice가 없는 Node: 같은 Graph의 Node 또는 빈칸
- Choice가 하나 이상 붙는 Node: 반드시 빈칸

## 6. 다른 CSV와의 연결

- 부모: `dialogue_graphs.csv`
- 자식: `dialogue_choices.csv`
- 참조함: Actor, Portrait, Localization, Unity Background, 같은 Graph의 다음 Node
- 참조받음: Graph `start_node_id`, 다른 Node·Choice의 `next_node_id`

## 7. 작성 순서

1. Graph ID를 선택합니다.
2. 같은 Graph 안에서 고유한 Node ID를 만듭니다.
3. 화자와 표정을 결정합니다.
4. 본문 Localization Key를 추가합니다.
5. 선형이면 다음 Node를, 종료면 빈칸을 씁니다.
6. 분기 Node면 `next_node_id`를 비우고 Choice를 작성합니다.

## 8. 올바른 예

```csv
graph_id,node_id,actor_id,portrait_id,background_id,body_text_key,next_node_id
DLG_CURATOR_INTRO,NODE_START,ACTOR_CURATOR,WORRIED,,dialogue.curator.intro.001,
```

현재 `NODE_START`에는 Choice가 있으므로 마지막 열이 빈칸입니다.

## 9. 잘못된 예

| 값 | 이유 |
| --- | --- |
| 없는 Graph | 부모 없음 |
| 같은 Graph에서 Node ID 중복 | 복합키 중복 |
| Choice가 있는데 `next_node_id`도 입력 | 두 기본 진행 경로 충돌 |
| CURATOR Node에 PLAYER 전용 Portrait | 실제 화자의 Portrait가 아님 |
| 없는 본문 Key | Localization 참조 오류 |

## 10. 자동 또는 Unity 검증 항목

- 부모 Graph 존재, 같은 Graph 내 Node ID 중복
- 본문 Key 존재
- 실제 Actor 존재
- Portrait가 실제 Actor 소유인지
- `next_node_id`가 같은 Graph에 존재하는지
- Choice가 있는 Node의 기본 다음 Node 금지
- Editor 경고: Node 배경 ID가 Catalog에 없음

## 11. 수정과 삭제 시 주의사항

Node ID 변경은 Graph 시작점, 이전 Node, Choice의 다음 Node에 영향을 줍니다. Node 삭제 전에 누가 시작·이동 대상으로 참조하는지 확인합니다.

## 12. 빠른 체크리스트

- [ ] 부모 Graph가 있는가?
- [ ] Node ID가 같은 Graph에서 고유한가?
- [ ] 실제 화자와 Portrait가 일치하는가?
- [ ] 본문 Key가 Localization에 있는가?
- [ ] 다음 Node가 같은 Graph에 있는가?
- [ ] Choice Node의 `next_node_id`가 비어 있는가?
- [ ] 마지막 Node는 의도적으로 종료되는가?

## 13. 관련 문서와 코드

- [Graphs](dialogue_graphs_csv.md)
- [Choices](dialogue_choices_csv.md)
- [Dialogue 로더](../../../Scripts/Gameplay/Data/Csv/DialogueCsvDefinitionLoader.cs)
- [Repository 참조 검증](../../../Scripts/Gameplay/Data/Repository/GameDataRepository.cs)
- [실제 CSV](../../../Data/Csv/dialogue_nodes.csv)

