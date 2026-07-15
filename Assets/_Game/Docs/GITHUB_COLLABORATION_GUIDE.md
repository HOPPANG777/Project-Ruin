# GitHub 협업·관리 가이드

## 1. 문서 목적

이 문서는 GitHub를 처음 사용하는 기획자와 개발자가 Project Ruin의 작업을 안전하게 공유하고 검토하기 위한 기준입니다.

GitHub에서 가장 중요한 원칙은 다음 한 문장입니다.

> 작업은 Issue에서 시작하고, 개인 Branch에서 진행하며, Pull Request 검토 후 `release`에 반영합니다.

기획자가 모든 Git 명령어를 알 필요는 없습니다.

- 업무 등록과 일정 관리만 하는 기획자: GitHub 웹의 Issue와 Project만 사용
- 문서·CSV·Unity 데이터를 직접 수정하는 기획자: GitHub Desktop도 사용
- 코드를 수정하는 개발자: GitHub Desktop 또는 Git 명령어 사용
- 저장소 관리자: 규칙 설정, 최종 병합, 릴리스 관리

이 문서에서 설명하지 않은 예외 상황이 생기면 임의로 해결하지 않고 저장소 관리자에게 먼저 알립니다.

## 2. 반드시 지키는 규칙

다음 규칙은 직군과 숙련도에 관계없이 모두 지킵니다.

1. 모든 작업은 GitHub Issue에서 시작합니다.
2. 하나의 Branch는 하나의 Issue 또는 하나의 분명한 목적만 처리합니다.
3. `release` Branch에는 직접 Commit 또는 Push하지 않습니다.
4. 작업은 Pull Request를 통해서만 `release`에 반영합니다.
5. Pull Request는 최소 1명이 확인한 뒤 병합합니다.
6. 최종 병합은 `Squash and merge`를 사용합니다.
7. 채팅에서 결정한 내용도 Issue 또는 프로젝트 문서에 다시 기록합니다.
8. 같은 Unity Scene 또는 Prefab은 두 명이 동시에 수정하지 않습니다.
9. Unity 에셋과 해당 `.meta` 파일은 반드시 함께 반영합니다.
10. 충돌, 잘못된 Branch 작업, 대량 파일 변경이 발생하면 Push 전에 작업을 멈추고 관리자에게 알립니다.

GitHub의 Branch 보호 설정이 아직 적용되지 않았더라도 위 규칙은 동일하게 적용합니다.

## 3. 꼭 알아야 할 용어

| 용어 | 쉬운 설명 |
| --- | --- |
| Repository | 프로젝트 파일과 변경 기록이 저장되는 공간 |
| Local | 내 컴퓨터에 내려받은 프로젝트 |
| Remote / Origin | GitHub에 올라가 있는 공동 프로젝트 |
| `release` | 검토가 끝난 안정적인 기준 Branch |
| Issue | 해야 할 일, 버그, 논의 사항을 기록하는 작업 카드 |
| Project | 여러 Issue의 진행 상태를 한눈에 보는 작업 보드 |
| Branch | 공동 기준본과 분리된 개인 작업 공간 |
| Commit | 변경 내용을 하나의 저장 지점으로 기록하는 것 |
| Push | 내 Commit을 GitHub에 업로드하는 것 |
| Pull Request / PR | 내 Branch의 변경을 `release`에 반영해 달라고 요청하는 것 |
| Review | PR의 내용과 완료 조건을 다른 사람이 확인하는 것 |
| Merge | 검토된 변경을 기준 Branch에 합치는 것 |
| Conflict | 서로 같은 부분을 다르게 수정해 자동으로 합칠 수 없는 상태 |

Commit은 내 작업의 저장 기록이고, Push는 그 기록을 GitHub에 올리는 동작입니다. Commit만 하고 Push하지 않으면 다른 팀원은 변경 내용을 볼 수 없습니다.

## 4. 역할별로 사용하는 기능

### 4.1 업무만 관리하는 기획자

다음 기능만 알아도 업무 관리가 가능합니다.

