using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// SFX 매니저 — UI 클릭 사운드 자동 재생
/// DontDestroyOnLoad 싱글톤. 모든 Button 클릭 시 click.mp3 재생.
/// </summary>
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    private AudioSource sfxSource;
    private AudioClip clickClip;
    private bool _muted;

    // ── 키 기반 SFX (SFXSettings.asset) ──
    private SFXSettings sfxSettings;
    private AudioSource loopSource;                              // 전역 루프 채널 1개(마네키네코 등)
    private string _loopKey;                                     // 현재 재생 중인 루프 key(전환 판정용)
    private readonly Dictionary<string, int> _activeCounts = new Dictionary<string, int>(); // maxConcurrent 카운터

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;

        clickClip = Resources.Load<AudioClip>("Audio/click");
        if (clickClip == null)
            Debug.LogWarning("[SFXManager] Resources/Audio/click 로드 실패");

        sfxSettings = Resources.Load<SFXSettings>("SFXSettings");
        if (sfxSettings == null)
            Debug.LogWarning("[SFXManager] SFXSettings 로드 실패 — PlaySFX(key)/PlayLoop(key) 호출은 이후 전부 무음 처리됨");
    }

    private void OnEnable()
    {
        // 모든 Button 클릭을 가로채는 글로벌 리스너
        RegisterGlobalClickListener();
    }

    /// <summary>
    /// SFX 음소거 ON/OFF
    /// </summary>
    public void Mute(bool muted) => _muted = muted;

    /// <summary>
    /// 클릭 사운드 재생 (외부에서 수동 호출 가능)
    /// </summary>
    public void PlayClick()
    {
        if (_muted) return;
        if (clickClip != null && sfxSource != null)
        {
            float vol = GameSettings.Instance != null ? GameSettings.Instance.sfxVolume : 0.3f;
            sfxSource.PlayOneShot(clickClip, vol);
        }
    }

    /// <summary>
    /// 임의 SFX 재생 (GameSettings.sfxVolume 적용)
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (_muted) return;
        if (clip != null && sfxSource != null)
        {
            float vol = GameSettings.Instance != null ? GameSettings.Instance.sfxVolume : 0.3f;
            sfxSource.PlayOneShot(clip, vol * volumeScale);
        }
    }

    // ══════════════════════════════════════
    //  키 기반 SFX (SFXKeys 상수 → SFXSettings.asset 조회)
    //  ★ 무음 불변조건: entry 없음/clips 미할당 시 크래시 없이 조용히 return (경고 로그만)
    // ══════════════════════════════════════

    /// <summary>SFXSettings.asset의 원본 엔트리 조회(레이서 로컬 루프 등 외부에서 clip/volume/loop 직접 사용 시)</summary>
    public SFXEntry GetEntry(string key) => sfxSettings != null ? sfxSettings.Find(key) : null;

    /// <summary>key 기반 원샷 재생. entry.maxConcurrent(0=무제한) 상한 적용. entry.delay(초)만큼 재생 시작 지연.</summary>
    public void PlaySFX(string key, float volumeScale = 1f)
    {
        if (_muted || sfxSettings == null) return;
        var entry = sfxSettings.Find(key);
        if (entry == null) { Debug.LogWarning("[SFXManager] 키 없음: " + key); return; }

        if (entry.delay > 0f) { StartCoroutine(PlaySFXDelayed(entry, volumeScale)); return; }
        PlaySFXImmediate(entry, volumeScale);
    }

    private IEnumerator PlaySFXDelayed(SFXEntry entry, float volumeScale)
    {
        yield return new WaitForSeconds(entry.delay);
        PlaySFXImmediate(entry, volumeScale);
    }

    private void PlaySFXImmediate(SFXEntry entry, float volumeScale)
    {
        var clip = entry.GetRandomClip();
        if (clip == null) return; // 무음 불변조건: clip 미할당 → 조용히 무음

        if (entry.maxConcurrent > 0)
        {
            _activeCounts.TryGetValue(entry.key, out int cur);
            if (cur >= entry.maxConcurrent) return;
            StartCoroutine(TrackConcurrent(entry.key, clip.length));
        }

        float baseVol = GameSettings.Instance != null ? GameSettings.Instance.sfxVolume : 0.3f;
        sfxSource.PlayOneShot(clip, baseVol * entry.volume * volumeScale);
    }

    private IEnumerator TrackConcurrent(string key, float duration)
    {
        _activeCounts.TryGetValue(key, out int cur);
        _activeCounts[key] = cur + 1;
        yield return new WaitForSeconds(duration);
        _activeCounts.TryGetValue(key, out int now);
        _activeCounts[key] = Mathf.Max(0, now - 1);
    }

    /// <summary>전역 루프 채널(1개)에서 key 재생. 동일 key 재호출=재시작, 다른 key=즉시 전환. entry.delay(초)만큼 재생 시작 지연.</summary>
    public void PlayLoop(string key)
    {
        if (_muted || sfxSettings == null) return;
        var entry = sfxSettings.Find(key);
        var clip = entry != null ? entry.GetRandomClip() : null;
        if (clip == null) return; // 무음 불변조건

        float baseVol = GameSettings.Instance != null ? GameSettings.Instance.sfxVolume : 0.3f;
        loopSource.Stop();
        loopSource.clip = clip;
        loopSource.volume = baseVol * entry.volume;
        loopSource.PlayDelayed(entry.delay); // delay=0이면 즉시 재생과 동일(Unity 내장 DSP 스케줄, 프레임 무관 정확)
        _loopKey = key;
    }

    /// <summary>전역 루프 정지. 재생 중이 아니어도 안전(no-op).</summary>
    public void StopLoop()
    {
        if (loopSource != null) loopSource.Stop();
        _loopKey = null;
    }

    // ══════════════════════════════════════
    //  글로벌 버튼 클릭 감지
    // ══════════════════════════════════════

    private void RegisterGlobalClickListener()
    {
        // EventSystem의 InputModule에서 모든 PointerClick을 감지할 수 없으므로
        // Update에서 씬의 버튼을 주기적으로 스캔하여 리스너 등록
        CancelInvoke(nameof(ScanAndAttachButtons));
        InvokeRepeating(nameof(ScanAndAttachButtons), 0.5f, 2.0f);
    }

    private void ScanAndAttachButtons()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            // 이미 등록된 버튼은 태그로 구분
            if (btn.GetComponent<SFXClickTag>() != null) continue;

            btn.gameObject.AddComponent<SFXClickTag>();
            btn.onClick.AddListener(PlayClick);
        }
    }

    private void OnDestroy()
    {
        CancelInvoke();
        if (Instance == this) Instance = null;
    }
}

/// <summary>
/// 버튼에 SFX 리스너가 이미 등록되었음을 표시하는 태그 컴포넌트
/// </summary>
public class SFXClickTag : MonoBehaviour { }
