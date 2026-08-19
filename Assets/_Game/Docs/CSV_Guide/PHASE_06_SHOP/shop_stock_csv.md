# shop_stock.csv 작성 가이드

## 1. 목적

상점이 판매할 상품, 표시 순서, 최초 유한 재고를 정의한다.

## 2. 언제 작성하는가

부모 상점과 판매 아이템을 만든 뒤 작성한다. 현재 원본은 헤더만이다.

## 3. 정확한 헤더

```csv
shop_id,item_id,display_order,initial_stock
```

## 4. 열 규칙

| 열 | 필수 | 형식/검증 |
|---|---:|---|
| `shop_id` | 예 | 존재하는 상점 ID |
| `item_id` | 예 | 존재하며 `price>0`, `can_sell=true`, 통화와 다른 아이템 |
| `display_order` | 예 | 0 이상 정수, 상점 안에서 유일 |
| `initial_stock` | 예 | 0 이상 정수. `0`은 무한이 아니라 품절 시작 |

같은 상품은 한 상점에 한 번만 등록한다.

## 5. CodeList

고정 enum은 없다. 두 숫자는 부호 없는 10진 정수이며, 표시는 `display_order` 오름차순이다.

## 6. 연결 관계

`shops.csv`와 `items.csv`를 참조한다. 구매는 이 목록을 사용한다. 플레이어가 상점에 판매할 수 있는 아이템은 재고 목록 여부가 아니라 아이템의 `can_sell=true`와 양수 가격 정책으로 결정된다.

## 7. 작성 순서

1. 상품의 가격과 판매 가능 여부를 확인한다.
2. 통화와 다른 아이템만 고른다.
3. 표시 순서를 0부터 배정한다.
4. 의도한 최초 수량을 입력한다.
5. 구매 후 품절과 저장/재로드를 확인한다.

## 8. 유효 예시

```csv
SHOP_ENGINEER,ITEM_PART,0,5
```

## 9. 잘못된 예시

| 값 | 이유 |
|---|---|
| 통화 아이템을 상품으로 지정 | 동일 아이템 불가 |
| `price=0` 또는 `can_sell=false` 상품 | 상품 정책 위반 |
| `initial_stock=-1` | 음수 불가; 무한 재고 표기도 아님 |
| 같은 상점의 상품/순서 중복 | 저장소 중복 오류 |

## 10. 자동/Unity 검증

로더가 정수를, 저장소가 부모·아이템 참조, 통화 차이, 가격/판매 정책, 상점 내부 중복을 검사한다. 런타임은 재고를 유한 수량으로 차감하고 상점+아이템 ID 기반 상태를 저장하므로 구매·저장·재로드를 확인한다.

## 11. 수정·삭제 주의

`shop_id` 또는 `item_id` 변경은 기존 저장 재고와 다른 키로 인식될 수 있다. `initial_stock` 변경이 기존 저장 슬롯의 남은 재고를 덮어쓴다고 가정하지 않는다.

## 12. 빠른 체크리스트

- [ ] 부모 상점과 상품이 존재한다.
- [ ] 상품은 통화와 다르고 양수 가격/판매 가능이다.
- [ ] 순서와 상품이 상점 안에서 유일하다.
- [ ] `0`을 무한 재고로 사용하지 않았다.
- [ ] 저장 후 남은 수량을 확인했다.

## 13. 관련 문서/코드

- [shops.csv](shops_csv.md)
- [items.csv](../PHASE_01_LOCALIZATION_ITEMS/items_csv.md)
- [ID 변경 영향](../PHASE_07_REFERENCE/ID_CHANGE_IMPACT_MAP.md)
- [ShopCsvDefinitionLoader.cs](../../../Scripts/Gameplay/Data/Csv/ShopCsvDefinitionLoader.cs)
- [ShopController.cs](../../../Scripts/Gameplay/Shop/ShopController.cs)
- [원본 CSV](../../../Data/Csv/shop_stock.csv)
