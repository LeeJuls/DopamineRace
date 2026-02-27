using UnityEngine;

/// <summary>
/// 트랙 CSV 1행 데이터 클래스
/// TrackDB.csv에서 파싱된 트랙 정보를 담는다.
/// 
/// 사용: TrackDatabase에서 로드 → GameSettings.currentTrack에 반영
/// </summary>
public class TrackInfo
{
    // ── 기본 정보 ──
    public string trackId;
    public string trackName;
    public string trackIcon;
    public string trackDescription;
    public string bgPrefabPath;     // Resources 경로 (TrackPrefabs/Track_Normal)
    public int weight;              // 선택 확률 가중치

    // ── 전체 속도 ──
    public float speedMultiplier = 1f;

    // ── calm → 속도 변동 ──
    public float noiseMultiplier = 1f;

    // ── endurance → 피로 ──
    public float fatigueMultiplier = 1f;

    // ── 충돌 관련 ──
    public float collisionRangeMultiplier = 1f;
    public float collisionPenaltyMultiplier = 1f;

    // ── 슬링샷 ──
    public float slingshotMultiplier = 1f;

    // ── 타입 보너스 ──
    public float earlyBonusMultiplier = 1f;
    public float midBonusMultiplier = 1f;
    public float lateBonusMultiplier = 1f;

    // ── 특수 스탯 → 속도 ──
    public float powerSpeedBonus = 0f;
    public float braveSpeedBonus = 0f;

    // ── 운 ──
    public float luckMultiplier = 1f;

    // ── 특수 구간 ──
    public bool hasMidSlowZone = false;
    public float midSlowZoneStart = 0f;
    public float midSlowZoneEnd = 0f;
    public float midSlowZoneSpeedMultiplier = 1f;

    // ── 충돌 후 추가 효과 ──
    public float loserPenaltyDurationMultiplier = 1f;

    // ── 트랙 타입 ──
    public TrackType trackType = TrackType.E_Base;

    // ── 로컬라이즈 표시용 ──
    /// <summary>로컬라이즈된 트랙 이름</summary>
    public string DisplayName => Loc.Get(trackName);
    /// <summary>로컬라이즈된 트랙 설명</summary>
    public string DisplayDesc => Loc.Get(trackDescription);

