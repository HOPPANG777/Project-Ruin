# 도감·박물관 CSV 가이드

[전체 README로 돌아가기](../README.md) · [공통 작성 규칙](../PHASE_00_FOUNDATION/COMMON_AUTHORING_RULES.md) · [CodeList Master](../PHASE_00_FOUNDATION/CODELIST_MASTER.md)

> 문서 상태: **Phase 7 기반 기능 검증 완료 / 콘텐츠 미확정**
>
> 기술·재건·도감, 벽화·제단 이동, 술병, 유물 전시 CSV가 런타임과 검증기에 연결됐습니다. 최종 방의 발견 기반 개방과 전시 기반 1회 수령도 연결했습니다. 콘텐츠 미확정 파일은 헤더만 유지하며, 기반 검증 통과를 콘텐츠 완료로 간주하지 않습니다.

## 1. 데이터 운영 원칙

- 기획자가 작성하는 이름 Key, 설명 Key, 순서, 챕터, 보상, 요구 목록은 CSV를 런타임 원본으로 사용합니다.
- Sprite, Timeline, Material, Particle, Trigger, SpawnPoint 같은 Unity 전용 참조는 CSV에 에셋 경로를 쓰지 않습니다. CSV의 안정적인 ID를 Unity Catalog 또는 씬 컴포넌트가 연결합니다.
- ScriptableObject를 기획 데이터의 중복 원본으로 유지하지 않습니다.
- 발견, 보유, 투입, 전시, 반환, 벽화 개봉, 보상 수령은 서로 다른 런타임 상태입니다. CSV에는 플레이 중 변하는 상태를 기록하지 않습니다.
- 아직 확정되지 않은 보상·벽화·아이템은 `TODO`, `NONE`, 가짜 ID로 채우지 않습니다. 해당 가이드가 허용하는 빈칸 또는 헤더 전용 파일을 사용합니다.

## 2. CSV 9종

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
5. `museum_bottles.csv`의 3개 병을 한 번에 작성한 뒤 `museum_bottle_items.csv`를 작성합니다.
6. 보상 내용이 확정되면 `museum_relic_milestones.csv`를 작성합니다.
7. 실제 씬의 ID가 확정되면 `altar_destinations.csv`, `museum_murals.csv`, 술병과 20개 전시대 컴포넌트를 연결합니다.
8. Unity 담당자가 아이콘, Timeline, SpawnPoint, 벽화·술병 채움·전시 Renderer 참조를 ID에 연결합니다.
9. 자동 검증 후 새 게임과 저장/로드를 함께 확인합니다.

## 4. CSV로 만들지 않는 데이터

| 대상 | CSV로 만들지 않는 이유 | 원본 |
| --- | --- | --- |
| 20개 자유 전시대 목록 | 전시대는 모두 같은 규칙이고 지정 유물 매핑이 없음 | 씬의 고유 `pedestal_id` + 저장 상태 |
| 전시대에 놓인 현재 유물 | 플레이 중 회수·교체되는 상태 | Progress/System State |
| 아이템 발견 여부 | 한 번 획득 후 영구 유지되는 플레이 상태 | Progress Flag |
| 술병에 이미 투입한 술 | 플레이 중 누적되는 상태 | `museum.bottles.v1` System State |
| 기술 획득·반환 여부 | 기존 복원 서비스가 관리하는 상태 | Progress Flag/복원 서비스 |
| 벽화 개봉 완료 | 반환 여부와 분리해야 하는 연출 상태 | Progress Flag 또는 Scene Object State |
| 최종 방 개방 규칙 | 모든 유물 종류 발견 여부에서 계산하는 단일 규칙 | 도감 발견 상태 → `museum.reward_room.open.v1` Flag |
| 왕의 보검 수령 여부 | 한 번만 지급되어야 하는 플레이 상태 | `museum.relic_displays.v1`의 마일스톤 수령 목록 |

