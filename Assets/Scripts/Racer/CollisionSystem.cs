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
                        string.Format("{0} 슬링샷! +{1:F0}% (brave:{2})",
                            res.racer.CharData.charName,
                            res.boost * 100,
                            res.racer.CharData.charBaseBrave));
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
//  충돌 VFX 타입
// ══════════════════════════════════════════

public enum CollisionVFXType
{
    Hit,        // 💥 충돌
    Dodge,      // 🛡️ 회피
    Slingshot   // 🚀 슬링샷
}

// ══════════════════════════════════════════
//  충돌 VFX 표시 컴포넌트 (SpriteRenderer 기반)
//
//  TextMesh는 이모지를 렌더링할 수 없으므로
//  런타임 생성 스프라이트 + 텍스트로 대체
// ══════════════════════════════════════════

public class CollisionVFX : MonoBehaviour
{
    private GameObject vfxRoot;
    private SpriteRenderer bgSprite;
    private SpriteRenderer iconSprite;
    private TextMesh label;

    private float timer = 0f;
    private float totalDuration = 0f;
    private Vector3 startLocalPos;
    private float floatSpeed = 1.5f;

    // ── 캐싱된 텍스처/스프라이트 (static으로 1번만 생성) ──
    private static Sprite circleSprite;
    private static Sprite starSprite;
    private static Sprite arrowSprite;
    private static Sprite shieldSprite;
    private static bool spritesCreated = false;

    private static void EnsureSprites()
    {
        if (spritesCreated) return;
        circleSprite = CreateCircleSprite(32, Color.white);
        starSprite = CreateStarSprite(32, Color.white);
        arrowSprite = CreateArrowSprite(32, Color.white);
        shieldSprite = CreateShieldSprite(32, Color.white);
        spritesCreated = true;
    }

    public void Show(CollisionVFXType type, float duration)
    {
        EnsureSprites();

        var gs = GameSettings.Instance;

        if (vfxRoot == null)
            CreateVFXObjects(gs);

        // ── GameSettings에서 크기 읽기 ──
        float iconScale = gs.vfxIconScale;
        float labelCharSize = gs.vfxLabelSize;
        float height = gs.vfxHeight;
        floatSpeed = gs.vfxFloatSpeed;

        // ── 타입별 설정 ──
        Color bgColor;
        Color iconColor;
        Sprite icon;
        string text;

        switch (type)
        {
            case CollisionVFXType.Hit:
                bgColor = new Color(1f, 0.2f, 0.1f, 0.9f);    // 빨강
                iconColor = new Color(1f, 1f, 0.3f, 1f);        // 노랑
                icon = gs.vfxHitIcon != null ? gs.vfxHitIcon : starSprite;
                text = "HIT!";
                break;

            case CollisionVFXType.Dodge:
                bgColor = new Color(0.2f, 0.6f, 1f, 0.9f);     // 파랑
                iconColor = new Color(0.8f, 1f, 1f, 1f);        // 연하늘
                icon = gs.vfxDodgeIcon != null ? gs.vfxDodgeIcon : shieldSprite;
                text = "MISS";
                break;

            case CollisionVFXType.Slingshot:
                bgColor = new Color(0.1f, 0.9f, 0.3f, 0.9f);   // 초록
                iconColor = new Color(1f, 1f, 0.5f, 1f);        // 연노랑
                icon = gs.vfxSlingshotIcon != null ? gs.vfxSlingshotIcon : arrowSprite;
                text = "BOOST!";
                break;

            default:
                bgColor = Color.white;
                iconColor = Color.white;
                icon = circleSprite;
                text = "?";
                break;
        }

        // ── 커스텀 아이콘이면 tint 안 먹이기 ──
        bool isCustomIcon = (type == CollisionVFXType.Hit && gs.vfxHitIcon != null)
            || (type == CollisionVFXType.Dodge && gs.vfxDodgeIcon != null)
            || (type == CollisionVFXType.Slingshot && gs.vfxSlingshotIcon != null);

        // ── 적용 ──
        bgSprite.sprite = circleSprite;
        bgSprite.color = bgColor;
        bgSprite.transform.localScale = Vector3.one * iconScale;

        iconSprite.sprite = icon;
        iconSprite.color = isCustomIcon ? Color.white : iconColor;
        iconSprite.transform.localScale = Vector3.one * (iconScale * 0.6f);
        iconSprite.transform.localPosition = new Vector3(0, 0, -0.01f);

        label.text = text;
        label.color = Color.white;
        label.characterSize = labelCharSize;

        startLocalPos = new Vector3(0, height, 0);
        vfxRoot.transform.localPosition = startLocalPos;
        vfxRoot.SetActive(true);

        timer = duration;
        totalDuration = duration;

        // 부모 스케일 반전 보정
        FixFlip();
    }

    private void CreateVFXObjects(GameSettings gs)
    {
        float height = gs != null ? gs.vfxHeight : 1.8f;
        float labelCharSize = gs != null ? gs.vfxLabelSize : 0.08f;

        vfxRoot = new GameObject("CollisionVFX");
        vfxRoot.transform.SetParent(transform);
        vfxRoot.transform.localPosition = new Vector3(0, height, 0);

        // 배경 원
        var bgObj = new GameObject("BG");
        bgObj.transform.SetParent(vfxRoot.transform);
        bgObj.transform.localPosition = Vector3.zero;
        bgSprite = bgObj.AddComponent<SpriteRenderer>();
        bgSprite.sortingOrder = 100;

        // 아이콘
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(vfxRoot.transform);
        iconObj.transform.localPosition = new Vector3(0, 0, -0.01f);
        iconSprite = iconObj.AddComponent<SpriteRenderer>();
        iconSprite.sortingOrder = 101;

        // 텍스트 라벨
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(vfxRoot.transform);
        labelObj.transform.localPosition = new Vector3(0, -0.35f, -0.02f);
        label = labelObj.AddComponent<TextMesh>();
        label.alignment = TextAlignment.Center;
        label.anchor = TextAnchor.MiddleCenter;
        label.characterSize = labelCharSize;
        label.fontSize = 36;
        label.fontStyle = FontStyle.Bold;
        label.color = Color.white;

        // 텍스트도 sorting 맞추기
        var labelRenderer = labelObj.GetComponent<MeshRenderer>();
        if (labelRenderer != null)
            labelRenderer.sortingOrder = 102;
    }

