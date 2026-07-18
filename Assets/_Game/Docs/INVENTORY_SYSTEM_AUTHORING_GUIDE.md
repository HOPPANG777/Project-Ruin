# Inventory CSV 작성 가이드

이 문서는 **개발 경험이 없는 기획자와 팀원**이 새 아이템을 만들고, 아이콘을 연결하고, 퀘스트 보상이나 소모 아이템으로 사용하는 방법을 설명합니다.

코드를 수정할 필요는 없습니다. 대부분의 작업은 CSV 세 칸과 Unity의 아이콘 목록만 정확히 입력하면 끝납니다.

---

## 0. 가장 짧은 작업 순서

새 아이템 하나를 추가할 때는 아래 순서만 기억하면 됩니다.

```text
1. items.csv에 아이템 한 줄 추가
2. localization.csv에 이름과 설명 두 줄 추가
3. 아이콘이 있으면 ItemVisualCatalog에 연결
4. 퀘스트에서 사용하면 Quest CSV에 item_id 입력
5. Unity CSV 데이터 검증기 실행
6. Play Mode에서 직접 확인
```

작업 파일은 모두 다음 폴더에 있습니다.

```text
Assets/_Game/Data/Csv/
```

아이콘 연결 파일은 다음 위치에 있습니다.

```text
Assets/_Game/Data/InventoryData/ItemVisualCatalog.asset
```

---

## 1. 먼저 알아야 하는 세 가지

### 1-1. CSV란 무엇인가요?

CSV는 표 형태의 데이터를 저장하는 파일입니다. Excel이나 Google Sheets의 표와 거의 같다고 생각하면 됩니다.

- 첫 번째 줄은 각 열의 이름인 `헤더`입니다.
- 두 번째 줄부터 실제 데이터입니다.
- 한 줄이 아이템 하나입니다.
- 쉼표 하나가 셀 하나를 구분합니다.

예:

```csv
item_id,display_name_key,description_text_key,icon_id,price,item_type,max_stack_count
ITEM_COIN,item.coin.name,item.coin.description,ICON_COIN,0,ITEM_MISC,999
```

CSV를 메모장으로 직접 편집할 수도 있지만, 쉼표나 큰따옴표 때문에 실수하기 쉽습니다. 가능하면 Excel이나 Google Sheets에서 표로 작성한 뒤 **UTF-8 CSV**로 내보내세요.

### 1-2. ID란 무엇인가요?

ID는 게임이 데이터를 찾을 때 사용하는 고유한 이름입니다. 화면에 보이는 이름과는 다릅니다.

```text
화면에 보이는 이름: 복원 도구
게임이 사용하는 ID: ITEM_REPAIR_KIT
```

ID는 사람의 주민등록번호나 사번과 비슷합니다. 두 아이템이 같은 ID를 사용하면 안 되고, 다른 CSV에서 참조한 뒤에는 함부로 바꾸면 안 됩니다.

다음 두 값은 서로 다르게 취급됩니다.

```text
ITEM_COIN
ITEM_CoIN
```

대문자와 소문자까지 완전히 같아야 합니다.

### 1-3. 왜 이름과 설명을 다른 파일에 쓰나요?

아이템 이름과 설명은 한국어, 영어, 일본어처럼 여러 언어로 표시될 수 있습니다. 그래서 `items.csv`에는 완성 문장 대신 문장을 찾는 Key를 적고, 실제 문장은 `localization.csv`에 적습니다.

```text
items.csv
display_name_key = item.coin.name

localization.csv
item.coin.name = 코인 / Coin
```

---

## 2. 아이템 데이터가 연결되는 구조

기획자가 수정하는 데이터는 아래와 같이 연결됩니다.

```text
items.csv
├─ item_id -------------------- 퀘스트와 저장 데이터가 아이템을 찾는 값
├─ display_name_key ───────┐
├─ description_text_key ───┴─ localization.csv의 실제 문장
└─ icon_id ------------------- ItemVisualCatalog.asset의 Unity Sprite
```

쉽게 말하면:

- 아이템의 규칙과 숫자는 `items.csv`
- 화면에 보이는 문장은 `localization.csv`
- 실제 이미지 파일은 `ItemVisualCatalog.asset`
- 퀘스트 지급과 소모는 Quest CSV

에 작성합니다.

기존 방식에서 사용하던 `ItemDataSO`와 개별 아이템 `.asset`은 더 이상 만들지 않습니다.

---

## 3. items.csv의 모든 열 설명

파일 위치:

```text
Assets/_Game/Data/Csv/items.csv
```

첫 번째 줄의 헤더 이름과 순서는 코드와 연결되어 있습니다. **헤더를 번역하거나, 순서를 바꾸거나, 새 열을 임의로 추가하지 마세요.**

| 열 이름 | 필수 | 쉬운 설명 | 예시 |
| --- | --- | --- | --- |
| `item_id` | 필수 | 게임 전체에서 아이템을 구분하는 고유 ID | `ITEM_COIN` |
| `display_name_key` | 필수 | `localization.csv`에서 아이템 이름을 찾는 Key | `item.coin.name` |
| `description_text_key` | 필수 | `localization.csv`에서 아이템 설명을 찾는 Key | `item.coin.description` |
| `icon_id` | 선택 | Unity 아이콘을 찾는 ID. 아이콘이 없으면 빈칸 | `ICON_COIN` |
| `price` | 필수 | 아이템 가격. 판매하지 않거나 미정이면 `0` | `50` |
| `item_type` | 필수 | 인벤토리 탭과 용도를 결정하는 종류 | `ITEM_MISC` |
| `max_stack_count` | 필수 | 인벤토리 한 칸에 겹쳐 보관할 수 있는 최대 수량 | `99` |

### 3-1. item_id 작성 규칙

`item_id`는 다음 규칙을 모두 지켜야 합니다.

- 영문 대문자로 시작합니다.
- 영문 대문자, 숫자, 밑줄 `_`만 사용합니다.
- 공백, 한글, 소문자, 하이픈 `-`은 사용하지 않습니다.
- 다른 아이템과 중복되면 안 됩니다.

좋은 예:

```text
ITEM_OLD_KEY
ITEM_RICE_WINE
ITEM_RELIC_BRUSH_01
```

잘못된 예:

```text
item_old_key       소문자 사용
ITEM OLD KEY       공백 사용
ITEM-OLD-KEY       하이픈 사용
낡은열쇠            한글 사용
```

권장 형식:

```text
ITEM_ + 아이템을 설명하는 영문 단어
```

### 3-2. display_name_key와 description_text_key 작성 규칙

Localization Key는 다음 규칙을 사용합니다.

- 영문 소문자로 시작합니다.
- 영문 소문자, 숫자, 마침표 `.`, 밑줄 `_`만 사용합니다.
- 이름 Key는 `.name`으로 끝내는 것을 권장합니다.
- 설명 Key는 `.description`으로 끝내는 것을 권장합니다.

예:

```text
item.old_key.name
item.old_key.description
```

두 Key는 반드시 `localization.csv`에도 존재해야 합니다.

### 3-3. icon_id 작성 규칙

`icon_id`는 아이콘 이미지와 연결하는 이름입니다.

- 아이콘이 있으면 `ICON_`으로 시작하는 대문자 ID를 권장합니다.
- 아이콘이 아직 없으면 셀을 완전히 비워도 됩니다.
- 파일 경로나 이미지 파일명을 적는 칸이 아닙니다.
- 같은 아이콘 ID를 카탈로그에 두 번 등록하지 않습니다.

좋은 예:

```text
ICON_COIN
ICON_OLD_KEY
ICON_RICE_WINE
```

잘못된 예:

```text
Assets/Icon/Coin.png
C:\Images\Coin.png
coin.png
```

### 3-4. price 작성 규칙

`price`에는 `0` 이상의 정수만 입력합니다.

```text
0
10
250
```

`무료`, `미정`, `10원`, `-1`, `10.5`처럼 글자, 음수, 소수는 사용할 수 없습니다.

현재 판매 기능을 사용하지 않는 아이템도 빈칸으로 두지 말고 `0`을 입력하세요.

### 3-5. max_stack_count 작성 규칙

한 인벤토리 슬롯에 몇 개까지 겹칠 수 있는지를 결정합니다.

| 값 | 결과 |
| ---: | --- |
| `1` | 한 슬롯에 하나만 보관 |
| `10` | 한 슬롯에 최대 10개 보관 |
| `99` | 한 슬롯에 최대 99개 보관 |
| `999` | 한 슬롯에 최대 999개 보관 |

예를 들어 `max_stack_count`가 `99`인 아이템을 120개 얻으면 `99개 슬롯`과 `21개 슬롯`으로 나뉩니다.

반드시 `1` 이상의 정수를 입력해야 합니다. 빈칸과 `0`은 허용되지 않습니다.

---

## 4. item_type 선택하기

