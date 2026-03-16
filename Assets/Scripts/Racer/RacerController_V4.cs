using UnityEngine;

/// <summary>
/// RacerController V4 — 5대 스탯(Speed/Accel/Stamina/Power/Intelligence)+Luck 기반 달리기 로직
///
/// ▶ 레이스 페이즈 (GameSettingsV4 기준)
///   Phase 1 [Positioning]  0 ~ positioningEndRatio
///     - 각 타입이 목표 순위 범위로 이동. 도주는 무조건 선두 추구.
///     - 지능 낮은 캐릭터는 오버페이스(스태미나 과소비) 가능성 있음.
///
///   Phase 2 [Cruising]     positioningEndRatio ~ 마지막 랩 spurtTriggerLapRemain 전
///     - 가상 페이스메이커 기준 목표 순위 유지.
///     - 지능 스탯으로 슬립스트림 탐색 및 오버페이스 방지.
///     - 추입: 의도적으로 Vmax의 70~80% 속도 유지 → 스태미나 비축.
///
///   Phase 3 [Spurt]        마지막 랩 spurtTriggerLapRemain 이후 (도주는 레이스 시작부터)
///     - 스퍼트 트리거: 남은 거리 / 예상Vmax <= 남은 스태미나 / 예상소모율
///     - Vmax × spurtVmaxBonus 해제, Accel × spurtAccelBonus 적용.
///     - 지능 높을수록 정확한 타이밍에 스퍼트 발동.
///
/// ▶ 마군 처리 (Phase 3, 선입/추입)
///   - 지능 P_smart 확률로 사전 회피 (Y축 차선 변경)
///   - 실패 시 파워 스탯으로 충돌 페널티 감소
///
/// ▶ 현재 상태: 스켈레톤 (구조만 정의, 로직 미구현)
///   → V4 달리기 로직 구현 시 이 파일에 작성
/// </summary>
public partial class RacerController : MonoBehaviour  // partial — RacerController.cs와 공유
{
    // ──────────────────────────────────────────────
    //  V4 런타임 상태
    // ──────────────────────────────────────────────

    // V4 캐릭터 데이터 (CharacterDB_V4.csv 기반)
    private CharacterDataV4 charDataV4;

    // 현재 스태미나 (HP)
    private float v4CurrentStamina;
    private float v4MaxStamina;

    // 현재 속도
    private float v4CurrentSpeed;

    // 페이즈
    private V4Phase v4Phase = V4Phase.Positioning;

    // 스퍼트 활성 여부
    private bool v4IsSpurting = false;

    // 오버페이스 상태
    private bool v4IsPanicking = false;
    private float v4PanicTimer = 0f;

    // 슬립스트림 수혜 여부 (앞 캐릭터 뒤에 있는지)
    private bool v4InSlipstream = false;

    // 지능 판단 틱 타이머
    private float v4ThinkTimer = 0f;

    // ──────────────────────────────────────────────
    //  V4 페이즈 열거형
    // ──────────────────────────────────────────────
    private enum V4Phase
    {
        Positioning,    // 자리잡기
        Cruising,       // 페이스 유지
        Spurt           // 전력질주
    }

    // ──────────────────────────────────────────────
    //  V4 초기화
    // ──────────────────────────────────────────────

    /// <summary>V4 시스템 초기화 (RaceManager에서 V4 활성 시 호출)</summary>
    private void InitV4()
    {
        // TODO: CharacterDatabaseV4.FindById(charData.charId) 로 V4 데이터 로드
        // TODO: 스태미나 초기화
        // TODO: 도주 타입이면 즉시 Spurt 페이즈 진입
        Debug.Log("[RacerController V4] InitV4() — 구현 예정");
    }

    // ──────────────────────────────────────────────
    //  V4 업데이트 (매 프레임)
    // ──────────────────────────────────────────────

    /// <summary>V4 달리기 로직 메인 루프</summary>
    private void UpdateV4(float dt)
    {
        // TODO: 페이즈 전환 체크
        // TODO: 판단 틱 처리 (슬립스트림 탐색, 회피 판정)
        // TODO: 속도 계산 (CalcSpeedV4)
        // TODO: 스태미나 소모 (ConsumeStaminaV4)
        // TODO: 오버페이스 업데이트
    }

    /// <summary>V4 속도 계산</summary>
    private float CalcSpeedV4(GameSettingsV4 gs)
    {
        // TODO:
        // 1. Vmax 결정 (스퍼트 여부 + Speed 스탯)
        // 2. 타겟 속도 결정 (페이즈별 + 추입 크루징 70~80% 제한)
        // 3. 현재속도 → 타겟속도 Lerp (Accel 스탯 기반)
        // 4. 스태미나 고갈 시 Exhaust 패널티 적용
        return v4CurrentSpeed;
    }

    /// <summary>V4 스태미나 소모</summary>
    private void ConsumeStaminaV4(float dt, GameSettingsV4 gs)
    {
        // TODO:
        // drain = v4CurrentSpeed × gs.v4_drainBaseRate
        // if (v4InSlipstream) drain × gs.v4_slipstreamDrainMul
        // if (v4IsPanicking) drain × gs.v4_panicDrainMul
        // v4CurrentStamina -= drain × dt
    }

    /// <summary>V4 지능 판단 틱 처리</summary>
    private void ProcessV4ThinkTick(GameSettingsV4 gs)
    {
        // TODO:
        // 1. 슬립스트림 탐색 (지능 가중치로 앞 캐릭터 Y축 추종)
        // 2. 사전 회피 판정 (P_smart = min(MaxLimit, Int/(Int+K)))
        // 3. 오버페이스 발동 판정 (P_panic = P_base × (1 - Int/IntMax))
    }

    /// <summary>V4 스퍼트 트리거 체크</summary>
    private bool CheckV4SpurtTrigger(GameSettingsV4 gs)
    {
        // TODO:
        // 도주: 항상 true (처음부터 스퍼트)
        // 나머지: 마지막 랩의 spurtTriggerLapRemain 이하 진행도 도달 시
        //        + 스태미나 계산으로 결승까지 Vmax 유지 가능 여부 확인
        return false;
    }
}
