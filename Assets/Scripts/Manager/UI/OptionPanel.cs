using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옵션 패널 — ESC 키로 열리는 독립 패널.
/// BGM/SFX 음소거 토글 + 재시작/종료 (확인 팝업 포함).
/// PlayerPrefs로 토글 상태 영속.
/// </summary>
public class OptionPanel : MonoBehaviour
{
    // ═══ Inspector 연결 ═══
    [SerializeField] private Toggle bgmToggle;
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private ConfirmPopup confirmPopup;

    [SerializeField] private Text titleLabel;
    [SerializeField] private Text bgmLabel;
    [SerializeField] private Text sfxLabel;
    [SerializeField] private Text restartLabel;
    [SerializeField] private Text quitLabel;

    // ═══ PlayerPrefs 키 ═══
    private const string PREF_BGM = "opt_bgm_muted";
    private const string PREF_SFX = "opt_sfx_muted";

    private void Start()
    {
        // PlayerPrefs 복원
        bool bgmMuted = PlayerPrefs.GetInt(PREF_BGM, 0) == 1;
        bool sfxMuted = PlayerPrefs.GetInt(PREF_SFX, 0) == 1;

        if (bgmToggle != null)
        {
            bgmToggle.isOn = bgmMuted;
            ApplyBGMMute(bgmMuted);
            bgmToggle.onValueChanged.AddListener(v =>
            {
                ApplyBGMMute(v);
                PlayerPrefs.SetInt(PREF_BGM, v ? 1 : 0);
            });
        }

        if (sfxToggle != null)
        {
            sfxToggle.isOn = sfxMuted;
            ApplySFXMute(sfxMuted);
            sfxToggle.onValueChanged.AddListener(v =>
            {
                ApplySFXMute(v);
                PlayerPrefs.SetInt(PREF_SFX, v ? 1 : 0);
            });
        }

        if (restartButton != null)
            restartButton.onClick.AddListener(() =>
                confirmPopup?.Show(Loc.Get("str.ui.option.confirm_restart"), OnRestartConfirmed));

        if (quitButton != null)
            quitButton.onClick.AddListener(() =>
                confirmPopup?.Show(Loc.Get("str.ui.option.confirm_quit"), OnQuitConfirmed));

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        RefreshLocalization();
    }

    // ═══ 공개 API ═══

    public void Show()
    {
        // 이전에 열려 있던 확인 팝업 상태 초기화
        confirmPopup?.gameObject.SetActive(false);
        gameObject.SetActive(true);
        RefreshLocalization();
    }

    public void Hide()
    {
        // 확인 팝업도 함께 닫아 다음 열릴 때 초기화 상태 보장
        confirmPopup?.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    // ═══ 내부 ═══

    private void ApplyBGMMute(bool muted)
    {
        BGMManager.Instance?.Mute(muted);
    }

    private void ApplySFXMute(bool muted)
    {
        SFXManager.Instance?.Mute(muted);
    }

    private void OnRestartConfirmed()
    {
        Hide();
        if (TrackTransition.Instance != null)
        {
            // TitleScene → SampleScene 전환과 동일한 페이드 연출
            TrackTransition.Instance.PlayTransition(0.4f, () =>
            {
                GameManager.Instance?.StartNewGame();
            });
        }
        else
        {
            GameManager.Instance?.StartNewGame();
        }
    }

    private void OnQuitConfirmed()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void RefreshLocalization()
    {
        if (titleLabel)   titleLabel.text   = Loc.Get("str.ui.option.title");
        if (bgmLabel)     bgmLabel.text     = Loc.Get("str.ui.option.bgm_off");
        if (sfxLabel)     sfxLabel.text     = Loc.Get("str.ui.option.sfx_off");
        if (restartLabel) restartLabel.text = Loc.Get("str.ui.option.restart");
        if (quitLabel)    quitLabel.text    = Loc.Get("str.ui.option.quit");
    }
}
