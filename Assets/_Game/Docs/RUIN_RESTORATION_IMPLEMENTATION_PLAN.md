# 유적 재건 및 기술 되돌려놓기 구현 계획

## 1. 문서 목적

이 문서는 다음 플레이 흐름을 현재 Project-Ruin 구조에 맞게 구현하기 위한 기준 문서다.

> 기술 획득 → 제단 상호작용 → 기술 되돌려놓기 → 재건도 증가 → 복원 컷씬 → 유적 상태 변경 → 100% 달성 시 엔딩

구현자는 기능을 추가하기 전에 이 문서와 루트의 [CONVENTIONS.md](../../../CONVENTIONS.md)를 함께 확인한다. 두 문서가 충돌하면 `CONVENTIONS.md`를 우선한다.

이 계획은 다음 목표를 동시에 만족해야 한다.

- 처음 읽는 사람도 데이터와 실행 흐름을 빠르게 이해할 수 있어야 한다.
- 코드의 의도와 책임이 클래스 이름과 공개 API에서 드러나야 한다.
- 런타임 중 불필요한 탐색, 할당, 반복 계산을 만들지 않아야 한다.
- 스크립트를 지나치게 쪼개지 않으면서도 거대한 단일 클래스가 생기지 않아야 한다.
- 기능 확장보다 현재 기획 범위를 단순하고 안정적으로 완성하는 것을 우선한다.

---

## 2. 절대 준수 규칙

아래 항목은 권장이 아니라 구현과 코드 리뷰에서 반드시 통과해야 하는 규칙이다.

### 2.1 `CONVENTIONS.md` 준수

- 전역 싱글턴은 기존 `GameRoot` 하나만 사용한다.
- 전역 도메인 기능은 `Controller`로 작성하고 `GameRoot`에서 초기화한다.
- 외부에는 구현체가 아니라 인터페이스인 `Service` 계약만 노출한다.
- `MonoBehaviour` 의존성은 `GameRoot.Start()`에서 `Init(...)`으로 주입한다.
- 깊은 상속을 만들지 않고 구성 방식으로 조립한다.
- Coroutine을 사용하지 않는다.
- UniTask는 씬 로딩과 파일 I/O처럼 실제 비동기가 필요한 곳에만 사용한다.
- DOTween은 UI와 값 보간 연출에만 사용하고 게임 상태 전환의 기준으로 사용하지 않는다.
- 영속화는 Easy Save 3만 사용한다. `PlayerPrefs` 또는 별도 저장 방식을 추가하지 않는다.
- 공개 API는 XML 주석을 작성하고, 새 C# 파일은 파일 헤더와 섹션 구분 규칙을 따른다.
- 직렬화 필드는 `private`와 `[SerializeField]`를 사용하고 모든 필드에 `[Tooltip]`을 작성한다.
- 실패할 수 있는 조회와 상태 변경은 `TryXxx` 패턴과 가드절을 우선한다.
- 컬렉션 순회는 배열과 리스트에 `for`, Dictionary에 `foreach`를 사용한다.

### 2.2 가독성 우선

- 짧은 코드보다 한 번에 의미가 읽히는 코드를 우선한다.
- 한 메서드는 한 가지 판단 또는 한 가지 상태 변경만 수행한다.
- 중첩 조건문은 가드절로 줄이고 정상 흐름은 아래로 곧게 읽히게 작성한다.
- 의미 없는 축약어, 매직 넘버, 매직 스트링을 사용하지 않는다.
- 공개 메서드 이름만 읽어도 호출 조건과 결과를 예상할 수 있어야 한다.
- 주석으로 복잡한 코드를 변명하지 않는다. 먼저 코드를 단순화한 뒤 필요한 의도만 주석으로 남긴다.
- 같은 조건과 상태 계산을 UI, 제단, 컷씬 코드에 중복 작성하지 않는다.

### 2.3 C# 파일당 순수 로직 300줄 제한

