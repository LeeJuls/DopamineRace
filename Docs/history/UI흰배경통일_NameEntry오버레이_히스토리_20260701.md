# UI 흰배경 통일 + NameEntryModal 오버레이 격리 히스토리

> 2026-06-30 ~ 07-01 · OptionPanel·NameEntryModal 흰배경(BG_01) 통일 + 물방울 + 종료후 오버레이 격리

## 작업 요약

모달/패널 패밀리(BetAmountModal·ExchangeModal)가 이미 흰 BG_01 9-슬라이스로 통일돼 있는데
OptionPanel·NameEntryModal만 다크 톤으로 남아 있어 통일. NameEntryModal엔 물방울 장식까지 추가.
이후 다른 PC의 S5 종료플로우(머지)와 합쳐지며 발생한 "Finish+이름입력 모달 겹침"을 오버레이 격리로 해소.

## 구현

| 산출물 | 내용 | 커밋 |
|--------|------|------|
| `OptionPanelPrefabCreator.cs` | BG_01 흰배경 + 다크 팔레트(darkNavy/darkAmber/darkGray) 버튼·토글 | 516441d |
| `NameEntryModalDecorator.cs` (신규) | 국소패치 — 흰배경+물방울(drop8)+다크텍스트, 이후 오버레이 Canvas+백드롭 확장 | 516441d, e0eee3f |
| `NameEntryModal.prefab` | BG_01·물방울·다크텍스트·Canvas(override/1000)·백드롭 0.92 (720×580 보존) | 516441d, e0eee3f |
| `SceneBootstrapper.NameEntry.cs` | 머지 충돌 해소(결과색 다크) + 코드폴백에 Canvas+백드롭 0.92 | cde3e87, e0eee3f |

## 핵심 결정

| 항목 | 결정 | 이유 |
|------|------|------|
| 생성 방식 | **국소패치 데코레이터**(Factory 재생성 금지) | 프리팹 720×580 수동조정값 롤백 방지(his046) |
| 텍스트 팔레트 | OptionPanel 명명상수(darkNavy/Amber/Gray) 복제 | 모달 패밀리 통일(노드별 의미색 쓰는 TrackInfoPanel과 구분) |
| 결과색(RankResult) | 런타임 `SceneBootstrapper.NameEntry.cs` 색을 다크로 | `ShowResult`가 정적 프리팹 색을 런타임 override → 정적만 바꾸면 무효 |
| 종료후 겹침 | **오버레이 유지 + 백드롭 불투명(0.92)** | S5 의도(Finish 위 모달) 유지. finishUI.SetActive(false)는 S5 페이드 코루틴 중단 위험으로 제외 |
| Canvas 격리 | sortingOrder=1000 베이킹 — **에디터는 SerializedObject** | `overrideSorting` 직접대입은 SaveAsPrefabAsset 후 0 저장 quirk(CurrencyUIPrefabCreator 실증) |

## 다른 PC 머지 (S5 종료플로우)

- 다른 PC가 **S5 "게임오버 점수·등수 요약 플로우"**(문서 없이 코드만, 108000b)를 push.
- 머지 충돌 2건 해소: `SceneBootstrapper.NameEntry.cs`(S5 rank 구조 채택+결과색 다크 재적용), `GameSettings.asset`(2라운드 채택).
- **S5 설계**: `ShowFinish`→`TryNameEntryThen`→`RefreshMyRankBadge` (Finish 먼저, 모달 오버레이, 확인후 배지 갱신). 머지 사고 아님 — 의도된 오버레이.

## 검증 (에이전트 교차검토 + Play)

- client·qa 교차검토로 plan 업그레이드(SerializedObject quirk, idempotent 가드, S5 4경로 회귀 TC).
- S0~S3 단계 게이트: 컴파일 0, 프리팹 정적(override/1000·백드롭 0.92·720×580·물방울 보존), 데코레이터 idempotent.
- **오너 Play 스모크 통과** — 흰배경·물방울·다크텍스트 + 종료후 모달이 Finish 위 단독 불투명 표시.

## 잔여 / 후속

- 위키 **S5 종료플로우 페이지 신규**(문서 없이 들어온 흐름 기록) — `/wiki-ingest` 예정.
- 별도 발견 버그: `TryShowBetAmountModal()`이 젤리<1 시 스톤·구제·고양이 무시하고 game over → 후속 수정(방향 B).