- Issue 작성
- 담당자, 우선순위, 목표 버전 지정
- Project 상태 변경
- PR의 설명, 스크린샷, 완료 조건 확인
- 수정 요청 또는 승인 의견 작성

파일을 직접 수정하지 않는다면 Branch, Commit, Push를 사용할 필요가 없습니다.

### 4.2 문서와 데이터를 수정하는 기획자

다음 작업을 추가로 수행합니다.

- GitHub Desktop에서 작업 Branch 생성
- Markdown, CSV 또는 허용된 Unity 데이터 수정
- 변경 파일 확인 후 Commit
- Push 후 Pull Request 생성

Unity Scene, Prefab, Project Settings를 수정해야 한다면 작업 전에 담당 개발자 또는 관리자와 수정 범위를 확인합니다.

### 4.3 개발자

다음 사항까지 책임집니다.

- Issue의 완료 조건과 기술 범위 확인
- 기능 단위 Branch와 Commit 관리
- 관련 테스트 및 Unity Console 확인
- 영향 범위와 확인 방법을 PR에 기록
- 리뷰 의견을 같은 Branch에 반영

### 4.4 저장소 관리자

다음 사항을 관리합니다.

- `release` Branch 보호
- Issue Form, PR Template, Label, Project 설정
- 권한과 승인자 관리
- 충돌 해결 지원
- Squash merge와 버전 Tag 관리
- 규약 변경 공지

## 5. 정보가 저장되는 위치

같은 내용을 채팅, 문서, Issue에 중복해서 관리하면 어느 내용이 최신인지 알기 어렵습니다. 정보 종류별 기준 위치를 다음과 같이 정합니다.

| 정보 | 기준 위치 |
| --- | --- |
| 해야 할 일과 버그 | GitHub Issue |
| 담당자와 진행 상태 | GitHub Project |
| 작업 중 질문과 결정 | 해당 Issue 또는 PR 댓글 |
| 확정된 기획과 작성법 | `Assets/_Game/Docs/` 문서 |
| 실제 게임 데이터 | `Assets/_Game/Data/` 및 관련 Unity 에셋 |
| 코드 변경 이유와 결과 | Pull Request |
| 출시 버전 | `release`의 Git Tag |

Discord, 메신저 또는 구두로 결정한 내용은 담당자가 해당 Issue나 문서에 요약해서 남깁니다.

## 6. 전체 작업 흐름

```text
Issue 작성
-> 요구사항과 완료 조건 확인
-> Project 상태를 Ready로 변경
-> 담당자 지정
-> 작업 Branch 생성
-> Project 상태를 In Progress로 변경
-> 파일 수정 및 확인
-> Commit
-> Push
-> Pull Request 생성
-> Project 상태를 Review로 변경
-> 리뷰 및 수정
-> Squash and merge
-> Issue 종료
-> Project 상태를 Done으로 변경
```

작업이 작아 보여도 이 흐름을 생략하지 않습니다. 작은 수정도 다른 작업과 충돌하거나 예상하지 못한 영향을 줄 수 있습니다.

## 7. Issue 작성

### 7.1 Issue를 만드는 순서

1. GitHub에서 Project Ruin Repository를 엽니다.
2. 상단의 `Issues`를 선택합니다.
3. `New issue`를 선택합니다.
4. 작업에 맞는 Template을 선택합니다.
5. 제목, 목적, 작업 내용, 완료 조건을 작성합니다.
6. 담당자와 Label을 지정합니다.
7. Project와 목표 버전을 지정합니다.
8. 내용이 확정되기 전에는 `Backlog`, 작업 가능한 상태가 되면 `Ready`로 둡니다.

### 7.2 Issue 제목

Issue 제목은 결과를 예상할 수 있도록 작성합니다.

좋은 예:

```text
[기능] NPC 대화 선택지 조건 처리
[버그] 저장 후 퀘스트 완료 상태가 초기화됨
[밸런스] 폐허 지역 일반 몬스터 체력 조정
[문서] 전투 시스템 상태 전이도 갱신
```

피해야 할 예:

```text
대화 수정
오류 있음
확인 부탁
작업
```

### 7.3 Issue 본문

다음 항목을 작성합니다.

