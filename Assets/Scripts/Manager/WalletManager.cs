using UnityEngine;
using System;

/// <summary>
/// 도파민 젤리·스톤 보유 관리 (게임 단위 휘발성).
/// - 매 게임 시작 시 ResetForNewGame() → 젤리 100, 스톤 0
/// - 베팅 시 TryBet(amount) — 잔액 부족 시 false 반환
/// - 적중 시 Reward(stone) — 스톤만 가산
/// - 마지막 라운드 완주 후 ResetStoneOnly() — 랭킹 제출 후 호출
///
/// SPEC-028 Step 1.3 · SPEC-051 럭키 잭팟 재설계
/// </summary>
public class WalletManager : MonoBehaviour
{
    public static WalletManager Instance { get; private set; }

    private const int INITIAL_JELLY = 100;

    // 젤리·스톤: 메모리 난독화(SecureInt) — Cheat Engine 평문 스캔 방어 (SPEC-044 Phase D).
    // 직렬화 안 됨(런타임 휘발). WalletManager는 런타임 AddComponent라 씬 직렬값 없음.
    private SecureInt _jelly;
    private SecureInt _stone;

    [Header("디버그 표시 (런타임 전용)")]
#if UNITY_EDITOR
    [SerializeField] private int _jellyDisplay;   // _jelly 미러 (권위값 아님 — 표시 전용)
    [SerializeField] private int _stoneDisplay;   // _stone 미러 (권위값 아님 — 표시 전용)
#endif
    // SPEC-051: 럭키 잭팟 게임당 사용 횟수 카운터.
    // (구 _catPowerUsesThisGame를 잭팟 공유 카운터로 재활용 — 자발 고양이클릭 + 0젤리 자동이 1회를 공유)
    [SerializeField] private int _jackpotUsesThisGame;
    [SerializeField] private int _lastJackpotGain;   // SPEC-051: 마지막 잭팟 획득 젤리 (사용 후 결과 지속 표시용)

    /// <summary>현재 보유 젤리</summary>
    public int Jelly => _jelly;

    /// <summary>현재 보유 스톤 (게임 종료 시 리셋)</summary>
    public int Stone => _stone;

    // ══════════════════════════════════════════════════════════════
    //  럭키 잭팟 (SPEC-051) — 스톤 전부 × N 젤리, 게임당 1회
    // ══════════════════════════════════════════════════════════════

    /// <summary>이번 게임 잭팟 사용 횟수.</summary>
    public int JackpotUsesThisGame => _jackpotUsesThisGame;

    /// <summary>마지막 잭팟으로 획득한 젤리 (게임당 1회라 사용 후 계속 표시).</summary>
    public int LastJackpotJellyGain => _lastJackpotGain;

    /// <summary>게임당 잭팟 최대 횟수 (GameSettings.jackpotUsesPerGame, 기본 1, 0=비활성).</summary>
    public int MaxJackpotUses => (GameSettings.Instance != null)
        ? Mathf.Max(0, GameSettings.Instance.jackpotUsesPerGame) : 1;

    /// <summary>남은 잭팟 사용 횟수.</summary>
    public int JackpotUsesLeft => Mathf.Max(0, MaxJackpotUses - _jackpotUsesThisGame);

    /// <summary>잭팟 가능: 남은 횟수 있음 && 스톤 ≥ 1(최소 입력).</summary>
    public bool CanJackpot() => JackpotUsesLeft > 0 && _stone >= 1;

    /// <summary>잭팟 1회 결과.</summary>
    public struct JackpotResult { public bool success; public int n; public int jellyGain; public int stoneSpent; }

    // SPEC-047: ACH_RICH 해금 임계값 — 누적 보유 스톤 기준.
    // 신경제(젤리 소멸)에서 스톤이 더 빨리 쌓이므로 기존 젤리 1000 → 스톤 3000으로 재조정.
    private const int RICH_STONE_THRESHOLD = 3000;

