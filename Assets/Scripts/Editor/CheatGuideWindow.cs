#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// DopamineRace > 치트 기능 보기
///
/// 플레이 모드 전용 디버그 단축키와 숨겨진 기능 목록.
/// 새 치트 기능 추가 시 이 파일에도 반드시 기재할 것.
/// </summary>
public class CheatGuideWindow : EditorWindow
{
    // ──────────────────────────────────────────────
    //  스타일 캐시
    // ──────────────────────────────────────────────
    private GUIStyle _styleHeader;
    private GUIStyle _styleSectionTitle;
    private GUIStyle _styleKeyBadge;
    private GUIStyle _styleDesc;
    private GUIStyle _styleNote;
    private GUIStyle _styleTableHeader;
    private GUIStyle _styleRow;
    private GUIStyle _styleRowAlt;
    private bool     _stylesReady;

    private Vector2 _scroll;

    // ──────────────────────────────────────────────
    //  메뉴
    // ──────────────────────────────────────────────
    [MenuItem("DopamineRace/치트 기능 보기 %#d")] // Ctrl+Shift+D
    public static void Open()
    {
        var win = GetWindow<CheatGuideWindow>(false, "DopamineRace 치트 가이드");
        win.minSize = new Vector2(680, 520);
        win.Show();
    }

    // ──────────────────────────────────────────────
    //  OnGUI
    // ──────────────────────────────────────────────
    private void OnGUI()
    {
        EnsureStyles();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        // ── 타이틀 ──
        GUILayout.Space(12);
        GUILayout.Label("🎮  DopamineRace  디버그 & 치트 가이드", _styleHeader);
        GUILayout.Label(
            "플레이 모드 전용 단축키 · 숨겨진 기능 목록\n" +
            "모든 기능은 #if UNITY_EDITOR 가드 — 릴리즈 빌드에 포함되지 않습니다.",
            _styleNote);
        GUILayout.Space(8);
        DrawHRule();

        // ══════════════════════════════════════════
        //  섹션 1: 결과창 미리보기
        // ══════════════════════════════════════════
        DrawSection("📊  결과창 미리보기 단축키", "SceneBootstrapper.Debug.cs");

        string[] resultKeys  = { "F8",  "F9",     "F10"  };
        string[] resultTypes = { "Win", "Exacta", "Trio" };
        string[] resultDescs =
        {
            "단승 배팅 — 1위 캐릭터 선택 → 무조건 적중",
            "연승 배팅 — 1·2위 캐릭터 선택 → 무조건 적중",
            "삼복승 배팅 — 1·2·3위 캐릭터 선택 → 무조건 적중",
        };

        BeginTable();
        DrawTableHeader("단축키", "배팅 타입", "동작");
        for (int i = 0; i < resultKeys.Length; i++)
            DrawTableRow(i, resultKeys[i], resultTypes[i], resultDescs[i]);
        EndTable();

        DrawNote("▸ 플레이 모드에서 언제든지 사용 가능 (배팅 화면 / 레이스 중 / 결과창 모두 가능)\n" +
                 "▸ 순위는 매번 랜덤으로 섞여 화살표(←) 위치가 다양하게 표시됨");

        GUILayout.Space(14);
        DrawHRule();

        // ══════════════════════════════════════════
        //  섹션 2: 프리팹 빌더 메뉴
        // ══════════════════════════════════════════
        DrawSection("🔧  Editor 메뉴 — 프리팹 관리", "DopamineRace 상단 메뉴");

        string[] menuItems =
        {
            "Create Betting UI Prefabs",
            "Patch Betting UI Prefabs (Safe)",
            "Create Result UI Prefabs",
            "Patch Result UI Prefabs (Safe)",
        };
        string[] menuDescs =
        {
            "배팅 패널 프리팹 완전 재생성 (기존 덮어쓰기, 최초 1회 사용)",
            "배팅 패널 프리팹 안전 패치 — 없는 요소만 추가, 기존 설정 유지",
            "결과 패널 프리팹 완전 재생성 (기존 덮어쓰기, 구조 변경 시 사용)",
            "결과 패널 프리팹 안전 패치 — 없는 요소만 추가, 기존 설정 유지",
        };
        string[] menuFiles =
        {
            "BettingUIPrefabCreator.cs",
            "BettingUIPrefabCreator.cs",
            "ResultUIPrefabCreator.cs",
            "ResultUIPrefabCreator.cs",
        };

        BeginTable();
        DrawTableHeader("메뉴 항목", "파일", "설명");
        for (int i = 0; i < menuItems.Length; i++)
            DrawTableRow(i, menuItems[i], menuFiles[i], menuDescs[i]);
        EndTable();

        DrawNote("▸ Patch는 기존 Inspector 수동 조정을 유지하므로 일반적인 업데이트에 권장\n" +
                 "▸ Create는 완전 재생성 — 수동 수정 내용이 모두 초기화됨");

        GUILayout.Space(14);
        DrawHRule();

        // ══════════════════════════════════════════
        //  섹션 3: 기타 숨겨진 기능
        // ══════════════════════════════════════════
        DrawSection("🕵️  기타 숨겨진 기능 / 설정", "여러 파일");

        DrawKeyRow("치트 가이드 열기",  "Ctrl+Shift+D", "이 창을 엽니다 (DopamineRace > 치트 기능 보기)");
        DrawKeyRow("MCP 재시작",       "수동",          "Tools > UnityCodeMcpServer > STDIO > Restart Server");
        DrawKeyRow("좀비 프로세스 제거", "수동",          "Docs/setup/mcp_kill_zombie.bat 실행 (새 세션 시작 전)");

        DrawNote("▸ SAVE_VERSION = 2 — 구버전 세이브 파일은 자동으로 삭제됨\n" +
                 "▸ hpSpeedCompress = 0.85 — 기본속도 격차 ~0.83% 압축 (GameConstants에서 조정)");

        GUILayout.Space(14);
        DrawHRule();

        // ══════════════════════════════════════════
        //  섹션 4: 추가 예정 / 빈 슬롯
        // ══════════════════════════════════════════
        DrawSection("➕  향후 추가될 치트 기능", "");

        EditorGUILayout.HelpBox(
            "새 치트 기능을 추가하면 여기에 항목을 기재하세요.\n\n" +
            "  [ 파일 ]  Assets/Scripts/Editor/CheatGuideWindow.cs\n" +
            "  [ 위치 ]  해당 섹션 DrawTableRow() 또는 DrawKeyRow() 추가\n" +
            "  [ 규칙 ]  #if UNITY_EDITOR 가드 필수 · 릴리즈 빌드 포함 금지",
            MessageType.Info);

        GUILayout.Space(20);

        EditorGUILayout.EndScrollView();
    }