```markdown
## 목적
왜 이 작업이 필요한지 작성합니다.

## 작업 내용
- 변경할 기능이나 데이터를 작성합니다.
- 작업하지 않는 범위가 있다면 함께 작성합니다.

## 완료 조건
- [ ] 기획자나 리뷰어가 직접 확인할 수 있는 조건
- [ ] 오류가 발생하지 않아야 하는 조건

## 영향 범위
관련 Scene, Prefab, CSV, 시스템 또는 문서를 작성합니다.

## 참고 자료
기획서, 이미지, 영상, 관련 Issue 링크를 첨부합니다.
```

완료 조건은 `기능 구현 완료`처럼 작성하지 않습니다. 다음처럼 실제 확인 가능한 문장으로 작성합니다.

```text
- [ ] 조건을 만족하는 선택지만 화면에 표시된다.
- [ ] 선택지를 누르면 지정된 다음 대화 노드로 이동한다.
- [ ] 저장 후 다시 불러와도 선택 결과가 유지된다.
```

### 7.4 Issue 종류

| 종류 | 사용 시점 |
| --- | --- |
| 기능 | 새로운 시스템 또는 동작 추가 |
| 버그 | 의도와 다르게 동작하는 문제 수정 |
| 콘텐츠 | 대화, 퀘스트, 맵, 아이템 등의 실제 내용 추가 |
| 밸런스 | 수치, 난이도, 보상 조정 |
| 문서 | 기획서, 작성 가이드, 규약 수정 |
| 기술 작업 | 구조 개선, 패키지, 설정, 빌드 관련 작업 |

### 7.5 우선순위

| 우선순위 | 기준 |
| --- | --- |
| P0 | 작업 진행, 실행 또는 빌드를 막는 문제 |
| P1 | 현재 목표 버전에 반드시 필요한 작업 |
| P2 | 일반적인 기능과 콘텐츠 작업 |
| P3 | 일정에 여유가 있을 때 진행할 개선 작업 |

모든 작업을 P0 또는 P1로 지정하지 않습니다. 우선순위가 불분명하면 기획 책임자 또는 관리자와 조정합니다.

## 8. Project 상태 관리

Project의 상태는 다음 의미로 사용합니다.

| 상태 | 의미 |
| --- | --- |
| Backlog | 아이디어이거나 요구사항이 아직 확정되지 않음 |
| Ready | 목적, 작업 내용, 완료 조건이 확정되어 시작할 수 있음 |
| In Progress | 담당자가 실제 작업 중 |
| Review | PR 또는 결과물 확인 중 |
| Blocked | 다른 작업, 결정 또는 외부 자료를 기다리는 중 |
| Done | 검토와 병합까지 완료됨 |

상태 변경 규칙:

- 기획 책임자: `Backlog -> Ready`
- 작업 담당자: `Ready -> In Progress`
- PR 작성자: `In Progress -> Review`
- 리뷰어 또는 관리자: `Review -> Done`
- 작업을 진행할 수 없게 된 담당자: `Blocked`로 변경하고 이유를 댓글로 기록

단순히 작업을 잠시 쉬는 경우에는 `Blocked`를 사용하지 않습니다. 해결해야 할 구체적인 방해 요인이 있을 때만 사용합니다.

## 9. Branch 규칙

### 9.1 기준 Branch

Project Ruin의 기준 Branch는 `release`입니다.

- 검토가 끝난 파일만 존재해야 합니다.
- Unity에서 열 수 있고 실행 가능한 상태를 유지합니다.
- 직접 Commit 또는 Push하지 않습니다.
- 모든 변경은 작업 Branch와 PR을 통해 반영합니다.

기존에 `0.0.1`, `0.0.1-VicZero`와 같은 Branch가 있더라도, 이 규약 적용 이후 새 작업은 목적 중심 이름을 사용합니다.

### 9.2 Branch 이름

```text
<종류>/<Issue 번호>-<짧은 영문 설명>
```

예시:

```text
feature/12-dialogue-choice
fix/25-save-reset
content/31-stage-2
balance/42-enemy-hp
docs/8-combat-guide
refactor/51-inventory-structure
chore/60-unity-settings
```

