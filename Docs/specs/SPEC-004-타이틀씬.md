# SPEC-004 — TitleScene 신규 생성 + 씬 전환 시스템

- **작성일**: 2026-02-23
- **상태**: 확정 (피드백 반영 완료)
- **원본 기획서**: `Docs/타이틀씬 추가.md`

---

## 1. 개요

게임 실행 시 가장 먼저 표시되는 타이틀 화면.
배경 이미지 + 타이틀 로고 + "Press to Start" + 하단 캐릭터 달리기 + 언어 선택 버튼으로 구성.
입력 시 블록 디졸브 전환으로 기존 SampleScene(배팅 화면)으로 이동.

---

## 2. 피드백 회의 결과 (원본 기획서 대비 변경점)

| # | 원본 명세 | 확정 |
|---|----------|------|
| 1 | 언어 변경 UI 없음 | **우하단 언어 선택 버튼 추가** (ko/en/jp) |
| 2 | 씬 이름 "GameScene" | **SampleScene 유지** (rename 리스크 회피) |
| 3 | "CharacterData의 run 스프라이트" | **기존 프리팹 Instantiate + RacerController 비활성화 + Animator "Run"** |
| 4 | BGMManager "확장" | **재작성** (DontDestroyOnLoad, FadeIn/Out, PlayBGM) |
| 5 | TrackTransition 관계 미정 | **공존** (TrackTransition 변경 없음, SceneTransitionManager 신규) |
| 6 | SceneManager.LoadScene | **LoadSceneAsync + allowSceneActivation** |
| 7 | Loc.cs 동적 언어 목록 | **현재 하드코딩 유지** (추가 시 수동 확장) |
| 8 | 타이틀 복귀 경로 | **불필요** (단방향만) |

---

## 3. 씬 구조

```
TitleScene (새로 생성)  →  [블록 디졸브 전환]  →  SampleScene (기존)
```

### Build Settings
| Index | 씬 | 역할 |
|-------|-----|------|
| 0 | TitleScene | 타이틀 화면 (게임 진입점) |
| 1 | SampleScene | 기존 게임 루프 (Betting → Racing → Result → Finish) |

### DontDestroyOnLoad 전략

TitleScene에서 아래 3개를 **먼저 생성 + DontDestroyOnLoad** 적용:

| 매니저 | 이유 |
|--------|------|
| BGMManager | 씬 전환 간 BGM 연속 재생 |
| CharacterDatabase | 타이틀 캐릭터 프리팹 로드 + GameScene에서 재사용 |
| TrackDatabase | GameScene SceneBootstrapper가 중복 생성하지 않도록 |

SampleScene의 `SceneBootstrapper.Awake()`는 이미 `if (XXX.Instance == null)` 가드가 있으므로,
TitleScene에서 생성된 인스턴스가 살아있으면 **스킵됨** → 기존 코드 변경 불필요.

---

## 4. 화면 구성

### 4-1. 레이어 구조 (아래 → 위)

```
[Layer 0] 배경 이미지        — main_title_bg (전체 화면 채움)
[Layer 1] 캐릭터 달리기       — 하단 길 위에서 좌→우 이동
[Layer 2] 타이틀 로고         — main_title (화면 상단~중앙)
[Layer 3] "Press to Start"  — 화면 중하단, 깜빡임 애니메이션
[Layer 4] 언어 선택 버튼      — 화면 우하단 (ko / en / jp)
[Layer 5] 전환 오버레이       — 블록 디졸브 이펙트 (평소 비활성)
```

### 4-2. 배경 이미지
- **파일:** `Assets/Resources/BG/main_title_bg`
- **배치:** SpriteRenderer, 카메라 orthographic size에 맞춰 스케일 자동 계산
- **해상도 대응:** Camera.main의 orthographicSize + aspect로 sprite 스케일 계산
  - `scale = max(camWidth / spriteWidth, camHeight / spriteHeight)` (Cover 방식, 잘림 허용)
- **Sorting Layer/Order:** Default, Order: -10

### 4-3. 타이틀 로고
- **파일:** `Assets/Resources/BG/main_title`
- **배치:** 화면 상단~중앙 영역
- **Sorting Order:** 5
- **연출:** 없음 (정적 이미지)