`item_type`은 아이템의 용도와 표시될 인벤토리 탭을 정합니다. 아래 값 중 하나를 **복사해서 그대로** 사용하세요.

| 입력 값 | 언제 사용하나요? | 표시되는 탭 |
| --- | --- | --- |
| `ITEM_QUEST` | 퀘스트 조건, NPC 전달, 문 열쇠 같은 진행용 아이템 | 전체 |
| `ITEM_ALCOHOL_COLLECTIBLE` | 완성된 술이나 수집 대상 술 | 전체, 술 |
| `ITEM_ALCOHOL_TOOL` | 술 제조에 사용하는 도구 | 전체, 술 |
| `ITEM_RELIC_COLLECTIBLE` | 발굴하거나 복원한 수집 대상 유물 | 전체, 유물 |
| `ITEM_RELIC_TOOL` | 유물 발굴이나 복원에 사용하는 도구 | 전체, 유물 |
| `ITEM_MISC` | 위 분류에 속하지 않는 코인, 잡화, 기타 아이템 | 전체, 기타 |

주의:

- 철자를 줄이거나 번역하지 마세요.
- `ITEM_QUEST` 아이템이 기타 탭에 보이지 않는 것은 정상입니다.
- 술 도구와 유물 도구는 해당 카테고리에서 다른 아이템보다 먼저 정렬됩니다.

---

## 5. 새 아이템 추가하기: 처음부터 끝까지

이 장에서는 `낡은 열쇠`라는 새 아이템을 만드는 과정을 예로 듭니다.

완성 목표:

```text
아이템 ID: ITEM_OLD_KEY
화면 이름: 낡은 열쇠
설명: 오래된 창고 문을 열 수 있을 것 같은 열쇠다.
타입: 퀘스트 아이템
가격: 0
한 슬롯 최대 수량: 99
```

### 5-1. items.csv에 한 줄 추가

`items.csv`를 열고 마지막 줄 아래에 다음 행을 추가합니다.

| item_id | display_name_key | description_text_key | icon_id | price | item_type | max_stack_count |
| --- | --- | --- | --- | ---: | --- | ---: |
| `ITEM_OLD_KEY` | `item.old_key.name` | `item.old_key.description` | `ICON_OLD_KEY` | `0` | `ITEM_QUEST` | `99` |

CSV로 보면 다음과 같습니다.

```csv
ITEM_OLD_KEY,item.old_key.name,item.old_key.description,ICON_OLD_KEY,0,ITEM_QUEST,99
```

기존 행을 지우거나 헤더를 다시 입력하지 말고, 새 행만 추가하세요.

### 5-2. localization.csv에 이름과 설명 추가

`localization.csv`를 열고 두 행을 추가합니다.

| text_key | ko | en | ja |
| --- | --- | --- | --- |
| `item.old_key.name` | `낡은 열쇠` | `Old Key` | 빈칸 가능 |
| `item.old_key.description` | `오래된 창고 문을 열 수 있을 것 같은 열쇠다.` | `A key that may open the old storage.` | 빈칸 가능 |

CSV로 보면 다음과 같습니다.

```csv
item.old_key.name,낡은 열쇠,Old Key,
item.old_key.description,오래된 창고 문을 열 수 있을 것 같은 열쇠다.,A key that may open the old storage.,
```

한국어 `ko`는 반드시 입력합니다. 영어와 일본어 번역이 아직 없다면 해당 셀은 비워도 됩니다.

### 5-3. CSV 저장 또는 내보내기

Excel을 사용한다면:

```text
파일
-> 다른 이름으로 저장
-> CSV UTF-8(쉼표로 분리)(*.csv)
```

Google Sheets를 사용한다면:

```text
파일
-> 다운로드
-> 쉼표로 구분된 값(.csv)
```

반드시 확인할 것:

- 파일 이름이 `items.csv`, `localization.csv` 그대로인가?
- `.xlsx`나 `.txt`로 저장되지 않았는가?
- UTF-8 형식인가?
- 원래 파일과 같은 `Assets/_Game/Data/Csv/` 위치에 들어갔는가?

Excel에서 “일부 기능이 손실될 수 있습니다”라는 안내가 나오면 CSV 형식을 유지하도록 선택합니다.

### 5-4. Unity가 파일을 읽을 때까지 기다리기

Unity로 돌아가면 오른쪽 아래에 Import 또는 Compile 진행 표시가 나타날 수 있습니다. 표시가 사라질 때까지 기다리세요.

