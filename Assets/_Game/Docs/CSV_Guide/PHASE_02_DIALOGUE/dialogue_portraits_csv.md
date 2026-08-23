# dialogue_portraits.csv

[README로 돌아가기](../README.md) · [Actors](dialogue_actors_csv.md) · [Nodes](dialogue_nodes_csv.md)

## 1. 이 CSV의 목적

화자별 표정과 Unity Sprite 연결 ID를 정의합니다. 한 행은 **특정 화자의 표정 한 개**입니다. 파일은 필수이며 현재 4행입니다.

## 2. 언제 작성하는가

새 화자나 새 표정을 만들 때 Actor 다음에 작성합니다. 실제 이미지는 Unity 담당자가 `DialogueVisualCatalogSO`에 연결합니다.

## 3. 정확한 헤더

```csv
actor_id,portrait_id,display_name_key,sprite_id
```

## 4. 열별 작성 규칙

| 열 이름 | 쉬운 설명 | 필수 여부 | 형식 | 입력 가능 값 | 참조 대상 | 올바른 예 |
| --- | --- | --- | --- | --- | --- | --- |
| `actor_id` | 표정을 소유한 화자 | 필수 | 시스템 ID·동적 참조 | 실제 Actor ID | `dialogue_actors.csv` | `ACTOR_CURATOR` |
| `portrait_id` | 같은 화자 안의 표정 이름 | 필수 | 시스템 ID | 같은 Actor에서 중복 금지 | Actor 기본 표정·Node | `WORRIED` |
| `display_name_key` | 이 표정에서 이름을 덮어쓸 Key | 선택 | Localization Key | 실제 Key 또는 빈칸 | `localization.csv` | `actor.curator.title` |
| `sprite_id` | Unity에서 실제 초상화를 찾는 ID | 필수 | 시스템 ID | Catalog Portrait ID | `DialogueVisualCatalogSO` | `SPR_CURATOR_WORRIED` |

`sprite_id`는 이미지가 준비되지 않았더라도 CSV 행에서는 비울 수 없습니다. 임시 ID를 개발자와 합의하고 Catalog 연결 상태는 경고로 관리합니다.

## 5. CodeList 및 입력 가능 값

고정 enum은 없습니다.

- `actor_id`: Actor 동적 목록
- `portrait_id`: 기획자가 만드는 시스템 ID, 같은 Actor 범위에서 고유
- `display_name_key`: Localization 동적 목록 또는 빈칸
- `sprite_id`: Unity Catalog 동적 목록, 필수

현재 CURATOR Portrait: `NORMAL`, `WORRIED`, `HAPPY`; PLAYER: `NORMAL`.

## 6. 다른 CSV와의 연결

- 부모: `dialogue_actors.csv`
- 참조받음: Actor `default_portrait_id`, Node `portrait_id`
- Unity: `sprite_id` → Dialogue Visual Catalog Portrait 목록

Node에서 Portrait를 사용할 때는 Node의 실제 화자(노드 Actor 또는 Graph 기본 Actor)와 같은 화자 소유여야 합니다.

## 7. 작성 순서

1. 부모 Actor를 등록합니다.
2. 표정 의미가 드러나는 `portrait_id`를 정합니다.
3. 표정별 이름 변경이 없으면 `display_name_key`를 비웁니다.
4. `SPR_...` ID를 정하고 Unity 담당자에게 Sprite 연결을 요청합니다.
5. Actor 기본 표정 또는 Node에서 참조합니다.

## 8. 올바른 예

```csv
actor_id,portrait_id,display_name_key,sprite_id
ACTOR_CURATOR,WORRIED,,SPR_CURATOR_WORRIED
```

## 9. 잘못된 예

| 값 | 이유 |
| --- | --- |
| 없는 `actor_id` | 부모 Actor 없음 |
| 같은 Actor에 `NORMAL` 두 행 | 복합키 중복 |
| 빈 `sprite_id` | 런타임 필수값 |
| `sprite_id=curator.png` | 시스템 ID 형식이 아니며 경로 입력 열이 아님 |

## 10. 자동 또는 Unity 검증 항목

- 부모 Actor 존재, Portrait ID 형식과 같은 Actor 내 중복
- `display_name_key`가 있으면 Localization 존재
- `sprite_id` 필수 시스템 ID
- Editor 경고: Catalog에 `sprite_id`가 없거나 Sprite 미연결

## 11. 수정과 삭제 시 주의사항

`portrait_id`를 변경하면 Actor 기본 표정과 모든 Node 참조를 함께 바꿉니다. `sprite_id`만 유지하고 Sprite 에셋을 교체하면 CSV 참조는 유지됩니다.

## 12. 빠른 체크리스트

- [ ] 부모 Actor가 있는가?
- [ ] 같은 Actor 안에서 Portrait ID가 고유한가?
- [ ] `sprite_id`가 비어 있지 않은가?
- [ ] 이름 Key를 썼다면 Localization에 있는가?
- [ ] Node의 실제 Actor와 Portrait 소유자가 같은가?
- [ ] Unity Catalog 연결을 확인했는가?

## 13. 관련 문서와 코드

- [Actors](dialogue_actors_csv.md)
- [Nodes](dialogue_nodes_csv.md)
- [Dialogue 로더](../../../Scripts/Gameplay/Data/Csv/DialogueCsvDefinitionLoader.cs)
- [Editor 검증기](../../../Scripts/Editor/GameDataEditorValidator.cs)
- [실제 CSV](../../../Data/Csv/dialogue_portraits.csv)

