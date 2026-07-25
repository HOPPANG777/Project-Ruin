# ES3 진행 상태 저장·불러오기 이해 문서

## 1. 이 문서의 목적

이 문서는 Project Ruin의 진행 상태가 어떻게 저장되고 다시 복원되는지 설명한다.

코드를 처음 보는 사람도 다음 질문에 답할 수 있도록 작성했다.

- ES3는 무엇인가?
- `SetFlag()`를 호출하면 즉시 파일에 저장되는가?
- Dirty는 왜 필요한가?
- 기술을 반납한 뒤 게임을 다시 실행하면 재건도는 어떻게 복원되는가?
- 새 저장 지점을 추가할 때 어떤 함수를 호출해야 하는가?

---

## 2. 가장 먼저 알아야 할 세 가지 공간

게임의 진행 데이터는 다음 세 공간을 거친다.

```text
게임 로직
   ↓ SetFlag / SetObjectState
메모리의 ProgressController
   ↓ SaveAsync
디스크의 ProjectRuinSave.es3
```

### 2.1 게임 로직

기술 획득, 기술 반납, 퀘스트 완료처럼 실제 게임 규칙이 실행되는 곳이다.

게임 로직은 ES3를 직접 호출하지 않는다. 대신 `IProgressService`에 상태 변경을 요청한다.

### 2.2 메모리

게임이 실행되는 동안 빠르게 읽고 쓸 수 있는 임시 작업 공간이다.

`ProgressController`는 다음 데이터를 메모리에 보관한다.

- 전역 플래그: 기술 획득, 기술 반납, 퀘스트 상태, 엔딩 완료 등
- 오브젝트 상태: 특정 스테이지의 문, 상자, 퍼즐 상태 등

메모리는 빠르지만 게임을 종료하면 사라진다.

### 2.3 디스크

게임을 종료해도 남아 있는 저장 공간이다. ES3가 메모리 데이터를 `.es3` 파일로 기록한다.

기본 파일 이름은 다음과 같다.

```text
ProjectRuinSave.es3
```

상대 경로이므로 실제 파일은 Unity의 `Application.persistentDataPath` 아래에 생성된다. 운영체제마다 실제 폴더 위치는 다를 수 있다.

---

## 3. ES3란 무엇인가?

ES3는 Easy Save 3의 줄임말이다. C# 객체를 파일에 저장하고 다시 C# 객체로 불러오는 Unity 플러그인이다.

이 프로젝트에서는 저장 방식을 ES3 하나로 통일한다.

- `PlayerPrefs`를 진행 데이터 저장에 사용하지 않는다.
- 직접 JSON 파일을 쓰는 별도 Save Manager를 만들지 않는다.
- 게임플레이 스크립트마다 `ES3.Save()`를 흩어 놓지 않는다.
- 실제 ES3 호출은 `ProgressController`만 담당한다.

이렇게 하면 저장 형식이나 파일 이름이 바뀌어도 한 파일만 확인하면 된다.

---

## 4. 실제로 저장하는 데이터

ES3 파일에는 `progress.data`라는 하나의 Key로 전체 진행 스냅샷을 저장한다.

```text
ProjectRuinSave.es3
└─ progress.data
   ├─ GlobalFlags
   │  ├─ skill.acquired.SKILL_TELEKINESIS = true
   │  └─ skill.returned.SKILL_TELEKINESIS = true
   └─ ObjectStates
      └─ stageNumber + objectId + stateJson
```

Key를 하나만 사용하는 이유는 체크포인트 한 번에 파일 쓰기를 한 번만 수행하기 위해서다.

현재 재건도 `30`, `60`, `100`은 저장하지 않는다. 재건도는 결과값이므로 다음과 같이 다시 계산한다.

```text
로드된 반납 플래그 확인
→ 반납된 기술의 RestoreValue 합산
→ 현재 재건도 계산
→ 재건 단계 계산
```

예를 들어 염력의 `RestoreValue`가 30이고 반납 플래그가 `true`라면, 게임을 다시 실행해도 재건도는 30이 된다.

재건도를 따로 저장하지 않으므로 플래그와 재건도 값이 서로 어긋나는 문제를 막을 수 있다.

---

## 5. Dirty란 무엇인가?

Dirty는 “메모리 데이터가 마지막 저장 이후 바뀌었다”는 표시다.

학교 과제를 수정한 뒤 저장하지 않은 상태와 비슷하다.

```text
게임 시작 직후 로드 완료  → Dirty = false
기술 획득 플래그 변경     → Dirty = true
SaveAsync 성공            → Dirty = false
같은 값을 다시 입력       → Dirty 변화 없음
```

Dirty가 필요한 이유는 디스크 쓰기가 메모리 접근보다 훨씬 느리기 때문이다.

