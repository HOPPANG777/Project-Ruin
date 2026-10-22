# 일시정지·설정·업적 팝업 UI

## 에셋

- `Assets/_Game/Prefabs/UI/PausePopup.prefab`: HUD 톱니 버튼, 좌측 일시정지 메뉴, 설정·업적·나가기 확인 팝업을 포함한 독립 Canvas.
- `Assets/_Game/Prefabs/UI/AchievementSlot.prefab`: 업적 아이콘·이름·설명·진행 표시를 넣는 재사용 슬롯.
- `PausePopupView` / `IPausePopupView`: 창 전환과 표시·종료 요청 이벤트.
- `SettingsPopupView` / `ISettingsPopupView`: 설정 시스템이 주입받는 표시 계약.
- `SettingSliderView`: 슬라이더 하나의 백분율 표시와 값 변경 이벤트.
- `AchievementsPopupView`: 업적 목록 참조와 요약 문구 표시.

별도 게임 씬이나 GameRoot에 설치하지 않았다. 프리팹을 필요한 UI 계층에 배치한 뒤 연결할 수 있다. 자체 EventSystem은 없으므로 사용하는 씬의 EventSystem과 InputSystemUIInputModule을 공유한다. Canvas 기준 해상도는 1920×1080, Sorting Order는 500이다.

기존 선택지 Sprite, 퀘스트 닫기 아이콘, 퍼즐 테두리와 Bazzi 폰트를 재사용한다. 톱니 아이콘은 크기에 맞춰 그리는 UI Graphic이다.

## 화면 흐름

```text
톱니 버튼 / 외부 OpenPauseMenu 호출
  → 일시정지 메뉴
      → 설정 → 닫기 → 일시정지 메뉴
      → 업적 보기 → 닫기 → 일시정지 메뉴
      → 나가기 → 취소 / 닫기 → 일시정지 메뉴
      → 계속하기 / 닫기 → 팝업 닫힘
```

설정 슬라이더는 표시 수치와 변경 이벤트만 갱신한다. 업적은 임시 표시 슬롯 6개와 스크롤만 구성했다. 실제 업적 조건, 해금, 보상, 저장 데이터는 없다.

## 외부에서 연결할 메서드

| 메서드 | 연결 용도 |
| --- | --- |
| `HandleCancel()` | ESC/InputAction Cancel 진입점. 닫혀 있으면 열고, 하위 팝업이면 메뉴로 돌아가며, 메뉴에서는 닫는다. |
| `TogglePauseMenu()` | 기존 HUD 버튼에서 전체 열기·닫기. |
| `SetLauncherVisible(bool)` | 기존 HUD 버튼을 사용할 때 프리팹의 기본 톱니 버튼 표시를 끄거나 켜기. Inspector의 Show Launcher Button으로도 조정한다. |
| `OpenPauseMenu()` / `CloseAll()` | 외부 시스템의 명시적 열기·닫기. |
| `ShowSettings()` / `ShowAchievements()` | 특정 팝업으로 직접 진입. |
| `ShowExitConfirmation()` | 나가기 확인창 표시. |
| `ISettingsPopupView.SetSettingsValues(bgm, effects, brightness)` | 설정 창에서 실제 설정 값을 0–1 범위로 표시. 변경 이벤트를 재발행하지 않는다. |
| `AchievementsPopupView.SetSummary(text)` | 업적 창 상단 요약 표시 교체. |
| `AchievementSlotView.SetDisplay(...)` | 각 슬롯의 아이콘·이름·설명·진행 문구·0–1 진행 표시 교체. |

`AchievementsPopupView.Slots`와 `Scroll.content`를 통해 목록을 채우거나 슬롯 프리팹을 추가할 수 있다. 임시 슬롯의 개수와 모든 문구는 프리팹에서 직접 편집할 수 있다.

## Inspector / 코드에서 연결할 이벤트

| 이벤트 | 전달 값 | 나중에 연결할 대상 |
| --- | --- | --- |
| `VisibilityChanged` | `bool` | 일시정지 상태, 플레이어 입력 잠금, 다른 UI 입력 차단. 하위 창 전환에는 재발행하지 않는다. |
| `ISettingsPopupView.BgmVolumeChanged` | `float` 0–1 | BGM 설정 시스템. |
| `ISettingsPopupView.EffectsVolumeChanged` | `float` 0–1 | 효과음 설정 시스템. |
| `ISettingsPopupView.BrightnessChanged` | `float` 0–1 | 화면 밝기 설정 시스템. |
| `ExitRequested` | 없음 | 타이틀 복귀 또는 게임 종료 처리. 확인창의 확인 버튼에서만 발행한다. |

기본 프리팹에서 위 이벤트는 모두 비어 있다. Inspector에서는 각 슬라이더의 `SettingSliderView.Value Changed`에 연결한다. 외부 입력·설정 처리기는 `Init(IPausePopupView, ISettingsPopupView)` 같은 초기화 인수로 뷰를 주입받아 사용할 수 있다.

`Time.timeScale`, 플레이어 이동, AudioMixer, 화면 후처리, 설정 저장, 씬 전환, `Application.Quit`을 호출하지 않는다. ESC를 직접 감시하는 Update도 없다. 기존 인벤토리·퍼즐 UI와 ESC가 동시에 처리되지 않도록 나중에 입력 라우터에서 우선순위를 정한다.

슬라이더의 초기 표시값은 BGM 70%, 효과음 80%, 밝기 100%이며 실제 게임 설정값을 의미하지 않는다. 창을 연결할 때 `SetSettingsValues`로 동기화한다. 종료 요청 이후 창을 닫거나 전환하는 일도 이벤트 구독자가 결정한다.

## 편집과 검증

프리팹은 닫힌 상태로 저장되어 있다. Prefab Mode에서 `PauseRoot`를 켜면 좌측 메뉴를 편집할 수 있다. 하위 창은 `PauseRoot/ModalRoot`와 그 안의 `SettingsPanel`, `AchievementsPanel`, `ExitConfirmationPanel` 중 하나를 켜서 확인한다. 런타임 초기화에서는 항상 닫힌 상태로 시작한다.

화면은 저장된 프리팹을 직접 편집한다. 일회성 생성기와 중복 미리보기 스크립트는 제거했다. 검증용 코드는 게임 소스에 포함하지 않는다.

미리보기의 게임 배경은 기존 맵 이미지이며 팝업 프리팹에는 포함되지 않는다.
