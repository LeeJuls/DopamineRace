/// <summary>
/// 라운드별 트랙 선택용 enum (GameSettings.roundTracks).
///
/// ⚠️ TrackDB.csv에 트랙 추가 시 본 enum도 동기화 필수.
///    enum int 값과 CSV trackId 매핑은 ToTrackId()에 정의.
/// </summary>
public enum TrackChoice
{
    Random   = 0,   // 가중 랜덤 (이전 트랙 제외)
    Normal   = 1,
    Rainy    = 2,
    Snow     = 3,
    Desert   = 4,
    Highland = 5
}

public static class TrackChoiceExtensions
{
    /// <summary>enum → CSV trackId 변환. Random은 빈 문자열.</summary>
    public static string ToTrackId(this TrackChoice c)
    {
        switch (c)
        {
            case TrackChoice.Normal:   return "normal";
            case TrackChoice.Rainy:    return "rainy";
            case TrackChoice.Snow:     return "snow";
            case TrackChoice.Desert:   return "desert";
            case TrackChoice.Highland: return "highland";
            default: return ""; // Random
        }
    }
}