`GetFlag()`를 호출할 때마다 파일을 읽거나, `SetFlag()`를 호출할 때마다 파일을 쓰면 프레임이 끊길 수 있다. 따라서 상태 변경은 메모리에서 빠르게 처리하고, 확정된 체크포인트에서만 한 번 저장한다.

### 같은 값은 변경으로 취급하지 않는다

```csharp
progress.SetFlag("skill.returned.SKILL_TELEKINESIS", true);
progress.SetFlag("skill.returned.SKILL_TELEKINESIS", true);
```

두 번째 호출은 실제 값이 바뀌지 않으므로 새 Dirty를 만들지 않는다.

### Dirty가 없으면 저장을 건너뛴다

`SaveAsync()`가 호출되어도 Dirty가 `false`라면 ES3 파일을 열지 않고 즉시 성공을 반환한다.

---

## 6. 저장 흐름

기술 반납을 예로 들면 다음 순서로 진행된다.

```text
1. TryReturnSkill 호출
2. 획득 여부와 중복 반납 여부 검사
3. skill.returned.<기술ID> = true
4. ProgressController가 Dirty = true 설정
5. 재건도와 복원 단계 계산
6. 컷씬 등 확정 작업 완료
7. SaveAsync를 await
8. ES3 파일 쓰기 성공
9. Dirty = false
```

`SetFlag()`는 메모리만 바꾼다. 게임 종료 후에도 남겨야 하는 시점에는 반드시 `SaveAsync()`까지 호출해야 한다.

```csharp
progressService.SetFlag(flagId, true);
bool saved = await progressService.SaveAsync(cancellationToken);
```

현재 자동 체크포인트는 씬 전환 직전이다.

```text
현재 씬 오브젝트 상태 수집
→ SaveAsync
→ 저장 완료
→ 이전 씬 언로드
→ 다음 씬 로드
```

기술 반납 UI와 컷씬이 구현되는 단계에서는 컷씬 완료 후에도 `SaveAsync()`를 호출해야 한다.

---

## 7. 왜 비동기로 저장하는가?

파일 쓰기는 저장 장치 상태에 따라 시간이 걸릴 수 있다. 메인 스레드에서 파일 작업을 끝까지 기다리면 화면이 잠깐 멈출 수 있다.

현재 구조는 다음과 같이 역할을 나눈다.

```text
메인 스레드: 현재 Dictionary를 안전한 스냅샷으로 복사
작업 스레드: ES3가 스냅샷을 파일에 기록
메인 스레드: 성공 여부와 Dirty 상태 정리
```

ES3에는 자체 비동기 Save API가 없으므로 `UniTask.RunOnThreadPool`을 사용한다. ES3의 Unity 관련 초기화는 작업 스레드로 이동하기 전에 메인 스레드에서 수행한다.

저장 중 새로운 변경이 발생할 수 있으므로 `ProgressController`는 변경 번호도 기록한다.

```text
스냅샷 버전 5 저장 중
→ 새로운 플래그 변경으로 현재 버전 6
→ 버전이 다르므로 Dirty 유지
→ 버전 6 스냅샷을 다시 저장
→ 더 이상 변경이 없으면 Dirty 해제
```

이 처리 덕분에 저장 도중 생긴 최신 변경이 저장되지 않은 채 Dirty가 꺼지는 문제를 막는다.

---

## 8. 불러오기 흐름

`GameRoot`는 다른 시스템보다 먼저 저장 파일을 불러온다.

```text
GameRoot 시작
→ 게임 CSV 데이터 초기화
→ ProgressController.LoadAsync
→ 플래그와 오브젝트 상태를 메모리에 적용
→ RelicRestorationController.Init
→ 반납 플래그를 기준으로 재건도 재계산
→ 나머지 Controller 초기화
```

로드 순서가 중요한 이유는 재건 시스템이 빈 플래그를 먼저 읽으면 재건도를 0으로 계산하기 때문이다.

저장 파일 또는 `progress.data` Key가 아직 없으면 새 게임으로 판단하고 빈 상태를 사용한다. 이것은 오류가 아니다.

파일이 손상되는 등 실제 ES3 예외가 발생하면 다음 접두사로 오류를 남긴다.

```text
[ProgressController]
```

---

## 9. 새 저장 지점을 추가하는 방법

다음 조건을 만족하는 시점만 체크포인트로 사용한다.

- 플레이어의 선택이나 보상이 확정되었다.
- 컷씬이 정상적으로 끝났다.
- 제단이 처음 활성화되었다.
- 씬을 전환하기 직전이다.
- 명시적인 세이브 포인트에 도달했다.

호출부가 이미 비동기 함수라면 다음 형태를 사용한다.

