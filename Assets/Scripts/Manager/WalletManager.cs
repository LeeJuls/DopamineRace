using UnityEngine;
using System;

/// <summary>
/// 도파민 젤리·스톤 보유 관리 (게임 단위 휘발성).
/// - 매 게임 시작 시 ResetForNewGame() → 젤리 100, 스톤 0
/// - 베팅 시 TryBet(amount) — 잔액 부족 시 false 반환
/// - 적중 시 Reward(jelly, stone) — 둘 다 가산
/// - 마지막 라운드 완주 후 ResetStoneOnly() — 랭킹 제출 후 호출
///
/// SPEC-028 Step 1.3
/// </summary>
public class WalletManager : MonoBehaviour
{
    public static WalletManager Instance { get; private set; }

    private const int INITIAL_JELLY = 100;

    [Header("디버그 표시 (런타임 전용)")]
    [SerializeField] private int _jelly;
    [SerializeField] private int _stone;
    [SerializeField] private int _currentExchangeRate;
    [SerializeField] private bool _exchangedThisRound;

    /// <summary>현재 보유 젤리</summary>
    public int Jelly => _jelly;

    /// <summary>현재 보유 스톤 (게임 종료 시 리셋)</summary>
    public int Stone => _stone;

    /// <summary>
    /// 이번 라운드 스톤 → 젤리 환전 비율 (스톤 N → 젤리 1).
    /// SPEC-028 Step 2.14 / R17. 매 라운드 시작 시 RollExchangeRate()로 갱신.
    /// </summary>
    public int CurrentExchangeRate => _currentExchangeRate;

    /// <summary>
    /// 이번 라운드 환전 사용 여부 (라운드당 1회 제한).
    /// SPEC-028 Step 2.14 / R18. 라운드 변경 시 false로 자동 리셋.
    /// </summary>
    public bool ExchangedThisRound => _exchangedThisRound;

    /// <summary>젤리 또는 스톤 변경 시 발생 (UI 구독용)</summary>
    public event Action OnChanged;

    /// <summary>환전 비율 또는 환전 가능 여부 변경 시 발생 (Exchange UI 구독용)</summary>
    public event Action OnExchangeStateChanged;