컴파일 중에는 Play 버튼을 반복해서 누르거나 Unity를 강제로 종료하지 마세요.

### 5-5. 아이콘 연결하기

아이콘이 없다면 이 단계는 건너뛰어도 됩니다. 이 경우 `items.csv`의 `icon_id`도 빈칸이어야 합니다.

아이콘이 있다면:

1. 아이콘 이미지 파일을 Unity 프로젝트의 Sprite 폴더에 넣습니다.
2. Unity Project 창에서 `Assets/_Game/Data/InventoryData/ItemVisualCatalog.asset`을 선택합니다.
3. Inspector에서 `Icons` 목록을 찾습니다.
4. 목록 오른쪽 아래의 `+` 버튼을 누릅니다.
5. 새 항목의 `Icon Id`에 `ICON_OLD_KEY`를 입력합니다.
6. `Sprite` 칸에 낡은 열쇠 아이콘을 드래그합니다.
7. `items.csv`의 `icon_id`와 Inspector의 `Icon Id`가 완전히 같은지 확인합니다.

연결 구조:

```text
items.csv: ICON_OLD_KEY
        ↓ 같은 ID
ItemVisualCatalog: ICON_OLD_KEY
        ↓
Unity의 낡은 열쇠 Sprite
```

이미지 파일이 Texture로만 보이고 Sprite 칸에 들어가지 않는다면 이미지 Import Settings의 `Texture Type`이 `Sprite (2D and UI)`인지 확인하거나 개발자에게 문의하세요.

### 5-6. CSV 데이터 검증하기

Unity 상단 메뉴에서 다음 항목을 실행합니다.

```text
Tools
-> Ruin Conservator
-> CSV 데이터 검증기
```

검증 창에 오류가 나오면 Play Mode로 확인하기 전에 먼저 수정합니다.

### 5-7. 게임에서 확인하기

Play 버튼을 누르고 실제 획득 흐름을 실행합니다.

확인할 것:

- 아이템이 인벤토리에 들어오는가?
- 이름과 설명이 한국어로 보이는가?
- 아이콘이 보이는가?
- 수량이 맞는가?
- 전체 탭에 보이는가?
- 의도한 술, 유물, 기타 탭에도 보이는가?
- 처음 획득했을 때 new 표시가 보이는가?

---

## 6. 아이콘이 아직 준비되지 않은 아이템

아이콘 없이 아이템부터 먼저 만들 수 있습니다.

`items.csv`에서 `icon_id` 셀만 비워 둡니다.

```csv
ITEM_OLD_KEY,item.old_key.name,item.old_key.description,,0,ITEM_QUEST,99
```

중요:

- 빈칸 대신 `NONE`, `NULL`, `미정`을 입력하지 마세요.
- 나중에 아이콘이 완성되면 `icon_id`를 추가하고 카탈로그에 같은 ID를 등록합니다.
- 아이콘이 없는 동안 이름, 설명, 수량 등 나머지 데이터는 정상 동작합니다.

---

## 7. 퀘스트 보상으로 아이템 지급하기

퀘스트 보상으로 아이템을 주려면 먼저 그 아이템이 `items.csv`에 존재해야 합니다.

사용 파일:

```text
Assets/_Game/Data/Csv/quest_rewards.csv
```

관련 열:

| 열 이름 | 설명 |
| --- | --- |
| `quest_id` | 보상을 주는 퀘스트 ID |
| `reward_order` | 같은 퀘스트 안에서 보상을 정렬하는 번호. 보통 0부터 시작 |
| `reward_id` | 해당 보상 행의 고유 ID |
| `description_text_key` | 퀘스트 UI에 표시할 보상 설명 Key |
| `progress_flag_id` | Flag 보상이 없으면 빈칸 |
| `flag_value` | Flag 보상이 없으면 빈칸 |
| `item_id` | 지급할 아이템의 `items.csv` ID |
| `item_amount` | 지급할 수량. 1 이상 |

예: 퀘스트 완료 보상으로 코인 100개 지급

```csv
QUEST_RESTORE_MURAL,1,REWARD_COINS,quest.restore_mural.reward.coins,,,ITEM_COIN,100
```

아이템 보상만 주는 경우 `progress_flag_id`와 `flag_value`는 둘 다 비웁니다. 쉼표 개수를 줄이지 말고 셀만 비워 두세요.

보상 설명 Key도 `localization.csv`에 있어야 합니다.

```csv
quest.restore_mural.reward.coins,보존 활동 지원금 100을 받는다.,Receive 100 conservation funds.,
```

