---
plan: SPEC-049 후속 — Color primaries 경고 근본 제거
date: 2026-06-28
status: ✅ 실행 완료 (2026-06-28) — owner Play 검증 경고 0
scope: A (색 메타만·무손실·fps 유지)
owner_decision: locked
rollback_tag: pre-color-fix
related: SPEC-049, Docs/history/라이브포트레이트_히스토리_20260628.md
---

# 계획 — Color primaries 경고 근본 제거 (스코프 A 확정·무손실)

> 2026-06-28 · SPEC-049 후속 폴리싱 (선택, 차단 아님) · **owner 결정 잠금 + 기술 검증 완료본**

## Context

SPEC-049 라이브 포트레이트 구현 완료(A~G). 런타임에 무해한 경고가 남아 있음:
```
Color primaries 0 is unknown or unsupported by WindowsMediaFoundation.
Falling back to default may result in color shift. (Library/Artifacts/...)
```
- **원인**: 8개 mp4의 색공간 메타(color primaries/transfer/matrix)가 H264 SPS VUI에 미지정(0). WindowsMediaFoundation(WMF)이 디코딩 시 Rec.709로 폴백하며 경고. 재생·기능 정상, 픽셀아트 640²서 색 시프트 체감 불가 → **현재 무해 수용 중**.
- 이 계획은 "콘솔을 완전히 깨끗하게" 만들기 위한 근본 제거안이다.

---

## ✅ 확정 결정 (owner 잠금 — 이대로 실행)

1. **스코프 A — 색 메타만, 무손실.** 비트스트림 필터로 H264 SPS VUI만 재기록(재인코딩 0). **fps는 유지**(24 통일 안 함).
2. **Unity 트랜스코드 OFF 확정.** 원본(메타 수정본)을 직접 재생. 트랜스코드 ON 유지 옵션은 폐기(아래 §핵심 분석 참조).
3. **하이브리드 실행.** ffmpeg 설치 + 최종 Play/빌드 육안 = **owner**. ffmpeg 배치·ffprobe 검증·Unity `.meta` OFF·재임포트·경고 소멸 검증 = **Claude**.
4. **롤백 태그** `pre-color-fix`를 작업 직전 생성. 원본은 git 추적 + `pre-spec049`(`f0f985c`) 태그로도 보호됨.

> fps 통일(스코프 B)은 본 계획에서 **제외**. 향후 필요 시 §부록 B 참고.

---

## 🔬 기술 검증 결과 (2026-06-28, ffmpeg/Unity 공식 문서·Unity 포럼 확인)

### ★ colr atom 함정 — 결론: 우리에게 유리하게 해소됨

H264 색 정보는 두 곳에 존재할 수 있다:
- ① **비트스트림 SPS VUI** (elementary stream 내부)
- ② **mp4 컨테이너 `colr` atom** (`moov/trak/mdia/minf/stbl/stsd/avc1/colr`)

