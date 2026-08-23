# localization.csv

[README로 돌아가기](../README.md) · [Items](items_csv.md) · [CodeList Master](../PHASE_00_FOUNDATION/CODELIST_MASTER.md)

## 1. 이 CSV의 목적

화면에 표시할 문장을 Key 하나로 모아 한국어·영어·일본어를 연결합니다. 한 행은 **문장 Key 한 개와 언어별 번역**입니다. 이 파일이 없으면 전체 게임 데이터 초기화가 실패하며, 다른 CSV가 존재하지 않는 Key를 참조해도 Repository 생성이 실패합니다.

현재 20행이 있으며 화자 이름, 대사, 선택지, 퀘스트, 아이템 문장을 담고 있습니다.

## 2. 언제 작성하는가

다른 CSV에 `*_text_key`, `display_name_key`, `body_text_key`를 추가하기 전이나 동시에 작성합니다. 문장 Key를 먼저 정하면 다른 CSV에서 오타를 줄일 수 있습니다.

## 3. 정확한 헤더

```csv
text_key,ko,en,ja
```

헤더 4개와 순서를 바꾸거나 언어 열을 임의로 추가할 수 없습니다.

## 4. 열별 작성 규칙

| 열 이름 | 쉬운 설명 | 필수 여부 | 형식 | 입력 가능 값 | 참조 대상 | 올바른 예 |
| --- | --- | --- | --- | --- | --- | --- |
| `text_key` | 다른 CSV가 문장을 찾는 고유 이름 | 필수 | Localization Key | `^[a-z][a-z0-9._]*$` | 여러 CSV가 이 값을 참조 | `item.coin.name` |
| `ko` | 한국어 원문 | 필수 | 문자열 | 공백만인 값 금지, `\n` 줄바꿈 가능 | 화면 표시 | `첫째 줄\n둘째 줄` |
| `en` | 영어 번역 | 선택 | 문자열 | 빈칸 가능 | 영어 표시, 없으면 다른 언어 fallback | `Coin` |
| `ja` | 일본어 번역 | 선택 | 문자열 | 빈칸 가능 | 일본어 표시, 없으면 fallback | `コイン` |

`text_key`는 전체 파일에서 중복할 수 없습니다. 모든 열은 기획자가 작성할 수 있으며 Unity 별도 연결은 필요하지 않습니다.

## 5. CodeList 및 입력 가능 값

이 CSV에는 고정 enum CodeList가 없습니다.

- `text_key`: 소문자 시작, 소문자·숫자·`.`·`_`만 허용
- `ko`: 필수 문장
- `en`, `ja`: 선택 문장
- 줄바꿈: 원본 셀에는 `\n`을 권장하며 런타임에서 실제 줄바꿈으로 변환
- 리터럴 역슬래시: `\\`

현재 Key 명명 패턴:

- `actor.<name>.name`
- `dialogue.<speaker>.<flow>.<number-or-purpose>`
- `quest.<quest>.title|description|objective.<name>|reward.<name>`
- `item.<item>.name|description`

이는 현재 데이터의 일관된 패턴이며 접미어 자체를 코드가 강제하지는 않습니다.

## 6. 다른 CSV와의 연결

이 파일은 문장 데이터의 부모입니다.

- Items: `display_name_key`, `description_text_key`
- Dialogue Actors·Portraits: `display_name_key`
- Dialogue Nodes·Choices: `body_text_key`, `choice_text_key`
- Quests·Objectives·Rewards: 모든 `*_text_key`

참조가 깨지면 Repository 생성이 중단됩니다. `text_key`를 변경하거나 삭제하기 전에는 [교차 참조 지도](../PHASE_07_REFERENCE/CROSS_REFERENCE_MAP.md)를 확인합니다.

## 7. 작성 순서

1. 어떤 화면 문장인지 한 문장으로 정의합니다.
2. 기존 Key와 중복되지 않는 소문자 Key를 정합니다.
3. `ko`를 작성합니다.
4. 준비된 번역만 `en`, `ja`에 쓰고 나머지는 완전히 비웁니다.
5. 다른 CSV의 Key 열에 같은 문자열을 복사합니다.
6. 검증기에서 누락 Key 오류가 없는지 확인합니다.

## 8. 올바른 예

```csv
text_key,ko,en,ja
item.coin.name,코인,Coin,
dialogue.curator.notice,첫째 줄\n둘째 줄,First line\nSecond line,
```

## 9. 잘못된 예

| 잘못된 값 | 이유 |
| --- | --- |
| `Item.Coin.Name` | 대문자 사용 |
| `.item.coin.name` | 소문자로 시작하지 않음 |
| `item-coin-name` | 하이픈 금지 |
| `ko` 빈칸 | 한국어 필수 |
| 같은 `text_key` 두 행 | 전체 파일에서 중복 금지 |
| 선택 번역에 `NULL` | 선택값은 완전 빈칸으로 표현 |

## 10. 자동 또는 Unity 검증 항목

- 필수 TextAsset 연결, 정확한 헤더 4개와 순서
- 모든 행의 열 개수
- `text_key` 형식과 중복
- `ko`가 비거나 공백뿐인지
- 다른 CSV가 참조한 Key의 존재 여부

`en`·`ja`가 비면 오류가 아니며 현재 언어 문장이 없을 때 한국어, 영어, 일본어 순으로 가능한 문장을 사용합니다.

## 11. 수정과 삭제 시 주의사항

표시 문장 자체를 바꾸는 것은 ID 변경보다 안전합니다. `text_key`를 바꾸거나 삭제하면 참조하는 모든 CSV도 함께 수정해야 합니다. Key는 현재 저장 데이터의 주 식별자는 아니지만 콘텐츠 연결을 즉시 끊습니다.

## 12. 빠른 체크리스트

- [ ] 헤더가 정확히 4열인가?
- [ ] `text_key`가 소문자 규칙을 지키는가?
- [ ] 중복 Key가 없는가?
- [ ] `ko`가 비어 있지 않은가?
- [ ] 없는 번역은 가짜 값 대신 빈칸인가?
- [ ] 문장 줄바꿈을 `\n`으로 썼는가?
- [ ] 참조 CSV와 Key 철자가 완전히 같은가?

## 13. 관련 문서와 코드

- [공통 작성 규칙](../PHASE_00_FOUNDATION/COMMON_AUTHORING_RULES.md)
- [교차 참조 지도](../PHASE_07_REFERENCE/CROSS_REFERENCE_MAP.md)
- [Localization 로더](../../../Scripts/Gameplay/Data/Csv/LocalizationCsvDefinitionLoader.cs)
- [줄바꿈 변환](../../../Scripts/Core/Data/Csv/CsvTextUtility.cs)
- [실제 CSV](../../../Data/Csv/localization.csv)

