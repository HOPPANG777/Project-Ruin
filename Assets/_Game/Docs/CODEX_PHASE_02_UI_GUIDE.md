# 도감 Phase 2 UI 가이드

## 구현 범위

- 펼친 책 형태의 왼쪽 1P와 오른쪽 2P
- 왼쪽 유물·술 탭과 독립적인 오른쪽 기술 목록
- 유물 정사각형 슬롯, 술 가로형 행, 기술 이미지 슬롯
- 세로 `ScrollRect`
- 유물·술·기술·재료가 함께 사용하는 `CodexEntryView` 풀
- 한 번에 하나만 표시하는 화면 경계 보정 Tooltip
- 잠긴 유물·술의 검은 실루엣과 잠긴 기술의 공유 흑백 Material
- 잠긴 이름 `???`, 잠긴 설명 `?`
- 술이 잠겨도 제조 재료와 위치는 표시
- 클릭 시 선택 강조만 적용

## 주요 파일

| 파일 | 역할 |
| --- | --- |
| `CodexPresenter.cs` | CSV 표시 데이터, Localization, 해제 상태를 UI에 중개 |
| `CodexView.cs` | 2P 책, 탭, ScrollRect, 풀, 단일 Tooltip 관리 |
| `CodexEntryView.cs` | 네 항목 종류의 표시와 Pointer 입력을 공통 처리 |
| `CodexUiGrayscale.shader` | 잠긴 기술 이미지의 공유 흑백 처리 |
| `CodexUiPrefabBuilder.cs` | 도감 UI 프리팹과 Material을 결정적으로 생성 |

## 프리팹 생성

Unity가 스크립트를 가져오면 프리팹이 없을 때 다음 에셋을 자동 생성합니다.

- `Assets/_Game/Prefabs/UI/CodexBook.prefab`
- `Assets/_Game/Prefabs/UI/CodexEntryView.prefab`
- `Assets/_Game/Prefabs/UI/CodexUiGrayscale.mat`

수동 재생성은 `Tools > Project Ruin > Codex > Rebuild Phase 2 UI Prefabs`를 사용합니다.

## 현재 연결 경계

Phase 2는 도감 UI 기반까지만 구현합니다. HUD 버튼, Caps Lock, X 닫기, 인벤토리·지도 전환과 입력 잠금은 Phase 3에서 연결합니다. 실제 콘텐츠 행이 아직 확정되지 않았으므로 프리팹은 헤더 전용 CSV에서도 빈 목록을 안전하게 표시합니다.

## 검수 항목

- 데이터 행 증감 시 기존 View를 재사용하고 남은 View를 비활성화하는가
- 유물·술 탭을 빠르게 반복해도 오른쪽 기술 목록이 유지되는가
- 긴 문장이 Tooltip 영역에서 줄바꿈되는가
- 아이콘이 없어도 항목과 Hover가 안전한가
- Pointer Exit 또는 UI 닫기 시 Tooltip이 사라지는가
- 반복 열기·닫기 뒤 이벤트가 중복 호출되지 않는가
- 풀의 최대 생성 이후 목록 갱신에서 추가 `Instantiate/Destroy`가 없는가