- 모든 신규 또는 수정 C# 파일은 **순수 로직 300줄을 초과할 수 없다.**
- 순수 로직은 `using`, 빈 줄, 파일 헤더, XML 문서화, 설명 주석을 제외한 실제 선언과 실행 코드다.
- 권장 목표는 200줄 이하이며, 250줄부터는 리뷰 경고 구간으로 본다.
- 300줄에 가까워지면 먼저 다음 순서로 줄인다.

  1. 중복 조건과 중복 상태 계산을 제거한다.
  2. 사용하지 않는 확장 가능성, 추상화, 래퍼를 삭제한다.
  3. 데이터 기반으로 바꿀 수 있는 분기를 Catalog로 옮긴다.
  4. 긴 메서드를 작은 private 메서드로 정리하되 의미 없는 한 줄 래퍼는 만들지 않는다.
  5. 그래도 독립 책임이 두 개 이상이면 그때만 파일 분리를 검토한다.

- 300줄을 맞추기 위해 여러 관련 없는 타입을 한 파일에 억지로 넣지 않는다.
- 반대로 작은 DTO나 열거형마다 파일을 하나씩 만들지 않는다. 한 기능에서만 사용하는 작은 타입은 관련 계약 또는 Catalog 안에 중첩한다.
- 300줄 초과가 불가피하다고 판단되면 구현을 진행하지 않고 구조와 이유를 먼저 리뷰받는다.

### 2.4 스크립트 수 제한

- MVP 기준 신규 런타임 C#은 최대 6개다.
- 자동 테스트 C#은 최대 1개다.
- 따라서 이 기능을 위해 추가하는 신규 C# 파일은 기본적으로 총 7개를 넘지 않는다.
- 새 `Manager`, 새 싱글턴, 별도 텔레포트 Controller, 데이터 타입별 파일을 만들지 않는다.
- 스크립트 수를 줄이기 위해 한 클래스에 서로 다른 책임을 몰아넣는 것도 금지한다.
- 신규 파일이 정말 필요하면 기존 7개 중 하나와 책임이 겹치지 않는지 먼저 검토하고 파일 예산 변경을 승인받는다.

### 2.5 성능과 안정성

- 재건 시스템 때문에 매 프레임 전체 기술이나 전체 씬 오브젝트를 탐색하지 않는다.
- `GameObject.Find`, `FindObjectOfType`, `FindObjectsByType`를 새 코드에 사용하지 않는다.
- 반복 호출 경로에서 LINQ를 사용하지 않는다.
- 기술 ID 조회용 Dictionary와 고정 목록은 초기화 시 한 번 만든 뒤 재사용한다.
- 재건도는 기술 반납 또는 로드 시점에만 다시 계산하고 매 프레임 계산하지 않는다.
- UI를 열 때마다 버튼을 `Instantiate/Destroy`하지 않는다. 최대 항목 수에 맞춘 고정 버튼 풀을 재사용한다.
- UI 목록용 List와 임시 버퍼는 멤버로 한 번 생성해 `Clear()` 후 재사용한다.
- 버튼 이벤트는 초기화 시 한 번 연결하고 `OnDestroy`에서 해제한다.
- 저장은 모든 프레임이나 모든 조회에서 실행하지 않는다. 상태가 확정되는 체크포인트에서만 수행한다.
- 컷씬, 저장, 씬 전환 도중 중복 입력을 차단한다.
- 취소 또는 예외가 발생해도 플레이어 이동과 상호작용 잠금이 반드시 복구되어야 한다.

---

## 3. 구현 기본 결정

기획서에서 확정되지 않은 부분은 MVP에서 다음과 같이 처리한다.

1. 기술을 되돌려놓아도 플레이어의 실제 능력은 제거하지 않는다.
2. 되돌려놓기는 기술 삭제가 아니라 `획득됨 → 복원됨` 진행 상태 변경이다.
3. 기술은 한 번에 하나씩 선택하고 확인 후 되돌려놓는다.
4. 반납 가능한 새 기술이 있으면 기술 되돌려놓기 화면을 우선 연다.
5. 반납 가능한 기술이 없으면 텔레포트 화면을 연다.
6. 컷씬과 복원 단계는 결과 재건도 `30`, `60`, `100`을 기준으로 결정한다.
7. 챕터 0 텔레포트 기술은 반납되지만 재건도와 복원 컷씬을 발생시키지 않는다.
8. 100%에서는 3차 복원 컷씬을 먼저 재생한 후 엔딩 씬으로 이동한다.
9. 현재 제단과 비활성화된 제단은 텔레포트 목적지에 표시하지 않는다.

기획이 변경되면 분산된 `if`문을 수정하지 않고 Catalog 데이터 또는 `RelicRestorationController`의 단일 규칙만 수정한다.

