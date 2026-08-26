# Project Ruin CSV 시스템 설계·작성 문서

> 기준일: 2026-08-24  
> 현재 단계: 기존 CSV 23종과 도감·박물관 CSV 8종이 런타임 계약입니다. 유물 Milestone 1종만 이후 Phase의 계획 계약입니다.

## 이 문서 세트의 목적과 독자

이 문서 세트는 개발 경험이 없는 기획자가 Project Ruin의 현재 CSV 23종과 계획 중인 박물관 CSV 5종을 안전하게 작성할 수 있도록 규칙을 쉬운 한국어로 풀어쓴 기준서입니다. 헤더, 필수값, 입력 가능 값, 다른 CSV와의 연결, 자동 검증, Unity에서 따로 연결할 항목을 한곳에서 찾을 수 있습니다.

현재 구현된 게임 데이터 원본은 [`Assets/_Game/Data/Csv/`](../../Data/Csv/)의 CSV입니다. 이 Markdown은 데이터를 대신하지 않습니다. 기술·재건·도감 4종은 Phase 1, 벽화·제단 2종은 Phase 4, 술병 2종은 Phase 5에서 런타임과 자동 검증에 연결됐습니다.

## 규칙을 확정한 우선순위

서로 다른 설명이 발견되면 다음 순서로 판단했습니다.

1. 런타임 로더
2. `GameDataRepository` 교차 참조 검증
3. 회귀 테스트
4. 현재 실제 CSV
5. 기존 Markdown
6. 기존 Excel

자세한 교정 내역은 [기존 가이드 교정표](PHASE_00_FOUNDATION/EXISTING_GUIDE_CORRECTIONS.md)에 있습니다.

## 권장 읽기 순서

1. [CSV 시스템 전체 구조](PHASE_00_FOUNDATION/CSV_SYSTEM_OVERVIEW.md)
2. [공통 작성 규칙](PHASE_00_FOUNDATION/COMMON_AUTHORING_RULES.md)
3. [CodeList Master](PHASE_00_FOUNDATION/CODELIST_MASTER.md)
4. 만들 기능의 CSV별 문서
5. [교차 참조 지도](PHASE_07_REFERENCE/CROSS_REFERENCE_MAP.md)
6. [기획자 작업 흐름](PHASE_08_WORKFLOW_QA/DESIGNER_WORKFLOWS.md)
7. [검증과 문제 해결](PHASE_08_WORKFLOW_QA/VALIDATION_AND_TROUBLESHOOTING.md)
8. [최종 체크리스트](PHASE_08_WORKFLOW_QA/FINAL_CHECKLIST_AND_HANDOFF.md)

## 기존 시스템 CSV 19종 빠른 찾기

`필수`는 현재 `GameDataController`가 시작할 때 반드시 TextAsset으로 연결해야 한다는 뜻입니다. `선택` 파일도 연결해서 사용할 때는 헤더와 모든 행 규칙을 똑같이 지켜야 합니다.

