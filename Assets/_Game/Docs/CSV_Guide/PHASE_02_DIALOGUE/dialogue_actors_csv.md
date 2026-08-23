# dialogue_actors.csv

[README로 돌아가기](../README.md) · [Portraits](dialogue_portraits_csv.md) · [Dialogue 작업 흐름](../PHASE_08_WORKFLOW_QA/DESIGNER_WORKFLOWS.md#3-새-대화)

## 1. 이 CSV의 목적

대화에 등장하는 화자를 등록합니다. 한 행은 **화자 한 명**입니다. 표시 이름과 기본 표정을 정하며, 없으면 Dialogue 전체 로드가 실패합니다. 현재 화자는 `ACTOR_PLAYER`, `ACTOR_CURATOR` 두 명입니다.

## 2. 언제 작성하는가

새 인물이 처음 대화에 등장하기 전에 작성합니다. 이름 Localization을 먼저 만들고, 기본 표정을 사용할 경우 Portrait 행을 함께 준비합니다.

## 3. 정확한 헤더

```csv
actor_id,display_name_key,default_portrait_id
```

## 4. 열별 작성 규칙

| 열 이름 | 쉬운 설명 | 필수 여부 | 형식 | 입력 가능 값 | 참조 대상 | 올바른 예 |
| --- | --- | --- | --- | --- | --- | --- |
| `actor_id` | 화자를 찾는 고유 ID | 필수 | 시스템 ID | 전체 Actor에서 중복 금지 | Portrait·Graph·Node·NPC | `ACTOR_CURATOR` |
| `display_name_key` | 화면에 보일 화자 이름 Key | 필수 | Localization Key | 실제 `text_key` | `localization.csv` | `actor.curator.name` |
| `default_portrait_id` | 별도 표정을 정하지 않았을 때 쓸 표정 | 선택 | 시스템 ID | 같은 actor의 실제 `portrait_id` | `dialogue_portraits.csv` | `NORMAL` |

세 열 모두 기획자가 작성합니다. Sprite 연결은 Portrait 문서와 Unity Catalog에서 처리합니다.

## 5. CodeList 및 입력 가능 값

고정 enum은 없습니다.

- `actor_id`: 시스템 ID
- `display_name_key`: `localization.csv.text_key` 동적 목록
- `default_portrait_id`: 같은 `actor_id`에 등록된 Portrait 동적 목록 또는 빈칸

현재 목록: `ACTOR_PLAYER`, `ACTOR_CURATOR`; 두 화자의 기본 표정은 모두 `NORMAL`입니다.

## 6. 다른 CSV와의 연결

- 부모: Portrait가 `actor_id`로 참조
- 참조받음: Graph의 세 Actor 열, Node `actor_id`, NPC `actor_override_id`
- 참조함: Localization 이름, 같은 화자의 기본 Portrait

Actor ID를 지우면 연결된 모든 대화가 깨질 수 있습니다.

## 7. 작성 순서

1. `actor.<name>.name` Key와 `ko` 문장을 Localization에 추가합니다.
2. `ACTOR_...` ID를 만듭니다.
3. Portrait 행을 먼저 계획합니다.
4. 기본 표정이 있으면 그 `portrait_id`를 입력하고, 없으면 빈칸으로 둡니다.
5. Graph·Node에서 Actor를 연결합니다.

## 8. 올바른 예

```csv
actor_id,display_name_key,default_portrait_id
ACTOR_CURATOR,actor.curator.name,NORMAL
```

## 9. 잘못된 예

| 값 | 이유 |
| --- | --- |
| `actor_curator` | 시스템 ID 형식 오류 |
| 없는 이름 Key | Localization 참조 오류 |
| `default_portrait_id=ANGRY`인데 Portrait 행 없음 | 같은 Actor의 기본 표정을 찾지 못함 |
| 같은 `actor_id` 두 행 | 중복 금지 |

## 10. 자동 또는 Unity 검증 항목

- 세 열 헤더, 필수값과 ID/Key 형식
- Actor ID 전체 중복
- 표시 이름 Key 존재
- 기본 Portrait가 같은 Actor 안에 존재

## 11. 수정과 삭제 시 주의사항

`actor_id` 변경은 Portrait, Graph, Node, NPC Binding에 연쇄 영향을 줍니다. 이름 문장만 수정할 때는 Key 대신 Localization의 문장 값을 바꾸는 편이 안전합니다.

## 12. 빠른 체크리스트

- [ ] Actor ID가 고유한가?
- [ ] 이름 Key가 Localization에 있는가?
- [ ] 기본 표정이 같은 Actor의 Portrait인가?
- [ ] 새 Actor를 사용하는 Graph·Node를 함께 확인했는가?
- [ ] 빈 선택값에 가짜 문자열을 쓰지 않았는가?

## 13. 관련 문서와 코드

- [Portrait 문서](dialogue_portraits_csv.md)
- [교차 참조 지도](../PHASE_07_REFERENCE/CROSS_REFERENCE_MAP.md)
- [Dialogue 로더](../../../Scripts/Gameplay/Data/Csv/DialogueCsvDefinitionLoader.cs)
- [실제 CSV](../../../Data/Csv/dialogue_actors.csv)
