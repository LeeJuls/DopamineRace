#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임오버 플로우 S3 — FinishPanel.prefab 에 "내 점수·등수" 1줄 표시 노드
/// MyRankText 를 국소 추가. (CharacterVideoPrefabPatcher 패턴 준수)
///
/// 위치: FinalJellyText(총 Stone) 와 동일 부모(FinishPanel 루트), 그 아래(y≈0.12).
/// 폰트/색/정렬: 형제 FinalJellyText 의 Text 값을 복사 — 톤 일치 + FontHelper 라우팅 충돌 방지.
/// 초기 text 는 빈값(런타임 ShowFinish/RefreshMyRankBadge — S4 가 채움).
///
/// 전체 프리팹 재생성 없이 노드만 추가. 멱등(FindDeep("MyRankText") 있으면 skip).
/// Unity 메뉴: DopamineRace > Patch MyRankText (게임오버 S3)
/// </summary>
public static class FinishMyRankBadgePatcher
{
    private const string PrefabPath = "Assets/Prefabs/UI/FinishPanel.prefab";
    private const string NodeName   = "MyRankText";
    private const string AnchorName = "FinalJellyText";

    [MenuItem("DopamineRace/Patch MyRankText (게임오버 S3)")]
    public static void PatchMyRankText()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError($"[FinishMyRankBadgePatcher] 프리팹 로드 실패: {PrefabPath}");
            return;
        }

        try
        {
            // ── 멱등 가드 ──
            if (FindDeep(root.transform, NodeName) != null)
            {
                Debug.Log("[FinishMyRankBadgePatcher] MyRankText 이미 존재 — 스킵");
                return;
            }

            Transform anchorNode = FindDeep(root.transform, AnchorName);
            if (anchorNode == null)
            {
                Debug.LogError($"[FinishMyRankBadgePatcher] {AnchorName} 를 찾지 못했습니다 — 중단");
                return;
            }

            // ── MyRankText GameObject 생성 (FinalJellyText 와 동일 부모) ──
            GameObject go = new GameObject(NodeName);
            go.transform.SetParent(anchorNode.parent, false);

            // ── RectTransform: FinalJellyText 에서 복사 후 y 만 아래로 조정 ──
            RectTransform srcRt = anchorNode.GetComponent<RectTransform>();
            RectTransform rt = go.AddComponent<RectTransform>();
            if (srcRt != null)
            {
                rt.anchorMin        = new Vector2(srcRt.anchorMin.x, 0.12f);
                rt.anchorMax        = new Vector2(srcRt.anchorMax.x, 0.12f);
                rt.pivot            = srcRt.pivot;
                rt.anchoredPosition = srcRt.anchoredPosition;
                rt.sizeDelta        = new Vector2(srcRt.sizeDelta.x, 40f);
                rt.localScale       = srcRt.localScale;
            }
            else
            {
                rt.anchorMin        = new Vector2(0.5f, 0.12f);
                rt.anchorMax        = new Vector2(0.5f, 0.12f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta        = new Vector2(700f, 40f);
            }

            // ── Text: 형제 FinalJellyText 의 스타일 복사 (톤 일치) ──
            Text t = go.AddComponent<Text>();
            Text srcText = anchorNode.GetComponent<Text>();
            if (srcText != null)
            {
                t.font             = srcText.font;
                t.fontSize         = srcText.fontSize;
                t.fontStyle        = srcText.fontStyle;
                t.alignment        = srcText.alignment;
                t.color            = srcText.color;
                t.supportRichText  = srcText.supportRichText;
                t.horizontalOverflow = srcText.horizontalOverflow;
                t.verticalOverflow   = srcText.verticalOverflow;
                t.lineSpacing      = srcText.lineSpacing;
            }
            else
            {
                t.fontSize  = 42;
                t.alignment = TextAnchor.MiddleCenter;
                t.color     = new Color(0.1f, 0.3f, 0.6f, 1f);
            }
            t.text          = "";   // 런타임(ShowFinish/RefreshMyRankBadge)에서 채움
            t.raycastTarget = false;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("[FinishMyRankBadgePatcher] MyRankText 추가 + FinishPanel.prefab 저장 완료 ✓");
        }
        finally
        {
            if (root != null)
                PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // 재귀 탐색 (CharacterVideoPrefabPatcher 와 동일 시그니처)
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
#endif
