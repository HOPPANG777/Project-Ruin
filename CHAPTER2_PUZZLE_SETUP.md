# 챕터2 퍼즐 배치·설정 안내

`Assets/_Game/Prefabs/Map/Grid_2.prefab`에 PDF(`퍼즐 렙디_ver2.pdf` 챕터2)의 퍼즐 16개를 배치했다. **위치는 임시**이고, 팀원이 지형을 꾸밀 때 그룹째 옮기면 된다. 2026-11-28 기준.

## 어디에 있나

Prefab Mode에서 아래 그룹을 선택하고 Frame Selected(`F`)를 누른다. 좌표는 **부모 타일맵의 로컬 좌표(= 타일 셀 좌표)**다.

| 그룹 | 부모 타일맵 | 내용 |
| --- | --- | --- |
| `Ch2_Puzzles_Village` | `Tilemap2` | 마을 퍼즐 1~9 |
| `Ch2_Puzzles_Ruin1F` | `Chapter1_ruin` | 유적 1층 퍼즐 1~6 |
| `Ch2_Puzzles_Ruin2F` | `Chapter1_ruin2f` | 유적 2층 퍼즐 7 |

(`Chapter1_ruin`, `Chapter1_ruin2f`는 이름만 챕터1 복사본이고 모양은 PDF 챕터2 유적 1·2층과 일치한다.)

각 퍼즐은 `V01_물줄기`, `R03_발판`처럼 **PDF 번호_종류** 폴더 하나로 묶여 있다. 임시 표시 색: 하늘색=조작대, **빨강=문(충돌 있음)**, 초록 마름모=보상 위치, 파랑 삼각=시작 지점, 흰 반투명 사각=퍼즐 영역 가이드(충돌 없음).

## 16개 목록

| 번호 | 종류 | 데이터 ID | 저장 ID | 클리어하면 |
| --- | --- | --- | --- | --- |
| 마을 V01 | 물줄기 | PIPE_STAGE_08 | CH2_VILLAGE_V01 | 자기 문 열림 |
| V02 | 발판(돌) | — | CH2_VILLAGE_V02_MAZE | 미로 문 열림 |
| V03 | 등불 | LIGHT_STAGE_02 | CH2_VILLAGE_V03 | 자기 문 |
| V04 | 등불 | LIGHT_STAGE_03 | CH2_VILLAGE_V04 | 자기 문 |
| V05 | 발판(돌) | — | CH2_VILLAGE_V05_MAZE | 미로 문 |
| V06 | 물줄기 | PIPE_STAGE_09 | CH2_VILLAGE_V06 | 자기 문 |
| V07 | 물줄기 | PIPE_STAGE_10 | CH2_VILLAGE_V07 | 자기 문 |
| V08 | 발판(돌) | — | CH2_VILLAGE_V08_MAZE | 미로 문 |
| V09 | 원형판 | CIRCLE_STAGE_03 | CH2_VILLAGE_V09 | 자기 문 |
| 유적1층 R01 | 원형판 | CIRCLE_STAGE_04 | CH2_RUIN_1F_R01 | 중앙 방 오른쪽 문 |
| R02 | 등불 | LIGHT_STAGE_04 | CH2_RUIN_1F_R02 | 왼쪽 통로 문 |
| R03 | 발판(돌) | — | CH2_RUIN_1F_R03_MAZE | 미로 문 |
| R04 | 물줄기 | PIPE_STAGE_11 | CH2_RUIN_1F_R04 | 위쪽 방 연결 문 |
| R05 | 빛잇기 | — | CH2_RUIN_1F_R05_BEAM | 위쪽 방 북쪽 통로 문 |
| R06 | 원형판 | CIRCLE_STAGE_05 | CH2_RUIN_1F_R06 | 계단 쪽 문 |
| 유적2층 R07 | 빛잇기 | — | CH2_RUIN_2F_R07_BEAM | 보상 방 문 |

저장 스테이지 번호는 `Ch2Util.StageNumber = 4`이고, SceneCatalog의 Chapter2 스테이지 번호(4)와 같다(테스트로 확인). 저장 ID는 한번 정하면 바꾸지 않는다.

## Chapter2 씬

`Assets/_Game/Scenes/Chapter/Chapter2.unity`: Chapter1 씬과 같은 스테이지 씬 구성(Main Camera + `Grid_2` 인스턴스 + 도착 지점)이다. GameRoot/플레이어는 넣지 않는다(스테이지 전환 때 이전 씬에서 넘어옴). SceneCatalog에 `Chapter2`(STAGE, 번호 4)로 등록했고 빌드 설정에도 추가했다.

