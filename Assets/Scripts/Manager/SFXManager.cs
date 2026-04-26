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

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        clickClip = Resources.Load<AudioClip>("Audio/click");
        if (clickClip == null)
            Debug.LogWarning("[SFXManager] Resources/Audio/click 로드 실패");
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
    //  글로벌 버튼 클릭 감지
    // ══════════════════════════════════════

    private void RegisterGlobalClickListener()
    {
        // EventSystem의 InputModule에서 모든 PointerClick을 감지할 수 없으므로
        // Update에서 씬의 버튼을 주기적으로 스캔하여 리스너 등록
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
}

/// <summary>
/// 버튼에 SFX 리스너가 이미 등록되었음을 표시하는 태그 컴포넌트
/// </summary>
public class SFXClickTag : MonoBehaviour { }
