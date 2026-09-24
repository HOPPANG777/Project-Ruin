# 코딩 컨벤션 (Coding Conventions)

본 프로젝트의 **코드 일관성, 런타임 성능, 유지보수 효율, 안정성**을 위해 정의된 가이드라인이다.

모든 팀원(개발·기획)은 코드를 작성하거나 리뷰할 때 이 문서를 기준으로 한다.

> 이 문서는 실제 프로젝트 코드(`GameRoot`, `Controller`, `SO`, `Service` 패턴)와
> Project-ZFC 주석·Git 규약을 바탕으로 정리했다.
> 새 코드를 작성할 때는 **이 문서**와 **이미 작성된 동일 계열 코드**를 함께 기준으로 삼는다.

---

## 0. 사용 기술 스택 (Tech Stack)

아래 라이브러리는 정해진 용도에만 사용한다. 같은 일을 하는 두 가지 방식을 혼용하지 않는다.

| 라이브러리 | 용도 | 사용 기준 (요약) |
| --- | --- | --- |
| **UniTask** | 비동기 처리 (`async`/`await`) | 씬·리소스 로딩, I/O, 네트워크 등 **반드시 비동기여야 하는 곳**에만 (→ 2.1) |
| **DOTween Pro** | 트위닝 / 연출 애니메이션 | UI·Transform·색상 등 **값 보간 연출**에 사용. 게임 로직 타이밍 제어 용도 아님 (→ 2.2) |
| **Easy Save 3** | 데이터 영속화 (저장/로드) | 세이브 데이터, 설정값 등 **디스크 저장이 필요한 모든 것** (→ 2.3) |
| **Odin Inspector** | 인스펙터 직렬화 / 에디터 UI | 직렬화 필드 그룹화, 버튼, 검증 등 (→ 4) |

> 위 4개 라이브러리는 프로젝트 종속성이다. 동일 기능의 다른 에셋(예: 다른 트윈 라이브러리, `PlayerPrefs` 기반 세이브 등)을 추가로 도입하지 않는다.

---

## 1. 핵심 설계 원칙 (Core Architecture)

### 1.1 의존성 및 결합도 (DI & Low Coupling)

- **의존성 주입(DI) 권장**: 클래스 내부에서 직접 `new`로 협력 객체를 생성하거나 `GameObject.Find` 계열을 사용하지 않는다.
- **낮은 결합도**: 각 클래스는 자신의 역할에만 집중하며, 협력 대상의 세부 구현이 아닌 **인터페이스에 의존**한다.
- **주입 방식**
  - `MonoBehaviour` 컨트롤러: `GameRoot.Start()`에서 시작한 Bootstrapper/Module이 호출하는 `Init(의존성...)` 메서드로 주입받는다.
  - 순수 C# 객체(POCO): 생성자 또는 `Construct(의존성...)` 메서드로 주입받는다.

```csharp
// MonoBehaviour 컨트롤러는 Init()으로 의존성을 받는다.
public void Init(IProgressService progressService)
{
    _progressService = progressService;
    RebuildQuestDatabase();
}
```

### 1.2 상속 구조 (Inheritance Limit)

- **1단계 상속 원칙**: 상속은 최대 1단계까지만 허용한다. (예: `UnitBase` → `UnitPlayer`)
- **구성(Composition) 중심**: 2단계 이상 깊은 상속 대신, 기능을 작은 클래스로 분리해 조립한다.

### 1.3 매니저 & 컨트롤러 구조 (Manager & Controller)

- **싱글턴은 오직 1개**: 전역 싱글턴은 `GameRoot` **하나만** 둔다. (`Singleton<T>` 상속은 `GameRoot` 전용)
- **컨트롤러는 `GameRoot` 소유 모듈의 멤버**: 전역 관리 클래스는 싱글턴으로 만들지 않고, `GameRoot`가 직렬화해 소유하는 기능별 Module 내부 멤버(`[SerializeField]`)인 **Controller**로 작성한다. 작은 시스템은 직접 멤버로 둘 수 있다.
- **접근 경로 단일화**: 컨트롤러 접근은 반드시 `GameRoot.Instance`를 경유한다.
  - 예: `GameRoot.Instance.Quest`, `GameRoot.Instance.Progress`
- **외부에는 인터페이스(계약)만 노출**: 기능별 Module은 구현체 필드를 직렬화하고, `GameRoot`의 외부 공개 프로퍼티는 해당 인터페이스 타입을 위임해 노출한다.

```csharp
[Header("Bootstrap Modules")]
[SerializeField] private GameplayBootstrapModule _gameplayModule;

// GameplayBootstrapModule 내부
[Tooltip("퀘스트 상태 전환, 목표 갱신, 보상 지급을 관리하는 컨트롤러입니다.")]
[SerializeField] private QuestController _questController;
public IQuestService Quest => _questController;

// GameRoot는 외부 호환 진입점을 Module 계약에 위임한다.
public IQuestService Quest => _gameplayModule.Quest;
```

---

## 2. 프로그래밍 가이드 및 최적화

