# 2026-07-18 작업일지 — Inventory ScriptableObject 데이터의 CSV 전환

## 1. 작업 정보

| 항목 | 내용 |
| --- | --- |
| 작업일 | 2026-07-18 |
| 작성일 | 2026-07-19 |
| 작업 주제 | Inventory 아이템 원본 데이터를 ScriptableObject에서 CSV 기반 구조로 전환 |
| 작업 목적 | Dialogue·Quest와 같은 방식으로 비개발 직군도 표 형식으로 아이템을 작성하고 검증할 수 있는 제작 흐름 마련 |
| 주요 데이터 | `Assets/_Game/Data/Csv/items.csv` |
| 주요 Unity 에셋 | `Assets/_Game/Data/InventoryData/ItemVisualCatalog.asset` |
| 주요 코드 | `ItemDefinition`, `ItemCsvDefinitionLoader`, `GameDataRepository`, `InventoryController` |
| 주요 문서 | `Assets/_Game/Docs/INVENTORY_SYSTEM_AUTHORING_GUIDE.md` |
| 작업 상태 | 구현·데이터 이관·C# 빌드·CSV 스모크 검증 완료 |
| Commit | 아직 생성하지 않음 |

## 2. 작업 요청과 배경

기존 Inventory 시스템은 아이템마다 `ItemDataSO` 에셋을 만들고, 해당 에셋을 `InventoryController`의 Item Database 목록에 직접 등록하는 방식이었습니다.

기존 제작 흐름:

```text
ItemDataSO 에셋 생성
-> Item Id, 이름, 설명, 가격, 타입, 중첩 수 입력
-> Sprite 아이콘 연결
-> GameRoot의 InventoryController Item Database에 수동 등록
-> Quest CSV에는 ItemDataSO.ItemId 입력
```

이 방식은 다음과 같은 문제가 있었습니다.

- 새 아이템마다 Unity ScriptableObject 에셋을 별도로 생성해야 함
- 아이템 에셋을 만들고도 `InventoryController` 목록 등록을 빠뜨릴 수 있음
- 이름, 설명, 가격처럼 기획자가 자주 바꾸는 데이터를 Unity Inspector에서 관리해야 함
- Dialogue·Quest는 CSV인데 Inventory만 별도 제작 흐름을 사용함
- 퀘스트 CSV의 `item_id`와 Unity 에셋의 `ItemId` 연결 상태를 사람이 직접 관리해야 함
- 개발 경험이 없는 기획자가 전체 등록 절차를 이해하기 어려움

따라서 Inventory의 기획 데이터를 Dialogue·Quest와 같은 CSV 원본으로 통합하고, Unity 오브젝트 참조가 꼭 필요한 Sprite만 별도 카탈로그에 남기는 방향으로 전환했습니다.

## 3. 최종 구조

전환 후 데이터 흐름은 다음과 같습니다.

```text
items.csv
-> ItemCsvDefinitionLoader
-> ItemDefinition 목록
-> GameDataRepository의 item_id 인덱스
-> InventoryController
-> InventoryPresenter / InventoryView
```

화면 표시 데이터는 다음 세 원본을 조합합니다.

```text
items.csv
├─ item_id, price, item_type, max_stack_count
├─ display_name_key ───────┐
├─ description_text_key ───┴─ localization.csv
└─ icon_id ------------------- ItemVisualCatalog.asset -> Unity Sprite
```

역할별 저장 위치:

| 데이터 종류 | 저장 위치 |
| --- | --- |
| 아이템 고유 ID | `items.csv` |
| 이름과 설명 Key | `items.csv` |
| 실제 다국어 이름과 설명 | `localization.csv` |
| 가격, 타입, 최대 중첩 수 | `items.csv` |
| 아이콘 연결 ID | `items.csv` |
| 실제 Unity Sprite 참조 | `ItemVisualCatalog.asset` |
| 플레이어 보유 수량과 new 상태 | `InventorySlotData` 런타임·세이브 데이터 |
| 퀘스트 지급과 소모 | `quest_rewards.csv`, `quest_consume_items.csv` |

## 4. 설계 결정

### 4.1 기획 데이터는 CSV로 이동

다음 항목을 기존 `ItemDataSO`에서 `items.csv`로 이동했습니다.

- Item ID
- 표시 이름 Key
- 설명 Key
- 아이콘 ID
- 가격
- Item Type
- 최대 중첩 수

