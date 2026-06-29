using UnityEditor;
using UnityEngine;

public static class BetAmountModalDecorator
{
    private const string PREFAB_PATH = "Assets/Prefabs/UI/BetAmountModalPrefab.prefab";

    [MenuItem("DopamineRace/Decorate BetAmountModal (물방울)")]
    public static void Decorate()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        if (root == null) { Debug.LogError("[BetAmountModalDecorator] 프리팹 로드 실패"); return; }

        Transform modal = FindDeep(root.transform, "Modal");
        if (modal == null)
        {
            Debug.LogError("[BetAmountModalDecorator] Modal 노드를 찾을 수 없음");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        // 중복 방지
        if (modal.GetComponent<WaterDropDecor>() != null)
        {
            Debug.LogWarning("[BetAmountModalDecorator] WaterDropDecor 이미 존재 — 스킵");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        WaterDropDecor decor = modal.gameObject.AddComponent<WaterDropDecor>();
        var so = new SerializedObject(decor);
        so.FindProperty("dropCount").intValue = 7;
        so.FindProperty("sizeMin").floatValue = 4f;
        so.FindProperty("sizeMax").floatValue = 10f;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[BetAmountModalDecorator] BetAmountModalPrefab.prefab 저장 완료");
    }

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