### 2.1 시간 / 대기 / 비동기 처리 (Update·for 우선, UniTask는 "필수 비동기"에만)

- **Coroutine 금지**: `StartCoroutine`은 사용하지 않는다.
- **대기/타이밍은 Update로 대체**: "몇 초 뒤 실행", "N프레임 후 실행", "조건 만족 시 진행"은 아래 방식으로 작성한다.
  - **Update + 타이머**: `elapsed += Time.deltaTime` 누적 후 분기
  - **Update + 상태 머신**: `State`에 따라 단계별 로직 분리
  - **for/while 분해**: 프레임에 걸쳐야 할 이유가 없으면 한 프레임에서 끝낸다.
- **UniTask는 반드시 비동기여야 하는 곳에만** 사용한다.
  - 씬/리소스 로딩, 네트워크/웹 요청, 파일 I/O, 비동기 초기화
- **프레임 대기용 await 남용 금지**: `await UniTask.Yield()` / `DelayFrame()`로 Update를 대체하지 않는다.
- **Cancellation 필수**: 오브젝트 생명주기와 함께 취소되어야 하는 비동기는 `this.GetCancellationTokenOnDestroy()` 또는 명시적 `CancellationToken`을 사용한다.

```csharp
public async UniTask FadeInAsync(CancellationToken token)
{
    await FadeAsync(0.0f, 1.0f, token);
}
```

### 2.2 트위닝 / 연출 애니메이션 (DOTween Pro)

- **용도**: UI alpha, `Transform`, 색상 등 **값 보간 연출**에만 사용한다.
- **게임 로직 타이밍 제어 용도가 아님**: "몇 초 뒤 상태 전환" 같은 흐름은 DOTween `OnComplete`에 의존하지 말고 2.1로 처리한다.
- **생명주기 연결 필수**: `SetLink(gameObject)` 또는 핸들 보관 후 `OnDestroy`/재생 직전 `Kill()`
- **무한/반복 트윈은 핸들 보관 필수**: `SetLoops(-1)` 등은 반드시 `Kill()` 한다.
- **재생 전 중복 제거**: 같은 대상에 새 트윈을 시작하기 전 이전 트윈을 정리한다.
- **UniTask 연동**: 완료 대기가 필요하면 `await tween.ToUniTask(cancellationToken: token)`

### 2.3 데이터 저장 / 로드 (Easy Save 3)

- **모든 영속화는 Easy Save(ES3)로 통일**한다. `PlayerPrefs`나 수동 파일 쓰기를 새로 도입하지 않는다.
- **저장/로드는 컨트롤러 책임**이다. 게임플레이 클래스에 흩지 않는다.
- **키는 상수로 관리**한다. 매직 스트링을 흩어 쓰지 않는다.
- **세이브 슬롯은 파일 경로로 분리**한다.
- **I/O는 비동기 + 예외 처리**를 고려한다. (`ES3.KeyExists`, `try/catch`)

### 2.4 루프 최적화 (Loops)

- 리스트/배열 순회는 `for`를 우선한다.
- `Dictionary` 등 인덱서가 없는 컬렉션은 `foreach`를 사용한다.

### 2.5 방어적 프로그래밍 & 안정성 (Stability)

- **가드절(Early Return)**: 유효하지 않은 입력은 메서드 앞부분에서 즉시 `return`한다.
- **참조 null 체크**: `[SerializeField]` 참조는 사용 전 `null`을 확인한다.
- **문자열 ID 검증**: `string.IsNullOrWhiteSpace`로 검증한다.
- **Try 패턴**: 실패가 정상 흐름인 조회/상태 변경은 `bool TryXxx(..., out 결과)` 형태를 사용한다.
- **로그 접두사 통일**: `[클래스명]` 접두사를 붙인다.
- **종료 시점 싱글턴 접근 금지**: `OnDestroy`/`OnApplicationQuit`에서 다른 싱글턴/컨트롤러 접근을 지양한다.

```csharp
public bool TryAcceptQuest(QuestDataSO questData)
{
    if (questData == null)
    {
        Debug.LogWarning("[QuestController] 수락할 퀘스트 데이터가 null입니다.");
        return false;
    }

    if (GetQuestState(questData) != QuestState.QUEST_NOT_STARTED)
    {
        return false;
    }

    SetQuestState(questData, QuestState.QUEST_IN_PROGRESS);
    return true;
}
```

---

## 3. 명명 규칙 (Naming Conventions)

### 3.1 Manager / Controller / Service

| 종류 | 규칙 | 예시 |
| --- | --- | --- |
| Manager | 싱글턴에만 사용 (`GameRoot`만 해당) | `GameRoot` |
| Controller | `GameRoot` 멤버로 두는 시스템/도메인 관리 클래스 | `QuestController` |
| Service | 컨트롤러가 외부에 노출하는 계약 | `IQuestService` |
| Presenter / View | UI 표시 / 연출·흐름 제어 | `QuestView`, `QuestPresenter` |
| ScriptableObject | `SO` 접미사 | `QuestDataSO` |

### 3.2 표기법 (Case)