| 분류 | CSV 파일 | 한 행의 의미 | 필수 여부 | 부모/자식 | 상세 문서 | CodeList |
| --- | --- | --- | --- | --- | --- | --- |
| Localization | `localization.csv` | 문장 Key 한 개와 언어별 문장 | 필수 | 여러 CSV가 참조하는 부모 | [열기](PHASE_01_LOCALIZATION_ITEMS/localization_csv.md) | 있음 |
| Inventory | `items.csv` | 아이템 종류 한 개 | 필수 | Quest·Shop이 참조하는 부모 | [열기](PHASE_01_LOCALIZATION_ITEMS/items_csv.md) | 있음 |
| Dialogue | `dialogue_actors.csv` | 화자 한 명 | 필수 | Portrait·Graph·Node가 참조하는 부모 | [열기](PHASE_02_DIALOGUE/dialogue_actors_csv.md) | 있음 |
| Dialogue | `dialogue_portraits.csv` | 한 화자의 표정 한 개 | 필수 | Actor의 자식, Node가 참조 | [열기](PHASE_02_DIALOGUE/dialogue_portraits_csv.md) | 있음 |
| Dialogue | `dialogue_graphs.csv` | 대화 흐름 한 개 | 필수 | Node·NPC가 참조하는 부모 | [열기](PHASE_02_DIALOGUE/dialogue_graphs_csv.md) | 있음 |
| Dialogue | `dialogue_nodes.csv` | 대사 노드 한 개 | 필수 | Graph의 자식, Choice의 부모 | [열기](PHASE_02_DIALOGUE/dialogue_nodes_csv.md) | 있음 |
| Dialogue | `dialogue_choices.csv` | 선택지 한 개 | 필수 | Node의 자식 | [열기](PHASE_02_DIALOGUE/dialogue_choices_csv.md) | 있음 |
| Quest | `quests.csv` | 퀘스트 한 개 | 필수 | 목표·보상·소모·NPC Route의 부모 | [열기](PHASE_03_QUEST/quests_csv.md) | 있음 |
| Quest | `quest_objectives.csv` | 완료 조건 한 개 | 필수 | Quest의 자식 | [열기](PHASE_03_QUEST/quest_objectives_csv.md) | 있음 |
| Quest | `quest_rewards.csv` | 보상 한 묶음 | 필수 | Quest의 자식, Item 참조 | [열기](PHASE_03_QUEST/quest_rewards_csv.md) | 있음 |
| Quest | `quest_consume_items.csv` | 완료 시 소모할 아이템 한 종류 | 필수 | Quest의 자식, Item 참조 | [열기](PHASE_03_QUEST/quest_consume_items_csv.md) | 있음 |
| NPC | `npc_dialogue_bindings.csv` | NPC 한 명의 기본 대화 연결 | 선택 | Route의 부모, Graph·Actor 참조 | [열기](PHASE_04_NPC/npc_dialogue_bindings_csv.md) | 있음 |
| NPC | `npc_quest_dialogue_routes.csv` | 퀘스트 상태별 대화 후보 한 개 | 선택 | Binding의 자식, Quest·Graph 참조 | [열기](PHASE_04_NPC/npc_quest_dialogue_routes_csv.md) | 있음 |
| Puzzle | `puzzles.csv` | 퍼즐 한 개의 종류와 크기 | 필수 | 종류별 상세 CSV의 부모 | [열기](PHASE_05_PUZZLE/puzzles_csv.md) | 있음 |
| Puzzle | `puzzle_pipe_tiles.csv` | 파이프 격자의 한 칸 | 필수 | PIPE 퍼즐의 자식 | [열기](PHASE_05_PUZZLE/puzzle_pipe_tiles_csv.md) | 있음 |
| Puzzle | `puzzle_light_tiles.csv` | 조명 격자의 한 타일과 클릭 영향 | 필수 | LIGHT 퍼즐의 자식 | [열기](PHASE_05_PUZZLE/puzzle_light_tiles_csv.md) | 있음 |
| Puzzle | `puzzle_circle_rings.csv` | 원형 퍼즐의 링 한 개 | 필수 | CIRCLE 퍼즐의 자식 | [열기](PHASE_05_PUZZLE/puzzle_circle_rings_csv.md) | 있음 |
| Shop | `shops.csv` | 상점 한 개와 화폐 종류 | 선택 | Stock의 부모, Item 참조 | [열기](PHASE_06_SHOP/shops_csv.md) | 있음 |
| Shop | `shop_stock.csv` | 상점 상품 한 종류와 초기 재고 | 선택 | Shop의 자식, Item 참조 | [열기](PHASE_06_SHOP/shop_stock_csv.md) | 있음 |

도메인별 개수는 Dialogue 5, Inventory·Localization 2, Quest 4, NPC 2, Puzzle 4, Shop 2로 총 19종입니다.