    // ──────────────────────────────────────────────
    //  드로우 헬퍼
    // ──────────────────────────────────────────────
    private void DrawSection(string title, string sourceFile)
    {
        GUILayout.Space(10);
        GUILayout.Label(title, _styleSectionTitle);
        if (!string.IsNullOrEmpty(sourceFile))
        {
            EditorGUI.indentLevel++;
            GUILayout.Label("소스: " + sourceFile, _styleNote);
            EditorGUI.indentLevel--;
        }
        GUILayout.Space(4);
    }

    private void BeginTable()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
    }

    private void EndTable()
    {
        EditorGUILayout.EndVertical();
    }

    private void DrawTableHeader(string col1, string col2, string col3)
    {
        EditorGUILayout.BeginHorizontal(_styleTableHeader);
        GUILayout.Label(col1, _styleTableHeader, GUILayout.Width(140));
        GUILayout.Label(col2, _styleTableHeader, GUILayout.Width(130));
        GUILayout.Label(col3, _styleTableHeader);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTableRow(int idx, string col1, string col2, string col3)
    {
        GUIStyle rowStyle = (idx % 2 == 0) ? _styleRow : _styleRowAlt;
        EditorGUILayout.BeginHorizontal(rowStyle);
        GUILayout.Label(col1, _styleKeyBadge, GUILayout.Width(140));
        GUILayout.Label(col2, _styleDesc,     GUILayout.Width(130));
        GUILayout.Label(col3, _styleDesc);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawKeyRow(string label, string key, string desc)
    {
        EditorGUILayout.BeginHorizontal(_styleRow);
        GUILayout.Label(label, _styleDesc,     GUILayout.Width(180));
        GUILayout.Label(key,   _styleKeyBadge, GUILayout.Width(120));
        GUILayout.Label(desc,  _styleDesc);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawNote(string text)
    {
        GUILayout.Space(4);
        GUILayout.Label(text, _styleNote);
    }

    private void DrawHRule()
    {
        var rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
        GUILayout.Space(4);
    }

    // ──────────────────────────────────────────────
    //  스타일 초기화
    // ──────────────────────────────────────────────
    private void EnsureStyles()
    {
        if (_stylesReady) return;

        _styleHeader = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 17,
            alignment = TextAnchor.MiddleLeft,
        };
        _styleHeader.normal.textColor = new Color(0.9f, 0.85f, 0.3f);

        _styleSectionTitle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 13,
            alignment = TextAnchor.MiddleLeft,
        };
        _styleSectionTitle.normal.textColor = new Color(0.5f, 0.85f, 1f);

        _styleKeyBadge = new GUIStyle(EditorStyles.miniButtonMid)
        {
            fontSize  = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding   = new RectOffset(6, 6, 3, 3),
        };
        _styleKeyBadge.normal.textColor = new Color(0.2f, 1f, 0.6f);

        _styleDesc = new GUIStyle(EditorStyles.label)
        {
            fontSize  = 12,
            wordWrap  = true,
            alignment = TextAnchor.MiddleLeft,
        };

        _styleNote = new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = true,
            richText = true,
        };
        _styleNote.normal.textColor = new Color(0.65f, 0.65f, 0.65f);

        _styleTableHeader = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
        };
        _styleTableHeader.normal.textColor  = Color.white;
        _styleTableHeader.normal.background = MakeTex(1, 1, new Color(0.2f, 0.2f, 0.25f));

        _styleRow = new GUIStyle(GUIStyle.none)
        {
            padding = new RectOffset(4, 4, 3, 3),
        };

        _styleRowAlt = new GUIStyle(GUIStyle.none)
        {
            padding = new RectOffset(4, 4, 3, 3),
        };
        _styleRowAlt.normal.background = MakeTex(1, 1, new Color(1f, 1f, 1f, 0.04f));

        _stylesReady = true;
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        var tex = new Texture2D(w, h);
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
#endif