| 분류 | 표기법 | 예시 |
| --- | --- | --- |
| 클래스 / 구조체 | `PascalCase` | `QuestController` |
| 인터페이스 | `IPascalCase` | `IQuestService` |
| 메서드 | `PascalCase` | `TryAcceptQuest()` |
| 프로퍼티 | `PascalCase` | `QuestId` |
| 열거형 타입 | `PascalCase` | `QuestState` |
| 열거형 상수 | `SCREAMING_SNAKE` + 접두어 | `QUEST_IN_PROGRESS` |
| 상수 (`const` / `static readonly`) | `SCREAMING_SNAKE` | `SAVE_KEY_FLAGS` |
| 멤버 변수 (private) | `_camelCase` | `_moveSpeed` |
| 지역 / 매개 변수 | `camelCase` | `targetPos` |

### 3.3 열거형 (enum)

`enum` 은 아래 규칙을 **예외 없이** 따른다.

1. **열거자 이름은 `SCREAMING_SNAKE_CASE`** 로 작성한다.
2. **접두어(prefix)를 반드시 붙인다.** 역할/소속을 한눈에 구분되게 한다.
   - 퀘스트라면 `QUEST_`, 타일이라면 `TILE_` 처럼.
3. **첫 번째 열거자에는 무조건 `= 0` 을 명시한다.**
4. **타입 이름**은 역할이 드러나는 `PascalCase`로 짓는다.

```csharp
public enum QuestState
{
    QUEST_NOT_STARTED = 0, // 첫 번째 값에는 반드시 = 0
    QUEST_IN_PROGRESS,
    QUEST_COMPLETED,
    QUEST_REWARDED
}
```

**금지 예시**

```csharp
// 접두어 없음 + 첫 값에 = 0 없음 + 카멜/파스칼 케이스 → 금지
public enum QuestState
{
    NotStarted,
    InProgress,
    Completed
}
```

---

## 4. 주석 (Comments)

### 4.0 기본 원칙

| 상황 | 사용 | 예시 |
| --- | --- | --- |
| 한 문장 ~ 두 문장 | `//` | `// 레이어들을 초기화한다.` |
| 세 문장 이상 / 긴 설명 | `/* ... */` | 여러 줄에 걸친 설명 블록 |
| 공개 API (인터페이스·공개 메서드) | `/// <summary>` XML | IntelliSense용 문서화 |

- 주석은 "코드가 무엇을 하는지"가 아니라 **왜 그렇게 했는지(의도, 제약, 트레이드오프)** 를 남긴다.
- 코드만 봐도 자명한 내용(`// i를 1 증가` 같은)은 달지 않는다.
- 모든 **클래스 / 공개 메서드 / 필드**에는 역할을 설명하는 주석을 최소 한 줄 이상 작성한다.

---

### 4.1 구분선 종류와 위계

구분선은 **폭과 문자로 위계(레벨)를 나눈다.** 위로 갈수록 큰 단위다.

| 레벨 | 스타일 | 쓰임 |
| --- | --- | --- |
| 최상위 | `//====` (풀 폭) | 파일 헤더, 클래스/메서드 정의 헤더, Synopsis |
| 대구획 | `//****` (풀 폭) | 파일 내부의 큰 논리 구획 배너 |
| 섹션 | `//====` (짧은 폭) | using / 직렬화 필드 / 공개 API 등 영역 라벨 |
| 소구획 | `///////` | 클래스 내부의 멤버 그룹 나눔 |

각 레벨의 생김새 예시:

```csharp
//=============================================================================
// [최상위] 파일 헤더 / 클래스·메서드 정의 헤더 / Synopsis 에 사용
//=============================================================================

//******************************************************************************
//
// [대구획] 파일 내부 큰 논리 구획 배너
//
//******************************************************************************

//========================================
// [섹션] using / 직렬화 필드 / 공개 API 영역 라벨
//========================================

///////////////////////////////////////////////////////////////////////
// [소구획] 클래스 내부 멤버 그룹
///////////////////////////////////////////////////////////////////////
```

한 파일 안에서 위계가 어떻게 겹쳐 쓰이는지:

```csharp
//=============================================================================
// File:        QuestController.cs
//
// Description: 퀘스트 상태 전환, 목표, 보상 처리를 담당한다.
//
// History:     07/19/2026 + Created -- VicZero
//
//=============================================================================

//========================================
// System Includes
//========================================
using System.Collections.Generic;
using UnityEngine;

//******************************************************************************
//
// Public Member Functions
//
//******************************************************************************

//=============================================================================
// QuestController.TryAcceptQuest
//=============================================================================
public bool TryAcceptQuest(QuestDataSO questData)
{
    return true;
}
```

---

### 4.2 파일 헤더 블록 (파일 최상단)

**쓰임:** 파일의 신원(파일/컴포넌트 이름, 설명, 변경 이력)을 파일 맨 위에 한 번 기록한다.

- `File:` 은 개별 파일 단위일 때, `Component:` 는 하나의 시스템/모듈을 대표하는 파일일 때 사용한다.
- `History:` 에는 `날짜 + 변경 내용 -- 작성자` 형식으로 이력을 누적한다.
- 저작권 표기는 사용하지 않는다(필요해지면 그때 첫 줄에 추가한다).

