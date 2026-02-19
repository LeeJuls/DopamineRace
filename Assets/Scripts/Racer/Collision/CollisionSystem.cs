using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 레이스 충돌 시스템 (A-3)
/// 물리 엔진 없이 거리 기반 판정 → 속도 페널티/보너스
///
/// RaceManager 오브젝트에 자동 부착됨
/// GameSettings.enableCollision으로 ON/OFF
///
/// 보호 체계:
///   1) CollisionPenalty > 0 → 감속 중 충돌 불가
///   2) racerCooldowns → 개별 레이서 쿨다운 (어떤 상대든)
///   3) pairCooldowns → 같은 쌍 쿨다운
///   4) return → 프레임당 최대 1건
///   5) collisionChance → 확률적 불발
/// </summary>
public class CollisionSystem : MonoBehaviour
{
    // ── 쿨다운 관리 ──
    private Dictionary<int, float> pairCooldowns = new Dictionary<int, float>();
    private Dictionary<int, float> racerCooldowns = new Dictionary<int, float>();

    // ── 글로벌 충돌 쿨다운 (어떤 쌍이든 최소 간격) ──
    private float globalCooldown = 0f;
    private const float GLOBAL_COLLISION_INTERVAL = 0.5f;

    // ── 슬링샷 예약 (감속 종료 후 발동) ──
    private List<SlingshotReservation> slingshotQueue = new List<SlingshotReservation>();

    private struct SlingshotReservation
    {
        public RacerController racer;
        public float triggerTime;
        public float boost;
        public float duration;
        public string reason;
    }

    private void Update()
    {
        var gs = GameSettings.Instance;
        if (!gs.enableCollision) return;

        var rm = RaceManager.Instance;
        if (rm == null || !rm.RaceActive) return;

        var racers = rm.Racers;
        if (racers == null || racers.Count < 2) return;

        UpdateCooldowns();
        CheckCollisions(racers, gs);
        ProcessSlingshotQueue(gs);
    }

    // ══════════════════════════════════════
    //  충돌 판정 (프레임당 최대 1건)
    // ══════════════════════════════════════

    private void CheckCollisions(List<RacerController> racers, GameSettings gs)
    {
        if (globalCooldown > 0f) return;

        TrackData track = gs.currentTrack;
        float range = gs.collisionRange;
        if (track != null) range *= track.collisionRangeMultiplier;

        for (int i = 0; i < racers.Count; i++)
        {
            var ri = racers[i];
            if (ri.IsFinished) continue;
            if (ri.CollisionPenalty > 0) continue;
            if (HasRacerCooldown(ri.RacerIndex)) continue;

            for (int j = i + 1; j < racers.Count; j++)
            {
                var rj = racers[j];
                if (rj.IsFinished) continue;
                if (rj.CollisionPenalty > 0) continue;
                if (HasRacerCooldown(rj.RacerIndex)) continue;

                float dist = Vector3.Distance(ri.transform.position, rj.transform.position);
                if (dist >= range) continue;

                int pairKey = GetPairKey(ri.RacerIndex, rj.RacerIndex);
                if (pairCooldowns.ContainsKey(pairKey) && pairCooldowns[pairKey] > 0f) continue;

                if (gs.crowdThreshold > 0)
                {
                    int nearby = CountNearby(racers, ri.transform.position, range);
                    if (nearby >= gs.crowdThreshold)
                    {
                        if (Random.value > gs.crowdDampen) continue;
                    }
                }

                if (Random.value > gs.collisionChance) continue;

                ResolveCollision(ri, rj, gs, track);

                pairCooldowns[pairKey] = gs.collisionCooldown;
                racerCooldowns[ri.RacerIndex] = gs.collisionCooldown;
                racerCooldowns[rj.RacerIndex] = gs.collisionCooldown;
                globalCooldown = GLOBAL_COLLISION_INTERVAL;

                return;
            }
        }
    }

    private bool HasRacerCooldown(int racerIndex)
    {
        if (racerCooldowns.ContainsKey(racerIndex) && racerCooldowns[racerIndex] > 0f)
            return true;
        return false;
    }

    // ══════════════════════════════════════
    //  충돌 해결
    // ══════════════════════════════════════

