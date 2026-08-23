# items.csv

[README로 돌아가기](../README.md) · [Localization](localization_csv.md) · [Shop](../PHASE_06_SHOP/shops_csv.md)

## 1. 이 CSV의 목적

인벤토리 아이템의 ID, 표시 문장, 아이콘 ID, 가격, 판매 가능 여부, 종류, 중첩 수를 정의합니다. 한 행은 **아이템 종류 한 개**입니다. Quest 보상·소모와 Shop의 화폐·상품이 이 파일을 참조합니다.

현재 2행(`ITEM_COIN`, `ITEM_REPAIR_KIT`)이며 둘 다 `can_sell=false`, `price=0`입니다.

## 2. 언제 작성하는가

새 아이템을 만들거나 이름·가격·분류·중첩 정책을 바꿀 때 작성합니다. 새 행을 넣기 전에 이름과 설명 Localization Key를 준비하고, Shop 상품이면 판매 정책을 먼저 결정합니다.

## 3. 정확한 헤더

```csv
item_id,display_name_key,description_text_key,icon_id,price,can_sell,item_type,max_stack_count
```

`can_sell`은 현재 필수 6번째 열입니다. 과거 7열 문서나 Excel을 그대로 내보내면 안 됩니다.

## 4. 열별 작성 규칙

| 열 이름 | 쉬운 설명 | 필수 여부 | 형식 | 입력 가능 값 | 참조 대상 | 올바른 예 |
| --- | --- | --- | --- | --- | --- | --- |
| `item_id` | 게임과 저장 데이터가 아이템을 찾는 ID | 필수 | 시스템 ID | 전체 Items에서 고유 | Quest·Shop·Inventory | `ITEM_COIN` |
| `display_name_key` | 화면 이름 Key | 필수 | Localization Key | 실제 `text_key` | `localization.csv` | `item.coin.name` |
| `description_text_key` | 설명 문장 Key | 필수 | Localization Key | 실제 `text_key` | `localization.csv` | `item.coin.description` |
| `icon_id` | Unity 아이콘 연결 ID | 선택 | 시스템 ID | 빈칸 또는 Catalog ID | `ItemVisualCatalogSO` | `ICON_COIN` |
| `price` | 구매·판매 단가 | 필수 | 0 이상 정수 | `can_sell=true`이면 1 이상 | ShopController | `50` |
| `can_sell` | 플레이어가 상점에 판매할 수 있는지 | 필수 | Boolean | `true`, `false` | Shop 정책 | `false` |
| `item_type` | 용도와 인벤토리 분류 | 필수 | 고정 CodeList | 아래 6종 | 인벤토리 UI | `ITEM_MISC` |
| `max_stack_count` | 한 슬롯 최대 수량 | 필수 | 1 이상 정수 | 1 이상 | Inventory 저장·표시 | `99` |

기획자는 모든 문자열·숫자를 작성할 수 있습니다. 실제 Sprite는 Unity 담당자가 Catalog에 연결합니다. `icon_id` 누락은 허용되며, 값은 있는데 Catalog에 없으면 Editor 경고입니다.

## 5. CodeList 및 입력 가능 값

`item_type`:

- `ITEM_QUEST`
- `ITEM_ALCOHOL_COLLECTIBLE`
- `ITEM_ALCOHOL_TOOL`
- `ITEM_RELIC_COLLECTIBLE`
- `ITEM_RELIC_TOOL`
- `ITEM_MISC`

`can_sell`: `true`, `false`

가격 정책:

| 역할 | `can_sell` | `price` |
| --- | --- | ---: |
| 상점 화폐 | `false` | 0 이상 |
| 판매 가능한 상품 | `true` | 1 이상 |
| 비판매 아이템 | `false` | 0 이상 |

동적 목록은 `display_name_key`와 `description_text_key`가 `localization.csv.text_key`, `icon_id`가 Unity Catalog입니다.

## 6. 다른 CSV와의 연결

