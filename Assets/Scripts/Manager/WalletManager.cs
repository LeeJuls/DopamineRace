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

    /// <summary>현재 보유 젤리</summary>
    public int Jelly => _jelly;

    /// <summary>현재 보유 스톤 (게임 종료 시 리셋)</summary>
    public int Stone => _stone;

    /// <summary>젤리 또는 스톤 변경 시 발생 (UI 구독용)</summary>
    public event Action OnChanged;

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
    }

    /// <summary>
    /// 새 게임 시작 시 호출 — 젤리 100 / 스톤 0 리셋.
    /// GameManager.StartNewGame() 또는 GameOver 후 재시작 시 사용.
    /// </summary>
    public void ResetForNewGame()
    {
        _jelly = INITIAL_JELLY;
        _stone = 0;
        Debug.Log($"[Wallet] ResetForNewGame → Jelly={_jelly} Stone={_stone}");
        OnChanged?.Invoke();
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
#endif
}
