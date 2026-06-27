#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SPEC-049 단계 C — BettingPanel.prefab 의 IllustrationMask 자식으로
/// IllustrationVideo(RawImage + AspectRatioFitter) 노드를 국소 추가.
///
/// 비디오가 PNG(Illustration) "위"에 렌더되도록 형제 맨 끝에 배치(단계 D 크로스페이드용).
/// RectTransform 초기값은 라이브 형제 Illustration에서 복사(하드코딩 금지 — 디자이너 조정값 보존).
/// CharacterVideoController 부착·재생은 단계 D 소관 — 본 단계는 프리팹 구조만 만든다.
///
/// 전체 프리팹 재생성 없이 IllustrationMask 노드만 건드림. 멱등(재실행해도 1개만).
/// </summary>
public static class CharacterVideoPrefabPatcher
{
    private const string PrefabPath = "Assets/Prefabs/UI/BettingPanel.prefab";

    [MenuItem("DopamineRace/Patch IllustrationVideo (SPEC-049)")]
    public static void PatchIllustrationVideo()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError($"[CharacterVideoPatcher] 프리팹 로드 실패: {PrefabPath}");
            return;
        }

        try
        {
            Transform mask = FindDeep(root.transform, "IllustrationMask");
            if (mask == null)
            {
                Debug.LogError("[CharacterVideoPatcher] IllustrationMask 를 찾지 못했습니다.");
                return;
            }

            Transform illustration = mask.Find("Illustration");
            if (illustration == null)
            {
                Debug.LogError("[CharacterVideoPatcher] IllustrationMask/Illustration 을 찾지 못했습니다.");
                return;
            }

            // ── 멱등 가드 ──
            if (mask.Find("IllustrationVideo") != null)
            {
                Debug.Log("[CharacterVideoPatcher] IllustrationVideo 이미 존재 — 스킵");
                return;
            }

            // ── IllustrationVideo GameObject 생성 (형제 맨 끝 = Illustration 위에 렌더) ──
            GameObject videoGO = new GameObject("IllustrationVideo");
            videoGO.transform.SetParent(mask, false); // SetParent(.,false) → 기본 맨 끝 sibling

            // ── RectTransform: 라이브 형제 Illustration에서 복사 (하드코딩 금지) ──
            RectTransform src = illustration.GetComponent<RectTransform>();
            RectTransform rt = videoGO.AddComponent<RectTransform>();
            if (src != null)
            {
                rt.anchorMin        = src.anchorMin;
                rt.anchorMax        = src.anchorMax;
                rt.pivot            = src.pivot;
                rt.anchoredPosition = src.anchoredPosition;
                rt.sizeDelta        = src.sizeDelta;
                rt.localScale       = src.localScale;
            }

            // ── RawImage: 프리팹 단계부터 비활성(플래시 방지) ──
            RawImage raw = videoGO.AddComponent<RawImage>();
            raw.enabled = false;
            raw.raycastTarget = false;
            raw.color = Color.white;

            // ── AspectRatioFitter: HeightControlsWidth. 비율은 형제 fitter 값(없으면 1.0) 복사 ──
            // (런타임 실제 비율은 단계 D CharacterVideoController.OnPrepareCompleted 가 갱신)
            AspectRatioFitter fitter = videoGO.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            AspectRatioFitter srcFitter = illustration.GetComponent<AspectRatioFitter>();
            fitter.aspectRatio = srcFitter != null ? srcFitter.aspectRatio : 1.0f;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("[CharacterVideoPatcher] IllustrationVideo 추가 + BettingPanel.prefab 저장 완료 ✓");
        }
        finally
        {
            if (root != null)
                PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // 재귀 탐색 (TrackInfoPanelDecorator 와 동일 시그니처 — 에디터 정적 클래스 간 공유 헬퍼 없음)
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