```csharp
bool saved = await progressService.SaveAsync(cancellationToken);

if (!saved)
{
    // 필요한 경우 UI에 저장 실패를 알린다.
}
```

다음 방식은 사용하지 않는다.

```csharp
// 금지: 매 프레임 저장
private void Update()
{
    progressService.SaveAsync().Forget();
}

// 금지: 게임플레이 클래스에서 ES3 직접 호출
ES3.Save("my.key", value);
```

---

## 10. 새 게임 처리

`Reset()`은 메모리의 플래그와 오브젝트 상태를 모두 비우고 Dirty를 설정한다.

```csharp
progressService.Reset();
await progressService.SaveAsync(cancellationToken);
```

`Reset()`만 호출하고 저장하지 않으면 기존 디스크 파일은 그대로 남아 있다. 따라서 새 게임 확정 흐름에서는 `Reset()` 다음에 `SaveAsync()`를 호출해야 한다.

---

## 11. 현재 자동 테스트가 확인하는 것

`RelicRestorationControllerTests`는 다음 저장 왕복을 검사한다.

```text
새 ProgressController 생성
→ 염력 획득·반납 플래그 설정
→ Dirty가 true인지 확인
→ ES3 저장
→ Dirty가 false인지 확인
→ 새로운 ProgressController 생성
→ 같은 ES3 파일 로드
→ RelicRestorationController 초기화
→ 반납 상태 true 확인
→ 재건도 30 / 복원 1단계 확인
```

테스트마다 고유한 임시 ES3 파일 이름을 사용하고 테스트 종료 후 파일을 삭제한다. 실제 플레이어 세이브 파일은 건드리지 않는다.

---

## 12. 관련 코드 위치

| 파일 | 책임 |
| --- | --- |
| `IProgressService.cs` | 저장·불러오기와 진행 상태 접근 계약 |
| `ProgressController.cs` | 메모리 상태, Dirty, ES3 I/O |
| `GameRoot.cs` | 로드가 끝난 후 다른 시스템을 초기화 |
| `StageFlowController.cs` | 씬 전환 직전 저장 체크포인트 실행 |
| `RelicRestorationController.cs` | 로드된 반납 플래그로 재건도 재계산 |
| `RelicRestorationControllerTests.cs` | 저장 왕복과 재건도 복원 검증 |

---

## 13. 한 문장 요약

진행 상태는 메모리에서 빠르게 변경하고 Dirty로 변경 여부를 기억한 뒤, 확정된 체크포인트에서만 ES3 파일에 비동기로 저장하며, 다음 실행에서는 반납 플래그를 불러와 재건도를 다시 계산한다.

---

## 14. 퀘스트 보상으로 기술을 획득하는 방법

기술 획득은 별도의 획득 전용 스크립트를 만들지 않고 기존 퀘스트 보상 경로를 재사용한다.

퀘스트 보상 데이터의 `progress_flag_id`를 다음 형식으로 설정한다.

```text
skill.acquired.<기술ID>
```

염력을 보상으로 지급하는 예시는 다음과 같다.

```text
progress_flag_id = skill.acquired.SKILL_TELEKINESIS
flag_value       = true
```

퀘스트 보상 처리 흐름은 다음과 같다.

```text
퀘스트 보상 수령
→ progress_flag_id가 skill.acquired.로 시작하는지 확인
→ 뒤쪽의 SKILL_TELEKINESIS 추출
→ Catalog에 등록된 기술인지 확인
→ RelicRestorationController.TryAcquireSkill 호출
→ 퀘스트 보상 완료 상태 기록
→ SaveAsync 호출
```

일반 퀘스트 Flag는 이전과 동일하게 `ProgressController.SetFlag()`로 전달된다. 기술 획득 Flag만 재건 서비스로 전달되므로 획득 조건과 중복 방지 규칙이 `RelicRestorationController` 한곳에 유지된다.

다음 입력은 보상 선검증에서 거부된다.

- `flag_value`가 `true`가 아닌 기술 보상
- `skill.acquired.` 뒤의 기술 ID가 비어 있는 보상
- Catalog에 등록되지 않은 기술 ID
- 재건 서비스가 초기화되지 않은 상태

현재 프로젝트에는 `ISkill` 계약과 `SkillDataSO`만 있고 염력·조각·부활의 실제 능력 실행 클래스는 아직 없다. 따라서 이번 연결이 활성화하는 것은 다음 두 가지다.

- 기술을 획득했다는 진행 상태
- 제단에서 해당 기술을 반납할 수 있는 상태

실제 전투나 퍼즐에서 기술을 사용하는 기능은 각 기술 구현체가 만들어질 때 `IsAcquired(skillId)`를 확인해 연결해야 한다.