    private const int EXCHANGE_RATE_MIN = 2;
    private const int EXCHANGE_RATE_MAX = 10;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 초기값 (Awake 시점에 자동 리셋)
        _jelly = INITIAL_JELLY;
        _stone = 0;
        _currentExchangeRate = EXCHANGE_RATE_MIN;
        _exchangedThisRound = false;
    }

    /// <summary>
    /// 새 게임 시작 시 호출 — 젤리 100 / 스톤 0 리셋.
    /// GameManager.StartNewGame() 또는 GameOver 후 재시작 시 사용.
    /// </summary>
    public void ResetForNewGame()
    {
        _jelly = INITIAL_JELLY;
        _stone = 0;
        _exchangedThisRound = false;
        Debug.Log($"[Wallet] ResetForNewGame → Jelly={_jelly} Stone={_stone}");
        OnChanged?.Invoke();
        OnExchangeStateChanged?.Invoke();
    }

    // ══════════════════════════════════════════════════════════════
    //  환전 시스템 (SPEC-028 Step 2.14 — R16~R20)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 라운드 시작 시 호출 — 환전 비율 2~10 자연수 랜덤 + 라운드당 1회 카운터 리셋.
    /// GameManager.StartNewGame()·NextRound()에서 호출.
    /// </summary>
    public void RollExchangeRate()
    {
        // Random.Range(int min, int max)는 max exclusive → +1
        _currentExchangeRate = UnityEngine.Random.Range(EXCHANGE_RATE_MIN, EXCHANGE_RATE_MAX + 1);
        _exchangedThisRound = false;
        Debug.Log($"[Wallet] RollExchangeRate → 비율 {_currentExchangeRate}:1, 라운드당 1회 사용 가능");
        OnExchangeStateChanged?.Invoke();
    }

    /// <summary>
    /// 환전 가능 여부 검증.
    /// R17 일반: 스톤 ≥ 비율 → 가능
    /// R19 구제: 젤리 0 AND 스톤 ≥ 1 (비율 미만이어도 가진 스톤 전부 → 젤리 1) → 가능
    /// R18: 라운드당 1회 제한
    /// </summary>
    public bool CanExchange()
    {
        if (_exchangedThisRound) return false;            // R18
        if (_stone <= 0) return false;
        if (_stone >= _currentExchangeRate) return true;  // 일반
        if (_jelly == 0 && _stone >= 1) return true;      // R19 구제 (확장)
        return false;
    }

    /// <summary>
    /// 환전 시 차감될 스톤 양 계산.
    /// 일반: 비율만큼 차감 / 구제: 가진 스톤 전부 차감
    /// </summary>
    public int GetExchangeStoneCost()
    {
        if (!CanExchange()) return 0;
        if (_jelly == 0 && _stone < _currentExchangeRate)
            return _stone;          // 구제: 전부
        return _currentExchangeRate;  // 일반: 비율만큼
    }

    /// <summary>
    /// 환전 실행 — 스톤 차감 후 젤리 +1.
    /// CanExchange() == false면 false 반환 + 변경 없음.
    /// </summary>
    public bool TryExchange()
    {
        if (!CanExchange())
        {
            Debug.LogWarning($"[Wallet] TryExchange 실패: CanExchange=false (stone={_stone} rate={_currentExchangeRate} exchanged={_exchangedThisRound})");
            return false;
        }

        int stoneCost = GetExchangeStoneCost();
        bool isRescue = (_jelly == 0 && _stone < _currentExchangeRate);

        _stone -= stoneCost;
        _jelly += 1;
        _exchangedThisRound = true;

        Debug.Log($"[Wallet] Exchange {(isRescue ? "[구제]" : "[일반]")} -{stoneCost}💎 → +1🟦 (Jelly={_jelly} Stone={_stone})");
        OnChanged?.Invoke();
        OnExchangeStateChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Game Over 조건 체크 (CalcScore 직후 GameManager가 호출).
    /// 젤리 > 0 → 계속 진행 / 젤리 0 + 환전 불가 → Game Over.
    /// </summary>
    public bool ShouldGameOver()
    {
        if (_jelly > 0) return false;
        return !CanExchange();
    }

    /// <summary>
    /// 마지막 라운드 완주 + 랭킹 제출 성공 시 호출.
    /// 스톤만 0으로 리셋 (다음 게임은 ResetForNewGame이 별도로 호출됨).
    /// SPEC-028 Phase 4에서 활용.
    /// </summary>
    public void ResetStoneOnly()
    {
        _stone = 0;
        Debug.Log($"[Wallet] ResetStoneOnly → Stone={_stone}");
        OnChanged?.Invoke();
    }

    /// <summary>
    /// 베팅 시 젤리 차감.
    /// 잔액 부족 시 false 반환 (변경 없음).
    /// </summary>
    public bool TryBet(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[Wallet] TryBet 실패: amount={amount} (1 이상이어야 함)");
            return false;
        }
        if (_jelly < amount)
        {
            Debug.LogWarning($"[Wallet] TryBet 실패: jelly={_jelly} < amount={amount}");
            return false;
        }

        _jelly -= amount;
        Debug.Log($"[Wallet] Bet {amount} → Jelly={_jelly}");
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 적중 보상 — 젤리·스톤 동시 가산.
    /// 0 또는 음수 입력은 무시.
    /// </summary>
    public void Reward(int jellyGain, int stoneGain)
    {
        if (jellyGain < 0 || stoneGain < 0)
        {
            Debug.LogWarning($"[Wallet] Reward 음수 무시: jelly={jellyGain} stone={stoneGain}");
            return;
        }
        if (jellyGain == 0 && stoneGain == 0) return;

        _jelly += jellyGain;
        _stone += stoneGain;
        Debug.Log($"[Wallet] Reward +{jellyGain}J +{stoneGain}S → Jelly={_jelly} Stone={_stone}");
        OnChanged?.Invoke();
    }

    /// <summary>
    /// 디버그/테스트용 — 외부에서 직접 값 설정 (운영 코드에서 사용 금지)
    /// </summary>
#if UNITY_EDITOR
    public void DebugSet(int jelly, int stone)
    {
        _jelly = Mathf.Max(0, jelly);
        _stone = Mathf.Max(0, stone);
        Debug.Log($"[Wallet] DebugSet → Jelly={_jelly} Stone={_stone}");
        OnChanged?.Invoke();
    }

    /// <summary>디버그용 — 환전 비율 강제 설정 (테스트 시나리오용)</summary>
    public void DebugSetExchangeRate(int rate, bool exchangedThisRound = false)
    {
        _currentExchangeRate = Mathf.Clamp(rate, EXCHANGE_RATE_MIN, EXCHANGE_RATE_MAX);
        _exchangedThisRound = exchangedThisRound;
        Debug.Log($"[Wallet] DebugSetExchangeRate → 비율={_currentExchangeRate} exchanged={_exchangedThisRound}");
        OnExchangeStateChanged?.Invoke();
    }
#endif
}
