# 타이틀 화면

대상 씬: `Assets/_Game/Scenes/Chapter/TitleScene.unity`

## 화면 구성

- `TitleCanvas/MainIllustration`: 메인 일러스트를 넣을 전체 화면 Image. 현재는 세이지색 바탕이다. Source Image에 Sprite를 넣으면 교체할 수 있다.
- `TitleCanvas/SafeArea`: 프로젝트 타이틀과 좌측 하단의 새 게임 시작 / 불러오기 / 나가기 메뉴.
- `TitleCanvas/ModalLayer/NewGamePanel`: 새 게임 확인 문구, 시작 버튼, 닫기 버튼.
- `TitleCanvas/ModalLayer/LoadGamePanel`: 저장 슬롯 3개와 닫기 버튼.
- `TitleCanvas/BusyLayer`: 저장 및 씬 전환 중 중복 입력을 막는 화면.

인게임 선택지 Sprite, 퀘스트 닫기 아이콘, 퍼즐 패널 테두리, Bazzi SDF 폰트를 재사용한다. Canvas는 1920×1080 기준으로 비례 조절하며 메뉴와 타이틀에 기기의 Safe Area를 적용한다. 타이틀 UI는 편집 모드에서도 보인다.

## 코드 역할과 주입

- `TitleScreenPresenter`: 모달 표시와 중복 입력 차단. `Init(ITitleGameService)`로 진입 서비스를 받는다.
- `TitleGameEntry`: 직렬화된 구성을 표시부에 주입하고 저장 복구·초기화·씬 진입을 수행한다. 실행 로직은 `IProgressService`에만 의존하며 `Init(IProgressService, bool)`로 대체 서비스를 주입할 수 있다.
- `UiSafeArea`: 같은 오브젝트의 RectTransform에 화면 영역 변경만 반영한다.

타이틀은 GameRoot 생성 전의 씬이므로 `TitleGameEntry.Awake`가 초기 연결을 수행한다. 저장 유무는 타이틀 진입 때 한 번 확인한다.

## 버튼 동작과 현재 저장 범위

- 새 게임 확인창의 시작을 누르면 기존 `ProgressController.Reset → SaveAsync`를 사용한 뒤 `Chapter0`을 연다. 확인창에 기존 저장 교체를 안내한다.
- 불러오기의 슬롯 01은 기존 단일 `ProjectRuinSave.es3`의 진행을 읽는다. 파일에 저장 데이터가 없으면 비활성화한다.
- 현재 저장 형식은 마지막 방문 씬을 기록하지 않는다. 불러오기는 `Chapter0`에서 저장된 진행을 복원하며 이를 팝업 하단에 표시한다.
- 슬롯 02와 03은 UI 배치용으로 마련되어 있으며 비활성 상태이다. 독립적인 다중 슬롯 저장 기능은 추가하지 않았다.
- ESC나 바깥 배경, 닫기 버튼으로 팝업을 닫는다. 키보드와 게임패드의 UI 탐색을 지원한다.
- 나가기는 빌드에서 게임을 종료하고 에디터에서는 플레이 모드를 종료한다.

## 편집과 확인

씬의 RectTransform과 TMP 텍스트를 직접 편집할 수 있다. 메인 일러스트를 추가할 때 `MainIllustration`의 Color를 흰색으로 바꾸면 원본 색상으로 표시된다.

일회성 씬 생성기와 중복 미리보기 스크립트는 제거했다. 저장된 씬의 UI를 직접 편집하며, 검증용 코드는 게임 소스에 포함하지 않는다.