**핵심 검증 사실: WindowsMediaFoundation은 SPS NALU(=SPS VUI)만 읽고, mp4 `colr` atom은 읽지 않는다.**
(출처: Unity Discussions "Color primaries 0..." 스레드 #3 — Unity 직원 답변. WMF는 colr box를 무시하고 SPS 메타만 본다.)

→ **따라서 `h264_metadata` 비트스트림 필터(= SPS VUI를 재기록)가 정확히 WMF가 읽는 쪽을 고친다.** colr atom은 WMF 경고와 무관하므로 본 작업에 필수 아님.
→ 함정의 실제 위험("bsf만으로는 부족"이라는 우려)은 **이 경고에 한해서는 해소.** colr atom은 ffprobe 결과 일관성을 위해 옵션으로만 보강(아래 명령 2안).

**`h264_metadata` 필터는 무손실:** 디코딩 없이 비트스트림 레벨에서 SPS의 VUI 파라미터만 재기록한다(FFmpeg 공식 bitstream-filters 문서 확인). `-c copy`와 함께 쓰면 재압축 0·화질 손실 0.

> ⚠️ 흔한 오해 반박: 일부 포럼 글은 "WMF용 색 메타는 재인코딩(`-vcodec libx264`)해야만 들어간다"고 한다. 이는 `h264_metadata` bsf의 존재를 몰랐던 경우다. bsf는 **재인코딩 없이** SPS VUI를 정확히 고치므로 재인코딩 불필요.

### H.273 코드값 (검증)
- `colour_primaries=1` = **BT.709** (H.273 Table 2, code 1 = CP_BT709) ✔
- `transfer_characteristics=1` = BT.709 (Table 3) ✔
- `matrix_coefficients=1` = BT.709 (Table 4) ✔

### `-c copy` 무손실 (검증)
`-c copy`는 스트림 카피(재인코딩 없음). `-bsf:v h264_metadata`는 비트스트림 레벨 메타 수정만 수행 → 픽셀 데이터 불변. ✔

### Unity 트랜스코드 OFF (검증)
- `VideoClipImporter.defaultTargetSettings`(get/set 프로퍼티) → `VideoImporterTargetSettings.enableTranscoding = false` → **struct 재대입** → `SaveAndReimport()`.
- 기존 `CharacterVideoTranscoder.cs`(단계 E)가 이미 이 패턴(get → 필드 수정 → 재대입 → SaveAndReimport)을 쓴다. OFF는 `enableTranscoding = false`로 대칭 확장.
- `importAudio = false`는 `enableTranscoding`과 독립 설정이라 OFF여도 유지됨(원본은 이미 무음).

### 원본 직접 재생 호환 (검증/명시)
- 원본 8개는 H264·640²·무음(`sourceAudioTrackCount=0`). Windows standalone VideoPlayer(MF backend)는 H264 mp4 직접 재생을 지원한다 → 트랜스코드 OFF여도 직접 재생 OK. 메타 주입 후 SPS VUI가 bt709이므로 경고도 사라진다.
- 단, 빌드 exe 스모크로 1회 실증(owner 육안).

### ffmpeg 설치 경로 (검증/기록)
- `winget install Gyan.FFmpeg` 설치 위치(전체 빌드 동봉):
  `%LOCALAPPDATA%\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-<버전>-full_build\bin\ffmpeg.exe`
  (`ffprobe.exe`도 같은 `bin\`)
- **PATH 함정**: winget이 PATH에 root만 추가하고 `\bin`을 빼는 알려진 버그 존재 → 새 셸에서 `ffmpeg` 직접 호출이 실패할 수 있음. **Claude Bash는 PATH 의존 금지, 항상 풀패스 사용.** `%LOCALAPPDATA%\Microsoft\WinGet\Links\ffmpeg.exe` shim도 후보지만 신뢰성 낮음 → `...\bin\ffmpeg.exe` 풀패스 권장.
- Bash(Git Bash)에서 풀패스 예: `"$LOCALAPPDATA/Microsoft/WinGet/Packages/Gyan.FFmpeg_*/ffmpeg-*-full_build/bin/ffmpeg.exe"` (glob 1회 확정 후 변수로 고정).

---

## ⚠️ 핵심 분석 — 왜 트랜스코드 OFF인가 (가장 중요)

단계 E에서 확인된 사실: 트랜스코드 ON 후 경고 경로가 원본 mp4 → `Library/Artifacts/*`(Unity 변환본)로 바뀌었고 **경고는 그대로**였다. 즉 **Unity의 H264 트랜스코더가 출력물 SPS에 color primaries 메타를 넣지 않는다.**

→ **결론**: 원본 mp4의 색 메타만 고쳐도, 트랜스코드가 ON이면 Unity가 재인코딩하며 메타를 다시 날려 경고가 잔존한다. 따라서 근본 제거는 반드시 **"원본 SPS VUI 메타 수정 + Unity 트랜스코드 OFF(원본 직접 재생)"** 조합이어야 한다 → 결정 #2의 근거.

---

## 현황 (측정값)

| 파일 | fps | 코덱 | color primaries | 크기 |
|------|-----|------|-----------------|------|
| Character_0 | 16 | H264 | 미지정(0) | 480²/640² |
| Character_1 | 32 | H264 | 미지정(0) | |
| Character_2·3·5·7·23·26 | 24 | H264 | 미지정(0) | |

- ffmpeg/ffprobe **미설치**(PATH·흔한 경로 없음). winget 사용 가능.
- 위치: `Assets/Resources/Icon/Char/video/Character_{0,1,2,3,5,7,23,26}.mp4`
- 원본 백업: git 커밋됨 + 태그 `pre-spec049`(`f0f985c`).
- 기존 도구: `Assets/Scripts/Editor/CharacterVideoTranscoder.cs` (단계 E, 트랜스코드 ON 메뉴).

## 목표

`Color primaries 0` 경고 근본 제거 — 8개 mp4 SPS VUI에 bt709 색공간 메타를 **무손실** 주입 + Unity 트랜스코드 OFF로 원본 직접 재생. fps 변경 없음.

---

## 확정 명령 (스코프 A — 검증된 정확 문법)

### ffmpeg 변환 (1순위: SPS VUI만, 무손실)
```bash
ffmpeg -i in.mp4 -map 0 -c copy \
  -bsf:v h264_metadata=colour_primaries=1:transfer_characteristics=1:matrix_coefficients=1 \
  out.mp4
```
- `1,1,1` = BT.709. `-c copy` = 재인코딩 0(무손실). bsf가 SPS VUI 재기록 → WMF가 읽는 쪽을 정확히 충족.
- `-map 0`: 모든 스트림 보존(무음이라 비디오 1개지만 안전).
- **이 명령만으로 WMF 경고 제거 충분**(colr atom 불필요 — §검증 참조).

### (옵션) colr atom까지 일관 — ffprobe 결과 통일용
ffprobe/타 도구에서 컨테이너 메타도 bt709로 보이게 하려면 muxer 색 옵션을 동반(여전히 `-c copy`, 무손실):
```bash
ffmpeg -i in.mp4 -map 0 -c copy \
  -bsf:v h264_metadata=colour_primaries=1:transfer_characteristics=1:matrix_coefficients=1 \
  -color_primaries bt709 -color_trc bt709 -colorspace bt709 \
  -movflags +write_colr \
  out.mp4
```
- `-color_*` 출력 옵션 + `-movflags +write_colr`로 mp4 `colr` atom 기록. 픽셀 미변경(무손실 유지).
- `+write_colr`는 experimental 플래그(스크립트 사용 비권장 표기 있음) → **콘솔 경고 해소만이 목적이면 1순위 명령으로 충분**, 본 옵션은 ffprobe 양쪽 일관성을 원할 때만.

### ffprobe 검증 (정확 문법)
```bash
ffprobe -v error -select_streams v:0 \
  -show_entries stream=color_primaries,color_transfer,color_space \
  -of default=noprint_wrappers=1 out.mp4
```
기대 출력:
```
color_primaries=bt709
color_transfer=bt709
color_space=bt709
```
- 1순위 명령만 쓴 경우: `ffprobe`가 SPS VUI를 읽어 `bt709`로 보고함(컨테이너 colr 없어도). 만약 도구가 colr만 보고 unknown으로 뜨면 옵션 명령으로 재처리.

---

## 실행 절차 (하이브리드 — 역할별 단계)

### Phase 0 — 준비
- **[owner]** `winget install Gyan.FFmpeg` → 설치 완료 통지.
- **[Claude]** 설치 경로 확정: `ls "$LOCALAPPDATA/Microsoft/WinGet/Packages/" | grep FFmpeg` → `...\bin\ffmpeg.exe` 풀패스 변수 고정. `ffmpeg.exe -version` / `ffprobe.exe -version` 풀패스로 동작 확인.
- **[Claude]** 롤백 태그: `git tag pre-color-fix` (작업 직전 main HEAD).

### Phase 1 — 배치 변환 + ffprobe 검증 (Claude)
1. 8개 mp4를 scratchpad로 복사.
2. 각 파일에 1순위 ffmpeg 명령(풀패스) 적용 → scratchpad에 `out` 생성.
3. 각 `out`에 ffprobe 검증 → `color_primaries/transfer/space=bt709` 3종 전부 확인.
4. (선택) 변환 전후 바이트 크기·해상도·fps·길이 비교로 무손실/메타외 변경 없음 확인.
5. 8개 전부 PASS 시 → 원본 위치(`Assets/Resources/Icon/Char/video/`)에 덮어쓰기. (덮어쓰기 전 1개로 Unity 재생 1차 확인 권장.)

### Phase 2 — Unity 트랜스코드 OFF (Claude)
- 기존 `CharacterVideoTranscoder.cs`를 **OFF 모드 메뉴 추가로 확장 권고**(별도 파일보다 단일 출처 유지):
  - 신규 `[MenuItem("DopamineRace/Disable Transcode + Verify Color (SPEC-049 후속)")]`.
  - 8개 각 importer: `var s = imp.defaultTargetSettings; s.enableTranscoding = false; imp.defaultTargetSettings = s; imp.importAudio = false; imp.SaveAndReimport();`
  - **멱등 가드 갱신**: 기존 ON 가드(`s.enableTranscoding && codec==H264 && !importAudio`)와 별개로, OFF 메뉴는 `!s.enableTranscoding && !imp.importAudio`면 skip.
  - struct 재대입 필수(기존 코드 주석대로) — 안 하면 미반영.
  - 단계 E `.meta`(enableTranscoding=true)가 false로 되돌아감.
- 코드 작성·MCP 컴파일·메뉴 실행·재임포트는 Claude(메인 디렉터리 직접 편집 — Unity가 메인에서 실행).

### Phase 3 — 검증 (Claude 자동 + owner 육안)
- **[Claude]** 재임포트 후 콘솔: `Color primaries 0` 경고 0건(원본·Library 양쪽) 확인.
- **[Claude]** StringTable 변경 없음(문자열 무관) — 해당 없음.
- **[owner]** Play → 비디오 캐릭터 클릭 → 경고 소멸·영상 재생/색감/루프/크로스페이드 정상 육안.
- **[owner]** 빌드 스모크(standalone exe)에서 비디오 재생 1회(트랜스코드 OFF 직접재생 호환 실증).
- **[Claude/qa]** 회귀: 8비디오 재생 + 크로스페이드 + 24PNG 폴백(SPEC-049 F 체크리스트 재확인).

### Phase 4 — 커밋 (상위/owner 지시 시)
- `[C] fix(SPEC-049): mp4 SPS VUI bt709 색 메타 무손실 주입 + 트랜스코드 OFF — Color primaries 경고 제거`
- 핸드오프/위키 "알려진 이슈"의 색 경고 항목 해소 기록.

---

## 검증 기준 (성공 조건)

- ffprobe: 8개 전부 `color_primaries=bt709, color_transfer=bt709, color_space=bt709`.
- ffmpeg 변환 무손실: 해상도·fps·길이·프레임 수 변경 없음(메타만 변경).
- Unity importer: 8개 `enableTranscoding=false`, `importAudio=false`.
- Play 런타임: `Color primaries 0` 경고 0건(원본·Library 양쪽).
- 영상 재생/색감/루프/크로스페이드 정상, 회귀 0. fps는 기존값 유지(16/32/24 그대로).

---

## 리스크 / 함정 / 롤백

| 리스크 / 함정 | 대응 |
|--------|------|
| **colr atom vs SPS VUI 혼동** | WMF는 SPS VUI만 읽음(검증). 1순위 명령(bsf)이 정답. colr는 옵션. |
| **"재인코딩해야 한다"는 오해** | `h264_metadata` bsf로 무손실 가능(검증). 재인코딩 불필요. |
| **트랜스코드 ON 잔존 시 경고 미해소** | 트랜스코드 OFF 필수(단계 E 실증). Phase 2 누락 금지. |
| **winget PATH 버그** | Claude Bash는 PATH 미사용, `...\bin\ffmpeg.exe` 풀패스 고정. |
| mp4 덮어쓰기 손상 | git 추적 + `pre-spec049` + 작업 직전 `pre-color-fix` 태그 → `git checkout pre-color-fix -- <경로>` 복구. |
| 트랜스코드 OFF 후 특정 환경 재생 실패 | 원본 H264·무음이라 Windows MF 호환(검증). 빌드 exe 스모크로 실증(owner). |
| ffprobe가 colr만 보고 unknown 표기 | 옵션 명령(`+write_colr` 동반)으로 재처리 — 무손실 유지. |
| ffmpeg 설치 실패 | winget 대안: scoop / 수동 zip(gyan.dev). 미설치 시 보류(무해 수용 유지). |
| `+write_colr` experimental 표기 | 콘솔 경고 해소엔 불필요(1순위로 충분). ffprobe 일관성 원할 때만 사용. |

**롤백**: `git checkout pre-color-fix -- Assets/Resources/Icon/Char/video/` + `.meta` 복구 → 재임포트. 전체 되돌림 시 `git reset --hard pre-color-fix`(미커밋 작업 한정).

---

## 부록 B — 향후 fps 통일 시 참고 (본 계획 범위 밖)

fps 24 통일이 향후 필요해지면(현재 owner 결정: 유지) 재인코딩 필수. 한 패스로 색 메타까지:
```bash
ffmpeg -i in.mp4 -vf fps=24 -an \
  -c:v libx264 -crf 18 -pix_fmt yuv420p \
  -color_primaries bt709 -color_trc bt709 -colorspace bt709 \
  out.mp4
```
- Character_0(16)·1(32)만 대상. 픽셀아트는 프레임 보간 회피 위해 `-sws_flags neighbor` 검토.
- 재인코딩이므로 본 계획(무손실)과 충돌 — 별도 결정·별도 plan으로 분리할 것.

## 참고

- 이 작업은 **선택·비차단**. 안 해도 SPEC-049는 완료 상태이며 경고는 무해.
- 관련: `Wiki/도파민 프로젝트/시스템/라이브_포트레이트.md` "알려진 이슈", `Docs/history/라이브포트레이트_히스토리_20260628.md`.
- 기존 도구: `Assets/Scripts/Editor/CharacterVideoTranscoder.cs`.
