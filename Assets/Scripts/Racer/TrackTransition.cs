using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// 트랙 전환 페이드 연출
/// Canvas 최상위에 검정 Image → alpha Lerp
/// 
/// GameSettings.enableTrackTransition 으로 ON/OFF
/// GameSettings.trackTransitionFadeDuration 으로 시간 조절
/// 
/// 사용:
///   TrackTransition.Instance.PlayTransition(0.3f, () => { 배경 교체 });
/// </summary>
public class TrackTransition : MonoBehaviour
{
    public static TrackTransition Instance { get; private set; }

    private Image fadeImage;
    private Canvas fadeCanvas;
    private bool isTransitioning = false;

    /// <summary>전환 중인지</summary>
    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        CreateFadeOverlay();
    }

    /// <summary>
    /// 페이드 오버레이 생성 (Canvas + 검정 Image)
    /// </summary>
    private void CreateFadeOverlay()
    {
        // Canvas 생성 (최상위)
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform);

        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999; // 최상위

        // CanvasScaler
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // GraphicRaycaster (페이드 중 클릭 차단)
        canvasObj.AddComponent<GraphicRaycaster>();

        // 검정 Image (화면 전체)
        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(canvasObj.transform, false);

        fadeImage = imgObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0); // 투명 시작
        fadeImage.raycastTarget = false;          // 기본은 클릭 통과

        // 화면 전체 덮기
        RectTransform rt = imgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 시작 시 숨김
        canvasObj.SetActive(false);
    }

    // ══════════════════════════════════════
    //  전환 연출
    // ══════════════════════════════════════

    /// <summary>
    /// FadeIn → 콜백(배경 교체) → FadeOut
    /// </summary>
    /// <param name="duration">한쪽 페이드 시간 (총 시간 = duration × 2)</param>
    /// <param name="onMidpoint">화면이 완전히 어두울 때 실행할 콜백</param>
    public void PlayTransition(float duration, Action onMidpoint)
    {
        if (isTransitioning)
        {
            // 이미 전환 중이면 콜백만 즉시 실행
            Debug.LogWarning("[TrackTransition] 이미 전환 중 → 즉시 실행");
            onMidpoint?.Invoke();
            return;
        }

        StartCoroutine(TransitionCoroutine(duration, onMidpoint));
    }

    private IEnumerator TransitionCoroutine(float duration, Action onMidpoint)
    {
        isTransitioning = true;

        // 캔버스 활성화 + 클릭 차단
        fadeCanvas.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;

        // ── FadeIn (투명 → 검정) ──
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / duration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, 1); // 완전 검정

        // ── 중간 지점: 배경 교체 ──
        onMidpoint?.Invoke();

        // 한 프레임 대기 (배경 교체 반영)
        yield return null;

        // ── FadeOut (검정 → 투명) ──
        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(timer / duration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, 0); // 완전 투명

        // 클릭 차단 해제 + 캔버스 비활성화
        fadeImage.raycastTarget = false;
        fadeCanvas.gameObject.SetActive(false);

        isTransitioning = false;
    }

    // ══════════════════════════════════════
    //  개별 페이드 (필요 시 외부 사용)
    // ══════════════════════════════════════

    /// <summary>화면 어두워지기 (alpha 0→1)</summary>
    public void FadeIn(float duration, Action onComplete = null)
    {
        StartCoroutine(FadeCoroutine(0f, 1f, duration, onComplete));
    }

    /// <summary>화면 밝아지기 (alpha 1→0)</summary>
    public void FadeOut(float duration, Action onComplete = null)
    {
        StartCoroutine(FadeCoroutine(1f, 0f, duration, onComplete));
    }

    private IEnumerator FadeCoroutine(float from, float to, float duration, Action onComplete)
    {
        fadeCanvas.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, Mathf.Clamp01(timer / duration));
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, to);

        if (to <= 0f)
        {
            fadeImage.raycastTarget = false;
            fadeCanvas.gameObject.SetActive(false);
        }

        onComplete?.Invoke();
    }
}