최종 방은 **모든 유물 종류 발견** 시 열고, 왕의 보검은 **모든 유물 종류가 실제 전시**된 뒤 방 안에서 한 번 수령하는 정책입니다. 후자는 `museum_relic_milestones.csv`의 `ENABLE_ROOM_PICKUP` 행으로 설정합니다.

## 5. 현재 확정된 정책

- 도감의 잠긴 이름은 `???`, 잠긴 상세 설명은 `?`로 표시합니다.
- 미발견 술은 이미지를 숨기되 제조 재료와 위치는 표시합니다.
- 술병은 박물관 전체 3개, 챕터당 1개입니다.
- 술 종류는 전역 한 번만 인정하고 투입 시 1개 소비되며 꺼낼 수 없습니다. 완성 후 추가 투입은 금지합니다.
- 마지막 술과 보상은 인벤토리 `TryExchangeItem`으로 함께 처리합니다. 보상을 담을 수 없으면 술을 소비하지 않아 재시도할 수 있습니다.
- 보상이 미확정이라 두 보상 열이 비어 있으면 완성은 정상 처리하며, 나중에 보상을 추가해도 기존 완성 저장에 소급 지급하지 않습니다.
- 유물 전시대는 20개 자유 배치형입니다. 같은 유물 종류의 전역 중복 전시는 금지하며 회수·교체는 허용합니다.
- 유물 배치·회수·교체·보상은 인벤토리 슬롯과 전시 저장 상태를 함께 적용합니다. 상태 적용 실패 시 둘 다 유지하며, 디스크 저장 실패는 적용을 취소하지 않고 Dirty 재저장 대상으로 남깁니다.
- 보상 Item ID, 정확한 유물·술 목록, 벽화·씬 ID는 아직 콘텐츠 확정 전입니다.

## 6. 현재 런타임 연결

- 벽화·제단 이동: `MuseumController`, `MuseumDataRepository`, Phase 4 Build Validator
- 술병: `MuseumBottleController`, `MuseumBottleDataRepository`, `MuseumBottlePresenter`, `MuseumBottleInteractable`
- 술병 UI: 고정 12개 선택 버튼, Bazzi 폰트, 진행도/완료/빈 상태 표시
- 술병 채움: 기여도 합산 비율을 UI Filled Image와 씬 액체 Transform Y Scale에 이벤트 방식으로 반영
- 유물 전시: `MuseumRelicDisplayController`, `MuseumRelicPedestal`, 고정 20개 전용 선택 UI
- 전시 저장: 전시대별 Item ID와 마일스톤 수령 완료를 `museum.relic_displays.v1`에 분리 저장
- 전시 마일스톤: 즉시 지급 실패 재시도, 최종 방 수령 조건 조회·1회 수령 API 제공
- 최종 방: `MuseumRewardRoomController`가 도감 동기화 후 영구 발견을 평가하고 문 개방 Flag를 복원·저장합니다. `MuseumRewardRoom`은 문·보검 Renderer, 방 내부 입력과 챕터 3 벽화를 연결합니다.
- 방과 벽화: 문 개방이 벽화를 자동 개봉시키지는 않습니다. 챕터 3도 기존 기술 반환 + Trigger 진입 규칙을 유지합니다.
- 퀘스트 구분: 개인 테스트용 벽화 관련 퀘스트는 도감·박물관의 필수 조건이 아닙니다. 문 개방·보검 수령·벽화 개봉에 Quest 완료 조건을 추가하지 않습니다. 여기서 표시 상태의 '복원'은 저장된 화면 상태를 다시 반영한다는 뜻입니다.
- 빌드 검증: CSV 교차 참조, GameRoot·Bazzi UI 연결, 전시대 0개 또는 정확히 20개·고유 ID

## 7. 검증 상태와 메뉴