    private void ResolveCollision(RacerController a, RacerController b,
        GameSettings gs, TrackData track)
    {
        var debugOverlay = GetComponent<RaceDebugOverlay>();
        var cdA = a.CharData;
        var cdB = b.CharData;
        if (cdA == null || cdB == null) return;

        // ── ★ 스킬 충돌 카운트 (양쪽 모두) ──
        a.OnSkillCollisionHit();
        b.OnSkillCollisionHit();

        // ── 승패 결정 ──
        RacerController winner, loser;

        bool aArmed = a.SkillActive;
        bool bArmed = b.SkillActive;

        if (aArmed && !bArmed)
        {
            // A만 무장 → A 무조건 승리
            winner = a; loser = b;
            if (debugOverlay != null)
                debugOverlay.LogEvent(RaceDebugOverlay.EventType.CollisionHit,
                    string.Format("★ {0} 스킬 발동 중 → 무조건 승리!", a.CharData.charName));
        }
        else if (bArmed && !aArmed)
        {
            // B만 무장 → B 무조건 승리
            winner = b; loser = a;
            if (debugOverlay != null)
                debugOverlay.LogEvent(RaceDebugOverlay.EventType.CollisionHit,
                    string.Format("★ {0} 스킬 발동 중 → 무조건 승리!", b.CharData.charName));
        }
        else
        {
            // 둘 다 무장 또는 둘 다 맨몸 → 기존 power 확률 로직
            float powerA = cdA.charBasePower;
            float powerB = cdB.charBasePower;
            float effA = powerA;
            float effB = powerB;

            if (powerA > powerB)
            {
                float benefit = powerA / (powerA + powerB);
                effA = powerA * (1f + benefit);
            }
            else if (powerB > powerA)
            {
                float benefit = powerB / (powerA + powerB);
                effB = powerB * (1f + benefit);
            }

            float totalEff = effA + effB;
            float bWinChance = totalEff > 0f ? effB / totalEff : 0.5f;

            if (Random.value < bWinChance)
            { winner = b; loser = a; }
            else
            { winner = a; loser = b; }
        }

        // ── luck 회피 판정 (패자 측) ──
        if (loser.TryDodge())
        {
            if (debugOverlay != null)
            {
                debugOverlay.LogEvent(RaceDebugOverlay.EventType.CollisionDodge,
                    string.Format("{0} → {1} 회피! (luck:{2})",
                        winner.CharData.charName, loser.CharData.charName,
                        loser.CharData.charBaseLuck));
            }
            if (gs.enableCollisionVFX)
                ShowVFX(loser, CollisionVFXType.Dodge, 0.8f);
            return;
        }

        // ── 페널티 적용 ──
        float trackPenaltyMul = track != null ? track.collisionPenaltyMultiplier : 1f;
        float trackLoserDurMul = track != null ? track.loserPenaltyDurationMultiplier : 1f;

        float winnerPenalty = gs.collisionBasePenalty * 0.5f * trackPenaltyMul;
        winner.ApplyCollisionPenalty(winnerPenalty, gs.winnerPenaltyDuration);

        float loserPenalty = gs.collisionBasePenalty * trackPenaltyMul;
        float loserDuration = gs.loserPenaltyDuration * trackLoserDurMul;
        loser.ApplyCollisionPenalty(loserPenalty, loserDuration);

        // ── 진행도 비교 → 뒤에 있는 쪽에 슬링샷 ──
        RacerController behind = a.OverallProgress <= b.OverallProgress ? a : b;
        float brave = behind.CharData.charBaseBrave;
        float slingshotMul = track != null ? track.slingshotMultiplier : 1f;
        float boost = brave * gs.slingshotFactor * slingshotMul;
        float behindPenaltyDur = (behind == loser) ? loserDuration : gs.winnerPenaltyDuration;

        slingshotQueue.Add(new SlingshotReservation
        {
            racer = behind,
            triggerTime = Time.time + behindPenaltyDur,
            boost = boost,
            duration = gs.slingshotDuration,
            reason = string.Format("{0}(pow:{1}) > {2}(pow:{3}) 충돌 → {4} 뒤처져서 슬링샷 (brave:{5})",
                winner.CharData.charName, winner.CharData.charBasePower,
                loser.CharData.charName, loser.CharData.charBasePower,
                behind.CharData.charName, behind.CharData.charBaseBrave)
        });

        // ── 디버그 로그 ──
        if (debugOverlay != null)
        {
            string behindName = behind.CharData.charName;
            debugOverlay.LogEvent(RaceDebugOverlay.EventType.CollisionHit,
                string.Format("{0}(pow:{1}) > {2}(pow:{3}) 충돌! 슬링샷→{4}(brv:{5})",
                    winner.CharData.charName, winner.CharData.charBasePower,
                    loser.CharData.charName, loser.CharData.charBasePower,
                    behindName, behind.CharData.charBaseBrave));
        }

        // ── 시각 효과 ──
        if (gs.enableCollisionVFX)
        {
            ShakeRacer(winner, gs.shakeWinnerDuration, gs.shakeMagnitude);
            ShakeRacer(loser, gs.shakeLoserDuration, gs.shakeMagnitude);

            ShowVFX(winner, CollisionVFXType.Hit, 0.6f);
            ShowVFX(loser, CollisionVFXType.Hit, 0.8f);

            bool attacked = winner.PlayAttackAnim();
            if (attacked)
            {
                var attackDebug = GetComponent<RaceDebugOverlay>();
                if (attackDebug != null)
                {
                    string weaponType = winner.CharData.charWeapon == WeaponHand.Left ? "Slash"
                        : winner.CharData.charWeapon == WeaponHand.Right ? "Shoot" : "?";
                    attackDebug.LogEvent(RaceDebugOverlay.EventType.Attack,
                        string.Format("{0} 공격모션! ({1}) → {2}",
                            winner.CharData.charName, weaponType,
                            loser.CharData.charName));
                }
            }
        }
    }

