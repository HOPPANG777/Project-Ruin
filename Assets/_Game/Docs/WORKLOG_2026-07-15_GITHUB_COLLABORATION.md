# 2026-07-15 작업일지 — GitHub 협업 규약 정리

## 1. 작업 정보

| 항목 | 내용 |
| --- | --- |
| 작업일 | 2026-07-15 |
| 작성일 | 2026-07-16 |
| 작업 주제 | Project Ruin GitHub 저장소 점검 및 협업·관리 규약 문서화 |
| 작업 목적 | GitHub를 모르는 기획자와 개발자도 같은 방식으로 업무를 등록하고 작업 결과를 검토할 수 있는 기준 마련 |
| 대상 저장소 | `HOPPANG777/Project-Ruin` |
| 핵심 산출물 | `Assets/_Game/Docs/GITHUB_COLLABORATION_GUIDE.md` |
| 관련 산출물 | `CODING_CONVENTIONS.md` |
| 결과 Commit | `a698db0` — `깃허브 관리 문서 작성완료` |
| 현재 기준 Branch | `release` |

## 2. 작업 배경

프로젝트 초기에 Branch와 Commit을 개인적으로 관리하고 있었지만, 팀 전체가 함께 지켜야 할 GitHub 규약은 아직 문서화되지 않은 상태였습니다.

특히 다음 요구가 있었습니다.

- 기존에 사용하던 Commit 작성 습관이 규약으로 볼 수 있는지 판단
- 기획자가 Git과 GitHub를 잘 몰라도 업무를 관리할 수 있는 구조 설계
- 개발자와 기획자가 같은 Repository에서 충돌 없이 협업할 수 있는 기준 마련
- Unity 프로젝트에서 발생할 수 있는 `.meta`, Scene, Prefab, Project Settings 문제 예방
- 기존 시스템 작성 가이드처럼 처음부터 순서대로 읽을 수 있는 문서 작성

이번 작업의 핵심은 복잡한 Git 기능을 많이 도입하는 것이 아니라, 역할별로 꼭 필요한 기능만 사용하도록 흐름을 단순화하는 것이었습니다.

## 3. 진행 내용 요약

```text
현재 Repository 상태 확인
-> 기존 Branch와 Commit 패턴 분석
-> 개인 습관과 팀 규약의 차이 정리
-> 기획자 중심의 최소 협업 흐름 설계
-> Unity 전용 위험 요소 점검
-> GitHub 협업·관리 가이드 작성
-> 실제 Commit 결과와 포함 파일 재확인
-> 남은 설정 작업 정리
```

## 4. Repository 점검 결과

### 4.1 기본 정보

점검 당시 Repository 상태는 다음과 같았습니다.

| 항목 | 점검 결과 |
| --- | --- |
| 공개 범위 | Private Repository |
| 기본 Branch | `release` |
| Unity 버전 | `6000.3.14f1` |
| Issue | 사용 이력 없음 |
| Pull Request | 사용 이력 없음 |
| Issue/PR Template | 없음 |
| Git LFS | 설정되어 있음 |
| 병합 방식 | Merge Commit, Squash, Rebase가 모두 허용됨 |
| Auto Merge | 비활성화 |
| Branch Update 허용 | 비활성화 |

Repository가 생성된 직후에 가까운 상태였기 때문에, 기존 작업 방식과 충돌하지 않고 규약을 도입하기 좋은 시점이라고 판단했습니다.

### 4.2 Branch 상태

초기 점검 당시 확인한 원격 Branch:

```text
release
0.0.1
0.0.1-VicZero
0.01-JEW
```

당시 네 Branch는 모두 같은 초기 Commit을 가리키고 있었습니다.

기존 이름에서 확인한 패턴:

- `release`: 안정 버전 또는 배포 기준으로 사용하려는 의도
- `0.0.1`: 버전 단위 작업 Branch로 사용하려는 의도
- `0.0.1-VicZero`, `0.01-JEW`: 버전과 작업자 이름을 조합한 개인 Branch

이 방식은 누가 어떤 버전에서 작업하는지는 알 수 있지만, 실제 작업 목적과 연결된 Issue를 Branch 이름만으로 확인하기 어렵다는 문제가 있었습니다.