- 2026-08-31 Unity 6000.3.14f1 재검증: Phase 6 **26/26**, Phase 4 **14/14**, Phase 5 **10/10**, 도감 Controller **4/4** 통과(합계 **54/54**).
- Phase 6 테스트: 비유물·미보유·중복 차단, 배치·교체·회수 원자성, 실제 인벤토리 슬롯·저장 상태 복원, 10/20 경계, 보상 재시도·중복 방지, 저장 실패 시 Dirty 유지.
- UI 테스트는 EditMode 실행기에서 빈 테스트 씬의 PlayMode로 진입하여 Tab·Caps Lock 차단, 실제 OnDisable 잠금 해제, 재초기화·재열기를 검증합니다.
- 콘텐츠 미확정 현재 상태: `bottles=0`, `relics=0`, `milestones=0`, `pedestals=0` 허용
- Phase 6 검증기는 위 빈 상태를 `기반 검증 통과 / 콘텐츠 미완성`으로 구분합니다. 검증 중 보상을 지급하거나 플레이어 저장 파일을 쓰지 않습니다.
- Phase 6 검증 메뉴 실행 완료: `relics=0, milestones=0, pedestals=0`. 실제 콘텐츠 연결은 별도입니다.
- 별도 기존 씬 문제: `LJScene.unity`의 Main Camera에 존재하지 않는 컴포넌트 참조(`fileID: 1483421850`)가 있습니다. Phase 6 기반 검증 통과가 이 씬 문제의 해결을 의미하지 않으며, 검증 중 씬 파일은 저장·수정하지 않았습니다.
- Phase 5 통합/검증: `Tools > Project Ruin > Museum > Integrate Phase 5 Into GameRoot`, `Validate Phase 5 Data`
- Phase 6 통합 메뉴: `Tools > Project Ruin > Museum > Integrate Phase 6 Into GameRoot`
- Phase 6 검증 메뉴: `Tools > Project Ruin > Museum > Validate Phase 6 Data`
- 2026-08-31 Phase 7 보완 **38/38** + 기존 관련 **54/54**, 합계 **92/92** 통과. Phase 7 씬·통합 테스트 21개는 빈 테스트 씬의 실제 PlayMode에서 실행했습니다.
- 실제 저장 검증: `Logs/Phase7Validation/test-save-<고유값>.es3` 파일에 저장한 뒤 새 Progress·인벤토리·전시·방 Controller로 수령 전/수령 후/보검 소비 후 상태를 재로드했습니다. 사용자 기본 세이브 파일을 사용하거나 삭제하지 않습니다.
- 실제 입력 경로 검증: 가상 키보드 → InputAction → Physics2D 탐지 → PlayerInteractionController → 방 수령을 확인했습니다. GameView 없는 배치 모드에서는 입력 설정 복제본과 게임용 Dynamic 입력 단계를 사용하며, 테스트 종료 시 설정을 되돌립니다.
- Phase 7 통합 메뉴: `Tools > Project Ruin > Museum > Integrate Phase 7 Into GameRoot`
- Phase 7 검증 메뉴: `Tools > Project Ruin > Museum > Validate Phase 7 Data`
- Phase 7 검증 메뉴 실행 완료: `기반 검증 통과 / 콘텐츠 미완성: relics=0, milestones=0, rooms=0`. 기존 `LJScene`의 손상된 카메라 참조 경고는 별도로 남아 있으며 씬을 저장하지 않았습니다.

## 8. 최종 보상 방 연결 — Unity 담당자

기획자는 기존 CSV에 유물·보상·벽화 정의만 작성합니다. 방 전용 CSV나 보상 SO를 추가하지 않습니다. 현재 실제 보검 Item, 벽화 ID, 방 배치가 미확정이므로 아래 작업은 콘텐츠 확정 후 수행합니다.

