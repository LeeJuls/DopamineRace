using UnityEditor;
using UnityEngine;

public static class EnterPlayModeSetup
{
    [MenuItem("DopamineRace/Enable Fast Enter Play Mode")]
    static void Enable()
    {
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        // DisableSceneReload는 추가하지 않음 (SceneBootstrapper가 씬 로드마다 UI 재구성)
        Debug.Log("[Setup] Fast Enter Play Mode enabled — DisableDomainReload only");
    }
}
