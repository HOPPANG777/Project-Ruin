# technologies.csv

[도감·박물관 가이드](README.md) · [도감 항목](codex_entries_csv.md) · [벽화](museum_murals_csv.md)

> 상태: 계획 계약. 현재 런타임은 `TechnologyDataSO`와 `RelicRestorationCatalogSO`를 사용하며 CSV 전환은 아직 구현되지 않았습니다.

## 1. 이 CSV의 목적

문명 재건에 사용하는 기술의 ID, 표시 문장, 아이콘 ID, 연결 환경 스킬, 챕터와 재건 기여도를 정의합니다. 한 행은 **문명 기술 한 종류**입니다. 획득·반환 여부 같은 플레이 상태는 이 파일에 넣지 않습니다.

## 2. 언제 작성하는가

문명 기술을 추가하거나 이름, 설명, 조작법, 챕터, 재건 기여도를 바꿀 때 작성합니다. 환경 스킬과 연결할 경우 Unity 담당자에게 실제 `SkillDataSO.SkillId`를 확인한 뒤 입력합니다.

## 3. 정확한 헤더

```csv
technology_id,display_name_key,description_text_key,operation_text_key,icon_id,derived_skill_id,chapter,restore_value
```

## 4. 열별 작성 규칙

| 열 이름 | 쉬운 설명 | 필수 여부 | 형식 | 입력 가능 값 | 참조 대상 | 올바른 예 |
| --- | --- | --- | --- | --- | --- | --- |
| `technology_id` | 저장·조회에 쓰는 기술 ID | 필수 | 시스템 ID | 전체 기술에서 고유 | 도감·벽화·복원 상태 | `TECH_EXAMPLE_GLIDE` |
| `display_name_key` | 기술 이름 Key | 필수 | Localization Key | 실제 `text_key` | `localization.csv` | `technology.example_glide.name` |
| `description_text_key` | 기술 설명 Key | 필수 | Localization Key | 실제 `text_key` | `localization.csv` | `technology.example_glide.description` |
| `operation_text_key` | 도감 Tooltip의 조작법 Key | 필수 | Localization Key | 실제 `text_key` | `localization.csv` | `technology.example_glide.operation` |
| `icon_id` | 기술 이미지 연결 ID | 선택 | 시스템 ID | 빈칸 또는 Unity Catalog ID | 아이콘 Catalog | `ICON_TECH_EXAMPLE_GLIDE` |
| `derived_skill_id` | 이 기술로 해제되는 환경 스킬 ID | 선택 | 시스템 ID | 빈칸 또는 실제 Skill ID | `SkillDataSO` | `SKILL_EXAMPLE_GLIDE` |
| `chapter` | 기술을 획득하는 챕터 | 필수 | 0 이상 정수 | 0 이상 | 도감·벽화 그룹 | `1` |
| `restore_value` | 제단 반환 시 오르는 재건도 | 필수 | 0 이상 정수 | 전체 합계 100 | 복원 시스템 | `25` |

`icon_id`는 파일 경로나 Sprite 이름이 아닙니다. 실제 Sprite 연결은 Unity Catalog에서 담당합니다.

## 5. CodeList 및 입력 가능 값

- 고정 enum은 없습니다.
- `technology_id`, `icon_id`, `derived_skill_id`: 시스템 ID 형식입니다.
- 표시 문장 3열: `localization.csv.text_key` 동적 목록입니다.
- `chapter`: 0 이상 정수입니다.
- `restore_value`: 각 행 0 이상이며 전체 기술 합계가 정확히 `100`이어야 합니다.
- `derived_skill_id`는 기술과 환경 스킬이 실제로 연결될 때만 입력합니다. 없으면 빈칸입니다.

## 6. 다른 CSV와의 연결

- 참조함: 이름·설명·조작법 → `localization.csv`
- 참조받음: `codex_entries.source_id`(`category=TECHNOLOGY`), `museum_murals.technology_id`
- Unity: `icon_id` → 기술 아이콘 Catalog, `derived_skill_id` → `SkillDataSO.SkillId`
- 저장: 획득 `skill.acquired.{technology_id}`, 반환 `skill.returned.{technology_id}` 규칙을 사용하되 표시 문장을 저장 Key로 쓰지 않습니다.

