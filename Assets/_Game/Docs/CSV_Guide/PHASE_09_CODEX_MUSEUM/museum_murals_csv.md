# museum_murals.csv

[도감·박물관 가이드](README.md) · [기술](technologies_csv.md)

> 상태: 선택 계획 계약. 벽화 ID와 실제 챕터 배치가 확정될 때 사용하며, 현재 런타임 로더는 없습니다.

## 1. 이 CSV의 목적

박물관의 벽화 한 개를 문명 기술 한 개와 연결합니다. 한 행은 **벽화와 기술의 연결 한 개**입니다. 검은 가림 Renderer, 원본 그림, 빛 효과와 Trigger는 Unity 씬 컴포넌트가 `mural_id`로 연결합니다.

## 2. 언제 작성하는가

실제 박물관 씬의 벽화 ID와 그 벽화를 여는 기술이 확정됐을 때 작성합니다. 콘텐츠가 미확정이면 헤더만 유지합니다.

## 3. 정확한 헤더

```csv
mural_id,chapter,technology_id
```

## 4. 열별 작성 규칙

| 열 이름 | 쉬운 설명 | 필수 여부 | 형식 | 입력 가능 값 | 참조 대상 | 올바른 예 |
| --- | --- | --- | --- | --- | --- | --- |
| `mural_id` | 벽화와 개봉 상태를 찾는 ID | 필수 | 시스템 ID | 전체 벽화에서 고유 | 씬·개봉 완료 Flag | `MURAL_EXAMPLE_CH1` |
| `chapter` | 벽화가 표현하는 챕터 | 필수 | 0 이상 정수 | 연결 기술 챕터와 일치 | 박물관 배치 | `1` |
| `technology_id` | 반환 여부를 확인할 문명 기술 | 필수 | 시스템 ID | 실제 Technology ID | `technologies.csv` | `TECH_EXAMPLE_GLIDE` |

## 5. CodeList 및 입력 가능 값

- 고정 enum은 없습니다.
- `technology_id`: `technologies.csv.technology_id` 동적 목록입니다.
- 같은 기술을 여러 벽화에 연결하지 않습니다.
- `chapter`는 연결 기술의 챕터와 같아야 합니다.
- 챕터 0~2 벽화는 박물관 홀, 챕터 3 벽화는 최종 보상 방에 둔다는 현재 공간 정책은 씬에서 검증합니다.

## 6. 다른 CSV와의 연결

- 참조함: `technology_id` → `technologies.csv`
- 상태 조건: 기술 반환 `skill.returned.{technology_id}`가 참일 때 Trigger 진입으로 개봉 가능
- 별도 저장: `mural_id`별 개봉 연출 완료 상태
- 씬: 같은 `mural_id`를 가진 벽화 컴포넌트가 Renderer, 효과, Trigger를 보관합니다.

기술 반환 상태와 벽화 개봉 완료 상태를 같은 Flag로 사용하지 않습니다.

## 7. 작성 순서

1. Technology와 박물관 공간 배치를 확정합니다.
2. 고유한 `MURAL_...` ID를 정합니다.
3. 같은 챕터의 기술 ID를 연결합니다.
4. Unity 씬 컴포넌트에 같은 Mural ID와 연출 참조를 연결합니다.
5. 반환 전, 반환 후 최초 접근, 저장 후 재진입을 테스트합니다.

## 8. 올바른 예

아래는 형식 설명용이며 실제 벽화 데이터가 아닙니다.

```csv
mural_id,chapter,technology_id
MURAL_EXAMPLE_CH1,1,TECH_EXAMPLE_GLIDE
MURAL_EXAMPLE_CH3,3,TECH_EXAMPLE_ARCHIVE
```

## 9. 잘못된 예

| 잘못된 값 | 이유 |
| --- | --- |
| 없는 Technology ID | 기술 참조 오류 |
| 기술 챕터 1인데 벽화 챕터 2 | 챕터 불일치 |
| 같은 기술을 두 벽화에 등록 | 동일 반환 상태의 중복 벽화 연결 |
| `mural_id=Mural Chapter 1` | 시스템 ID 형식 위반 |
| Renderer 경로를 새 열에 작성 | Unity 전용 참조이며 정확한 헤더 위반 |

## 10. 자동 또는 Unity 검증 항목

Phase 4 구현 시 헤더, 모든 필수값, Mural ID·Technology 중복, 기술 존재, 챕터 일치를 검사합니다. Unity Editor에서는 CSV Mural ID와 씬 컴포넌트의 1:1 연결, 가림/원본/효과/Trigger 참조, 챕터 3의 최종 방 배치를 확인합니다. 현재 자동 검증은 없습니다.

## 11. 수정과 삭제 시 주의사항

`mural_id`는 개봉 완료 저장 Key입니다. 변경하면 로드 후 이미 본 연출이 다시 재생될 수 있습니다. 연결 기술을 바꾸면 기존 반환·개봉 상태의 의미가 달라지므로 출시 후에는 마이그레이션 없이 수정하지 않습니다.

## 12. 빠른 체크리스트

- [ ] 정확한 3열 헤더인가?
- [ ] Mural ID가 고유한 시스템 ID인가?
- [ ] Technology가 존재하고 챕터가 같은가?
- [ ] 같은 기술을 중복 연결하지 않았는가?
- [ ] 씬 컴포넌트와 연출 참조가 연결되었는가?
- [ ] 반환 상태와 개봉 완료 상태를 분리했는가?
- [ ] 챕터 3 벽화가 최종 보상 방에 있는가?

## 13. 관련 문서와 코드

- [기술 CSV](technologies_csv.md)
- [현재 복원 서비스](../../../Scripts/Gameplay/Relics/IRelicRestorationService.cs)
- [Progress 저장 가이드](../../ES3_PROGRESS_SAVE_GUIDE.md)
- [검증과 문제 해결](../PHASE_08_WORKFLOW_QA/VALIDATION_AND_TROUBLESHOOTING.md)

