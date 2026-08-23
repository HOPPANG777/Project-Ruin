# shops.csv 작성 가이드

## 1. 목적

상점 식별자와 구매에 사용할 통화 아이템을 선언한다.

## 2. 언제 작성하는가

아이템을 만든 뒤 상점 기능을 사용할 때 작성한다. 현재 원본은 헤더만 있으며 `GameRoot` 직렬화 연결도 확인되지 않았으므로 도입 시 연결 작업을 별도로 검증한다.

## 3. 정확한 헤더

```csv
shop_id,currency_item_id
```

## 4. 열 규칙

| 열 | 필수 | 형식/검증 |
|---|---:|---|
| `shop_id` | 예 | 시스템 ID, 전역 유일 |
| `currency_item_id` | 예 | 존재하는 아이템 ID, `can_sell=false` |

## 5. CodeList

고정 enum은 없다. 두 값 모두 시스템 ID다.

## 6. 연결 관계

통화는 `items.csv`를 참조한다. 판매 가능한 일반 상품을 통화로 쓰지 않도록 `can_sell=false`가 강제된다. `shop_stock.csv`가 이 상점을 부모로 참조하며, 대화 `OPEN_SHOP` 액션의 `action_id`도 이 `shop_id`를 사용한다.

## 7. 작성 순서

1. 통화 아이템을 `can_sell=false`로 만든다.
2. 상점 ID를 등록한다.
3. 재고 행을 만든다.
4. 대화의 `OPEN_SHOP` 액션을 연결한다.
5. `GameDataController` TextAsset 연결과 구매 UI를 확인한다.

## 8. 유효 예시

```csv
SHOP_ENGINEER,ITEM_CREDIT
```

## 9. 잘못된 예시

| 값 | 이유 |
|---|---|
| 존재하지 않는 `ITEM_CREDIT` | 아이템 참조 오류 |
| `can_sell=true`인 통화 | 통화 정책 위반 |
| 같은 `SHOP_ENGINEER` 두 행 | 상점 ID 중복 |

## 10. 자동/Unity 검증

로더가 ID를, 저장소가 상점 중복·통화 존재·판매 불가 정책을 검사한다. 상점 CSV는 런타임에서 선택 입력이므로 TextAsset 미연결 자체는 필수 입력 오류가 아니다. 현재 프로젝트에 연결하고 대화에서 열리는지는 Unity에서 확인한다.

## 11. 수정·삭제 주의

`shop_id`는 재고, 대화 액션, 저장된 상점별 재고 키에 영향을 준다. 통화 변경은 기존 플레이어 자산 경제와 함께 마이그레이션해야 한다.

## 12. 빠른 체크리스트

- [ ] 상점 ID가 유일하다.
- [ ] 통화 아이템이 존재하고 `can_sell=false`다.
- [ ] 재고와 `OPEN_SHOP` 액션이 같은 ID를 쓴다.
- [ ] TextAsset 연결을 확인했다.
- [ ] 구매/저장/재로드를 플레이했다.

## 13. 관련 문서/코드

- [items.csv](../PHASE_01_LOCALIZATION_ITEMS/items_csv.md)
- [shop_stock.csv](shop_stock_csv.md)
- [대화 선택](../PHASE_02_DIALOGUE/dialogue_choices_csv.md)
- [GameDataController.cs](../../../Scripts/Gameplay/Data/GameDataController.cs)
- [ShopController.cs](../../../Scripts/Gameplay/Shop/ShopController.cs)
- [원본 CSV](../../../Data/Csv/shops.csv)