1. `Integrate Phase 7 Into GameRoot`를 실행합니다. 이미 연결돼 있으면 프리팹을 재저장하지 않습니다.
2. 방 내부 전체를 감싸는 **별도 BoxCollider2D Trigger**를 만들고, 문에는 통과를 막는 **별도 비 Trigger Collider2D**를 둡니다.
3. 보검 Pickup 오브젝트에 `MuseumRewardRoom`과 BoxCollider2D를 두고 **Is Trigger를 켭니다**. 이 오브젝트만 플레이어 `PlayerInteractionController`의 Interaction Layers에 포함되는 상호작용 레이어에 둡니다. 방 범위 Trigger는 상호작용 대상 레이어에서 제외합니다.
4. Reward Milestone Id에 `ENABLE_ROOM_PICKUP` 행 ID를 연결합니다. Item ID·보상 수량은 Inspector에 복제하지 않습니다.
5. Room Bounds, Door Blocker, Closed Door Renderer, Reward Renderer를 연결합니다. 문과 보검 Renderer는 서로 달라야 합니다. 방·문·보검·벽화 및 Renderer 오브젝트는 활성 상태로 두며, 문 Collider와 보검 Collider의 enabled 상태는 방 컴포넌트가 제어합니다. 벽화는 보검·문과 별도 오브젝트에 둡니다.
6. `chapter=3` CSV 행의 `MuseumMural`을 방 안에 배치하고 Chapter 3 Mural에 연결합니다. 기존 Mural Id·가림·원본·빛 효과 참조도 설정합니다. 보검과 벽화의 원점은 Room Bounds 내부여야 합니다.
7. Phase 4·6·7 검증 메뉴를 실행한 뒤 실제 씬에서 충돌, 상호작용 거리, 벽화 연출을 확인합니다. Phase 7은 전체 씬의 최종 방 1개, 방 내부 배치, 활성 참조, 보상 모드와 벽화 챕터를 검사합니다. 프로젝트 씬의 실제 PlayerInteractionController 마스크에 보검 레이어가 포함되고 방 범위 Trigger 레이어가 제외되는지도 검사합니다. 플레이어를 나중에 생성하는 씬이라도 검증 가능한 마스크 설정이 프로젝트 씬에 있어야 합니다.

문은 한 번 열리면 유물 회수·소비·재로드 후에도 닫히지 않습니다. 유물 CSV가 비어 있으면 새 문을 열지 않습니다. 보검은 수령 시점에 전체 전시 조건을 다시 검사하고, 인벤토리 또는 상태 적용 실패 때 수령 완료로 기록하지 않습니다. 디스크 저장 실패는 기존 Dirty 재저장 경로를 사용합니다. 이미 수령한 보검을 소비해도 재지급하지 않습니다.

현재 기반 테스트는 실제 확정 콘텐츠를 대신하지 않습니다. 방 배치와 연출·실기기 프레임 성능 확인은 콘텐츠 확정 후 수행합니다. Phase 8의 통합·제한된 할당 검사는 아래 항목과 구분합니다.

벽화의 저장 표시 재적용은 서비스 연결·준비 완료·재활성화 시에만 수행합니다. 전시 변경이나 보검 수령 알림으로 진행 중인 벽화 빛 효과를 중단하지 않습니다.

## 9. Phase 8 통합 검증 — Unity 담당자

