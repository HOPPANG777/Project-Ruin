# 2026-07-23 작업일지 — 유적 재건 및 기술 되돌려놓기 기반 구현

## 1. 작업 정보

| 항목 | 내용 |
| --- | --- |
| 작업 기간 | 2026-07-22 ~ 2026-07-23 |
| 작성일 | 2026-07-23 |
| 작업 주제 | 기술 획득·반납, 유적 재건도 계산, ES3 저장, 최소 제단 UI 기반 구현 |
| 기준 문서 | `Assets/_Game/Docs/RUIN_RESTORATION_IMPLEMENTATION_PLAN.md` |
| 저장 안내 문서 | `Assets/_Game/Docs/ES3_PROGRESS_SAVE_GUIDE.md` |
| 핵심 코드 | `RelicRestorationController`, `ProgressController`, `AltarPresenter` |
| 작업 상태 | 기반 코드 구현과 컴파일 검증 완료, 기획·Unity 에셋 연결 대기 |
| Commit | 현재 변경 사항은 아직 커밋하지 않음 |

## 2. 작업 목표

이번 작업의 목표는 기술을 획득하고 유적에 되돌려놓는 전체 기능 중, 기획과 Unity 콘텐츠가 없어도 먼저 검증할 수 있는 기반을 만드는 것이었다.

핵심 흐름:

```text
퀘스트·보스·이벤트
-> 기술 획득 요청
-> 획득 상태 저장
-> 제단에서 반납 가능한 기술 조회
-> 기술 선택 및 확인
-> 반납 상태 변경
-> 재건도와 복원 단계 계산
-> ES3 저장
```

기획에서 아직 확정되지 않은 유적 외형, 제단의 세계관 정의, 복원 Timeline, UI 디자인과 엔딩 연출은 임의로 만들지 않는 것을 작업 경계로 정했다.

## 3. 절대 준수 기준

이번 구현에서는 `CONVENTIONS.md`와 구현 계획의 다음 규칙을 적용했다.

- 프로젝트 전역 싱글턴은 기존 `GameRoot` 하나만 사용
- 도메인 Controller는 `GameRoot`에서 초기화
- 외부에는 구현체가 아닌 Service 인터페이스 노출
- `MonoBehaviour` 의존성은 `Init(...)`으로 주입
- Coroutine 미사용
- 실제 파일 I/O에만 UniTask 사용
- 모든 영속화는 Easy Save 3 사용
- 리스트와 배열은 `for`로 순회
- 반복 호출 경로에서 LINQ 미사용
- `GameObject.Find` 계열 미사용
- UI 버튼을 열 때마다 생성하거나 파괴하지 않음
- 신규 및 수정 C# 파일의 순수 로직 300줄 제한
- 신규 런타임 C# 최대 6개, 자동 테스트 C# 최대 1개

## 4. 구현 결과 요약

| 영역 | 구현 결과 |
| --- | --- |
| 기술 데이터 | ID, 표시 Key, 아이콘, 챕터, 재건도 증가량 추가 |
| 재건 Catalog | 기술·복원 단계·Timeline·제단 목적지 정의와 검증 추가 |
| 도메인 계약 | 기술 획득·반납·조회와 실패 이유 계약 추가 |
| 도메인 로직 | 획득, 반납, 중복 방지, 재건도와 복원 단계 계산 구현 |
| 기술 획득 연결 | 퀘스트 기술 보상을 재건 서비스의 단일 획득 진입점으로 연결 |
| 저장 | Progress 전체 상태를 ES3 단일 데이터로 비동기 Save/Load |
| 체크포인트 | 보상 확정과 씬 전환 직전에 Dirty 진행 상태 저장 |
| 제단 진입 | 기존 `IInteractable` 기반의 얇은 제단 진입점 구현 |
| 제단 UI | 고정 버튼 풀, 확인·취소, 중복 입력 차단, 조작 잠금 구현 |
| 테스트 | 도메인·저장·줄 수 검증을 기존 단일 테스트 파일에 통합 |

## 5. 최종 코드 구조

```text
Assets/_Game/Scripts/
├── Gameplay/Relics/
│   ├── IRelicRestorationService.cs
│   ├── RelicRestorationCatalogSO.cs
│   ├── RelicRestorationController.cs
│   └── AltarInteractable.cs
├── Presentation/UI/Relics/
│   └── AltarPresenter.cs
└── Tests/Editor/
    └── RelicRestorationControllerTests.cs
```

