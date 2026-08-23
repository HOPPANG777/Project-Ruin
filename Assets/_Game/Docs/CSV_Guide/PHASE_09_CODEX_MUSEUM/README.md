# 도감·박물관 CSV 가이드

[전체 README로 돌아가기](../README.md) · [공통 작성 규칙](../PHASE_00_FOUNDATION/COMMON_AUTHORING_RULES.md) · [CodeList Master](../PHASE_00_FOUNDATION/CODELIST_MASTER.md)

> 문서 상태: **Phase 1 데이터 기반 구현 완료 + 박물관 계획 계약**
> 기술·재건·도감 CSV 4종은 런타임과 검증기에 연결됐으며 현재는 콘텐츠 미확정으로 헤더만 있습니다. 박물관 CSV 5종은 이후 Phase에서 구현합니다.

## 1. 데이터 운영 원칙

- 기획자가 작성하는 이름 Key, 설명 Key, 순서, 챕터, 보상, 요구 목록은 CSV를 런타임 원본으로 사용합니다.
- Sprite, Timeline, Material, Particle, Trigger, SpawnPoint 같은 Unity 전용 참조는 CSV에 에셋 경로를 쓰지 않습니다. CSV의 안정적인 ID를 Unity Catalog 또는 씬 컴포넌트가 연결합니다.
- ScriptableObject를 기획 데이터의 중복 원본으로 유지하지 않습니다. 기존의 빈 `TechnologyDataSO`·`RelicRestorationCatalogSO` 계약과 Catalog 에셋은 Phase 1에서 제거했습니다.
- 발견, 보유, 투입, 전시, 반환, 벽화 개봉, 보상 수령은 서로 다른 런타임 상태입니다. CSV에는 플레이 중 변하는 상태를 기록하지 않습니다.
- 아직 확정되지 않은 보상·벽화·아이템은 `TODO`, `NONE`, 가짜 ID로 채우지 않습니다. 해당 가이드가 허용하는 빈칸 또는 헤더 전용 파일을 사용합니다.

## 2. 계획 CSV 9종

| 분류 | CSV | 한 행의 의미 | 부모/자식 | 상세 문서 |
| --- | --- | --- | --- | --- |
| 기술 | `technologies.csv` | 문명 기술 한 종류 | 도감·벽화가 참조하는 부모 | [열기](technologies_csv.md) |
| 재건 | `restoration_stages.csv` | 재건도 단계 한 개 | 독립 마스터 | [열기](restoration_stages_csv.md) |
| 도감 | `codex_entries.csv` | 도감에 표시할 항목 한 개 | 재료의 부모, Item·Technology 참조 | [열기](codex_entries_csv.md) |
| 도감 | `codex_ingredients.csv` | 술 제조 재료 한 개 | 술 도감 항목의 자식 | [열기](codex_ingredients_csv.md) |
| 박물관 | `museum_bottles.csv` | 챕터 술병 한 개 | 술병 요구 항목의 부모 | [열기](museum_bottles_csv.md) |
| 박물관 | `museum_bottle_items.csv` | 술병에 인정되는 술 한 종류 | 술병의 자식, Item 참조 | [열기](museum_bottle_items_csv.md) |
| 박물관 | `museum_relic_milestones.csv` | 전시 개수 보상 한 단계 | Item 참조 | [열기](museum_relic_milestones_csv.md) |
| 이동 | `altar_destinations.csv` | 텔레포트 목적지 제단 한 곳 | Localization·씬 SpawnPoint 참조 | [열기](altar_destinations_csv.md) |
| 박물관 | `museum_murals.csv` | 벽화와 기술의 연결 한 개 | Technology 참조 | [열기](museum_murals_csv.md) |

## 3. 권장 작성 순서

1. `localization.csv`에 필요한 문장 Key를 작성합니다.
2. `items.csv`에 유물, 술, 재료, 보상 아이템을 작성합니다.
3. `technologies.csv`와 `restoration_stages.csv`를 작성합니다.
4. `codex_entries.csv`를 작성하고 술 항목에만 `codex_ingredients.csv`를 연결합니다.
5. `museum_bottles.csv`를 만든 뒤 `museum_bottle_items.csv`를 작성합니다.
6. 보상 내용이 확정되면 `museum_relic_milestones.csv`를 작성합니다.
7. 실제 씬의 ID가 확정되면 `altar_destinations.csv`와 `museum_murals.csv`를 작성합니다.
8. Unity 담당자가 아이콘, Timeline, SpawnPoint, 벽화 연출 참조를 ID에 연결합니다.
9. 자동 검증 후 새 게임과 저장/로드를 함께 확인합니다.

## 4. CSV로 만들지 않는 데이터

| 대상 | CSV로 만들지 않는 이유 | 원본 |
| --- | --- | --- |
| 20개 자유 전시대 목록 | 전시대는 모두 같은 규칙이고 지정 유물 매핑이 없음 | 씬의 고유 `pedestal_id` + 저장 상태 |
| 전시대에 놓인 현재 유물 | 플레이 중 회수·교체되는 상태 | Progress/System State |
| 아이템 발견 여부 | 한 번 획득 후 영구 유지되는 플레이 상태 | Progress Flag |
| 술병에 이미 투입한 술 | 플레이 중 누적되는 상태 | Progress/System State |
| 기술 획득·반환 여부 | 기존 복원 서비스가 관리하는 상태 | Progress Flag/복원 서비스 |
| 벽화 개봉 완료 | 반환 여부와 분리해야 하는 연출 상태 | Progress Flag 또는 Scene Object State |
| 최종 방 개방 규칙 | 모든 유물 종류 발견 여부에서 계산하는 단일 규칙 | 도감 발견 상태에서 계산 |
| 왕의 보검 수령 여부 | 한 번만 지급되어야 하는 플레이 상태 | 보상 수령 Flag |

최종 방은 **모든 유물 종류 발견** 시 열고, 왕의 보검은 **모든 유물 종류가 실제 전시**된 뒤 방 안에서 한 번 수령하는 정책입니다. 후자는 `museum_relic_milestones.csv`의 `ENABLE_ROOM_PICKUP` 행으로 설정합니다.

## 5. 현재 확정된 정책

- 도감의 잠긴 이름은 `???`, 잠긴 상세 설명은 `?`로 표시합니다. 이 두 값은 CSV에 반복 입력하지 않습니다.
- 도감 클릭은 선택 강조만 하며 별도 행동을 실행하지 않습니다.
- 미발견 술은 이미지를 숨기되 제조 재료와 위치는 표시합니다.
- 기술 도감 한 행은 문명 기술 한 개이며, 연결된 환경 스킬은 보조 정보입니다.
- 술병은 박물관 전체 3개, 챕터당 1개입니다.
- 술 종류는 한 번만 인정하고 투입 시 소비되며 꺼낼 수 없습니다. 완성 후 추가 투입은 금지합니다.
- 유물 전시대는 20개 자유 배치형입니다. 같은 유물 종류의 전역 중복 전시는 금지하며 회수·교체는 허용합니다.
- 유물 마일스톤은 중복 복사본이 아니라 서로 다른 실제 전시 종류 수를 사용합니다.
- 보상 Item ID, 정확한 유물·술 목록, 벽화·씬 ID는 아직 콘텐츠 확정 전입니다.

## 6. 구현 전 주의

기술·재건·도감 4종은 Definition, Loader, Repository, `GameDataController`, Editor Validator, 회귀 테스트가 연결된 실행 계약입니다. 나머지 박물관 5종은 아직 문서 계약이며 실제 구현 시 코드와 문서를 같은 변경에서 수정합니다.