```csharp
//=============================================================================
// File:        QuestController.cs
//
// Description: CSV 퀘스트 정의를 사용해 상태 전환, 목표, 소모 및 보상을 처리한다.
//
// History:     07/15/2026 + Created -- VicZero
//              07/19/2026 + 보상 중복 수령 방지 추가 -- VicZero
//
//=============================================================================
```

컴포넌트 단위일 때:

```csharp
//=============================================================================
// Component:   GameRoot
//
// Description: GameRoot 는 외부 시스템이 각 Controller 와 상호작용하기 위해
//              필요한 모든 인터페이스를 제공한다.               [07/15/2026]
//
// History:     + 초기 인터페이스 구현        -- VicZero [07/15/2026]
//
//=============================================================================
```

기존 스타일(`기능:` / `설명:`)로 이미 작성된 파일은 수정 시점에 점진적으로 맞춘다.
새 파일과 손대는 파일부터 위 형식을 적용한다.

---

### 4.3 섹션 라벨 (짧은 `//====`)

**쓰임:** 파일 내부의 영역(using 그룹, 직렬화 필드, 공개 API 등)을 나눠 시각적으로 구분한다.

대표 라벨: `System Includes`, `Project Includes`, `상수`, `직렬화 필드`, `내부 상태`, `프로퍼티`, `초기화`, `공개 API`, `내부 로직`

```csharp
//========================================
// System Includes
//========================================
using System.Threading;
using UnityEngine;

//========================================
// Project Includes
//========================================
using Cysharp.Threading.Tasks;
```

- 섹션 안에서 다시 성격이 갈리면 `// Foundation Tech` 처럼 `//` 한 줄짜리 소분류 주석으로 묶는다.

---

### 4.4 TODO 블록

**쓰임:** 나중에 손봐야 할 구조적 개선점을 눈에 띄게 박아둔다. 짧으면 `// TODO:` 한 줄, 설명이 길면 섹션 블록으로.

```csharp
//========================================
// TODOs: 아래 데이터셋을 다른 파일로
//        재캡슐화할 것:
//========================================
```

```csharp
// TODO: 예외 처리 추가 필요
// FIXME: 메모리 누수 발생 가능성 있음
```

---

### 4.5 Synopsis 블록 (클래스/타입 설명)

**쓰임:** 클래스 정의 바로 위에 그 타입이 "무엇이고 어떤 책임을 지는지" 요약한다.
파일 헤더의 Description 이 파일 수준이라면, Synopsis 는 타입 수준의 설명이다.

```csharp
//=============================================================================
//
// Synopsis:   QuestController;
//                - 퀘스트 상태 전환의 진입점
//                - 지원 기능
//                   - 수락 / 완료 / 보상 수령        -- VicZero [07/15/2026]
//                   - Progress Flag 동기화
//=============================================================================
public sealed class QuestController : MonoBehaviour, IQuestService
{
    // ...
}
```

짧은 파일에서는 기존처럼 한 블록으로 요약해도 된다.

```csharp
//=============================================================================
// File:        QuestController.cs
//
// Description: CSV 퀘스트 정의를 사용해 상태 전환, 목표, 소모 및 보상을 처리한다.
//
//=============================================================================
```

---

### 4.6 함수 정의 헤더 블록

**쓰임:** 각 함수 구현 위에 붙여, 함수의 설명/매개변수/반환값을 정형화된 형태로 문서화한다.
함수 경계를 시각적으로도 확실히 나눈다.

- 항목: `Description`, `Parameters`, `Return`
- 없으면 `None.` / `N/A.` / `void` 로 명시한다. (빈칸으로 두지 않는다)
- **공개 API**는 위 블록에 더해 `/// <summary>` XML 주석도 작성한다. (IntelliSense)

```csharp
//=============================================================================
// QuestController.TryAcceptQuest
//=============================================================================
// Description: 퀘스트를 수락 가능한지 검사하고 진행 중 상태로 전환한다.
//
// Parameters:  ( QuestDataSO questData )
//
// Return:      bool — 수락에 성공하면 true
//
//=============================================================================
/// <summary>퀘스트를 수락 가능한지 검사하고 진행 중 상태로 전환한다.</summary>
/// <param name="questData">수락할 퀘스트 데이터입니다.</param>
/// <returns>수락에 성공하면 true입니다.</returns>
public bool TryAcceptQuest(QuestDataSO questData)
{
    // ...
}
```

생성자/소멸자처럼 매개변수·반환이 없을 때:

```csharp
//=============================================================================
// ExampleController.ExampleController
//=============================================================================
// Description: Constructor.
//
// Parameters:  None.
//
// Return:      N/A.
//
//=============================================================================
public ExampleController()
{
}
```

짧은 private 헬퍼는 정형 헤더 대신 한 줄 `//` 또는 짧은 노트 박스로 대체할 수 있다.
다만 **공개 API / 핵심 상태 전환 메서드**에는 정형 헤더를 붙인다.