현재 신규 런타임 스크립트 사용량은 `5/6`이다.

남은 한 파일은 복원 Root와 Timeline을 담당할 계획인 `RelicRestorationPresenter.cs`다. 실제 유적 외형과 연출 기획이 확정되기 전에는 만들지 않는다.

자동 테스트 파일은 계획된 최대 수인 `1/1`을 사용했다.

## 6. 데이터 설계

### 6.1 SkillDataSO 확장

`SkillDataSO`에 다음 유적 재건용 고정 데이터를 추가했다.

| 필드 | 역할 |
| --- | --- |
| `SkillId` | 저장과 조회에 사용하는 중복되지 않는 기술 ID |
| `DisplayNameKey` | 기술 이름 Localization Key |
| `DescriptionKey` | 기술 설명 Localization Key |
| `Icon` | 제단 기술 버튼에 표시할 Sprite |
| `Chapter` | 기술을 획득하는 챕터 번호 |
| `RestoreValue` | 기술 반납 시 증가하는 재건도 |

`SkillDataSO`는 고정 데이터만 보관한다. 플레이 중 변경되는 획득·반납 상태는 저장하지 않는다.

### 6.2 RelicRestorationCatalogSO

재건 시스템의 고정 설정을 한 에셋에서 관리하기 위해 Catalog를 추가했다.

Catalog의 책임:

- 되돌려놓을 수 있는 기술 목록
- `30`, `60`, `100` 재건도 임계값
- 단계별 Timeline 참조
- 제단 ID, 이름 Key, 스테이지, SpawnPoint, 메인 제단 여부
- 빈 ID와 중복 ID 검증
- 음수 재건도 검증
- 기술 재건도 합계 100 검증
- 복원 단계 순서와 임계값 검증

고정 데이터와 플레이 진행 상태를 분리해, 기획 변경은 Catalog에서 처리하고 런타임 상태는 Progress Flag로 관리하도록 했다.

## 7. 기술 획득·반납 도메인 로직

### 7.1 IRelicRestorationService

외부 시스템에는 다음 계약만 노출한다.

- 현재 재건도와 최대 재건도
- 현재 복원 단계
- 등록된 기술 여부
- 반납 가능한 기술 존재 여부
- 재사용 목록에 반납 가능한 기술 채우기
- 기술 획득 여부
- 기술 반납 여부
- 기술 획득 시도
- 기술 반납 시도

상태 변경이 실패할 수 있으므로 `TryAcquireSkill`, `TryReturnSkill` 패턴을 사용했다.

실패 이유:

```text
초기화 안 됨
잘못된 기술 ID
Catalog에 없는 기술
획득하지 않은 기술
이미 획득한 기술
이미 반납한 기술
```

### 7.2 Progress Flag

기술 진행 상태는 다음 Flag 규칙으로 저장한다.

```text
skill.acquired.<skillId>
skill.returned.<skillId>
```

예시:

```text
skill.acquired.SKILL_TELEKINESIS
skill.returned.SKILL_TELEKINESIS
```

### 7.3 재건도 계산

재건도 자체를 별도 저장하지 않는다.

```text
로드된 반납 Flag
-> 반납된 기술의 RestoreValue 합산
-> 현재 재건도 계산
-> Catalog 임계값으로 복원 단계 계산
```

이 방식은 재건도 숫자와 반납 상태가 서로 어긋나는 중복 저장을 방지한다.

### 7.4 기술 반납 의미

현재 확정한 도메인 규칙:

- 기술 반납은 기술 삭제가 아니라 진행 상태 변경
- 반납해도 플레이어의 실제 능력은 제거하지 않음
- 같은 기술은 한 번만 반납 가능
- 획득하지 않은 기술은 반납 불가
- 챕터 0 기술은 반납되지만 재건도 증가 없음
- 재건도는 최대 100을 넘지 않음

기술을 반납해도 능력이 유지되는 세계관 설명과 화면 표현은 기획 확정이 필요하다.

## 8. 기술 획득 콘텐츠 연결

퀘스트 보상의 Progress Flag가 기술 획득 Flag 형식이면 `ProgressController.SetFlag`를 직접 호출하지 않고 재건 서비스에 전달하도록 변경했다.

```text
QuestRewardDefinition
-> skill.acquired. 접두사 검사
-> 기술 ID 분리
-> Catalog 등록 여부 선검증
-> TryAcquireSkill(skillId)
-> 보상 확정 후 SaveAsync
```

