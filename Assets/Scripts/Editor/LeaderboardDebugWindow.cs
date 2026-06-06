using UnityEngine;
using UnityEditor;

/// <summary>
/// [임시] STEP7 리더보드 저장 진단용 디버그 윈도우.
/// - leaderboard.json FilePath + 엔트리 표 표시
/// - Play 중 RoundHistory.Count 실시간 표시 (저장 가드 진단)
/// - 강제 Finish 저장 / 새로고침 / 초기화
/// 검증 완료 후 제거 가능 (오너 판단). 메뉴: DopamineRace > [임시] 리더보드 디버그
/// </summary>
public class LeaderboardDebugWindow : EditorWindow
{
    private Vector2 scroll;

    [MenuItem("DopamineRace/[임시] 리더보드 디버그")]
    public static void ShowWindow()
    {
        GetWindow<LeaderboardDebugWindow>("리더보드 디버그");
    }

    // Play 중 RoundHistory.Count 실시간 갱신
    private void OnInspectorUpdate() => Repaint();

    private void OnGUI()
    {
        EditorGUILayout.LabelField("[임시] 리더보드 저장 진단 (STEP7)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // ── FilePath + 파일 존재 ──
        string fp = System.IO.Path.Combine(Application.persistentDataPath, "leaderboard.json");
        EditorGUILayout.LabelField("FilePath", EditorStyles.miniBoldLabel);
        EditorGUILayout.SelectableLabel(fp, EditorStyles.textField, GUILayout.Height(18));
        bool exists = System.IO.File.Exists(fp);
        EditorGUILayout.LabelField("파일 존재", exists ? "O" : "X (아직 저장 안 됨)");
        EditorGUILayout.Space();

        // ── Play 중 RoundHistory.Count 실시간 ──
        if (Application.isPlaying)
        {
            var sm = ScoreManager.Instance;
            int rhCount = sm != null ? sm.RoundHistory.Count : -1;
            int score = sm != null ? sm.CurrentGameScore : 0;
            EditorGUILayout.HelpBox(
                $"[Play] RoundHistory.Count = {rhCount}   CurrentGameScore = {score}\n" +
                (rhCount == 0
                    ? "⚠ Count=0 → SaveToLeaderboard 가드에 막혀 저장 스킵됨!"
                    : "Count>0 → 저장 가능 상태"),
                rhCount == 0 ? MessageType.Warning : MessageType.Info);

            var gm = GameManager.Instance;
            if (gm != null)
                EditorGUILayout.LabelField(
                    $"상태={gm.CurrentState}  Round={gm.CurrentRound}/{gm.TotalRounds}  IsLastRound={gm.IsLastRound}");
        }
        else
        {
            EditorGUILayout.HelpBox("Play 모드 아님 — RoundHistory는 Play 중에만 표시", MessageType.None);
        }
        EditorGUILayout.Space();

        // ── 버튼 ──
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("새로고침")) Repaint();

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("강제 Finish 저장"))
            {
                var gm = GameManager.Instance;
                if (gm != null) gm.ChangeState(GameManager.GameState.Finish);
            }
            GUI.enabled = true;

            if (GUILayout.Button("리더보드 초기화"))
            {
                if (EditorUtility.DisplayDialog("확인", "leaderboard.json 전체 삭제?", "삭제", "취소"))
                    LeaderboardData.Clear();
            }
        }
        EditorGUILayout.Space();

        // ── 엔트리 표 ──
        var entries = LeaderboardData.GetTop(100);
        EditorGUILayout.LabelField($"엔트리 수: {entries.Count}", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("#", GUILayout.Width(28));
            GUILayout.Label("점수", GUILayout.Width(64));
            GUILayout.Label("R", GUILayout.Width(24));
            GUILayout.Label("날짜", GUILayout.Width(120));
            GUILayout.Label("요약");
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label((i + 1).ToString(), GUILayout.Width(28));
                GUILayout.Label(e.score.ToString(), GUILayout.Width(64));
                GUILayout.Label(e.rounds.ToString(), GUILayout.Width(24));
                GUILayout.Label(e.date, GUILayout.Width(120));
                GUILayout.Label(e.summary);
            }
        }
        EditorGUILayout.EndScrollView();
    }
}