## 7. 작성 순서

1. 고유한 `TECH_...` ID를 정합니다.
2. 이름, 설명, 조작법 Localization 행을 만듭니다.
3. 획득 챕터와 재건 기여도를 정합니다.
4. 실제 환경 스킬이 파생될 때만 Skill ID를 연결합니다.
5. 아이콘이 준비되면 Unity 담당자와 `icon_id`를 합의합니다.
6. 전체 `restore_value` 합계가 100인지 확인합니다.
7. 도감 항목과 벽화를 나중에 연결합니다.

## 8. 올바른 예

아래는 형식 설명용이며 실제 게임 데이터가 아닙니다.

```csv
technology_id,display_name_key,description_text_key,operation_text_key,icon_id,derived_skill_id,chapter,restore_value
TECH_EXAMPLE_GLIDE,technology.example_glide.name,technology.example_glide.description,technology.example_glide.operation,ICON_TECH_EXAMPLE_GLIDE,SKILL_EXAMPLE_GLIDE,1,25
TECH_EXAMPLE_ARCHIVE,technology.example_archive.name,technology.example_archive.description,technology.example_archive.operation,,,0,75
```

## 9. 잘못된 예

| 잘못된 값 | 이유 |
| --- | --- |
| `technology_id=tech_glide` | 시스템 ID 형식 위반 |
| 없는 `operation_text_key` | Localization 참조 오류 |
| `icon_id=Assets/Icon.png` | 에셋 경로를 쓰는 열이 아님 |
| 없는 `derived_skill_id` | Unity Skill 참조 오류 |
| `chapter=-1` | 0 이상이어야 함 |
| 전체 `restore_value` 합계 90 | 최대 재건도 100 계약 위반 |

## 10. 자동 또는 Unity 검증 항목

Phase 1 구현 시 다음을 오류로 검증합니다.

- 정확한 8열 헤더와 필수값
- 기술 ID 형식·중복
- 세 Localization Key의 존재
- `chapter >= 0`, `restore_value >= 0`
- 전체 재건 기여도 합계 100
- 도감·벽화의 기술 참조 존재

Unity Editor에서는 `icon_id`와 `derived_skill_id` 연결 누락을 경고로 확인합니다. 현재는 아직 이 CSV용 자동 검증이 없습니다.

## 11. 수정과 삭제 시 주의사항

`technology_id`는 획득·반환 저장 Flag와 도감·벽화 연결의 기준입니다. 출시 후 변경하면 기존 저장에서 기술 상태를 잃을 수 있으므로 마이그레이션 없이 이름을 바꾸지 않습니다. `restore_value`를 바꾸면 기존 반환 기술로부터 계산되는 재건도가 달라질 수 있습니다.

## 12. 빠른 체크리스트

- [ ] 정확한 8열 헤더인가?
- [ ] 기술 ID가 고유한 시스템 ID인가?
- [ ] 이름·설명·조작법 Key가 실제로 존재하는가?
- [ ] 환경 스킬이 없을 때 `derived_skill_id`를 비웠는가?
- [ ] 챕터와 기여도가 0 이상인가?
- [ ] 전체 기여도 합계가 100인가?
- [ ] 도감·벽화 참조와 Unity 아이콘 연결을 확인했는가?

## 13. 관련 문서와 코드

- [공통 작성 규칙](../PHASE_00_FOUNDATION/COMMON_AUTHORING_RULES.md)
- [CodeList Master](../PHASE_00_FOUNDATION/CODELIST_MASTER.md)
- [ID 변경 영향](../PHASE_07_REFERENCE/ID_CHANGE_IMPACT_MAP.md)
- [현재 TechnologyDataSO](../../../Scripts/Gameplay/Relics/Data/TechnologyDataSO.cs)
- [현재 복원 Catalog](../../../Scripts/Gameplay/Relics/RelicRestorationCatalogSO.cs)

