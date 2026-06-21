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
    }

    public void Show()
    {
        if (backdrop != null) backdrop.SetActive(true);
        gameObject.SetActive(true);

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
        if (holdingText != null) holdingText.text = SafeLoc("str.exchange.modal.holding",
            "현재 보유: {0}개", stone);

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

        bool ok;
        switch (wallet.GetAvailableAction())
        {
            case WalletManager.ExchangeAction.CatPower: ok = wallet.TryUseCatPower(); break;
            case WalletManager.ExchangeAction.Rescue:   ok = wallet.TryRescue();      break;
            default:
                Debug.LogWarning("[ExchangeModal] 사용 가능한 환전 액션 없음");
                return;
        }
        if (!ok) Debug.LogWarning("[ExchangeModal] 환전 실행 실패");

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
