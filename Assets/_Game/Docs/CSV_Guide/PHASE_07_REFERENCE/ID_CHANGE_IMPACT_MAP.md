# ID 변경 영향 지도

CSV의 ID 변경은 단순 문구 수정이 아니라 참조와 저장 상태의 계약 변경이다. 출시 후에는 가능하면 새 ID를 추가하고 구 ID 마이그레이션/호환 기간을 설계한다.

## 1. 핵심 영향표

| 변경 대상 | 반드시 검색/수정할 CSV | 추가 확인 대상 | 저장 영향 위험 |
|---|---|---|---:|
| `text_key` | 이를 쓰는 모든 표시 키 열 | UI 누락 문구 | 낮음 |
| `item_id` | rewards, consume, shops, stock | 인벤토리·상점 저장, 코드 참조 | 높음 |
| `actor_id` | portraits, graphs, nodes, binding | 장면/카탈로그 | 중간 |
| `portrait_id` | actors 기본 초상화, nodes | sprite 카탈로그 | 낮음~중간 |
| `graph_id` | nodes, choices, bindings, routes | 대화 진행 상태/장면 | 높음 |
| `node_id` | graph 시작, node/choice 다음, choice 부모 | 진행 중 대화 | 높음 |
| `quest_id` | objectives, rewards, consume, routes, dialogue action | 파생 퀘스트 상태 플래그 | 높음 |
| `objective_id` | 퀘스트 내부 데이터/분석 | 진행 판정 재검증 | 중간 |
| 진행 플래그 ID | objectives, rewards, dialogue flag action | 상호작용/코드/저장 플래그 | 높음 |
| `npc_id` | binding, routes | 장면 NPC 컴포넌트 | 높음 |
| `puzzle_id` | 종류별 상세 | 장면 트리거/퍼즐 저장 | 높음 |
| LIGHT `tile_id` | 모든 `affected_tile_ids` 목록 | 읽기용 매핑 | 중간 |
| `shop_id` | stock, OPEN_SHOP action | 상점 재고 저장 | 높음 |
| `technology_id` | codex entries, museum murals | 기술 획득·반환 Flag, 벽화 조건 | 높음 |
| `codex entry_id` | codex ingredients | 도감 구성 연결; 발견 상태는 source Item ID 사용 | 중간 |
| `bottle_id` | museum bottle items | 투입 목록·완성·보상 저장 | 높음 |
| `milestone_id` | 박물관 보상 연결 | 보상 1회 수령 Flag | 높음 |
| `altar_id` | 제단 UI·씬 컴포넌트 | 텔레포트 목적지 연결 | 중간~높음 |
| `mural_id` | 씬 벽화 컴포넌트 | 개봉 완료 Flag | 높음 |
| `spawn_point_id` | altar destinations | 실제 씬 이동 위치 | 중간~높음 |

## 2. 복합키 영향

- 초상화: `(actor_id, portrait_id)`
- 대화 노드: `(graph_id, node_id)`
- 선택지: `(graph_id, node_id, choice_order)`
- 퀘스트 목표/보상/소비의 유일성은 부모 퀘스트 내부 범위
- 퍼즐 좌표: `(puzzle_id,row,column)`
- 상점 재고: `(shop_id,item_id)` 및 상점 내부 `display_order`
- 도감 표시 순서: `(category,display_order)`
- 도감 재료: `(entry_id,ingredient_order)` 및 `(entry_id,item_id)`
- 술병 인정 목록: `(bottle_id,item_order)` 및 `(bottle_id,alcohol_item_id)`
- 유물 마일스톤 요구값: `required_exhibit_count`

복합키의 부모 부분을 바꾸면 자식 열을 그대로 두어도 다른 개체가 되거나 참조가 끊어진다.

## 3. 안전한 변경 절차

1. 변경 전 전체 프로젝트에서 기존 값을 정확 문자열로 검색한다.
2. 교차 참조 지도에서 대상의 부모·자식을 확인한다.
3. 저장 데이터에 쓰이는 ID인지 판단한다.
4. 저장 호환이 필요하면 코드/데이터 마이그레이션 승인을 먼저 받는다.
5. 부모와 모든 자식을 같은 변경 묶음에서 수정한다.
6. CSV 자동 검증을 실행한다.
7. 기존 저장 슬롯과 새 게임을 모두 확인한다.
8. 문서의 현재 예시/매핑도 갱신한다.

## 4. 삭제 절차

삭제하려는 행을 참조하는 자식이 하나라도 있으면 먼저 참조를 제거하거나 다른 유효 ID로 옮긴다. 퍼즐 그리드 행처럼 완전성을 요구하는 데이터는 단일 행 삭제가 허용되지 않는다. 퀘스트 목표 전부 삭제, 상점 재고 0, 대화 `next_node_id` 공백 등은 문법상 가능해도 의미가 크게 달라진다.

## 5. 검색 예시

프로젝트 루트에서 정확한 ID를 찾는다.

```powershell
rg -n --fixed-strings "QUEST_REPAIR_GATE" Assets/_Game
```

검색 결과를 기계적으로 모두 바꾸기 전에 각 열의 의미와 저장 호환성을 판별한다.

## 6. 관련 문서

- [교차 참조 지도](CROSS_REFERENCE_MAP.md)
- [공통 작성 규칙](../PHASE_00_FOUNDATION/COMMON_AUTHORING_RULES.md)
- [최종 체크리스트](../PHASE_08_WORKFLOW_QA/FINAL_CHECKLIST_AND_HANDOFF.md)
