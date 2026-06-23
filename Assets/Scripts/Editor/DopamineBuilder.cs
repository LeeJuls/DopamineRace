#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// DopamineRace 빌드 자동화 (SPEC-045). [MenuItem] + BuildPipeline.BuildPlayer.
/// 첫 IL2CPP 빌드는 MCP unity_build 금지(timeout→좀비) → 이 스크립트(에디터 메뉴 / batchmode)로 워밍.
///   batchmode: Unity.exe -batchmode -quit -projectPath &lt;path&gt; -executeMethod DopamineBuilder.CIRelease
/// </summary>
public static class DopamineBuilder
{
    const string OUT_DEV = "Builds/Win64-IL2CPP-Dev";
    const string OUT_REL = "Builds/Win64-IL2CPP-Release";
    const string EXE_NAME = "DopamineRace.exe";
    const string STEAM_CONTENT_KEY = "DRZ.SteamContentPath";        // EditorPrefs — 로컬별 경로 차이 흡수
    const string STEAM_CONTENT_DEFAULT = @"C:\steamcmd\content\DopamineRace";

    [MenuItem("DopamineRace/Build/Windows IL2CPP — Dev (Fast)")]
    public static void BuildDev() => Run(dev: true, copyToSteam: false);

    [MenuItem("DopamineRace/Build/Windows IL2CPP — Release")]
    public static void BuildRelease() => Run(dev: false, copyToSteam: false);

    [MenuItem("DopamineRace/Build/Release + Copy to Steam Content")]
    public static void BuildReleaseSteam() => Run(dev: false, copyToSteam: true);

    /// <summary>batchmode/CI 진입점 (MCP timeout 회피).</summary>
    public static void CIRelease() => Run(dev: false, copyToSteam: false);

    static void Run(bool dev, bool copyToSteam)
    {
        EnsureIL2CPPSettings(dev);

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

        string outDir = dev ? OUT_DEV : OUT_REL;
        if (Directory.Exists(outDir)) Directory.Delete(outDir, true);   // 이전 산출물 정리(혼입 방지)
        Directory.CreateDirectory(outDir);

        var opts = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = Path.Combine(outDir, EXE_NAME),
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = dev ? (BuildOptions.Development | BuildOptions.AllowDebugging) : BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[Build] 실패: {report.summary.result} (errors={report.summary.totalErrors})");
            return;
        }
        Debug.Log($"[Build] 성공: {outDir}  (소요 {report.summary.totalTime})");

        if (!DopamineBuildVerify.Validate(outDir, out string reason))
        {
            Debug.LogError($"[Build] 산출물 검증 실패 — {reason}");
            return;
        }
        Debug.Log("[Build] 산출물 검증 통과 (IL2CPP 마커 OK)");

        if (copyToSteam)
            CopyToSteamContent(outDir, EditorPrefs.GetString(STEAM_CONTENT_KEY, STEAM_CONTENT_DEFAULT));
    }

    /// <summary>빌드 직전 IL2CPP/stripping/engineCode 강제 — 인스펙터에서 누가 바꿔도 안전(보안 L2 보장).</summary>
    static void EnsureIL2CPPSettings(bool dev)
    {
        var nbt = NamedBuildTarget.Standalone;
        PlayerSettings.SetScriptingBackend(nbt, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(nbt, ManagedStrippingLevel.Disabled);  // 기록보관 직렬화 보호
        PlayerSettings.SetIl2CppCompilerConfiguration(nbt, dev ? Il2CppCompilerConfiguration.Debug : Il2CppCompilerConfiguration.Release);
        PlayerSettings.stripEngineCode = false;  // Cryptography/UnityWebRequest 백엔드 보호
    }

    /// <summary>산출물 미러 — DoNotShip/*.mac/*.pdb 제외. 검증 통과 후에만 호출(멱등: 대상 폴더 초기화).</summary>
    static void CopyToSteamContent(string buildDir, string steamContent)
    {
        if (Directory.Exists(steamContent)) Directory.Delete(steamContent, true);
        Directory.CreateDirectory(steamContent);

        int copied = 0;
        foreach (string src in Directory.GetFiles(buildDir, "*", SearchOption.AllDirectories))
        {
            string rel = src.Substring(buildDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (rel.IndexOf("BurstDebugInformation_DoNotShip", StringComparison.OrdinalIgnoreCase) >= 0) continue;
            if (rel.EndsWith(".mac", StringComparison.OrdinalIgnoreCase)) continue;
            if (rel.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)) continue;
            string dst = Path.Combine(steamContent, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dst));
            File.Copy(src, dst, true);
            copied++;
        }
        Debug.Log($"[Build] Steam content 복사 완료 → {steamContent} ({copied} files, DoNotShip/*.mac/*.pdb 제외)");
        Debug.LogWarning("[Build] steam_appid.txt 는 빌드 산출물에 없음 — steamcmd/vdf 가 관리. content 루트에 별도 배치 필요.");
    }
}
#endif
