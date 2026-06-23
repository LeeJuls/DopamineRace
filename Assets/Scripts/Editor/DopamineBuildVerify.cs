#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 빌드 산출물 IL2CPP 마커 검증 (SPEC-045). 화이트리스트(반드시 존재) + 블랙리스트(Mono 잔재 부재).
/// DopamineBuilder.Run 빌드 직후 자동 호출 + 메뉴 수동 호출. CLI/배치에서 (bool, reason)로 게이트 가능.
/// </summary>
public static class DopamineBuildVerify
{
    [MenuItem("DopamineRace/Build/Verify Last Release Build")]
    public static void VerifyReleaseMenu()
    {
        if (Validate("Builds/Win64-IL2CPP-Release", out string reason))
            Debug.Log("[Verify] 통과 — IL2CPP 마커 OK");
        else
            Debug.LogError($"[Verify] 실패 — {reason}");
    }

    /// <summary>
    /// buildDir 검증: GameAssembly.dll·UnityPlayer.dll·exe·il2cpp_data 존재(=IL2CPP 빌드) +
    /// Assembly-CSharp.dll·MonoBleedingEdge 부재(=Mono 잔재 0).
    /// </summary>
    public static bool Validate(string buildDir, out string reason)
    {
        if (!Directory.Exists(buildDir)) { reason = $"빌드 폴더 없음: {buildDir}"; return false; }
        string dataDir = Path.Combine(buildDir, "DopamineRace_Data");

        // 화이트리스트 — 반드시 존재
        string[] must =
        {
            Path.Combine(buildDir, "GameAssembly.dll"),       // IL2CPP 네이티브
            Path.Combine(buildDir, "UnityPlayer.dll"),
            Path.Combine(buildDir, "DopamineRace.exe"),
            Path.Combine(dataDir, "il2cpp_data"),             // IL2CPP 메타데이터
        };
        foreach (string p in must)
            if (!File.Exists(p) && !Directory.Exists(p)) { reason = $"필수 누락(IL2CPP 빌드 아님?): {p}"; return false; }

        // 블랙리스트 — Mono 잔재(있으면 전환 실패 → 보안 L2 무효)
        string[] forbid =
        {
            Path.Combine(dataDir, "Managed", "Assembly-CSharp.dll"),
            Path.Combine(buildDir, "MonoBleedingEdge"),
        };
        foreach (string p in forbid)
            if (File.Exists(p) || Directory.Exists(p)) { reason = $"Mono 잔재 발견(전환 실패): {p}"; return false; }

        reason = "OK";
        return true;
    }
}
#endif