---

### 4.7 대구획 배너 (`//****`)

**쓰임:** 파일 안을 큰 논리 단위로 나누는 배너. 개별 함수 헤더보다 상위 개념으로, 여러 함수를 묶는 "구역"을 표시한다.

대표 배너: `Public Member Functions`, `Private Member Functions`, `Unity Lifecycle`, `Save / Load`

```csharp
//******************************************************************************
//
// Public Member Functions
//
//******************************************************************************

// ... 여러 public 함수 구현들 ...

//******************************************************************************
//
// Private Member Functions
//
//******************************************************************************
```

---

### 4.8 클래스 내부 소구획 divider (`///////`)

**쓰임:** 클래스 선언 안에서 멤버들을 성격별 그룹으로 나눈다.

대표: `Accessors`, `Serialized Fields`, `IQuestService`, `Internal State`

```csharp
    ///////////////////////////////////////////////////////////////////////
    // Accessors
    ///////////////////////////////////////////////////////////////////////
    public int Score => _score;

    ///////////////////////////////////////////////////////////////////////
    // IQuestService
    ///////////////////////////////////////////////////////////////////////
    public bool TryAcceptQuest(QuestDataSO questData);
```

- 그룹 안에서 더 잘게 나눌 땐 `// Context Interface`, `// Static Methods` 처럼 `//` 한 줄 라벨을 쓴다.
- 기존에 쓰던 짧은 `//====` 섹션 라벨(`상수`, `직렬화 필드` 등)도 동일 목적의 섹션 구분으로 허용한다.

---

### 4.9 코드 바로 위 짧은 설명 주석

**쓰임:** 바로 아래 한두 줄 코드의 의도를 설명한다. 특히 "왜 이렇게 막아뒀는지" 같은 방어적 코드의 이유를 남긴다.

```csharp
// 0 이하 가산은 의미가 없으므로 무시한다.
if (amount <= 0)
{
    return false;
}
```

```csharp
// 참조 누락 시 크래시 대신 안전하게 종료한다.
if (_popupCanvasGroup == null)
{
    Debug.LogWarning("[ExampleController] 팝업 CanvasGroup이 연결되지 않았습니다.");
    return;
}
```

---

### 4.10 트레일링(같은 줄) 주석

**쓰임:** 선언 오른쪽에 붙여, 그 변수의 실제 의미/실제 타입을 간단히 보충한다.

```csharp
int _layer;   // LayerEnum
int _level;   // LevelEnum
int _mission; // MissionEnum
```

- 멤버 변수의 역할 설명은 선언 위 `//` 한 줄로 두는 것도 허용한다.

```csharp
// 현재 점수입니다.
private int _score;

// 진행 중인 팝업 페이드 트윈 핸들입니다.
private Tween _popupTween;
```

---

### 4.11 HACK / 버그 추적 메모

**쓰임:** 임시방편(hack)이거나 정상적이지 않은 코드에 대해, 그 사실과 **추적 경로(이슈/버그 번호 등)** 를 남긴다.

```csharp
// 캐시 보관용 임시 이름. 정상 구조 아님.
// Issue #1259 로 추적 중.
//
const string MY_CACHE = "MyResourceCache";
```

---

### 4.12 짧은 노트 박스

**쓰임:** 함수/유틸리티 위에 가볍게 붙이는 설명. 정형 헤더까지는 필요 없지만 한 줄 의도는 남기고 싶을 때 위아래 `//` 로 감싼다.

```csharp
//
// 이 싱글턴에 접근하기 위한 간단한 헬퍼.
//
public static GameRoot GetGameRoot()
{
    return GameRoot.Instance;
}
```

---

### 4.13 XML 문서화 주석 (공개 API)

**쓰임:** 인터페이스와 공개 메서드/프로퍼티에 사용한다. 구현체와 호출부 양쪽에서 IntelliSense로 보인다.

```csharp
/// <summary>
/// 퀘스트 ID로 퀘스트 데이터를 조회한다.
/// </summary>
/// <param name="questId">조회할 퀘스트 ID입니다.</param>
/// <param name="questData">조회된 퀘스트 데이터입니다.</param>
/// <returns>퀘스트 데이터를 찾으면 true입니다.</returns>
bool TryGetQuestData(string questId, out QuestDataSO questData);
```

---

## 5. 인스펙터 및 필드 규칙

### 5.1 멤버 변수 및 프로퍼티

- **Private 필드**: `_` 접두사를 사용하며, 외부 노출이 필요하면 프로퍼티로 감싼다.
- **읽기 전용 노출**: `public int Hp => _hp;`
- **외부 쓰기 제어**: `get; private set;` 또는 명시적 set 로직으로 캡슐화한다.

### 5.2 인스펙터 속성 (Attributes)

- **`[SerializeField]`**: Private 필드에 사용해 인스펙터에 노출한다. `public` 필드 직접 노출은 지양한다.
- **`[Tooltip]`**: 모든 직렬화 필드에 한 문장으로 역할을 명시한다.
- **그룹화**
  - Odin 사용 시: `[TabGroup]`, `[BoxGroup]`, `[FoldoutGroup]`
  - 순수 Unity: `[Header("Group")]`
