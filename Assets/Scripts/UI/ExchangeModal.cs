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
        if (exchangeButton != null)
            exchangeButton.onClick.AddListener(OnExchangeClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (WalletManager.Instance != null)
            WalletManager.Instance.OnExchangeStateChanged += RefreshUI;
    }

    private void OnDisable()
    {
        if (exchangeButton != null)
            exchangeButton.onClick.RemoveAllListeners();
        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();

        if (WalletManager.Instance != null)
            WalletManager.Instance.OnExchangeStateChanged -= RefreshUI;
    }

    public void Show()
    {
        if (backdrop != null) backdrop.SetActive(true);
        gameObject.SetActive(true);

        // 진입 시 항상 최신 상태로 갱신
        if (titleText != null) titleText.text = SafeLoc("str.exchange.modal.title", "💱 도파민 스톤 환전");
        if (optionsTitleText != null) optionsTitleText.text = SafeLoc("str.exchange.modal.options_title", "─── 환전 옵션 ───");
        if (noteText != null) noteText.text = SafeLoc("str.exchange.note.once_per_round", "※ 라운드당 1회만 가능");
        SetButtonLabel(closeButton, SafeLoc("str.exchange.btn.close", "닫기"));

        RefreshUI();
    }

    public void Hide()
    {
        if (backdrop != null) backdrop.SetActive(false);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 4가지 분기에 따라 UI 상태 갱신.
    /// WalletManager.OnExchangeStateChanged 이벤트로 자동 호출 + 외부에서 직접 호출 가능.
    /// </summary>
    public void RefreshUI()
    {
        var wallet = WalletManager.Instance;
        if (wallet == null) return;

        int rate = wallet.CurrentExchangeRate;
        int jelly = wallet.Jelly;
        int stone = wallet.Stone;
        bool exchanged = wallet.ExchangedThisRound;
        bool canExchange = wallet.CanExchange();
        bool isRescue = (jelly == 0 && stone >= 1 && stone < rate);

        // 비율·보유량 표시
        if (rateText != null) rateText.text = SafeLoc("str.exchange.modal.rate",
            "이번 라운드 환전율: {0}:1", rate);
        if (rateDescText != null) rateDescText.text = SafeLoc("str.exchange.modal.rate_desc",
            "(스톤 {0}개 → 젤리 1개)", rate);
        if (holdingText != null) holdingText.text = SafeLoc("str.exchange.modal.holding",
            "현재 보유: 🟦{0}  💎{1}", jelly, stone);

        // 4분기 — completed 우선, 그 다음 canExchange, rescue 표시
        bool showRescue = canExchange && isRescue;
        bool showCompleted = exchanged;
        bool showError = !canExchange && !exchanged;

        if (rescueIndicator != null) rescueIndicator.SetActive(showRescue);
        if (completedIndicator != null) completedIndicator.SetActive(showCompleted);
        if (errorText != null) errorText.gameObject.SetActive(showError);

        // 환전 버튼
        if (exchangeButton != null && exchangeButtonText != null)
        {
            exchangeButton.interactable = canExchange;
            int stoneCost = wallet.GetExchangeStoneCost();

            if (showRescue)
            {
                exchangeButtonText.text = SafeLoc("str.exchange.btn.rescue_action",
                    "환전: 💎{0} → 🟦1 (구제)", stoneCost);
                if (rescueIndicatorText != null)
                    rescueIndicatorText.text = SafeLoc("str.exchange.btn.rescue", "⚡ 구제 환전 (1회 한정)");
            }
            else if (canExchange)
            {
                exchangeButtonText.text = SafeLoc("str.exchange.btn",
                    "환전: 💎{0} → 🟦1", stoneCost);
            }
            else
            {
                exchangeButtonText.text = ""; // 비활성 상태
            }
        }

        // 완료 표시
        if (showCompleted)
        {
            if (completedText != null)
                completedText.text = SafeLoc("str.exchange.completed", "✅ 이번 라운드 환전 완료");
            if (completedNextText != null)
                completedNextText.text = SafeLoc("str.exchange.completed_next", "다음 라운드에 다시 가능");
        }

        // 에러 표시
        if (showError && errorText != null)
        {
            errorText.text = SafeLoc("str.exchange.error.insufficient",
                "스톤이 부족합니다 ({0}개 필요)", rate);
        }
    }

    private void OnExchangeClicked()
    {
        var wallet = WalletManager.Instance;
        if (wallet == null) return;

        if (!wallet.TryExchange())
        {
            Debug.LogWarning("[ExchangeModal] TryExchange 실패");
            return;
        }

        // 즉시 갱신 (OnExchangeStateChanged 이벤트로도 갱신되지만 명시 호출)
        RefreshUI();
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