필수 확인:

- `ITEM_COIN`이 `items.csv`에 존재하는가?
- `item_amount`가 빈칸이나 0이 아닌가?
- `reward_id`가 같은 퀘스트 안에서 중복되지 않는가?
- 보상 설명 Key가 `localization.csv`에 존재하는가?
- 보상을 받는 대화 선택지가 실제 보상 수령 흐름에 연결되어 있는가?

---

## 8. 퀘스트 완료 시 아이템 소모하기

플레이어가 특정 아이템을 가지고 있어야 퀘스트를 완료하고, 완료와 동시에 그 아이템을 없애려면 `quest_consume_items.csv`를 사용합니다.

사용 파일:

```text
Assets/_Game/Data/Csv/quest_consume_items.csv
```

| 열 이름 | 설명 |
| --- | --- |
| `quest_id` | 아이템을 소모하는 퀘스트 ID |
| `consume_order` | 같은 퀘스트 안에서 소모 항목을 정렬하는 번호. 보통 0부터 시작 |
| `item_id` | 소모할 아이템의 `items.csv` ID |
| `item_amount` | 소모할 수량. 1 이상 |

예: 벽화 복원 퀘스트 완료 시 복원 도구 1개 소모

```csv
QUEST_RESTORE_MURAL,0,ITEM_REPAIR_KIT,1
```

동작 결과:

```text
플레이어가 ITEM_REPAIR_KIT 1개 이상 보유
-> 퀘스트 완료 가능
-> 완료 처리
-> ITEM_REPAIR_KIT 1개 자동 소모
```

플레이어가 필요한 수량을 가지고 있지 않으면 퀘스트 완료가 실패합니다.

주의:

- 단순히 “가지고 있기만 하면 되는” 조건과 “완료 시 실제로 사라지는” 조건을 구분하세요.
- 현재 이 CSV에 등록한 아이템은 완료 시 실제로 차감됩니다.
- 같은 퀘스트에서 여러 아이템을 소모하면 행을 여러 개 만들고 `consume_order`를 `0`, `1`, `2`처럼 다르게 입력합니다.

예:

```csv
QUEST_RESTORE_STATUE,0,ITEM_REPAIR_KIT,1
QUEST_RESTORE_STATUE,1,ITEM_COIN,50
```

---

## 9. 실전 예시: 수집형 술을 퀘스트 보상으로 지급

목표:

```text
퀘스트 보상으로 쌀술 3개 지급
쌀술은 전체 탭과 술 탭에 표시
한 슬롯에 최대 99개 보관
```

### 9-1. items.csv

```csv
ITEM_RICE_WINE,item.rice_wine.name,item.rice_wine.description,ICON_RICE_WINE,50,ITEM_ALCOHOL_COLLECTIBLE,99
```

### 9-2. localization.csv

```csv
item.rice_wine.name,쌀술,Rice Wine,
item.rice_wine.description,맑고 은은한 향이 나는 술이다.,A clear rice wine with a subtle aroma.,
quest.elder.reward.rice_wine,쌀술 3개를 받는다.,Receive 3 rice wines.,
```

### 9-3. ItemVisualCatalog.asset

```text
Icon Id: ICON_RICE_WINE
Sprite: 쌀술 아이콘 이미지
```

### 9-4. quest_rewards.csv

```csv
QUEST_ELDER_REQUEST,0,REWARD_RICE_WINE,quest.elder.reward.rice_wine,,,ITEM_RICE_WINE,3
```

### 9-5. 예상 결과

```text
플레이어가 퀘스트 보상 수령
-> 쌀술 3개 획득
-> 인벤토리에 new 표시
-> 전체 탭과 술 탭에 표시
```

---

## 10. 기존 아이템 수정하기

### 수정해도 비교적 안전한 값

다음 값은 기획 변경에 따라 수정할 수 있습니다.

- `display_name_key`가 가리키는 실제 문장
- `description_text_key`가 가리키는 실제 문장
- `icon_id`와 연결된 Sprite
- `price`
- `max_stack_count`

`item_type`을 바꾸면 아이템이 표시되는 탭과 정렬 순서가 달라집니다. 의도한 변경인지 반드시 확인하세요.

### 함부로 바꾸면 안 되는 값

`item_id`는 저장 데이터, 퀘스트, 대화 흐름에서 참조할 수 있으므로 함부로 변경하지 않습니다.

```text
ITEM_OLD_KEY -> ITEM_STORAGE_KEY
```

