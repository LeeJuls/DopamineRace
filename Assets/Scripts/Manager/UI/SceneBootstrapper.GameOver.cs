using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// GAME OVER 화면 partial (SPEC-029 / SPEC-029.1).
/// 젤리 0 + 환전 불가 = GameState.GameOver 진입 시 자동 표시.
/// "GAME OVER" 텍스트(폰트 42) 표시 후 화면을 클릭하면 타이틀 화면으로 이동.
/// 스코어/리더보드는 GameManager.ChangeState(GameOver)에서 저장됨. 캐릭터 개별 기록은 CalcScore에서 이미 저장됨.
/// </summary>
public partial class SceneBootstrapper : MonoBehaviour
{
    // ── GameOver UI ──
    private GameObject _gameOverUI;
    private Text _gameOverHeadlineText;
    private Text _gameOverHintText;
    private Button _gameOverButton;

    /// <summary>
    /// BuildUI에서 호출 — GameOver 패널 생성 (SPEC-029).
    /// 프리팹(GameSettings.gameOverPanelPrefab) 우선, 없으면 코드 폴백.
    /// 화면 중앙에 모달 박스로 "GAME OVER"(폰트 42) 표시.
    /// </summary>
    private void BuildGameOverUI(Transform root)
    {
        var gs = GameSettings.Instance;
        if (gs != null && gs.gameOverPanelPrefab != null)
        {
            GameObject panelObj = Instantiate(gs.gameOverPanelPrefab, root);
            panelObj.name = "GameOverPanel";

            // 전체 화면 채우기 (프리팹 루트는 full-rect로 설계됨)
            var rt = panelObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // 중앙 박스 내 헤드라인 텍스트 캐싱
            Transform headline = panelObj.transform.Find("Panel/HeadlineText");
            if (headline != null)
                _gameOverHeadlineText = headline.GetComponent<Text>();

            if (_gameOverHeadlineText == null)
                Debug.LogWarning("[SceneBootstrapper] GameOverPanel 프리팹에서 Panel/HeadlineText를 찾지 못했습니다.");

            // SPEC-029.1: 클릭 안내 텍스트 캐싱
            Transform hint = panelObj.transform.Find("Panel/HintText");
            if (hint != null)
                _gameOverHintText = hint.GetComponent<Text>();

            // SPEC-029.1: 전체화면 Button 캐싱 → 클릭 시 타이틀 이동
            _gameOverButton = panelObj.GetComponent<Button>();
            if (_gameOverButton == null)
                Debug.LogWarning("[SceneBootstrapper] GameOverPanel 프리팹 루트에 Button이 없습니다. " +
                    "Unity 메뉴 DopamineRace > Patch GameOver UI Prefabs (Safe) 실행 권장.");
            WireGameOverButton();
            return;
        }

        // ── 프리팹 미연결 시 코드 폴백 ──
        Debug.LogWarning("[SceneBootstrapper] gameOverPanelPrefab이 없습니다! " +
            "Unity 메뉴 DopamineRace > Create GameOver UI Prefabs 실행 권장.");
        BuildGameOverUIFallback(root);
        WireGameOverButton();
    }

    /// <summary>프리팹 미연결 시 최소 코드 생성 (화면 중앙 박스 + 텍스트).</summary>
    private void BuildGameOverUIFallback(Transform root)
    {
        // 전체 화면 어두운 백드롭
        var backdrop = MkImage(root, "GameOverBackdrop",
            new Vector2(0, 0), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            new Color(0, 0, 0, 0.9f));

        // SPEC-029.1: 백드롭 전체를 클릭 타깃으로 (클릭 시 타이틀 이동)
        _gameOverButton = backdrop.gameObject.AddComponent<Button>();

        // 화면 중앙 모달 박스
        var panel = MkImage(backdrop.transform, "Panel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(760, 360),
            new Color(0.12f, 0.04f, 0.04f, 0.96f));

        // 중앙 "GAME OVER" 텍스트 (폰트 42)
        _gameOverHeadlineText = MkText(panel.transform, "GAME OVER",
            new Vector2(0, 0), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            42, TextAnchor.MiddleCenter,
            new Color(1f, 0.35f, 0.35f));

        // SPEC-029.1: 클릭 안내 서브텍스트 (패널 하단)
        _gameOverHintText = MkText(panel.transform, "화면을 클릭하면 타이틀로 돌아갑니다",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 28f), new Vector2(0f, 40f),
            20, TextAnchor.MiddleCenter,
            new Color(0.85f, 0.85f, 0.85f, 0.7f));
    }

    /// <summary>SPEC-029.1: 전체화면 Button onClick → LoadTitleScene 연결.</summary>
    private void WireGameOverButton()
    {
        if (_gameOverButton == null) return;
        _gameOverButton.onClick.RemoveAllListeners();
        _gameOverButton.onClick.AddListener(LoadTitleScene);
        _gameOverButton.transition = Selectable.Transition.None;
    }

    /// <summary>
    /// GameOver 상태 진입 시 호출 (OnStateChanged에서 분기).
    /// 텍스트 표시 후 화면 클릭을 기다림 (SPEC-029.1 — 자동 이동 제거).
    /// </summary>
    private void ShowGameOverPanel()
    {
        if (_gameOverUI != null) _gameOverUI.SetActive(true);

        if (_gameOverHeadlineText != null)
            _gameOverHeadlineText.text = SafeLocFor("str.gameover.headline", "GAME OVER");

        if (_gameOverHintText != null)
            _gameOverHintText.text = SafeLocFor("str.gameover.hint", "화면을 클릭하면 타이틀로 돌아갑니다");

        // 재진입 대비 클릭 활성 복구
        if (_gameOverButton != null) _gameOverButton.interactable = true;

        Debug.Log("[GameOver] 패널 표시 → 화면 클릭 대기 (클릭 시 타이틀 이동)");
    }

    private void HideGameOverPanel()
    {
        if (_gameOverUI != null) _gameOverUI.SetActive(false);
    }

    /// <summary>
    /// 타이틀 화면으로 이동 (CLAUDE.md: TitleScene = 빌드 인덱스 0).
    /// 스코어 정보는 저장하지 않음. 캐릭터 개별 기록은 CalcScore에서 이미 저장됨.
    /// </summary>
    private void LoadTitleScene()
    {
        // SPEC-029.1: 중복 클릭 방지
        if (_gameOverButton != null) _gameOverButton.interactable = false;
        Debug.Log("[GameOver] 화면 클릭 → 타이틀 화면 이동");
        SceneManager.LoadScene("TitleScene");
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
