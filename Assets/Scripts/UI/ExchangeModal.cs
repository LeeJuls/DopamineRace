using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 도파민 스톤 → 젤리 환전 팝업.
/// 베팅 화면 헤더 우측 환전 아이콘(💱) 클릭 시 표시.
///
/// 4가지 분기:
///   1. 일반 환전 가능 (Stone ≥ 비율) → "환전: 💎N → 🟦1" 버튼
///   2. 구제 환전 (Jelly=0 + Stone<비율, R19 확장) → "환전: 💎{보유} → 🟦1 (구제)" 버튼
///   3. 환전 완료 (이번 라운드 사용) → 비활성 + "이번 라운드 환전 완료"
///   4. 환전 불가 (스톤 부족 + 구제 X) → 비활성 + "스톤 N개 필요"
///
/// SPEC-028 Steps 2.15, 2.16 (R16~R20)
/// </summary>
public class ExchangeModal : MonoBehaviour
{
    [Header("─── 정보 표시 ───")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text rateText;             // "이번 라운드 환전율: N:1"
    [SerializeField] private Text rateDescText;         // "(스톤 N개 → 젤리 1개)"
    [SerializeField] private Text holdingText;          // "현재 보유: 🟦X 💎Y"

    [Header("─── 환전 옵션 ───")]
    [SerializeField] private Text optionsTitleText;     // "─── 환전 옵션 ───"
    [SerializeField] private Button exchangeButton;
    [SerializeField] private Text exchangeButtonText;
    [SerializeField] private GameObject rescueIndicator;   // "⚡ 구제 환전" 라벨 컨테이너
    [SerializeField] private Text rescueIndicatorText;
    [SerializeField] private GameObject completedIndicator;  // "✅ 환전 완료" 라벨 컨테이너
    [SerializeField] private Text completedText;
    [SerializeField] private Text completedNextText;
    [SerializeField] private Text errorText;            // "스톤 N개 필요" 등
    [SerializeField] private Text noteText;             // "※ 라운드당 1회"

    [Header("─── 닫기 ───")]
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject backdrop;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // 환전 버튼 호버 팝 효과 (런타임 부착 — 프리팹 무변경, 중복 방지)
        if (exchangeButton != null && exchangeButton.GetComponent<HoverScale>() == null)
            exchangeButton.gameObject.AddComponent<HoverScale>();

        if (exchangeButton != null)
            exchangeButton.onClick.AddListener(OnExchangeClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        // 백드롭 클릭 = 닫기 (모달 밖 영역 클릭 시 닫기)
        if (backdrop != null)
        {
            var bdBtn = backdrop.GetComponent<Button>() ?? backdrop.AddComponent<Button>();
            bdBtn.onClick.AddListener(Hide);
        }

        if (WalletManager.Instance != null)
            WalletManager.Instance.OnExchangeStateChanged += RefreshUI;
    }

    private void OnDisable()
    {
        if (exchangeButton != null)
            exchangeButton.onClick.RemoveAllListeners();
        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();
        if (backdrop != null)
        {
            var bdBtn = backdrop.GetComponent<Button>();
            if (bdBtn != null) bdBtn.onClick.RemoveAllListeners();
        }

        if (WalletManager.Instance != null)
            WalletManager.Instance.OnExchangeStateChanged -= RefreshUI;

        // 모달 닫힘 시 진행 중 획득 연출 즉시 제거 (잔상 방지)
        if (_activeBurst != null) { Destroy(_activeBurst.gameObject); _activeBurst = null; }
    }

    public void Show()
    {
        if (backdrop != null) backdrop.SetActive(true);
        gameObject.SetActive(true);
        HideConfirm();   // 재진입 시 확인 팝업 잔상 제거

        // 진입 시 항상 최신 상태로 갱신
        if (titleText != null) titleText.text = SafeLoc("str.exchange.modal.title", "💱 도파민 스톤 환전");
        if (optionsTitleText != null) optionsTitleText.text = SafeLoc("str.exchange.modal.options_title", "─── 환전 옵션 ───");
        SetButtonLabel(closeButton, SafeLoc("str.exchange.btn.close", "닫기"));

        RefreshUI();
    }

    public void Hide()
    {
        if (backdrop != null) backdrop.SetActive(false);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 상태 갱신 — GetAvailableAction() 우선순위(고양이의 힘 &gt; 구제 &gt; 없음) 기반.
    /// WalletManager.OnExchangeStateChanged 이벤트로 자동 호출 + 외부에서 직접 호출 가능.
    /// </summary>
    public void RefreshUI()
    {
        var wallet = WalletManager.Instance;
        if (wallet == null) return;

        int rate     = wallet.CurrentExchangeRate;
        int stone    = wallet.Stone;
        int usesLeft = wallet.CatPowerUsesLeft;
        var action   = wallet.GetAvailableAction();

        bool showCat    = (action == WalletManager.ExchangeAction.CatPower);
        bool showRescue = (action == WalletManager.ExchangeAction.Rescue);
        bool blocked    = (action == WalletManager.ExchangeAction.None);

        // 비율·보유량 표시
        if (rateText != null) rateText.text = SafeLoc("str.exchange.modal.rate",
            "이번 라운드 환전율: {0}:1", rate);
        if (rateDescText != null) rateDescText.text = SafeLoc("str.exchange.modal.rate_desc",
            "(스톤 {0}개 → 젤리 1개)", rate);
        if (holdingText != null)
        {
            // 픽셀폰트(size 38)로 넓어진 보유 숫자가 Wrap+Truncate로 잘리던 버그 방지 → 한 줄 유지
            holdingText.horizontalOverflow = HorizontalWrapMode.Overflow;
            holdingText.text = SafeLoc("str.exchange.modal.holding", "현재 보유: {0}개", stone);
        }

        // 인디케이터 초기화
        if (rescueIndicator != null) rescueIndicator.SetActive(showRescue);
        if (completedIndicator != null) completedIndicator.SetActive(false);
        if (errorText != null) errorText.gameObject.SetActive(false);

        // 액션 버튼
        if (exchangeButton != null && exchangeButtonText != null)
        {
            exchangeButton.interactable = (showCat || showRescue);

            if (showCat)
            {
                exchangeButtonText.text = SafeLoc("str.exchange.catpower.btn",
                    "🐾 전부 환전: 💎{0} → 🟦{1}", wallet.GetCatPowerStoneCost(), wallet.GetCatPowerJellyGain());
            }
            else if (showRescue)
            {
                exchangeButtonText.text = SafeLoc("str.exchange.btn.rescue_action",
                    "환전: 💎{0} → 🟦1 (구제)", Mathf.Min(stone, rate));
                if (rescueIndicatorText != null)
                    rescueIndicatorText.text = SafeLoc("str.exchange.btn.rescue", "⚡ 구제 (라운드당 1회)");
            }
            else
            {
                exchangeButtonText.text = ""; // 비활성
            }
        }

        // 하단 안내 — 고양이의 힘 남은 횟수 / 소진
        if (noteText != null)
            noteText.text = (usesLeft > 0)
                ? SafeLoc("str.exchange.catpower.remain", "🐾 고양이의 힘 · 남은 {0}회", usesLeft)
                : SafeLoc("str.exchange.catpower.used", "🐾 고양이의 힘 사용 완료 (이번 게임)");

        // 비활성 사유 표시
        if (blocked)
        {
            bool rescueUsed   = (wallet.Jelly == 0 && wallet.RescuedThisRound);
            bool catExhausted = (usesLeft <= 0 && stone >= rate);

            if (rescueUsed)
            {
                if (completedIndicator != null) completedIndicator.SetActive(true);
                if (completedText != null)
                    completedText.text = SafeLoc("str.exchange.completed", "이번 라운드 구제 완료");
                if (completedNextText != null)
                    completedNextText.text = SafeLoc("str.exchange.completed_next", "다음 라운드에 다시 가능");
            }
            else if (catExhausted)
            {
                if (completedIndicator != null) completedIndicator.SetActive(true);
                if (completedText != null)
                    completedText.text = SafeLoc("str.exchange.catpower.used", "🐾 고양이의 힘 사용 완료 (이번 게임)");
                if (completedNextText != null)
                    completedNextText.text = SafeLoc("str.exchange.catpower.next_game", "다음 게임에 다시 가능");
            }
            else if (errorText != null)
            {
                errorText.gameObject.SetActive(true);
                errorText.text = SafeLoc("str.exchange.error.insufficient",
                    "스톤이 부족합니다 ({0}개 필요)", rate);
            }
        }
    }

    private void OnExchangeClicked()
    {
        var wallet = WalletManager.Instance;
        if (wallet == null) return;

        // 즉시 실행하지 않고 확인 팝업 표시 → Yes에서 실제 실행
        switch (wallet.GetAvailableAction())
        {
            case WalletManager.ExchangeAction.CatPower:
            {
                int gain = wallet.GetCatPowerJellyGain();
                int left = wallet.CatPowerUsesLeft;
                ShowConfirm(() => wallet.TryUseCatPower(), gain, true, left, Mathf.Max(0, left - 1));
                break;
            }
            case WalletManager.ExchangeAction.Rescue:
                ShowConfirm(() => wallet.TryRescue(), 1, false, 0, 0);
                break;
            default:
                Debug.LogWarning("[ExchangeModal] 사용 가능한 환전 액션 없음");
                break;
        }
    }

    // ───────── 교환 확인 팝업 (런타임 빌드 · 프리팹 무변경) ─────────

    private GameObject _confirmRoot;
    private Text _confirmGainText;
    private Text _confirmUsesText;
    private System.Func<bool> _pendingExchange;   // 실행 성공 여부 반환
    private int _pendingGain;                      // 연출용 — 실행 전 캡처값
    private RewardBurst _activeBurst;

    /// <summary>교환 전 확인 팝업 표시. Yes에서 onYes(실제 환전) 실행.</summary>
    private void ShowConfirm(System.Func<bool> onYes, int jellyGain, bool showUses, int usesBefore, int usesAfter)
    {
        EnsureConfirmBuilt();
        _pendingExchange = onYes;
        _pendingGain = jellyGain;

        if (_confirmGainText != null)
            _confirmGainText.text = SafeLoc("str.exchange.confirm.gain", "획득 도파민 젤리: {0}", jellyGain);
        if (_confirmUsesText != null)
        {
            _confirmUsesText.gameObject.SetActive(showUses);
            if (showUses)
                _confirmUsesText.text = SafeLoc("str.exchange.confirm.uses", "남은 횟수 {0} → {1}", usesBefore, usesAfter);
        }

        _confirmRoot.SetActive(true);
        _confirmRoot.transform.SetAsLastSibling();   // 모달 위 최상단
    }

    private void HideConfirm()
    {
        _pendingExchange = null;
        if (_confirmRoot != null) _confirmRoot.SetActive(false);
    }

    private void OnConfirmYes()
    {
        var act = _pendingExchange;
        int gain = _pendingGain;
        HideConfirm();

        bool ok = act != null && act();
        RefreshUI();

        if (ok)
        {
            // 획득 연출 — 모달과 수명 분리(부모=메인 캔버스). 재진입 시 기존 것 제거.
            if (_activeBurst != null) Destroy(_activeBurst.gameObject);
            Transform parent = (transform.parent != null) ? transform.parent : transform;
            _activeBurst = RewardBurst.Spawn(parent, gain, titleText != null ? titleText.font : null);
        }
    }

    private void EnsureConfirmBuilt()
    {
        if (_confirmRoot != null) return;
        Font font = (titleText != null) ? titleText.font : null;

        // 전체 딤 백드롭 (뒤 모달 클릭 차단)
        _confirmRoot = NewUIChild("ExchangeConfirm", transform, Vector2.zero, Vector2.one, Vector2.zero);
        var rrt = _confirmRoot.GetComponent<RectTransform>();
        rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
        var dim = _confirmRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        dim.raycastTarget = true;

        // 패널
        var panel = NewUIChild("Panel", _confirmRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460, 300));
        var pimg = panel.AddComponent<Image>();
        pimg.color = Color.white;

        var title = NewTextChild("Title", panel.transform, font, 30, new Color(0.15f, 0.15f, 0.2f),
            new Vector2(0.5f, 1f), new Vector2(0, -52), new Vector2(420, 56));
        title.text = SafeLoc("str.exchange.confirm.title", "진짜 교환하시겠습니까?");

        _confirmGainText = NewTextChild("Gain", panel.transform, font, 26, new Color(0.1f, 0.2f, 0.45f),
            new Vector2(0.5f, 0.5f), new Vector2(0, 28), new Vector2(420, 44));
        _confirmUsesText = NewTextChild("Uses", panel.transform, font, 22, new Color(0.35f, 0.2f, 0.1f),
            new Vector2(0.5f, 0.5f), new Vector2(0, -16), new Vector2(420, 38));

        var yes = NewButtonChild("Yes", panel.transform, font, SafeLoc("str.ui.option.yes", "예"),
            new Color(0.30f, 0.50f, 0.75f), new Vector2(-100, 50), new Vector2(170, 60));
        yes.onClick.AddListener(OnConfirmYes);
        var no = NewButtonChild("No", panel.transform, font, SafeLoc("str.ui.option.no", "아니오"),
            new Color(0.45f, 0.45f, 0.5f), new Vector2(100, 50), new Vector2(170, 60));
        no.onClick.AddListener(HideConfirm);

        _confirmRoot.SetActive(false);
    }

    // ── UGUI 런타임 빌드 헬퍼 ──
    private static GameObject NewUIChild(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 size)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    private static Text NewTextChild(string name, Transform parent, Font font, int size, Color color,
        Vector2 anchor, Vector2 pos, Vector2 sd)
    {
        var go = NewUIChild(name, parent, anchor, anchor, sd);
        go.GetComponent<RectTransform>().anchoredPosition = pos;
        var t = go.AddComponent<Text>();
        if (font != null) t.font = font;
        t.fontSize = size; t.fontStyle = FontStyle.Bold; t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static Button NewButtonChild(string name, Transform parent, Font font, string label, Color bg,
        Vector2 pos, Vector2 sd)
    {
        var go = NewUIChild(name, parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), sd);
        go.GetComponent<RectTransform>().anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var t = NewTextChild("Text", go.transform, font, 26, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, sd);
        var trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        t.text = label;
        return btn;
    }

    // ───── 유틸 ─────

    private static string SafeLoc(string key, string fallback, params object[] args)
    {
        string val = Loc.Get(key);
        if (string.IsNullOrEmpty(val) || val == key)
            val = fallback;
        if (args != null && args.Length > 0)
        {
            try { val = string.Format(val, args); } catch { /* swallow */ }
        }
        return val;
    }

    private static void SetButtonLabel(Button btn, string text)
    {
        if (btn == null) return;
        var t = btn.GetComponentInChildren<Text>();
        if (t != null) t.text = text;
    }

    /// <summary>
    /// SceneBootstrapper가 임시 레이아웃 만든 후 참조 주입.
    /// </summary>
    public void SetReferences(
        Text title, Text rate, Text rateDesc, Text holding, Text optionsTitle,
        Button exchangeBtn, Text exchangeBtnText,
        GameObject rescueIndic, Text rescueText,
        GameObject completedIndic, Text completed, Text completedNext,
        Text error, Text note,
        Button close, GameObject backdropObj)
    {
        titleText = title;
        rateText = rate;
        rateDescText = rateDesc;
        holdingText = holding;
        optionsTitleText = optionsTitle;
        exchangeButton = exchangeBtn;
        exchangeButtonText = exchangeBtnText;
        rescueIndicator = rescueIndic;
        rescueIndicatorText = rescueText;
        completedIndicator = completedIndic;
        completedText = completed;
        completedNextText = completedNext;
        errorText = error;
        noteText = note;
        closeButton = close;
        backdrop = backdropObj;
    }
}