처럼 바꾸면 기존 저장 데이터의 아이템과 Quest CSV의 참조가 끊길 수 있습니다.

정말 변경해야 한다면 기획자 혼자 수정하지 말고 개발자에게 먼저 알리세요.

### 아이템을 삭제할 때

`items.csv`에서 행을 지우기 전에 프로젝트 전체에서 해당 `item_id`를 사용 중인지 확인해야 합니다.

특히 다음 파일을 확인합니다.

- `quest_rewards.csv`
- `quest_consume_items.csv`
- 기타 개발 코드나 저장 데이터

참조 중인 아이템을 먼저 삭제하면 CSV 검증 오류가 발생하거나 기존 세이브 데이터에 문제가 생길 수 있습니다.

---

## 11. 게임 안에서의 인벤토리 동작 규칙

### 중첩

같은 `item_id`의 아이템은 `max_stack_count`까지 같은 슬롯에 합쳐집니다. 남는 수량은 새 슬롯에 들어갑니다.

### new 표시

처음 획득한 아이템에는 new 표시가 붙습니다. 플레이어가 해당 아이템을 확인하면 표시가 사라집니다.

### 카테고리

- 전체: 모든 정상 아이템
- 술: 술 수집품과 술 도구
- 유물: 유물 수집품과 유물 도구
- 기타: `ITEM_MISC`

퀘스트 아이템은 전체 탭에만 표시됩니다.

### 기본 정렬

플레이어가 직접 슬롯을 옮기기 전에는 도구 아이템이 먼저 보이고, 그 외에는 최근 획득한 아이템이 우선됩니다.

```text
술 도구
-> 유물 도구
-> 그 외 최근 획득 아이템
```

플레이어가 드래그로 순서를 바꾼 뒤에는 수동 배치 순서가 우선됩니다.

### 아이템 삭제

현재 인벤토리에는 플레이어가 마음대로 아이템을 버리는 기능이 없습니다. 퀘스트 완료, 재료 사용, 술 제조, 유물 발굴처럼 정해진 게임 흐름에서만 자동 소모됩니다.

---

## 12. 데이터 검증 방법

### 12-1. CSV 데이터 검증기

Unity 상단 메뉴:

```text
Tools > Ruin Conservator > CSV 데이터 검증기
```

검증기는 다음과 같은 문제를 확인합니다.

- 필수 CSV 파일 누락
- 헤더 이름이나 순서 오류
- 중복 아이템 ID
- 허용되지 않는 ID 문자
- 가격 또는 최대 중첩 수 오류
- 존재하지 않는 Localization Key
- 잘못된 `item_type`
- Quest CSV가 존재하지 않는 아이템을 참조하는 문제
- 아이콘 ID와 Sprite 연결 누락

`Error`는 반드시 수정해야 합니다. Error가 있으면 Play Mode 진입이나 빌드가 차단될 수 있습니다.

아이콘이 아직 없는 경우처럼 의도된 상태는 `icon_id`를 빈칸으로 두면 됩니다.

### 12-2. 팀 최종 검수

기능을 합치기 전에는 Unity Test Runner도 실행합니다.

```text
Window > General > Test Runner
-> Edit Mode
-> GameDataCsvRegressionTests
-> Run Selected
```

테스트 실행 방법을 모르거나 메뉴가 보이지 않으면 개발자에게 요청하세요. 기획자는 최소한 CSV 데이터 검증기의 Error를 0개로 만든 뒤 작업을 전달해야 합니다.

### 12-3. Play Mode 확인

데이터 검증 통과는 문법과 연결이 맞다는 뜻입니다. 실제 연출과 기획 의도까지 맞는지는 Play Mode에서 별도로 확인해야 합니다.

```text
아이템 획득
-> 이름/설명/아이콘 확인
-> 수량 및 중첩 확인
-> 탭 노출 확인
-> 퀘스트 완료 또는 보상 수령 확인
-> 저장 후 다시 불러오기 확인
```

---

## 13. 자주 발생하는 문제와 해결법

### “중복 아이템 ID가 있습니다”

원인:

- `items.csv`에 같은 `item_id`가 두 번 있습니다.

해결:

- 중복 행을 찾습니다.
- 서로 다른 아이템이면 한쪽에 새로운 ID를 부여합니다.
- 같은 아이템을 실수로 두 번 넣었다면 중복 행을 삭제합니다.

### “ID는 영문 대문자로 시작해야 합니다”

원인:

- `item_id` 또는 `icon_id`에 소문자, 공백, 한글, 하이픈을 사용했습니다.