Branch 이름 규칙:

- 공백을 사용하지 않습니다.
- 한글 대신 짧은 영문을 사용합니다.
- 단어는 `-`로 구분합니다.
- 사람 이름보다 작업 목적을 적습니다.
- 하나의 Branch에 관련 없는 Issue를 섞지 않습니다.
- 버전 번호만 사용한 Branch를 새로 만들지 않습니다.

## 10. GitHub Desktop으로 작업 시작

### 10.1 시작 전 확인

1. 현재 Unity 작업을 저장하고 가능하면 Unity를 종료합니다.
2. GitHub Desktop에서 `Current repository`가 Project Ruin인지 확인합니다.
3. `Fetch origin`을 선택해 GitHub의 최신 정보를 가져옵니다.
4. `Current branch`에서 `release`를 선택합니다.
5. `Pull origin`이 표시되면 실행해 최신 상태로 맞춥니다.
6. 다시 `Current branch`를 선택합니다.
7. `New branch`를 선택합니다.
8. Issue 번호가 포함된 Branch 이름을 입력합니다.
9. 새 Branch가 `release`에서 만들어지는지 확인합니다.
10. Project 상태를 `In Progress`로 변경합니다.

`Fetch origin`은 변경 정보를 확인하는 동작이고, `Pull origin`은 실제 변경 파일을 내 컴퓨터에 받는 동작입니다.

### 10.2 작업 중 확인

GitHub Desktop의 `Changes`에는 현재 수정된 파일이 표시됩니다.

- 내가 의도한 파일만 변경되었는지 확인합니다.
- 수정한 기억이 없는 파일이 많으면 Commit하지 않습니다.
- `Library`, `Temp`, `Logs`, 개인 IDE 설정 파일이 보이면 관리자에게 알립니다.
- Unity가 자동으로 수정한 Project Settings가 포함되면 변경 이유를 확인합니다.

다른 작업에서 남아 있던 변경을 현재 작업 Commit에 함께 넣지 않습니다.

### 10.3 Branch를 바꿀 수 없는 경우

수정 중인 파일이 있으면 GitHub Desktop이 Branch 전환을 막거나 변경 파일을 함께 가져갈 수 있습니다.

이 경우 다음 중 하나를 선택하기 전에 관리자와 확인합니다.

- 현재 Branch에서 Commit한 뒤 이동
- 임시 보관 후 이동
- 변경 내용을 폐기

무엇인지 모르는 변경을 임의로 폐기하지 않습니다.

## 11. Commit 작성

### 11.1 Commit의 역할

Commit은 작업 과정의 저장 지점입니다. 작업 Branch 안의 모든 Commit을 완벽하게 작성할 필요는 없지만, 다른 사람이 변경 목적을 알 수 있어야 합니다.

하나의 Commit에는 하나의 목적만 담는 것을 권장합니다.

```text
<타입>: <변경 내용>
```

사용하는 타입:

| 타입 | 사용 시점 |
| --- | --- |
| feat | 새로운 기능 추가 |
| fix | 오류 수정 |
| content | 콘텐츠 데이터 추가·수정 |
| balance | 밸런스 수치 변경 |
| docs | 문서 변경 |
| refactor | 기능 변화 없는 구조 개선 |
| chore | 설정, 파일 정리, 인코딩 등의 작업 |
| test | 테스트 추가·수정 |

좋은 예:

```text
feat: 대화 선택지 조건 처리 추가
content: 큐레이터 첫 퀘스트 대사 작성
fix: 퀘스트 보상 중복 지급 방지
docs: 전투 시스템 작성 가이드 갱신
chore: 소스 파일 인코딩을 UTF-8로 통일
```

피해야 할 예:

```text
작업
수정
Update
최종
진짜 최종
```

### 11.2 Commit 본문

변경 이유나 영향이 제목만으로 설명되지 않으면 본문을 작성합니다.

```text
refactor: Vector2를 ZFC namespace로 이동

- SimpleMath2의 같은 이름 타입과 충돌하지 않도록 변경
- 기존 Vector2 사용 위치를 함께 수정
```

### 11.3 Commit하는 순서