---

## 4. 기존 코드 재사용 범위

새 기능을 만들기 전에 아래 기반을 그대로 재사용한다.

| 기존 기능 | 재사용 위치 | 새로 만들지 않을 것 |
| --- | --- | --- |
| `IInteractable` / `PlayerInteractionController` | 제단 상호작용 진입 | 별도 상호작용 입력 시스템 |
| `IStageFlowService.FadeThroughAsync` | 텔레포트 암전 연출 | 별도 페이드 Controller |
| `IStageFlowService.ChangeStageAsync` | 다른 스테이지 제단 이동 | 별도 씬 로더 |
| `ProgressController` | 기술·제단·엔딩 상태 저장 | 재건 전용 Save Manager |
| `GameRoot` | 재건 Controller 초기화와 Service 공개 | 새 싱글턴 |
| Dialogue의 플레이어 조작 잠금 패턴 | 제단 UI와 컷씬 조작 차단 | 입력 시스템 중복 구현 |
| Unity Timeline / Cinemachine | 복원 컷씬과 카메라 연출 | 별도 컷씬 프레임워크 |

인벤토리의 `IsNewItem`은 사용하지 않는다. 인벤토리의 new 표시는 아이템을 확인하면 해제되지만, 기술의 새 상태는 제단에 되돌려놓기 전까지 유지되어야 하기 때문이다.

---

## 5. 최소 스크립트 구조

폴더 구조 가이드에서 `Gameplay/Relics`는 유적, 기억, 기술 수집과 재건 진행 로직을 위한 위치다. 신규 스크립트는 다음 7개만 기본안으로 사용한다.

```text
Assets/_Game/Scripts/
├── Gameplay/Relics/
│   ├── IRelicRestorationService.cs
│   ├── RelicRestorationCatalogSO.cs
│   ├── RelicRestorationController.cs
│   └── AltarInteractable.cs
├── Presentation/UI/Relics/
│   ├── AltarPresenter.cs
│   └── RelicRestorationPresenter.cs
└── Tests/Editor/
    └── RelicRestorationControllerTests.cs
```

### 5.1 파일 책임과 줄 수 예산

| 파일 | 단일 책임 | 목표 | 절대 상한 |
| --- | --- | ---: | ---: |
| `IRelicRestorationService.cs` | 외부 계약과 작은 결과 타입 | 100줄 | 180줄 |
| `RelicRestorationCatalogSO.cs` | 기술, 단계, 컷씬, 제단 목적지 정의 | 180줄 | 250줄 |
| `RelicRestorationController.cs` | 획득·반납 검증과 재건 상태 계산 | 220줄 | 300줄 |
| `AltarInteractable.cs` | 현재 제단 정보를 Presenter에 전달 | 80줄 | 120줄 |
| `AltarPresenter.cs` | 제단 UI와 반납·텔레포트 흐름 조정 | 240줄 | 300줄 |
| `RelicRestorationPresenter.cs` | 유적 단계 표시와 Timeline 재생 | 200줄 | 280줄 |
| `RelicRestorationControllerTests.cs` | 도메인·저장·줄 수 규칙 검증 | 230줄 | 300줄 |

작은 결과 구조체, Catalog Entry, UI 버튼 Binding은 해당 파일 내부의 중첩 타입으로 둔다. 다른 시스템에서도 재사용될 때만 별도 파일 분리를 검토한다.

### 5.2 수정할 기존 파일

신규 스크립트를 늘리지 않고 다음 기존 파일을 최소 범위로 수정한다.

| 기존 파일 | 수정 내용 |
| --- | --- |
| `SkillDataSO.cs` | Skill ID, 표시 키, 챕터, 재건도 증가량, 아이콘 추가 |
| `IProgressService.cs` | 재건 상태 저장에 필요한 최소 Save/Load 계약 추가 |
| `ProgressController.cs` | ES3 기반 저장·로드와 Dirty 상태 추가 |
| `GameRoot.cs` | Controller 참조, Service 공개, Init 순서 추가 |
| `GameRoot.prefab` | Controller·Presenter 직렬화 참조 연결 |

기존 파일도 수정 후 순수 로직 300줄 제한을 검사한다. 제한에 가까워진 경우 새 파일부터 만들지 말고 중복과 불필요한 API를 먼저 제거한다.

