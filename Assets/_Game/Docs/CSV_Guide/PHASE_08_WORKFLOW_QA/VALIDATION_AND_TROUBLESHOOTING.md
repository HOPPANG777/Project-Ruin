# 검증 및 문제 해결

## 1. 검증 층

| 층 | 잡는 문제 | 잡지 못하는 대표 문제 |
|---|---|---|
| CSV 파서 | 따옴표, 열 수, 행 구조 | 의미상 잘못된 ID |
| 개별 로더 | 헤더, 필수값, 숫자/Boolean/CodeList 형식 | 다른 파일 참조 |
| 저장소 구성 | 중복, 부모/자식, 참조, 도메인 규칙 | 장면/카탈로그 전체, 해결 가능성 |
| Editor 경고/테스트 | 일부 에셋 ID, 회귀 데이터 | 모든 플레이 분기 |
| PlayMode 수동 QA | UI, 흐름, 저장, 실제 해결 | 미실행 분기 |

문법 검증을 통과했다는 사실만으로 콘텐츠가 플레이 가능하다는 뜻은 아니다.

## 2. 빠른 진단 순서

1. Unity Console의 첫 번째 CSV 오류를 본다. 뒤 오류는 연쇄일 수 있다.
2. 오류가 난 파일의 첫 줄을 전용 가이드의 정확한 헤더와 비교한다.
3. 오류 행의 열 수와 쉼표/따옴표를 확인한다.
4. 필수 셀, ID 정규식, Boolean, 숫자를 확인한다.
5. 부모 ID와 로컬라이제이션/아이템/그래프 참조를 검색한다.
6. 복합키 범위의 중복을 확인한다.
7. 퍼즐이면 전체 좌표/링 완전성을 확인한다.
8. 자동 검증 후 PlayMode로 의미와 저장을 확인한다.

## 3. 증상별 점검표

| 증상/메시지 유형 | 가장 먼저 볼 것 |
|---|---|
| Header mismatch | 열 이름, 순서, 개수, BOM 외 숨은 문자 |
| Column count mismatch | 따옴표 없는 쉼표, 마지막 빈 열, 닫히지 않은 따옴표 |
| Invalid system ID | 대문자 시작, 대문자/숫자/밑줄만 사용 |
| Invalid localization key | 소문자 시작, 소문자/숫자/점/밑줄만 사용 |
| Invalid Boolean | `true`/`false` 소문자, 공백 없음 |
| Duplicate | 전역인지 부모 내부인지 해당 가이드 확인 |
| Missing reference | 부모 CSV가 먼저 로드되는지와 정확 문자열 확인 |
| Dialogue does not advance | 같은 그래프의 다음 노드, 선택지 노드의 node next 공백 확인 |
| Action rejected | `action_type`별 `action_id`와 next 규칙 확인 |
| Quest will not complete | 모든 목표 플래그와 소비 아이템 보유량 확인 |
| Reward not applied | 플래그/아이템 조건부 쌍, 이미 수령 상태 확인 |
| PIPE repository failure | 전 좌표, START/END 1개, 회전 0~3 확인 |
| LIGHT wrong tile toggles | 자기 자동 토글 가정 제거, 영향 체크리스트·가독성 보드·`|` 자동 생성 미리보기를 서로 대조 |
| CIRCLE prefab error | `pattern_index`가 Unity 배열 범위인지 확인 |
| Shop does not open | shop/stock TextAsset 연결, `OPEN_SHOP` 액션 규칙 확인 |
| Shop starts empty | `initial_stock=0` 여부 확인 |

## 4. CSV 문자 규칙

- 필드에 쉼표, 따옴표, 실제 줄바꿈이 있으면 필드를 큰따옴표로 감싼다.
- 필드 내부 `"`는 `""`로 쓴다.
- 파서는 따옴표 안 줄바꿈과 UTF-8 BOM을 처리한다.
- 완전히 빈 물리 행은 무시되지만 공백만 있는 행은 데이터 행이 될 수 있으므로 제거한다.
- 로컬라이제이션의 의도된 줄바꿈은 실제 여러 줄보다 `\n` 표기를 권장한다.

예:

```csv
ui.example,"쉼표, 포함 문장","He said ""go"".",
```

## 5. 필수/선택 TextAsset

런타임 `GameDataController` 기준 필수 19개는 대화 5, 아이템 1, 퀘스트 4, 로컬라이제이션 1, 퍼즐 4, 기술·재건·도감 4다. NPC 2개와 상점 2개는 선택 입력이다.

- 필수 TextAsset이 null이면 로딩을 진행할 수 없다.
- 선택 TextAsset이 null이면 해당 기능 데이터는 빈 목록으로 처리될 수 있다.
- 퍼즐 상세 CSV는 해당 종류의 행이 없어도 TextAsset 자체는 필수이므로 헤더 전용 파일을 유지한다.
- 회귀 테스트는 현재 23개 원본 파일을 모두 읽는다. 기술·재건·도감 4종은 헤더 전용 파일도 유효하지만 TextAsset 연결 자체는 필수다.

## 6. 현재 자동 검증 경계

자동으로 보장되지 않는 항목:

- `owner_npc_id`와 실제 NPC 존재
- 진행 플래그를 실제 코드/상호작용이 설정하는지
- 퍼즐 해결 가능성
- Circle 패턴 인덱스의 장면 배열 상한
- 모든 아이콘/초상화/배경 카탈로그 연결(일부는 경고)
- 선택 TextAsset의 현재 프리팹 직렬화 상태
- 기존 저장 슬롯과 ID 변경의 마이그레이션

## 7. 리뷰용 최소 테스트

1. 데이터 로드/저장소 구성 오류 0건
2. 변경한 콘텐츠의 정상 진입
3. 잘못된 조건 또는 부족 자원에서 안전한 실패
4. 완료/보상/구매/퍼즐 클리어 등 핵심 결과
5. 저장 후 장면 재진입 또는 앱 재실행 복원
6. 기존 대표 콘텐츠 회귀 확인

## 8. 관련 문서/코드

- [공통 작성 규칙](../PHASE_00_FOUNDATION/COMMON_AUTHORING_RULES.md)
- [교차 참조 지도](../PHASE_07_REFERENCE/CROSS_REFERENCE_MAP.md)
- [가이드 정정](../PHASE_00_FOUNDATION/EXISTING_GUIDE_CORRECTIONS.md)
- [GameDataController.cs](../../../Scripts/Gameplay/Data/GameDataController.cs)
- [GameDataCsvRegressionTests.cs](../../../Scripts/Editor/GameDataCsvRegressionTests.cs)