- 도착 지점 `마을 입구 (챕터1에서 도착)`, ID `CH2_VILLAGE_ENTRANCE`: 마을 서쪽 입구 통로 중앙(주변 3×3 칸이 실제 바닥임을 테스트로 확인).
- **챕터1에서 이 씬으로 넘어오는 이동 발판은 아직 없다.** 챕터1의 `SceneTransitionPad`에 목적지 맵 = Chapter2, 도착 지점 = 위 ID를 지정하면 된다.
- 씬은 `Grid_2` 프리팹 인스턴스라서 프리팹 수정이 그대로 반영된다. 메뉴 `Tools > Project Ruin > Chapter 2 > Chapter2 씬 만들기`는 Chapter1 씬을 복제해 다시 만들므로, 씬에 직접 추가한 것이 있으면 다시 실행하지 않는다.

## PDF와 다르게 한 곳 (반드시 확인)

1. **물줄기 V01, R04는 PDF 그대로면 풀 수 없어서 최소 수정했다.** 슬라이드+회전 전수 탐색으로 확인했다(기존 PIPE_STAGE_04의 문서화된 6회 해법은 같은 솔버로 재현됨).
   - V01(PIPE_STAGE_08): (0,1) 빈 칸 → 곡선, (2,2) 직선 → 빈 칸. 한 번 옮겨야 풀림(6회).
   - R04(PIPE_STAGE_11): (2,2) 직선 → 곡선(7회).
2. **물줄기의 "원 달린 2~3방향 조각"은 기존 코드에 없는 종류다.** (START/END는 출구 1개뿐.) 한 방향만 남겨 START/END로 옮겼다. V06(09) 8회, V07(10) 4회, V01 6회, R04 7회로 풀린다. PDF의 의도가 다르면 기획 확인이 필요하다.
3. **등불**: 노란색 = 켜짐, 전부 켜면 클리어, 누르면 상하좌우+자기 자신 토글(챕터1 규칙)로 가정했다. 최소 클릭 V03 2회, V04 5회, R02 6회.
4. **원형판 정답**: R01 쌍둥이·처녀·물고기, R06 사자·저울·염소·사수자리(바깥→안쪽 순서로 가정). **마을 V09는 PDF에 정답이 비어 있어** `PLACEHOLDER_*` ID를 넣었다. 문양 그림과 ID 대응은 챕터1 a 퍼즐처럼 `Assets/_Game/Data/Chapter2/*_Ring*.prefab`의 CirclePattern에서 기획자가 맞춘다(첫 정답 문양 외에는 `DRAFT_...`). 편집 방법은 `CHAPTER1_RUIN_SETUP.md`의 "a 별자리 편집"과 같다.
5. **빛잇기(R05, R07)**: PDF의 "조각상을 옮겨 연결" 기믹은 **기존 Beam이 지원하지 않는다**(기둥은 회전만, 돌/벽만 이동·파괴). 그래서 옮길 위치를 포함해 모든 조각상을 회전 기둥으로 배치했고, 중간 기둥을 한 번씩 누르면 풀린다. 움직이는 조각상 시작 위치는 `[참고_기믹미지원]움직이는조각상_N`(주황)으로만 표시했다. 기둥 연결 순서는 PDF의 노란 선이 아니라 "다른 기둥에 가리지 않는 최단 경로"로 자동 계산했다. 안 돌린 기둥은 45° 틀어져 쏘기 때문에 우연히 끝 기둥을 맞히는 지름길이 생길 수 있어, 눌렀는지/안 눌렀는지 모든 조합을 시뮬레이션해 "전부 눌러야만 클리어"되는 경로만 골랐다(R05는 이 검사로 경로를 바꿨다). 기믹을 구현할지 정해야 한다.
6. **발판(V02, V05, V08, R03)**: 컨트롤러·발판·돌·문·보상 위치·시작 지점·초기화 발판을 PDF 비율로 배치했다. **미로 벽은 만들지 않았다**(챕터1처럼 PD 지형 작업). 돌 수만큼 가장 가까운 발판이 돌 발판, 나머지는 플레이어 발판이다. R03은 방(약 21×16)에 맞춰 축소해서 PDF 미로를 플레이 가능한 크기로 담지 못한다. 방을 키우거나 크기를 조정해야 한다.
7. **문·막힌 길**: PDF에 문(빨강)이 표시된 곳 외에는 제가 "퍼즐 옆 통로"에 정했다. 연결을 바꾸려면 `UiPuzzleSceneState`의 Closed Door(UI 퍼즐), 컨트롤러의 Unsolved/Solved Result Object(발판), Beam 관리자의 Exit Obstacles(빛잇기)를 바꾼다.
8. **마을 위치는 임시**다. Tilemap2가 PDF 마을 모양이 아니라, PDF 개략도를 0.25배로 큰 평지 위에 펼쳤다. 지형이 완성되면 `Ch2_Puzzles_Village`의 각 폴더만 옮기면 된다.