    private void Update()
    {
        if (vfxRoot == null || !vfxRoot.activeSelf) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            vfxRoot.SetActive(false);
            return;
        }

        float progress = 1f - (timer / totalDuration); // 0 → 1

        // ── 위로 떠오르기 ──
        Vector3 pos = startLocalPos;
        pos.y += progress * floatSpeed;
        vfxRoot.transform.localPosition = pos;

        // ── 페이드아웃 (후반 40%에서) ──
        float alpha = 1f;
        if (progress > 0.6f)
            alpha = 1f - ((progress - 0.6f) / 0.4f);

        Color bgCol = bgSprite.color;
        bgCol.a = alpha * 0.9f;
        bgSprite.color = bgCol;

        Color iconCol = iconSprite.color;
        iconCol.a = alpha;
        iconSprite.color = iconCol;

        Color lblCol = label.color;
        lblCol.a = alpha;
        label.color = lblCol;

        // ── 스케일 팝 효과 (처음 20%에서 확대 후 축소) ──
        float scale = 1f;
        if (progress < 0.2f)
        {
            float t = progress / 0.2f;
            scale = 1f + 0.3f * Mathf.Sin(t * Mathf.PI);
        }
        vfxRoot.transform.localScale = Vector3.one * scale;

        // 부모 반전 보정
        FixFlip();
    }

    private void FixFlip()
    {
        if (vfxRoot == null) return;
        float parentScaleX = Mathf.Sign(transform.localScale.x);
        Vector3 ls = vfxRoot.transform.localScale;
        ls.x = Mathf.Abs(ls.x) * parentScaleX;
        vfxRoot.transform.localScale = ls;
    }

    // ══════════════════════════════════════════
    //  런타임 스프라이트 생성
    // ══════════════════════════════════════════

    /// <summary>원형 스프라이트</summary>
    private static Sprite CreateCircleSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float center = size / 2f;
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                    tex.SetPixel(x, y, color);
                else if (dist <= radius + 1f)
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, 0.5f));
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    /// <summary>별 모양 스프라이트 (충돌)</summary>
    private static Sprite CreateStarSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float center = size / 2f;

        Color[] clear = new Color[size * size];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);

        int points = 6;
        float outerR = size / 2f - 1f;
        float innerR = outerR * 0.45f;

        // 별 꼭짓점 계산
        Vector2[] verts = new Vector2[points * 2];
        for (int i = 0; i < points * 2; i++)
        {
            float angle = (i * Mathf.PI / points) - Mathf.PI / 2f;
            float r = (i % 2 == 0) ? outerR : innerR;
            verts[i] = new Vector2(center + Mathf.Cos(angle) * r, center + Mathf.Sin(angle) * r);
        }

        // 채우기 (간단한 scanline)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (IsPointInPolygon(new Vector2(x, y), verts))
                    tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    /// <summary>위쪽 화살표 스프라이트 (슬링샷)</summary>
    private static Sprite CreateArrowSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color[] clear = new Color[size * size];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);

        float center = size / 2f;

        // 화살표 머리 (삼각형)
        Vector2[] head = new Vector2[]
        {
            new Vector2(center, size - 2),           // 꼭대기
            new Vector2(3, size * 0.5f),             // 좌하
            new Vector2(size - 3, size * 0.5f),      // 우하
        };

        // 화살표 몸통 (직사각형)
        float bodyLeft = size * 0.3f;
        float bodyRight = size * 0.7f;
        float bodyTop = size * 0.55f;
        float bodyBottom = 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inHead = IsPointInTriangle(new Vector2(x, y), head[0], head[1], head[2]);
                bool inBody = x >= bodyLeft && x <= bodyRight && y >= bodyBottom && y <= bodyTop;
                if (inHead || inBody)
                    tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    /// <summary>방패 모양 스프라이트 (회피)</summary>
    private static Sprite CreateShieldSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color[] clear = new Color[size * size];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);

        float centerX = size / 2f;
        float top = size - 2f;
        float bottom = 2f;

        for (int y = 0; y < size; y++)
        {
            float t = (float)y / size;
            // 방패: 위쪽은 넓고 아래로 갈수록 좁아지는 형태
            float halfWidth;
            if (t > 0.5f)
                halfWidth = (size / 2f - 2f);  // 상단: 넓은 폭
            else
                halfWidth = (size / 2f - 2f) * (t / 0.5f);  // 하단: 점점 좁아짐 (뾰족)

            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - centerX);
                if (dx <= halfWidth && y >= bottom && y <= top)
                {
                    // 테두리 효과
                    bool isBorder = (dx >= halfWidth - 1.5f) ||
                                    (y <= bottom + 1.5f) ||
                                    (y >= top - 1.5f) ||
                                    (t <= 0.5f && dx >= halfWidth - 1.5f);
                    if (isBorder)
                        tex.SetPixel(x, y, color);
                    else
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, 0.5f));
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // ── 기하 유틸 ──

    private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;
        for (int i = 0; i < polygon.Length; i++)
        {
            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y)
                / (polygon[j].y - polygon[i].y) + polygon[i].x))
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }

    private static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
}