따라서 신규 Branch는 버전 또는 작업자보다 작업 목적을 중심으로 작성하도록 결정했습니다.

```text
feature/12-dialogue-choice
fix/25-save-reset
content/31-stage-2
balance/42-enemy-hp
docs/8-combat-guide
```

### 4.3 Git LFS

`.gitattributes`에는 다음 종류의 파일을 Git LFS로 관리하는 규칙이 준비되어 있었습니다.

- 이미지와 PSD
- 음원과 영상
- 3D 모델
- DLL과 실행 파일
- Font
- 압축 파일
- PDF

실제 에셋 중 이미지와 DLL 등도 LFS 대상으로 등록되어 있었습니다.

대용량 파일 관리 기반은 이미 준비되어 있었으므로, 이 설정은 유지하는 방향으로 정리했습니다.

### 4.4 Unity `.meta` 문제 발견

가장 중요한 발견은 `.gitignore`의 다음 규칙이었습니다.

```gitignore
*.meta
```

점검 당시 결과:

- Git이 추적하고 있던 `.meta`: 9개
- 무시되고 있던 `.meta`: 약 15,871개

Unity는 `.meta` 파일 안의 GUID로 Scene, Prefab, Sprite, Script 참조를 연결합니다. 원본 에셋만 공유하고 `.meta`가 누락되면 다른 팀원의 Unity가 새로운 GUID를 만들 수 있으며, 기존 참조가 끊어질 수 있습니다.

이를 통해 다음 내용을 확인했습니다.

- Git LFS가 설정되어 있어도 `.meta` 누락 문제는 해결되지 않음
- Unity 협업에서는 에셋과 `.meta`를 한 쌍으로 관리해야 함
- 새 파일뿐 아니라 새 폴더의 `.meta`도 필요함
- `.meta` 문제를 해결하기 전 인원이 늘어나면 참조 오류 위험이 커짐

이 문제는 문서에도 필수 규칙으로 포함했지만, `.gitignore` 자체의 수정과 대량 `.meta` 검토는 별도 안전 작업으로 남겨두었습니다.

## 5. 기존 Commit 패턴 분석

이전에 비교적 엄격하게 관리했던 다른 프로젝트의 GitHub Desktop 기록을 확인했습니다.

확인한 Commit 예시:

```text
Update Vector.h
Point, Map 작성완료
폴더 구조 변경
파일 인코딩 변환
주석 컨벤션 추가 및 주석 적용
Vector2, TileType 완성
중간저장
```

### 5.1 잘하고 있던 부분

- 작업을 한 번에 몰아넣지 않고 여러 Commit으로 나누고 있었음
- Commit 본문에 파일별 변경 내용을 작성하고 있었음
- `Point`, `Map`처럼 서로 관련된 기능을 하나의 작업 단위로 묶고 있었음
- 변경 이유가 필요한 경우 본문에 충돌 우려와 변경 목적을 기록했음
- 폴더 구조, 인코딩, 주석 규칙처럼 기능 외 작업도 별도 Commit으로 구분했음

예를 들어 `Point, Map 작성완료` Commit은 다음과 같이 실제 작업 내용을 본문에 기록하고 있었습니다.

```text
- Map에 멤버변수, GetSize(), Contains, TileAt, IsWalkable 작성 완료
- Point.h 완성
- COMBAT_SYSTEM_DESIGN.md에 기본 객체 내용 추가
```

이 패턴은 나중에 변경 내용을 다시 확인하기 좋은 기록 방식이었습니다.

### 5.2 보완이 필요했던 부분

`Update Vector.h`처럼 파일 이름만 들어간 제목은 본문을 열기 전까지 실제 변경 목적을 알 수 없었습니다.

기존 제목:

```text
Update Vector.h
```

개선 예시:

```text
refactor: Vector2를 ZFC namespace로 이동
```

`폴더 구조 변경`, `파일 인코딩 변환`, `중간저장`도 범위와 목적을 조금 더 구체적으로 작성할 필요가 있었습니다.

```text
refactor: Core와 Map 모듈의 디렉터리 구조 분리
chore: C++ 소스 파일 인코딩을 UTF-8로 통일
wip: Point와 Map 기본 구조 작성
```

추가로 보완할 부분:

- Commit 제목의 한국어·영어 형식이 일정하지 않음
- `feat`, `fix`, `docs` 같은 작업 종류가 표시되지 않음
- Issue 번호가 없어 업무 카드와 변경 기록을 직접 연결하기 어려움
- 버전과 작업자 중심 Branch 이름은 작업 목적을 설명하지 못함
- Pull Request와 Review를 사용하지 않으면 다른 팀원이 결과를 확인하는 단계가 없음

### 5.3 개인 관리 습관과 팀 규약의 차이

기존 방식은 좋은 개인 관리 습관이었지만, 다음 조건이 없어 아직 팀 규약은 아니라고 판단했습니다.

1. 문서로 합의되어 있지 않음
2. 다른 사람이 같은 형식으로 반복할 수 있는 Template이 없음
3. 어떤 상황에 어떤 규칙을 적용할지 정의되어 있지 않음
4. PR 승인이나 Branch 보호처럼 실수를 막는 장치가 없음
5. 규칙을 지키지 않았을 때 수정하는 절차가 없음

결론적으로 기존 Commit은 규약을 만들기 위한 좋은 재료였고, 그 습관을 Issue와 PR 중심의 팀 흐름으로 확장하기로 했습니다.

## 6. 결정한 GitHub 관리 방식

### 6.1 복잡한 Git Flow를 사용하지 않음

GitHub를 처음 사용하는 기획자가 참여하는 프로젝트이므로 `develop`, `release`, `hotfix`를 여러 단계로 운영하는 복잡한 Git Flow는 사용하지 않기로 했습니다.

선택한 기본 흐름:

```text
Issue
-> Project Ready
-> 작업 Branch
-> Commit과 Push
-> Pull Request
-> Review
-> Squash and merge
-> Done
```

### 6.2 `release`의 역할

`release`는 다음 조건을 만족하는 안정적인 기준 Branch로 정의했습니다.

- Unity에서 정상적으로 열 수 있어야 함
- 실행 가능한 상태여야 함
- 검토가 끝난 변경만 포함해야 함
- 직접 Commit 또는 Push하지 않음
- 모든 변경은 PR을 통해 반영

### 6.3 하나의 Issue와 하나의 목적

모든 작업은 Issue에서 시작하도록 정리했습니다.

Issue에 반드시 들어갈 내용:

- 작업 목적
- 실제 작업 내용
- 확인 가능한 완료 조건
- 영향받는 Scene, Prefab, CSV, 시스템, 문서
- 참고 자료
- 담당자와 우선순위

완료 조건은 `기능 구현 완료`처럼 추상적으로 적지 않고 실제로 확인할 수 있도록 작성합니다.

```text
- [ ] 조건을 만족하는 선택지만 표시된다.
- [ ] 선택 후 지정된 다음 대화로 이동한다.
- [ ] 저장 후 다시 불러와도 선택 결과가 유지된다.
```

### 6.4 Project 상태

상태는 Label과 중복하지 않고 Project에서 관리하도록 정리했습니다.

| 상태 | 의미 |
| --- | --- |
| Backlog | 아이디어 또는 미확정 작업 |
| Ready | 목적과 완료 조건이 확정되어 시작 가능 |
| In Progress | 담당자가 작업 중 |
| Review | PR 또는 결과물 검토 중 |
| Blocked | 외부 결정이나 다른 작업을 기다리는 중 |
| Done | 검토와 병합까지 완료 |

### 6.5 Label

Label은 상태가 아니라 작업 종류, 분야, 우선순위를 표현하도록 정리했습니다.

```text
type:feature
type:bug
type:balance
type:docs

area:planning
area:programming
area:art
area:ui
area:sound

priority:P0
priority:P1
priority:P2
priority:P3
```

### 6.6 Commit 제목

Commit은 다음 형식을 기준으로 정했습니다.

```text
<타입>: <무엇을 변경했는지>
```

| 타입 | 용도 |
| --- | --- |
| feat | 새로운 기능 |
| fix | 오류 수정 |
| content | 콘텐츠 데이터 |
| balance | 밸런스 수치 |
| docs | 문서 |
| refactor | 동작 변화 없는 구조 개선 |
| chore | 설정, 인코딩, 파일 정리 |
| test | 테스트 |

예시:

```text
feat: 대화 선택지 조건 처리 추가
content: 큐레이터 첫 퀘스트 대사 작성
fix: 퀘스트 보상 중복 지급 방지
docs: 전투 시스템 작성 가이드 갱신
```

