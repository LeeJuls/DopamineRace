using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// V4 캐릭터 데이터베이스 — CharacterDB_V4.csv 로더
/// </summary>
public static class CharacterDatabaseV4
{
    private static List<CharacterDataV4> _db;

    public static CharacterDataV4 FindById(string charId)
    {
        if (_db == null) Load();
        return _db?.Find(c => c.charId == charId);
    }

    /// <summary>CSV 핫리로드용 — 다음 호출에서 재로드.</summary>
    public static void Reset()
    {
        _db = null;
    }

    private static void Load()
    {
        var csv = Resources.Load<TextAsset>("Data/CharacterDB_V4");
        if (csv == null)
        {
            Debug.LogWarning("[CharacterDatabaseV4] CharacterDB_V4.csv 로드 실패");
            _db = new List<CharacterDataV4>();
            return;
        }

        _db = new List<CharacterDataV4>();
        var lines = csv.text.Split('\n');
        for (int i = 1; i < lines.Length; i++) // row 0 = 헤더
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            var data = CharacterDataV4.ParseCSVLine(line);
            if (data != null) _db.Add(data);
        }

        Debug.Log($"[CharacterDatabaseV4] {_db.Count}개 캐릭터 로드 완료");
    }
}
