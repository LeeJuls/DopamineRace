#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// GameSettings Inspector 커스텀 에디터
/// 각 섹션별 "기본값 복원" 버튼 제공
/// </summary>
[CustomEditor(typeof(GameSettings))]
public class GameSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 Inspector 표시
        DrawDefaultInspector();

        GameSettings gs = (GameSettings)target;

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("═══ 기본값 복원 ═══", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "각 섹션의 설정을 기본값으로 되돌립니다.\n변경 후 Ctrl+Z로 되돌릴 수 있습니다.",
            MessageType.Info);

        EditorGUILayout.Space(5);

        // ── 레이스 기본 공식 ──
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("레이스 기본 공식", EditorStyles.boldLabel);
        if (GUILayout.Button("기본값 복원", GUILayout.Width(100)))
        {
            Undo.RecordObject(gs, "Reset Race Formula");
            gs.globalSpeedMultiplier = 2.5f;
            gs.noiseFactor = 0.1f;
            gs.fatigueFactor = 0.15f;
            gs.raceSpeedLerp = 3f;
            EditorUtility.SetDirty(gs);
            Debug.Log("✅ 레이스 기본 공식 → 기본값 복원");
        }
        EditorGUILayout.EndHorizontal();

        // ── 타입 보너스 (전반) ──
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("타입 보너스 (전반)", EditorStyles.boldLabel);
        if (GUILayout.Button("기본값 복원", GUILayout.Width(100)))
        {
            Undo.RecordObject(gs, "Reset Early Bonus");
            gs.earlyBonus_Runner = 0.15f;
            gs.earlyBonus_Leader = 0.08f;
            gs.earlyBonus_Chaser = 0f;
            gs.earlyBonus_Reckoner = -0.05f;
            EditorUtility.SetDirty(gs);
            Debug.Log("✅ 타입 보너스 (전반) → 기본값 복원");
        }
        EditorGUILayout.EndHorizontal();

        // ── 타입 보너스 (중반) ──
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("타입 보너스 (중반)", EditorStyles.boldLabel);
        if (GUILayout.Button("기본값 복원", GUILayout.Width(100)))
        {
            Undo.RecordObject(gs, "Reset Mid Bonus");
            gs.midBonus_Runner = -0.05f;
            gs.midBonus_Leader = 0.05f;
            gs.midBonus_Chaser = 0.10f;
            gs.midBonus_Reckoner = 0.03f;
            EditorUtility.SetDirty(gs);
            Debug.Log("✅ 타입 보너스 (중반) → 기본값 복원");
        }
        EditorGUILayout.EndHorizontal();

        // ── 타입 보너스 (후반) ──
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("타입 보너스 (후반)", EditorStyles.boldLabel);
        if (GUILayout.Button("기본값 복원", GUILayout.Width(100)))
        {
            Undo.RecordObject(gs, "Reset Late Bonus");
            gs.lateBonus_Runner = -0.10f;
            gs.lateBonus_Leader = 0f;
            gs.lateBonus_Chaser = 0.05f;
            gs.lateBonus_Reckoner = 0.18f;
            EditorUtility.SetDirty(gs);
            Debug.Log("✅ 타입 보너스 (후반) → 기본값 복원");
        }
        EditorGUILayout.EndHorizontal();

        // ── 타입 보너스 전체 ──
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("  ↳ 타입 보너스 (전체)", EditorStyles.miniLabel);
        if (GUILayout.Button("전체 복원", GUILayout.Width(100)))
        {
            Undo.RecordObject(gs, "Reset All Type Bonus");
            gs.earlyBonus_Runner = 0.15f;  gs.earlyBonus_Leader = 0.08f;
            gs.earlyBonus_Chaser = 0f;     gs.earlyBonus_Reckoner = -0.05f;
            gs.midBonus_Runner = -0.05f;   gs.midBonus_Leader = 0.05f;
            gs.midBonus_Chaser = 0.10f;    gs.midBonus_Reckoner = 0.03f;
            gs.lateBonus_Runner = -0.10f;  gs.lateBonus_Leader = 0f;
            gs.lateBonus_Chaser = 0.05f;   gs.lateBonus_Reckoner = 0.18f;
            EditorUtility.SetDirty(gs);
            Debug.Log("✅ 타입 보너스 (전체 12개) → 기본값 복원");
        }
        EditorGUILayout.EndHorizontal();

        // ── 충돌 시스템 ──
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("충돌 시스템", EditorStyles.boldLabel);
        if (GUILayout.Button("기본값 복원", GUILayout.Width(100)))
        {
            Undo.RecordObject(gs, "Reset Collision");
            gs.enableCollision = true;
            gs.enableCollisionVFX = true;
            gs.collisionChance = 0.6f;
            gs.shakeMagnitude = 0.05f;
            gs.shakeWinnerDuration = 0.15f;
            gs.shakeLoserDuration = 0.25f;
            gs.collisionRange = 0.8f;
            gs.collisionCooldown = 2.0f;
            gs.collisionBasePenalty = 0.3f;
            gs.winnerPenaltyDuration = 0.3f;
            gs.loserPenaltyDuration = 0.5f;
            gs.crowdThreshold = 3;
            gs.crowdDampen = 0.5f;
            EditorUtility.SetDirty(gs);
            Debug.Log("✅ 충돌 시스템 → 기본값 복원");
        }
        EditorGUILayout.EndHorizontal();

        // ── 슬링샷 ──
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("슬링샷", EditorStyles.boldLabel);
        if (GUILayout.Button("기본값 복원", GUILayout.Width(100)))
        {
            Undo.RecordObject(gs, "Reset Slingshot");
            gs.slingshotFactor = 0.02f;
            gs.slingshotDuration = 1.0f;
            gs.slingshotMaxBoost = 0.4f;
            EditorUtility.SetDirty(gs);
            Debug.Log("✅ 슬링샷 → 기본값 복원");
        }
        EditorGUILayout.EndHorizontal();

        // ── 운 (Luck) ──
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("운 (Luck)", EditorStyles.boldLabel);
        if (GUILayout.Button("기본값 복원", GUILayout.Width(100)))
        {
            Undo.RecordObject(gs, "Reset Luck");
            gs.luckCheckInterval = 3.0f;
            gs.luckCritChance = 0.005f;
            gs.luckCritBoost = 1.3f;
            gs.luckCritDuration = 1.5f;
            gs.luckDodgeChance = 0.02f;
            EditorUtility.SetDirty(gs);
            Debug.Log("✅ 운 (Luck) → 기본값 복원");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // ── 트랙 설정 ──
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("트랙 설정", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("트랙 설정 리셋"))
        {
            Undo.RecordObject(gs, "Reset Track Settings");
            gs.randomTrackPerRound = false;
            gs.enableTrackTransition = true;
            gs.trackTransitionFadeDuration = 0.3f;
            EditorUtility.SetDirty(gs);
            Debug.Log("✅ 트랙 설정 → 기본값 복원");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // ── 레이스 공식 전체 리셋 ──
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("⚠️ 레이스 공식 전체 기본값 복원", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("전체 복원",
                "레이스 기본 공식, 타입 보너스, 충돌, 슬링샷, Luck\n모든 값을 기본값으로 되돌립니다.\n\n계속하시겠습니까?",
                "복원", "취소"))
            {
                Undo.RecordObject(gs, "Reset All Race Settings");

                // 레이스 기본
                gs.globalSpeedMultiplier = 2.5f;
                gs.noiseFactor = 0.1f;
                gs.fatigueFactor = 0.15f;
                gs.raceSpeedLerp = 3f;

                // 타입 보너스
                gs.earlyBonus_Runner = 0.15f;  gs.earlyBonus_Leader = 0.08f;
                gs.earlyBonus_Chaser = 0f;     gs.earlyBonus_Reckoner = -0.05f;
                gs.midBonus_Runner = -0.05f;   gs.midBonus_Leader = 0.05f;
                gs.midBonus_Chaser = 0.10f;    gs.midBonus_Reckoner = 0.03f;
                gs.lateBonus_Runner = -0.10f;  gs.lateBonus_Leader = 0f;
                gs.lateBonus_Chaser = 0.05f;   gs.lateBonus_Reckoner = 0.18f;

                // 충돌
                gs.enableCollision = true;
                gs.enableCollisionVFX = true;
                gs.collisionChance = 0.6f;
                gs.shakeMagnitude = 0.05f;
                gs.shakeWinnerDuration = 0.15f;
                gs.shakeLoserDuration = 0.25f;
                gs.collisionRange = 0.8f;
                gs.collisionCooldown = 2.0f;
                gs.collisionBasePenalty = 0.3f;
                gs.winnerPenaltyDuration = 0.3f;
                gs.loserPenaltyDuration = 0.5f;
                gs.crowdThreshold = 3;
                gs.crowdDampen = 0.5f;

                // 슬링샷
                gs.slingshotFactor = 0.02f;
                gs.slingshotDuration = 1.0f;
                gs.slingshotMaxBoost = 0.4f;

                // Luck
                gs.luckCheckInterval = 3.0f;
                gs.luckCritChance = 0.005f;
                gs.luckCritBoost = 1.3f;
                gs.luckCritDuration = 1.5f;
                gs.luckDodgeChance = 0.02f;

                // 트랙
                gs.randomTrackPerRound = false;
                gs.enableTrackTransition = true;
                gs.trackTransitionFadeDuration = 0.3f;

                EditorUtility.SetDirty(gs);
                Debug.Log("✅ 레이스 공식 전체 → 기본값 복원 완료!");
            }
        }
        GUI.backgroundColor = Color.white;
    }
}
#endif