---

## 6. 데이터 설계

### 6.1 고정 데이터와 진행 상태 분리

`SkillDataSO`에는 플레이 중 변하지 않는 정보만 저장한다.

```text
SkillId
DisplayNameKey
DescriptionKey
Icon
Chapter
RestoreValue
```

`IsAcquired`, `IsReturned`는 ScriptableObject에 넣지 않는다. 해당 값은 세이브 슬롯마다 달라지는 진행 상태다.

### 6.2 기술 데이터

| Skill ID | 챕터 | 표시 이름 | 재건도 |
| --- | ---: | --- | ---: |
| `SKILL_TELEPORT` | 0 | 텔레포트 | 0 |
| `SKILL_TELEKINESIS` | 1 | 염력 | 30 |
| `SKILL_CARVING` | 2 | 조각 | 30 |
| `SKILL_REVIVAL` | 3 | 부활 | 40 |

### 6.3 진행 플래그

플래그 접두사와 Save Key는 상수로 한곳에서 관리한다.

```text
skill.acquired.SKILL_TELEKINESIS
skill.returned.SKILL_TELEKINESIS
altar.activated.MAIN_ALTAR
ending.restoration.completed
```

현재 재건도는 별도의 원본 값으로 저장하지 않는다. 반납 완료된 기술의 `RestoreValue` 합으로 로드와 반납 시점에 계산한다.

이 방식은 다음 불일치를 방지한다.

```text
염력은 반납 완료인데 CurrentRestore가 0인 상태
같은 기술을 두 번 더해 CurrentRestore가 60이 된 상태
재건도가 100을 넘은 상태
```

### 6.4 재건 단계

| 재건도 | 단계 | 표시 상태 | 컷씬 |
| ---: | ---: | --- | --- |
| 0 | 0 | 파괴 상태 | 없음 |
| 30 | 1 | 1차 복원 | 1차 복원 Timeline |
| 60 | 2 | 2차 복원 | 2차 복원 Timeline |
| 100 | 3 | 최종 복원 | 3차 복원 Timeline |

임계값은 코드에 여러 번 쓰지 않고 Catalog 한곳에 정의한다.

---

## 7. 클래스별 설계

### 7.1 `IRelicRestorationService`

외부 시스템이 알아야 하는 최소 기능만 제공한다.

```text
CurrentRestore
MaxRestore
RestoreStage
HasReturnableSkill()
FillReturnableSkills(buffer)
IsAcquired(skillId)
IsReturned(skillId)
TryAcquireSkill(skillId)
TryReturnSkill(skillId, out result)
```

`GetReturnableSkills()`가 매번 새 List를 반환하지 않도록 호출자가 전달한 재사용 버퍼를 채우는 방식을 사용한다.

### 7.2 `RelicRestorationController`

이 클래스가 재건 상태의 유일한 진실 공급원이다.

담당하는 것:

- Catalog ID 캐시 생성
- 기술 획득 여부 기록
- 기술 반납 조건 검사
- 중복 반납 방지
- 현재 재건도와 단계 계산
- 상태 변경 결과 반환
- Progress 플래그 동기화

담당하지 않는 것:

- UI 표시
- 버튼 이벤트
- 플레이어 이동
- Timeline 재생
- 유적 GameObject 활성화
- 씬 전환

`TryReturnSkill`은 다음 순서로 처리한다.

1. ID가 비어 있지 않은지 검사한다.
2. Catalog에 등록된 기술인지 검사한다.
3. 기술을 획득했는지 검사한다.
4. 이미 반납했는지 검사한다.
5. 이전 재건도와 단계를 기록한다.
6. 반납 플래그를 기록한다.
7. 재건도와 단계를 한 번 다시 계산한다.
8. 후속 연출에 필요한 `RelicRestorationResult`를 반환한다.

### 7.3 `AltarInteractable`

`IInteractable`을 구현하는 얇은 진입점이다.

보유 정보:

```text
AltarId
StageNumber
SpawnPointId
IsMainAltar
AltarPresenter 참조
```

`Interact()`는 현재 제단 정보를 `AltarPresenter.Open(this)`에 전달하는 것까지만 담당한다. 기술 검사와 텔레포트 판단을 직접 하지 않는다.

### 7.4 `AltarPresenter`