### 4-4. "Press to Start" 텍스트
- **폰트:** Neo둥근모 (`FontHelper.GetUIFontWithFallback()`)
- **위치:** 화면 중하단 (뷰포트 약 y=0.25~0.3)
- **다국어:** `Loc.Get("str.ui.press_start")` — StringTable에 키 추가
- **색상:** 흰색
- **애니메이션:** 알파 PingPong (1.0 ↔ 0.3, 주기 1~1.5초)
- **Sorting Order:** 10
- **구현:** TextMesh + FontHelper.ApplyToTextMesh()

### 4-5. 캐릭터 달리기 연출

#### 구현 방식 (확정)
- **기존 캐릭터 프리팹을 그대로 Instantiate**
- RacerController 컴포넌트 → `enabled = false` (NullRef 방지)
- `GetComponentInChildren<Animator>().SetTrigger("Run")` 으로 달리기 재생
- TitleCharacterRunner가 이동/리셋/스폰 관리

#### 동작
- 게임 시작 약 1초 후 첫 캐릭터 등장 (화면 왼쪽 밖에서)
- 이후 1~2초 랜덤 간격으로 새 캐릭터 1마리씩 **누적 등장**
- 12캐릭터 전부 등장하면 **스폰 중지**
- 화면 오른쪽 밖으로 나간 캐릭터 → **position 리셋** (왼쪽에서 재진입, Destroy 안 함)
- 캐릭터마다 약간 다른 속도 (자연스러운 간격)
- **Sorting Order:** 0~3 (배경 위, 로고 아래)
- y좌표: 배경 하단 길 영역 (고정 또는 소폭 랜덤)

### 4-6. 언어 선택 버튼
- **위치:** 화면 우하단
- **구성:** ko / en / jp 버튼 3개 (현재 Loc.langIndex 기준)
- **현재 선택 언어 강조:** 선택된 버튼 하이라이트 (색상 변경 또는 밑줄)
- **동작:** 버튼 클릭 → `Loc.SetLang(lang)` → Press to Start 텍스트 즉시 갱신
- **PlayerPrefs 저장:** `Loc.SetLang()`이 이미 `PlayerPrefs.SetString("DR_Lang", lang)` 처리
- **구현:** UI Canvas (Screen Space Camera 또는 Overlay) + Button 컴포넌트

---

## 5. 입력 처리

### "Press to Start" 트리거
- **마우스 클릭:** `Input.GetMouseButtonDown(0)`
- **아무 키:** `Input.anyKeyDown`
- **터치:** `Input.touchCount > 0 && first touch began`
- 입력 감지 → 전환 시퀀스 시작
- **전환 중 입력 무시:** bool 플래그로 중복 트리거 방지
- **씬 로드 직후 입력:** 최소 0.5초 딜레이 후 입력 수용 (안전장치)

---

## 6. 화면 전환 연출

### 6-1. 전체 시퀀스

```
[입력 감지]
  → ① BGM 페이드아웃 시작 (약 1.5초)
  → ② Press to Start 텍스트 비활성화
  → ③ 블록 디졸브 아웃 시작 (약 1.0~1.5초)
       타이틀 화면이 격자 블록 단위로 랜덤 순서로 사라짐
  → ④ 완전히 검은 화면 (약 0.2~0.3초 유지)
  → ⑤ LoadSceneAsync("SampleScene") + allowSceneActivation = true
  → ⑥ SampleScene 로드 완료 후: 블록 빌드업 인 (약 1.0~1.5초)
       배팅 화면이 격자 블록 단위로 랜덤하게 나타남
  → ⑦ 빌드업 완료 후 게임 BGM 페이드인 시작
```

### 6-2. 블록 디졸브 상세
| 항목 | 값 |
|------|-----|
| 격자 크기 | 16×10 블록 (=160개 Image) |
| 사라지는 순서 | 랜덤 셔플 |
| 소요 시간 | 전체 블록 약 1.0~1.5초 |
| 사라지는 방식 | 각 블록 alpha → 0 즉시 전환 (팍팍 사라지는 느낌) |
| 뒤에 보이는 것 | 검은 배경 (Camera clear color = black) |

### 6-3. 블록 빌드업 상세
| 항목 | 값 |
|------|-----|
| 격자 크기 | 디졸브와 동일 (16×10) |
| 나타나는 순서 | 랜덤 셔플 (디졸브와 다른 시드) |
| 소요 시간 | 약 1.0~1.5초 |
| 나타나는 방식 | 각 블록 alpha 0 → 1 즉시 전환 |
| 오버레이 위치 | SampleScene 카메라 위에 (sortingOrder 최상위) |

