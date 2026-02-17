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
///   1) CollisionPenalty > 0  → 감속 중 충돌 불가
///   2) racerCooldowns        → 개별 레이서 쿨다운 (어떤 상대든)
///   3) pairCooldowns         → 같은 쌍 쿨다운
///   4) return                → 프레임당 최대 1건
///   5) collisionChance       → 확률적 불발
/// </summary>
public class CollisionSystem : MonoBehaviour
{
    // ── 쿨다운 관리 ──
    private Dictionary<int, float> pairCooldowns = new Dictionary<int, float>();
    private Dictionary<int, float> racerCooldowns = new Dictionary<int, float>();

    // ── 글로벌 충돌 쿨다운 (어떤 쌍이든 최소 간격) ──
    private float globalCooldown = 0f;
    private const float GLOBAL_COLLISION_INTERVAL = 0.5f; // 최소 0.5초 간격

    // ── 슬링샷 예약 (감속 종료 후 발동) ──
    private List<SlingshotReservation> slingshotQueue = new List<SlingshotReservation>();

    private struct SlingshotReservation
    {
        public RacerController racer;
        public float triggerTime;
        public float boost;
        public float duration;
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
        // ★ 글로벌 쿨다운: 어떤 충돌이든 최소 간격 보장
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

                // ① 거리 체크
                float dist = Vector3.Distance(ri.transform.position, rj.transform.position);
                if (dist >= range) continue;

                // ② 쌍 쿨다운 체크
                int pairKey = GetPairKey(ri.RacerIndex, rj.RacerIndex);
                if (pairCooldowns.ContainsKey(pairKey) && pairCooldowns[pairKey] > 0f) continue;

                // ③ 밀집 감쇄
                if (gs.crowdThreshold > 0)
                {
                    int nearby = CountNearby(racers, ri.transform.position, range);
                    if (nearby >= gs.crowdThreshold)
                    {
                        if (Random.value > gs.crowdDampen) continue;
                    }
                }

                // ④ 충돌 발생 확률 체크
                if (Random.value > gs.collisionChance) continue;

                // ⑤ 충돌 발생!
                ResolveCollision(ri, rj, gs, track);

                // 쌍 쿨다운
                pairCooldowns[pairKey] = gs.collisionCooldown;

                // 개별 레이서 쿨다운 (양쪽 모두)
                racerCooldowns[ri.RacerIndex] = gs.collisionCooldown;
                racerCooldowns[rj.RacerIndex] = gs.collisionCooldown;

                // ★ 글로벌 쿨다운 설정
                globalCooldown = GLOBAL_COLLISION_INTERVAL;

