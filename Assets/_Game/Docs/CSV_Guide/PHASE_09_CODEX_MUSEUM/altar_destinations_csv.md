# altar_destinations.csv

[도감·박물관 가이드](README.md) · [기술](technologies_csv.md)

> 상태: 계획 계약. 현재 데이터는 `RelicRestorationCatalogSO.AltarDestinations`에 있으며 실제 Catalog는 비어 있습니다.

## 1. 이 CSV의 목적

제단 텔레포트 화면에 표시할 목적지 이름, 스테이지와 SpawnPoint ID를 정의합니다. 한 행은 **이동 가능한 제단 목적지 한 곳**입니다. SpawnPoint Transform 자체는 씬에 남습니다.

## 2. 언제 작성하는가

실제 제단과 이동 목적지 SpawnPoint ID가 확정됐을 때 작성합니다. 현재 씬 연결이 미확정이면 헤더만 유지하고 가짜 목적지를 만들지 않습니다.

## 3. 정확한 헤더

```csv
altar_id,display_name_key,stage_number,spawn_point_id,is_main_altar
```

## 4. 열별 작성 규칙

| 열 이름 | 쉬운 설명 | 필수 여부 | 형식 | 입력 가능 값 | 참조 대상 | 올바른 예 |
| --- | --- | --- | --- | --- | --- | --- |
| `altar_id` | 제단과 저장·상호작용 연결 ID | 필수 | 시스템 ID | 전체 제단에서 고유 | 씬 AltarInteractable | `ALTAR_EXAMPLE_MAIN` |
| `display_name_key` | 텔레포트 UI 목적지 이름 Key | 필수 | Localization Key | 실제 `text_key` | `localization.csv` | `altar.example_main.name` |
| `stage_number` | 제단이 위치한 스테이지 번호 | 필수 | 0 이상 정수 | 0 이상 | Stage Flow | `0` |
| `spawn_point_id` | 이동 후 플레이어 배치 지점 ID | 필수 | 시스템 ID | 해당 스테이지의 실제 ID | 씬 SpawnPoint | `SPAWN_EXAMPLE_ALTAR_MAIN` |
| `is_main_altar` | 박물관 메인 제단인지 여부 | 필수 | Boolean | `true`, `false` | 기본 목적지 정책 | `true` |

## 5. CodeList 및 입력 가능 값

- `is_main_altar`: 정확히 `true` 또는 `false`입니다.
- `altar_id`, `spawn_point_id`: 시스템 ID 형식입니다.
- `display_name_key`: `localization.csv.text_key` 동적 목록입니다.
- `stage_number`: 0 이상 정수입니다.
- `is_main_altar=true` 행은 정확히 한 개를 권장하며 구현 계약에서 한 개로 검증합니다.

## 6. 다른 CSV와의 연결

- 참조함: 이름 → `localization.csv`
- 씬: `altar_id` → `AltarInteractable`, `spawn_point_id` → 해당 Stage의 SpawnPoint
- 진행 조건: 원형 퍼즐 완료 등 활성화 조건은 기존 Progress/Stage Flow가 담당하며 이 CSV에 Flag ID를 중복 작성하지 않습니다.
- Unity 전용: 실제 Transform, 씬 이름, 전환 효과는 CSV 열이 아닙니다.

## 7. 작성 순서

1. 실제 Stage와 제단 오브젝트를 확정합니다.
2. 제단 ID와 SpawnPoint ID를 Unity 담당자와 합의합니다.
3. 목적지 이름 Localization 행을 작성합니다.
4. 메인 제단 한 곳만 `true`로 표시합니다.
5. 씬 ID 존재, 이동 후 위치, 중복 입력 차단을 PlayMode에서 확인합니다.

## 8. 올바른 예

아래는 형식 설명용이며 실제 씬 ID가 아닙니다.

```csv
altar_id,display_name_key,stage_number,spawn_point_id,is_main_altar
ALTAR_EXAMPLE_MAIN,altar.example_main.name,0,SPAWN_EXAMPLE_ALTAR_MAIN,true
ALTAR_EXAMPLE_STAGE1,altar.example_stage1.name,1,SPAWN_EXAMPLE_ALTAR_STAGE1,false
```

## 9. 잘못된 예

| 잘못된 값 | 이유 |
| --- | --- |
| `is_main_altar=TRUE` | Boolean은 소문자만 허용 |
| `stage_number=-1` | 0 이상이어야 함 |
| `spawn_point_id=Lobby/Spawn` | 씬 경로가 아니라 시스템 ID 필요 |
| 메인 제단 두 행 | 기본 목적지가 모호함 |
| 존재하지 않는 SpawnPoint ID | 이동 실패 |

## 10. 자동 또는 Unity 검증 항목

Phase 4 구현 시 헤더, 필수값, ID·SpawnPoint 중복 여부, 이름 Key, 스테이지 범위, Boolean, 메인 제단 정확히 한 개를 검사합니다. 실제 Stage·AltarInteractable·SpawnPoint 존재와 이동 성공은 Unity Editor/PlayMode 검증입니다. 현재 CSV 자동 검증은 없습니다.

## 11. 수정과 삭제 시 주의사항

`altar_id`는 씬 오브젝트와 UI 선택의 계약입니다. 변경할 때 모든 AltarInteractable과 저장 참조를 검색합니다. `spawn_point_id` 변경은 저장 호환보다 씬 이동 안전성에 직접 영향을 주므로 해당 Stage에서 실제 이동을 다시 테스트합니다.

## 12. 빠른 체크리스트

- [ ] 정확한 5열 헤더인가?
- [ ] 제단 ID와 SpawnPoint ID가 시스템 ID 형식인가?
- [ ] 이름 Key와 실제 씬 대상이 존재하는가?
- [ ] 스테이지 번호가 0 이상인가?
- [ ] Boolean을 소문자로 썼는가?
- [ ] 메인 제단이 정확히 한 개인가?

## 13. 관련 문서와 코드

- [현재 복원 Catalog](../../../Scripts/Gameplay/Relics/RelicRestorationCatalogSO.cs)
- [현재 AltarInteractable](../../../Scripts/Gameplay/Relics/AltarInteractable.cs)
- [공통 작성 규칙](../PHASE_00_FOUNDATION/COMMON_AUTHORING_RULES.md)
- [Progress 저장 가이드](../../ES3_PROGRESS_SAVE_GUIDE.md)

