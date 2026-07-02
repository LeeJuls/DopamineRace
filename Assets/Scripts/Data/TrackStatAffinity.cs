using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// V4 스탯 종류 — 트랙 스탯 상성(stat_affinity) 데이터 주도용.
/// CharacterDataV4의 6대 스탯과 1:1 대응.
/// </summary>
public enum V4StatType
{
    Speed,
    Accel,
    Stamina,
    Power,
    Intelligence,
    Luck,
}

/// <summary>
/// 트랙별 스탯 상성 한 항목 — "이 트랙에서 이 스탯이 속도에 유리".
/// TrackDB.csv stat_affinity 컬럼에서 콜론 인코딩으로 파싱(PassiveSkillData 패턴 미러).
///
/// CSV 형식(세미콜론으로 다중 항목):
///   "Power:0.06"                   ← 사막: Power 유리
///   "Intelligence:0.07"            ← 고원: Intelligence 유리
///   "Power:0.05;Intelligence:0.03" ← 복합(중복 스탯은 합산)
///   "" 또는 "none"                 ← 무보너스(무효과)
///
/// ★ 하드코딩 금지 원칙: 트랙별 유리 스탯·계수는 전부 CSV 데이터에만 존재. 코드는 값에 무관.
/// </summary>
[System.Serializable]
public class TrackStatAffinity
{
    public V4StatType stat;
    public float bonus;   // vmax ×= 1 + (스탯값 × HiddenWeight / 20) × bonus

    /// <summary>affinity 계수 안전 상한 (만렙 20 스탯 기준 +15% vmax). 지배 방지.</summary>
    public const float BONUS_MAX = 0.15f;

    /// <summary>정규화 기준 스탯 만렙(20). 만렙·weight=1일 때 항이 = bonus로 수렴.</summary>
    private const float STAT_MAX = 20f;

    /// <summary>
    /// CSV stat_affinity 문자열 → 리스트 파싱. 빈/none → 빈 리스트(무효과).
    /// 각 항목 "Stat:bonus", 다중은 ';' 구분. 파싱 실패 항목은 조용히 무시(graceful).
    /// ※ 로드 시 1회만 호출 → 경고 로그 허용(매 틱 호출되는 ComputeVmaxMultiplier와 구분).
    /// </summary>
    public static List<TrackStatAffinity> ParseList(string raw)
    {
        var list = new List<TrackStatAffinity>();
        if (string.IsNullOrEmpty(raw)) return list;

        string trimmed = raw.Trim();
        if (trimmed.Length == 0 || trimmed.ToLower() == "none") return list;

        string[] segments = trimmed.Split(';');
        foreach (var segRaw in segments)
        {
            string seg = segRaw.Trim();
            if (seg.Length == 0) continue;

            string[] kv = seg.Split(':');
            if (kv.Length < 2)
            {
                Debug.LogWarning($"[TrackStatAffinity] 형식 오류(콜론 없음): \"{seg}\" → 무시");
                continue;
            }

            V4StatType stat;
            try
            {
                stat = (V4StatType)System.Enum.Parse(typeof(V4StatType), kv[0].Trim(), true);
            }
            catch
            {
                Debug.LogWarning($"[TrackStatAffinity] 알 수 없는 스탯명: \"{kv[0]}\" → 무시");
                continue;
            }

            if (!float.TryParse(kv[1].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float bonus))
            {
                Debug.LogWarning($"[TrackStatAffinity] 계수 파싱 실패: \"{kv[1]}\" → 무시");
                continue;
            }

            bonus = Mathf.Clamp(bonus, 0f, BONUS_MAX);
            if (bonus <= 0f) continue;   // 0 이하 = 무효과 → 제외

            list.Add(new TrackStatAffinity { stat = stat, bonus = bonus });
        }
        return list;
    }

    /// <summary>
    /// 트랙 스탯 상성 → vmax 곱계수. 실엔진(CalcSpeedV4)·백테스트(SimTickV4)가 공유 호출 → 미러 드리프트 불가.
    /// = 1 + Σ (스탯값 × HiddenWeight / 20 × bonus). affs 없음/빈 → 1.0(무효과, 원동작 보존).
    /// ★ 매 틱·매 레이서 호출 → 순수 산술만(로그·할당 금지). 중복 스탯은 자연 합산.
    /// </summary>
    public static float ComputeVmaxMultiplier(CharacterDataV4 cd, List<TrackStatAffinity> affs)
    {
        if (cd == null || affs == null || affs.Count == 0) return 1f;

        float sum = 0f;
        for (int i = 0; i < affs.Count; i++)
        {
            var a = affs[i];
            float statVal, weight;
            switch (a.stat)
            {
                case V4StatType.Speed:        statVal = cd.v4Speed;        weight = HiddenStatWeights.Speed;        break;
                case V4StatType.Accel:        statVal = cd.v4Accel;        weight = HiddenStatWeights.Accel;        break;
                case V4StatType.Stamina:      statVal = cd.v4Stamina;      weight = HiddenStatWeights.Stamina;      break;
                case V4StatType.Power:        statVal = cd.v4Power;        weight = HiddenStatWeights.Power;        break;
                case V4StatType.Intelligence: statVal = cd.v4Intelligence; weight = HiddenStatWeights.Intelligence; break;
                case V4StatType.Luck:         statVal = cd.v4Luck;         weight = HiddenStatWeights.Luck;         break;
                default: continue;
            }
            sum += (statVal * weight / STAT_MAX) * a.bonus;
        }
        return 1f + sum;
    }
}
