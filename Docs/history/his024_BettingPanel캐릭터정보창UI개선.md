# 히스토리: BettingPanel — CharacterInfoPopup UI 개선 (2026-03-10)

> 브랜치: `main` (워크트리: `claude/keen-khayyam`)
> 전달노트: `Docs/incoming/전달노트_양식_260309_UI적용.md`

---

## 1. TrackInfoPanel + CharacterInfoPopup — 흰색 배경 적용

### 개요
BettingPanel 내 두 패널에 `Img_BG_ListSlot_BG_01` 스프라이트를 배경으로 적용.

### 대상 오브젝트
| 오브젝트 | 경로 | 비고 |
|----------|------|------|
| TrackInfoPanel | BettingPanel/TrackInfoPanel (root) | Image Type: Sliced, Border L2 R2 T2 B2 |
| CharacterInfoPopup | BettingPanel/CharacterInfoPopup (root) | Image Type: Sliced, Border L2 R2 T2 B2 |

### 적용 방법
- 두 오브젝트 모두 기존 Image 컴포넌트를 재사용 (MCP EditPrefabContentsScope)
- 스프라이트: `Assets/Resources/UI/Img_BG_ListSlot_BG_01` (기존 에셋 재사용)
- Image Type → `Sliced`, 별도 border 설정 필요 (Sprite Import 설정에 이미 포함)

### Image Type: Sliced 필요 이유
- 단순 Simple 모드로는 늘릴 때 외곽선 비율이 깨짐
- Sliced 모드: border 영역(L2 R2 T2 B2)은 고정, 중앙만 늘어남
- 버튼·패널 배경에 일관 적용

---

## 2. CharacterInfoPopup Layout 자식 패널 — 배경 투명화

### 문제
root에 흰색 배경을 적용했으나, 자식 Layout 오브젝트들이 자체 어두운 Image를 가지고 있어 흰색이 일부만 보이는 현상.

### 원인
| 오브젝트 | 원래 색상 (RGBA) |
|----------|-----------------|
| Layout1_TopArea | (0.08, 0.08, 0.15, 0.70) |
| Layout2_Left | (0.06, 0.06, 0.12, 0.50) |
| Layout3_Bottom | (0.08, 0.06, 0.04, 0.70) |

### 해결
세 오브젝트 모두 Image color alpha → **0** (완전 투명)으로 변경.
root의 흰색 배경이 전체에 온전히 보이게 됨.

---

## 3. CharacterInfoPopup — IllustrationMask 프레임 효과

### 문제
캐릭터 일러스트가 일러스트레이션 영역에 프레임 없이 노출됨.

### 시도한 방법

#### Option A: 오버레이 Frame 오브젝트 추가 (실패)
- IllustrationMask 위에 `Img_BG_ListSlot` 스프라이트를 가진 Frame 오브젝트 추가
- 속이 꽉 찬 배경 이미지라 일러스트가 덮이는 문제 발생
- **롤백**

#### Option B: IllustrationMask에 배경 + Illustration 인셋 (채택)
- IllustrationMask Image → `Img_BG_ListSlot`, color 흰색
- Illustration: anchorMin=(0,0) anchorMax=(1,1), offsetMin=(4,4) offsetMax=(-4,-4) (4px 인셋)
- Illustration에 AspectRatioFitter가 붙어있어 X축 인셋이 무효화됨 → 사용자가 직접 수동 조정

### 최종 상태
- IllustrationMask: `Img_BG_ListSlot` 배경 (흰색)
- Illustration: Y축 4px 인셋 + 사용자 수동 위치 조정

---

## 4. CharTypeLabel — Image + Text 구조 변환

### 배경
전달노트: `Img_CharType` 스프라이트를 CharTypeLabel 오브젝트에 적용 요청.

### 문제: Unity Graphic 컴포넌트 제약
- Image와 Text 모두 `UnityEngine.UI.Graphic` 상속
- 동일 GameObject에 두 Graphic 컴포넌트 추가 불가
- 에러: `"Can't add 'Image' to CharTypeLabel because a 'Text' is already added to the game object!"`

### 해결: 부모-자식 분리 패턴
기존 `CharTypeLabel`(Text 단일 GO) → `CharTypeLabel`(Image) + `CharTypeLabel/Text`(Text) 구조로 변환.

```
CharacterInfoPopup
 └─ Layout2_Left
      └─ CharTypeLabel          ← Image (Img_CharType, color white)
           └─ Text              ← Text (기존 설정 이전, full stretch)
```

### MCP 적용 절차
1. 기존 Text 컴포넌트의 설정값 저장 (fontSize, color, alignment, font, text 등)
2. `Object.DestroyImmediate(textComponent)` — Text 제거
3. `charTypeLabelGO.AddComponent<Image>()` — Image 추가
4. Img_CharType 스프라이트 할당
5. 자식 "Text" GO 신규 생성 → Text 컴포넌트 추가 → 저장한 설정값 복원
6. Text RectTransform: full stretch (anchorMin=0,0 / anchorMax=1,1 / offset=0)

### CharacterInfoPopup.cs 수정
```csharp
// 변경 전
charTypeLabel = FindText("Layout2_Left/CharTypeLabel");

// 변경 후
charTypeLabel = FindText("Layout2_Left/CharTypeLabel/Text");
```

### 파일 적용
- 스프라이트: `Assets/Design/Incoming/UI/Img_CharType.png` → `Assets/Resources/UI/Img_CharType.png`
- Import: Sprite (2D and UI) / Point (no filter) / None

---

## 5. Inspector 조정 (오너 직접 수정)

| 오브젝트 | 변경 내용 |
|----------|----------|
| CharTypeLabel | anchoredPosition (110, 230), sizeDelta (200, 48) |
| BetDescText | fontSize 32→26, minSize 10→2 |
| Illustration | anchoredPosition X 30 조정 |

---

## 주요 커밋

| 커밋 | 내용 |
|------|------|
| `4a7fff2` | TrackInfoPanel + CharacterInfoPopup 흰색 배경 적용 |
| `fae7ce7` | CharTypeLabel — Img_CharType Image+Text 구조 변환 |
| `0e18f21` | BettingPanel Inspector 조정 (오너) |