    /// <summary>
    /// CSV 한 줄 파싱
    /// 헤더 순서:
    /// track_id,track_name,track_icon,track_description,bg_prefab,weight,
    /// speed_mul,noise_mul,fatigue_mul,collision_range_mul,collision_penalty_mul,
    /// slingshot_mul,early_bonus_mul,mid_bonus_mul,late_bonus_mul,
    /// power_speed_bonus,brave_speed_bonus,luck_mul,
    /// has_mid_slow_zone,mid_slow_start,mid_slow_end,mid_slow_speed_mul,
    /// loser_penalty_dur_mul,track_type
    /// </summary>
    public static TrackInfo ParseCSVLine(string line)
    {
        if (string.IsNullOrEmpty(line)) return null;

        string[] cols = line.Split(',');
        if (cols.Length < 23)
        {
            Debug.LogWarning("[TrackInfo] 컬럼 부족 (" + cols.Length + "): " + line);
            return null;
        }

        var t = new TrackInfo();

        try
        {
            int i = 0;
            t.trackId           = cols[i++].Trim();
            t.trackName         = cols[i++].Trim();
            t.trackIcon         = cols[i++].Trim();
            t.trackDescription  = cols[i++].Trim();
            t.bgPrefabPath      = cols[i++].Trim();
            t.weight            = ParseInt(cols[i++], 100);

            t.speedMultiplier               = ParseFloat(cols[i++], 1f);
            t.noiseMultiplier               = ParseFloat(cols[i++], 1f);
            t.fatigueMultiplier             = ParseFloat(cols[i++], 1f);
            t.collisionRangeMultiplier      = ParseFloat(cols[i++], 1f);
            t.collisionPenaltyMultiplier    = ParseFloat(cols[i++], 1f);
            t.slingshotMultiplier           = ParseFloat(cols[i++], 1f);
            t.earlyBonusMultiplier          = ParseFloat(cols[i++], 1f);
            t.midBonusMultiplier            = ParseFloat(cols[i++], 1f);
            t.lateBonusMultiplier           = ParseFloat(cols[i++], 1f);
            t.powerSpeedBonus               = ParseFloat(cols[i++], 0f);
            t.braveSpeedBonus               = ParseFloat(cols[i++], 0f);
            t.luckMultiplier                = ParseFloat(cols[i++], 1f);

            string slowStr = cols[i++].Trim().ToLower();
            t.hasMidSlowZone = (slowStr == "true" || slowStr == "1");

            t.midSlowZoneStart              = ParseFloat(cols[i++], 0f);
            t.midSlowZoneEnd                = ParseFloat(cols[i++], 0f);
            t.midSlowZoneSpeedMultiplier    = ParseFloat(cols[i++], 1f);
            t.loserPenaltyDurationMultiplier = ParseFloat(cols[i++], 1f);

            // track_type (옵션: 없으면 E_Base)
            if (i < cols.Length)
            {
                string typeStr = cols[i++].Trim();
                t.trackType = typeStr switch
                {
                    "E_Dirt" => TrackType.E_Dirt,
                    "E_Snow" => TrackType.E_Snow,
                    "E_Rain" => TrackType.E_Rain,
                    _ => TrackType.E_Base
                };
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[TrackInfo] 파싱 에러: " + e.Message + " → " + line);
            return null;
        }

        if (string.IsNullOrEmpty(t.trackId))
        {
            Debug.LogWarning("[TrackInfo] trackId 비어있음: " + line);
            return null;
        }

        return t;
    }

    /// <summary>
    /// TrackInfo 값을 TrackData ScriptableObject에 주입
    /// </summary>
    public TrackData ToTrackData()
    {
        var td = ScriptableObject.CreateInstance<TrackData>();
        td.trackName                    = trackName;
        td.trackType                    = trackType;
        td.trackIcon                    = trackIcon;
        td.trackDescription             = trackDescription;
        td.speedMultiplier              = speedMultiplier;
        td.noiseMultiplier              = noiseMultiplier;
        td.fatigueMultiplier            = fatigueMultiplier;
        td.collisionRangeMultiplier     = collisionRangeMultiplier;
        td.collisionPenaltyMultiplier   = collisionPenaltyMultiplier;
        td.slingshotMultiplier          = slingshotMultiplier;
        td.earlyBonusMultiplier         = earlyBonusMultiplier;
        td.midBonusMultiplier           = midBonusMultiplier;
        td.lateBonusMultiplier          = lateBonusMultiplier;
        td.powerSpeedBonus              = powerSpeedBonus;
        td.braveSpeedBonus              = braveSpeedBonus;
        td.luckMultiplier               = luckMultiplier;
        td.hasMidSlowZone               = hasMidSlowZone;
        td.midSlowZoneStart             = midSlowZoneStart;
        td.midSlowZoneEnd               = midSlowZoneEnd;
        td.midSlowZoneSpeedMultiplier   = midSlowZoneSpeedMultiplier;
        td.loserPenaltyDurationMultiplier = loserPenaltyDurationMultiplier;
        td.name                         = "Track_" + trackId;
        return td;
    }

    // ── 유틸 ──

    private static float ParseFloat(string s, float fallback)
    {
        s = s.Trim();
        float val;
        if (float.TryParse(s, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out val))
            return val;
        return fallback;
    }

    private static int ParseInt(string s, int fallback)
    {
        s = s.Trim();
        int val;
        if (int.TryParse(s, out val))
            return val;
        return fallback;
    }

    public override string ToString()
    {
        return string.Format("{0} {1} (w:{2})", trackIcon, trackName, weight);
    }
}
