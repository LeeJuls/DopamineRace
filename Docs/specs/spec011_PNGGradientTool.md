# SPEC-011: PNGGradientTool — PNG 알파 그라데이션 Editor 도구

**작성일**: 2026-03-10
**상태**: 적용 완료
**관련 파일**: `Assets/Scripts/Editor/PNGGradientTool.cs`

---

## 1. 개요

PNG 이미지의 픽셀 알파값을 직접 수정해 수평 페이드 그라데이션을 적용하는 Editor 전용 도구.
런타임 컴포넌트 없이 PNG 파일 자체를 영구 변경.

### 현재 등록된 타겟
- `Assets/Resources/UI/Img_TopBG.png` (BettingPanel TopArea 배경)
  - fadeStart: 70% — 우측 30% 구간이 서서히 투명해짐

---

## 2. 메뉴

```
DopamineRace > Apply TopBG Gradient
```

---

## 3. 파라미터

| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `assetPath` | string | `"Assets/Resources/UI/Img_TopBG.png"` | 대상 PNG 에셋 경로 |
| `fadeStart` | float | `0.70f` | 페이드 시작 위치 (0~1, 정규화 X 좌표) |

---

## 4. 알고리즘

### Smoothstep 알파 페이드
```csharp
for (int x = fadeStart * width; x < width; x++)
{
    float nx = (float)x / (width - 1);
    float t = (nx - fadeStart) / (1f - fadeStart);
    t = t * t * (3f - 2f * t);  // Smoothstep
    pixel.a = (byte)(pixel.a * (1f - t));
}
```

- `t = 0` (fadeStart 지점): 알파 100% 유지
- `t = 1` (이미지 끝): 알파 0% (완전 투명)
- Smoothstep 곡선: 선형보다 자연스러운 S자 페이드

### 처리 순서
1. TextureImporter: `isReadable = true` 강제 설정 + SaveAndReimport
2. `tex.GetPixels32()` 픽셀 배열 추출
3. X축 순회 → fadeStart 이후 픽셀 알파값 Smoothstep 적용
4. `new Texture2D` → `SetPixels32` → `Apply` → `EncodeToPNG`
5. `File.WriteAllBytes` → PNG 파일 덮어쓰기
6. `isReadable` 원상복구 + `AssetDatabase.Refresh()`

---

## 5. 주의사항

### 비가역적 변경
PNG 파일 자체를 덮어쓰므로 **원본 백업 필수**.
재적용 시 이미 페이드된 이미지에 또 페이드가 쌓임 → 원본에서 재실행할 것.

### MCP 환경 제약
MCP Roslyn 환경에서 `UnityEngine.ImageConversion.EncodeToPNG()` 확장메서드 미포함.
→ Editor 스크립트 파일로 작성 후 리플렉션 (`MethodInfo.Invoke`)으로 MCP에서 호출.

### isReadable 복구
`isReadable=true`로 강제 변경 후, 완료 시 원래 값으로 반드시 복구.
복구 안 하면 빌드 시 불필요한 메모리 비용 발생.

---

## 6. 확장 방법

새 이미지에 그라데이션 적용 시 코드에 메서드 추가:

```csharp
[MenuItem("DopamineRace/Apply XXX Gradient")]
public static void ApplyXXXGradient()
{
    ApplyHorizontalFade("Assets/Resources/UI/Img_XXX.png", fadeStart: 0.60f);
}
```

---

## 7. 관련 문서

- 히스토리: `Docs/history/BettingPanel_TopAreaUI_그라데이션_BetDescText아웃라인_히스토리_20260310.md`