1. GitHub Desktop의 `Changes`에서 변경 파일을 확인합니다.
2. 이번 Commit에 포함할 파일만 체크합니다.
3. 왼쪽 아래 `Summary`에 Commit 제목을 작성합니다.
4. 필요한 경우 `Description`에 상세 내용을 작성합니다.
5. `Commit to <현재 Branch>`를 선택합니다.
6. 작업을 공유하거나 백업하려면 `Push origin`을 선택합니다.

Commit 후에도 작업을 계속할 수 있습니다. PR을 만들기 전에는 필요한 Commit이 모두 Push되었는지 확인합니다.

## 12. Pull Request 작성

### 12.1 PR을 만드는 시점

- 작업이 완료되었고 직접 확인까지 끝났다면 일반 PR을 만듭니다.
- 작업 방향을 먼저 검토받고 싶다면 Draft PR을 만듭니다.
- 단순히 작업을 백업하려는 목적이라면 Push만 하고 PR은 나중에 만들어도 됩니다.

### 12.2 GitHub Desktop에서 PR 만들기

1. 작업 Branch에서 마지막 Commit을 Push합니다.
2. GitHub Desktop의 `Create Pull Request`를 선택합니다.
3. 브라우저에서 GitHub PR 작성 화면이 열리는지 확인합니다.
4. `base`가 `release`인지 확인합니다.
5. `compare`가 내 작업 Branch인지 확인합니다.
6. PR 제목과 본문을 작성합니다.
7. 관련 Issue, 담당 리뷰어, Label, Project를 연결합니다.
8. 검토 준비가 끝났으면 PR을 생성합니다.

`base`와 `compare`가 반대로 되어 있으면 생성하지 않습니다.

### 12.3 PR 제목

```text
<타입>: <최종 변경 결과> (#Issue 번호)
```

예시:

```text
feat: NPC 대화 선택지 조건 처리 (#12)
fix: 저장 후 퀘스트 상태 초기화 수정 (#25)
content: 폐허 지역 2차 대화 데이터 추가 (#31)
docs: GitHub 협업 가이드 작성 (#8)
```

PR은 Squash merge 후 `release`에 하나의 Commit으로 남습니다. 따라서 작업 중 Commit보다 PR 제목과 설명을 더 정확하게 작성합니다.

### 12.4 PR 본문

```markdown
## 관련 Issue
Closes #12

## 변경 내용
- 실제 변경 결과를 작성합니다.

## 확인 방법
1. 확인할 Scene 또는 메뉴를 엽니다.
2. 필요한 조작을 수행합니다.
3. 기대 결과를 확인합니다.

## 확인 결과
- [ ] 관련 기능을 직접 실행했다.
- [ ] Unity Console 오류가 없다.
- [ ] 기존 기능에 문제가 없는지 확인했다.

## 영향 범위
변경한 Scene, Prefab, CSV, 시스템, Project Settings를 작성합니다.

## 첨부 자료
화면 변경이 있으면 스크린샷 또는 영상을 첨부합니다.

## 리뷰어 참고 사항
특히 확인이 필요한 부분과 남은 위험을 작성합니다.
```

`Closes #12`처럼 작성하면 PR이 병합될 때 연결된 Issue가 자동으로 종료됩니다.

## 13. Review

### 13.1 기획 리뷰

기획자는 코드 전체를 이해할 필요가 없습니다. 다음 내용을 확인합니다.

- Issue의 목적과 완료 조건을 충족하는가?
- 기획 데이터와 화면 결과가 의도와 일치하는가?
- 빠진 예외 상황이 없는가?
- 확인 방법을 실제로 따라 할 수 있는가?
- 스크린샷 또는 영상이 변경 결과와 일치하는가?

### 13.2 개발 리뷰

개발자는 다음 내용을 추가로 확인합니다.

- 코드와 폴더 구조가 프로젝트 규칙을 따르는가?
- 관련 없는 파일이 섞이지 않았는가?
- 저장 데이터, 참조, 기존 기능에 위험이 없는가?
- 테스트와 Unity Console 확인이 충분한가?
- Scene, Prefab, Project Settings 충돌 가능성이 없는가?