다음 UI 흐름만 조정한다.

```text
제단 열기
→ 반납 가능 목록 요청
→ 기술 목록 또는 텔레포트 목록 표시
→ 선택 확인
→ Controller 상태 변경 요청
→ 복원 Presenter 재생 요청
→ 저장 요청 또는 스테이지 이동
```

최대 기술 4개와 제단 목적지 수에 맞춰 버튼을 미리 배치하고 재사용한다. 작은 슬롯별 View 스크립트는 만들지 않는다.

직렬화 가능한 중첩 `ButtonBinding`에 다음 참조를 묶는다.

```text
Root GameObject
Button
Icon Image
Name TMP_Text
State TMP_Text
```

### 7.5 `RelicRestorationPresenter`

유적 오브젝트와 Timeline을 한곳에서 처리한다.

보유 정보:

```text
파괴 상태 Root
1차 복원 Root
2차 복원 Root
최종 복원 Root
PlayableDirector
복원 단계별 TimelineAsset
플레이어 조작 Controller 참조
```

컷씬 대기는 Coroutine 또는 프레임 대기 UniTask로 구현하지 않는다. 상태 enum과 `Update()`를 사용하되 IDLE 상태에서는 즉시 return한다.

```text
RESTORATION_IDLE
RESTORATION_PLAYING
RESTORATION_APPLYING
RESTORATION_ENDING
```

Timeline 완료 여부를 확인한 뒤 유적 단계를 적용하고 Presenter에 완료 결과를 전달한다. 게임 상태 전환을 DOTween 완료 콜백에 연결하지 않는다.

---

## 8. 실행 흐름

### 8.1 기술 획득

```text
보스·퀘스트·이벤트
→ TryAcquireSkill(skillId)
→ acquired 플래그 기록
→ Progress Dirty 처리
→ 체크포인트 저장
```

획득 콘텐츠는 재건도나 UI를 직접 수정하지 않는다.

### 8.2 기술 되돌려놓기

```text
제단 상호작용
→ 반납 가능 기술 존재
→ 기술 버튼 표시
→ 기술 선택
→ 확인 팝업
→ TryReturnSkill
→ UI 닫기
→ 플레이어 조작 잠금
→ 복원 Timeline
→ 유적 단계 적용
→ 저장
→ 조작 복구
```

챕터 0 기술은 반납 처리와 저장까지만 수행하고 복원 Timeline은 건너뛴다.

### 8.3 100%와 엔딩

```text
부활 기술 반납
→ 재건도 100 / 3단계
→ 3차 복원 Timeline
→ 최종 유적 Root 적용
→ ending.restoration.completed 저장
→ ENDING 씬 전환
```

### 8.4 텔레포트

```text
제단 상호작용
→ 반납 가능 기술 없음
→ 활성화된 목적지 목록 표시
→ 현재 제단 제외
→ 목적지 선택
→ StageFlow 전환 선점
→ UI 닫기
→ ChangeStageAsync(stageNumber, transferData)
```

화면 암전과 SpawnPoint 복구는 기존 StageFlow를 재사용한다.

---

## 9. 저장 전략

`ProgressController`가 기존 플래그와 재건 관련 플래그를 함께 저장한다. 재건 전용 저장 클래스를 새로 만들지 않는다.

### 9.1 저장 대상

- 기술 획득 플래그
- 기술 반납 플래그
- 제단 활성화 플래그
- 엔딩 완료 플래그
- 기존 퀘스트와 오브젝트 진행 상태

### 9.2 저장 시점

- 기술 획득 확정 후
- 기술 반납과 컷씬 종료 후
- 제단 최초 활성화 후
- 스테이지 전환 직전
- 명시적인 세이브 포인트

### 9.3 최적화 기준

- `SetFlag`는 메모리 상태를 갱신하고 Dirty만 설정한다.
- 조회 API는 파일 I/O를 발생시키지 않는다.
- 저장할 변경이 없으면 디스크 쓰기를 건너뛴다.
- Save Key와 파일 경로는 상수로 관리한다.
- ES3 I/O는 예외를 처리하고 실패 로그에 `[ProgressController]` 접두사를 사용한다.
- 같은 프레임에 여러 플래그가 바뀌면 한 번의 저장으로 묶는다.

---

## 10. 구현 순서

### 단계 1: 규칙과 데이터 준비