- 메뉴: `Tools > Project Ruin > Museum > Validate Phase 8 Integration`
- 기존 게임 CSV·Localization·시각 에셋 검사와 Phase 4~7 검사를 재사용합니다. GameRoot 단일 소유·필수 연결·활성 상태와 실제 씬의 중복도 검사합니다.
- 씬·프리팹 YAML의 로컬 Component 참조는 원문에서도 검사합니다. Unity가 Preview 로드 중 누락 참조를 제거하더라도 통합 성공으로 처리하지 않습니다. 검증 메뉴는 에셋을 저장하거나 플레이어 Progress를 불러오지 않습니다.
- 원본 참조 오류가 하나라도 있으면 전체 메뉴 및 배치 실행은 실패합니다. 개별 단계의 기반 통과, 콘텐츠 완성, 프로젝트 전체 무결성은 서로 다른 결과입니다.
- 초기화는 Progress 불러오기 실패 시 중단합니다. 발견 조회는 초기화 시 만든 키를 재사용하되, 발견 bool은 계속 Progress에서 읽습니다.
- 테스트용 유물·CSV는 메모리에만 만들며 실물 CSV에는 추가하지 않습니다. 실제 GameRoot 초기화 본문과 이전 저장 보유 아이템 동기화는 `Logs/Phase8Validation/root-<고유값>.es3`로 검증합니다. 입력·생명주기 검사는 빈 테스트 씬의 PlayMode에서 실행합니다.
- 도감 UI 회귀 테스트는 기존 프리팹을 재생성하지 않습니다. 개인 벽화 퀘스트는 런타임 데이터 회귀 검사의 필수 행에서도 제외합니다.
- 할당 검사는 Unity `GC.Alloc` 기록기가 실제 할당을 감지하는지 먼저 확인한 뒤, 워밍업된 발견 조회와 20종 전시 후보 버퍼 채우기 1,000회만 측정합니다. 이 결과는 UI 렌더링·저장 직렬화·전체 프레임의 무할당을 뜻하지 않습니다.
- 2026-08-31 최종 실행: 관련 테스트 **172/172 통과**, 실패·건너뜀 0. 도감·박물관 전 단계, 실제 GameRoot 초기화, 저장 실패 중단, CSV 회귀, 문명 기술·환경 스킬·상점 회귀를 포함합니다. 근거: `Logs/Phase8Validation/verified-tests.xml`, `verified-tests.log`.
- 할당 검사 결과: 대조용 배열 할당은 GC.Alloc 1회 감지, 워밍업 후 대상 조회 1,000회는 **GC.Alloc 0회**. 이 환경의 GC.Alloc 마커 Value 단위는 `TimeNanoseconds`이므로 Value 합을 바이트로 해석하지 않습니다. 일반 .NET 할당 카운터 결과도 검증 근거로 사용하지 않습니다.
- 통합 메뉴 실제 결과: CSV·Localization 및 Phase 4~7 기반 검사는 통과했으나, 기존 `LJScene.unity`의 `fileID: 1483421850` 누락으로 **전체 실패(exit 1)**. 근거: `Logs/Phase8Validation/integration-validation.log`. 씬 원문과 프리팹을 저장하지 않았으며 누락 참조를 임의 삭제하지 않았습니다.
- 최종 완료는 아직 보류입니다. 실제 유물·술·보상·벽화·목적지 CSV 확정과 씬 배치, LJScene 참조 복구, 실제 콘텐츠의 연출·기기 성능 확인이 남아 있습니다. 이번 실행은 Player 빌드나 실제 플레이어 세이브 복구 테스트가 아닙니다.

## 10. Phase 8 보완 — 초기화와 실제 입력 연결

- GameRoot가 저장 복구와 시스템 초기화를 마친 뒤 직렬화된 CodexNavigationPresenter를 한 번 초기화합니다. 준비 전 HUD·키 입력은 도감을 열지 않으며, 매 프레임 초기화를 재시도하지 않습니다.
- 도감의 전역 Find 탐색을 제거했습니다. InventoryNavigationBridge는 GameRoot 프리팹에 연결하고, UnitPlayer는 같은 오브젝트의 이동·상호작용 Controller를 OnEnable/Start에서 등록합니다. 두 Controller가 모두 없는 UnitPlayer는 등록하지 않습니다.
- 플레이어 교체·비활성화 시 이전 창과 소유한 입력 잠금을 정리합니다. 이전 씬 플레이어의 늦은 해제가 새 플레이어 연결을 제거하지 않으며, 파괴 시 새 싱글턴을 찾거나 생성하지 않습니다.
- Unity 담당자는 `Tools > Project Ruin > Codex > Integrate Game UI Into GameRoot`로 연결을 갱신할 수 있습니다. 이미 같은 참조로 연결되어 있으면 프리팹을 재저장하지 않습니다. 이번 GameRoot 프리팹 변경은 도감 참조 한 개와 Unity가 만든 해당 참조 블록뿐입니다.
- 통합 검증은 GameRoot와 도감·인벤토리 UI의 교차 연결도 확인하며, 대상 도감·박물관 런타임에 금지된 전역 탐색/Coroutine 호출 패턴이 다시 생기면 실패합니다.