이를 통해 퀘스트, 보스, 이벤트가 서로 다른 방식으로 기술 Flag를 직접 변경하는 문제를 막고 `TryAcquireSkill`을 단일 진입점으로 사용한다.

`QuestController`는 새 로직을 추가하면서 300줄 제한을 지키기 위해 다음 중복도 함께 정리했다.

- 같은 아이템 소모량 계산을 재사용 Dictionary 버퍼로 통합
- 보상 선검증과 실제 적용 흐름 단순화
- 반복 조건과 불필요한 래퍼 제거

## 9. ES3 저장·불러오기

### 9.1 IProgressService 확장

다음 최소 계약을 추가했다.

- `IsDirty`
- `LoadAsync`
- `SaveAsync`
- 기존 Flag와 오브젝트 상태 API
- `Reset`

### 9.2 ProgressController 저장 구조

저장 설정:

```text
기본 파일: ProjectRuinSave.es3
Save Key: progress.data
```

`progress.data`에는 다음 상태를 하나로 묶어 저장한다.

- Progress Flag
- 스테이지별 오브젝트 상태

ES3 I/O는 `UniTask.RunOnThreadPool`에서 실행하고 예외를 처리한다.

### 9.3 Dirty 처리

```text
SetFlag / SetObjectState
-> 실제 값이 변경되었을 때만 Dirty
-> SaveAsync 체크포인트
-> Dirty가 아니면 파일 쓰기 생략
```

저장 중 새로운 변경이 발생했을 때 이전 저장 완료가 새 Dirty 상태를 지우지 않도록 내부 버전을 비교한다.

### 9.4 저장 체크포인트

현재 연결된 저장 시점:

- 게임 초기화 시 저장 데이터 로드
- 퀘스트 보상 확정 후
- 기술 반납 확정 후
- 스테이지 전환 직전

조회 API는 파일 I/O를 발생시키지 않는다.

## 10. GameRoot 초기화 순서

`GameRoot.Start()`를 비동기 초기화 흐름으로 변경했다.

```text
CSV 게임 데이터 초기화
-> Progress LoadAsync
-> RelicRestorationController 초기화 및 재건도 재계산
-> AltarPresenter 초기화
-> Inventory 초기화
-> Quest 초기화
-> Dialogue 초기화
-> StageFlow 초기화
-> SceneStateBridge 연결
-> Camera 초기화
```

저장 데이터를 불러오기 전에 재건도나 퀘스트 상태를 계산하지 않도록 순서를 고정했다.

외부에는 다음 인터페이스를 추가로 노출한다.

```text
GameRoot.Instance.RelicRestoration
```

## 11. 최소 제단 UI 코드

### 11.1 AltarInteractable

기존 `IInteractable`을 구현하는 얇은 진입점으로 작성했다.

보관 정보:

- Altar ID
- Stage Number
- SpawnPoint ID
- Main Altar 여부
- AltarPresenter 참조

`Interact()`는 ID와 참조를 확인한 뒤 `AltarPresenter.Open(this)`만 호출한다.

기술 조회, 반납 판단, 저장, 텔레포트 판단은 수행하지 않는다.

### 11.2 AltarPresenter

현재 구현된 흐름:

```text
제단 UI 열기
-> 재사용 List에 반납 가능한 기술 채우기
-> 고정 버튼 풀에 기술 표시
-> 기술 선택
-> 확인 팝업
-> TryReturnSkill
-> UI 닫기
-> SaveAsync
-> 플레이어 조작 복구
```

최적화와 안정성 기준:

- UI를 열 때 `Instantiate/Destroy`하지 않음
- 미리 배치한 고정 버튼 배열 재사용
- 목록용 `List<SkillDataSO>`를 멤버로 한 번 생성해 재사용
- 버튼 리스너는 `Init()`에서 한 번 연결
- `OnDestroy()`에서 같은 리스너 해제
- `_isBusy`로 확인 연속 입력 차단
- 저장 중 닫기와 재확인 차단
- `finally`에서 플레이어 조작 복구
- 기존 `SetCanMove`, `SetCanInteract` API 재사용

반납 가능한 기술이 없을 때는 텔레포트 구현 전까지 빈 상태 화면을 표시한다.

실제 텔레포트 목록은 계획의 7단계이므로 이번 코드에 미리 넣지 않았다.

## 12. 자동 테스트

