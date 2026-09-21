# museum_relic_milestones.csv

[도감·박물관 가이드](README.md) · [도감 항목](codex_entries_csv.md) · [items.csv](../PHASE_01_LOCALIZATION_ITEMS/items_csv.md)

> 상태: **Phase 7 보완 검증 완료 / 콘텐츠 미확정**. 2026-08-31 최종 방 테스트 38/38, 기존 관련 회귀 54/54 통과. 실제 입력 경로와 별도 ES3 파일 재로드도 검증했습니다. 보상 Item ID와 수량은 아직 확정되지 않았으므로 실제 CSV는 헤더만 유지합니다. 실제 방 배치는 콘텐츠 확정 후 수행합니다.

## 1. 이 CSV의 목적

서로 다른 유물 종류를 실제 전시한 개수에 따라 한 번만 해제되는 박물관 보상을 정의합니다. 한 행은 **유물 전시 마일스톤 한 개**입니다. 전역 업적 시스템을 새로 만드는 대신 박물관에 필요한 작은 보상 목록만 관리합니다.

## 2. 언제 작성하는가

유물 10종 전시 보상, 모든 유물 전시 후 최종 방 수령 보상처럼 정확한 요구 개수와 Item이 확정됐을 때 작성합니다. 미확정 상태에서는 가짜 행을 만들지 않습니다.

## 3. 정확한 헤더

```csv
milestone_id,required_exhibit_count,reward_mode,reward_item_id,reward_amount
```

## 4. 열별 작성 규칙

| 열 이름 | 쉬운 설명 | 필수 여부 | 형식 | 입력 가능 값 | 참조 대상 | 올바른 예 |
| --- | --- | --- | --- | --- | --- | --- |
| `milestone_id` | 보상과 1회 수령 상태의 ID | 필수 | 시스템 ID | 전체 마일스톤에서 고유 | 보상 지급 Flag | `MILESTONE_EXAMPLE_RELIC_10` |
| `required_exhibit_count` | 필요한 서로 다른 전시 유물 수 | 필수 | 1 이상 정수 | 유물 도감 전체 수 이하, 중복 금지 | 전시 상태 | `10` |
| `reward_mode` | 보상을 받는 방식 | 필수 | 고정 CodeList | 아래 2종 | 박물관 Controller | `GRANT_IMMEDIATELY` |
| `reward_item_id` | 지급할 Item ID | 필수 | 시스템 ID | 실제 Item ID | `items.csv` | `ITEM_EXAMPLE_REWARD` |
| `reward_amount` | 지급 수량 | 필수 | 1 이상 정수 | 1 이상 | Inventory | `1` |

## 5. CodeList 및 입력 가능 값

| `reward_mode` | 의미 | 사용 예 |
| --- | --- | --- |
| `GRANT_IMMEDIATELY` | 조건을 처음 충족하면 즉시 지급을 시도 | 유물 10종 전시 보상 |
| `ENABLE_ROOM_PICKUP` | 열린 방 내부에서 상호작용 시 전시 조건을 재검증하고 한 번 지급 | 모든 유물 전시 후 왕의 보검 |

- `required_exhibit_count`는 배치된 전시대 수나 중복 복사본 수가 아니라 **서로 다른 실제 전시 유물 종류 수**입니다.
- 최대값은 `codex_entries.csv`의 `RELIC` 항목 수에서 계산합니다. 20을 코드에 별도로 하드코딩하지 않습니다.
- 최종 방 문 개방은 모든 유물 **발견** 조건이며 이 CSV의 전시 마일스톤과 다릅니다.
- 모든 열이 필수입니다. 보상이 미확정이면 행 전체를 만들지 않습니다.
- 현재 검증 정책은 **헤더만 있거나 보상 2행**입니다. 첫 행은 10종·즉시 지급, 둘째 행은 전체 유물 수·방 안 수령이어야 합니다. 한 행만 작성하거나 임의의 중간 보상 행을 추가하면 검증에 실패합니다.

## 6. 다른 CSV와의 연결