이를 통해 새 아이템 추가 시 개별 ScriptableObject를 만들거나 GameRoot 목록에 등록할 필요가 없어졌습니다.

### 4.2 Sprite 참조만 ScriptableObject 카탈로그로 유지

CSV 파일은 Unity `Sprite`를 직접 참조할 수 없습니다. CSV에 Unity 경로나 GUID를 직접 작성하면 파일 이동이나 협업 과정에서 참조가 불안정해질 수 있습니다.

따라서 Dialogue의 `DialogueVisualCatalogSO`와 같은 방식으로 `ItemVisualCatalogSO`를 추가했습니다.

```text
items.csv의 icon_id
-> ItemVisualCatalogSO의 Icon Id
-> Unity Sprite
```

아이콘 파일을 교체해도 `icon_id`를 유지하면 CSV와 Quest 데이터를 수정할 필요가 없습니다.

### 4.3 이름과 설명은 Localization Key 사용

기존 `ItemDataSO`에는 한국어 이름과 설명 문자열이 직접 들어 있었습니다.

전환 후에는 다음 Key를 사용합니다.

```text
display_name_key
description_text_key
```

실제 한국어, 영어, 일본어 문장은 `localization.csv`에 저장합니다. 이 방식으로 Dialogue, Quest, Inventory가 같은 다국어 처리 규칙을 사용하게 됐습니다.

### 4.4 세이브 데이터는 계속 item_id 중심으로 유지

`InventorySlotData`는 이전부터 `ItemDataSO` 참조가 아니라 `ItemId`와 수량을 저장하고 있었습니다.

이번 전환에서도 다음 원칙을 유지했습니다.

```text
세이브 데이터
-> item_id로 ItemDefinition 재조회
-> Localization 문장과 Sprite 결합
-> 인벤토리 UI 표시
```

CSV 행 순서를 바꾸더라도 `item_id`가 유지되면 아이템 조회가 가능합니다.

## 5. items.csv 추가

새로운 필수 CSV를 추가했습니다.

파일:

```text
Assets/_Game/Data/Csv/items.csv
```

확정 헤더:

```csv
item_id,display_name_key,description_text_key,icon_id,price,item_type,max_stack_count
```

각 열의 규칙:

| 열 | 규칙 |
| --- | --- |
| `item_id` | 영문 대문자로 시작하며 대문자, 숫자, 밑줄만 허용 |
| `display_name_key` | `localization.csv`에 존재하는 필수 Key |
| `description_text_key` | `localization.csv`에 존재하는 필수 Key |
| `icon_id` | 선택값. 입력 시 대문자 시스템 ID 규칙 사용 |
| `price` | 0 이상의 정수 |
| `item_type` | 코드에 정의된 `ItemType` 이름과 정확히 일치 |
| `max_stack_count` | 1 이상의 정수 |

지원 Item Type:

```text
ITEM_QUEST
ITEM_ALCOHOL_COLLECTIBLE
ITEM_ALCOHOL_TOOL
ITEM_RELIC_COLLECTIBLE
ITEM_RELIC_TOOL
ITEM_MISC
```

현재 이관된 데이터:

```csv
ITEM_COIN,item.coin.name,item.coin.description,,0,ITEM_MISC,999
ITEM_REPAIR_KIT,item.repair_kit.name,item.repair_kit.description,,0,ITEM_QUEST,99
```

기존 두 아이템에는 연결된 Sprite가 없었으므로 `icon_id`는 빈 셀로 이관했습니다.

## 6. Localization 데이터 이관

아이템 이름과 설명을 `localization.csv`에 추가했습니다.

추가 Key:

```csv
item.coin.name,코인,Coin,
item.coin.description,퀘스트 보상으로 획득하는 코인입니다.,A coin received as a quest reward.,
item.repair_kit.name,복원 도구,Restoration Tool,
item.repair_kit.description,손상된 유물을 복원할 때 사용하는 퀘스트 도구입니다.,A quest tool used to restore damaged artifacts.,
```

Repository 생성 시 이름 또는 설명 Key가 `localization.csv`에 없으면 초기화를 실패하도록 검증을 추가했습니다.

## 7. 순수 C# 아이템 Definition 추가

### 7.1 ItemDefinition

파일:

```text
Assets/_Game/Scripts/Gameplay/Inventory/Data/ItemDefinition.cs
```

역할:

- `items.csv` 한 행을 런타임 읽기 전용 데이터로 표현
- Unity `ScriptableObject`나 `MonoBehaviour`를 상속하지 않는 순수 C# 데이터
- Item ID, Localization Key, Icon ID, 가격, 타입, 중첩 수 제공

주요 프로퍼티:

```text
ItemId
DisplayNameKey
DescriptionTextKey
IconId
Price
ItemType
MaxStackCount
```

### 7.2 InventoryItemDisplayData

파일:

```text
Assets/_Game/Scripts/Gameplay/Inventory/Data/InventoryItemDisplayData.cs
```

역할:

- `ItemDefinition`에 현재 언어의 이름과 설명을 결합
- `ItemVisualCatalogSO`에서 찾은 Sprite를 결합
- Inventory UI에 필요한 표시 데이터를 한 객체로 전달

구조:

```text
ItemDefinition
+ 현재 언어 DisplayName
+ 현재 언어 Description
+ Unity Sprite Icon
= InventoryItemDisplayData
```

## 8. CSV 로더 추가

파일:

```text
Assets/_Game/Scripts/Gameplay/Data/Csv/ItemCsvDefinitionLoader.cs
```

로더가 수행하는 검증:

- 필수 `items.csv` 존재 여부
- 헤더 개수, 이름, 순서
- `item_id` 형식과 중복
- 이름·설명 Localization Key 형식
- 선택 `icon_id` 형식
- `price`가 0 이상의 정수인지 확인
- `item_type`이 실제 `ItemType` enum에 존재하는지 확인
- `max_stack_count`가 1 이상의 정수인지 확인

문제가 있는 첫 번째 CSV 행의 원본 이름, 레코드, 줄, 열과 오류 메시지를 `GameDataCsvLoadError`로 반환합니다.

## 9. 전체 CSV 초기화 흐름 연결

### 9.1 GameDataCsvSource

전체 CSV 원본 묶음에 `Items` 항목을 추가했습니다.

생성자 호출 순서에도 `items.csv`를 포함하도록 런타임, 에디터 검증기, 회귀 테스트를 모두 갱신했습니다.

### 9.2 GameDataController

Inspector 필드 추가:

```text
Inventory CSV
-> Items Csv
```

`items.csv`를 필수 `TextAsset`으로 검증하고 `GameDataCsvSource`에 전달하도록 수정했습니다.

### 9.3 GameDataCsvLoader

전체 게임 데이터 초기화 시 다음 순서로 Item Definition을 생성하도록 연결했습니다.

```text
Localization CSV 로드
-> Items CSV 로드
-> Quest CSV 로드
-> Dialogue CSV 로드
-> Repository 생성과 교차 참조 검증
```

## 10. GameDataRepository 확장

`GameDataRepository`와 `IGameDataRepository`에 Item Definition 저장과 조회 기능을 추가했습니다.

추가 API:

```csharp
int ItemCount { get; }
bool TryGetItem(string itemId, out ItemDefinition itemDefinition);
```

내부 인덱스:

```text
Dictionary<string, ItemDefinition> _itemById
```

Repository 생성 시 추가 검증:

- Item Definition이 null이 아닌지 확인
- Item ID가 비어 있지 않은지 확인
- 필수 Localization Key 확인
- 가격, Item Type, 최대 중첩 수 확인
- 중복 Item ID 확인
- 이름과 설명 Key가 실제 Localization Repository에 존재하는지 확인
- Quest 보상 `item_id`가 Item Repository에 존재하는지 확인
- Quest 소모 `item_id`가 Item Repository에 존재하는지 확인

이전에는 Quest의 아이템 참조를 Unity `ItemDataSO` 목록과 에디터 검증 단계에서 비교했습니다. 전환 후에는 Repository 자체가 CSV 간 교차 참조를 검증하므로 런타임 초기화에서도 잘못된 참조를 차단합니다.

## 11. InventoryController 전환

### 11.1 제거한 구조

기존 직렬화 필드와 런타임 캐시를 제거했습니다.

```csharp
List<ItemDataSO> _itemDatabase
Dictionary<string, ItemDataSO> _itemById
```

따라서 더 이상 GameRoot Inspector의 Item Database 목록에 개별 아이템 에셋을 등록하지 않습니다.