- `SkillDataSO`에 고정 필드를 추가한다.
- 기술 에셋 4개를 만든다.
- Catalog에 기술, 임계값, Timeline, 제단 목적지를 등록한다.
- ID 중복, 빈 ID, 음수 값, 재건도 합계 100을 검증한다.

완료 조건:

- 에디터에서 기술 4개와 재건 단계가 명확하게 보인다.
- 잘못된 데이터는 플레이 전에 검출된다.

### 단계 2: 순수 재건 로직 구현

- `IRelicRestorationService`를 작성한다.
- `RelicRestorationController`를 작성한다.
- 염력 하나를 대상으로 획득, 반납, 30%, 중복 방지를 검증한다.

완료 조건:

- UI와 Unity 씬 없이도 테스트에서 재건 상태가 계산된다.
- `TryReturnSkill` 실패 이유가 호출부에서 구분 가능하다.

### 단계 3: 저장·불러오기

- `ProgressController`에 ES3 Save/Load와 Dirty 처리를 추가한다.
- 획득·반납 플래그를 저장하고 다시 로드한다.
- 현재 재건도는 로드된 반납 플래그로 재계산한다.

완료 조건:

- 게임 재실행 후 염력 반납과 재건도 30%가 유지된다.

### 단계 4: 최소 제단 UI

- 메인 제단에 `AltarInteractable`을 연결한다.
- `AltarPresenter`에 고정 버튼 풀과 확인 팝업을 연결한다.
- UI가 열릴 때 이동과 상호작용을 잠근다.

완료 조건:

- 획득한 염력만 목록에 표시된다.
- 취소하면 상태가 바뀌지 않는다.
- 확인을 연속 입력해도 한 번만 반납된다.

### 단계 5: 1차 복원 수직 슬라이스

- 파괴 상태와 1차 복원 Root를 연결한다.
- 임시 1차 Timeline을 연결한다.
- 컷씬 종료 후 1차 복원 상태를 적용한다.
- 저장 후 다시 실행해도 1차 복원이 유지되는지 확인한다.

완료 조건:

> 염력 획득 → 제단 → 반납 → 30% → 1차 컷씬 → 1차 복원 → 재실행 후 유지

이 한 줄이 완전히 통과하기 전에는 나머지 챕터를 추가하지 않는다.

### 단계 6: 나머지 기술과 엔딩

- 챕터 0, 2, 3 기술 데이터를 추가한다.
- 2차, 3차 Root와 Timeline을 연결한다.
- 100%에서 엔딩 씬으로 이동한다.

완료 조건:

- `0 → 30 → 60 → 100` 진행이 정확하다.
- 챕터 0 반납은 재건도를 올리지 않는다.
- 엔딩이 중복 재생되지 않는다.

### 단계 7: 텔레포트

- 각 제단 활성화 플래그를 연결한다.
- 반납할 기술이 없을 때 목적지 버튼을 표시한다.
- 현재 제단과 비활성 제단을 제외한다.
- 기존 StageFlow로 스테이지와 SpawnPoint를 전환한다.

완료 조건:

- 별도 페이드 또는 씬 로딩 코드를 추가하지 않고 이동한다.
- 이동 중 중복 입력이 차단된다.

---

## 11. 테스트 계획

### 11.1 자동 테스트

`RelicRestorationControllerTests.cs` 한 파일에 관련 테스트를 모은다.

- 획득하지 않은 기술 반납 실패
- 빈 ID와 미등록 ID 처리
- 같은 기술 중복 반납 실패
- 챕터 0 반납 후 재건도 0
- 염력 반납 후 30, 1단계
- 조각까지 반납 후 60, 2단계
- 부활까지 반납 후 100, 3단계
- 재건도 100 초과 방지
- Reset 후 모든 상태 초기화
- Save/Load 후 획득·반납 상태 유지
- 대상 C# 파일 순수 로직 300줄 초과 검사

### 11.2 플레이 모드 검증

- 제단 범위 밖에서는 상호작용할 수 없다.
- 새 기술이 있으면 기술 화면, 없으면 텔레포트 화면이 열린다.
- 팝업 취소 시 아무 상태도 변경되지 않는다.
- 컷씬 중 이동, 공격, 상호작용, UI 중복 입력이 차단된다.
- 컷씬 종료 또는 취소 후 조작이 복구된다.
- 재진입과 게임 재실행 후 올바른 유적 단계가 보인다.
- 현재 제단과 비활성 제단이 목적지에 표시되지 않는다.
- 다른 스테이지 이동 후 올바른 SpawnPoint에 배치된다.