- 참조함: 이름·설명 → `localization.csv`
- 참조받음: `quest_rewards.item_id`, `quest_consume_items.item_id`, `shops.currency_item_id`, `shop_stock.item_id`
- Unity: `icon_id` → `ItemVisualCatalogSO`
- 저장: Inventory Slot은 `item_id`와 수량을 저장하며, Shop 재고 상태 ID에도 상품 `item_id`가 포함됩니다.

## 7. 작성 순서

1. 중복되지 않는 `ITEM_...` ID를 정합니다.
2. Localization에 이름과 설명 행을 만듭니다.
3. `icon_id`가 있으면 Unity 담당자와 Catalog ID를 합의합니다.
4. Shop 거래 여부를 정하고 `price`와 `can_sell`을 함께 작성합니다.
5. `item_type`과 `max_stack_count`를 정합니다.
6. Quest·Shop의 참조 행을 나중에 작성합니다.

## 8. 올바른 예

```csv
item_id,display_name_key,description_text_key,icon_id,price,can_sell,item_type,max_stack_count
ITEM_COIN,item.coin.name,item.coin.description,,0,false,ITEM_QUEST,999
ITEM_MUSEUM_CARD,item.museum_card.name,item.museum_card.description,ICON_MUSEUM_CARD,25,true,ITEM_MISC,10
```

두 번째 행은 예시이며 현재 실제 CSV에는 없습니다.

## 9. 잘못된 예

| 잘못된 값 | 이유 |
| --- | --- |
| `ITEM_TOOL,...,0,true,...` | 판매 가능인데 가격 0 |
| `can_sell=TRUE` | 소문자 Boolean만 허용 |
| `item_type=QUEST` | 여섯 고정값 중 아님 |
| `max_stack_count=0` | 1 이상 필요 |
| 없는 Localization Key | Repository 참조 오류 |
| `icon_id=coin.png` | 시스템 ID 형식이 아니며 파일 경로 열도 아님 |

## 10. 자동 또는 Unity 검증 항목

- 정확한 8열 헤더와 필수값
- 시스템 ID·Localization Key 형식
- `item_id` 중복
- `price >= 0`, `max_stack_count >= 1`
- `can_sell=true`일 때 `price > 0`
- `item_type` 6종
- 이름·설명 Key 존재
- Unity Editor 경고: `icon_id`가 Catalog에 없거나 Sprite가 비어 있음

## 11. 수정과 삭제 시 주의사항

`item_id`는 Quest, Shop, Inventory 저장 데이터의 연결 기준입니다. 변경·삭제 전에 네 CSV 참조와 기존 세이브 마이그레이션을 확인합니다. `can_sell`이나 `price`를 바꾸면 모든 상점 정책 검증에 영향을 줍니다. 화폐 아이템을 `true`로 바꾸거나 Stock 상품을 `false`로 바꾸면 Repository 생성이 실패합니다.

## 12. 빠른 체크리스트

- [ ] 헤더에 `can_sell`이 포함된 8열인가?
- [ ] `item_id`가 고유한 시스템 ID인가?
- [ ] 이름·설명 Key가 실제로 존재하는가?
- [ ] `price`와 `can_sell` 조합이 맞는가?
- [ ] `item_type`이 6종 중 하나인가?
- [ ] `max_stack_count`가 1 이상인가?
- [ ] Quest·Shop 참조를 확인했는가?
- [ ] 아이콘 ID를 썼다면 Unity Catalog 연결을 요청했는가?

## 13. 관련 문서와 코드

- [CodeList Master](../PHASE_00_FOUNDATION/CODELIST_MASTER.md)
- [ID 변경 영향](../PHASE_07_REFERENCE/ID_CHANGE_IMPACT_MAP.md)
- [Item 로더](../../../Scripts/Gameplay/Data/Csv/ItemCsvDefinitionLoader.cs)
- [Repository 정책](../../../Scripts/Gameplay/Data/Repository/GameDataRepository.cs)
- [실제 CSV](../../../Data/Csv/items.csv)