## 도감·박물관 확장 CSV 9종

`구현` 8종은 현재 런타임 계약입니다. `계획` 1종은 유물 전시 Phase에서 구현합니다. 콘텐츠 미확정 CSV는 실제 파일을 헤더만 둡니다.

| 상태 | 분류 | CSV 파일 | 한 행의 의미 | 상세 문서 |
| --- | --- | --- | --- | --- |
| 구현 | Technology | `technologies.csv` | 문명 기술 한 종류 | [열기](PHASE_09_CODEX_MUSEUM/technologies_csv.md) |
| 구현 | Restoration | `restoration_stages.csv` | 재건도 단계 한 개 | [열기](PHASE_09_CODEX_MUSEUM/restoration_stages_csv.md) |
| 구현 | Codex | `codex_entries.csv` | 도감 항목 한 개 | [열기](PHASE_09_CODEX_MUSEUM/codex_entries_csv.md) |
| 구현 | Codex | `codex_ingredients.csv` | 술 도감 재료 한 개 | [열기](PHASE_09_CODEX_MUSEUM/codex_ingredients_csv.md) |
| 구현 | Museum | `museum_bottles.csv` | 챕터 술병 한 개 | [열기](PHASE_09_CODEX_MUSEUM/museum_bottles_csv.md) |
| 구현 | Museum | `museum_bottle_items.csv` | 술병에 인정되는 술 한 종류 | [열기](PHASE_09_CODEX_MUSEUM/museum_bottle_items_csv.md) |
| 계획 | Museum | `museum_relic_milestones.csv` | 유물 전시 보상 한 단계 | [열기](PHASE_09_CODEX_MUSEUM/museum_relic_milestones_csv.md) |
| 구현 | Teleport | `altar_destinations.csv` | 제단 목적지 한 곳 | [열기](PHASE_09_CODEX_MUSEUM/altar_destinations_csv.md) |
| 구현 | Museum | `museum_murals.csv` | 벽화와 기술 연결 한 개 | [열기](PHASE_09_CODEX_MUSEUM/museum_murals_csv.md) |

전체 운영 원칙과 작성 순서는 [도감·박물관 CSV 가이드](PHASE_09_CODEX_MUSEUM/README.md)에서 확인합니다.

## 현재 데이터 상태

- 필수 연결 19종: Dialogue 5, Items 1, Quest 4, Localization 1, Puzzle 4, Technology·Codex 4
- 선택 연결 4종: NPC 2, Shop 2
- 현재 행이 있는 선택 CSV: NPC 2종
- 현재 헤더만 있고 데이터 행이 없는 CSV: `shops.csv`, `shop_stock.csv`, 기술·재건·도감 4종, 벽화·제단 2종, 술병 2종
- 현재 실제 데이터 행: Dialogue 17, Items 2, Localization 20, Quest 6, NPC 3, Puzzle 32, Shop 0

## 페이즈별 문서