### 11.2 초기화 의존성 변경

기존:

```csharp
InventoryController.Init()
```

변경:

```csharp
InventoryController.Init(
    IGameDataRepository gameDataRepository,
    ILocalizationService localizationService)
```

`GameRoot`가 CSV 저장소와 Localization 서비스를 InventoryController에 주입하도록 수정했습니다.

### 11.3 공개 API 변경

아이템 지급 API:

```csharp
// 이전
TryAddItem(ItemDataSO itemData, int amount)

// 변경
TryAddItem(string itemId, int amount)
```

아이템 조회 API:

```csharp
// 이전
TryGetItem(string itemId, out ItemDataSO itemData)

// 변경
TryGetItem(string itemId, out ItemDefinition itemData)
```

UI 표시 데이터 조회 API 추가:

```csharp
TryGetItemDisplay(
    string itemId,
    out InventoryItemDisplayData displayData)
```

아이템 지급 코드가 ScriptableObject 참조 대신 안정적인 문자열 ID만 사용하게 됐습니다.

```csharp
GameRoot.Instance.Inventory.TryAddItem("ITEM_COIN", 100);
```

### 11.4 기존 Inventory 동작 유지

CSV 전환 후에도 다음 동작은 그대로 유지했습니다.

- 동일 Item ID 슬롯 중첩
- 최대 중첩 수 초과 시 새 슬롯 생성
- 최초 획득 new 표시
- 아이템 자동 소모
- 카테고리 필터
- 도구 우선 정렬
- 최근 획득 정렬
- 플레이어 수동 슬롯 이동
- 슬롯 저장 데이터의 Item ID 중심 구조

## 12. Inventory UI 전환

다음 UI 코드의 데이터 타입을 `ItemDataSO`에서 `InventoryItemDisplayData`로 변경했습니다.

- `InventoryPresenter`
- `InventoryView`
- `InventorySlotView`

UI 흐름:

```text
InventorySlotData.ItemId
-> IInventoryService.TryGetItemDisplay()
-> ItemDefinition 조회
-> Localization 문장 조회
-> ItemVisualCatalog Sprite 조회
-> InventoryItemDisplayData
-> 슬롯 아이콘과 상세 화면 표시
```

이름과 설명은 조회 시 현재 선택된 언어에 맞게 결정됩니다.

## 13. QuestController 연동 변경

기존 Quest 보상 지급은 Item ID로 `ItemDataSO`를 찾은 뒤 다시 해당 객체를 Inventory에 전달했습니다.

기존 흐름:

```text
reward.ItemId
-> Inventory.TryGetItem()
-> ItemDataSO
-> Inventory.TryAddItem(ItemDataSO, amount)
```

변경 흐름:

```text
reward.ItemId
-> Inventory.TryAddItem(itemId, amount)
-> Repository에서 ItemDefinition 검증
-> 지급
```

수정 결과:

- QuestController가 Unity 아이템 에셋 타입을 알 필요가 없어짐
- Quest CSV의 Item ID가 존재하지 않으면 Repository 생성 단계에서 차단
- 보상 지급 코드가 단순해짐

## 14. ItemVisualCatalogSO 추가

스크립트:

```text
Assets/_Game/Scripts/Gameplay/Inventory/ItemVisualCatalogSO.cs
```

에셋:

```text
Assets/_Game/Data/InventoryData/ItemVisualCatalog.asset
```

구성 항목:

```text
Icon Id
Sprite
```

런타임 동작:

- 최초 조회 시 목록을 Dictionary 캐시로 변환
- 빈 ID 또는 빈 Sprite는 조회 대상에서 제외
- 중복 ID는 경고 후 뒤쪽 항목을 건너뜀
- `items.csv`의 빈 `icon_id`는 아이콘 없는 정상 아이템으로 처리

기존 Coin과 RepairKit 에셋 모두 Sprite가 비어 있었으므로 현재 카탈로그는 빈 목록으로 생성했습니다.

## 15. GameRoot 프리팹 마이그레이션

`Assets/_Game/Prefabs/GameRoot.prefab`을 수정했습니다.

`GameDataController` 변경:

```text
_itemsCsv
-> Assets/_Game/Data/Csv/items.csv 연결
```

`InventoryController` 변경:

```text
기존 _itemDatabase 목록 제거
-> _visualCatalog
-> ItemVisualCatalog.asset 연결
```