신규 테스트 파일을 늘리지 않고 `RelicRestorationControllerTests.cs` 한 파일에 모았다.

작성한 테스트:

| 테스트 | 검증 내용 |
| --- | --- |
| `AcquiredSkillBecomesReturnable` | 획득한 기술만 반납 가능 목록에 포함 |
| `QuestSkillRewardUsesRestorationService` | 퀘스트 기술 보상이 재건 서비스 사용 |
| `UnacquiredSkillCannotBeReturned` | 미획득 기술 반납 실패 |
| `ReturnedSkillCannotBeReturnedTwice` | 같은 기술 중복 반납과 재건도 중복 증가 방지 |
| `TeleportSkillDoesNotIncreaseRestore` | 챕터 0 기술의 재건도 증가 없음 |
| `ThreeRestorationSkillsReachEnding` | 30, 60, 100과 엔딩 조건 계산 |
| `SavedReturnStateRestoresThirtyPercent` | ES3 저장·로드 후 염력 30% 복원 |
| `ProductionScriptStaysUnderLogicLineLimit` | 대상 C# 순수 로직 300줄 제한 |

테스트 코드는 Editor 어셈블리까지 컴파일했다. Unity Test Runner의 실제 실행은 별도로 남아 있다.

## 13. 순수 로직 줄 수

테스트와 같은 계산 규칙으로 확인한 결과다.

| 파일 | 순수 로직 줄 수 | 판정 |
| --- | ---: | --- |
| `SkillDataSO.cs` | 41 | 통과 |
| `RelicRestorationCatalogSO.cs` | 200 | 통과 |
| `IRelicRestorationService.cs` | 53 | 통과 |
| `RelicRestorationController.cs` | 215 | 통과 |
| `AltarInteractable.cs` | 41 | 통과 |
| `AltarPresenter.cs` | 249 | 통과 |
| `IProgressService.cs` | 11 | 통과 |
| `ProgressController.cs` | 248 | 통과 |
| `StageFlowController.cs` | 222 | 통과 |
| `GameRoot.cs` | 131 | 통과 |
| `QuestController.cs` | 298 | 통과, 상한 근접 |
| `RelicRestorationControllerTests.cs` | 297 | 통과, 상한 근접 |

`QuestController`와 테스트 파일은 이후 기능을 단순 추가하면 300줄을 초과할 가능성이 높다. 다음 수정에서는 먼저 중복 제거 또는 기존 테스트 정리를 검토해야 한다.

## 14. 검증 결과

### 14.1 C# 빌드

최종 검증:

| 대상 | 결과 |
| --- | --- |
| `Assembly-CSharp.csproj` | 빌드 성공, 이번 변경 코드 오류 없음 |
| `Assembly-CSharp-Editor.csproj --no-restore` | 경고 0개, 오류 0개 |

전체 복원 빌드 중 Unity 패키지와 Easy Save 기존 코드 경고가 출력됐지만, 이번 변경 파일의 컴파일 오류는 없었다. 최종 증분 Editor 빌드는 경고 0개, 오류 0개로 완료됐다.

### 14.2 정적 검증

- `GameObject.Find` 계열 미사용
- LINQ 미사용
- Coroutine 미사용
- 제단 UI의 동적 버튼 생성·파괴 없음
- 새 싱글턴 없음
- 새 Save Manager 없음
- 별도 텔레포트 Controller 없음
- 모든 줄 수 검사 대상 300줄 이하
- `git diff --check`에서 공백 오류 없음

### 14.3 아직 수행하지 않은 검증

- Unity Test Runner 실제 실행
- 실제 염력 획득 콘텐츠 플레이
- 제단 범위 상호작용 플레이
- 확인 팝업 취소와 연속 입력 플레이
- 게임 종료 후 재실행 저장 검증
- Unity Profiler GC Alloc 검증

## 15. 계획 단계별 현재 상태

