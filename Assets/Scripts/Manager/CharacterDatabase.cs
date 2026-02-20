using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 CSV 로드 + 전체 풀 관리 + 레이스 선발
/// Resources/Data/CharacterDB.csv 에서 로드
/// </summary>
public class CharacterDatabase : MonoBehaviour
{
    public static CharacterDatabase Instance { get; private set; }

    [Header("CSV 파일 (Resources 내 확장자 제외)")]
    public string csvPath = "Data/CharacterDB";

    private List<CharacterData> allCharacters = new List<CharacterData>();
    private List<CharacterData> selectedCharacters = new List<CharacterData>();

    /// <summary>전체 캐릭터 목록</summary>
    public List<CharacterData> AllCharacters => allCharacters;

    /// <summary>이번 레이스 선발 목록</summary>
    public List<CharacterData> SelectedCharacters => selectedCharacters;

    /// <summary>전체 캐릭터 수</summary>
    public int TotalCount => allCharacters.Count;

    /// <summary>선발 버전 (갱신 감지용)</summary>
    public int SelectionVersion { get; private set; } = 0;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        LoadCSV();
    }

    /// <summary>
    /// CSV 파일 로드
    /// </summary>
    public void LoadCSV()
    {
        allCharacters.Clear();

        TextAsset csv = Resources.Load<TextAsset>(csvPath);
        if (csv == null)
        {
            Debug.LogError("[CharacterDB] CSV 파일 없음: Resources/" + csvPath + ".csv");
            return;
        }

        string[] lines = csv.text.Split('\n');
        int loaded = 0;

        for (int i = 1; i < lines.Length; i++) // 0번 = 헤더 스킵
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            CharacterData data = CharacterData.ParseCSVLine(line);
            if (data != null)
            {
                allCharacters.Add(data);
                loaded++;
            }
            else
            {
                Debug.LogWarning("[CharacterDB] 파싱 실패 (line " + i + "): " + line);
            }
        }

        Debug.Log("[CharacterDB] 로드 완료: " + loaded + "명");
    }

    /// <summary>
    /// 전체 풀에서 count명 랜덤 선발
    /// </summary>
    public List<CharacterData> SelectRandom(int count)
    {
        selectedCharacters.Clear();

        if (allCharacters.Count == 0)
        {
            Debug.LogWarning("[CharacterDB] 캐릭터가 없습니다!");
            return selectedCharacters;
        }

        // 셔플 복사본
        List<CharacterData> pool = new List<CharacterData>(allCharacters);
        int n = pool.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
        }

        // 선발
        int pick = Mathf.Min(count, pool.Count);
        for (int i = 0; i < pick; i++)
        {
            selectedCharacters.Add(pool[i]);
        }

        Debug.Log("[CharacterDB] " + pool.Count + "명 중 " + pick + "명 선발: "
            + string.Join(", ", selectedCharacters.ConvertAll(c => c.DisplayName)));

        SelectionVersion++;
        GameConstants.InvalidateCache();

        return selectedCharacters;
    }

    /// <summary>
    /// 이름으로 캐릭터 검색
    /// </summary>
    public CharacterData FindByName(string name)
    {
        return allCharacters.Find(c => c.charName == name);
    }

    /// <summary>
    /// 타입별 캐릭터 목록
    /// </summary>
    public List<CharacterData> GetByType(CharacterType type)
    {
        return allCharacters.FindAll(c => c.charType == type);
    }
}