### 11.3 성능 검증

- Unity Profiler에서 제단 UI를 반복해서 열고 닫아 GC Alloc을 확인한다.
- 버튼 목록 갱신 시 매회 Instantiate/Destroy가 없는지 확인한다.
- 재건 시스템이 IDLE 상태일 때 의미 있는 CPU 사용이 없는지 확인한다.
- 기술 반납과 컷씬 종료 시에만 재건도 계산이 실행되는지 확인한다.
- 저장 호출이 같은 프레임에 중복 실행되지 않는지 확인한다.
- Timeline 종료 후 이벤트와 버튼 리스너가 남지 않는지 확인한다.

---

## 12. 금지 목록

다음 구현은 코드 리뷰에서 허용하지 않는다.

- 재건 전용 싱글턴 또는 `Manager` 추가
- 제단마다 재건도 계산 로직 복사
- UI가 직접 Progress 플래그 수정
- ScriptableObject의 `IsReturned` 값을 런타임에 변경
- 인벤토리 `IsNewItem`을 기술 반납 상태로 사용
- 컷씬 시간을 하드코딩한 `Delay`로 기다리기
- 게임 상태 전환을 DOTween `OnComplete`에 의존
- Coroutine 사용
- UI를 열 때마다 버튼 프리팹 생성과 파괴 반복
- 매 프레임 모든 기술과 모든 제단 검색
- 반복 경로의 LINQ 사용
- `GameObject.Find` 계열 사용
- 빈 ID와 null 참조를 검사하지 않는 공개 API
- 동일한 재건도, 단계, 반환 조건을 여러 클래스에서 각각 계산
- 신규 런타임 C# 6개 또는 전체 신규 C# 7개를 승인 없이 초과
- C# 파일의 순수 로직 300줄 초과

---

## 13. 작업 단위와 완료 기준

한 번의 작업은 아래 크기를 넘지 않는다.

1. 데이터 정의와 검증
2. 순수 재건 로직
3. 저장·불러오기
4. 제단 UI
5. 복원 오브젝트와 컷씬
6. 텔레포트
7. 엔딩과 전체 검증

각 작업 완료 시 다음을 확인한다.

- `dotnet build` 또는 Unity 컴파일 오류가 없다.
- 관련 자동 테스트가 통과한다.
- 변경한 C# 파일의 순수 로직이 300줄 이하이다.
- 신규 스크립트 누적 개수가 예산 안에 있다.
- `CONVENTIONS.md`의 이름, 주석, DI, 저장, 비동기 규칙을 지켰다.
- Profiler 또는 코드 검토로 새 반복 할당과 불필요한 Update가 없음을 확인했다.
- 하나의 논리적 변경 단위로 커밋할 수 있는 상태다.

---

## 14. 최종 완료 정의

다음 항목이 모두 만족되어야 유적 재건 시스템 구현 완료로 본다.

- 기술 4개의 획득과 반납 상태가 정확히 관리된다.
- 기술을 두 번 반납할 수 없다.
- 재건도가 `0 → 30 → 60 → 100`으로 정확히 진행된다.
- 30, 60, 100에서 올바른 컷씬과 유적 상태가 적용된다.
- 챕터 0 기술은 재건도를 올리지 않는다.
- 반납할 기술이 없으면 활성화된 제단으로 텔레포트할 수 있다.
- 현재 제단과 비활성 제단으로 이동할 수 없다.
- 게임을 재실행해도 기술, 재건도, 유적, 제단, 엔딩 상태가 유지된다.
- 100%에서 3차 복원 후 엔딩이 재생된다.
- 모든 신규·수정 C# 파일이 순수 로직 300줄 이하이다.
- 신규 런타임 C# 6개, 테스트 포함 총 7개 예산을 지킨다.
- `CONVENTIONS.md`를 위반한 구현이 없다.
- 반복적인 GC 할당, 전체 탐색, 중복 저장, 중복 전환이 없다.

이 완료 정의를 통과하기 전에는 기능을 완료 처리하거나 `release`에 반영하지 않는다.
