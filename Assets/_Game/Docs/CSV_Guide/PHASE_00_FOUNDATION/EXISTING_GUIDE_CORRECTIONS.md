# 기존 문서와 Excel 교정 기록

[README로 돌아가기](../README.md)

이 표는 기존 자료를 삭제하거나 수정하지 않고, 새 문서에서 어떤 기준으로 교정했는지 기록합니다.

| 항목 | 기존 자료 | 실제 코드 기준 | 새 문서 처리 |
| --- | --- | --- | --- |
| Items의 `can_sell` | `INVENTORY_SYSTEM_AUTHORING_GUIDE.md`, 기존 범용 Excel, 2026-07-18 작업일지의 7열 헤더에는 없음 | 현재 로더와 CSV는 `price` 뒤에 `can_sell`이 있는 8열. 필수 소문자 Boolean | 최신 8열로 교정 |
| `can_sell`과 `price` | 과거 문서는 판매 기능을 전제로 하지 않음 | `can_sell=true`이면 `price > 0` 필수. `false`이면 `price=0` 허용 | Items·Shop 문서에 정책 분리 |
| `OPEN_SHOP` | 오래된 Dialogue 가이드에 없음 | 실제 `action_type` 9종 중 하나. 존재하는 `shop_id`가 `action_id`에 필요하고 `next_node_id`는 빈칸 | Choice·Shop 워크플로에 추가 |
| Shop CSV | 오래된 범용 가이드에 없음 | `shops.csv`, `shop_stock.csv`가 선택 입력으로 존재 | 독립 문서 2개 추가 |
| Shop 현재 상태 | 최신 Excel에는 빈 시트가 있음 | 실제 CSV 두 파일은 헤더만 있고 행 0개. GameRoot prefab 직렬화에도 Shop 연결이 아직 보이지 않음 | “현재 미사용 데이터”로 명시 |
| `dialogue_portraits.sprite_id` | 일부 설명은 이미지가 없어도 비울 수 있다고 오해할 수 있음 | 로더에서 필수 시스템 ID이며 Repository도 빈 값을 거부 | 필수, Unity Catalog 연결은 별도라고 구분 |
| `dialogue_portraits.display_name_key` | 자료마다 명확하지 않음 | 선택 Localization Key | 선택으로 교정 |
| `localization.ko` | 일부 다국어 설명에서 fallback만 강조 | 로더와 Repository 모두 필수 한국어 문장을 요구 | `ko` 필수, `en`·`ja` 선택으로 확정 |
| 퍼즐 Excel의 `메모(내보내지 않음)` | `puzzle_light_tiles` 작업 시트에 7번째 메모 열 존재 | 런타임 헤더는 6열뿐이며 추가 열 불가 | Excel 전용 열이며 CSV 내보내기 금지 명시 |
| `affected_tile_ids` 의미 | 일부 자료는 주변 규칙처럼 읽힐 수 있음 | 클릭 행에 명시된 타일 ID만 반전 | 9행 전체를 격자와 보드로 펼침 |
| 클릭한 조명 타일 | 옛 구현 계획은 자기 자신 포함을 규칙처럼 요구 | 현재 매니저는 클릭 타일을 자동 반전하지 않음. 목록에 자기 ID가 있을 때만 반전 | 자동 반전 아님을 반복 명시 |
| 조명 퍼즐 파일 구조 | 옛 계획은 `lighting_puzzles/tiles/effects.csv` 3개 | 현재는 `puzzles.csv` + `puzzle_light_tiles.csv`, 영향 목록은 `|` 압축 셀 | 옛 구조는 향후 정규화 참고안으로만 기록 |
| 조명 퍼즐 해결 가능성 | 옛 계획은 자동 해결 가능성 검사를 예정 | 현재 로더·회귀 테스트는 구조와 참조만 검사하고 해답 존재를 검사하지 않음 | Play Mode 검수 항목으로 이동 |
| 필수 CSV | `GameDataCsvSource` 생성자 기본값만 보면 일부 파일이 선택처럼 보일 수 있음 | 현재 `GameDataController`는 기존 15종 + Technology·Codex 4종 = 19종을 필수 연결 | Controller 실제 흐름 기준으로 확정 |
| 선택 CSV | NPC·Shop의 역할이 자료마다 다름 | NPC 2종과 Shop 2종은 null/공백 입력 허용 | 기능 사용 시 연결하는 선택 CSV로 확정 |
| `SET_PROGRESS_FLAG` | 오래된 Dialogue 가이드에 단일 이름 | 실제 값은 `SET_PROGRESS_FLAG_TRUE`, `SET_PROGRESS_FLAG_FALSE` | 허용하지 않는 옛 이름으로 표시 |
| Progress Flag 표기 | Progress 가이드 예시는 `story.npc...` 또는 소문자 밑줄 | CSV의 Flag 열은 `TryGetRequired/OptionalSystemId`를 사용해 대문자·숫자·밑줄만 허용 | CSV에서는 `FLAG_...` 형식을 사용하도록 교정 |
| 기술 보상 Flag | Quest 런타임은 `skill.acquired.` 접두어를 특수 처리 | 현재 `quest_rewards.csv` 로더의 시스템 ID 검사와 충돌하여 점이 있는 접두어를 통과할 수 없음 | 현재 알려진 코드 계약 불일치로 기록. CSV에서 임의 사용 금지 |
| `quests.owner_npc_id` | NPC CSV와 반드시 연결된다고 설명되기도 함 | 로더는 선택 시스템 ID 형식만 검사하고 Repository는 존재 여부를 교차 검증하지 않음 | 코드 강제 아님, 기획 연결 권장으로 구분 |
| 파이프 `T_JUNCTION` 기본 방향 | 퍼즐 Excel 방향표는 rot 0을 위+좌+우로 표시 | 현재 `PipeData` rot 0은 좌+우+아래. 이후 시계 방향 회전 | 최신 코드 방향표로 교정 |
| `pattern_index` 범위 | 기존 Excel은 “현재 0이 안전”이라고 설명 | 로더는 0 이상을 허용하지만 현재 `All.unity`의 원형 매니저 `_ringPrefabs`는 한 항목이라 현재 연결 범위는 0만 확인됨 | 코드 범위와 현재 Unity 목록 범위를 나눠 표시 |
| 기존 작성 가이드 경로 | `DIALOGUE_QUEST_AUTHORING_GUIDE.md`는 존재하지 않는 `Docs/CSV_Guide/RuinConservator_CSV_Authoring_Guide.xlsx`를 안내 | 실제 확인된 Excel은 `Assets/_Game/Docs/`의 3종 | 기존 링크를 기준으로 삼지 않고 실제 파일명을 README에 기록 |

## 특히 주의할 현재 제한

1. `quest_rewards.progress_flag_id`의 기술 보상 특수 접두어는 런타임 처리 코드와 CSV 입력 형식 검사가 서로 맞지 않습니다. 로더나 계약이 바뀌기 전에는 `skill.acquired.X`를 CSV에 넣지 않습니다.
2. `owner_npc_id`는 형식만 자동 검사합니다. 실제 NPC Scene ID나 Binding과 일치하는지는 기획자와 Unity 담당자가 별도로 확인합니다.
3. `pattern_index`의 실제 상한은 CSV가 아니라 각 원형 퍼즐 매니저의 Unity 목록 길이입니다.
4. 비주얼 카탈로그 누락은 경고일 수 있지만 출시 화면 품질에는 영향을 줍니다.