- **검증/편의**: `[Required]`, `[Button]`을 활용해 설정 실수를 조기에 잡는다.

---

## 6. 표준 코드 예시 (Best Practice)

```csharp
//=============================================================================
// File:        ExampleController.cs
//
// Description: 표준 컨벤션을 종합 적용한 컨트롤러 예시
//
// History:     07/19/2026 + Created -- VicZero
//
//=============================================================================

//========================================
// System Includes
//========================================
using System.Threading;
using UnityEngine;

//========================================
// Project Includes
//========================================
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;

//=============================================================================
//
// Synopsis:   IExampleService;
//                - Example 시스템의 외부 공개 계약
//=============================================================================
public interface IExampleService
{
    /// <summary>점수를 추가한다.</summary>
    /// <param name="amount">추가할 점수입니다.</param>
    /// <returns>추가에 성공하면 true입니다.</returns>
    bool TryAddScore(int amount);
}

//=============================================================================
//
// Synopsis:   ExampleController;
//                - 점수 추가 / 팝업 연출 / Easy Save 저장
//=============================================================================
[DisallowMultipleComponent]
public class ExampleController : MonoBehaviour, IExampleService
{
    //========================================
    // 상수
    //========================================

    // Easy Save 저장 키. 매직 스트링 방지를 위해 상수로 관리한다.
    private const string SAVE_KEY_SCORE = "example.score";

    //========================================
    // 직렬화 필드
    //========================================

    [TabGroup("Refs")]
    [Tooltip("점수 변동 시 연출할 팝업 CanvasGroup입니다.")]
    [Required]
    [SerializeField]
    private CanvasGroup _popupCanvasGroup;

    [TabGroup("Settings")]
    [Tooltip("팝업 페이드 연출 시간(초)입니다.")]
    [SerializeField]
    private float _popupFadeDuration = 0.3f;

    //========================================
    // 내부 상태
    //========================================

    // 현재 점수입니다.
    private int _score;

    // 진행 중인 팝업 페이드 트윈 핸들입니다.
    private Tween _popupTween;

    // 점수 변동을 기록할 진행 상태 서비스입니다.
    private IProgressService _progressService;

    //========================================
    // 프로퍼티
    //========================================

    /// <summary>현재 점수입니다.</summary>
    public int Score => _score;

    //******************************************************************************
    //
    // Public Member Functions
    //
    //******************************************************************************

    //=============================================================================
    // ExampleController.Init
    //=============================================================================
    // Description: GameRoot에서 호출하는 초기화 함수.
    //
    // Parameters:  ( IProgressService progressService )
    //
    // Return:      void
    //
    //=============================================================================
    /// <summary>GameRoot에서 호출하는 초기화 함수입니다.</summary>
    public void Init(IProgressService progressService)
    {
        _progressService = progressService;
        Load();
    }

    //=============================================================================
    // ExampleController.TryAddScore
    //=============================================================================
    // Description: 점수를 추가하고 팝업 연출을 재생한다.
    //
    // Parameters:  ( int amount )
    //
    // Return:      bool — 추가에 성공하면 true
    //
    //=============================================================================
    /// <summary>점수를 추가하고 팝업 연출을 재생한다.</summary>
    public bool TryAddScore(int amount)
    {
        // 0 이하 가산은 의미가 없으므로 무시한다.
        if (amount <= 0)
        {
            return false;
        }

        _score += amount;
        PlayPopup();
        return true;
    }

    //=============================================================================
    // ExampleController.Save
    //=============================================================================
    // Description: 현재 점수를 디스크에 저장한다.
    //
    // Parameters:  None.
    //
    // Return:      void
    //
    //=============================================================================
    public void Save()
    {
        ES3.Save(SAVE_KEY_SCORE, _score);
    }

    //=============================================================================
    // ExampleController.Load
    //=============================================================================
    // Description: 저장된 점수가 있으면 불러온다.
    //
    // Parameters:  None.
    //
    // Return:      void
    //
    //=============================================================================
    public void Load()
    {
        if (ES3.KeyExists(SAVE_KEY_SCORE) == false)
        {
            return;
        }

        _score = ES3.Load(SAVE_KEY_SCORE, _score);
    }

    //******************************************************************************
    //
    // Private Member Functions
    //
    //******************************************************************************

    //=============================================================================
    // ExampleController.PlayPopup
    //=============================================================================
    // Description: 점수 팝업을 페이드 인 → 아웃으로 연출한다.
    //
    // Parameters:  None.
    //
    // Return:      void
    //
    //=============================================================================
    private void PlayPopup()
    {
        // 참조 누락 시 크래시 대신 안전하게 종료한다.
        if (_popupCanvasGroup == null)
        {
            Debug.LogWarning("[ExampleController] 팝업 CanvasGroup이 연결되지 않았습니다.");
            return;
        }

        // 이전 트윈이 남아 있으면 정리해 연출 충돌을 막는다.
        _popupTween?.Kill();

        _popupCanvasGroup.alpha = 0.0f;
        _popupTween = _popupCanvasGroup
            .DOFade(1.0f, _popupFadeDuration)
            .SetLoops(2, LoopType.Yoyo) // 인 → 아웃
            .SetLink(gameObject);       // 오브젝트 파괴 시 자동 Kill
    }
}
```