    /// <summary>젤리 또는 스톤 변경 시 발생 (UI 구독용)</summary>
    public event Action OnChanged;

    /// <summary>잭팟 가능 여부/횟수 변경 시 발생 (Exchange UI 구독용)</summary>
    public event Action OnExchangeStateChanged;

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
        _jackpotUsesThisGame = 0;
        _lastJackpotGain = 0;
#if UNITY_EDITOR
        SyncDisplay();
#endif
    }

    /// <summary>OnChanged 발생 + (에디터) 인스펙터 미러 동기화 단일화. 빌드에선 OnChanged만 남음.</summary>
    private void RaiseChanged()
    {
#if UNITY_EDITOR
        SyncDisplay();
#endif
        OnChanged?.Invoke();
    }

#if UNITY_EDITOR
    private void SyncDisplay() { _jellyDisplay = _jelly; _stoneDisplay = _stone; }
#endif

    /// <summary>
    /// 새 게임 시작 시 호출 — 젤리 100 / 스톤은 GameSettings.startingStone(기본 0)로 리셋.
    /// GameManager.StartNewGame() 또는 GameOver 후 재시작 시 사용.
    /// </summary>
    public void ResetForNewGame()
    {
        _jelly = INITIAL_JELLY;
        _stone = StartingStone();
        _jackpotUsesThisGame = 0;          // 게임 단위 — 잭팟 횟수 리셋
        _lastJackpotGain = 0;
        Debug.Log($"[Wallet] ResetForNewGame → Jelly={_jelly} Stone={_stone}");
        RaiseChanged();
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
    //  럭키 잭팟 산식 (SPEC-051)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 잭팟 배수 N 가중 랜덤 (GameSettings.jackpotMultiplierMin..Max).
    /// jackpotWeights 길이가 (Max-Min+1)과 다르거나 합&lt;=0이면 균등 폴백.
    /// </summary>
    public int RollJackpotN()
    {
        var gs = GameSettings.Instance;
        int min = (gs != null) ? Mathf.Max(1, gs.jackpotMultiplierMin) : 2;
        int max = (gs != null) ? Mathf.Max(min, gs.jackpotMultiplierMax) : 8;
        int span = max - min + 1;

        int[] w = (gs != null) ? gs.jackpotWeights : null;
        if (w == null || w.Length != span)
        {
            if (w != null && w.Length != span)
                Debug.LogWarning($"[Wallet] jackpotWeights 길이 불일치 ({w.Length} != {span}) → 균등 폴백");
            return UnityEngine.Random.Range(min, max + 1);   // max exclusive → +1
        }

        int sum = 0;
        for (int i = 0; i < w.Length; i++) sum += Mathf.Max(0, w[i]);
        if (sum <= 0)
        {
            Debug.LogWarning("[Wallet] jackpotWeights 합<=0 → 균등 폴백");
            return UnityEngine.Random.Range(min, max + 1);
        }

        int roll = UnityEngine.Random.Range(0, sum);   // [0, sum)
        int acc = 0;
        for (int i = 0; i < w.Length; i++)
        {
            acc += Mathf.Max(0, w[i]);
            if (roll < acc) return min + i;
        }
        return max;   // 부동소수 없는 정수합이라 도달 불가지만 안전 폴백
    }

    /// <summary>
    /// 럭키 잭팟 — 보유 스톤 전부를 (스톤 × N) 젤리로. 게임당 1회.
    /// autoFloor(0젤리 자동 진입): 산출 젤리에 jackpotRescueFloorJelly 바닥 보장 — max(stone×N, floor).
    /// !CanJackpot()이면 카운터 미차감(막판 안전망 보존, 무효 소비 방지).
    /// </summary>
    public JackpotResult TryJackpot(bool autoFloor)
    {
        if (!CanJackpot())
        {
            Debug.LogWarning($"[Wallet] 잭팟 실패 (stone={_stone} usesLeft={JackpotUsesLeft})");
            return new JackpotResult { success = false };   // E14: 카운터 미차감
        }

        int stoneSpent = _stone;            // 스톤 전부 (SecureInt → 평문 int)
        int n = RollJackpotN();
        int jellyGain = stoneSpent * n;     // 무캡 — 평문 int 연산 (실측 max~1만 스톤, 오버플로 불가)

        if (autoFloor)
        {
            int floor = (GameSettings.Instance != null)
                ? Mathf.Max(1, GameSettings.Instance.jackpotRescueFloorJelly) : 5;
            jellyGain = Mathf.Max(jellyGain, floor);   // max 방향 (큰 산출을 깎지 않음)
        }

        _stone = 0;                         // E16: 전부 변환 → 잔여 0
        _jelly += jellyGain;
        _jackpotUsesThisGame++;             // autoFloor라도 1회만 차감 (원자성)
        _lastJackpotGain = jellyGain;       // SPEC-051: 결과 지속 표시용

        Debug.Log($"[Wallet] 럭키 잭팟 {(autoFloor ? "[자동]" : "[자발]")} -{stoneSpent}💎 ×{n} → +{jellyGain}🟦 (Jelly={_jelly} Stone={_stone}, {_jackpotUsesThisGame}/{MaxJackpotUses})");

        // Steam 도전과제 재배선 (상수 유지): 자발=CatPower, 0젤리 자동(구제 성격)=Rescue
        SteamAchievements.Unlock(autoFloor ? SteamAchievements.Rescue : SteamAchievements.CatPower);

        RaiseChanged();
        OnExchangeStateChanged?.Invoke();
        return new JackpotResult { success = true, n = n, jellyGain = jellyGain, stoneSpent = stoneSpent };
    }

    /// <summary>
    /// Game Over — 0젤리이고 잭팟으로도 못 살아날 때만 (SSOT).
    /// 스톤 ≥1 && 잭팟 횟수 남으면 생존(잭팟 모달 제시).
    /// (GameManager.NextRound 진입 시 호출 — 플래그 비의존이라 리셋 순서 무관)
    /// </summary>
    public bool ShouldGameOver()
    {
        return _jelly == 0 && !CanJackpot();
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
        RaiseChanged();
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
        RaiseChanged();
        return true;
    }

    /// <summary>
    /// 적중 보상 — 스톤만 가산 (SPEC-047: 젤리는 배팅 시 소멸, 적중해도 반환 안 함).
    /// 0 또는 음수 입력은 무시.
    /// </summary>
    public void Reward(int stoneGain)
    {
        if (stoneGain < 0)
        {
            Debug.LogWarning($"[Wallet] Reward 음수 무시: stone={stoneGain}");
            return;
        }
        if (stoneGain == 0) return;

        _stone += stoneGain;
        Debug.Log($"[Wallet] Reward +{stoneGain}S → Jelly={_jelly} Stone={_stone}");
        if (_stone >= RICH_STONE_THRESHOLD) SteamAchievements.Unlock(SteamAchievements.Rich);   // [Steam] 누적 스톤 도달 (SPEC-047)
        RaiseChanged();
        OnExchangeStateChanged?.Invoke();   // 스톤 변동 → 잭팟 가능 여부 갱신
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
        RaiseChanged();
        OnExchangeStateChanged?.Invoke();
    }

    /// <summary>디버그용 — 잭팟 사용 횟수 강제 설정 (테스트 시나리오용).</summary>
    public void DebugSetJackpot(int usesThisGame)
    {
        _jackpotUsesThisGame = Mathf.Max(0, usesThisGame);
        Debug.Log($"[Wallet] DebugSetJackpot → jackpotUses={_jackpotUsesThisGame}");
        OnExchangeStateChanged?.Invoke();
    }
#endif
}
