# SPEC-003 — UI 개선 및 버그 수정 (스킬 설명 · 일러스트 로딩 · 승률 배경)

- **작성일**: 2026-02-23
- **커밋**: `5e73274`
- **이전 커밋**: `c756d1d` (SPEC-002 문서 커밋)

---

## 1. 작업 목적

SPEC-002에서 텍스트 기반 경기기록 UI 구현 후 발견된 UI 버그 수정 및 개선 작업.

## 2. 작업 항목

### 2-1. StringTable 스킬 설명 수정
| 항목 | 내용 |
|------|------|
| **대상** | `Assets/Resources/Data/StringTable.csv` |
| **변경** | 12캐릭터 스킬 설명 "충돌 5회시 공격" → "충돌 5회시 스킬 발동" |
| **범위** | ko / en / jp 3개국어 동시 수정 |
| **키** | `str.char.001~012.skill.desc` |

### 2-2. 캐릭터 일러스트 잘림 현상 수정
| 항목 | 내용 |
|------|------|
| **대상** | `Assets/Scripts/Data/CharacterData.cs` → `LoadIllustration()` |
| **원인** | `.meta` 파일의 `spriteMode: 2` (Multiple) 설정 → `Resources.Load<Sprite>()` 시 sub-sprite(잘린 영역)가 로드됨 |
| **수정** | `Resources.Load<Texture2D>()` + `Sprite.Create()` 로 전체 이미지 로딩 |
| **영향** | CharacterInfoPopup 정보창에서만 사용, 다른 곳에서는 `LoadIcon()` 사용 |

### 2-3. 승률 반투명 배경 박스 (WinRateBg) 추가
| 항목 | 내용 |
|------|------|
| **대상** | `BettingUIPrefabCreator.cs` (Create + Patch), `CharacterInfoPopup.cs` |
| **위치** | Layout2_Left > WinRateBg > WinRateLabel |
| **스타일** | 검정(0,0,0) 50% 알파(0.5), 기존 WinRateLabel을 자식으로 래핑 |
| **Patch 로직** | WinRateLabel이 직접 자식이고 WinRateBg가 없을 때만 래핑 수행 (기존 위치/크기 보존) |
| **호환** | CharacterInfoPopup.Init()에서 WinRateBg 유무에 따라 양쪽 경로 지원 |

### 2-4. 캐릭터 일러스트 이미지 교체
| 항목 | 내용 |
|------|------|
| **대상** | `Assets/Resources/Icon/Char/Character_0~11.png` (12종) |
| **내용** | 일러스트 이미지 최적화 교체 |

## 3. 수정 파일 목록

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Resources/Data/StringTable.csv` | 스킬 설명 12줄 수정 (3개국어) |
| `Assets/Scripts/Data/CharacterData.cs` | LoadIllustration() Texture2D 로딩 |
| `Assets/Scripts/Editor/BettingUIPrefabCreator.cs` | WinRateBg Create + Patch 추가 |
| `Assets/Scripts/Manager/UI/CharacterInfoPopup.cs` | WinRateBg/WinRateLabel 양쪽 경로 호환 |
| `Assets/Prefabs/UI/BettingPanel.prefab` | Patch 적용 결과 |
| `Assets/Prefabs/UI/CharacterItem.prefab` | UI 업데이트 |
| `Assets/Resources/Icon/Char/Character_0~11.png` | 일러스트 12종 교체 |
| `Assets/Pixem/Resources/SampleData.asset` | 샘플 데이터 업데이트 |

## 4. 기술 세부사항

### LoadIllustration() 수정 전/후
```csharp
// 수정 전 — spriteMode=Multiple일 때 sub-sprite 로드 (잘림)
Sprite spr = Resources.Load<Sprite>(path);
if (spr != null) return spr;

// 수정 후 — Texture2D 전체 이미지 → Sprite 생성
Texture2D tex = Resources.Load<Texture2D>(path);
if (tex != null)
    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
        new Vector2(0.5f, 0.5f));
```

### WinRateBg Patch 로직
```
Layout2_Left 탐색
  └─ WinRateLabel이 직접 자식? + WinRateBg 없음?
       → WinRateBg 생성 (기존 위치/크기 복사)
       → WinRateLabel을 WinRateBg 자식으로 이동
       → WinRateLabel을 stretch 앵커로 변경
```
