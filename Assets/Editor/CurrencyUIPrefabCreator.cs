using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// SPEC-028 Step 2.13 — CurrencyHeader / ExchangeIcon 프리팹 생성기.
/// 메뉴: DopamineRace > Create Currency UI Prefabs
///
/// 생성 위치:
///   Resources/Prefabs/UI/CurrencyHeaderPrefab.prefab
///   Resources/Prefabs/UI/ExchangeIconPrefab.prefab
/// </summary>
public static class CurrencyUIPrefabCreator
{
    [MenuItem("DopamineRace/Create Currency UI Prefabs")]
    public static void CreateAll()
    {
        EnsureFolder("Assets/Resources/Prefabs");
        EnsureFolder("Assets/Resources/Prefabs/UI");

        var jellySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Items/dopamine_jelly_icon.asset");
        var stoneSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Items/dopamine_stone_icon.asset");

        if (jellySprite == null || stoneSprite == null)
        {
            Debug.LogError("[Prefab] dopamine_jelly_icon.asset 또는 dopamine_stone_icon.asset 없음 — 먼저 sprite 생성");
            return;
        }

        BuildCurrencyHeaderPrefab(jellySprite, stoneSprite);
        BuildExchangeIconPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Prefab] CurrencyHeader + ExchangeIcon 프리팹 생성 완료");
    }

    // ═══════════════════════════════════════════════════════
    //  CurrencyHeaderPrefab
    // ═══════════════════════════════════════════════════════
    private static void BuildCurrencyHeaderPrefab(Sprite jellySprite, Sprite stoneSprite)
    {
        var root = new GameObject("CurrencyHeader");
        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(360, 80);

        // 반투명 배경
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.55f);
        bg.raycastTarget = false;

        // ── Jelly 영역 (좌측 50%) ──
        var jellyContainer = NewRect("JellyContainer", root.transform,
            new Vector2(0, 0), new Vector2(0.5f, 1),
            new Vector2(8, 8), new Vector2(-4, -8));

        var jellyIconGo = NewRect("JellyIcon", jellyContainer.transform,
            new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(0, 0), new Vector2(56, 0));
        var jellyIcon = jellyIconGo.AddComponent<Image>();
        jellyIcon.sprite = jellySprite;
        jellyIcon.color = Color.white;
        jellyIcon.raycastTarget = false;
        jellyIcon.preserveAspect = true;

        var jellyTextGo = NewRect("JellyText", jellyContainer.transform,
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(64, 0), new Vector2(-4, 0));
        var jellyText = jellyTextGo.AddComponent<Text>();
        jellyText.text = "100";
        jellyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        jellyText.fontSize = 32;
        jellyText.fontStyle = FontStyle.Bold;
        jellyText.alignment = TextAnchor.MiddleLeft;
        jellyText.color = new Color(0.9f, 0.95f, 1f);
        jellyText.raycastTarget = false;

        // ── Stone 영역 (우측 50%) ──
        var stoneContainer = NewRect("StoneContainer", root.transform,
            new Vector2(0.5f, 0), new Vector2(1, 1),
            new Vector2(4, 8), new Vector2(-8, -8));

        var stoneIconGo = NewRect("StoneIcon", stoneContainer.transform,
            new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(0, 0), new Vector2(56, 0));
        var stoneIcon = stoneIconGo.AddComponent<Image>();
        stoneIcon.sprite = stoneSprite;
        stoneIcon.color = Color.white;
        stoneIcon.raycastTarget = false;
        stoneIcon.preserveAspect = true;

        var stoneTextGo = NewRect("StoneText", stoneContainer.transform,
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(64, 0), new Vector2(-4, 0));
        var stoneText = stoneTextGo.AddComponent<Text>();
        stoneText.text = "0";
        stoneText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stoneText.fontSize = 32;
        stoneText.fontStyle = FontStyle.Bold;
        stoneText.alignment = TextAnchor.MiddleLeft;
        stoneText.color = new Color(0.85f, 1f, 1f);
        stoneText.raycastTarget = false;

        // CurrencyHeader 컴포넌트 + 참조 주입
        var header = root.AddComponent<CurrencyHeader>();
        header.SetReferences(jellyText, stoneText, jellyIcon, stoneIcon);

        string path = "Assets/Resources/Prefabs/UI/CurrencyHeaderPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Prefab] {path}");
    }

    // ═══════════════════════════════════════════════════════
    //  ExchangeIconPrefab
    // ═══════════════════════════════════════════════════════
    private static void BuildExchangeIconPrefab()
    {
        var root = new GameObject("ExchangeIcon");
        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(64, 64);

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.4f, 0.6f, 0.95f);
        bg.raycastTarget = true;

        var btn = root.AddComponent<Button>();
        btn.targetGraphic = bg;

        var labelGo = NewRect("Label", root.transform,
            new Vector2(0, 0), new Vector2(1, 1),
            Vector2.zero, Vector2.zero);
        var label = labelGo.AddComponent<Text>();
        label.text = "💱";
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 36;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;

        string path = "Assets/Resources/Prefabs/UI/ExchangeIconPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Prefab] {path}");
    }

    // ═══════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════
    private static GameObject NewRect(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return go;
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
