using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 베팅액 입력 모달 — START 버튼 클릭 시 팝업.
/// 슬라이더 + InputField + 빠른 버튼(±1, ±10) 3중 동기화 + 미리보기 실시간 갱신.
///
/// 사용 흐름:
///   1. SceneBootstrapper.Betting.OnStartBtn() → BetAmountModal.Show(bet, racers)
///   2. 유저가 베팅액 조정
///   3. [배팅 확정] → onConfirmed 콜백 호출 (실제 라운드 시작)
///   4. [취소] → onCancelled 콜백 호출 (베팅 화면 복귀)
///
/// SPEC-028 Steps 2.3, 2.4, 2.6, 2.7, 2.8, 2.9, 2.10, 2.11
/// </summary>
public class BetAmountModal : MonoBehaviour
{
    [Header("─── 정보 표시 ───")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text betTypeText;
    [SerializeField] private Text selectionText;
    [SerializeField] private Text oddsText;

    [Header("─── 배팅가능 표시 ───")]
    [SerializeField] private Text bettableLabel;    // "배팅가능:" 라벨 (Best Fit)
    [SerializeField] private Text jellyCountText;   // 보유 젤리 개수 (= 배팅 가능 최대치)

    [Header("─── 입력 ───")]
    [SerializeField] private Slider amountSlider;
    [SerializeField] private InputField amountInput;
    [SerializeField] private Button btnMinus10;
    [SerializeField] private Button btnMinus1;
    [SerializeField] private Button btnPlus1;
    [SerializeField] private Button btnPlus10;

    [Header("─── 미리보기 ───")]
    [SerializeField] private Text previewWinText;
    [SerializeField] private Text previewLoseText;

    [Header("─── 버튼 ───")]
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Text errorText;          // 에러 메시지 (잔액 부족 등)

    [Header("─── 백드롭 ───")]
    [SerializeField] private GameObject backdrop;     // 어두운 반투명 (외부 클릭 차단)

    // 콜백
    private Action<int> _onConfirmed;
    private Action _onCancelled;

    // 현재 베팅 상태
    private BetInfo _currentBet;
    private float _currentOdds;
    private int _amount;
    private bool _isUpdatingFromSlider;     // 슬라이더↔InputField 무한 루프 방지
    private bool _isUpdatingFromInput;

    private void Awake()
    {
        // 시작 시 비활성
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // 이벤트 바인딩 (한 번만)
        if (amountSlider != null)
            amountSlider.onValueChanged.AddListener(OnSliderChanged);
        if (amountInput != null)
            amountInput.onValueChanged.AddListener(OnInputChanged);
        if (btnMinus10 != null) btnMinus10.onClick.AddListener(() => ApplyDelta(-10));
        if (btnMinus1 != null) btnMinus1.onClick.AddListener(() => ApplyDelta(-1));
        if (btnPlus1 != null) btnPlus1.onClick.AddListener(() => ApplyDelta(+1));
        if (btnPlus10 != null) btnPlus10.onClick.AddListener(() => ApplyDelta(+10));
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
        // 백드롭 클릭 = 취소 (모달 밖 영역 클릭 시 닫기)
        if (backdrop != null)
        {
            var bdBtn = backdrop.GetComponent<Button>() ?? backdrop.AddComponent<Button>();
            bdBtn.onClick.AddListener(OnCancel);
        }
    }

    private void OnDisable()
    {
        // 이벤트 해제 (메모리 누수 방지)
        if (amountSlider != null)
            amountSlider.onValueChanged.RemoveAllListeners();
        if (amountInput != null)
            amountInput.onValueChanged.RemoveAllListeners();
        if (btnMinus10 != null) btnMinus10.onClick.RemoveAllListeners();
        if (btnMinus1 != null) btnMinus1.onClick.RemoveAllListeners();
        if (btnPlus1 != null) btnPlus1.onClick.RemoveAllListeners();
        if (btnPlus10 != null) btnPlus10.onClick.RemoveAllListeners();
        if (confirmButton != null) confirmButton.onClick.RemoveAllListeners();
        if (cancelButton != null) cancelButton.onClick.RemoveAllListeners();
        if (backdrop != null)
        {
            var bdBtn = backdrop.GetComponent<Button>();
            if (bdBtn != null) bdBtn.onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// 모달 표시. Step 2.6: 기본값 = 보유 젤리의 25%.
    /// </summary>
    /// <param name="bet">현재 베팅 정보 (타입·선택·배당 표시용)</param>
    /// <param name="oddsForBet">계산된 배당 (UI 표시 + 미리보기 계산용)</param>
    /// <param name="selectionLabel">선택된 캐릭터 이름들 (쉼표 구분)</param>
    /// <param name="onConfirmed">확정 시 호출 (베팅액 전달)</param>
    /// <param name="onCancelled">취소 시 호출</param>
    public void Show(BetInfo bet, float oddsForBet, string selectionLabel,
                     Action<int> onConfirmed, Action onCancelled)
    {
        if (bet == null || WalletManager.Instance == null)
        {
            Debug.LogError("[BetAmountModal] Show 실패: bet 또는 WalletManager null");
            onCancelled?.Invoke();
            return;
        }

        _currentBet = bet;
        _currentOdds = oddsForBet;
        _onConfirmed = onConfirmed;
        _onCancelled = onCancelled;

        int maxBet = WalletManager.Instance.Jelly;
        if (maxBet < 1)
        {
            // 보유 젤리 0 → 모달 진입 차단 (CalcScore에서 GameOver 처리되어 이 상황은 없어야 정상)
            Debug.LogWarning("[BetAmountModal] 보유 젤리 0 → 모달 진입 차단");
            onCancelled?.Invoke();
            return;
        }

        // 슬라이더 범위 설정
        if (amountSlider != null)
        {
            amountSlider.minValue = 1;
            amountSlider.maxValue = maxBet;
            amountSlider.wholeNumbers = true;
        }

        // 배팅가능 표시 (라벨 + 보유 젤리 = 배팅 가능 최대치)
        if (bettableLabel != null) bettableLabel.text = SafeLoc("str.bet.modal.bettable", "배팅가능:");
        if (jellyCountText != null) jellyCountText.text = maxBet.ToString();

        // 정보 표시
        if (titleText != null) titleText.text = SafeLoc("str.bet.modal.title", "🎰 배팅액을 정하세요");
        if (betTypeText != null) betTypeText.text = SafeLoc("str.bet.modal.bet_type", "종목: {0}",
            BettingCalculator.GetTypeName(bet.type));
        if (selectionText != null) selectionText.text = SafeLoc("str.bet.modal.selection", "선택: {0}", selectionLabel);
        if (oddsText != null) oddsText.text = SafeLoc("str.bet.modal.odds", "배당: {0}x",
            oddsForBet.ToString("F1"));

        // 빠른 버튼 라벨
        if (btnMinus10 != null) SetButtonLabel(btnMinus10, SafeLoc("str.bet.modal.quick.minus10", "-10"));
        if (btnMinus1 != null) SetButtonLabel(btnMinus1, SafeLoc("str.bet.modal.quick.minus1", "-1"));
        if (btnPlus1 != null) SetButtonLabel(btnPlus1, SafeLoc("str.bet.modal.quick.plus1", "+1"));
        if (btnPlus10 != null) SetButtonLabel(btnPlus10, SafeLoc("str.bet.modal.quick.plus10", "+10"));
        if (confirmButton != null) SetButtonLabel(confirmButton, SafeLoc("str.bet.modal.confirm", "배팅 확정 →"));
        if (cancelButton != null) SetButtonLabel(cancelButton, SafeLoc("str.bet.modal.cancel", "취소"));

        // Step 2.6: 기본값 = 보유 젤리의 25% (최소 1, 최대 보유)
        int defaultAmount = Mathf.Max(1, Mathf.RoundToInt(maxBet * 0.25f));
        SetAmount(defaultAmount);

        // 에러 텍스트 초기화
        if (errorText != null) errorText.text = "";

        // 표시
        if (backdrop != null) backdrop.SetActive(true);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        // 모달 위에 떠 있던 젤리 설명 팝업도 함께 닫기 (잔류 방지)
        if (ItemInfoPopup.Instance != null) ItemInfoPopup.Instance.Hide();
        if (backdrop != null) backdrop.SetActive(false);
        gameObject.SetActive(false);
    }

    // Step 2.7: 빠른 버튼 — 현재값에 ±N 적용 + Clamp
    private void ApplyDelta(int delta)
    {
        int newAmount = _amount + delta;
        SetAmount(newAmount);
    }

    // Step 2.8: 슬라이더 → InputField 동기화
    private void OnSliderChanged(float value)
    {
        if (_isUpdatingFromInput) return;   // 무한 루프 방지
        _isUpdatingFromSlider = true;
        SetAmount(Mathf.RoundToInt(value));
        _isUpdatingFromSlider = false;
    }

    // Step 2.8: InputField → 슬라이더 동기화 + 자동 클램프
    private void OnInputChanged(string value)
    {
        if (_isUpdatingFromSlider) return;   // 무한 루프 방지
        _isUpdatingFromInput = true;
        if (int.TryParse(value, out int parsed))
        {
            SetAmount(parsed);
        }
        else if (string.IsNullOrEmpty(value))
        {
            // 빈 입력은 일시 허용 (사용자가 지우는 중) — 확정 시점에 검증
        }
        _isUpdatingFromInput = false;
    }

    // 모든 입력의 단일 진입점 — Clamp + UI 동기화 + 미리보기 갱신
    private void SetAmount(int amount)
    {
        int max = (WalletManager.Instance != null) ? WalletManager.Instance.Jelly : 1;
        _amount = Mathf.Clamp(amount, 1, Mathf.Max(1, max));

        // 슬라이더 갱신 (input에서 호출된 경우)
        if (!_isUpdatingFromSlider && amountSlider != null)
            amountSlider.SetValueWithoutNotify(_amount);

        // InputField 갱신 (slider/button에서 호출된 경우)
        if (!_isUpdatingFromInput && amountInput != null)
            amountInput.SetTextWithoutNotify(_amount.ToString());

        RefreshPreview();
        UpdateConfirmButtonState();
    }

    // Step 2.9 → SPEC-047: 미리보기. 젤리는 배팅 시 소멸 → 적중 보상은 스톤만.
    private void RefreshPreview()
    {
        if (_currentBet == null) return;

        // 적중 시 받는 스톤 = ceil(amount × odds) — 실제 보상(CalculateReward)과 동일.
        // 젤리는 배팅 시 차감되어 적중해도 반환되지 않으므로 젤리 순이익 표시 제거.
        int stoneGain = Mathf.CeilToInt(_amount * Mathf.Max(1.1f, _currentOdds));

        if (previewWinText != null)
            previewWinText.text = SafeLoc("str.bet.modal.preview.win",
                "적중 시: +{0} 💎", stoneGain);

        if (previewLoseText != null)
            previewLoseText.text = SafeLoc("str.bet.modal.preview.lose",
                "실패 시: -{0} 🟦", _amount);
    }

    private void UpdateConfirmButtonState()
    {
        if (confirmButton == null) return;
        var wallet = WalletManager.Instance;
        bool canConfirm = wallet != null && _amount >= 1 && _amount <= wallet.Jelly;
        confirmButton.interactable = canConfirm;
    }

    // Step 2.10: 확정 → WalletManager.TryBet + 콜백
    private void OnConfirm()
    {
        var wallet = WalletManager.Instance;
        if (wallet == null) return;

        if (!wallet.TryBet(_amount))
        {
            // 잔액 부족 (정상 흐름에선 안 됨, 안전망)
            if (errorText != null) errorText.text = SafeLoc("str.bet.modal.error.too_high",
                "보유 젤리보다 많이 베팅할 수 없습니다");
            return;
        }

        // betAmount 필드에 기록 (CalcScore에서 사용)
        if (_currentBet != null) _currentBet.betAmount = _amount;

        var cb = _onConfirmed;
        Hide();
        cb?.Invoke(_amount);
    }

    // Step 2.11: 취소 → 모달 닫기 (Wallet 변경 없음)
    private void OnCancel()
    {
        var cb = _onCancelled;
        Hide();
        cb?.Invoke();
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
    /// 외부에서 모달 컴포넌트 참조를 동적으로 주입 (SceneBootstrapper에서 임시 레이아웃 만든 후 사용).
    /// </summary>
    public void SetReferences(
        Text title, Text betType, Text selection, Text odds,
        Slider slider, InputField input,
        Button minus10, Button minus1, Button plus1, Button plus10,
        Text previewWin, Text previewLose,
        Button cancel, Button confirm, Text error,
        GameObject backdropObj)
    {
        titleText = title;
        betTypeText = betType;
        selectionText = selection;
        oddsText = odds;
        amountSlider = slider;
        amountInput = input;
        btnMinus10 = minus10;
        btnMinus1 = minus1;
        btnPlus1 = plus1;
        btnPlus10 = plus10;
        previewWinText = previewWin;
        previewLoseText = previewLose;
        cancelButton = cancel;
        confirmButton = confirm;
        errorText = error;
        backdrop = backdropObj;
    }
}
