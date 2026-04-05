# 히스토리: BettingPanel TopArea UI — LeftBg/RightBg + 그라데이션 + BetDescText 아웃라인 (2026-03-10)

> 브랜치: `main` (워크트리: `claude/keen-khayyam`)
> 전달노트: `Docs/incoming/전달노트_양식_260309_UI적용.md`

---

## 1. TopArea LeftBg / RightBg — Img_TopBG 배경 패널 추가

### 개요
BettingPanel 상단 TopArea에 `Img_TopBG` 이미지를 좌/우 배경 패널로 분리 배치.
오른쪽은 Scale X=-1(좌우반전)로 미러링.

### 프리팹 구조
```
BettingPanel
  └─ TopArea
       ├─ LeftBg   ← 신규 (anchorMin=0,0 / anchorMax=0.5,1)
       └─ RightBg  ← 신규 (anchorMin=0.5,0 / anchorMax=1,1, Scale X=-1)
```

### RectTransform 설정
| 오브젝트 | anchorMin | anchorMax | Scale |
|----------|-----------|-----------|-------|
| LeftBg  | (0, 0) | (0.5, 1) | (1, 1, 1) |
| RightBg | (0.5, 0) | (1, 1) | (-1, 1, 1) |
- offsetMin/Max = (0, 0) (풀스트레치)

### 스프라이트
- `Assets/Resources/UI/Img_TopBG.png` (Design/Incoming에서 복사)
- Import: Sprite (2D and UI) / Point / None

### 전달노트 형식 (Scale X=-1 미러링 패턴)
```
|Img_TopBG|BettingPanel|TopArea/LeftBg|신규 추가|
|Img_TopBG|BettingPanel|TopArea/RightBg|신규 추가, Scale X=-1 (좌우반전)|
```

---

## 2. Img_TopBG — Smoothstep 알파 그라데이션 적용

### 문제
LeftBg/RightBg 이미지 끝이 딱 잘린 느낌 → 우측(안쪽) 끝을 서서히 투명하게 fade 필요.

### 해결 방법: PNG 픽셀 알파 직접 수정
- 우측 70%~100% 구간: Smoothstep 알파 페이드 (`t = t*t*(3f-2f*t)`)
- MCP 환경에서 `EncodeToPNG`/`ImageConversion` 불가 → Editor 스크립트 파일로 우회

### Editor 도구
`Assets/Scripts/Editor/PNGGradientTool.cs` 신규 작성
- 메뉴: `DopamineRace > Apply TopBG Gradient`
- `fadeStart=0.70f` (우측 30% 구간을 페이드)
- MCP 리플렉션으로 호출: `AppDomain.GetAssemblies()` + `MethodInfo.Invoke()`

> 상세 스펙: `Docs/specs/SPEC-011_PNGGradientTool_명세서_20260310.md`

### 적용 파일
- `Assets/Resources/UI/Img_TopBG.png` — 그라데이션 영구 적용 (PNG 픽셀 직접 수정)

---

## 3. BetDescText — UI.Outline 외곽선 + 흰색 텍스트

### 문제
TopArea 배경 이미지 위에 텍스트(예: "Predict 1st place")가 잘 안 보임 → 가독성 개선 필요.

### 해결: UI.Outline 컴포넌트 추가
- 컴포넌트: `UnityEngine.UI.Outline` (legacy UI 계열 `BaseMeshEffect`)
- effectColor: (0, 0, 0, 0.9) — 검은 외곽선
- effectDistance: (1, -1) — 1px 두께
- 텍스트 색상: 흰색 (기존 검정에서 변경)

### 굵기 조절 방법
`effectDistance`의 절댓값으로 조절:
| effectDistance | 굵기 |
|----------------|------|
| (1, -1) | 얇음 (현재) |
| (2, -2) | 보통 |
| (3, -3) | 굵음 |

### MCP 적용 방법
- `UnityEngine.UI` 네임스페이스 MCP 직접 사용 불가
- `AppDomain.CurrentDomain.GetAssemblies()` 리플렉션으로 Outline 타입 조회
- `betDescGO.AddComponent(outlineType)` — **Transform이 아닌 GameObject에 AddComponent**
  (Transform.AddComponent 없음 주의)

### 오너 Inspector 조정값 (함께 커밋)
- BetDescText fontSize: 32 → 36
- BetDescText anchoredPosition: 미세 조정
- WaterDropDecor dropCount: 5 → 8
- GameSettings mainFont: guid 변경
- LeftBg/RightBg: offset 미세 조정

---

## 주요 커밋

| 커밋 | 내용 |
|------|------|
| `3633d7d` | BettingPanel TopArea — Img_TopBG LeftBg/RightBg 추가 |
| `a9a97fd` | TopBG 그라데이션 + LeftBg/RightBg RectTransform 리셋 |
| `7a8f128` | BetDescText Outline 추가 + Inspector 조정값 반영 |
