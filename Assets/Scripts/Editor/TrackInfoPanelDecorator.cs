using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BettingPanel.prefab 내 TrackInfoPanel에만 국소 수정을 적용.
/// - 배경: 흰색
/// - 텍스트: 어두운 계열 (흰 배경 가독성)
/// - WaterDropDecor 컴포넌트 추가 (dropCount=8, sizeMin=5, sizeMax=12)
///
/// 전체 프리팹 재생성 없이 TrackInfoPanel 노드만 건드림.
/// </summary>
public static class TrackInfoPanelDecorator
{
    private const string PrefabPath = "Assets/Prefabs/UI/BettingPanel.prefab";

    [MenuItem("DopamineRace/Decorate TrackInfoPanel (물방울+흰배경)")]
    public static void DecorateTrackInfoPanel()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[TrackInfoPanel] 프리팹 로드 실패: {PrefabPath}");
            return;
        }

        Transform tip = FindDeep(prefabRoot.transform, "TrackInfoPanel");
        if (tip == null)
        {
            Debug.LogError("[TrackInfoPanel] TrackInfoPanel GameObject를 찾지 못했습니다.");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        // ── 1. 배경 흰색 ──
        Image bg = tip.GetComponent<Image>();
        if (bg != null)
            bg.color = Color.white;

        // ── 2. 텍스트 색 변경 (흰 배경 위 가독성) ──
        SetTextColor(tip, "Text",         new Color(0.2f,  0.3f,  0.55f)); // ToggleBtn > Text
        SetTextColor(tip, "TotalRoundLabel", new Color(0.15f, 0.15f, 0.2f));
        SetTextColor(tip, "TrackNameLabel",  new Color(0.1f,  0.2f,  0.45f));
        SetTextColor(tip, "DistanceLabel",   new Color(0.15f, 0.3f,  0.15f));
        SetTextColor(tip, "TrackTypeLabel",  new Color(0.35f, 0.2f,  0.1f));
        SetTextColor(tip, "TrackDescLabel",  new Color(0.15f, 0.25f, 0.35f));

        // ── 3. WaterDropDecor (이미 있으면 스킵) ──
        if (tip.GetComponent<WaterDropDecor>() == null)
        {
            WaterDropDecor decor = tip.gameObject.AddComponent<WaterDropDecor>();
            var so = new SerializedObject(decor);
            so.FindProperty("dropCount").intValue  = 8;
            so.FindProperty("sizeMin").floatValue  = 5f;
            so.FindProperty("sizeMax").floatValue  = 12f;
            so.ApplyModifiedProperties();
            Debug.Log("[TrackInfoPanel] WaterDropDecor 추가 완료");
        }
        else
        {
            Debug.Log("[TrackInfoPanel] WaterDropDecor 이미 존재 — 스킵");
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        Debug.Log("[TrackInfoPanel] BettingPanel.prefab 저장 완료 ✓");
    }

    // 자식 중 name이 일치하는 Text 컴포넌트 색 변경
    private static void SetTextColor(Transform root, string goName, Color color)
    {
        Transform t = FindDeep(root, goName);
        if (t == null) return;
        Text txt = t.GetComponent<Text>();
        if (txt != null) txt.color = color;
    }

    // 재귀 탐색
    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
