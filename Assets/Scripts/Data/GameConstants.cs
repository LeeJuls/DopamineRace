using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 기존 코드 호환용 래퍼.
/// 이름/색상은 CharacterDatabase.SelectedCharacters에서 동적으로 가져옴.
/// </summary>
public static class GameConstants
{
    private static GameSettings S => GameSettings.Instance;

    public static int RACER_COUNT
    {
        get
        {
            var db = CharacterDatabase.Instance;
            if (db != null && db.SelectedCharacters.Count > 0)
                return db.SelectedCharacters.Count;
            return S.racerCount;
        }
    }
    public static float RACER_MIN_SPEED => S.racerMinSpeed;
    public static float RACER_MAX_SPEED => S.racerMaxSpeed;
    public static float SPEED_CHANGE_INTERVAL => S.speedChangeInterval;
    public static float SPEED_LERP_RATE => S.speedLerpRate;
    public static int TOTAL_LAPS => S.GetLapsForRound(1);
    public static int FIRST_PLACE_SCORE => S.firstPlaceScore;
    public static int SECOND_PLACE_SCORE => S.secondPlaceScore;

    // ★ 선발된 캐릭터에서 동적으로 생성
    private static string[] _cachedNames;
    private static Color[] _cachedColors;
    private static int _cachedVersion = -1;

    public static string[] RACER_NAMES
    {
        get
        {
            RefreshCache();
            return _cachedNames;
        }
    }

    public static Color[] RACER_COLORS
    {
        get
        {
            RefreshCache();
            return _cachedColors;
        }
    }

    /// <summary>
    /// 선발 변경 시 캐시 갱신 호출
    /// </summary>
    public static void InvalidateCache()
    {
        _cachedVersion = -1;
    }

    private static void RefreshCache()
    {
        var db = CharacterDatabase.Instance;
        if (db == null || db.SelectedCharacters.Count == 0)
        {
            // DB 없으면 기본값
            if (_cachedNames == null)
            {
                _cachedNames = new string[] {
                    "번개", "태풍", "혜성", "로켓",
                    "불꽃", "질풍", "천둥", "유성",
                    "폭풍", "섬광", "회오리", "울트라"
                };
                _cachedColors = new Color[] {
                    new Color(0.90f, 0.20f, 0.20f),
                    new Color(0.20f, 0.50f, 0.90f),
                    new Color(0.20f, 0.80f, 0.30f),
                    new Color(0.95f, 0.75f, 0.10f),
                    new Color(0.80f, 0.30f, 0.80f),
                    new Color(1.00f, 0.50f, 0.10f),
                    new Color(0.10f, 0.80f, 0.80f),
                    new Color(0.90f, 0.40f, 0.60f),
                    new Color(0.50f, 0.30f, 0.10f),
                    new Color(0.60f, 0.60f, 0.60f),
                    new Color(0.00f, 0.50f, 0.25f),
                    new Color(0.40f, 0.20f, 0.60f),
                };
            }
            return;
        }

        int ver = db.SelectionVersion;
        if (ver == _cachedVersion) return;

        var selected = db.SelectedCharacters;
        _cachedNames = new string[selected.Count];
        _cachedColors = new Color[selected.Count];

        for (int i = 0; i < selected.Count; i++)
        {
            _cachedNames[i] = selected[i].charName;
            _cachedColors[i] = selected[i].GetTypeColor();
        }

        _cachedVersion = ver;
        Debug.Log("[GameConstants] 캐시 갱신: " + selected.Count + "명");
    }

    // 이전 코드 호환
    public const int WAYPOINT_COUNT = 24;
    public const float TRACK_RADIUS_X = 5.8f;
    public const float TRACK_RADIUS_Y = 2.5f;
}