### 13.3 수정 요청

수정이 필요하면 PR 댓글에 다음 내용을 구체적으로 작성합니다.

```text
현재 결과:
어떤 문제가 보이는지 작성합니다.

기대 결과:
어떻게 변경되어야 하는지 작성합니다.

확인 위치:
파일, 화면, 재현 순서를 작성합니다.
```

PR 작성자는 새 Branch나 새 PR을 만들지 않고 기존 작업 Branch에서 수정합니다. 수정 후 Commit과 Push를 수행하면 기존 PR에 자동으로 반영됩니다.

## 14. Merge와 작업 종료

다음 조건을 모두 만족할 때만 병합합니다.

- 연결된 Issue가 있다.
- 완료 조건을 모두 확인했다.
- 필요한 리뷰어 최소 1명이 승인했다.
- 수정 요청이 모두 해결되었다.
- GitHub가 충돌을 표시하지 않는다.
- Unity Console 오류와 필수 테스트 실패가 없다.
- 작성자가 변경 영향과 확인 방법을 기록했다.

병합 순서:

1. PR에서 최종 승인 상태를 확인합니다.
2. `Squash and merge`를 선택합니다.
3. 최종 Commit 제목이 PR 제목과 일치하는지 확인합니다.
4. 병합을 완료합니다.
5. 작업 Branch를 삭제합니다.
6. Issue가 자동으로 종료되었는지 확인합니다.
7. Project 상태를 `Done`으로 변경합니다.
8. 다른 팀원은 작업 시작 전에 `release`를 다시 Pull합니다.

`Create a merge commit`이나 `Rebase and merge`는 관리자가 별도로 지시한 경우가 아니면 사용하지 않습니다.

## 15. Unity 프로젝트 추가 규칙

### 15.1 `.meta` 파일

Unity는 `.meta` 파일의 GUID로 Scene, Prefab, Sprite 등의 참조를 연결합니다.

- 새 파일과 새 폴더의 `.meta`를 함께 반영합니다.
- 기존 `.meta`를 임의로 삭제하거나 다시 만들지 않습니다.
- 파일을 이동할 때 원본 `.meta`도 함께 이동되어야 합니다.
- `.meta`가 보이지 않거나 Commit되지 않으면 관리자에게 알립니다.

`.meta`가 누락되면 다른 팀원의 프로젝트에서 이미지, Prefab, Script 참조가 끊어질 수 있습니다.

### 15.2 Scene과 Prefab

- 작업 시작 전에 Issue의 영향 범위에 수정할 Scene과 Prefab을 작성합니다.
- 같은 파일을 다른 사람이 수정 중인지 Project와 Issue에서 확인합니다.
- 같은 Scene 또는 Prefab을 동시에 수정하지 않습니다.
- 가능하면 기능을 독립 Prefab 또는 별도 Scene으로 분리합니다.
- 충돌이 발생한 Scene이나 Prefab을 텍스트 편집기로 임의 수정하지 않습니다.

### 15.3 Unity 버전

프로젝트에서 지정한 Unity Editor 버전을 사용합니다.

현재 기준 버전:

```text
6000.3.14f1
```

Unity 버전을 변경해야 한다면 별도 Issue와 PR로 진행하고 모든 팀원에게 공지합니다.

### 15.4 에셋 이동과 이름 변경

- Unity 에셋은 Windows 탐색기가 아니라 Unity Project 창에서 이동합니다.
- 파일 이름과 폴더 이름 변경도 Unity 안에서 진행합니다.
- 이동 후 Scene과 Prefab 참조가 유지되는지 확인합니다.
- 대량 이동은 기능 작업과 분리된 별도 PR로 진행합니다.

### 15.5 대용량 파일과 Git LFS

이미지, 음원, 영상, PSD, 모델, DLL 등의 대용량 파일은 Git LFS로 관리합니다.

- `.gitattributes`의 LFS 규칙을 임의로 삭제하지 않습니다.
- 파일 내용 대신 짧은 LFS Pointer가 보이거나 에셋이 열리지 않으면 관리자에게 알립니다.
- 대용량 원본을 ZIP으로 묶어 Repository에 추가하지 않습니다.

