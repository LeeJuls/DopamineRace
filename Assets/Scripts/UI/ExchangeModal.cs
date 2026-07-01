using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 럭키 잭팟 모달 (SPEC-051) — 스톤 전부를 (스톤 × N) 젤리로. 게임당 1회.
/// 두 진입:
///   - Show()     : 자발 (고양이 클릭) — 닫기 허용.
///   - ShowAuto() : 0젤리 자동 — 강제(닫기 버튼·backdrop 비활성), 당겨야 진행.
/// 연출: [잭팟 당기기] → N 가중 랜덤 → "1 스톤 → N 젤리!" 카운트업.
///
/// SerializeField 필드·SetReferences 16인자 시그니처는 프리팹/폴백빌더 호환을 위해 유지.
/// (구 구제/완료 라벨 필드는 잭팟 의미로 재활용 또는 미사용)
/// SPEC-028 Steps 2.15-2.16 → SPEC-051 잭팟 재설계
/// </summary>
public class ExchangeModal : MonoBehaviour
{
    [Header("─── 정보 표시 ───")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text rateText;             // 잭팟: 헤더/프롬프트 ("1 스톤 → ? 젤리")
    [SerializeField] private Text rateDescText;         // 잭팟: 결과 ("1 스톤 → N 젤리!")
    [SerializeField] private Text holdingText;          // "현재 보유: 💎X"

    [Header("─── 잭팟 옵션 ───")]
    [SerializeField] private Text optionsTitleText;
    [SerializeField] private Button exchangeButton;     // 잭팟: [잭팟 당기기]
    [SerializeField] private Text exchangeButtonText;
    [SerializeField] private GameObject rescueIndicator;   // (미사용 — 시그니처 유지)
    [SerializeField] private Text rescueIndicatorText;
    [SerializeField] private GameObject completedIndicator;  // 잭팟: 사용 완료 라벨 컨테이너
    [SerializeField] private Text completedText;
    [SerializeField] private Text completedNextText;
    [SerializeField] private Text errorText;            // (미사용 — 시그니처 유지)
    [SerializeField] private Text noteText;             // "🐾 럭키 잭팟 · 게임당 1회"

    [Header("─── 닫기 ───")]
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject backdrop;

    private bool _isAutoMode;        // ShowAuto 진입 = 강제(닫기 차단)
    private bool _isRolling;         // 연출 중 (중복 클릭 차단)
    private RewardBurst _activeBurst;
    private GameObject _rollOverlay; // 연출 중 글씨 뒤 반투명 검정 판 (끝나면 제거)

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // 당기기 버튼 호버 팝 효과 (런타임 부착 — 프리팹 무변경, 중복 방지)
        if (exchangeButton != null && exchangeButton.GetComponent<HoverScale>() == null)
            exchangeButton.gameObject.AddComponent<HoverScale>();

        if (exchangeButton != null)
            exchangeButton.onClick.AddListener(OnPullClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        // 백드롭 클릭 = 닫기 (자발 모드에서만 — ShowAuto는 ApplyMode에서 비활성)
        if (backdrop != null)
        {
            var bdBtn = backdrop.GetComponent<Button>() ?? backdrop.AddComponent<Button>();
            bdBtn.onClick.AddListener(OnBackdropClicked);
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

        // 모달 닫힘 시 진행 중 연출 즉시 제거 (잔상·누수 방지)
        if (_activeBurst != null) { Destroy(_activeBurst.gameObject); _activeBurst = null; }
        if (_rollOverlay != null) { Destroy(_rollOverlay); _rollOverlay = null; }
        _isRolling = false;
        SFXManager.Instance?.StopLoop(); // SFX 안전망: 강제 비활성화 시 롤 사운드 잔존 방지
    }

    /// <summary>자발 진입 (고양이 클릭) — 닫기 허용.</summary>
    public void Show()
    {
        _isAutoMode = false;
        OpenInternal();
    }

    /// <summary>
    /// 0젤리 자동 진입 — 강제 모드(닫기 버튼·backdrop 비활성). 당겨야 진행.
    /// SPEC-051: TryShowBetAmountModal 0젤리 분기에서 호출.
    /// </summary>
    public void ShowAuto()
    {
        _isAutoMode = true;
        OpenInternal();
    }

    private void OpenInternal()
    {
        if (backdrop != null) backdrop.SetActive(true);
        gameObject.SetActive(true);
        _isRolling = false;

        // 진입 시 항상 최신 상태로 갱신
        if (titleText != null)
            titleText.text = SafeLoc("str.jackpot.modal.title", "🐾 마네키네코");
        if (optionsTitleText != null)
            optionsTitleText.text = SafeLoc("str.jackpot.desc", "스톤 전부를 젤리로! 게임당 1회");
        SetButtonLabel(closeButton, SafeLoc("str.exchange.btn.close", "닫기"));

        // 텍스트 중앙 정렬 통일 (SPEC-051 폴리싱) + desc 잘림 방지 wrap
        AlignCenter(titleText); AlignCenter(rateText); AlignCenter(rateDescText);
        AlignCenter(holdingText); AlignCenter(optionsTitleText); AlignCenter(noteText);
        if (optionsTitleText != null)
        {
            optionsTitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            optionsTitleText.verticalOverflow   = VerticalWrapMode.Overflow;
        }

        ApplyMode();
        RefreshUI();
    }

    /// <summary>자동/자발 모드별 닫기 경로 제어.</summary>
    private void ApplyMode()
    {
        bool canClose = !_isAutoMode;
        if (closeButton != null) closeButton.gameObject.SetActive(canClose);

        // 강제 모드: backdrop 클릭으로 우회 닫기 차단
        if (backdrop != null)
        {
            var bdBtn = backdrop.GetComponent<Button>();
            if (bdBtn != null) bdBtn.interactable = canClose;
        }
    }

    public void Hide()
    {
        if (_isRolling) return;   // 연출 중에는 닫기 무시 (입력 잠금)
        // 강제 모드에서는 당기기 전까지 닫힘 무시 (backdrop·닫기 모두)
        if (_isAutoMode) return;
        if (backdrop != null) backdrop.SetActive(false);
        gameObject.SetActive(false);
    }

    private void OnBackdropClicked()
    {
        if (_isAutoMode) return;   // 강제 모드 우회 차단
        Hide();
    }

    /// <summary>
    /// 잭팟 가능 여부에 따른 UI 갱신.
    /// WalletManager.OnExchangeStateChanged 이벤트로 자동 호출 + 외부 직접 호출 가능.
    /// </summary>
    public void RefreshUI()
    {
        var wallet = WalletManager.Instance;
        if (wallet == null) return;
        if (_isRolling) return;   // 연출 중에는 덮어쓰지 않음

        int stone     = wallet.Stone;
        int usesLeft  = wallet.JackpotUsesLeft;
        bool canPull  = wallet.CanJackpot();

        // 프롬프트 (자동/자발 헤더)
        if (rateText != null)
        {
            rateText.text = _isAutoMode
                ? SafeLoc("str.jackpot.auto.header", "젤리가 떨어졌어요! 마지막 기회")
                : SafeLoc("str.jackpot.modal.prompt", "1 스톤 → ? 젤리");
        }
        if (rateDescText != null) rateDescText.text = "";   // 결과 표시 전 비움

        if (holdingText != null)
        {
            holdingText.horizontalOverflow = HorizontalWrapMode.Overflow;
            holdingText.verticalOverflow   = VerticalWrapMode.Overflow;   // CJK 픽셀폰트(Fusion Pixel) 38pt 라인높이 ~50px > 박스 40px → Truncate면 한 줄이 통째로 잘려 사라짐(중국어 미표시 버그)
            holdingText.text = SafeLoc("str.exchange.modal.holding", "현재 보유: {0}개", stone);
        }

        // 미사용 인디케이터 숨김 (시그니처 유지 필드)
        if (rescueIndicator != null) rescueIndicator.SetActive(false);

        // 당기기 버튼 — 못 당길 때(스톤0/소진)는 빈 회색 버튼 노출 대신 숨김 (SPEC-051 폴리싱 #3)
        if (exchangeButton != null)
        {
            exchangeButton.gameObject.SetActive(canPull);
            if (canPull)
            {
                exchangeButton.interactable = true;
                if (exchangeButtonText != null)
                    exchangeButtonText.text = SafeLoc("str.jackpot.btn.pull", "🐾 고양이의 보은");
            }
        }

        // 스톤 부족 사유 (횟수는 남았는데 스톤이 없음 — 소진은 completedIndicator가 안내)
        if (errorText != null)
        {
            bool showNoStone = !canPull && usesLeft > 0;
            errorText.gameObject.SetActive(showNoStone);
            if (showNoStone)
            {
                AlignCenter(errorText);
                errorText.text = SafeLoc("str.jackpot.no_stone", "스톤이 없어요");
            }
        }

        // 잭팟 소진 시 — "사용 완료/다음 라운드" 대신 획득 젤리 표시 (게임당 1회라 결과 지속, SPEC-051)
        bool used = (usesLeft <= 0);
        if (completedIndicator != null) completedIndicator.SetActive(used);
        if (used)
        {
            if (completedText != null)
            {
                AlignCenter(completedText);
                completedText.text = SafeLoc("str.jackpot.gained", "{0} 도파민 젤리 획득!!!", wallet.LastJackpotJellyGain);
            }
            if (completedNextText != null) completedNextText.gameObject.SetActive(false);  // "다음 라운드" 라벨 제거
        }

        // 하단 안내 (항상 "게임당 1회")
        if (noteText != null)
            noteText.text = SafeLoc("str.jackpot.remain", "게임당 1회");
    }

    private void OnPullClicked()
    {
        var wallet = WalletManager.Instance;
        if (wallet == null) return;
        if (_isRolling) return;
        if (!wallet.CanJackpot())
        {
            Debug.LogWarning("[ExchangeModal] 잭팟 불가 (당기기 무시)");
            return;
        }

        // 연출 중 재입력 차단
        _isRolling = true;
        if (exchangeButton != null) exchangeButton.interactable = false;

        // 실제 잭팟 (autoFloor = 0젤리 자동 진입일 때만 바닥 보장)
        var result = wallet.TryJackpot(_isAutoMode);
        if (!result.success)
        {
            _isRolling = false;
            RefreshUI();
            return;
        }

        StartCoroutine(PlayRollThenResult(result.n, result.jellyGain));
    }

    /// <summary>N 굴림 연출(간단 스크롤) → 결과 표시 → 획득 버스트.</summary>
    private IEnumerator PlayRollThenResult(int finalN, int jellyGain)
    {
        SFXManager.Instance?.PlayLoop(SFXKeys.JackpotRoll);

        // 연출 강화 (SPEC-051): 글씨 뒤 반투명 검정 판 + 진노랑 글씨. 입력은 _isRolling가 차단.
        int origSibling = -1;
        if (rateDescText != null)
        {
            rateDescText.color = new Color(1f, 0.95f, 0.2f);   // 밝은 노란색 (검정 판 위 대비)
            Transform modalT = rateDescText.transform.parent;
            if (modalT != null)
            {
                _rollOverlay = new GameObject("RollOverlay", typeof(RectTransform), typeof(Image));
                _rollOverlay.transform.SetParent(modalT, false);
                var ort = _rollOverlay.GetComponent<RectTransform>();
                ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
                ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
                var oimg = _rollOverlay.GetComponent<Image>();
                oimg.color = new Color(0f, 0f, 0f, 0.6f);
                oimg.raycastTarget = true;                  // 모달 영역 클릭 흡수
                _rollOverlay.transform.SetAsLastSibling();
                origSibling = rateDescText.transform.GetSiblingIndex();
                rateDescText.transform.SetAsLastSibling();  // 글씨를 판 위로
            }
        }

        // 1) 빠른 스크롤 연출 (N 후보를 짧게 훑음)
        if (rateDescText != null)
        {
            const int spins = 12;
            for (int i = 0; i < spins; i++)
            {
                int fake = WalletManager.Instance != null ? WalletManager.Instance.RollJackpotN() : finalN;
                rateDescText.text = SafeLoc("str.jackpot.result", "1 스톤 → {0} 젤리!", fake);
                yield return new WaitForSeconds(0.04f + i * 0.006f);   // 점점 느려짐
            }
            // 2) 확정 N
            rateDescText.text = SafeLoc("str.jackpot.result", "1 스톤 → {0} 젤리!", finalN);
        }
        SFXManager.Instance?.StopLoop();
        SFXManager.Instance?.PlaySFX(SFXKeys.JackpotReveal);

        // 결과 음미 (검정 판 위 확정 N 0.5s 유지) → 판 제거 + sibling 복원
        yield return new WaitForSeconds(0.5f);
        if (_rollOverlay != null) { Destroy(_rollOverlay); _rollOverlay = null; }
        if (rateDescText != null && origSibling >= 0) rateDescText.transform.SetSiblingIndex(origSibling);

        if (rateText != null) rateText.text = SafeLoc("str.jackpot.modal.title", "🐾 마네키네코");

        // 3) 획득 연출 — 모달과 수명 분리(부모=메인 캔버스). 재진입 시 기존 것 제거.
        if (_activeBurst != null) Destroy(_activeBurst.gameObject);
        Transform parent = (transform.parent != null) ? transform.parent : transform;
        _activeBurst = RewardBurst.Spawn(parent, jellyGain, titleText != null ? titleText.font : null);

        _isRolling = false;
        RefreshUI();   // 사용 완료 라벨 + 닫기 안내 (자동 모드는 ApplyMode가 닫기 차단 유지)

        // 자동 모드: 잭팟 사용 후엔 닫기 허용(충전 완료 → 재START 유도). 모달은 열어둠.
        if (_isAutoMode)
        {
            _isAutoMode = false;
            ApplyMode();
            if (closeButton != null) SetButtonLabel(closeButton, SafeLoc("str.exchange.btn.close", "닫기"));
        }
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

    private static void AlignCenter(Text t)
    {
        if (t != null) t.alignment = TextAnchor.MiddleCenter;
    }

    /// <summary>
    /// SceneBootstrapper/프리팹 생성기가 임시 레이아웃 만든 후 참조 주입.
    /// 시그니처 유지 (Spec028UI.cs·CurrencyUIPrefabCreator.cs·ExchangeModalPrefab.prefab 호환).
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