                // ★ 프레임당 1건만! 즉시 전체 루프 종료
                return;
            }
        }
    }

    private bool HasRacerCooldown(int racerIndex)
    {
        if (racerCooldowns.ContainsKey(racerIndex) && racerCooldowns[racerIndex] > 0f)
        {
            return true;
        }
        return false;
    }

    // ══════════════════════════════════════
    //  충돌 해결
    // ══════════════════════════════════════

    private void ResolveCollision(RacerController a, RacerController b, GameSettings gs, TrackData track)
    {
        var debugOverlay = GetComponent<RaceDebugOverlay>();
        var cdA = a.CharData;
        var cdB = b.CharData;
        if (cdA == null || cdB == null) return;

        // ── power 확률적 승패 결정 ──
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

        RacerController winner, loser;
        if (Random.value < bWinChance)
        { winner = b; loser = a; }
        else
        { winner = a; loser = b; }

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
                ShowEmoji(loser, "🛡️", 0.5f);
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
            duration = gs.slingshotDuration
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
            ShowEmoji(winner, "💥", 0.4f);
            ShowEmoji(loser, "💥", 0.6f);

            // 승자 공격 애니메이션
            bool attacked = winner.PlayAttackAnim();

            // ★ 실제 모션 발동 시에만 로그
            if (attacked)
            {
                var attackDebug = GetComponent<RaceDebugOverlay>();
                if (attackDebug != null)
                {
                    string weaponType = winner.CharData.charWeapon == WeaponHand.Left ? "Slash" :
                                        winner.CharData.charWeapon == WeaponHand.Right ? "Shoot" : "?";
                    attackDebug.LogEvent(RaceDebugOverlay.EventType.Attack,
                        string.Format("{0} 공격모션! ({1}) → {2}",
                            winner.CharData.charName, weaponType, loser.CharData.charName));
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
                        string.Format("{0} 슬링샷! +{1:F0}% (brave:{2})",
                            res.racer.CharData.charName,
                            res.boost * 100,
                            res.racer.CharData.charBaseBrave));
                }

                if (gs.enableCollisionVFX)
                    ShowEmoji(res.racer, "🚀", 0.8f);

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

        // 글로벌 쿨다운
        if (globalCooldown > 0f) globalCooldown -= dt;

        // 쌍 쿨다운
        var pairKeys = new List<int>(pairCooldowns.Keys);
        for (int i = 0; i < pairKeys.Count; i++)
        {
            int key = pairKeys[i];
            float val = pairCooldowns[key] - dt;
            if (val <= 0f)
                pairCooldowns.Remove(key);
            else
                pairCooldowns[key] = val;
        }

        // 개별 레이서 쿨다운
        var racerKeys = new List<int>(racerCooldowns.Keys);
        for (int i = 0; i < racerKeys.Count; i++)
        {
            int key = racerKeys[i];
            float val = racerCooldowns[key] - dt;
            if (val <= 0f)
                racerCooldowns.Remove(key);
            else
                racerCooldowns[key] = val;
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
            if (Vector3.Distance(racers[i].transform.position, pos) < range)
                count++;
        }
        return count;
    }

    // ══════════════════════════════════════
    //  시각 효과
    // ══════════════════════════════════════

    private void ShakeRacer(RacerController racer, float duration, float magnitude)
    {
        var shaker = racer.GetComponent<CollisionShake>();
        if (shaker == null) shaker = racer.gameObject.AddComponent<CollisionShake>();
        shaker.StartShake(duration, magnitude);
    }

    private void ShowEmoji(RacerController racer, string emoji, float duration)
    {
        var display = racer.GetComponent<CollisionEmoji>();
        if (display == null) display = racer.gameObject.AddComponent<CollisionEmoji>();
        display.Show(emoji, duration);
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

// ══════════════════════════════════════════
//  충돌 흔들림 컴포넌트
// ══════════════════════════════════════════

public class CollisionShake : MonoBehaviour
{
    private float shakeTimer = 0f;
    private float shakeMagnitude = 0.05f;
    private bool isShaking = false;

    public void StartShake(float duration, float magnitude)
    {
        shakeMagnitude = magnitude;
        shakeTimer = duration;
        isShaking = true;
    }

    private void Update()
    {
        if (!isShaking) return;

        shakeTimer -= Time.deltaTime;
        if (shakeTimer <= 0f)
        {
            isShaking = false;
            return;
        }

        float offsetX = Random.Range(-shakeMagnitude, shakeMagnitude);
        float offsetY = Random.Range(-shakeMagnitude, shakeMagnitude);
        transform.localPosition += new Vector3(offsetX, offsetY, 0f);
    }
}

// ══════════════════════════════════════════
//  충돌 이모지 표시 컴포넌트
// ══════════════════════════════════════════

public class CollisionEmoji : MonoBehaviour
{
    private GameObject emojiObj;
    private TextMesh textMesh;
    private float timer = 0f;

    public void Show(string emoji, float duration)
    {
        if (emojiObj == null)
        {
            emojiObj = new GameObject("CollisionEmoji");
            emojiObj.transform.SetParent(transform);
            emojiObj.transform.localPosition = new Vector3(0, 1.8f, 0);

            textMesh = emojiObj.AddComponent<TextMesh>();
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.characterSize = 0.15f;
            textMesh.fontSize = 40;
        }

        textMesh.text = emoji;
        emojiObj.SetActive(true);
        timer = duration;

        float parentScaleX = Mathf.Sign(transform.localScale.x);
        Vector3 ls = emojiObj.transform.localScale;
        ls.x = Mathf.Abs(ls.x) * parentScaleX;
        emojiObj.transform.localScale = ls;
    }

    private void Update()
    {
        if (emojiObj == null || !emojiObj.activeSelf) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            emojiObj.SetActive(false);
        }
        else
        {
            float parentScaleX = Mathf.Sign(transform.localScale.x);
            Vector3 ls = emojiObj.transform.localScale;
            ls.x = Mathf.Abs(ls.x) * parentScaleX;
            emojiObj.transform.localScale = ls;
        }
    }
}