### 6-4. 구현 방식 (확정: UI Canvas + Image Grid)
- Screen Space Overlay Canvas 위에 16×10 격자형 Image 배치
- 각 Image의 color.a를 제어
- **SceneTransitionManager** (DontDestroyOnLoad 싱글톤)
- 160개 Image는 전환 완료 후 Canvas 비활성화로 메모리 최소화
- **CanvasScaler:** 1920×1080 기준 Scale With Screen Size
- 블록이 화면 전체를 빈틈없이 커버하도록 **블록 크기를 ceil 처리**

### 6-5. TrackTransition과의 관계 (확정: 공존)
- TrackTransition: 기존 인게임 트랙 전환 (풀스크린 fade) — **변경 없음**
- SceneTransitionManager: 씬 간 블록 디졸브 전환 — **신규**
- Canvas sortingOrder: TrackTransition=9999, SceneTransitionManager=10000

---

## 7. BGM 시스템 (재작성)

### 7-1. 현재 BGMManager 문제점
- DontDestroyOnLoad 아님
- 클립 경로 "Audio/BGM" 하드코딩
- SetVolume() 하나뿐
- Start()에서 자동 재생 (외부 제어 불가)

### 7-2. 신규 BGMManager 설계

```csharp
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    // === Public API ===
    void PlayBGM(string resourcePath, bool loop = true)
    void StopBGM()
    void FadeIn(float targetVolume, float duration)
    void FadeOut(float duration, Action onComplete = null)
    void CrossFade(string newClipPath, float fadeDuration)
    void SetVolume(float vol)

    // === 내부 ===
    AudioSource audioSource;
    Coroutine fadeCoroutine;      // 중복 페이드 방지
}
```

### 7-3. 생명주기
- **TitleScene** TitleSceneManager.Awake()에서 생성 + DontDestroyOnLoad
- **TitleScene** Start()에서 `PlayBGM("Audio/Title_Bgm")`
- **전환 시** `FadeOut(1.5f, callback)`
- **SampleScene** SceneBootstrapper가 `BGMManager.Instance != null` → 스킵
- **SampleScene** 빌드업 완료 후 `PlayBGM("Audio/BGM")` + `FadeIn()`

### 7-4. SampleScene SceneBootstrapper 수정
기존:
```csharp
if (BGMManager.Instance == null)
    new GameObject("BGMManager").AddComponent<BGMManager>();
```
→ 변경 없음 (가드 이미 존재). 단, 기존 BGMManager.Start()의 **자동 재생 로직 제거** 필요.
SampleScene 진입 시 BGM 재생은 SceneBootstrapper가 명시적으로 호출.

---

## 8. 신규 스크립트

| 파일 | 위치 | 역할 |
|------|------|------|
| `TitleSceneManager.cs` | Scripts/Manager/ | 타이틀 씬 전체 관리 (초기화, 입력, 전환 시퀀스) |
| `TitleCharacterRunner.cs` | Scripts/Racer/ | 프리팹 인스턴스의 이동/리셋/스폰 관리 (경량) |
| `SceneTransitionManager.cs` | Scripts/Manager/ | 블록 디졸브/빌드업 + 씬 로딩 (DontDestroyOnLoad) |

### 수정 스크립트

| 파일 | 수정 내용 |
|------|-----------|
| `BGMManager.cs` | **재작성** — DontDestroyOnLoad, FadeIn/Out/CrossFade/PlayBGM, 자동재생 제거 |
| `SceneBootstrapper.cs` | BGMManager 생성 가드 유지 (변경 최소화), 빌드업 완료 시 BGM 재생 호출 추가 |
| `StringTable.csv` | `str.ui.press_start` 키 추가 (ko/en/jp) |

---

## 9. TitleSceneManager 초기화 순서

```
Awake()
  ├─ Loc.Init()                        // 저장된 언어 로드
  ├─ CharacterDatabase 생성 + DontDestroyOnLoad
  ├─ TrackDatabase 생성 + DontDestroyOnLoad
  ├─ BGMManager 생성 + DontDestroyOnLoad
  └─ SceneTransitionManager 생성 + DontDestroyOnLoad

Start()
  ├─ 배경 이미지 배치 (카메라 fit 스케일)
  ├─ 타이틀 로고 배치
  ├─ Press to Start 텍스트 생성 (FontHelper)
  ├─ 언어 선택 버튼 생성
  ├─ BGMManager.PlayBGM("Audio/Title_Bgm")
  └─ 캐릭터 스폰 코루틴 시작 (1초 후 첫 캐릭터)
```