| 페이즈 | 내용 |
| --- | --- |
| PHASE 00 | [시스템 구조](PHASE_00_FOUNDATION/CSV_SYSTEM_OVERVIEW.md), [공통 규칙](PHASE_00_FOUNDATION/COMMON_AUTHORING_RULES.md), [CodeList](PHASE_00_FOUNDATION/CODELIST_MASTER.md), [기존 자료 교정](PHASE_00_FOUNDATION/EXISTING_GUIDE_CORRECTIONS.md) |
| PHASE 01 | [Localization](PHASE_01_LOCALIZATION_ITEMS/localization_csv.md), [Items](PHASE_01_LOCALIZATION_ITEMS/items_csv.md) |
| PHASE 02 | Dialogue CSV 5종 |
| PHASE 03 | Quest CSV 4종 |
| PHASE 04 | NPC CSV 2종 |
| PHASE 05 | Puzzle CSV 4종과 [조명 퍼즐 시각 가이드](PHASE_05_PUZZLE/LIGHT_PUZZLE_READABLE_MAPPING.md) |
| PHASE 06 | Shop CSV 2종 |
| PHASE 07 | [교차 참조 지도](PHASE_07_REFERENCE/CROSS_REFERENCE_MAP.md), [ID 변경 영향](PHASE_07_REFERENCE/ID_CHANGE_IMPACT_MAP.md) |
| PHASE 08 | [작업 흐름](PHASE_08_WORKFLOW_QA/DESIGNER_WORKFLOWS.md), [검증·문제 해결](PHASE_08_WORKFLOW_QA/VALIDATION_AND_TROUBLESHOOTING.md), [향후 Excel 요구사항](PHASE_08_WORKFLOW_QA/FUTURE_EXCEL_REQUIREMENTS.md), [최종 인수인계](PHASE_08_WORKFLOW_QA/FINAL_CHECKLIST_AND_HANDOFF.md) |
| PHASE 09 | [도감·박물관 계획 CSV 9종](PHASE_09_CODEX_MUSEUM/README.md) |

## 기존 참고 자료

- [Dialogue·Quest 작성 가이드](../DIALOGUE_QUEST_AUTHORING_GUIDE.md)
- [Inventory 작성 가이드](../INVENTORY_SYSTEM_AUTHORING_GUIDE.md)
- [Puzzle 짧은 작성 가이드](../PUZZLE_CSV_AUTHORING_GUIDE.md)
- [Puzzle 시스템 기획자 참고서](../PUZZLE_CSV_SYSTEM_DESIGNER_REFERENCE.md)
- [Progress 시스템](../PROGRESS_SYSTEM_GUIDE.md)
- [ES3 저장 가이드](../ES3_PROGRESS_SAVE_GUIDE.md)
- [Interaction 제작 가이드](../INTERACTION_AUTHORING_GUIDE.md)
- `Project-Ruin_CSV_작성_가이드.xlsx`
- `Project-Ruin_퀘스트_대화_CSV_작성_가이드.xlsx`
- `Project-Ruin_퍼즐_CSV_작성_가이드.xlsx`

## 문서를 수정할 때 함께 확인할 코드

- 공통 파싱: [`CsvParser.cs`](../../Scripts/Core/Data/Csv/CsvParser.cs), [`CsvDefinitionReadUtility.cs`](../../Scripts/Gameplay/Data/Csv/CsvDefinitionReadUtility.cs)
- 도메인 로더: [`Assets/_Game/Scripts/Gameplay/Data/Csv/`](../../Scripts/Gameplay/Data/Csv/)
- 교차 참조: [`GameDataRepository.cs`](../../Scripts/Gameplay/Data/Repository/GameDataRepository.cs)
- Unity 연결: [`GameDataController.cs`](../../Scripts/Gameplay/Data/GameDataController.cs)
- 검증·회귀: [`GameDataEditorValidator.cs`](../../Scripts/Editor/GameDataEditorValidator.cs), [`GameDataCsvRegressionTests.cs`](../../Scripts/Editor/GameDataCsvRegressionTests.cs)

## 문서 세트 완료 상태

- [x] 현재 CSV 23종 각각 독립 문서
- [x] 정확한 헤더와 필수·선택·조건부 구분
- [x] 고정·동적·범위·Boolean·복합 CodeList
- [x] 부모·자식·Unity 연결 지도
- [x] 현재 조명 퍼즐 9행의 격자·영향 보드
- [x] 향후 Excel에서 LIGHT 타일 생성 시 `N×N` 영향 체크리스트 자동 생성·확장 요구사항
- [x] 기존 문서·Excel의 불일치 교정
- [x] 기획자 워크플로·검증·인수인계 체크리스트
- [x] Phase 1 도감 4종, Phase 4 벽화·제단 2종, Phase 5 술병 2종의 런타임 계약과 유물 Milestone 작성 정책