    // ══════════════════════════════════════
    //  슬링샷 예약 처리
    // ══════════════════════════════════════

    private void ProcessSlingshotQueue(GameSettings gs)
    {
        for (int i = slingshotQueue.Count - 1; i >= 0; i--)
        {
            var res = slingshotQueue[i];
            if (res.racer == null || res.racer.IsFinished)
            {
                slingshotQueue.RemoveAt(i);
                continue;
            }

            if (Time.time >= res.triggerTime)
            {
                res.racer.ApplySlingshot(res.boost, res.duration);

                var debugOverlay = GetComponent<RaceDebugOverlay>();
                if (debugOverlay != null)
                {
                    debugOverlay.LogEvent(RaceDebugOverlay.EventType.Slingshot,
                        string.Format("{0} 슬링샷! +{1:F0}% | 원인: {2}",
                            res.racer.CharData.charName,
                            res.boost * 100,
                            res.reason));
                }

                if (gs.enableCollisionVFX)
                    ShowVFX(res.racer, CollisionVFXType.Slingshot, 1.0f);

                slingshotQueue.RemoveAt(i);
            }
        }
    }

    // ══════════════════════════════════════
    //  쿨다운 갱신
    // ══════════════════════════════════════

    private void UpdateCooldowns()
    {
        float dt = Time.deltaTime;

        if (globalCooldown > 0f) globalCooldown -= dt;

        var pairKeys = new List<int>(pairCooldowns.Keys);
        for (int i = 0; i < pairKeys.Count; i++)
        {
            int key = pairKeys[i];
            float val = pairCooldowns[key] - dt;
            if (val <= 0f) pairCooldowns.Remove(key);
            else pairCooldowns[key] = val;
        }

        var racerKeys = new List<int>(racerCooldowns.Keys);
        for (int i = 0; i < racerKeys.Count; i++)
        {
            int key = racerKeys[i];
            float val = racerCooldowns[key] - dt;
            if (val <= 0f) racerCooldowns.Remove(key);
            else racerCooldowns[key] = val;
        }
    }

    // ══════════════════════════════════════
    //  유틸
    // ══════════════════════════════════════

    private int GetPairKey(int a, int b)
    {
        int min = Mathf.Min(a, b);
        int max = Mathf.Max(a, b);
        return min * 100 + max;
    }

    private int CountNearby(List<RacerController> racers, Vector3 pos, float range)
    {
        int count = 0;
        for (int i = 0; i < racers.Count; i++)
        {
            if (racers[i].IsFinished) continue;
            if (Vector3.Distance(racers[i].transform.position, pos) < range) count++;
        }
        return count;
    }

    // ══════════════════════════════════════
    //  시각 효과
    // ══════════════════════════════════════

    private void ShakeRacer(RacerController racer, float duration, float magnitude)
    {
        var shaker = racer.GetComponent<CollisionShake>();
        if (shaker == null)
            shaker = racer.gameObject.AddComponent<CollisionShake>();
        shaker.StartShake(duration, magnitude);
    }

    private void ShowVFX(RacerController racer, CollisionVFXType vfxType, float duration)
    {
        var display = racer.GetComponent<CollisionVFX>();
        if (display == null)
            display = racer.gameObject.AddComponent<CollisionVFX>();
        display.Show(vfxType, duration);
    }

    // ══════════════════════════════════════
    //  레이스 리셋
    // ══════════════════════════════════════

    public void ClearAll()
    {
        pairCooldowns.Clear();
        racerCooldowns.Clear();
        slingshotQueue.Clear();
        globalCooldown = 0f;
    }
}
