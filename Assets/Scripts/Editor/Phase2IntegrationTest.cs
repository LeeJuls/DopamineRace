#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Phase 2 캐릭터 데이터 연동 테스트
/// Unity 메뉴: Tools > Phase2 연동 테스트
/// Play 모드에서 실행해야 합니다.
/// </summary>
public class Phase2IntegrationTest : EditorWindow
{
    private Vector2 scrollPos;
    private string resultText = "";
    private int passCount = 0;
    private int failCount = 0;

    [MenuItem("Tools/Phase2 연동 테스트")]
    static void ShowWindow()
    {
        GetWindow<Phase2IntegrationTest>("Phase2 테스트");
    }

    private void OnGUI()
    {
        GUILayout.Label("Phase 2 캐릭터 데이터 연동 테스트", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Play 모드에서 실행하세요!", MessageType.Info);
        EditorGUILayout.Space();

        if (GUILayout.Button("전체 테스트 실행", GUILayout.Height(35)))
        {
            RunAllTests();
        }

        EditorGUILayout.Space();

        if (!string.IsNullOrEmpty(resultText))
        {
            Color c = GUI.color;
            GUI.color = failCount == 0 ? Color.green : Color.red;
            GUILayout.Label("결과: " + passCount + " PASS / " + failCount + " FAIL", EditorStyles.boldLabel);
            GUI.color = c;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(500));
            EditorGUILayout.TextArea(resultText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }

    private void RunAllTests()
    {
        resultText = "";
        passCount = 0;
        failCount = 0;

        if (!Application.isPlaying)
        {
            resultText = "⚠️ Play 모드에서 실행해주세요!";
            return;
        }

        Log("═══════════════════════════════════════");
        Log(" Phase 2 캐릭터 데이터 연동 테스트");
        Log("═══════════════════════════════════════\n");

        // 1. CSV 로드
        Test_CSVLoad();

        // 2. CharacterDatabase 싱글톤
        Test_DatabaseSingleton();

        // 3. 캐릭터 선발
        Test_SelectRandom();

        // 4. GameConstants 연동
        Test_GameConstants();

        // 5. GameManager 연동
        Test_GameManager();

        // 6. RaceManager 연동
        Test_RaceManager();

        // 7. 이름/색상 일치
        Test_NameColorMatch();

        // 8. 프리팹 경로
        Test_PrefabPaths();

        // 9. 아이콘 경로
        Test_IconPaths();

        // 10. 선발 랜덤성
        Test_RandomSelection();

        Log("\n═══════════════════════════════════════");
        Log(" 최종: " + passCount + " PASS / " + failCount + " FAIL");
        Log("═══════════════════════════════════════");

        Debug.Log(resultText);
    }

    // ── 1. CSV 로드 ──
    private void Test_CSVLoad()
    {
        Log("\n[테스트 1] CSV 로드");

        TextAsset csv = Resources.Load<TextAsset>("Data/CharacterDB");
        Assert(csv != null, "CSV 파일 존재 (Resources/Data/CharacterDB)");

        if (csv != null)
        {
            string[] lines = csv.text.Split('\n');
            int dataLines = 0;
            for (int i = 1; i < lines.Length; i++)
                if (!string.IsNullOrEmpty(lines[i].Trim())) dataLines++;

            Assert(dataLines > 0, "CSV 데이터 행 존재 (" + dataLines + "행)");

            // 헤더 확인
            string header = lines[0].Trim();
            Assert(header.Contains("char_name"), "CSV 헤더에 char_name 포함");
            Assert(header.Contains("char_type"), "CSV 헤더에 char_type 포함");
            Assert(header.Contains("char_icon"), "CSV 헤더에 char_icon 포함");

            // 첫 번째 데이터 파싱
            CharacterData first = CharacterData.ParseCSVLine(lines[1].Trim());
            Assert(first != null, "첫 번째 행 파싱 성공");
            if (first != null)
            {
                Assert(!string.IsNullOrEmpty(first.charName), "이름 비어있지 않음: " + first.charName);
                Assert(first.charBaseSpeed > 0, "속도 > 0: " + first.charBaseSpeed);
            }
        }
    }

    // ── 2. CharacterDatabase 싱글톤 ──
    private void Test_DatabaseSingleton()
    {
        Log("\n[테스트 2] CharacterDatabase 싱글톤");

        var db = CharacterDatabase.Instance;
        Assert(db != null, "CharacterDatabase.Instance 존재");

        if (db != null)
        {
            Assert(db.AllCharacters != null, "AllCharacters 리스트 존재");
            Assert(db.AllCharacters.Count > 0, "AllCharacters 로드됨 (" + db.AllCharacters.Count + "명)");
        }
    }

    // ── 3. 캐릭터 선발 ──
    private void Test_SelectRandom()
    {
        Log("\n[테스트 3] 캐릭터 선발");

        var db = CharacterDatabase.Instance;
        if (db == null) { Fail("DB 없음, 스킵"); return; }

        Assert(db.SelectedCharacters != null, "SelectedCharacters 리스트 존재");
        Assert(db.SelectedCharacters.Count > 0, "선발된 캐릭터 존재 (" + db.SelectedCharacters.Count + "명)");

        int expected = Mathf.Min(GameSettings.Instance.racerCount, db.AllCharacters.Count);
        Assert(db.SelectedCharacters.Count == expected,
            "선발 수 일치: 기대 " + expected + " / 실제 " + db.SelectedCharacters.Count);

        // 중복 체크
        HashSet<string> names = new HashSet<string>();
        bool hasDuplicate = false;
        foreach (var c in db.SelectedCharacters)
        {
            if (!names.Add(c.charName)) hasDuplicate = true;
        }
        Assert(!hasDuplicate, "선발 캐릭터 중복 없음");
    }

    // ── 4. GameConstants 연동 ──
    private void Test_GameConstants()
    {
        Log("\n[테스트 4] GameConstants 연동");

        var db = CharacterDatabase.Instance;
        if (db == null || db.SelectedCharacters.Count == 0) { Fail("DB/선발 없음, 스킵"); return; }

        Assert(GameConstants.RACER_NAMES != null, "RACER_NAMES not null");
        Assert(GameConstants.RACER_COLORS != null, "RACER_COLORS not null");
        Assert(GameConstants.RACER_COUNT == db.SelectedCharacters.Count,
            "RACER_COUNT 일치: " + GameConstants.RACER_COUNT + " == " + db.SelectedCharacters.Count);
        Assert(GameConstants.RACER_NAMES.Length == db.SelectedCharacters.Count,
            "RACER_NAMES 길이 일치: " + GameConstants.RACER_NAMES.Length);
        Assert(GameConstants.RACER_COLORS.Length == db.SelectedCharacters.Count,
            "RACER_COLORS 길이 일치: " + GameConstants.RACER_COLORS.Length);

        // 이름 매칭
        for (int i = 0; i < db.SelectedCharacters.Count; i++)
        {
            Assert(GameConstants.RACER_NAMES[i] == db.SelectedCharacters[i].charName,
                "이름[" + i + "] 일치: " + GameConstants.RACER_NAMES[i]);
        }
    }

    // ── 5. GameManager 연동 ──
    private void Test_GameManager()
    {
        Log("\n[테스트 5] GameManager 연동");

        var gm = GameManager.Instance;
        Assert(gm != null, "GameManager.Instance 존재");

        if (gm != null)
        {
            Assert(gm.CurrentState == GameManager.GameState.Betting, "초기 상태 = Betting");
            Assert(gm.CurrentRound == 1, "CurrentRound = 1");
            Assert(gm.CurrentBet != null, "CurrentBet 존재");
        }
    }

    // ── 6. RaceManager 연동 ──
    private void Test_RaceManager()
    {
        Log("\n[테스트 6] RaceManager 연동");

        var rm = RaceManager.Instance;
        Assert(rm != null, "RaceManager.Instance 존재");

        if (rm != null)
        {
            Assert(rm.Racers != null, "Racers 리스트 존재");
            Assert(rm.Racers.Count == GameConstants.RACER_COUNT,
                "Racer 수 일치: " + rm.Racers.Count + " == " + GameConstants.RACER_COUNT);

            // 레이서 이름 확인
            for (int i = 0; i < rm.Racers.Count; i++)
            {
                string expected = "Racer_" + GameConstants.RACER_NAMES[i];
                Assert(rm.Racers[i].gameObject.name == expected,
                    "Racer[" + i + "] 이름: " + rm.Racers[i].gameObject.name);
            }
        }
    }

    // ── 7. 이름/색상 타입 매칭 ──
    private void Test_NameColorMatch()
    {
        Log("\n[테스트 7] 이름/색상 타입 매칭");

        var db = CharacterDatabase.Instance;
        if (db == null) { Fail("DB 없음, 스킵"); return; }

        foreach (var c in db.SelectedCharacters)
        {
            Color typeColor = c.GetTypeColor();
            string typeName = c.GetTypeName();
            Assert(!string.IsNullOrEmpty(typeName),
                c.charName + " 타입: " + typeName + " (" + c.charType + ")");
            Assert(typeColor != Color.clear,
                c.charName + " 색상: R" + typeColor.r.ToString("F1")
                + " G" + typeColor.g.ToString("F1")
                + " B" + typeColor.b.ToString("F1"));
        }
    }

    // ── 8. 프리팹 경로 ──
    private void Test_PrefabPaths()
    {
        Log("\n[테스트 8] 프리팹 경로");

        var db = CharacterDatabase.Instance;
        if (db == null) { Fail("DB 없음, 스킵"); return; }

        foreach (var c in db.AllCharacters)
        {
            if (string.IsNullOrEmpty(c.charResourcePrefabs))
            {
                Log("  ⚠ " + c.charName + ": 프리팹 경로 비어있음 (허용)");
                continue;
            }

            GameObject prefab = c.LoadPrefab();
            if (prefab != null)
                Pass(c.charName + " 프리팹 로드 성공");
            else
                Log("  ⚠ " + c.charName + " 프리팹 미발견 (경로: " + c.charResourcePrefabs + ") — 프리팹 미배치면 정상");
        }
    }

    // ── 9. 아이콘 경로 ──
    private void Test_IconPaths()
    {
        Log("\n[테스트 9] 아이콘 경로");

        var db = CharacterDatabase.Instance;
        if (db == null) { Fail("DB 없음, 스킵"); return; }

        foreach (var c in db.AllCharacters)
        {
            if (string.IsNullOrEmpty(c.charIcon))
            {
                Log("  ⚠ " + c.charName + ": 아이콘 경로 비어있음 (허용)");
                continue;
            }

            Sprite icon = c.LoadIcon();
            if (icon != null)
                Pass(c.charName + " 아이콘 로드 성공 (128x128)");
            else
                Log("  ⚠ " + c.charName + " 아이콘 미발견 (경로: " + c.charIcon + ") — 아이콘 미배치면 정상");
        }
    }

    // ── 10. 선발 랜덤성 ──
    private void Test_RandomSelection()
    {
        Log("\n[테스트 10] 선발 랜덤성 (10회 재선발)");

        var db = CharacterDatabase.Instance;
        if (db == null || db.AllCharacters.Count <= GameSettings.Instance.racerCount)
        {
            Log("  ⚠ 전체 캐릭터 수 ≤ 선발 수 → 랜덤성 테스트 불가 (항상 전원 선발)");
            Log("  ⚠ 캐릭터를 추가하면 이 테스트가 의미를 갖습니다");
            return;
        }

        HashSet<string> orders = new HashSet<string>();
        for (int t = 0; t < 10; t++)
        {
            db.SelectRandom(GameSettings.Instance.racerCount);
            string order = string.Join(",", db.SelectedCharacters.ConvertAll(c => c.charName));
            orders.Add(order);
        }

        Assert(orders.Count > 1, "10회 선발 중 서로 다른 조합: " + orders.Count + "가지");

        // 원래 선발로 복원
        db.SelectRandom(GameSettings.Instance.racerCount);
    }

    // ── 유틸리티 ──
    private void Assert(bool condition, string message)
    {
        if (condition)
            Pass(message);
        else
            Fail(message);
    }

    private void Pass(string message)
    {
        passCount++;
        Log("  ✅ PASS: " + message);
    }

    private void Fail(string message)
    {
        failCount++;
        Log("  ❌ FAIL: " + message);
    }

    private void Log(string msg)
    {
        resultText += msg + "\n";
    }
}
#endif
