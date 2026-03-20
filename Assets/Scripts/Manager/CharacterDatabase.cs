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
        DontDestroyOnLoad(gameObject);
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
    /// 타입 균형 선발: 도주2 / 선행2 / 선입2 / 추입2 + 나머지1 (랜덤)
    /// 같은 타입 내에서는 charAppearanceRate 가중치 기반 확률 선택
    /// count가 9가 아닌 경우 폴백으로 단순 가중치 랜덤 사용
    /// </summary>
    public List<CharacterData> SelectRandom(int count)
    {
        selectedCharacters.Clear();

        if (allCharacters.Count == 0)
        {
            Debug.LogWarning("[CharacterDB] 캐릭터가 없습니다!");
            return selectedCharacters;
        }

        if (count == 9)
        {
            // ── 타입 균형 선발 (도주2 / 선행2 / 선입2 / 추입2 + 와일드1) ──
            var picked = new List<CharacterData>();

            PickWeighted(GetByType(CharacterType.Runner),   2, picked);
            PickWeighted(GetByType(CharacterType.Leader),   2, picked);
            PickWeighted(GetByType(CharacterType.Chaser),   2, picked);
            PickWeighted(GetByType(CharacterType.Reckoner), 2, picked);

            // 와일드카드 1명: 아직 선발되지 않은 전체 캐릭터 중 가중치 랜덤
            var remaining = new List<CharacterData>(allCharacters);
            foreach (var c in picked) remaining.Remove(c);
            PickWeighted(remaining, 1, picked);

            selectedCharacters.AddRange(picked);
        }
        else
        {
            // 폴백: 전체 풀 가중치 랜덤 (count가 9가 아닌 경우)
            var pool = new List<CharacterData>(allCharacters);
            PickWeighted(pool, Mathf.Min(count, pool.Count), selectedCharacters);
        }

        Debug.Log("[CharacterDB] " + allCharacters.Count + "명 중 " + selectedCharacters.Count + "명 선발: "
            + string.Join(", ", selectedCharacters.ConvertAll(c => c.DisplayName)));

        SelectionVersion++;
        GameConstants.InvalidateCache();

        return selectedCharacters;
    }

    /// <summary>
    /// pool에서 n명을 charAppearanceRate 가중치 기반으로 비복원 추출하여 result에 추가
    /// </summary>
    private void PickWeighted(List<CharacterData> pool, int n, List<CharacterData> result)
    {
        // 풀 복사 (비복원 추출을 위해)
        var available = new List<CharacterData>(pool);

        int pickCount = Mathf.Min(n, available.Count);
        for (int i = 0; i < pickCount; i++)
        {
            float totalWeight = 0f;
            foreach (var c in available) totalWeight += c.charAppearanceRate;

            float rnd = Random.Range(0f, totalWeight);
            float sum = 0f;
            CharacterData selected = available[available.Count - 1]; // 부동소수점 오차 방어용 폴백
            foreach (var c in available)
            {
                sum += c.charAppearanceRate;
                if (rnd < sum) { selected = c; break; }
            }

            result.Add(selected);
            available.Remove(selected);
        }
    }

    /// <summary>
    /// charId(UID)로 캐릭터 검색
    /// </summary>
    public CharacterData FindById(string charId)
    {
        return allCharacters.Find(c => c.charId == charId);
    }

    /// <summary>
    /// charName(Loc 키)으로 캐릭터 검색
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