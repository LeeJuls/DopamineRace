using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// 캐릭터 정보창 비디오 재생 래퍼 (SPEC-049).
/// VideoPlayer(RenderTexture 640²) → RawImage. 무음·루프·멱등·prepare race 가드·errorReceived 폴백.
/// RectTransform은 건드리지 않음(BettingPanel 프리팹/디자이너 소관).
/// </summary>
[RequireComponent(typeof(RawImage))]
[DisallowMultipleComponent]
public class CharacterVideoController : MonoBehaviour
{
    private const int   RT_SIZE = 640;          // §0 측정: 최대 소스 640²
    private const float FADE_SEC = 0.18f;       // 크로스페이드 150~200ms
    private const float PREPARE_TIMEOUT = 2f;

    private RawImage _raw;
    private AspectRatioFitter _fitter;
    private VideoPlayer _vp;
    private RenderTexture _rt;

    private string _loadingPath;   // prepare 중인 경로
    private string _activePath;    // 재생 중인 경로
    private int    _token;         // prepare race 가드 (TryPlay마다 증가)
    private int    _loadingToken;
    private Coroutine _fadeCo;
    private float  _prepareStart;

    /// <summary>비디오가 화면에 활성(재생/표시) 중인가.</summary>
    public bool IsActive => _raw != null && _raw.enabled;
    /// <summary>준비 완료된 비디오의 종횡비(w/h). 미준비 시 0.</summary>
    public float CurrentAspect { get; private set; }

    /// <summary>prepare 완료(종횡비 확정) 1회 — CharacterInfoPopup가 PNG 페이드아웃 트리거에 사용.</summary>
    public event System.Action<float> OnPrepared;
    /// <summary>로드/디코드 실패 → PNG 폴백 신호.</summary>
    public event System.Action OnFallback;

    private void Awake()
    {
        _raw = GetComponent<RawImage>();
        _fitter = GetComponent<AspectRatioFitter>();
        _raw.enabled = false;
        _raw.raycastTarget = false;

        _rt = new RenderTexture(RT_SIZE, RT_SIZE, 0, RenderTextureFormat.ARGB32);
        _rt.filterMode = FilterMode.Point;   // 픽셀아트 톤
        _rt.Create();

        _vp = gameObject.AddComponent<VideoPlayer>();
        _vp.playOnAwake = false;
        _vp.isLooping = true;
        _vp.waitForFirstFrame = true;
        _vp.skipOnDrop = true;
        _vp.renderMode = VideoRenderMode.RenderTexture;
        _vp.targetTexture = _rt;
        _vp.audioOutputMode = VideoAudioOutputMode.None;   // 무음
        _vp.source = VideoSource.VideoClip;
        _vp.prepareCompleted += OnPrepareCompleted;
        _vp.errorReceived += OnErrorReceived;
    }

    /// <summary>비디오 표시 시도. 클립 있으면 prepare 후 true, 없으면 false(→호출측 PNG 폴백).</summary>
    public bool TryPlayForCharacter(CharacterData data)
    {
        if (data == null) return false;
        string path = data.VideoResourcePath;
        if (string.IsNullOrEmpty(path)) return false;
        var clip = Resources.Load<VideoClip>(path);
        if (clip == null) return false;

        if (_activePath == path && _raw.enabled) return true;   // 동일 클립 재생 중 → skip

        _loadingPath = path;
        _loadingToken = ++_token;
        _vp.clip = clip;
        _prepareStart = Time.realtimeSinceStartup;
        _vp.Prepare();
        return true;
    }

    private void OnPrepareCompleted(VideoPlayer vp)
    {
        if (_loadingToken != _token || _loadingPath == null) return;   // race: 더 최신 요청이 있으면 무시

        CurrentAspect = _vp.height != 0 ? (float)_vp.width / _vp.height : 1f;
        if (_fitter != null) _fitter.aspectRatio = CurrentAspect;
        _raw.texture = _rt;
        _activePath = _loadingPath;
        _loadingPath = null;
        _vp.Play();
        _raw.enabled = true;
        StartFade(0f, 1f);
        OnPrepared?.Invoke(CurrentAspect);
    }

    private void OnErrorReceived(VideoPlayer vp, string message)
    {
        Debug.LogWarning("[CharacterVideo] 디코드 실패 → PNG 폴백: " + message);
        StopAndRelease();
        OnFallback?.Invoke();
    }

    /// <summary>정지 + RawImage 비활성 + RT 해제(파괴는 OnDestroy). Hide/OnDisable/전환에서 호출.</summary>
    public void StopAndRelease()
    {
        _loadingPath = null;
        _activePath = null;
        _token++;   // 진행 중 prepare 무효화
        if (_fadeCo != null) { StopCoroutine(_fadeCo); _fadeCo = null; }
        if (_vp != null) { _vp.Stop(); _vp.clip = null; }
        if (_raw != null) { _raw.enabled = false; var c = _raw.color; c.a = 1f; _raw.color = c; }
    }

    private void Update()
    {
        // prepare 타임아웃 → PNG 폴백 (늦은 깜빡임 차단)
        if (_loadingPath != null && _activePath == null && _vp != null && !_vp.isPrepared
            && Time.realtimeSinceStartup - _prepareStart > PREPARE_TIMEOUT)
        {
            Debug.LogWarning("[CharacterVideo] prepare 타임아웃 → PNG 폴백");
            StopAndRelease();
            OnFallback?.Invoke();
        }
    }

    private void StartFade(float from, float to)
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        if (!isActiveAndEnabled) { var c0 = _raw.color; c0.a = to; _raw.color = c0; return; }
        _fadeCo = StartCoroutine(FadeCo(from, to));
    }

    private IEnumerator FadeCo(float from, float to)
    {
        float t = 0f; var c = _raw.color;
        while (t < FADE_SEC) { t += Time.unscaledDeltaTime; c.a = Mathf.Lerp(from, to, t / FADE_SEC); _raw.color = c; yield return null; }
        c.a = to; _raw.color = c; _fadeCo = null;
    }

    private void OnDestroy()
    {
        if (_vp != null) { _vp.prepareCompleted -= OnPrepareCompleted; _vp.errorReceived -= OnErrorReceived; }
        if (_rt != null) { _rt.Release(); Destroy(_rt); _rt = null; }
    }
}
