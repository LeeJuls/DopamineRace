# HIST-003 — UI 개선 및 버그 수정 작업 히스토리

- **작업일**: 2026-02-23
- **커밋**: `5e73274`
- **이전 커밋**: `c756d1d`

---

## 1. 작업 과정

### Phase 1: StringTable 스킬 설명 수정
1. 사용자 요청: "충돌 5회시 공격" → "충돌 5회시 스킬 발동"
2. StringTable.csv에서 `str.char.001~012.skill.desc` 12줄 수정
3. 영어: "Attack after 5 collisions" → "Skill activates after 5 collisions"
4. 일본어: "5回衝突で攻撃" → "5回衝突でスキル発動"
5. **결과**: 정상 반영 확인

### Phase 2: 캐릭터 일러스트 잘림 현상 수정
1. **문제 발견**: CharacterInfoPopup에서 캐릭터 이미지가 확대/잘려서 표시됨
2. **원인 분석**: `.meta` 파일에 `spriteMode: 2` (Multiple) 설정 → `Resources.Load<Sprite>()` 시 sub-sprite가 로드
3. **해결 방안 검토**:
   - (A) `.meta` 파일 수정 → 다른 곳에서도 영향 줄 수 있어 제외
   - (B) `Texture2D` 로딩 → CharacterInfoPopup에서만 사용하므로 안전
4. **수정**: `CharacterData.LoadIllustration()` → `Texture2D` + `Sprite.Create()`
5. **결과**: 전체 이미지 정상 표시 확인 (스크린샷 검증)

### Phase 3: 승률 반투명 배경 박스 (WinRateBg)
1. **사용자 요청**: 승률 텍스트에 검은색 50% 반투명 배경 박스 추가
2. **구현 방식**: WinRateBg(Image 컴포넌트) 생성 → 기존 WinRateLabel을 자식으로 래핑
3. **BettingUIPrefabCreator.cs 수정**:
   - `CreateBettingPanelPrefab()`: WinRateBg 생성 후 WinRateLabel을 자식으로 배치
   - `PatchPrefabs()`: 기존 프리팹의 WinRateLabel을 WinRateBg로 래핑하는 Safe Patch 로직 추가
4. **CharacterInfoPopup.cs 수정**: Init()에서 WinRateBg 유무에 따라 양쪽 경로 지원
5. **사용자 우려**: "프리팹 safe 다시 실행하면 기존 세팅 다 날라갈텐데?"
   - → Safe Patch는 Find로 기존 요소 확인 후 없을 때만 생성하므로 안전함을 설명
6. **결과**: WinRateBg 정상 표시 확인

### Phase 4: 캐릭터 일러스트 이미지 12종 교체
1. 캐릭터 일러스트 이미지 최적화 교체
2. Character_0~11.png 12개 파일 업데이트

## 2. 발견된 문제 및 해결

| 문제 | 원인 | 해결 |
|------|------|------|
| 일러스트 잘림 | spriteMode: 2 (Multiple) → sub-sprite 로드 | Texture2D 로딩 + Sprite.Create() |
| 승률 배경 없음 | WinRateLabel에 배경 컴포넌트 부재 | WinRateBg(Image) 래핑 |
| Patch 안전성 우려 | 기존 수동 설정 덮어쓰기 가능성 | Find() 검사 후 없을 때만 생성 |

## 3. 사용자 피드백 기반 가이드

- **WinRateBg 투명도 조절**: BettingPanel.prefab > infoPopup > Layout2_Left > WinRateBg > Image > Color > Alpha 값
- **WinRateBg 크기 조절**: BettingPanel.prefab > WinRateBg > RectTransform > Width/Height
- **BetOrderLabel 폰트 크기**: CharacterItem.prefab > BetOrderLabel > Text > Font Size
- **캐릭터 이미지 크기**: 프리팹의 IllustImage RectTransform에서 조절 (코드가 아닌 프리팹에서)

## 4. 테스트 결과

| 항목 | 결과 |
|------|------|
| 스킬 설명 변경 (ko/en/jp) | ✅ 정상 |
| 일러스트 전체 이미지 표시 | ✅ 정상 (스크린샷 확인) |
| WinRateBg 반투명 배경 | ✅ 정상 (스크린샷 확인) |
| Safe Patch 기존 설정 보존 | ✅ 정상 |
| CharacterInfoPopup 패치 전/후 호환 | ✅ 양쪽 경로 동작 |

## 5. 최종 파일 변경 목록

```
M  Assets/Resources/Data/StringTable.csv
M  Assets/Scripts/Data/CharacterData.cs
M  Assets/Scripts/Editor/BettingUIPrefabCreator.cs
M  Assets/Scripts/Manager/UI/CharacterInfoPopup.cs
M  Assets/Prefabs/UI/BettingPanel.prefab
M  Assets/Prefabs/UI/CharacterItem.prefab
M  Assets/Resources/Icon/Char/Character_0~11.png (12종)
M  Assets/Pixem/Resources/SampleData.asset
```