`GameRoot.Start()` 초기화 변경:

```text
GameDataController가 Repository 생성
-> InventoryController.Init(GameData, Localization)
-> InventoryPresenter 초기화
-> QuestController 초기화
```

## 16. 레거시 데이터 제거

CSV 이관 완료 후 다음 레거시 파일을 제거했습니다.

```text
Assets/_Game/Scripts/Gameplay/Inventory/Data/ItemDataSO.cs
Assets/_Game/Data/InventoryData/Coin.asset
Assets/_Game/Data/InventoryData/RepairKit.asset
```

삭제 이유:

- 동일 데이터를 SO와 CSV 두 곳에서 중복 관리하는 상황 방지
- 신규 팀원이 이전 방식으로 아이템 에셋을 만드는 실수 방지
- 런타임 타입과 Editor 검증기에서 `ItemDataSO` 의존성 완전 제거

필요한 경우 삭제 파일은 Git 기록에서 복구할 수 있습니다.

## 17. Editor 데이터 검증기 변경

`GameDataEditorValidator`를 CSV Item Definition 구조에 맞게 변경했습니다.

### 제거한 검증

```text
InventoryController._itemDatabase 읽기
-> ItemDataSO null 검사
-> ItemDataSO ItemId 중복 검사
-> ItemDataSO Icon 검사
```

### 추가한 검증

```text
GameDataController._itemsCsv 연결 확인
-> ItemCsvDefinitionLoader로 items.csv 읽기
-> ItemVisualCatalogSO 연결 확인
-> 카탈로그의 빈 ID, 중복 ID, 빈 Sprite 확인
-> items.csv icon_id와 카탈로그 연결 확인
```

Quest Item ID의 존재 여부는 `GameDataRepository`의 교차 참조 검증으로 이동했습니다.

자동 검증 대상 폴더가 기존부터 `Assets/_Game/Data/Csv/` 전체를 감시하고 있어 `items.csv`도 자동 검증 범위에 포함됩니다.

## 18. 회귀 테스트 변경

`GameDataCsvRegressionTests`를 Inventory CSV 전환에 맞게 갱신했습니다.

변경 내용:

- 실제 런타임 CSV 묶음을 12개에서 13개로 변경
- `items.csv` 필수 로드 추가
- Repository의 `ItemCount`가 1개 이상인지 확인
- 필수 `items.csv` 누락 시 로드 실패 확인
- 레거시 타입 목록에 `ItemDataSO` 추가
- 레거시 에셋 목록에 `Coin.asset`, `RepairKit.asset` 추가
- 모든 `GameDataCsvSource` 복사 헬퍼에 Items 원본 반영

테스트가 방지하는 회귀:

- `items.csv` 연결이 빠진 상태
- Item CSV 로더가 전체 게임 데이터 흐름에서 제외된 상태
- 제거한 `ItemDataSO`가 다시 추가된 상태
- 제거한 개별 아이템 SO 에셋이 다시 포함된 상태

## 19. 기획 문서 갱신

### 19.1 Dialogue·Quest CSV 작성 가이드

파일:

```text
Assets/_Game/Docs/DIALOGUE_QUEST_AUTHORING_GUIDE.md
```

변경 내용:

- Dialogue, Quest뿐 아니라 Inventory도 CSV 원본을 사용한다고 명시
- `ItemDataSO`가 제거됐음을 명시
- Unity Sprite 연결 데이터로 `ItemVisualCatalogSO` 추가
- Quest `item_id`의 기준을 InventoryController 목록에서 `items.csv`로 변경
- 새 Quest 아이템 추가 절차를 CSV와 Localization 중심으로 변경
- 완료 체크리스트의 Item Database 표현을 `items.csv` 존재 여부로 변경

### 19.2 Inventory CSV 작성 가이드

파일:

```text
Assets/_Game/Docs/INVENTORY_SYSTEM_AUTHORING_GUIDE.md
```

개발 경험이 없는 기획자와 팀원이 처음부터 따라 할 수 있도록 기존 문서를 전면 재작성했습니다.

최종 문서 분량:

```text
약 917줄
17개 주요 장
```

포함 내용:

- CSV, 헤더, ID, Localization 기초 설명
- 가장 짧은 신규 아이템 작업 순서
- `items.csv` 모든 열의 의미와 규칙
- 좋은 ID와 잘못된 ID 예시
- 여섯 Item Type과 인벤토리 탭 관계
- 낡은 열쇠 신규 아이템 따라 하기
- Excel과 Google Sheets의 CSV 내보내기 방법
- Unity `ItemVisualCatalog.asset` 아이콘 연결 절차
- 아이콘이 없는 아이템 작성 방법
- Quest 보상 지급 방법
- Quest 완료 시 아이템 소모 방법
- 쌀술 보상 전체 실전 예시
- 기존 아이템 수정과 삭제 시 주의사항
- 중첩, new 표시, 카테고리, 정렬 규칙
- CSV 데이터 검증기와 Test Runner 사용법
- 자주 발생하는 오류와 해결 방법
- 기획자용 체크박스 검수 목록
- 기획자가 직접 수정할 범위와 개발자 협의가 필요한 범위
- 프로젝트의 실제 Coin과 RepairKit 예시

## 20. 검증 결과

### 20.1 런타임 C# 빌드

실행:

```text
dotnet build Assembly-CSharp.csproj
```

최종 결과:

```text
빌드 성공
경고 0개
오류 0개
```

Unity가 생성한 기존 `.csproj`에 새 소스 파일을 반영해 컴파일 범위를 확인했습니다.

### 20.2 Editor C# 빌드

실행:

```text
dotnet build Assembly-CSharp-Editor.csproj
```

최종 결과:

```text
빌드 성공
경고 0개
오류 0개
```

Editor 검증기와 회귀 테스트 코드까지 컴파일되는 것을 확인했습니다.

### 20.3 실제 CSV 스모크 테스트

실제 프로젝트의 CSV 13개를 읽어 `GameDataCsvLoader`와 `GameDataRepository`를 생성하는 별도 스모크 테스트를 실행했습니다.

결과:

```text
PASS
items=2
quests=1
dialogueGraphs=3
localization=20
```

추가 실패 검증:

```text
quest_rewards.csv의 ITEM_COIN을 ITEM_UNKNOWN으로 임시 교체
-> Repository 생성 실패 확인
-> 존재하지 않는 Quest Item ID가 정상적으로 거부됨
```

### 20.4 변경 파일 형식 검사

실행:

```text
git diff --check -- Assets/_Game
```

결과:

```text
Inventory 작업 파일의 trailing whitespace 오류 없음
```

### 20.5 Unity Test Runner

작업 당시 프로젝트가 Unity Editor에서 열려 있어 같은 프로젝트 경로에 별도 Batch Mode Unity를 실행할 수 없었습니다.

열려 있는 Unity를 자동 제어하려고 했으나 Windows 앱 제어 런타임이 사용자 프로필 경로 권한 제한으로 초기화되지 않았습니다.

따라서 다음 범위까지 확인했습니다.

- 런타임 어셈블리 빌드 성공
- Editor 어셈블리와 Test 코드 빌드 성공
- 실제 CSV 전체 로드 스모크 테스트 성공
- 잘못된 Quest Item 참조 거부 성공

Unity Test Runner의 `GameDataCsvRegressionTests` 직접 실행과 Play Mode 시각 확인은 후속 확인 항목으로 남겼습니다.

## 21. 변경 파일 목록

### 21.1 새로 추가한 파일

```text
Assets/_Game/Data/Csv/items.csv
Assets/_Game/Data/InventoryData/ItemVisualCatalog.asset
Assets/_Game/Scripts/Gameplay/Data/Csv/ItemCsvDefinitionLoader.cs
Assets/_Game/Scripts/Gameplay/Inventory/Data/ItemDefinition.cs
Assets/_Game/Scripts/Gameplay/Inventory/Data/InventoryItemDisplayData.cs
Assets/_Game/Scripts/Gameplay/Inventory/ItemVisualCatalogSO.cs
Assets/_Game/Docs/WORKLOG_2026-07-18_INVENTORY_CSV_MIGRATION.md
```

### 21.2 수정한 데이터와 문서

```text
Assets/_Game/Data/Csv/localization.csv
Assets/_Game/Docs/DIALOGUE_QUEST_AUTHORING_GUIDE.md
Assets/_Game/Docs/INVENTORY_SYSTEM_AUTHORING_GUIDE.md
Assets/_Game/Prefabs/GameRoot.prefab
```

### 21.3 수정한 런타임 코드