### 15.6 작업 완료 전 확인

- Unity의 에셋 Import가 끝났는가?
- Unity Console 오류가 0개인가?
- 변경한 기능을 Play Mode에서 직접 확인했는가?
- 관련 자동 테스트가 통과하는가?
- 의도하지 않은 Scene 또는 Project Settings 변경이 없는가?
- 새 에셋과 `.meta`가 함께 변경 목록에 있는가?

## 16. Conflict가 발생했을 때

Conflict는 잘못을 의미하지 않습니다. 두 작업이 같은 부분을 다르게 수정했다는 뜻입니다.

Conflict가 표시되면 다음 순서로 대응합니다.

1. 추가 수정과 병합을 멈춥니다.
2. 해당 Issue 또는 PR에 충돌 사실을 기록합니다.
3. 충돌 파일 경로와 각 Branch 이름을 관리자에게 전달합니다.
4. 두 작업 담당자가 어떤 변경을 유지할지 결정합니다.
5. 개발자 또는 관리자가 충돌을 해결합니다.
6. Unity에서 참조와 동작을 다시 확인합니다.

기획자가 혼자 처리하지 말아야 하는 충돌:

- `.unity`
- `.prefab`
- `.asset`
- `ProjectSettings/`
- `.meta`
- 저장 및 데이터 스키마 관련 코드

충돌 해결 화면에서 의미를 모른 채 `Use mine`, `Use theirs`, `Accept current`, `Accept incoming`을 선택하지 않습니다.

## 17. 기획자 작업 유형별 순서

### 17.1 Issue와 일정만 관리

```text
GitHub 웹 접속
-> Issue 작성
-> 담당자와 완료 조건 지정
-> Project 상태 관리
-> PR 결과 확인
-> 승인 또는 수정 요청
```

GitHub Desktop과 Unity를 사용할 필요가 없습니다.

### 17.2 Markdown 기획 문서의 작은 수정

GitHub 웹에서 파일의 연필 모양 `Edit` 기능을 사용할 수 있습니다.

1. 수정할 Markdown 파일을 엽니다.
2. `Edit this file`을 선택합니다.
3. 내용을 수정하고 Preview를 확인합니다.
4. `release`에 직접 Commit하지 않습니다.
5. 새 Branch를 생성하는 옵션을 선택합니다.
6. Issue 번호가 포함된 Branch 이름을 사용합니다.
7. Pull Request를 생성합니다.

문서 이동, 대량 변경, 다른 Unity 에셋과 연결된 변경은 GitHub Desktop을 사용합니다.

### 17.3 CSV 또는 Unity 데이터 수정

```text
Issue 확인
-> GitHub Desktop에서 Branch 생성
-> Excel 또는 Unity에서 데이터 수정
-> 데이터 검증기 실행
-> Play Mode 확인
-> Commit과 Push
-> PR 생성
-> 기획 및 개발 리뷰
```

CSV 파일명, 헤더, ID 규칙은 각 시스템의 작성 가이드를 따릅니다.

### 17.4 코드 작업

```text
Issue 기술 범위 확인
-> Branch 생성
-> 코드와 테스트 수정
-> 로컬 검증
-> Commit과 Push
-> PR에 기술 영향 기록
-> 개발 리뷰
-> 기획 결과 확인
```

## 18. 버전과 Release 관리

버전 완료 기록은 작업자 이름이 포함된 장기 Branch가 아니라 Git Tag로 남깁니다.

```text
v0.0.1
v0.1.0
v1.0.0
```

기본 기준:

- 버그 수정만 포함: 마지막 숫자 증가
- 호환 가능한 기능 추가: 가운데 숫자 증가
- 큰 구조 변화 또는 정식 출시: 첫 번째 숫자 증가

Tag 생성과 Release 작성은 저장소 관리자가 담당합니다. 버전별 작업 목록은 Project의 목표 버전 또는 Milestone으로 관리합니다.

## 19. 하지 말아야 할 작업

