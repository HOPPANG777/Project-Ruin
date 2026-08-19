# Project Ruin CodeList Master

[README로 돌아가기](../README.md) · [공통 작성 규칙](COMMON_AUTHORING_RULES.md)

CodeList는 드롭다운 enum만 뜻하지 않습니다. 이 문서에서는 입력 제한을 고정값, 다른 CSV의 동적 ID, 숫자 범위, Boolean, 복합값으로 나눕니다.

## 1. 고정 CodeList

| CSV | 열 | 허용 값 | 의미 | 상세 문서 |
| --- | --- | --- | --- | --- |
| `dialogue_choices.csv` | `action_type` | 아래 9종 | 선택 시 실행할 동작 | [열기](../PHASE_02_DIALOGUE/dialogue_choices_csv.md) |
| `items.csv` | `item_type` | 아래 6종 | 아이템 용도·인벤토리 분류 | [열기](../PHASE_01_LOCALIZATION_ITEMS/items_csv.md) |
| `npc_quest_dialogue_routes.csv` | `quest_state` | 아래 4종 | Route가 적용될 퀘스트 상태 | [열기](../PHASE_04_NPC/npc_quest_dialogue_routes_csv.md) |
| `puzzles.csv` | `puzzle_type` | `PIPE`, `LIGHT`, `CIRCLE` | 상세 자식 CSV 종류 | [열기](../PHASE_05_PUZZLE/puzzles_csv.md) |
| `puzzle_pipe_tiles.csv` | `tile_type` | 아래 7종 | 파이프 칸 모양 | [열기](../PHASE_05_PUZZLE/puzzle_pipe_tiles_csv.md) |
| `puzzle_pipe_tiles.csv` | `rotation` | `0`, `1`, `2`, `3` | 시계 방향 90도 회전 횟수 | [열기](../PHASE_05_PUZZLE/puzzle_pipe_tiles_csv.md) |
| `codex_entries.csv` | `category` | `RELIC`, `ALCOHOL`, `TECHNOLOGY` | 도감 화면 분류 | [열기](../PHASE_09_CODEX_MUSEUM/codex_entries_csv.md) |
| `museum_relic_milestones.csv` | `reward_mode` | `GRANT_IMMEDIATELY`, `ENABLE_ROOM_PICKUP` | 전시 보상 수령 방식 | [열기](../PHASE_09_CODEX_MUSEUM/museum_relic_milestones_csv.md) |

### DialogueChoices.action_type 9종

| 값 | `action_id` | `next_node_id` | 동작 |
| --- | --- | --- | --- |
| `NONE` | 반드시 빈칸 | 선택 | 별도 동작 없이 다음 노드로 이동. 다음 노드도 비면 종료 |
| `SET_PROGRESS_FLAG_TRUE` | Progress Flag 시스템 ID 필수 | 선택 | Flag를 `true`로 변경하고 진행 중 퀘스트를 갱신 |
| `SET_PROGRESS_FLAG_FALSE` | Progress Flag 시스템 ID 필수 | 선택 | Flag를 `false`로 변경하고 진행 중 퀘스트를 갱신 |
| `ACCEPT_QUEST` | 존재하는 `quest_id` 필수 | 선택 | 미시작 퀘스트 수락 |
| `REFRESH_QUEST_PROGRESS` | 존재하는 `quest_id` 필수 | 선택 | 목표 조건을 다시 확인 |
| `COMPLETE_QUEST` | 존재하는 `quest_id` 필수 | 선택 | 목표와 소모 아이템 검사 후 완료 |
| `CLAIM_QUEST_REWARD` | 존재하는 `quest_id` 필수 | 선택 | 완료 처리와 보상 지급 시도 |
| `END_DIALOGUE` | 반드시 빈칸 | 반드시 빈칸 | 즉시 대화 종료 |
| `OPEN_SHOP` | 존재하는 `shop_id` 필수 | 반드시 빈칸 | 대화를 닫고 상점 열기 요청 |

`SET_PROGRESS_FLAG`는 허용 값이 아닙니다. TRUE/FALSE 중 하나를 정확히 선택합니다.

### Items.item_type 6종

| 값 | 기획 의미 |
| --- | --- |
| `ITEM_QUEST` | 퀘스트 조건·전달·진행용 |
| `ITEM_ALCOHOL_COLLECTIBLE` | 술 수집품 |
| `ITEM_ALCOHOL_TOOL` | 술 제작 도구 |
| `ITEM_RELIC_COLLECTIBLE` | 유물 수집품 |
| `ITEM_RELIC_TOOL` | 유물 작업 도구 |
| `ITEM_MISC` | 그 밖의 일반 아이템 |