### 6.7 Pull Request와 Squash merge

기획자에게 모든 중간 Commit을 완벽하게 작성하도록 요구하면 Git 사용 부담이 커질 수 있습니다.

따라서 다음 방식으로 최종 기록의 품질을 관리하기로 했습니다.

- 작업 Branch의 중간 Commit은 작업 흐름에 맞게 작성
- 최종 PR 제목과 본문은 정확하게 작성
- 병합 시 `Squash and merge` 사용
- `release`에는 PR 하나가 하나의 정리된 Commit으로 남음

PR 제목 형식:

```text
<타입>: <최종 변경 결과> (#Issue 번호)
```

예시:

```text
feat: NPC 대화 선택지 조건 처리 (#12)
fix: 저장 후 퀘스트 상태 초기화 수정 (#25)
docs: GitHub 협업 가이드 작성 (#8)
```

PR 본문에는 관련 Issue, 변경 내용, 확인 방법, 확인 결과, 영향 범위, 첨부 자료를 작성하도록 정리했습니다.

## 7. 기획자가 편하게 관리하도록 정한 부분

### 7.1 업무 관리만 하는 기획자

파일을 수정하지 않는 기획자는 Git Branch와 Commit을 알 필요가 없도록 역할을 분리했습니다.

필요한 기능:

- Issue 작성
- 담당자와 우선순위 지정
- Project 상태 변경
- PR 결과와 완료 조건 확인
- 승인 또는 수정 요청

### 7.2 문서와 데이터를 수정하는 기획자

Markdown, CSV, Unity 데이터를 직접 수정하는 기획자만 GitHub Desktop을 사용합니다.

```text
Fetch origin
-> release Pull
-> 작업 Branch 생성
-> 파일 수정
-> Changes 확인
-> Commit
-> Push origin
-> Create Pull Request
```

### 7.3 기획 리뷰의 범위

기획자는 코드 전체를 검토하지 않고 다음 내용을 확인합니다.

- Issue의 목적과 완료 조건 충족 여부
- 게임 화면과 데이터가 기획 의도와 일치하는지
- 빠진 예외 상황이 없는지
- PR의 확인 순서를 실제로 따라 할 수 있는지
- 스크린샷이나 영상이 결과와 일치하는지

개발자는 코드 구조, 저장 데이터 영향, 테스트, Scene과 Prefab 충돌 위험을 추가로 확인합니다.

## 8. Unity 협업 규칙으로 정리한 내용

### 8.1 에셋과 `.meta`

- Unity 에셋과 `.meta`는 반드시 함께 반영
- 기존 `.meta`를 임의로 삭제하거나 재생성하지 않음
- 파일과 폴더 이동은 Unity Project 창에서 수행
- `.meta`가 변경 목록에 없다면 Commit 전에 확인

### 8.2 Scene과 Prefab

- Issue의 영향 범위에 수정할 Scene과 Prefab을 작성
- 같은 Scene 또는 Prefab을 두 명이 동시에 수정하지 않음
- 가능하면 기능을 독립 Prefab 또는 별도 Scene으로 분리
- YAML Conflict를 이해하지 못한 상태에서 직접 해결하지 않음

### 8.3 Project Settings

- Project Settings 변경은 PR에 이유를 기록
- 기능 작업과 무관한 설정 변경이 함께 들어가지 않았는지 확인
- Unity 버전은 `6000.3.14f1`로 고정
- Unity Console 오류가 있는 상태로 병합하지 않음

### 8.4 Git LFS

- 기존 `.gitattributes` 규칙 유지
- 이미지, 음원, 영상, 모델, PSD, DLL은 LFS로 관리
- LFS Pointer만 보이거나 에셋이 열리지 않으면 관리자에게 알림

## 9. 작성한 문서

다음 문서를 작성했습니다.

```text
Assets/_Game/Docs/GITHUB_COLLABORATION_GUIDE.md
```

문서에 포함한 내용:

1. 문서 목적과 반드시 지키는 규칙
2. GitHub 초보자를 위한 용어 설명
3. 기획자, 개발자, 관리자 역할 분리
4. Issue 작성 방법과 완료 조건 예시
5. Project 상태와 우선순위
6. Branch 이름 규칙
7. GitHub Desktop 실제 작업 순서
8. Commit과 PR 작성 예시
9. 기획 리뷰와 개발 리뷰 기준
10. Squash merge와 작업 종료 순서
11. Unity `.meta`, Scene, Prefab, LFS 규칙
12. Conflict 발생 시 대응 순서
13. 기획자 작업 유형별 절차
14. 버전과 Release 관리
15. 금지 사항과 자주 발생하는 문제
16. 작업자와 리뷰어 완료 체크리스트

문서는 기존 `DIALOGUE_QUEST_AUTHORING_GUIDE.md`처럼 다음 흐름을 따라 작성했습니다.

```text
현재 기준 설명
-> 실제 작업 대상
-> 단계별 작업 순서
-> 주의할 문제
-> 완료 체크리스트
```

## 10. 실제 Commit 결과

2026-07-15 13:58에 다음 Commit이 생성되었습니다.

```text
a698db0 깃허브 관리 문서 작성완료

- _Game/Docs에 GITHUB_COLLABORATION_GUIDE.md 작성 완료
```

Commit에 포함된 파일:

| 파일 | 변경 내용 |
| --- | --- |
| `Assets/_Game/Docs/GITHUB_COLLABORATION_GUIDE.md` | GitHub 협업·관리 가이드 추가 |
| `CODING_CONVENTIONS.md` | Unity 개발 코딩 컨벤션 추가 |
| `Assets/Resources/DOTweenSettings.asset` | UI Toolkit, TextMeshPro DOTween 모듈 활성화 |
| `ProjectSettings/ProjectSettings.asset` | 여러 플랫폼에 DOTween 관련 Scripting Define 추가 |

Commit 통계:

```text
4 files changed
1,241 insertions
17 deletions
```

### Commit에서 확인한 개선점

Commit 제목과 본문은 GitHub 관리 문서만 설명하지만 실제로는 코딩 컨벤션과 Unity 설정 변경도 함께 포함되었습니다.

이는 이번에 만든 규약의 다음 항목이 필요한 이유를 보여주는 실제 사례입니다.

- Commit 전에 `Changes`에서 포함 파일 전체 확인
- 관련 없는 문서와 설정 변경은 별도 Commit 또는 PR로 분리
- Project Settings가 포함되면 변경 이유를 PR에 명시
- Commit 제목과 실제 변경 범위를 일치시킴

이미 공유된 Commit 기록을 임의로 다시 작성하지는 않습니다. 이후 작업부터 목적별로 분리하는 방식으로 개선합니다.

## 11. 함께 추가된 코딩 컨벤션

`CODING_CONVENTIONS.md`에는 다음 개발 기준이 정리되어 있습니다.

- 사용 기술 스택
- 의존성 주입과 낮은 결합도
- 상속 구조 제한
- Manager와 Controller 역할 구분
- Update, 반복문, UniTask 사용 기준
- DOTween Pro 연출 규칙
- Easy Save 3 저장·로드 규칙
- 루프 최적화와 방어적 프로그래밍
- 주석과 문서화 규칙
- 클래스, 함수, 변수 Naming Convention
- Inspector Field와 Attribute 규칙
- 표준 코드 예시

GitHub 협업 가이드는 작업을 어떻게 공유하고 검토할지를 설명하고, 코딩 컨벤션은 코드 자체를 어떤 기준으로 작성할지를 설명합니다.

두 문서의 역할은 다음과 같이 구분합니다.

```text
GITHUB_COLLABORATION_GUIDE.md
-> Issue, Branch, Commit, PR, Review, Merge 기준

CODING_CONVENTIONS.md
-> Architecture, Code, Naming, Comment, Optimization 기준
```

## 12. 이번 작업으로 알게 된 내용

### 12.1 Commit을 꼼꼼하게 작성하는 것만으로 규약이 되지는 않음

개인이 좋은 제목과 본문을 작성하고 있어도 다른 사람이 같은 방식으로 반복할 수 없다면 개인 습관에 가깝습니다.

팀 규약이 되려면 다음 요소가 필요합니다.

- 문서화
- 공통 Template
- 역할과 책임
- 예외 처리 방법
- Review 절차
- GitHub 설정을 통한 실수 방지

