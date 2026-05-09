using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// GAME OVER 화면 partial.
/// 젤리 0 + 환전 불가 = GameState.GameOver 진입 시 자동 표시.
///
/// SPEC-028 Steps 3.1, 3.2, 3.3, 3.4 (R20·R21)
/// </summary>
public partial class SceneBootstrapper : MonoBehaviour
{
    // ── GameOver UI ──
    private GameObject _gameOverUI;
    private Text _gameOverHeadlineText;
    private Text _gameOverDescText;
    private Text _gameOverProgressText;
    private Text _gameOverNoRankText;
    private Button _gameOverRestartButton;
    private Text _gameOverRestartBtnText;

    /// <summary>
    /// BuildUI에서 호출 — GameOver 패널 임시 생성 (SPEC-028 Step 3.2).
    /// 디자인은 추후 프리팹화 (Step 2.13).
    /// </summary>
    private void BuildGameOverUI(Transform root)
    {
        // 어두운 백드롭 (전체 화면)
        var backdrop = MkImage(root, "GameOverBackdrop",
            new Vector2(0, 0), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            new Color(0, 0, 0, 0.85f));

        // 모달 패널 (중앙)
        var modal = new GameObject("GameOverModal");
        modal.transform.SetParent(backdrop.transform, false);
        var modalRT = modal.AddComponent<RectTransform>();
        modalRT.anchorMin = new Vector2(0.5f, 0.5f);
        modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        modalRT.pivot = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta = new Vector2(720, 480);
        modalRT.anchoredPosition = Vector2.zero;
        var modalImg = modal.AddComponent<Image>();
        modalImg.color = new Color(0.15f, 0.05f, 0.05f, 0.95f);

        // 헤드라인 (큰 글씨)
        _gameOverHeadlineText = MkText(modal.transform, "💔 GAME OVER",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -90), new Vector2(0, 80),
            64, TextAnchor.MiddleCenter,
            new Color(1f, 0.3f, 0.3f));

        // 설명
        _gameOverDescText = MkText(modal.transform, "도파민 젤리가 모두 소진됐습니다",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -180), new Vector2(0, 40),
            28, TextAnchor.MiddleCenter,
            new Color(0.95f, 0.95f, 0.95f));

        // 도달 라운드
        _gameOverProgressText = MkText(modal.transform, "{0} / {1} 라운드에서 종료",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -240), new Vector2(0, 40),
            32, TextAnchor.MiddleCenter,
            new Color(1f, 0.85f, 0.4f));

        // 랭킹 미등록 안내
        _gameOverNoRankText = MkText(modal.transform, "랭킹에 등록되지 않았습니다",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -310), new Vector2(0, 36),
            24, TextAnchor.MiddleCenter,
            new Color(0.8f, 0.8f, 0.8f));

        // 다시 도전 버튼
        var btnGo = new GameObject("RestartButton");
        btnGo.transform.SetParent(modal.transform, false);
        var btnRT = btnGo.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0);
        btnRT.anchorMax = new Vector2(0.5f, 0);
        btnRT.pivot = new Vector2(0.5f, 0);
        btnRT.sizeDelta = new Vector2(280, 70);
        btnRT.anchoredPosition = new Vector2(0, 50);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.85f, 0.25f, 0.25f);
        _gameOverRestartButton = btnGo.AddComponent<Button>();
        _gameOverRestartButton.targetGraphic = btnImg;

        _gameOverRestartBtnText = MkText(btnGo.transform, "다시 도전",
            new Vector2(0, 0), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            28, TextAnchor.MiddleCenter,
            Color.white);

        _gameOverRestartButton.onClick.AddListener(OnGameOverRestartClicked);
    }

    /// <summary>
    /// GameOver 상태 진입 시 호출 (OnStateChanged에서 분기).
    /// SPEC-028 Step 3.3
    /// </summary>
    private void ShowGameOverPanel()
    {
        if (_gameOverUI != null) _gameOverUI.SetActive(true);

        // 라벨 갱신
        if (_gameOverHeadlineText != null)
            _gameOverHeadlineText.text = SafeLocFor("str.gameover.headline", "💔 GAME OVER");
        if (_gameOverDescText != null)
            _gameOverDescText.text = SafeLocFor("str.gameover.desc", "도파민 젤리가 모두 소진됐습니다");
        if (_gameOverNoRankText != null)
            _gameOverNoRankText.text = SafeLocFor("str.gameover.no_ranking", "랭킹에 등록되지 않았습니다");
        if (_gameOverRestartBtnText != null)
            _gameOverRestartBtnText.text = SafeLocFor("str.gameover.btn.restart", "다시 도전");

        // 도달 라운드
        var gm = GameManager.Instance;
        if (_gameOverProgressText != null && gm != null)
        {
            _gameOverProgressText.text = SafeLocFor("str.gameover.progress",
                "{0} / {1} 라운드에서 종료", gm.CurrentRound, gm.TotalRounds);
        }

        Debug.Log("[GameOver] 패널 표시 (Step 3.3)");
    }

    private void HideGameOverPanel()
    {
        if (_gameOverUI != null) _gameOverUI.SetActive(false);
    }

    /// <summary>
    /// [다시 도전] 클릭 — 새 게임 시작 (씬 재로드).
    /// SPEC-028 Step 3.4
    /// </summary>
    private void OnGameOverRestartClicked()
    {
        Debug.Log("[GameOver] 다시 도전 클릭 → 씬 재로드");
        // SampleScene 재로드 — 모든 상태 초기화 (WalletManager Awake에서 100/0 리셋)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ───── 유틸 ─────

    private static string SafeLocFor(string key, string fallback, params object[] args)
    {
        string val = Loc.Get(key);
        if (string.IsNullOrEmpty(val) || val == key)
            val = fallback;
        if (args != null && args.Length > 0)
        {
            try { val = string.Format(val, args); } catch { }
        }
        return val;
    }

    private static Image MkImage(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta,
        Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }
}