### 요구사항과 검사 연결

| 확인 항목 | 자동 검사 연결 | 별도 확인 범위 |
|---|---|---|
| 영구 발견·구버전 보유 동기화·기술·CSV 관계 | CodexControllerTests, GameDataCsvRegressionTests, RelicRestorationControllerTests | 확정 기획 데이터 검수 |
| 잠금 표시·Tooltip·2P 유지·빈 항목·풀 재사용 | CodexUiPhase2Tests, CodexUiPhase2PrefabTests | 실제 아트·긴 문장 레이아웃 |
| 스크롤·50회 재열기 슬롯 유지 | CodexMuseumInteractionPhase8Tests | 실제 기기 입력감·렌더링 성능 |
| 창 전환·중복 입력·입력 잠금 복원 | CodexNavigationPhase3Tests, CodexBootstrapPhase8Tests | 확정 씬의 다른 UI와 동선 |
| 실제 Start·로드 실패·손상 ES3·취소·플레이어 교체 | CodexBootstrapPhase8Tests, CodexMuseumPhase8Tests | 사용자 기본 세이브는 테스트 대상 아님 |
| 반환·개봉 1회·저장 표시·물리 Trigger | MuseumControllerPhase4Tests, MuseumMuralPhase4Tests, CodexMuseumInteractionPhase8Tests | 실제 벽화 아트·빛 연출 |
| 술 투입·전시·보상 경계·원자성·재로드 | MuseumBottlePhase5Tests, MuseumRelicPhase6Tests, MuseumRewardRoomPhase7SceneTests | 확정 CSV와 실제 배치 |
| 조회 할당·코드 규약·참조 무결성 | CodexMuseumPhase8Tests, Phase 8 검증 메뉴 | 전체 프레임 Profiler는 별도 |

새 손상 저장 검사는 `Logs/Phase8Validation/bootstrap-<고유값>.es3`만 사용합니다. 벽화 물리 검사는 서비스 대역으로 Trigger 전달을 검사하며, 실제 반환 조건·저장 판정은 기존 Phase 4 도메인 검사와 구분합니다. 기본 플레이어 세이브와 기획 CSV는 변경하지 않습니다.

LJScene의 누락 참조는 최초 Git 커밋 `54a6415`에도 존재합니다. 정상 원본이나 컴포넌트 정보 없이 삭제·추측 복구하지 않았으며, 이 항목이 해결되기 전까지 통합 메뉴의 전체 성공 판정은 보류합니다. 개인 벽화 테스트 퀘스트를 게임 조건에 추가하지 않습니다.

- 2026-09-01 최종 보완 검증: **187/187 통과**, 실패·건너뜀 0, C# 컴파일 오류·경고 0. 근거: `Logs/Phase8Validation/supplement-all-tests.xml`, `supplement-all-tests.log`.
- 실제 자동 Start·씬 플레이어 교체·손상 ES3 원문 보존·취소·재초기화 실패 시 잠금 해제를 확인했습니다. UI는 50회 재열기 후 기존 슬롯 유지, 실제 ScrollRect 이벤트 이동과 Physics2D 벽화 진입을 확인했습니다.
- 제한된 조회 할당 검사는 계속 **1,000회 조회 중 GC.Alloc 0회**입니다. UI 전체 프레임의 무할당이나 시각 품질을 보장하는 수치가 아닙니다.
- UI 통합 메뉴 재실행은 **exit 0**, GameRoot 프리팹 SHA-256은 실행 전후 동일했습니다. Phase 8 통합 메뉴는 CSV·기반 연결·금지 호출 검사를 통과했지만 기존 LJScene 참조 때문에 **exit 1**입니다. 근거: `supplement-integrate-menu.log`, `supplement-validation.log`.