### 12.2 기획자에게 Git 전체를 가르칠 필요는 없음

업무만 관리하는 기획자는 Issue, Project, PR Review만 알아도 됩니다. 파일을 직접 수정하는 사람에게만 Branch, Commit, Push가 필요합니다.

역할에 따라 필요한 기능을 나누는 것이 학습 부담을 줄이고 실수를 예방합니다.

### 12.3 중간 Commit보다 최종 PR의 품질을 관리하는 것이 효율적임

모든 중간 Commit을 강하게 통제하면 Git 초보자의 부담이 커집니다. 작업 Branch의 Commit은 작업 저장 기록으로 사용하고, PR 제목과 본문을 정확하게 작성한 뒤 Squash merge하면 `release`의 기록을 깔끔하게 유지할 수 있습니다.

### 12.4 Branch 이름에는 사람보다 작업 목적이 중요함

`0.0.1-VicZero`는 작업자와 버전은 알 수 있지만 어떤 기능을 다루는지 알기 어렵습니다. `feature/12-dialogue-choice`처럼 Issue 번호와 목적을 넣으면 Project와 변경 기록을 연결할 수 있습니다.

### 12.5 완료 조건은 기획과 개발 사이의 계약 역할을 함

Issue에 확인 가능한 완료 조건이 있으면 개발자는 구현 범위를 알 수 있고, 기획자는 코드를 몰라도 결과를 검토할 수 있습니다.

### 12.6 상태와 분류는 다른 정보임

- Project 상태: 지금 작업이 어디까지 진행되었는지
- Label: 작업 종류, 담당 분야, 우선순위가 무엇인지

두 기능을 같은 용도로 중복 사용하지 않는 것이 관리하기 쉽습니다.

### 12.7 Unity 협업에서는 `.meta`가 핵심 데이터임

`.meta`는 부가 파일이 아니라 참조를 유지하는 필수 파일입니다. Git LFS와 `.gitignore`가 올바르게 설정되어 있는지 함께 확인해야 합니다.

### 12.8 Scene과 Prefab 충돌은 사후 해결보다 사전 분담이 중요함

Unity YAML 파일은 코드보다 병합이 어렵습니다. Issue에 수정 대상을 기록하고 같은 Scene과 Prefab을 동시에 수정하지 않는 방식이 가장 안전합니다.

### 12.9 Commit 전 파일 목록 확인이 반드시 필요함

Commit 제목을 잘 작성하더라도 관련 없는 Unity 설정이나 문서가 함께 포함되면 변경 이력이 불명확해집니다. GitHub Desktop의 `Changes` 확인이 Commit 메시지 작성만큼 중요합니다.

## 13. 완료한 작업

- [x] 현재 Repository와 Branch 구조 확인
- [x] Issue와 PR 사용 현황 확인
- [x] 기존 Commit과 Branch 패턴 분석
- [x] 개인 습관과 팀 규약의 차이 정의
- [x] 기획자와 개발자의 역할 분리
- [x] 최소 GitHub 협업 흐름 설계
- [x] Commit, Branch, PR Naming 기준 설계
- [x] Project 상태와 Label 사용 기준 설계
- [x] Unity `.meta`, Scene, Prefab, LFS 위험 확인
- [x] 초보자용 GitHub 협업·관리 가이드 작성
- [x] 작성 문서 Commit 및 원격 Branch 반영 확인
- [x] 실제 Commit 포함 파일 재검토

## 14. 남은 작업

### 우선순위 P0 — Unity 참조 보호

- [ ] `.gitignore`의 전체 `*.meta` 제외 규칙 검토 및 수정
- [ ] 누락된 `.meta`를 무조건 한 번에 추가하지 말고 에셋 참조와 변경량 확인
- [ ] 다른 팀원이 새로 Clone했을 때 Scene, Prefab, Sprite 참조가 유지되는지 확인
- [ ] 새 문서의 `.meta`도 Git에서 추적되는지 확인

### 우선순위 P1 — GitHub 규칙 적용

- [ ] `release` Branch 직접 Push 금지 설정
- [ ] PR 승인 최소 1명 설정
- [ ] 해결되지 않은 Review 대화가 있으면 병합 금지
- [ ] Force Push와 Branch 삭제 금지
- [ ] 기본 병합 방식을 Squash merge로 통일
- [ ] 병합 후 작업 Branch 자동 삭제 설정

