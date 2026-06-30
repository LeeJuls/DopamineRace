using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// BGM 매니저
/// - DontDestroyOnLoad 싱글톤
/// - AudioSource A/B 두 채널로 진짜 동시 크로스페이드 지원
///   (A 볼륨↓ + B 볼륨↑ 동시 진행 → 완료 후 역할 교체)
/// </summary>
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    private AudioSource _sourceA;
    private AudioSource _sourceB;
    private AudioSource _active;    // 현재 재생 중인 채널
    private AudioSource _inactive;  // 크로스페이드 대상 채널

    private Coroutine _fadeCoroutine;
    private Coroutine _crossFadeCoroutine;
    private bool _muted;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sourceA = MakeSource();
        _sourceB = MakeSource();
        _active   = _sourceA;
        _inactive = _sourceB;
    }

    private AudioSource MakeSource()
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.loop        = true;
        src.playOnAwake = false;
        src.volume      = 0f;
        return src;
    }

    // ══════════════════════════════════════
    //  Public API
    // ══════════════════════════════════════

    /// <summary>BGM 즉시 재생 (같은 클립 중복 재생 방지)</summary>
    public void PlayBGM(string resourcePath, bool loop = true)
    {
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null) { Debug.LogWarning($"[BGMManager] 클립 없음: {resourcePath}"); return; }

        if (_active.clip == clip && _active.isPlaying) return;

        StopCrossFade();
        _inactive.Stop();
        _inactive.clip = null;

        _active.clip   = clip;
        _active.loop   = loop;
        float vol = TargetVol();
        _active.volume = _muted ? 0f : vol;
        _active.Play();
        Debug.Log($"[BGMManager] PlayBGM: {resourcePath}");
    }

    /// <summary>BGM 정지</summary>
    public void StopBGM()
    {
        StopFade();
        StopCrossFade();
        _sourceA.Stop(); _sourceA.clip = null;
        _sourceB.Stop(); _sourceB.clip = null;
    }

    /// <summary>볼륨 페이드인 (active 채널)</summary>
    public void FadeIn(float targetVolume, float duration)
    {
        StopFade();
        _fadeCoroutine = StartCoroutine(FadeVolume(_active, _active.volume, targetVolume, duration, null));
    }

    /// <summary>볼륨 페이드아웃 → Stop (active 채널)</summary>
    public void FadeOut(float duration, Action onComplete = null)
    {
        StopFade();
        var src = _active;
        _fadeCoroutine = StartCoroutine(FadeVolume(src, src.volume, 0f, duration, () =>
        {
            src.Stop();
            onComplete?.Invoke();
        }));
    }

    /// <summary>
    /// 진짜 동시 크로스페이드:
    /// active 볼륨↓ + inactive(새 클립) 볼륨↑ 를 duration 동안 동시에 진행.
    /// 완료 후 active/inactive 역할 교체.
    /// </summary>
    public void CrossFade(string newClipPath, float duration)
    {
        AudioClip clip = Resources.Load<AudioClip>(newClipPath);
        if (clip == null) { Debug.LogWarning($"[BGMManager] 크로스페이드 클립 없음: {newClipPath}"); return; }

        if (_active.clip == clip && _active.isPlaying) return;

        StopFade();
        StopCrossFade();

        float targetVol = _muted ? 0f : TargetVol();
        _inactive.clip   = clip;
        _inactive.loop   = true;
        _inactive.volume = 0f;
        _inactive.Play();

        _crossFadeCoroutine = StartCoroutine(CrossFadeRoutine(_active, _inactive, targetVol, duration));
    }

    /// <summary>볼륨 직접 설정 (active 채널)</summary>
    public void SetVolume(float vol)
    {
        if (!_muted) _active.volume = Mathf.Clamp01(vol);
    }

    /// <summary>BGM 음소거 ON/OFF</summary>
    public void Mute(bool muted)
    {
        _muted = muted;
        float vol = _muted ? 0f : TargetVol();
        if (_sourceA != null) _sourceA.volume = _sourceA.isPlaying ? vol : 0f;
        if (_sourceB != null) _sourceB.volume = _sourceB.isPlaying ? vol : 0f;
    }

    public bool IsPlaying => _active != null && _active.isPlaying;

    // ══════════════════════════════════════
    //  내부
    // ══════════════════════════════════════

    private IEnumerator CrossFadeRoutine(AudioSource from, AudioSource to, float targetVol, float duration)
    {
        float elapsed   = 0f;
        float fromStart = from.volume;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            from.volume = Mathf.Lerp(fromStart, 0f,         t);
            to.volume   = Mathf.Lerp(0f,         targetVol, t);
            yield return null;
        }
        from.volume = 0f;
        to.volume   = targetVol;
        from.Stop();
        from.clip = null;

        // 역할 교체
        (_active, _inactive) = (to, from);
        _crossFadeCoroutine = null;
        Debug.Log($"[BGMManager] CrossFade 완료 → {to.clip?.name}");
    }

    private IEnumerator FadeVolume(AudioSource src, float from, float to, float duration, Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            src.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        src.volume      = to;
        _fadeCoroutine  = null;
        onComplete?.Invoke();
    }

    private void StopFade()
    {
        if (_fadeCoroutine != null) { StopCoroutine(_fadeCoroutine); _fadeCoroutine = null; }
    }

    private void StopCrossFade()
    {
        if (_crossFadeCoroutine != null)
        {
            StopCoroutine(_crossFadeCoroutine);
            _crossFadeCoroutine = null;
            _inactive.Stop();
            _inactive.volume = 0f;
        }
    }

    private float TargetVol() =>
        GameSettings.Instance != null ? GameSettings.Instance.bgmVolume : 0.5f;

    private void OnDestroy()
    {
        StopBGM();
        if (Instance == this) Instance = null;
    }
}
