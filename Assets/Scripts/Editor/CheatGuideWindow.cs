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
            "플레이 모드 단축키 · 숨겨진 기능 목록\n" +
            "⚠️  [Editor Only] = #if UNITY_EDITOR 릴리즈 빌드 제외  |  [항상 활성] = 릴리즈 빌드에도 포함됨",
            _styleNote);
        GUILayout.Space(8);
        DrawHRule();

        // ══════════════════════════════════════════
        //  섹션 1: 디버그 메뉴 + 결과창 미리보기  [Editor Only]
        // ══════════════════════════════════════════
        DrawSection("📊  디버그 메뉴 / 결과창 미리보기 단축키", "SceneBootstrapper.Debug.cs", editorOnly: true);

        BeginTable();
        DrawTableHeader("단축키", "대상", "동작");
        DrawTableRow(0, "F4",          "디버그 메뉴",  "번호 선택 메뉴 토글 (1~5번) / ESC 또는 F4로 닫기");
        DrawTableRow(1, "F4 → 1",      "Win",          "단승 결과창 강제 표시 → 무조건 적중");
        DrawTableRow(2, "F4 → 2",      "Exacta",       "연승 결과창 강제 표시 → 무조건 적중");
        DrawTableRow(3, "F4 → 3",      "Trio",         "삼복승 결과창 강제 표시 → 무조건 적중");
        DrawTableRow(4, "F4 → 4",      "Finish",       "최종 결산(Finish) 화면 강제 표시");
        DrawTableRow(5, "F4 → 5",      "Leaderboard",  "Top100 리더보드 팝업 강제 표시");
        DrawTableRow(6, "F8",          "Win",          "단승 배팅 — 1위 캐릭터 선택 → 무조건 적중 (빠른 접근)");
        DrawTableRow(7, "F9",          "Exacta",       "연승 배팅 — 1·2위 캐릭터 선택 → 무조건 적중 (빠른 접근)");
        DrawTableRow(8, "F10",         "Trio",         "삼복승 배팅 — 1·2·3위 캐릭터 선택 → 무조건 적중 (빠른 접근)");
        EndTable();

        DrawNote("▸ 플레이 모드에서 언제든지 사용 가능 (배팅 화면 / 레이스 중 / 결과창 모두)\n" +
                 "▸ F8/F9/F10 은 F4 메뉴 없이 바로 결과창으로 점프하는 빠른 접근 단축키");

        GUILayout.Space(14);
        DrawHRule();

        // ══════════════════════════════════════════
        //  섹션 2: 레이스 디버그 오버레이  [항상 활성]
        // ══════════════════════════════════════════
        DrawSection("🖥️  레이스 디버그 오버레이", "RaceDebugOverlay.cs", editorOnly: true);

        BeginTable();
        DrawTableHeader("단축키", "상태", "동작");
        DrawTableRow(0, "F1", "토글",   "디버그 오버레이 전체 표시 / 숨김");
        DrawTableRow(1, "F2", "토글",   "간략 모드 ↔ 상세 모드 전환");
        DrawTableRow(2, "F3", "순환",   "라운드별 이벤트 로그 순회 (R1→R2→...→현재→R1)");
        EndTable();

        DrawNote("▸ 레이스 진행 중 실시간 HP·CP·순위·이벤트 정보 확인\n" +
                 "▸ 릴리즈 빌드에도 포함되어 있으므로 출시 전 제거 여부 검토 필요");

        GUILayout.Space(14);
        DrawHRule();

        // ══════════════════════════════════════════
        //  섹션 3: 트랙 & 스폰 편집기  [항상 활성]
        // ══════════════════════════════════════════
        DrawSection("🗺️  트랙 & 스폰 위치 편집기", "WaypointEditor.cs / SpawnEditor.cs / TrackDebugPath.cs", editorOnly: true);

        BeginTable();
        DrawTableHeader("단축키", "편집 대상", "동작");
        DrawTableRow(0, "E",        "웨이포인트",  "WaypointEditor — 트랙 경로 편집 모드 ON/OFF");
        DrawTableRow(1, "R",        "스폰 위치",   "SpawnEditor — 캐릭터 출발 위치 편집 모드 ON/OFF");
        DrawTableRow(2, "S",        "저장",        "편집 모드 중 → 변경사항을 JSON 파일로 저장\n(track_waypoints.json / spawn_positions.json)");
        DrawTableRow(3, "D",        "경로 표시",   "TrackDebugPath — 트랙 웨이포인트 연결선 표시 ON/OFF");
        EndTable();

        DrawNote("▸ E / R 키로 편집 모드를 켠 뒤 마우스로 포인트를 드래그하여 수정\n" +
                 "▸ S 키로 저장하면 Assets/Resources/Data/ 아래 JSON 파일이 갱신됨\n" +
                 "▸ ⚠️ 릴리즈 빌드에도 키 리스너가 살아 있음 — 출시 전 #if UNITY_EDITOR 처리 권장");

        GUILayout.Space(14);
        DrawHRule();

        // ══════════════════════════════════════════
        //  섹션 4: Editor 메뉴 — 프리팹 관리
        // ══════════════════════════════════════════
        DrawSection("🔧  Editor 메뉴 — 프리팹 관리", "DopamineRace 상단 메뉴", editorOnly: true);

        BeginTable();
        DrawTableHeader("메뉴 항목", "파일", "설명");
        DrawTableRow(0, "Create Betting UI Prefabs",       "BettingUIPrefabCreator.cs",  "배팅 패널 프리팹 완전 재생성 (기존 수동 수정 초기화됨)");
        DrawTableRow(1, "Patch Betting UI Prefabs (Safe)", "BettingUIPrefabCreator.cs",  "배팅 패널 안전 패치 — 없는 요소만 추가, 기존 설정 유지");
        DrawTableRow(2, "Create Result UI Prefabs",        "ResultUIPrefabCreator.cs",   "결과 패널 프리팹 완전 재생성 (구조 변경 시 사용)");
        DrawTableRow(3, "Patch Result UI Prefabs (Safe)",  "ResultUIPrefabCreator.cs",   "결과 패널 안전 패치 — 없는 요소만 추가, 기존 설정 유지");
        EndTable();

        DrawNote("▸ 일반적인 업데이트는 Patch 사용 권장 (Inspector 조정 유지)\n" +
                 "▸ Create는 완전 재생성 — 수동 Inspector 수정 내용 모두 초기화됨");

        GUILayout.Space(14);
        DrawHRule();

        // ══════════════════════════════════════════
        //  섹션 5: 기타 숨겨진 기능 / 설정값
        // ══════════════════════════════════════════
        DrawSection("🕵️  기타 기능 & 주요 설정값", "여러 파일", editorOnly: true);

        DrawKeyRow("치트 가이드 열기",  "Ctrl+Shift+D", "이 창을 엽니다 (DopamineRace > 치트 기능 보기)");
        DrawKeyRow("MCP 재시작",        "수동",          "Tools > UnityCodeMcpServer > STDIO > Restart Server");
        DrawKeyRow("좀비 프로세스 제거", "수동",          "Docs/setup/mcp_kill_zombie.bat 실행 (새 세션 시작 전)");

        DrawNote("▸ SAVE_VERSION = 2 — 구버전 세이브 파일은 자동 삭제됨\n" +
                 "▸ V4 레이스 시스템 활성 (5대 스탯: Speed/Accel/Stamina/Power/Intelligence + Luck)");

        GUILayout.Space(14);
        DrawHRule();

        // ══════════════════════════════════════════
        //  섹션 6: 향후 추가될 치트 기능
        // ══════════════════════════════════════════
        DrawSection("➕  향후 추가될 치트 기능", "", editorOnly: false);

        EditorGUILayout.HelpBox(
            "새 치트 기능을 추가하면 여기에 항목을 기재하세요.\n\n" +
            "  [ 파일 ]  Assets/Scripts/Editor/CheatGuideWindow.cs\n" +
            "  [ 위치 ]  해당 섹션의 DrawTableRow() 또는 DrawKeyRow() 한 줄 추가\n" +
            "  [ 규칙 ]  릴리즈 빌드 포함 여부를 editorOnly 파라미터로 명시",
            MessageType.Info);

        GUILayout.Space(20);

        EditorGUILayout.EndScrollView();
    }

    // ──────────────────────────────────────────────
    //  드로우 헬퍼
    // ──────────────────────────────────────────────
    private void DrawSection(string title, string sourceFile, bool editorOnly)
    {
        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(title, _styleSectionTitle);
        string badge = editorOnly ? "[Editor Only]" : "[항상 활성]";
        Color  badgeColor = editorOnly
            ? new Color(0.4f, 0.8f, 1f)
            : new Color(1f, 0.6f, 0.3f);
        var badgeStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
        };
        badgeStyle.normal.textColor = badgeColor;
        GUILayout.Label(badge, badgeStyle, GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

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