## 다시 만들기 / 주의

메뉴 `Tools > Project Ruin > Chapter 2 > 퍼즐 배치 생성 (Grid_2)`는 `Ch2_Puzzles*` 그룹을 **지우고 처음부터 다시 만든다.** 팀원이 그룹 안을 손봤다면 다시 실행하지 않는다. 배치 모드는 `-executeMethod Chapter2LayoutBuilder.BuildBatch`.

## 검증

Chapter2 테스트 19개 모두 통과.

- `Chapter2LayoutTests` 6개: CSV ID·종류 일치, 저장 ID 16개 고유, 문 연결, 원형판 별자리 설정의 런타임 검증, 광선 경로 기하, 미로 컨트롤러 등록.
- `Chapter2PuzzleSolutionTests` 10개: 실제 UI 매니저로 새 파이프 4개(드래그·회전)·등불 3개(클릭)·원형판 3개(정답 문양 12시 정렬·스냅)가 처음엔 안 풀려 있다가 풀린다.
- `Chapter2SceneTests` 3개: SceneCatalog 번호 4·빌드 설정 등록, 씬 구성과 도착 지점이 실제 바닥 위, 광선·미로 컨트롤러의 자체 설정 검증 통과, **실제 Physics2D 레이캐스트로 광선 2개가 중간 기둥을 한 번씩 눌러야만 클리어**.
- 전체 EditMode 중 35개 실패는 챕터1 Grid_1·Codex UI·챕터0 등 이번에 수정하지 않은 영역이다. 가장 큰 원인(9개)은 Grid_1의 `c_...` 발판 프리팹에 내장된 컨트롤러가 ObjectId 없이 오류를 내는 것으로, 챕터2는 이를 제거해 만들었다. 챕터1 마을 Point1/Point3 통행 실패는 `CHAPTER1_VILLAGE_SETUP.md`에 이미 기록돼 있다. 변경 전 기준선과의 정식 비교는 하지 못했다.
- 사람이 Unity에서 직접 플레이하거나 화면으로 확인하지 않았다. 발판·문·플레이어 이동·화면 연출은 자동 테스트로 검증하지 못했다.

## 남은 문제

1. **Grid_2 타일맵에 충돌체가 없다.** `Tilemap2`·`Chapter1_ruin`·`Chapter1_ruin2f`에 TilemapCollider2D가 없어 플레이어가 문(충돌 있음)을 돌아 지나가거나 맵 밖으로 나갈 수 있다. 챕터1 `town`/`Chapter1_ruin`에는 있다. 지형 담당이 벽 타일 충돌을 설정해야 문·미로가 의미를 가진다.
2. 챕터1 → 챕터2 이동 발판이 없다(위 "Chapter2 씬").
3. 발판(돌 밀기) 미로는 벽이 없고, 컨트롤러는 즉시 저장 연결(`WorldPuzzleSceneState`)을 지원하지 않아 클리어가 다음 씬 캡처/저장 때 기록된다(UI 퍼즐·광선은 클리어 즉시 저장).
4. 마을 V09 정답, 빛잇기 조각상 이동 기믹, 물줄기 원 조각 해석 등 위 "PDF와 다르게 한 곳"의 기획 확인 사항.
5. 모든 임시 표시(색 사각형)와 마을 좌표는 교체 대상이다.

## 변경 파일

- 수정: `Assets/_Game/Prefabs/Map/Grid_2.prefab`, `Data/Csv/puzzles.csv`, `puzzle_pipe_tiles.csv`, `puzzle_light_tiles.csv`, `puzzle_circle_rings.csv`(행 추가만)
- 신규: `Assets/_Game/Data/Chapter2/`(원형판 링 프리팹 11개 + 설정 SO 3개), `Assets/_Game/Sprites/Chapter2Layout/Ch2_Marker.png`, `Scripts/Editor/Chapter2/`(빌더 6개), `Scripts/Tests/Editor/Chapter2*Tests.cs`