- `release`에서 직접 작업하고 Push하기
- 다른 사람의 Branch를 허락 없이 삭제하기
- `force push`로 공동 변경 기록 덮어쓰기
- Conflict 내용을 이해하지 못한 채 임의 병합하기
- `Library`, `Temp`, `Logs`, 개인 IDE 설정 파일 Commit하기
- Unity 에셋만 Commit하고 `.meta` 누락하기
- 같은 Scene 또는 Prefab을 여러 명이 동시에 수정하기
- 관련 없는 기능, 문서, 포맷 변경을 하나의 PR에 섞기
- 리뷰 없이 자신의 PR을 바로 병합하기
- 채팅에서만 요구사항을 변경하고 Issue를 갱신하지 않기
- 동작 확인 없이 `완료`로 변경하기

## 20. 자주 발생하는 상황

### 변경 파일이 너무 많이 보이는 경우

Commit하지 않습니다. Unity 버전 차이, 자동 생성 파일, 잘못된 Branch 또는 기존 작업의 변경일 수 있습니다. 변경 파일 목록을 캡처해서 관리자에게 전달합니다.

### Commit했지만 GitHub에서 보이지 않는 경우

`Push origin`을 했는지 확인합니다. Commit은 내 컴퓨터에 저장하고, Push는 GitHub에 업로드합니다.

### 다른 사람의 변경이 보이지 않는 경우

`Fetch origin` 후 기준 Branch에서 `Pull origin`을 실행합니다. 작업 중인 Branch에 임의로 다른 Branch를 Merge하지 않습니다.

### 잘못해서 `release`에서 수정한 경우

Commit하거나 변경을 폐기하지 말고 작업을 멈춥니다. 관리자에게 알린 뒤 변경 내용을 새 Branch로 안전하게 옮깁니다.

### PR에 Conflict가 표시되는 경우

GitHub의 자동 해결 버튼을 바로 누르지 않습니다. 충돌 파일과 담당자를 확인하고 관리자와 함께 처리합니다.

### PR을 만든 뒤 추가 수정이 필요한 경우

기존 작업 Branch에서 수정하고 Commit과 Push를 수행합니다. 새 PR을 만들 필요 없이 기존 PR에 자동으로 추가됩니다.

## 21. 작업자 완료 체크리스트

- 작업을 설명하는 Issue가 있는가?
- Issue에 목적과 확인 가능한 완료 조건이 있는가?
- 올바른 작업 Branch에서 수정했는가?
- 관련 없는 변경 파일이 포함되지 않았는가?
- Commit 제목이 변경 내용을 설명하는가?
- 모든 Commit을 Push했는가?
- PR의 `base`가 `release`인가?
- PR에 `Closes #Issue번호`를 작성했는가?
- 변경 내용과 확인 방법을 작성했는가?
- 필요한 스크린샷 또는 영상을 첨부했는가?
- Unity Console 오류가 0개인가?
- 관련 기능과 기존 기능을 직접 확인했는가?
- 새 Unity 에셋과 `.meta`가 함께 포함되었는가?

## 22. 리뷰어 완료 체크리스트

- Issue의 목적과 PR 결과가 일치하는가?
- 모든 완료 조건을 확인했는가?
- 관련 없는 파일이 포함되지 않았는가?
- Scene, Prefab, Project Settings 충돌 위험을 확인했는가?
- 기획 데이터와 화면 결과가 의도와 일치하는가?
- 테스트 방법을 따라 할 수 있는가?
- 남아 있는 질문과 수정 요청이 해결되었는가?
- 병합 후 Issue와 Project 상태가 정리되는가?

## 23. 빠른 요약

```text
1. Issue를 만든다.
2. 완료 조건을 적는다.
3. Ready가 되면 담당자를 정한다.
4. release에서 작업 Branch를 만든다.
5. 작업 후 변경 파일을 확인한다.
6. Commit하고 Push한다.
7. release를 대상으로 PR을 만든다.
8. 다른 사람이 결과를 확인한다.
9. Squash and merge한다.
10. Issue와 Project를 Done으로 정리한다.
```

모르겠거나 이상한 파일이 보일 때는 Commit, Push, Merge를 서두르지 않는 것이 가장 안전합니다.
