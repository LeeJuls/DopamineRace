# 계획 — Color primaries 근본 제거 + (선택) fps 통일

> 2026-06-28 · SPEC-049 후속 폴리싱 (선택, 차단 아님) · **코딩 전 계획안**

## Context

SPEC-049 라이브 포트레이트 구현 완료(A~G). 런타임에 무해한 경고가 남아 있음:
```
Color primaries 0 is unknown or unsupported by WindowsMediaFoundation.
Falling back to default may result in color shift. (Library/Artifacts/...)
```
- **원인**: 8개 mp4의 색공간 메타(color primaries/transfer/matrix)가 미지정(0). WindowsMediaFoundation이 디코딩 시 Rec.709로 폴백하며 경고. 재생·기능 정상, 픽셀아트 640²서 색 시프트 체감 불가 → **현재 무해 수용 중**.
- 이 계획은 "콘솔을 완전히 깨끗하게" 하고 싶을 때의 근본 제거안 + fps 통일(선택).

## ⚠️ 핵심 분석 — 트랜스코드 상호작용 (가장 중요)

단계 E에서 확인된 사실: 트랜스코드 ON 후 경고 경로가 원본 mp4 → `Library/Artifacts/*`(Unity 변환본)로 바뀌었고 **경고는 그대로**였다. 즉 **Unity의 H264 트랜스코더가 출력물에 color primaries 메타를 넣지 않는다.**

→ **결론**: 원본 mp4의 색 메타만 고쳐도, Unity 트랜스코드가 ON이면 Unity가 재인코딩하며 메타를 다시 날려 경고가 잔존할 수 있다. 따라서 근본 제거는 반드시 **"원본 메타 수정 + Unity 트랜스코드 OFF(원본 직접 재생)"** 조합이어야 한다.

- 트랜스코드 OFF여도 `importAudio=false`는 독립 설정이라 유지됨. 원본은 이미 H264·무음(sourceAudioTrackCount=0)이라 Windows MF 직접 재생 호환.
- 단, 트랜스코드 OFF 시 단계 E의 `.meta`(enableTranscoding=true)를 되돌려야 함.

## 현황 (측정값)

| 파일 | fps | 코덱 | color primaries | 크기 |
|------|-----|------|-----------------|------|
| Character_0 | 16 | H264 | 미지정(0) | 480²/640² |
| Character_1 | 32 | H264 | 미지정(0) | |
| Character_2·3·5·7·23·26 | 24 | H264 | 미지정(0) | |

- ffmpeg/ffprobe **미설치**(PATH·흔한 경로 없음). winget 사용 가능.
- 위치: `Assets/Resources/Icon/Char/video/Character_{0,1,2,3,5,7,23,26}.mp4`
- 원본 백업: git에 커밋돼 있음 + 태그 `pre-spec049`(`f0f985c`).

## 목표

1. (필수) `Color primaries 0` 경고 근본 제거 — 8개 mp4에 bt709 색공간 메타 주입.
2. (선택) fps 24 통일 — Character_0(16)·Character_1(32) → 24. 나머지 6개는 이미 24.

## 방안 — 2가지 스코프 (owner 선택)

### 스코프 A — 색 메타만 (무손실, 권장 기본)
fps는 손대지 않고 색 메타만. **재인코딩 없이 비트스트림 필터로 SPS VUI만 재기록 → 화질 손실 0·빠름.**
```
ffmpeg -i in.mp4 -c copy \
  -bsf:v h264_metadata=colour_primaries=1:transfer_characteristics=1:matrix_coefficients=1 \
  out.mp4
```
- `1` = bt709. 스트림 카피라 화질·용량 거의 불변.

### 스코프 B — 색 메타 + fps 24 통일 (재인코딩)
fps 변경은 재인코딩 필수. 한 패스로 색 메타까지 설정.
```
ffmpeg -i in.mp4 -vf fps=24 -an \
  -c:v libx264 -crf 18 -pix_fmt yuv420p \
  -color_primaries bt709 -color_trc bt709 -colorspace bt709 \
  out.mp4
```
- Character_0·1만 fps 변경 필요하지만, 일관성 위해 8개 전체 재인코딩도 가능(약간의 화질 재압축 발생, crf 18이면 사실상 무손상).
- 픽셀아트라 `-sws_flags neighbor`(fps 변경 시 프레임 보간 없음) 검토.

