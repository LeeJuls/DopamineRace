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
    [SerializeField] private bool _rescuedThisRound;       // 구제: 라운드당 1회
    [SerializeField] private int  _catPowerUsesThisGame;   // 고양이의 힘: 게임당 N회 카운터

    /// <summary>현재 보유 젤리</summary>
    public int Jelly => _jelly;

    /// <summary>현재 보유 스톤 (게임 종료 시 리셋)</summary>
    public int Stone => _stone;

    /// <summary>
    /// 이번 라운드 스톤 → 젤리 환전 비율 (스톤 N → 젤리 1).
    /// SPEC-028 Step 2.14 / R17. 매 라운드 시작 시 RollExchangeRate()로 갱신.
    /// </summary>
    public int CurrentExchangeRate => _currentExchangeRate;

    /// <summary>이번 라운드 구제 사용 여부 (라운드당 1회).</summary>
    public bool RescuedThisRound => _rescuedThisRound;

    /// <summary>이번 게임 고양이의 힘 사용 횟수.</summary>
    public int CatPowerUsesThisGame => _catPowerUsesThisGame;

    /// <summary>게임당 고양이의 힘 최대 횟수 (GameSettings.catPowerUsesPerGame, 기본 1, 0=비활성).</summary>
    public int MaxCatPowerUses => (GameSettings.Instance != null)
        ? Mathf.Max(0, GameSettings.Instance.catPowerUsesPerGame) : 1;

    /// <summary>남은 고양이의 힘 사용 횟수.</summary>
    public int CatPowerUsesLeft => Mathf.Max(0, MaxCatPowerUses - _catPowerUsesThisGame);

    /// <summary>모달 표시용 환전 액션.</summary>
    public enum ExchangeAction { None, CatPower, Rescue }

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
        _stone = StartingStone();
        _currentExchangeRate = EXCHANGE_RATE_MIN;
        _rescuedThisRound = false;
        _catPowerUsesThisGame = 0;
    }

    /// <summary>
    /// 새 게임 시작 시 호출 — 젤리 100 / 스톤은 GameSettings.startingStone(기본 0)로 리셋.
    /// GameManager.StartNewGame() 또는 GameOver 후 재시작 시 사용.
    /// </summary>
    public void ResetForNewGame()
    {
        _jelly = INITIAL_JELLY;
        _stone = StartingStone();
        _rescuedThisRound = false;
        _catPowerUsesThisGame = 0;          // 게임 단위 — 고양이의 힘 횟수 리셋
        Debug.Log($"[Wallet] ResetForNewGame → Jelly={_jelly} Stone={_stone}");
        OnChanged?.Invoke();
        OnExchangeStateChanged?.Invoke();
    }

    /// <summary>
    /// 게임 시작 시 부여할 초기 스톤 (GameSettings.startingStone, 기본 0 = 없음).
    /// 테스트 편의용 — Inspector에서 0보다 크게 설정하면 매 게임 시작부터 보유.
    /// </summary>
    private static int StartingStone()
    {
        var gs = GameSettings.Instance;
        return (gs != null) ? Mathf.Max(0, gs.startingStone) : 0;
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
        _rescuedThisRound = false;            // 라운드 단위만 리셋 (게임 카운터 보존)
        Debug.Log($"[Wallet] RollExchangeRate → 비율 {_currentExchangeRate}:1");
        OnExchangeStateChanged?.Invoke();
    }

    // ───────── 능력 1: 고양이의 힘 (게임당 N회, 전부환전) ─────────

    /// <summary>고양이의 힘 사용 가능: 남은 횟수 있고 스톤 ≥ 비율.</summary>
    public bool CanUseCatPower()
    {
        if (_catPowerUsesThisGame >= MaxCatPowerUses) return false;
        if (_currentExchangeRate <= 0) return false;
        return _stone >= _currentExchangeRate;
    }

    /// <summary>전부환전 시 획득 젤리 = floor(스톤 / 비율).</summary>
    public int GetCatPowerJellyGain()
    {
        if (_currentExchangeRate <= 0) return 0;
        return _stone / _currentExchangeRate;
    }

    /// <summary>전부환전 시 차감 스톤 = floor(스톤/비율) × 비율 (나머지 보존).</summary>
    public int GetCatPowerStoneCost() => GetCatPowerJellyGain() * _currentExchangeRate;

    /// <summary>
    /// 고양이의 힘 — 가진 스톤 전부를 최대 젤리로 변환. 게임당 N회.
    /// 변환량(≥1)·사용가능 둘 다 충족해야 카운터 소모 (무효 소비 방지).
    /// </summary>
    public bool TryUseCatPower()
    {
        if (!CanUseCatPower())
        {
            Debug.LogWarning($"[Wallet] 고양이의 힘 실패 (stone={_stone} rate={_currentExchangeRate} uses={_catPowerUsesThisGame}/{MaxCatPowerUses})");
            return false;
        }
        int jellyGain = GetCatPowerJellyGain();
        if (jellyGain < 1) return false;            // 변환량 0 → 카운터 미소모 방어
        int stoneCost = jellyGain * _currentExchangeRate;

        _stone -= stoneCost;
        _jelly += jellyGain;
        _catPowerUsesThisGame++;

        Debug.Log($"[Wallet] 고양이의 힘 [전부] -{stoneCost}💎 → +{jellyGain}🟦 (Jelly={_jelly} Stone={_stone}, {_catPowerUsesThisGame}/{MaxCatPowerUses})");
        OnChanged?.Invoke();
        OnExchangeStateChanged?.Invoke();
        return true;
    }

    // ───────── 능력 2: 구제 (라운드당 1회, 파산 안전망) ─────────

    /// <summary>구제 가능: 이번 라운드 미사용 + 파산(젤리0) + 스톤 ≥1.</summary>
    public bool CanRescue()
    {
        if (_rescuedThisRound) return false;
        return _jelly == 0 && _stone >= 1;
    }

    /// <summary>
    /// 구제 — 파산 직전 스톤을 젤리 1개로. 라운드당 1회 안전망.
    /// 스톤은 min(보유, 비율)만큼만 차감.
    /// </summary>
    public bool TryRescue()
    {
        if (!CanRescue())
        {
            Debug.LogWarning($"[Wallet] 구제 실패 (jelly={_jelly} stone={_stone} rescued={_rescuedThisRound})");
            return false;
        }
        int stoneCost = Mathf.Min(_stone, _currentExchangeRate);
        _stone -= stoneCost;
        _jelly += 1;
        _rescuedThisRound = true;

        Debug.Log($"[Wallet] 구제 -{stoneCost}💎 → +1🟦 (Jelly={_jelly} Stone={_stone})");
        OnChanged?.Invoke();
        OnExchangeStateChanged?.Invoke();
        return true;
    }

    /// <summary>모달이 표시할 환전 액션 (우선순위: 고양이의 힘 &gt; 구제 &gt; 없음).</summary>
    public ExchangeAction GetAvailableAction()
    {
        if (CanUseCatPower()) return ExchangeAction.CatPower;
        if (CanRescue())      return ExchangeAction.Rescue;
        return ExchangeAction.None;
    }

    /// <summary>
    /// Game Over — 젤리 0 AND 스톤 0일 때만.
    /// 구제가 매 라운드 리셋되므로 스톤이 1개라도 있으면 다음 라운드 구제로 생존 가능.
    /// (GameManager.NextRound 진입 시 호출 — 플래그 비의존이라 리셋 순서 무관)
    /// </summary>
    public bool ShouldGameOver()
    {
        return _jelly == 0 && _stone == 0;
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

    /// <summary>디버그용 — 환전 비율/플래그 강제 설정 (테스트 시나리오용)</summary>
    public void DebugSetExchangeRate(int rate, bool rescuedThisRound = false, int catPowerUses = 0)
    {
        _currentExchangeRate = Mathf.Clamp(rate, EXCHANGE_RATE_MIN, EXCHANGE_RATE_MAX);
        _rescuedThisRound = rescuedThisRound;
        _catPowerUsesThisGame = Mathf.Max(0, catPowerUses);
        Debug.Log($"[Wallet] DebugSetExchangeRate → 비율={_currentExchangeRate} rescued={_rescuedThisRound} catUses={_catPowerUsesThisGame}");
        OnExchangeStateChanged?.Invoke();
    }
#endif
}
