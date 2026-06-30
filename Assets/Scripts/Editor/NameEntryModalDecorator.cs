using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NameEntryModal.prefab 내 Panel 노드에만 국소 수정을 적용.
/// - 배경: BG_01 9-슬라이스 흰색 (모달 패밀리 통일)
/// - 텍스트: 다크 팔레트 (흰 배경 가독성, OptionPanel/ExchangeModal 컨벤션)
/// - WaterDropDecor 컴포넌트 추가 (dropCount=8, sizeMin=5, sizeMax=12 — TrackInfoPanel·CharacterItem과 동일)
///
/// 전체 프리팹 재생성 없이 Panel 노드만 건드림 (720×580 수동조정 크기·배선·SerializeField 참조 보존).
/// 결과 화면 텍스트 색은 런타임이 덮어쓰므로 SceneBootstrapper.NameEntry.cs에서 별도 처리.
/// </summary>
public static class NameEntryModalDecorator
{
    private const string PrefabPath = "Assets/Prefabs/UI/NameEntryModal.prefab";
    private const string BgSpritePath = "Assets/Resources/UI/Img_BG_ListSlot_BG_01.png";

    // 흰 배경 팔레트 (OptionPanelPrefabCreator/CurrencyUIPrefabCreator 표준)
    private static readonly Color darkNavy  = new Color(0.10f, 0.10f, 0.20f, 1f); // 주 텍스트(점수·슬롯글자)
    private static readonly Color darkAmber = new Color(0.55f, 0.30f, 0.00f, 1f); // 제목·결과(자격)
    private static readonly Color darkGray  = new Color(0.35f, 0.35f, 0.45f, 1f); // 보조(가이드·계속)

    [MenuItem("DopamineRace/Decorate NameEntryModal (물방울+흰배경)")]
    public static void DecorateNameEntryModal()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[NameEntryModalDecorator] 프리팹 로드 실패: {PrefabPath}");
            return;
        }

        Transform panel = FindDeep(prefabRoot.transform, "Panel");
        if (panel == null)
        {
            Debug.LogError("[NameEntryModalDecorator] Panel GameObject를 찾지 못했습니다.");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        // ── 1. 배경 BG_01 9-슬라이스 흰색 ──
        Image panelImg = panel.GetComponent<Image>();
        if (panelImg != null)
        {
            panelImg.raycastTarget = false;   // 차단은 Backdrop 노드 전담
            Sprite bg = AssetDatabase.LoadAssetAtPath<Sprite>(BgSpritePath);
            if (bg != null)
            {
                panelImg.sprite     = bg;
                panelImg.type       = Image.Type.Sliced;
                panelImg.fillCenter = true;
                panelImg.color      = Color.white;
            }
            else
            {
                Debug.LogWarning("[NameEntryModalDecorator] Img_BG_ListSlot_BG_01 없음 — 단색 폴백");
            }
        }

        // ── 2. 텍스트 색 변경 (흰 배경 위 가독성) ──
        SetTextColor(panel, "TitleText",      darkAmber);
        SetTextColor(panel, "ScoreText",      darkNavy);
        SetTextColor(panel, "GuideText",      darkGray);
        SetTextColor(panel, "ContinueText",   darkGray);
        SetTextColor(panel, "SlotText0",      darkNavy);
        SetTextColor(panel, "SlotText1",      darkNavy);
        SetTextColor(panel, "SlotText2",      darkNavy);
        SetTextColor(panel, "RankResultText", darkAmber);   // 정적 기본값 (런타임 ShowResult가 덮어씀)

        // ── 3. WaterDropDecor (이미 있으면 스킵) ──
        if (panel.GetComponent<WaterDropDecor>() == null)
        {
            WaterDropDecor decor = panel.gameObject.AddComponent<WaterDropDecor>();
            var so = new SerializedObject(decor);
            so.FindProperty("dropCount").intValue = 8;
            so.FindProperty("sizeMin").floatValue = 5f;
            so.FindProperty("sizeMax").floatValue = 12f;
            so.ApplyModifiedProperties();
            Debug.Log("[NameEntryModalDecorator] WaterDropDecor 추가 완료");
        }
        else
        {
            Debug.Log("[NameEntryModalDecorator] WaterDropDecor 이미 존재 — 스킵");
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        Debug.Log("[NameEntryModalDecorator] NameEntryModal.prefab 저장 완료 ✓");
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