**권장**: 콘솔 클린이 목적이면 **스코프 A**(무손실·최소위험). fps 불일치가 실제로 거슬릴 때만 B.

## 실행 절차 (실제 작업 시)

0. **ffmpeg 설치**: `winget install Gyan.FFmpeg` → 새 셸에서 `ffmpeg -version` 확인.
1. **세이프포인트**: 현재 main 커밋됨 + 태그 `pre-spec049` 존재. 추가로 작업 브랜치 또는 `git tag pre-color-fix` 권장.
2. **배치 변환**: 8개 mp4를 scratchpad로 복사 → ffmpeg 변환(스코프 A 또는 B) → 결과 검증(ffprobe로 `color_primaries=bt709` 확인) → 원본 위치에 덮어쓰기.
3. **Unity 트랜스코드 OFF**: `CharacterVideoTranscoder`를 "트랜스코드 해제" 모드로 쓰거나, 신규 메뉴/직접 `.meta` 수정으로 8개 `enableTranscoding=false` (importAudio=false 유지). → 원본(메타 수정본) 직접 재생.
   - 대안 검토: 트랜스코드 ON 유지 + Unity가 메타 보존하는지 1개 테스트. 보존 안 하면 OFF 확정.
4. **재임포트** → Unity가 변경 mp4 인식.
5. **검증**: Play → 비디오 캐릭터 클릭 → `Color primaries` 경고 **소멸** 확인. 영상 재생·색감·루프 정상.
6. **회귀**: 8비디오 재생 + 크로스페이드 + 24PNG 폴백 정상(SPEC-049 F 체크리스트 재확인).
7. **커밋**: `[C] fix/chore: SPEC-049 후속 — mp4 색공간 메타 주입(+fps 통일)` + 핸드오프/위키 "알려진 이슈" 해소 기록.

## 검증 기준 (성공 조건)

- ffprobe: 8개 전부 `color_primaries=bt709, color_transfer=bt709, color_space=bt709`.
- Play 런타임: `Color primaries 0` 경고 0건(원본·Library 양쪽).
- 영상 재생/색감/루프/크로스페이드 정상, 회귀 0.
- (스코프 B) fps 8개 모두 24.

## 리스크 / 롤백

| 리스크 | 대응 |
|--------|------|
| mp4 덮어쓰기 손상 | git 추적 + `pre-spec049` 태그 → `git checkout -- <경로>` 복구. 작업 전 `pre-color-fix` 태그 추가 |
| 재인코딩 화질 저하(스코프 B) | crf 18·pix_fmt yuv420p, 픽셀아트는 neighbor 스케일. A는 무손실이라 무관 |
| 트랜스코드 OFF 후 특정 환경 재생 실패 | 원본 H264·무음이라 Windows MF 호환. 빌드 exe 스모크로 확인 |
| ffmpeg 설치 실패 | winget 대안: scoop/수동 zip(gyan.dev). 미설치 시 작업 보류(무해 수용 유지) |

## 미결정 (owner 선택 필요)

1. **스코프 A(색만, 무손실) vs B(색+fps 통일, 재인코딩)** — 권장 A.
2. **fps 통일 여부** — 16/32 2개만 24로? 아니면 전체 재인코딩? 아니면 fps 유지?
3. **트랜스코드 처리** — OFF 확정 vs ON 유지 후 메타 보존 테스트.
4. **실행 주체** — ffmpeg 배치를 client/build 에이전트 위임 vs owner 직접. (Unity 재임포트·.meta는 MCP/메뉴, ffmpeg는 Bash)

## 참고

- 이 작업은 **선택·비차단**. 안 해도 SPEC-049는 완료 상태이며 경고는 무해.
- 관련: `Wiki/도파민 프로젝트/시스템/라이브_포트레이트.md` "알려진 이슈", `Docs/history/라이브포트레이트_히스토리_20260628.md`.