---

## 10. 해상도 대응

### 배경 이미지
- Orthographic Camera의 size + aspect 기반 스케일 계산
- `Cover` 방식: 화면을 완전히 채움, 초과분 잘림 허용
- ```csharp
  float camH = cam.orthographicSize * 2f;
  float camW = camH * cam.aspect;
  float scaleX = camW / spriteWorldWidth;
  float scaleY = camH / spriteWorldHeight;
  float scale = Mathf.Max(scaleX, scaleY);
  ```

### 블록 디졸브 격자
- CanvasScaler: Reference Resolution 1920×1080, Match Width or Height
- 블록 크기 = `ceil(screenWidth / 16)` × `ceil(screenHeight / 10)`
- 마지막 행/열 블록이 화면 밖으로 약간 초과 → 빈틈 없음 보장

### 캐릭터 영역
- 화면 하단 고정 비율 영역 (Camera.ViewportToWorldPoint 기준 y=0.05~0.15)

---

## 11. StringTable 추가 키

| 키 | ko | en | jp |
|----|----|----|-----|
| `str.ui.press_start` | 아무 곳이나 눌러 시작 | Press to Start | タッチしてスタート |

---

## 12. 구현 순서 (Phase별 + 테스트)

> 각 Phase 완료 후 반드시 중간 테스트 통과 후 다음 진행.

---

### Phase 1: 기반 구조 + 빈 씬 전환
- [ ] TitleScene.unity 씬 파일 생성
- [ ] Build Settings에 TitleScene(0), SampleScene(1) 등록
- [ ] TitleSceneManager 기본 구조 (빈 껍데기 + 입력 감지 → LoadSceneAsync)
- [ ] 카메라 설정 (Orthographic, 배경색 검정)
- [ ] CharacterDatabase/TrackDatabase DontDestroyOnLoad 적용

**테스트 1: 빈 씬 전환**
| 확인 항목 | 기대 결과 |
|----------|-----------|
| Play 시작 | TitleScene이 첫 화면 (검은 화면) |
| 아무 키 입력 | SampleScene으로 전환 → 배팅 화면 정상 |
| SampleScene 동작 | 배팅/레이싱 기존 기능 정상 |
| 싱글톤 중복 | BGMManager, CharacterDatabase, TrackDatabase 각 1개만 존재 |
| 콘솔 | NullRef, MissingRef 에러 없음 |

> **Phase 1이 가장 중요.** 씬 분리가 기존 시스템을 깨뜨리지 않는지 확인.

---

### Phase 2: 타이틀 화면 비주얼 + 언어 선택
- [ ] 배경 이미지 배치 (Cover 스케일, 해상도 대응)
- [ ] 타이틀 로고 배치
- [ ] "Press to Start" 텍스트 (FontHelper + Loc + 깜빡임)
- [ ] 언어 선택 버튼 UI (우하단, ko/en/jp)
- [ ] 언어 변경 시 Press to Start 텍스트 즉시 갱신
- [ ] StringTable.csv에 `str.ui.press_start` 키 추가

**테스트 2: 정적 화면 + 언어**
| 확인 항목 | 기대 결과 |
|----------|-----------|
| 화면 구성 | 배경 전체 채움, 로고 상단~중앙, Press to Start 중하단 |
| 이미지 비율 | 찌그러짐 없이 정상 (16:9, 16:10 모두) |
| 깜빡임 | Press to Start 부드럽게 깜빡거림 |
| 폰트 | Neo둥근모 한글 정상 렌더링 |
| 언어 버튼 | 클릭 시 텍스트 즉시 변경 |
| 언어 저장 | 재시작 후에도 마지막 선택 언어 유지 |

---

### Phase 3: 캐릭터 달리기
- [ ] TitleCharacterRunner 컴포넌트 작성
- [ ] CharacterDatabase에서 전체 캐릭터 목록 취득
- [ ] 프리팹 Instantiate + RacerController.enabled = false
- [ ] Animator.SetTrigger("Run")으로 달리기 재생
- [ ] 순차 스폰 코루틴 (1~2초 랜덤 간격, 누적, 12마리 후 중지)
- [ ] 좌→우 이동 (Transform.Translate, 캐릭터별 속도 차이)
- [ ] 화면 밖 나가면 position 리셋 (왼쪽에서 재진입)