### NPC Route quest_state 4종

| 값 | 의미 |
| --- | --- |
| `QUEST_NOT_STARTED` | 아직 수락하지 않음 |
| `QUEST_IN_PROGRESS` | 수락 후 진행 중 |
| `QUEST_COMPLETED` | 완료됐지만 보상 수령 전 |
| `QUEST_REWARDED` | 보상 수령까지 끝남 |

### PuzzlePipeTiles.tile_type 7종

| 값 | 의미 | 코드 기본 연결 방향(rotation 0) |
| --- | --- | --- |
| `EMPTY` | 빈 칸 | 없음 |
| `START` | 물 시작점 | 위 |
| `END` | 물 도착점 | 위 |
| `STRAIGHT` | 직선 | 위·아래 |
| `CORNER` | 꺾임 | 왼쪽·아래 |
| `T_JUNCTION` | 세 방향 | 왼쪽·오른쪽·아래 |
| `CROSS` | 네 방향 | 위·오른쪽·아래·왼쪽 |

`rotation`은 0=기본, 1=시계 90도, 2=시계 180도, 3=시계 270도입니다. 모든 결과는 [파이프 방향표](../PHASE_05_PUZZLE/puzzle_pipe_tiles_csv.md#5-codelist)에 있습니다.

## 2. Boolean CodeList

| CSV | 열 | 입력 가능 값 | 필수 여부 |
| --- | --- | --- | --- |
| `items.csv` | `can_sell` | `true`, `false` | 필수 |
| `quest_objectives.csv` | `required_flag_value` | `true`, `false` | 필수 |
| `quest_rewards.csv` | `flag_value` | 빈칸, `true`, `false` | `progress_flag_id`가 있으면 필수 |
| `puzzle_light_tiles.csv` | `is_on` | `true`, `false` | 필수 |
| `altar_destinations.csv` | `is_main_altar` | `true`, `false` | 필수 |

대문자, 숫자, O/X 표기는 허용되지 않습니다.

## 3. 범위형 CodeList

| CSV | 열 | 코드상 범위 | 중복·연속 조건 |
| --- | --- | --- | --- |
| `items.csv` | `price` | 0 이상 | `can_sell=true`이면 1 이상 |
| `items.csv` | `max_stack_count` | 1 이상 | 없음 |
| `dialogue_choices.csv` | `choice_order` | 0 이상 | 같은 graph+node에서 중복 금지; 연속은 권장 |
| `quest_objectives.csv` | `objective_order` | 0 이상 | 같은 quest에서 중복 금지; 연속은 권장 |
| `quest_rewards.csv` | `reward_order` | 0 이상 | 같은 quest에서 중복 금지; 연속은 권장 |
| `quest_rewards.csv` | `item_amount` | 빈칸 또는 1 이상 | `item_id`와 함께 입력 |
| `quest_consume_items.csv` | `consume_order` | 0 이상 | 같은 quest에서 중복 금지; 연속은 권장 |
| `quest_consume_items.csv` | `item_amount` | 1 이상 | 필수 |
| `npc_quest_dialogue_routes.csv` | `route_order` | 0 이상 | 같은 NPC에서 중복 금지; 낮은 값부터 검사 |
| `puzzles.csv` | `rows`, `columns` | PIPE/LIGHT: 각각 1~100; CIRCLE: 둘 다 0 | 부모 크기 |
| `puzzle_pipe_tiles.csv` | `row` | 0~`rows-1` | 좌표 중복 금지, 모든 칸 필요 |
| `puzzle_pipe_tiles.csv` | `column` | 0~`columns-1` | 좌표 중복 금지, 모든 칸 필요 |
| `puzzle_pipe_tiles.csv` | `rotation` | 0~3 | 필수 |
| `puzzle_light_tiles.csv` | `tile_id` | 1 이상 | 같은 puzzle에서 중복 금지 |
| `puzzle_light_tiles.csv` | `row` | 0~`rows-1` | 좌표 중복 금지, 모든 칸 필요 |
| `puzzle_light_tiles.csv` | `column` | 0~`columns-1` | 좌표 중복 금지, 모든 칸 필요 |
| `puzzle_circle_rings.csv` | `ring_order` | 0 이상 | 0부터 빈 번호 없이 연속 |
| `puzzle_circle_rings.csv` | `pattern_index` | 0 이상 | 실제 상한은 Unity `_ringPrefabs.Count-1`; 현재 확인된 `All.unity`는 0만 연결 |
| `puzzle_circle_rings.csv` | `segments` | 2 이상 | 필수 |
| `puzzle_circle_rings.csv` | `initial_offset` | 정수, `-segments < 값 < segments`, 0 금지 | 필수 |
| `shop_stock.csv` | `display_order` | 0 이상 | 같은 shop에서 중복 금지; 연속은 권장 |
| `shop_stock.csv` | `initial_stock` | 0 이상 | 0은 품절 시작이며 무한 재고가 아님 |
| `technologies.csv` | `chapter`, `restore_value` | 각각 0 이상 | 전체 `restore_value` 합계 100 |
| `restoration_stages.csv` | `stage` | 1 이상 | 1부터 빈 번호 없이 연속 |
| `restoration_stages.csv` | `required_restore` | 1~100 | 엄격한 오름차순, 마지막 값 100 |
| `codex_entries.csv` | `display_order`, `chapter` | 각각 0 이상 | 순서는 같은 category에서 중복 금지 |
| `codex_ingredients.csv` | `ingredient_order` | 0 이상 | 같은 entry에서 중복 금지 |
| `museum_bottles.csv` | `chapter` | 0 이상 | 술병끼리 중복 금지, 현재 전체 3행 |
| `museum_bottles.csv` | `completion_reward_amount` | 빈칸 또는 1 이상 | 보상 Item ID와 함께 입력 |
| `museum_bottle_items.csv` | `item_order` | 0 이상 | 같은 bottle에서 중복 금지 |
| `museum_bottle_items.csv` | `contribution_value` | 1 이상 | 기본 권장값 1 |
| `museum_relic_milestones.csv` | `required_exhibit_count` | 1~유물 도감 전체 수 | 중복 금지 |
| `museum_relic_milestones.csv` | `reward_amount` | 1 이상 | 필수 |
| `altar_destinations.csv` | `stage_number` | 0 이상 | 필수 |
| `museum_murals.csv` | `chapter` | 0 이상 | 연결 기술 챕터와 일치 |

## 4. 동적 참조 CodeList

아래 열에는 소스 CSV에 **현재 실제로 존재하는 값**만 선택해야 합니다. Unity 목록을 참조하는 열은 별도로 표시했습니다.

| CSV | 열 | 가져오는 목록 / 연결 대상 | 빈칸 |
| --- | --- | --- | --- |
| `items.csv` | `display_name_key` | `localization.csv.text_key` | 불가 |
| `items.csv` | `description_text_key` | `localization.csv.text_key` | 불가 |
| `items.csv` | `icon_id` | Unity `ItemVisualCatalogSO` ID | 가능 |
| `dialogue_actors.csv` | `display_name_key` | `localization.csv.text_key` | 불가 |
| `dialogue_actors.csv` | `default_portrait_id` | 같은 actor의 `dialogue_portraits.csv.portrait_id` | 가능 |
| `dialogue_portraits.csv` | `actor_id` | `dialogue_actors.csv.actor_id` | 불가 |
| `dialogue_portraits.csv` | `display_name_key` | `localization.csv.text_key` | 가능 |
| `dialogue_portraits.csv` | `sprite_id` | Unity `DialogueVisualCatalogSO` Portrait ID | 불가 |
| `dialogue_graphs.csv` | `start_node_id` | 같은 graph의 `dialogue_nodes.csv.node_id` | 불가 |
| `dialogue_graphs.csv` | `default_actor_id` | `dialogue_actors.csv.actor_id` | 가능 |
| `dialogue_graphs.csv` | `left_actor_id` | `dialogue_actors.csv.actor_id` | 가능 |
| `dialogue_graphs.csv` | `right_actor_id` | `dialogue_actors.csv.actor_id` | 가능 |
| `dialogue_graphs.csv` | `background_id` | Unity `DialogueVisualCatalogSO` Background ID | 가능 |
| `dialogue_nodes.csv` | `graph_id` | `dialogue_graphs.csv.graph_id` | 불가 |
| `dialogue_nodes.csv` | `actor_id` | `dialogue_actors.csv.actor_id`, 없으면 graph 기본 화자 | 가능 |
| `dialogue_nodes.csv` | `portrait_id` | 실제 사용 화자의 `portrait_id` | 가능 |
| `dialogue_nodes.csv` | `background_id` | Unity Background ID, 없으면 graph 기본 배경 | 가능 |
| `dialogue_nodes.csv` | `body_text_key` | `localization.csv.text_key` | 불가 |
| `dialogue_nodes.csv` | `next_node_id` | 같은 graph의 `node_id` | 가능; 선택지가 있으면 반드시 빈칸 |
| `dialogue_choices.csv` | `graph_id` | `dialogue_graphs.csv.graph_id` | 불가 |
| `dialogue_choices.csv` | `node_id` | 같은 graph의 `dialogue_nodes.csv.node_id` | 불가 |
| `dialogue_choices.csv` | `choice_text_key` | `localization.csv.text_key` | 불가 |
| `dialogue_choices.csv` | `next_node_id` | 같은 graph의 `node_id` | 액션별 가능/금지 |
| `dialogue_choices.csv` | `action_id` | Flag 시스템 ID / `quests.quest_id` / `shops.shop_id` | 액션별 가능/필수/금지 |
| `quests.csv` | `title_text_key` | `localization.csv.text_key` | 불가 |
| `quests.csv` | `description_text_key` | `localization.csv.text_key` | 불가 |
| `quests.csv` | `owner_npc_id` | 기획상 NPC Scene ID/Binding ID; 코드 존재 검증 없음 | 가능 |
| `quest_objectives.csv` | `quest_id` | `quests.csv.quest_id` | 불가 |
| `quest_objectives.csv` | `description_text_key` | `localization.csv.text_key` | 불가 |
| `quest_objectives.csv` | `required_progress_flag_id` | 게임플레이가 설정하는 Progress Flag 시스템 ID | 불가 |
| `quest_rewards.csv` | `quest_id` | `quests.csv.quest_id` | 불가 |
| `quest_rewards.csv` | `description_text_key` | `localization.csv.text_key` | 불가 |
| `quest_rewards.csv` | `progress_flag_id` | 새로 설정할 Progress Flag 시스템 ID | Flag 보상 없으면 가능 |
| `quest_rewards.csv` | `item_id` | `items.csv.item_id` | 아이템 보상 없으면 가능 |
| `quest_consume_items.csv` | `quest_id` | `quests.csv.quest_id` | 불가 |
| `quest_consume_items.csv` | `item_id` | `items.csv.item_id` | 불가 |
| `npc_dialogue_bindings.csv` | `default_graph_id` | `dialogue_graphs.csv.graph_id` | 불가 |
| `npc_dialogue_bindings.csv` | `actor_override_id` | `dialogue_actors.csv.actor_id` | 가능 |
| `npc_quest_dialogue_routes.csv` | `npc_id` | `npc_dialogue_bindings.csv.npc_id` | 불가 |
| `npc_quest_dialogue_routes.csv` | `quest_id` | `quests.csv.quest_id` | 불가 |
| `npc_quest_dialogue_routes.csv` | `graph_id` | `dialogue_graphs.csv.graph_id` | 불가 |
| 퍼즐 상세 3종 | `puzzle_id` | `puzzles.csv`에서 일치하는 `puzzle_type`의 ID | 불가 |
| `puzzle_light_tiles.csv` | `affected_tile_ids` | 같은 puzzle에 실제 존재하는 `tile_id` | 불가 |
| `puzzle_circle_rings.csv` | `pattern_index` | 해당 Unity 원형 매니저의 `_ringPrefabs` 인덱스 | 불가 |
| `shops.csv` | `currency_item_id` | `items.csv.item_id`, 반드시 `can_sell=false` | 불가 |
| `shop_stock.csv` | `shop_id` | `shops.csv.shop_id` | 불가 |
| `shop_stock.csv` | `item_id` | `items.csv.item_id`, 화폐와 다르고 `price>0`, `can_sell=true` | 불가 |
| `technologies.csv` | 표시 문장 3열 | `localization.csv.text_key` | 불가 |
| `technologies.csv` | `icon_id` | Unity 기술 아이콘 Catalog ID | 가능 |
| `technologies.csv` | `derived_skill_id` | Unity `SkillDataSO.SkillId` | 가능 |
| `codex_entries.csv` | `source_id` | category별 `items.item_id` 또는 `technologies.technology_id` | 불가 |
| `codex_ingredients.csv` | `entry_id` | `codex_entries`의 `ALCOHOL` Entry | 불가 |
| `codex_ingredients.csv` | `item_id`, `location_text_key` | `items.item_id`, `localization.text_key` | 불가 |
| `museum_bottles.csv` | 이름·보상 | `localization.text_key`, `items.item_id` | 보상 쌍은 가능 |
| `museum_bottle_items.csv` | `bottle_id`, `alcohol_item_id` | `museum_bottles.bottle_id`, 술 수집 Item | 불가 |
| `museum_relic_milestones.csv` | `reward_item_id` | `items.csv.item_id` | 불가 |
| `altar_destinations.csv` | `display_name_key`, `spawn_point_id` | Localization, Unity 씬 SpawnPoint ID | 불가 |
| `museum_murals.csv` | `technology_id` | `technologies.csv.technology_id` | 불가 |

## 5. 현재 실제 동적 목록

문서를 데이터 원본으로 사용하지 마십시오. 이 목록은 2026-08-16 현재 예시와 선택지 이해를 위한 스냅샷입니다.

| 종류 | 현재 값 |
| --- | --- |
| Actor | `ACTOR_PLAYER`, `ACTOR_CURATOR` |
| Portrait | PLAYER: `NORMAL`; CURATOR: `NORMAL`, `WORRIED`, `HAPPY` |
| Graph | `DLG_CURATOR_INTRO`, `DLG_CURATOR_IN_PROGRESS`, `DLG_CURATOR_COMPLETE` |
| Item | `ITEM_COIN`, `ITEM_REPAIR_KIT` |
| Quest | `QUEST_RESTORE_MURAL` |
| NPC Binding | `NPC_CURATOR` |
| Puzzle | `PIPE_STAGE_01`, `LIGHT_STAGE_01`, `CIRCLE_STAGE_01` |
| Shop | 현재 없음 |
| LIGHT tile_id | `LIGHT_STAGE_01`: 1~9 |
| CIRCLE pattern index | 현재 `All.unity`에서 확인된 연결: 0 |

Localization Key 20개 전체는 [localization.csv 문서](../PHASE_01_LOCALIZATION_ITEMS/localization_csv.md)에 현재 패턴 중심으로 설명합니다. 새 데이터 작성 시에는 반드시 실제 CSV를 다시 읽어 드롭다운 목록을 갱신합니다.

기술·재건·도감 4종은 실제 CSV가 있지만 현재 헤더만 있어 동적 목록이 비어 있습니다. 박물관 5종은 아직 실제 CSV가 없습니다. 예시 `EXAMPLE_...` ID를 실제 선택지로 사용하지 않습니다.

## 6. 복합 입력 형식

| CSV | 열 | 원본 형식 | 규칙 | 시각 문서 |
| --- | --- | --- | --- | --- |
| `puzzle_light_tiles.csv` | `affected_tile_ids` | `1|2|4` | `|` 구분, 각 값 1 이상, 중복 금지, 같은 puzzle에 존재, 빈칸 불가 | [조명 퍼즐 시각 가이드](../PHASE_05_PUZZLE/LIGHT_PUZZLE_READABLE_MAPPING.md) |

## 7. 조건부 조합 빠른 표

### Quest reward

| 보상 종류 | `progress_flag_id` | `flag_value` | `item_id` | `item_amount` |
| --- | --- | --- | --- | --- |
| Flag만 | 값 | `true`/`false` | 빈칸 | 빈칸 |
| Item만 | 빈칸 | 빈칸 | 값 | 1 이상 |
| Flag+Item | 값 | `true`/`false` | 값 | 1 이상 |
| 모두 없음 | 오류 | 오류 | 오류 | 오류 |

### Item·Shop 정책

| 역할 | `can_sell` | `price` | Shop 조건 |
| --- | --- | --- | --- |
| 상점 화폐 | `false` | 0 이상 | `shops.currency_item_id`로 사용 가능 |
| 판매·구매 상품 | `true` | 1 이상 | `shop_stock.item_id`로 사용 가능 |
| 비판매 일반 아이템 | `false` | 0 이상 | 상품으로 등록하면 오류 |

### Museum bottle completion reward

| 상태 | `completion_reward_item_id` | `completion_reward_amount` |
| --- | --- | --- |
| 보상 확정 | 실제 Item ID | 1 이상 |
| 아직 미확정 | 빈칸 | 빈칸 |
| 한쪽만 입력 | 오류 | 오류 |