- 참조함: `reward_item_id` → `items.csv`
- 계산 원본: 유물 전체 수 → `codex_entries`의 `RELIC` 행, 현재 전시 종류 수 → 전시대 저장 상태
- 저장: `museum.relic_displays.v1` System State의 `milestone_id`별 수령 완료 목록
- 씬: `ENABLE_ROOM_PICKUP` 행 ID를 `MuseumRewardRoom`의 Reward Milestone Id에 연결합니다. [Inspector 연결 순서](README.md#8-최종-보상-방-연결--unity-담당자)를 따릅니다.
- 문 개방은 별도 `museum.reward_room.open.v1` Flag입니다. 이 CSV에 개방 여부 열을 추가하지 않습니다.
- 개인 테스트용 벽화 관련 퀘스트는 문 개방이나 보상 수령의 필수 조건이 아니므로 Quest ID를 연결하지 않습니다.

## 7. 작성 순서

1. 유물 도감 전체 목록을 확정합니다.
2. 정확한 보상 Item과 수량을 먼저 확정합니다.
3. 즉시 지급인지 방 안 수령인지 정합니다.
4. 낮은 요구 개수부터 행을 작성합니다.
5. 최종 방 수령 행의 요구 개수를 유물 전체 수와 맞춥니다.
6. 보상 중복 수령과 지급 실패 재시도를 테스트합니다.

## 8. 올바른 예

아래는 형식 설명용이며 실제 보상 데이터가 아닙니다.

```csv
milestone_id,required_exhibit_count,reward_mode,reward_item_id,reward_amount
MILESTONE_EXAMPLE_RELIC_10,10,GRANT_IMMEDIATELY,ITEM_EXAMPLE_REWARD,1
MILESTONE_EXAMPLE_RELIC_ALL,20,ENABLE_ROOM_PICKUP,ITEM_EXAMPLE_KING_SWORD,1
```

실제 유물 총수가 20으로 최종 확정된 경우에만 두 번째 요구값 20을 사용합니다.

## 9. 잘못된 예

| 잘못된 값 | 이유 |
| --- | --- |
| 보상 미정인데 `ITEM_TODO` 행 작성 | 가짜 참조가 됨; 행을 만들지 않아야 함 |
| `required_exhibit_count=0` | 1 이상이어야 함 |
| 유물 전체 수보다 큰 요구값 | 달성 불가능 |
| 같은 요구 개수 두 행 | 같은 경계에서 중복 지급 위험 |
| `reward_mode=OPEN_ROOM` | 허용 CodeList가 아님 |
| 왕의 보검을 `GRANT_IMMEDIATELY` | 방 안에서 수령한다는 정책 위반 |

## 10. 자동 또는 Unity 검증 항목

`MuseumRelicMilestoneRepository`는 헤더, 모든 필수값, ID·요구 개수 중복, 유물 총수 범위, 모드 2종, Item 존재, 수량 1 이상, 오름차순을 검사합니다. `MuseumPhase6EditorValidator`는 GameRoot·Bazzi UI·전시대 개수와 고유 ID를 빌드 전에 검사합니다. 지급 성공 전에는 수령 완료 상태를 저장하지 않으며 즉시 보상 실패는 재시도됩니다.

검증 메뉴는 CSV·프리팹·씬만 검사하며 보상 지급이나 플레이어 세이브 저장을 수행하지 않습니다. 유물·보상·전시대가 아직 없으면 `기반 검증 통과 / 콘텐츠 미완성`으로 표시합니다. 인벤토리와 수령 상태는 함께 적용되며, 디스크 저장 실패는 Dirty를 유지하는 별도 재저장 대상입니다.

실행 메뉴: `Tools > Project Ruin > Museum > Validate Phase 6 Data`

최종 방 메뉴: `Tools > Project Ruin > Museum > Validate Phase 7 Data`. 이 메뉴는 CSV·GameRoot·방 배치 참조를 검사합니다. 문 개방, 방 밖 수령 차단, 전체 전시 재확인, 중복·재진입·저장 복원은 별도 Phase 7 자동 테스트에서 검증했습니다. 보검 Item을 소비해도 수령 완료 목록은 유지하며, 이미 받은 보상을 다시 지급하지 않습니다.

## 11. 수정과 삭제 시 주의사항

`milestone_id`를 바꾸면 이미 받은 보상을 다시 받을 수 있으므로 저장 마이그레이션이 필수입니다. 요구 개수를 낮추거나 새 중간 행을 추가하려면 먼저 10종/전체 2행 검증 정책과 소급 지급 정책을 함께 변경해야 합니다. 보상 Item을 바꿔도 이미 수령한 Flag는 유지하는 것이 기본입니다.

## 12. 빠른 체크리스트

- [ ] 보상 미확정 행을 만들지 않았는가?
- [ ] 정확한 5열 헤더와 모든 필수값이 있는가?
- [ ] 요구 개수가 고유하고 유물 전체 수 이하인가?
- [ ] 보상 모드가 2종 중 하나인가?
- [ ] Item과 수량이 유효한가?
- [ ] 최종 보상은 `ENABLE_ROOM_PICKUP`인가?
- [ ] 발견 기반 문 개방과 전시 기반 보상 수령을 구분했는가?

## 13. 관련 문서와 코드

- [도감 Entry CSV](codex_entries_csv.md)
- [items.csv](../PHASE_01_LOCALIZATION_ITEMS/items_csv.md)
- [ID 변경 영향](../PHASE_07_REFERENCE/ID_CHANGE_IMPACT_MAP.md)
- [Progress 저장 가이드](../../ES3_PROGRESS_SAVE_GUIDE.md)
