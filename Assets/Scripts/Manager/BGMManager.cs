using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// BGM 매니저 (재작성)
/// - DontDestroyOnLoad 싱글톤
/// - PlayBGM / StopBGM / FadeIn / FadeOut / CrossFade
/// - 외부 제어 전용 (자동 재생 없음)
/// </summary>
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
    }

    // ══════════════════════════════════════
    //  Public API
    // ══════════════════════════════════════

    /// <summary>
    /// BGM 재생 (Resources 경로)
    /// </summary>
    public void PlayBGM(string resourcePath, bool loop = true)
    {
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null)
        {
            Debug.LogWarning($"[BGMManager] 클립 없음: Resources/{resourcePath}");
            return;
        }

        // 같은 클립이면 중복 재생 방지
        if (audioSource.clip == clip && audioSource.isPlaying)
            return;

        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.volume = GameSettings.Instance != null
            ? GameSettings.Instance.bgmVolume
            : 0.5f;
        audioSource.Play();

        Debug.Log($"[BGMManager] PlayBGM: {resourcePath}");
    }

    /// <summary>
    /// BGM 정지
    /// </summary>
    public void StopBGM()
    {
        StopFade();
        audioSource.Stop();
        audioSource.clip = null;
    }

    /// <summary>
    /// 볼륨 페이드 인
    /// </summary>
    public void FadeIn(float targetVolume, float duration)
    {
        StopFade();
        fadeCoroutine = StartCoroutine(FadeVolume(audioSource.volume, targetVolume, duration, null));
    }

    /// <summary>
    /// 볼륨 페이드 아웃
    /// </summary>
    public void FadeOut(float duration, Action onComplete = null)
    {
        StopFade();
        fadeCoroutine = StartCoroutine(FadeVolume(audioSource.volume, 0f, duration, () =>
        {
            audioSource.Stop();
            onComplete?.Invoke();
        }));
    }

    /// <summary>
    /// 크로스페이드: 현재 곡 페이드아웃 → 새 곡 페이드인
    /// </summary>
    public void CrossFade(string newClipPath, float fadeDuration)
    {
        float halfDur = fadeDuration * 0.5f;
        FadeOut(halfDur, () =>
        {
            PlayBGM(newClipPath);
            audioSource.volume = 0f;
            FadeIn(GameSettings.Instance != null ? GameSettings.Instance.bgmVolume : 0.5f, halfDur);
        });
    }

    /// <summary>
    /// 볼륨 직접 설정
    /// </summary>
    public void SetVolume(float vol)
    {
        audioSource.volume = Mathf.Clamp01(vol);
    }

    /// <summary>
    /// 현재 재생 중인지
    /// </summary>
    public bool IsPlaying => audioSource != null && audioSource.isPlaying;

    // ══════════════════════════════════════
    //  내부
    // ══════════════════════════════════════
    private void StopFade()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    private IEnumerator FadeVolume(float from, float to, float duration, Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        audioSource.volume = to;
        fadeCoroutine = null;
        onComplete?.Invoke();
    }
}