해결:

```text
item-old-key  (잘못됨)
ITEM_OLD_KEY  (올바름)
```

### “아이템 이름 또는 설명 문장 키를 찾을 수 없습니다”

원인:

- `items.csv`의 Key가 `localization.csv`에 없습니다.
- 마침표, 밑줄, 철자 중 하나가 다릅니다.

해결:

- `display_name_key`와 `description_text_key`를 복사합니다.
- `localization.csv`의 `text_key` 열에서 완전히 같은 값을 찾습니다.
- 없다면 이름과 설명 행을 추가합니다.

### “지원하지 않는 아이템 타입입니다”

원인:

- `item_type` 철자가 잘못됐습니다.

해결:

- 이 문서의 4장 표에서 값을 복사해 붙여넣습니다.
- 한글 이름이나 줄인 표현을 사용하지 않습니다.

### 아이템은 들어오지만 이름 대신 Key가 보인다

확인할 것:

- `localization.csv`에 같은 Key가 있는가?
- 현재 선택된 언어 열에 문장이 있는가?
- 한국어 `ko` 셀이 비어 있지 않은가?
- Unity가 CSV 변경을 다시 Import했는가?

### 아이템은 들어오지만 아이콘이 보이지 않는다

확인할 것:

- `items.csv`의 `icon_id`가 비어 있지 않은가?
- `ItemVisualCatalog.asset`에 같은 `Icon Id`가 있는가?
- 대문자와 밑줄까지 완전히 같은가?
- 해당 항목의 `Sprite` 칸이 비어 있지 않은가?
- 이미지의 Texture Type이 `Sprite (2D and UI)`인가?

### 퀘스트 보상을 받아도 아이템이 들어오지 않는다

확인할 것:

- `quest_rewards.csv`의 `item_id`가 `items.csv`에 존재하는가?
- `item_amount`가 1 이상인가?
- `reward_id`와 `reward_order`가 중복되지 않는가?
- 보상 수령 대화나 버튼이 실제 보상 지급 흐름에 연결됐는가?
- 이미 보상을 받은 퀘스트를 다시 시험하고 있지 않은가?

### 퀘스트 완료 시 아이템이 소모되지 않는다

확인할 것:

- `quest_consume_items.csv`에 행을 추가했는가?
- `quest_id`가 실제 퀘스트 ID와 같은가?
- `item_id`가 `items.csv`와 같은가?
- `item_amount`가 1 이상인가?
- 플레이어가 해당 수량만큼 실제로 보유하고 있는가?
- 퀘스트가 완료 처리까지 진행됐는가?

### 술, 유물, 기타 탭에 아이템이 안 보인다

확인할 것:

- `item_type`이 원하는 카테고리와 맞는가?
- `ITEM_QUEST`는 전체 탭에만 보이는 것이 정상입니다.
- 기타 탭에는 `ITEM_MISC`만 보입니다.

### CSV를 수정했더니 모든 데이터가 로드되지 않는다

확인할 것:

- 첫 번째 헤더 줄을 수정하지 않았는가?
- 셀 중간에 임의의 줄바꿈이나 쉼표를 직접 입력하지 않았는가?
- 파일을 UTF-8 CSV로 저장했는가?
- 파일 이름이 바뀌지 않았는가?
- Excel 파일 `.xlsx` 자체를 CSV 폴더에 넣지 않았는가?

문장에 쉼표나 줄바꿈이 필요하면 텍스트 편집기에서 직접 처리하지 말고 Excel 또는 Google Sheets에서 입력한 뒤 CSV로 내보내는 것을 권장합니다.

---

## 14. 작업 완료 체크리스트

### items.csv

- [ ] 새 아이템이 한 행으로 추가되어 있는가?
- [ ] `item_id`가 다른 아이템과 중복되지 않는가?
- [ ] `item_id`가 대문자, 숫자, 밑줄만 사용하는가?
- [ ] 이름 Key와 설명 Key가 비어 있지 않은가?
- [ ] 아이콘이 없으면 `icon_id`를 완전히 비웠는가?
- [ ] `price`가 0 이상의 정수인가?
- [ ] `item_type`이 허용된 여섯 값 중 하나인가?
- [ ] `max_stack_count`가 1 이상의 정수인가?

### localization.csv

- [ ] 이름 Key 행을 추가했는가?
- [ ] 설명 Key 행을 추가했는가?
- [ ] `items.csv`와 Key 철자가 완전히 같은가?
- [ ] 한국어 `ko` 문장이 비어 있지 않은가?
- [ ] 같은 `text_key`를 중복 작성하지 않았는가?

