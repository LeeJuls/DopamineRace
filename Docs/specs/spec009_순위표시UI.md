# SPEC-009: 순위 표시 UI 규격

**작성일**: 2026-03-06
**상태**: 적용 완료

---

## 1. 거리별 최근 순위 표시 (CharacterInfoPopup)

### 표시 개수
- 거리 유형별 (단거리/중거리/장거리) 최근 **5개** 까지 표시
- 기존: 6개 (창 밖으로 잘리는 문제 있었음)
- 상수: `CharacterInfoPopup.MAX_DIST_DISPLAY = 5`

### 순위 포맷
- `Loc.GetRank(rank)` → `str.hud.rank` StringTable 키 사용
- 순위별 색상 (Rich Text):
  - 1착: `#FFD700` (금)
  - 2착: `#C0C0C0` (은)
  - 3착: `#CD7F32` (동)
  - 4착+: `#CCCCCC` (회색)

---

## 2. 다국어 순위 포맷 규격 (str.hud.rank)

### 원칙
> **숫자 + 1자리 접미사** 형태 유지. 2자리 이상 접미사 사용 금지.

| 언어 | 포맷 | 예시 |
|------|------|------|
| ko (한국어) | `{0}위` | 1위, 2위 |
| en (영어) | `{0}.` | 1., 2., 3. |
| ja (일본어) | `{0}位` | 1位, 2位 |
| zh_CN (중국어) | `{0}名` | 1名, 2名 |
| de (독일어) | `{0}.` | 1., 2., 3. |
| es (스페인어) | `{0}º` | 1º, 2º |
| br (포르투갈어) | `{0}º` | 1º, 2º |

### 영어 처리 방식
- `Loc.GetRank()` 에서 언어별 분기 제거 → 모든 언어 StringTable 통일
- 기존 코드 오버라이드 (1st/2nd/3rd/4th) 삭제됨

### zh_CN 변경 이력
- 구: `第{0}名` → "第1名" (접두사+접미사 혼합, 글자 3개)
- 신: `{0}名` → "1名" (접미사 1자리로 통일)

---

## 3. 구현 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/Manager/UI/CharacterInfoPopup.cs` | MAX_DIST_DISPLAY=5, FormatRankList() |
| `Assets/Scripts/Utility/Loc.cs` | GetRank() — StringTable 통일 |
| `Assets/Resources/Data/StringTable.csv` | str.hud.rank 각 언어 포맷 |