```text
Assets/_Game/Scripts/Core/Bootstrap/GameRoot.cs
Assets/_Game/Scripts/Gameplay/Data/Csv/GameDataCsvLoader.cs
Assets/_Game/Scripts/Gameplay/Data/Csv/GameDataCsvSource.cs
Assets/_Game/Scripts/Gameplay/Data/GameDataController.cs
Assets/_Game/Scripts/Gameplay/Data/Repository/GameDataRepository.cs
Assets/_Game/Scripts/Gameplay/Data/Repository/IGameDataRepository.cs
Assets/_Game/Scripts/Gameplay/Inventory/Contracts/IInventoryService.cs
Assets/_Game/Scripts/Gameplay/Inventory/Data/InventorySlotData.cs
Assets/_Game/Scripts/Gameplay/Inventory/InventoryController.cs
Assets/_Game/Scripts/Gameplay/Quests/QuestController.cs
Assets/_Game/Scripts/Presentation/UI/Inventory/InventoryPresenter.cs
Assets/_Game/Scripts/Presentation/UI/Inventory/InventorySlotView.cs
Assets/_Game/Scripts/Presentation/UI/Inventory/InventoryView.cs
```

### 21.4 수정한 Editor 코드

```text
Assets/_Game/Scripts/Editor/GameDataCsvRegressionTests.cs
Assets/_Game/Scripts/Editor/GameDataEditorValidator.cs
```

### 21.5 제거한 파일

```text
Assets/_Game/Scripts/Gameplay/Inventory/Data/ItemDataSO.cs
Assets/_Game/Data/InventoryData/Coin.asset
Assets/_Game/Data/InventoryData/RepairKit.asset
```

## 22. 기획자의 새 아이템 작업 흐름

전환 후 기획자는 다음 순서로 작업합니다.

```text
1. items.csv에 새 아이템 행 추가
2. localization.csv에 이름과 설명 두 행 추가
3. 아이콘이 있으면 icon_id 입력
4. ItemVisualCatalog.asset에 같은 Icon Id와 Sprite 연결
5. Quest에서 사용하면 quest_rewards.csv 또는 quest_consume_items.csv 작성
6. Tools > Ruin Conservator > CSV 데이터 검증기 실행
7. Error 0개 확인
8. Play Mode에서 이름, 설명, 아이콘, 수량, 탭, 지급·소모 확인
```

신규 아이템마다 ScriptableObject를 만들거나 InventoryController 목록에 등록하는 단계는 없어졌습니다.

## 23. 주의사항과 남은 위험

### 23.1 Unity `.meta` 파일 무시 규칙

현재 `.gitignore`에는 다음 규칙이 있습니다.

```gitignore
*.meta
```

이번 작업에서 생성한 `items.csv`, `ItemVisualCatalog.asset`, 새 C# 스크립트에는 Unity가 참조에 사용하는 `.meta`와 GUID가 필요합니다. 로컬에는 해당 `.meta` 파일을 만들었지만 현재 Git 기본 규칙에서는 추적 대상에 나타나지 않습니다.

이 상태로 원본 파일만 Commit하면 다른 팀원의 Unity에서 새로운 GUID가 생성되고 다음 참조가 끊길 수 있습니다.

```text
GameRoot.prefab -> items.csv
GameRoot.prefab -> ItemVisualCatalog.asset
ItemVisualCatalog.asset -> ItemVisualCatalogSO.cs
```

따라서 Commit 전 반드시 다음 중 하나를 결정해야 합니다.

1. 프로젝트 전체 `.meta` 관리 정책을 정리하고 `.gitignore`의 `*.meta` 규칙을 제거
2. 이번 신규 파일의 `.meta`를 강제로 추적하면서 전체 정책 정리 일정을 별도로 확정

Unity 협업 안정성을 위해서는 첫 번째 방식을 권장합니다. 다만 기존 프로젝트 전체 `.meta`가 대량으로 나타날 수 있으므로 별도 검토와 안전한 Commit 분리가 필요합니다.

### 23.2 현재 아이콘 카탈로그가 비어 있음

기존 Coin과 RepairKit의 Sprite 참조가 null이었기 때문에 이관된 `items.csv`의 `icon_id`와 새 카탈로그 목록도 비어 있습니다.

실제 아이콘이 준비되면 다음 작업이 필요합니다.

