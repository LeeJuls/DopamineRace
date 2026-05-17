using UnityEngine;

/// <summary>
/// SPEC-033: per-stat 숨은 보정 multiplier.
/// 유저는 스탯을 1~20으로 보지만(육각형), 실제 계산은 effective = 표시값 × hidden_&lt;stat&gt;.
/// 데이터: Resources/Data/HiddenStatWeights.csv (헤더 + 값 1줄).
/// 기본 1.0 (보정 없음). 밸런스 튜닝으로 스탯간 가치 비중을 보이지 않게 조정.
/// </summary>
public static class HiddenStatWeights
{
    public static float Speed        { get; private set; } = 1f;
    public static float Accel        { get; private set; } = 1f;
    public static float Power        { get; private set; } = 1f;
    public static float Stamina      { get; private set; } = 1f;
    public static float Intelligence { get; private set; } = 1f;
    public static float Luck         { get; private set; } = 1f;

    private static bool _loaded = false;

    /// <summary>최초 접근/명시 호출 시 CSV 로드. 누락 시 전부 1.0.</summary>
    public static void EnsureLoaded()
    {
        if (_loaded) return;
        Load();
    }

    public static void Reload()
    {
        _loaded = false;
        Load();
    }

    private static void Load()
    {
        _loaded = true;
        TextAsset csv = Resources.Load<TextAsset>("Data/HiddenStatWeights");
        if (csv == null)
        {
            Debug.LogWarning("[HiddenStatWeights] Data/HiddenStatWeights.csv 없음 → 전부 1.0");
            return;
        }

        string[] lines = csv.text.Split('\n');
        if (lines.Length < 2)
        {
            Debug.LogWarning("[HiddenStatWeights] CSV 형식 오류(값 줄 없음) → 전부 1.0");
            return;
        }

        string[] v = lines[1].Trim().Split(',');
        if (v.Length < 6)
        {
            Debug.LogWarning("[HiddenStatWeights] CSV 컬럼 부족 → 전부 1.0");
            return;
        }

        Speed        = Parse(v[0]);
        Accel        = Parse(v[1]);
        Power        = Parse(v[2]);
        Stamina      = Parse(v[3]);
        Intelligence = Parse(v[4]);
        Luck         = Parse(v[5]);
        Debug.Log($"[HiddenStatWeights] 로드: SPD={Speed} ACC={Accel} POW={Power} STA={Stamina} INT={Intelligence} LUCK={Luck}");
    }

    private static float Parse(string s)
    {
        float f;
        if (float.TryParse(s.Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out f) && f > 0f)
            return f;
        return 1f;
    }
}