### 아이콘

- [ ] 아이콘이 있다면 `icon_id`를 작성했는가?
- [ ] `ItemVisualCatalog.asset`에 같은 ID를 등록했는가?
- [ ] Sprite를 연결했는가?
- [ ] 같은 아이콘 ID가 중복되지 않는가?

### 퀘스트 연동

- [ ] 보상 아이템이면 `quest_rewards.csv`에 작성했는가?
- [ ] 소모 아이템이면 `quest_consume_items.csv`에 작성했는가?
- [ ] Quest CSV의 `item_id`가 `items.csv`와 완전히 같은가?
- [ ] 지급 또는 소모 수량이 1 이상인가?
- [ ] 보상 설명 Key가 `localization.csv`에 존재하는가?

### 최종 확인

- [ ] CSV를 UTF-8 형식으로 저장했는가?
- [ ] Unity Import가 끝날 때까지 기다렸는가?
- [ ] CSV 데이터 검증기의 Error가 0개인가?
- [ ] Play Mode에서 이름, 설명, 아이콘, 수량을 확인했는가?
- [ ] 의도한 인벤토리 탭에 표시되는가?
- [ ] 퀘스트 지급 또는 소모 흐름을 직접 확인했는가?

---

## 15. 기획자가 수정해도 되는 범위

기획자가 직접 수정해도 되는 항목:

- `items.csv`의 아이템 행 추가
- 아이템 이름과 설명
- 아이콘 ID와 카탈로그 Sprite 연결
- 가격
- 아이템 타입
- 최대 중첩 수
- Quest CSV의 지급 및 소모 수량

개발자와 먼저 상의해야 하는 항목:

- 이미 사용 중인 `item_id` 변경 또는 삭제
- 새로운 `item_type` 추가
- 인벤토리 탭 추가
- 저장 데이터 구조 변경
- 아이템 사용 버튼이나 버리기 기능 추가
- 새로운 소비 방식이나 아이템 효과 추가
- CSV 헤더 추가, 삭제, 이름 변경, 순서 변경

판단하기 어렵다면 CSV를 임의로 확장하지 말고 원하는 기획 결과를 개발자에게 설명하세요.

---

## 16. 용어 모음

| 용어 | 뜻 |
| --- | --- |
| CSV | 표 데이터를 저장하는 텍스트 파일 |
| 헤더 | CSV 첫 줄에 있는 각 열의 이름 |
| ID | 데이터를 구분하고 서로 연결하는 고유한 문자열 |
| Key | 다른 파일의 값을 찾기 위해 사용하는 이름 |
| Localization | 여러 언어의 화면 표시 문장을 관리하는 방식 |
| Sprite | Unity UI나 2D 화면에 표시하는 이미지 |
| Catalog | CSV의 이미지 ID와 실제 Unity Sprite를 연결하는 목록 |
| Item Type | 아이템 용도와 인벤토리 탭을 결정하는 분류 |
| Stack | 같은 아이템 여러 개를 한 슬롯에 겹쳐 보관하는 것 |
| Quest Reward | 퀘스트 보상으로 지급하는 데이터 |
| Consume Item | 퀘스트 완료 시 차감되는 아이템 |
| Play Mode | Unity에서 Play 버튼을 눌러 게임을 실행한 상태 |

---

## 17. 현재 프로젝트의 실제 예시

현재 `items.csv`에는 다음 두 아이템이 등록되어 있습니다.

| item_id | 화면 이름 | 타입 | 최대 중첩 |
| --- | --- | --- | ---: |
| `ITEM_COIN` | 코인 | `ITEM_MISC` | 999 |
| `ITEM_REPAIR_KIT` | 복원 도구 | `ITEM_QUEST` | 99 |

`ITEM_COIN`은 `quest_rewards.csv`에서 퀘스트 보상 100개로 지급됩니다.

```csv
QUEST_RESTORE_MURAL,1,REWARD_COINS,quest.restore_mural.reward.coins,,,ITEM_COIN,100
```

`ITEM_REPAIR_KIT`은 `quest_consume_items.csv`에서 퀘스트 완료 시 1개 소모됩니다.

```csv
QUEST_RESTORE_MURAL,0,ITEM_REPAIR_KIT,1
```

새 아이템을 만들 때 이 두 행을 참고하되, 기존 ID를 그대로 복사하지 말고 새 아이템만의 고유 ID와 Localization Key를 만드세요.