```text
items.csv에 icon_id 입력
-> ItemVisualCatalog.asset에 같은 ID 추가
-> Sprite 연결
-> CSV 데이터 검증기 실행
```

### 23.3 기존 세이브 데이터 확인

기존 슬롯 저장 구조가 Item ID 중심이어서 CSV 전환과 기본적으로 호환되지만, 실제 사용 중인 세이브 파일이 있다면 다음 항목을 Play Mode에서 확인해야 합니다.

- 기존 `ITEM_COIN` 슬롯 복원
- 기존 `ITEM_REPAIR_KIT` 슬롯 복원
- 수량과 new 상태 유지
- 아이템 상세 이름과 설명 표시
- 잘못된 또는 삭제된 Item ID의 처리

### 23.4 item_id 변경 금지

`item_id`는 Quest CSV와 세이브 데이터의 연결 기준입니다. 출시 또는 저장 데이터 사용 후에는 의미 없이 변경하지 않습니다.

변경이 꼭 필요한 경우 CSV만 수정하지 말고 세이브 마이그레이션 여부를 개발자와 함께 검토해야 합니다.

## 24. 후속 확인 항목

- [ ] Unity 상단 메뉴의 CSV 데이터 검증기에서 Error 0개 확인
- [ ] Unity Test Runner Edit Mode의 `GameDataCsvRegressionTests` 실행
- [ ] Play Mode에서 Coin 100개 퀘스트 보상 수령 확인
- [ ] Play Mode에서 RepairKit 1개 퀘스트 소모 확인
- [ ] Inventory UI의 이름, 설명, 수량, new 표시 확인
- [ ] 전체 및 기타 탭에서 Coin 표시 확인
- [ ] 전체 탭에서 RepairKit 표시 확인
- [ ] 저장 후 재실행 시 기존 슬롯 복원 확인
- [ ] 실제 아이콘 준비 후 ItemVisualCatalog 연결
- [ ] `.meta` 파일 Git 추적 정책 정리
- [ ] 변경 범위를 검토한 뒤 Inventory CSV 전환 Commit 생성

## 25. 권장 Commit 분리

아직 Commit하지 않았다면 다음과 같이 분리하는 것을 권장합니다.

### Commit 1 — Inventory CSV 런타임 전환

```text
feat: Inventory 아이템 데이터를 CSV Definition으로 전환
```

포함 범위:

- Item Definition과 CSV Loader
- GameDataRepository 확장
- InventoryController와 UI 타입 변경
- QuestController Item ID 지급 변경

### Commit 2 — 실제 데이터와 프리팹 이관

```text
data: Coin과 RepairKit을 items.csv로 이관
```

포함 범위:

- `items.csv`
- `localization.csv`
- `ItemVisualCatalog.asset`
- `GameRoot.prefab`
- 레거시 ItemDataSO와 개별 아이템 에셋 제거

### Commit 3 — 검증과 테스트

```text
test: Inventory CSV 참조 검증과 회귀 테스트 추가
```

포함 범위:

- `GameDataEditorValidator`
- `GameDataCsvRegressionTests`

### Commit 4 — 제작 문서

```text
docs: 비개발 직군용 Inventory CSV 작성 가이드 추가
```

포함 범위:

- Inventory CSV 작성 가이드
- Dialogue·Quest 가이드의 Item 연결 내용
- 이번 작업일지

`.meta` 정책을 먼저 해결한다면 해당 변경은 Inventory 기능 Commit과 섞지 말고 별도 Commit으로 분리하는 것을 권장합니다.

## 26. 작업 결과 요약

이번 작업으로 Inventory 아이템의 원본 데이터 관리 방식이 Dialogue·Quest와 같은 CSV 중심 구조로 통합됐습니다.

최종 결과:

```text
개별 ItemDataSO 제작 제거
-> items.csv 한 줄로 아이템 정의
-> localization.csv에서 이름과 설명 관리
-> ItemVisualCatalogSO에서 Sprite만 연결
-> GameDataRepository에서 모든 Item ID와 Quest 참조 검증
-> Inventory와 Quest가 문자열 item_id로 동작
-> 비개발 직군용 전체 작성 가이드 제공
```

개발 코드, 실제 데이터, Unity 프리팹, Editor 검증, 회귀 테스트, 기획 문서까지 CSV 구조에 맞게 함께 전환했습니다.