---

## 7. Git 규약 (Git Conventions)

> 이 규약은 현재 개발 방식(개인 작업 브랜치에서 작업 · 완료 후 `release` 반영 · 커밋 즉시 push · 한국어 완료형 메시지)을
> **유지하되 일관성만 강화**하는 방향으로 정한다.
>
> 기획자도 개발자와 **같은 브랜치 구조**를 사용한다.
> 버전 번호 브랜치(`0.0.1` 등)는 쓰지 않는다. (번거롭고 오해하기 쉽다)

### 7.1 브랜치 (Branch)

브랜치는 **안정 브랜치**와 **개인 작업 브랜치** 두 종류만 사용한다.

| 종류 | 이름 규칙 | 역할 |
| --- | --- | --- |
| 안정 브랜치 | `release` | 언제든 빌드/실행 가능한 안정 상태를 보관한다. **직접 개발하지 않는다.** |
| 개인 작업 브랜치 | 이름 **이니셜** (소문자, 예: `vz`, `jew`) | 해당 사람의 실제 작업 브랜치. `release`에서 파생한다. |

#### 흐름

```
release          ← 최종 완료분만 올라가는 안정 브랜치
   ├─ vz         ← VicZero 작업 브랜치
   ├─ jew        ← JEW 작업 브랜치
   └─ (다른 이니셜...)
```

1. 작업을 시작할 때 **`release`에서 본인 이니셜 브랜치를 생성**하거나, 이미 있으면 `release`를 받아와 맞춘 뒤 그 브랜치에서 작업한다.
2. 일상 작업·커밋·푸시는 **본인 이니셜 브랜치**에서만 한다.
3. 작업이 **최종 완료**되면 그 결과를 `release`로 올린다. (PR 또는 합의된 merge)
4. `release`에는 검토가 끝난 변경만 반영한다. `release`에 직접 커밋/푸시하지 않는다.

#### 규칙

- 브랜치 이름에 `feature/`, `fix/` 같은 접두어를 붙이지 않는다.
- **버전 번호 브랜치(`0.0.1`, `0.0.1-VicZero` 등)를 새로 만들지 않는다.**
  - 버전은 브랜치가 아니라 **Git Tag / Release / 이슈·프로젝트 목표 버전**으로 관리한다.
- 한 사람 = 한 이니셜 브랜치를 기본으로 한다. (기획자 포함)
- 이니셜은 팀에 공유된 표기를 쓴다. (예: VicZero → `vz`)
- 기존에 쓰이던 이름/버전 혼합 브랜치(`VicZero`, `0.0.1` 등)는 새 작업부터 이니셜 브랜치로 옮긴다.

---

### 7.2 커밋 메시지 (Commit Message)

#### 7.2.1 기본 형식

```
[태그] 완료형 요약
```

- **한국어 완료형 서술**을 사용한다. (예: `완성`, `작성완료`, `변경`, `추가`)
- 요약 첫머리에 **한국어 태그**를 대괄호로 붙인다.
- 요약은 **한 줄 50자 내외**로, 무엇을 했는지 한눈에 보이게 쓴다.
- 마침표(`.`)로 끝내지 않는다.

#### 7.2.2 태그 종류

| 태그 | 쓰임 | 예시 |
| --- | --- | --- |
| `[기능]` | 새 기능 · 새 타입/클래스 추가 | `[기능] QuestController 작성완료` |
| `[수정]` | 버그 수정 · 동작 교정 | `[수정] 퀘스트 보상 중복 지급 방지` |
| `[구조]` | 폴더 구조 변경 · 리팩터링(동작 불변) | `[구조] 인벤토리 폴더 구조 정리` |
| `[문서]` | 주석 · 문서 · 컨벤션 | `[문서] CONVENTIONS 주석·Git 규약 정리` |
| `[설정]` | 인코딩 · 빌드 · 환경 · 도구 설정 | `[설정] 파일 인코딩 UTF-8 통일` |
| `[콘텐츠]` | 대사·퀘스트·아이템 등 데이터/콘텐츠 | `[콘텐츠] 큐레이터 첫 퀘스트 대사 작성` |
| `[밸런스]` | 수치·밸런스 조정 | `[밸런스] 적 HP 수치 조정` |
| `[작업]` | 작업일지 등 개발 산출물 외 기록 | `[작업] 작업일지 업로드` |

- 한 커밋이 여러 성격을 가지면 **가장 핵심이 되는 태그 하나**만 붙인다. → 안 되면 커밋을 나눈다.
- `중간저장`, `최종`, `진짜 최종` 같은 무의미한 요약은 쓰지 않는다. WIP가 필요하면 `[작업] ...` 로 내용을 남긴다.

#### 7.2.3 커밋 단위 (Granularity)

