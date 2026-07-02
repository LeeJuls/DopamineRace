using UnityEngine;
using UnityEditor;

/// <summary>
/// 트랙 스탯 상성(stat_affinity) 데이터 검증 — 오타 스탯명·범위초과·형식오류 검출.
/// TrackStatAffinity.ParseList가 graceful(불량 항목을 침묵 제거)이라 데이터 오류가 무경고로 묻히는 것을 방지.
/// (예: "Powr:0.06" 오타 → 런타임은 조용히 무보너스 → 검증툴만이 잡을 수 있음)
/// SFXSettingsValidator 패턴. 메뉴: DopamineRace > Validate Track Data.
/// </summary>
public static class TrackStatAffinityValidator
{
    [MenuItem("DopamineRace/Validate Track Data")]
    public static void Validate()
    {
        var csv = Resources.Load<TextAsset>("Data/TrackDB");
        if (csv == null) { Debug.LogError("[TrackValidator] Data/TrackDB.csv 로드 실패"); return; }

        var lines = csv.text.Split('\n');
        if (lines.Length < 2) { Debug.LogError("[TrackValidator] CSV 데이터 행 없음"); return; }

        var header = lines[0].Trim().Split(',');
        int affIdx = System.Array.IndexOf(header, "stat_affinity");
        if (affIdx < 0) { Debug.LogError("[TrackValidator] 헤더에 stat_affinity 컬럼 없음"); return; }
        int idIdx = System.Array.IndexOf(header, "track_id");

        int errors = 0, warns = 0, tracksWithAff = 0;
        var validStats = System.Enum.GetNames(typeof(V4StatType));
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== 트랙 stat_affinity 검증 ===");

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            var cols = line.Split(',');
            string trackId = (idIdx >= 0 && idIdx < cols.Length) ? cols[idIdx].Trim() : "?";
            if (affIdx >= cols.Length) continue;   // 해당 행에 stat_affinity 없음(빈=무보너스, 정상)
            string raw = cols[affIdx].Trim();
            if (raw.Length == 0 || raw.ToLower() == "none") continue;

            tracksWithAff++;
            foreach (var segRaw in raw.Split(';'))
            {
                var seg = segRaw.Trim();
                if (seg.Length == 0) continue;
                var kv = seg.Split(':');
                if (kv.Length < 2)
                {
                    sb.AppendLine($"❌ [{trackId}] 형식 오류(콜론 없음): '{seg}'"); errors++; continue;
                }
                string statName = kv[0].Trim();
                bool statOk = System.Array.Exists(validStats,
                    s => s.Equals(statName, System.StringComparison.OrdinalIgnoreCase));
                if (!statOk)
                {
                    sb.AppendLine($"❌ [{trackId}] 알 수 없는 스탯명: '{statName}' (유효: {string.Join("/", validStats)})");
                    errors++; continue;
                }
                if (!float.TryParse(kv[1].Trim(), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float b))
                {
                    sb.AppendLine($"❌ [{trackId}] 계수 숫자 아님: '{kv[1]}'"); errors++; continue;
                }
                if (b <= 0f) { sb.AppendLine($"⚠ [{trackId}] {statName} 계수 ≤0 → 무효과: {b}"); warns++; }
                else if (b > TrackStatAffinity.BONUS_MAX)
                { sb.AppendLine($"⚠ [{trackId}] {statName} 계수 {b} > 상한 {TrackStatAffinity.BONUS_MAX} → 클램프됨"); warns++; }
                else sb.AppendLine($"✅ [{trackId}] {statName}:{b}");
            }
        }

        sb.AppendLine($"— 상성 보유 트랙 {tracksWithAff}종 / 오류 {errors} / 경고 {warns}");
        if (errors > 0) Debug.LogError(sb.ToString());
        else if (warns > 0) Debug.LogWarning(sb.ToString());
        else Debug.Log(sb.ToString());
    }
}