### 우선순위 P1 — 업무 관리 Template

- [ ] 기능·콘텐츠 Issue Form 작성
- [ ] 버그 Issue Form 작성
- [ ] 밸런스 Issue Form 작성
- [ ] Pull Request Template 작성
- [ ] Project Board 생성
- [ ] Type, Area, Priority Label 생성

### 우선순위 P2 — 기존 구조 정리

- [ ] `0.0.1`, `0.0.1-VicZero`, `0.01-JEW`, `VicZero` Branch의 역할 확인
- [ ] 필요한 변경이 모두 `release`에 반영되었는지 확인
- [ ] 불필요한 Branch는 팀원 확인 후 삭제
- [ ] 이후 신규 Branch는 Issue 번호와 목적 중심 이름 사용

### 우선순위 P2 — 문서 운영

- [ ] `GITHUB_COLLABORATION_GUIDE.md`를 팀원과 함께 읽고 질문 수집
- [ ] 실제 첫 Issue와 PR에서 규약이 이해하기 쉬운지 검증
- [ ] 복잡하거나 사용하지 않는 규칙은 줄이고 필요한 예시 추가
- [ ] 규약이 변경되면 문서와 GitHub 설정을 함께 갱신

## 15. 다음 작업 시작 체크리스트

1. `.meta` 문제를 먼저 별도 Branch와 Issue에서 해결합니다.
2. GitHub 웹에서 Project와 Label을 생성합니다.
3. Issue Form과 PR Template을 Repository에 추가합니다.
4. `release` Branch 보호와 Squash merge 설정을 적용합니다.
5. 테스트 Issue 하나를 만들어 기획자와 개발자가 전체 흐름을 연습합니다.
6. 연습 결과를 바탕으로 협업 가이드를 보완합니다.

## 16. 작업 회고

### 잘된 점

- 실제 Repository 상태와 기존 작업 습관을 기준으로 규약을 설계함
- 개발자만 이해하는 문서가 아니라 기획자의 역할을 별도로 정의함
- GitHub Desktop의 실제 조작 순서와 문제 상황을 함께 기록함
- Unity 프로젝트에서 치명적일 수 있는 `.meta` 누락 위험을 조기에 발견함
- 기존의 좋은 Commit 본문 작성 습관을 버리지 않고 팀 규칙으로 확장함

### 개선할 점

- 문서만 만들고 GitHub의 실제 Branch 보호와 Template 설정은 아직 적용하지 못함
- `*.meta` 문제를 발견했지만 실제 수정과 복구 검증은 별도 작업으로 남음
- 결과 Commit에 GitHub 문서 외 코드 규약과 Unity 설정이 함께 포함됨
- 다음부터는 Commit 직전에 제목, 본문, 포함 파일이 하나의 목적과 일치하는지 다시 확인해야 함

### 한 줄 정리

> 개인적으로 잘 관리하던 Commit 습관을, GitHub를 모르는 기획자도 참여할 수 있는 Issue·PR 중심의 팀 협업 규약으로 문서화했다.

## 17. 재사용할 작업일지 형식

다음 작업일지부터 아래 항목을 복사해서 사용합니다.

```markdown
# YYYY-MM-DD 작업일지 — 작업 제목

## 1. 작업 정보
- 작업일:
- 작업 목적:
- 관련 Issue:
- 작업 Branch:
- 결과 Commit/PR:

## 2. 작업 배경
- 왜 시작했는가?
- 어떤 문제가 있었는가?

## 3. 수행한 작업
- [ ] 작업 1
- [ ] 작업 2

## 4. 확인한 기존 패턴
- 기존 구조:
- 유지할 부분:
- 개선할 부분:

## 5. 결정한 내용
- 결정:
- 이유:
- 적용 범위:

## 6. 변경 파일
| 파일 | 변경 이유 |
| --- | --- |
| `경로` | 내용 |

## 7. 검증 결과
- 실행 확인:
- 테스트:
- 오류:

## 8. 알게 된 내용
- 학습 내용:
- 다음 작업에 적용할 점:

## 9. 남은 작업
- [ ] 후속 작업

## 10. 회고
- 잘된 점:
- 개선할 점:
- 한 줄 정리:
```