- **하나의 논리적 변경 = 하나의 커밋**을 원칙으로 한다.
- 파일 이름만 적는 커밋(`Update QuestController.cs` 같은)은 지양하고, **무엇을 왜 바꿨는지**를 적는다.
  - 나쁨: `Update QuestController.cs`
  - 좋음: `[수정] 퀘스트 보상 중복 지급 방지`
- 서로 다른 성격의 변경(기능 + 구조 변경 등)은 **커밋을 분리**한다.

**금지 예시**

```
중간저장                  // 내용 없음 → 금지
Update QuestController.cs // 파일명만, 의도 없음 → 금지
버그 여러개 수정함        // 태그 없음 + 단위 뭉침 → 금지
최종                      // 의미 없음 → 금지
```

**권장 예시**

```
[기능] QuestController 작성완료
[콘텐츠] 큐레이터 첫 퀘스트 대사 작성
[문서] CONVENTIONS 주석·Git 규약 정리
[구조] 폴더 구조 변경
[설정] 파일 인코딩 변환
```

#### 7.2.4 커밋 본문 / Description

**요약(제목) 한 줄로 부족할 때**, 빈 줄 하나를 띄우고 본문을 적는다.
제목이 "무엇을 했는지"라면, 본문은 **"왜 했는지 · 어떻게 했는지 · 무엇을 주의해야 하는지"** 를 남긴다.

**전체 형식**

```
[태그] 완료형 요약          <- 제목 (필수, 50자 내외)
                            <- 빈 줄 (필수)
- 왜: 이 변경이 필요한 이유 / 배경
- 무엇: 실제로 바꾼 핵심 내용
- 주의: 부작용 · 남은 일 · 참고사항 (있을 때만)
```

**작성 규칙**

- 본문은 **`- ` 불릿**으로 항목을 나눠 쓴다.
- 항목 순서는 **왜 → 무엇 → 주의** 를 권장하되, 필요 없는 항목은 생략한다.
- 제목만으로 충분히 이해되는 작은 변경(오타, 인코딩 등)은 본문을 생략해도 된다.
- 관련 이슈/버그 번호가 있으면 마지막에 `참고: #12` 처럼 남긴다.

**작성 예시 — 기능**

```
[기능] QuestController 작성완료

- 왜: 퀘스트 수락/완료/보상 흐름을 한곳에서 관리해야 함
- 무엇: QuestController + IQuestService 추가, GameRoot 연결
- 주의: 상태는 Progress Flag와 동기화됨
```

**작성 예시 — 수정**

```
[수정] 퀘스트 보상 중복 지급 방지

- 왜: 완료 상태에서 보상을 여러 번 받을 수 있던 버그
- 무엇: QUEST_REWARDED 전환 전 상태 검사 강화
- 주의: 세이브 데이터 영향 없음. 참고: #12
```

**작성 예시 — 구조 (본문 생략 가능한 경우)**

```
[구조] 폴더 구조 변경
```

**금지 예시**

```
[수정] 수정함

여러가지 고침         <- 제목과 중복 + 정보 없음 → 금지
```

```
[기능] QuestController 작성완료
- 왜: ...            <- 제목과 본문 사이 빈 줄 없음 → 금지
```

---

### 7.3 푸시 (Push)

- **커밋 직후 즉시 push** 흐름을 유지한다. (로컬-원격 상태를 항상 일치시킴)
- 단, **커밋 메시지 규약(7.2)을 지킨 커밋만** push 한다. (원격에 `중간저장` 류가 남지 않게)
- 이미 push 한 커밋에 대해서는 `--force` 로 히스토리를 갈아엎지 않는다. (되돌릴 땐 새 커밋으로 수정)
- push 대상은 **자신이 작업 중인 이니셜 브랜치**다. (`release` 직접 push는 안정 반영 시점에만, 합의된 방식으로)

---

### 7.4 `release` 반영

1. 본인 이니셜 브랜치에서 작업을 완료하고 직접 확인한다.
2. 최신 `release`를 받아와 충돌을 해결한다.
3. `release`를 대상으로 Pull Request를 만들거나, 팀에 합의된 방식으로 merge한다.
4. 반영이 끝나면 다른 팀원은 작업 시작 전에 `release`를 다시 pull한다.

---

### 7.5 요약 (Cheat Sheet)

```
브랜치 : release(안정) ← vz, jew …(개인 이니셜 작업 브랜치)
커밋   : [태그] 완료형 요약        예) [기능] QuestController 작성완료
태그   : 기능 · 수정 · 구조 · 문서 · 설정 · 콘텐츠 · 밸런스 · 작업
본문   : 제목 + 빈 줄 + "- 왜 / - 무엇 / - 주의" 불릿 (필요 시)
단위   : 논리적 변경 1개 = 커밋 1개
푸시   : 규약 지킨 커밋을 즉시 push, force-push 금지
반영   : 최종 완료분만 release 로 올린다 (버전 브랜치 사용 안 함)
```

---

> 본 문서는 팀 협의를 통해 업데이트될 수 있으며, 수정 시 팀원 전체 공유가 필요하다.
