using UnityEngine;
using UnityEditor;

/// <summary>
/// 와이드/울트라와이드 배팅 화면 레이아웃 국소 패치 (SPEC: 와이드 모니터 수정).
/// 라이브 프리팹을 LoadPrefabContents로 열어 RectTransform 2개만 외과적으로 수정한다.
/// **전체 재생성(BettingUIPrefabCreator) 금지** — 라이브 프리팹이 단일 진실원.
///
/// 변경:
///  1) BettingPanel.prefab / CharacterListPanel: 손상 앵커(anchorMax.x=-0.1) → 클린 stretch.
///     좌측 리스트가 화면 폭에 비례한 고정 분수가 되어 와이드에서 축소·잘림이 사라진다.
///     (세로는 원본 그대로 보존 — 가로만 교정)
///  2) CharacterItem.prefab / OddsLabel: 좌측 고정(493px) → 우측 가장자리 앵커.
///     배지가 행 우변을 추적 → 어떤 폭에서도 노출. (OddsLabel은 상단 행 y=+33.5라
///     하단 행 전적/2착과 세로가 겹치지 않음 → 우측 이동해도 충돌 없음. 자식 Text는
///     배지 크기 불변이라 함께 이동 — 별도 패치 불필요.)
/// </summary>
public static class WideMonitorLayoutPatcher
{
    private const string BettingPanelPath = "Assets/Prefabs/UI/BettingPanel.prefab";
    private const string CharacterItemPath = "Assets/Prefabs/UI/CharacterItem.prefab";

    [MenuItem("DopamineRace/Fix Wide Monitor Betting Layout")]
    public static void Patch()
    {
        int changed = 0;
        changed += PatchCharacterListPanel();
        changed += PatchOddsLabel();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[WideMonitorLayoutPatcher] 완료 — {changed}/2 노드 패치됨.");
    }

    // ── 1) CharacterListPanel: 손상 anchorMax.x=-0.1 → 클린 stretch (가로만 교정, 세로 보존) ──
    private static int PatchCharacterListPanel()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BettingPanelPath);
        try
        {
            Transform t = FindDeep(root.transform, "CharacterListPanel");
            if (t == null) { Debug.LogError("[Patcher] CharacterListPanel 못 찾음"); return 0; }
            var rt = (RectTransform)t;

            Debug.Log($"[Patcher] CharacterListPanel 전: min={rt.anchorMin} max={rt.anchorMax} pos={rt.anchoredPosition} size={rt.sizeDelta}");

            // 가로: 손상 앵커 → 클린. 세로(y)는 원본 그대로 유지.
            rt.anchorMin = new Vector2(0.01f, rt.anchorMin.y);   // 좌/상 원본 유지
            rt.anchorMax = new Vector2(0.35f, rt.anchorMax.y);   // 우변: -0.1 → 0.35 (콘텐츠 전부 수용 폭)
            rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);  // x오프셋 제거, y보존
            rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y);                // x여분 제거, y보존

            Debug.Log($"[Patcher] CharacterListPanel 후: min={rt.anchorMin} max={rt.anchorMax} pos={rt.anchoredPosition} size={rt.sizeDelta}");

            CleanMissingScripts(root);   // 기존 'Illustration' missing-script가 저장 차단 → 제거(비기능 컴포넌트)
            PrefabUtility.SaveAsPrefabAsset(root, BettingPanelPath);
            return 1;
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // ── 2) OddsLabel: 좌측(0,0.5)+493px → 우측(1,0.5)+(-60). 행 우변 추적 ──
    private static int PatchOddsLabel()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CharacterItemPath);
        try
        {
            Transform t = FindDeep(root.transform, "OddsLabel");
            if (t == null) { Debug.LogError("[Patcher] OddsLabel 못 찾음"); return 0; }
            var rt = (RectTransform)t;

            Debug.Log($"[Patcher] OddsLabel 전: min={rt.anchorMin} max={rt.anchorMax} pos={rt.anchoredPosition} size={rt.sizeDelta} pivot={rt.pivot}");

            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-60f, rt.anchoredPosition.y);  // y(33.5) 보존, x를 우변 기준 -60
            // sizeDelta(75,44) 유지

            Debug.Log($"[Patcher] OddsLabel 후: min={rt.anchorMin} max={rt.anchorMax} pos={rt.anchoredPosition} size={rt.sizeDelta} pivot={rt.pivot}");

            CleanMissingScripts(root);
            PrefabUtility.SaveAsPrefabAsset(root, CharacterItemPath);
            return 1;
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // 저장을 막는 missing-script(스크립트 삭제/이동으로 깨진 비기능 컴포넌트) 제거.
    private static int CleanMissingScripts(GameObject root)
    {
        int removed = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        if (removed > 0) Debug.Log($"[Patcher] missing-script {removed}개 제거 (저장 차단 해소)");
        return removed;
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var found = FindDeep(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