**테스트 3: 캐릭터 동작**
| 확인 항목 | 기대 결과 |
|----------|-----------|
| 첫 캐릭터 | 1초 후 화면 왼쪽에서 등장 |
| 추가 캐릭터 | 1~2초 간격으로 누적 |
| 12마리 후 | 스폰 중지, 기존 캐릭터만 순환 |
| run 애니메이션 | 프레임 순환 정상 |
| 레이어 순서 | 배경 위, 로고 아래 |
| 5분 방치 | 에러 없이 안정, 메모리 증가 없음 |

---

### Phase 4: 화면 전환 연출
- [ ] SceneTransitionManager 작성 (DontDestroyOnLoad 싱글톤)
- [ ] 블록 디졸브 아웃 (UI Canvas + 16×10 Image Grid)
- [ ] 블록 빌드업 인
- [ ] LoadSceneAsync + allowSceneActivation 연동
- [ ] 전환 시퀀스 (디졸브 → 검정 유지 → 씬 로드 → 빌드업)
- [ ] Canvas sortingOrder: 10000 (TrackTransition 9999 위)

**테스트 4: 전환 연출**
| 확인 항목 | 기대 결과 |
|----------|-----------|
| 입력 시 | Press to Start 사라짐 + 디졸브 시작 |
| 디졸브 | 블록 단위 랜덤 사라짐 (1~1.5초) |
| 검은 화면 | 0.2~0.3초 유지 |
| 빌드업 | 배팅 화면 블록 단위 나타남 |
| 빌드업 완료 | 오버레이 투명, 배팅 조작 가능 |
| 전환 중 입력 | 무시됨 |
| 즉시 입력 (0.5초 이내) | 안전장치로 무시 |

---

### Phase 5: BGM 시스템
- [ ] BGMManager 재작성 (DontDestroyOnLoad + 전체 API)
- [ ] TitleScene에서 Title_Bgm 재생
- [ ] 전환 시퀀스: 타이틀 BGM FadeOut → 게임 BGM FadeIn
- [ ] SceneBootstrapper에서 빌드업 완료 시 BGM 재생 호출
- [ ] 기존 BGMManager.Start() 자동재생 제거, 명시적 호출로 변경

**테스트 5: BGM**
| 확인 항목 | 기대 결과 |
|----------|-----------|
| 타이틀 진입 | Title_Bgm 즉시 루프 재생 |
| Press to Start | BGM 서서히 페이드아웃 (1.5초) |
| 빌드업 완료 후 | 게임 BGM 페이드인 |
| 두 BGM | 겹침 없이 자연스럽게 이어짐 |
| GameScene 기존 기능 | BGMManager 변경으로 깨짐 없음 |
| Title_Bgm 파일 없을 때 | NullRef 없이 씬 전환 정상 |

---

### Phase 6: 최종 통합 테스트
- [ ] 콜드 스타트 (에디터 재시작 후 Play)
- [ ] 전체 플로우 3회 연속 테스트
- [ ] 디버그 요소 제거 확인

**테스트 6: 통합**
| 확인 항목 | 기대 결과 |
|----------|-----------|
| 전체 흐름 | 타이틀 → 입력 → 디졸브 → 배팅 → 레이싱 → 결과 정상 |
| 타이틀 30초 방치 | 캐릭터 누적 + BGM 루프 + 메모리 안정 |
| 빠른 연타 입력 | 전환 1회만 발생 |
| 콘솔 | Warning/Error 없음 |

---

## 13. 리소스 체크리스트

| 리소스 | 경로 | 상태 |
|--------|------|------|
| 배경 이미지 | Assets/Resources/BG/main_title_bg | 있음 |
| 타이틀 로고 | Assets/Resources/BG/main_title | 있음 |
| 타이틀 BGM | Assets/Resources/Audio/Title_Bgm.mp3 | 있음 |
| 캐릭터 프리팹 | CharacterDB.csv의 charResourcePrefabs | 있음 |
| Neo둥근모 폰트 | Assets/Fonts/NeoDunggeunmo/neodgm.ttf | 있음 |

---

## 14. 향후 확장 고려

- **Result씬 분리:** SceneTransitionManager 재사용 가능
- **전환 연출 변형:** TransitionType enum 추가 (블록 디졸브, 페이드, 와이프 등)
- **BGM 추가:** BGMManager.PlayBGM(path)으로 어디서든 클립 교체 가능
- **언어 추가:** Loc.cs langIndex + StringTable.csv 열 추가만으로 확장

---

*이 명세서는 피드백 회의(기획/개발/QA) 결과를 반영한 확정 버전입니다.*