| 계획 단계 | 상태 | 비고 |
| --- | --- | --- |
| 구현 계획 작성 | 완료·커밋됨 | `37df341` `[문서] 유적 재건 구현 계획 작성완료` |
| 1. 규칙과 데이터 준비 | 코드 완료, 에셋 미완료 | 기술·Catalog 타입은 있으나 실제 에셋 없음 |
| 2. 순수 재건 로직 | 코드와 테스트 작성 완료 | Editor 어셈블리 컴파일 완료 |
| 기술 획득 연결 | 코드 완료 | 실제 염력 획득 콘텐츠 연결은 남음 |
| 3. 저장·불러오기 | 코드 완료 | 실제 게임 재실행 검증은 남음 |
| 4. 최소 제단 UI | 코드 완료 | UI 프리팹·씬 오브젝트·Inspector 연결은 남음 |
| 5. 1차 복원 수직 슬라이스 | 기획 대기 | 유적 Root와 Timeline 정의 필요 |
| 6. 나머지 기술과 엔딩 | 미착수 | 1차 수직 슬라이스 완료 후 진행 |
| 7. 텔레포트 | 미착수 | 제단 목적지와 활성화 규칙 확정 후 진행 |

## 16. 현재 만들지 않은 Unity 콘텐츠

다음 항목은 코드가 아니라 기획, 아트, 연출, 씬 구성이 필요하므로 임의로 만들지 않았다.

- 실제 기술 `SkillDataSO` 에셋 4개
- 실제 `RelicRestorationCatalogSO` 에셋
- Localization 문구
- 메인 제단 또는 복원 장치 오브젝트
- 제단 상호작용용 Collider와 Layer 연결
- 제단 UI 프리팹과 버튼 4개
- `GameRoot.prefab`의 Controller·Presenter 참조
- 유적 파괴 상태 Root
- 유적 30%, 60%, 100% 복원 Root
- 1·2·3차 복원 Timeline
- 엔딩 씬 연결
- 제단 텔레포트 목적지

현재 `GameRoot.prefab`과 게임 씬은 변경하지 않았다.

## 17. 확인된 기획 공백

구현 계획에는 시스템 규칙이 있지만 플레이어가 실제로 경험할 콘텐츠 정의가 부족했다.

확정이 필요한 내용:

1. 유적이 게임 안에서 정확히 어떤 장소나 오브젝트인지
2. 제단이 유적의 일부인지 별도 장치인지
3. `제단`이 최종 명칭인지
4. 기술을 되돌려놓아도 능력이 유지되는 세계관 이유
5. 0%, 30%, 60%, 100%에서 유적 외형이 어떻게 달라지는지
6. 기술 반납 UI의 제목, 안내, 확인 문구
7. 플레이어에게 이 시스템을 처음 소개하는 방법
8. 각 복원 단계 Timeline의 실제 연출
9. 100% 복원 후 엔딩의 정확한 흐름
10. 제단이 텔레포트도 제공하는 이유와 목적지 규칙

이 결정 없이 5단계 구현을 진행하면 오브젝트 구조, UI, 이름, 연출 코드를 다시 작성할 가능성이 높다.

따라서 이번 작업은 기반 코드에서 중단하고 기획 확정 후 재개하기로 했다.

## 18. 재개 시 작업 순서

기획이 확정되면 다음 순서로 이어간다.

1. 유적·제단의 최종 용어와 플레이 흐름을 구현 계획에 반영
2. 기술 에셋 4개와 Catalog 에셋 생성
3. Localization Key와 실제 문구 추가
4. 메인 제단 오브젝트에 `AltarInteractable` 연결
5. 제단 UI와 `AltarPresenter` 직렬화 참조 연결
6. `GameRoot.prefab`에 재건 Controller와 Presenter 연결
7. 실제 염력 획득 콘텐츠를 `TryAcquireSkill`과 연결
8. 제단 UI 플레이 모드 검증
9. 마지막 런타임 파일 `RelicRestorationPresenter.cs` 작성
10. 파괴 상태와 30% 복원 Root 연결
11. 1차 Timeline 연결
12. 염력 획득부터 재실행 후 30% 유지까지 수직 슬라이스 완주
13. 60%, 100%, 엔딩 확장
14. 마지막으로 텔레포트 구현
15. Test Runner와 Profiler 최종 검증

## 19. 작업 종료 시점의 핵심 상태

```text
완료:
기술 획득·반납 규칙
재건도와 복원 단계 계산
퀘스트 기술 획득 연결
ES3 저장·불러오기 기반
제단 상호작용과 최소 UI 코드
중복 입력과 300줄 제한 방어

대기:
실제 기술·Catalog 에셋
제단과 UI의 Unity 연결
유적 외형과 Timeline
나머지 기술, 엔딩, 텔레포트
```

이번 구현은 실제 콘텐츠를 임의로 추정하지 않고, 기획이 확정되면 안정적으로 연결할 수 있는 기술적 기반까지 완성한 상태다.
