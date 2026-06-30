using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 환전 획득 연출 — 모달 위에 "+N 🟦" 팝 + 별 버스트 + 플래시.
/// 모달과 수명 분리: 자체 Canvas(sortingOrder 1001) + 자체 코루틴 → 모달이 닫혀도
/// 끝까지 재생 후 자가 소멸(누수/잔상 차단). RewardBurst.Spawn(parent, gain, font)로 호출.
/// </summary>
public class RewardBurst : MonoBehaviour
{
    private const int SORTING    = 1001;   // 모달(1000) 위
    private const int STAR_COUNT = 10;

    private class Star { public RectTransform rt; public Image img; public Vector2 dir; public float speed; public float spin; }

    private Image _flash;
    private Text  _gainText;
    private CanvasGroup _gainGroup;   // "+N [아이콘]" 그룹 — 스케일/페이드 단위
    private readonly List<Star> _stars = new List<Star>();

    /// <summary>획득 연출 1회 생성. uiParent는 모달의 부모(메인 캔버스) 권장(모달과 수명 분리).</summary>
    public static RewardBurst Spawn(Transform uiParent, int gain, Font font)
    {
        var go = new GameObject("RewardBurst");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(uiParent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        // 자체 Canvas — 모달 위(1001), 런타임이라 직접 대입 OK
        var canvas = go.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = SORTING;

        var rb = go.AddComponent<RewardBurst>();
        rb.Build(gain, font);
        return rb;
    }

    private void Build(int gain, Font font)
    {
        // 플래시 (전체 화면, 입력 통과)
        _flash = NewImage("Flash", Vector2.zero, Vector2.one, Vector2.zero);
        _flash.color = new Color(1f, 0.95f, 0.6f, 0f);

        // 별 버스트 (vfx_star는 spriteMode Multiple → LoadAll 필수, 없으면 생략)
        Sprite[] stars = LoadStarSprites();
        if (stars != null && stars.Length > 0)
        {
            for (int i = 0; i < STAR_COUNT; i++)
            {
                var img = NewImage("Star" + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(38, 38));
                img.sprite = stars[i % stars.Length];
                img.color  = new Color(1f, 0.9f, 0.35f, 1f);
                float ang  = (360f / STAR_COUNT) * i + i * 11f;   // 약간 불규칙
                _stars.Add(new Star {
                    rt = img.rectTransform, img = img,
                    dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)),
                    speed = 230f + (i % 3) * 55f,
                    spin  = (i % 2 == 0 ? 1f : -1f) * 200f
                });
            }
        }

        // "+N [젤리아이콘]" 그룹 (HLG 중앙, CanvasGroup 페이드) — 이모지 미렌더 회피, 실제 스프라이트
        var grt = NewRect("GainGroup", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(820, 200));
        _gainGroup = grt.gameObject.AddComponent<CanvasGroup>();
        var hlg = grt.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 6;
        hlg.childControlWidth = true;  hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

        var numGo = new GameObject("Num", typeof(RectTransform));
        numGo.transform.SetParent(grt, false);
        _gainText = numGo.AddComponent<Text>();
        if (font != null) _gainText.font = font;
        _gainText.fontSize  = 120;   // SPEC-051: 최종 결과 임팩트
        _gainText.fontStyle = FontStyle.Bold;
        _gainText.alignment = TextAnchor.MiddleCenter;
        _gainText.color = new Color(1f, 0.82f, 0.18f, 1f);
        _gainText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _gainText.verticalOverflow   = VerticalWrapMode.Overflow;
        _gainText.raycastTarget = false;
        _gainText.text = "+" + gain;

        var jelly = LoadJellyIcon();
        if (jelly != null)
        {
            var iconGo = new GameObject("JellyIcon", typeof(RectTransform));
            iconGo.transform.SetParent(grt, false);
            var iimg = iconGo.AddComponent<Image>();
            iimg.sprite = jelly; iimg.preserveAspect = true; iimg.raycastTarget = false;
            var le = iconGo.AddComponent<LayoutElement>();
            le.preferredWidth = 130; le.preferredHeight = 130;   // +30%
        }
        grt.localScale = Vector3.zero;

        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        const float DUR = 1.0f;
        float t = 0f;
        var grt = (_gainGroup != null) ? _gainGroup.transform as RectTransform : null;

        while (t < DUR)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;

            // 플래시: 0→0.25 (~0.08s) → 0 (~0.3s)
            if (_flash != null)
            {
                float fa = (t < 0.08f) ? Mathf.Lerp(0f, 0.25f, t / 0.08f)
                                       : Mathf.Lerp(0.25f, 0f, Mathf.Clamp01((t - 0.08f) / 0.22f));
                var c = _flash.color; c.a = Mathf.Max(0f, fa); _flash.color = c;
            }

            // 별: 방사형 이동 + 회전 + ~0.55s 페이드·축소
            for (int i = 0; i < _stars.Count; i++)
            {
                var s = _stars[i];
                s.rt.anchoredPosition += s.dir * s.speed * dt;
                s.rt.Rotate(0f, 0f, s.spin * dt);
                float life = Mathf.Clamp01(t / 0.55f);
                s.rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, life);
                var col = s.img.color; col.a = 1f - life; s.img.color = col;
            }

            // 텍스트: 바운스(0→1.3→1.0, ~0.3s) → 0.55s부터 떠오름+페이드
            if (grt != null)
            {
                float scale = (t < 0.16f) ? Mathf.Lerp(0f, 1.3f, t / 0.16f)
                            : (t < 0.30f) ? Mathf.Lerp(1.3f, 1.0f, (t - 0.16f) / 0.14f)
                            : 1.0f;
                grt.localScale = Vector3.one * scale;

                if (t > 0.55f)
                {
                    float u = Mathf.Clamp01((t - 0.55f) / 0.45f);
                    grt.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 60f, u));
                    if (_gainGroup != null) _gainGroup.alpha = 1f - u;
                }
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    // ── 헬퍼 ──
    private RectTransform NewRect(string name, Vector2 aMin, Vector2 aMax, Vector2 size)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    private Image NewImage(string name, Vector2 aMin, Vector2 aMax, Vector2 size)
    {
        var rt = NewRect(name, aMin, aMax, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.raycastTarget = false;     // 연출은 입력 가로채지 않음
        return img;
    }

    private static Sprite LoadJellyIcon()
    {
        var item = Resources.Load<CurrencyItem>("Items/DopamineJelly");
        return (item != null) ? item.icon : null;
    }

    private static Sprite[] LoadStarSprites()
    {
        var list = new List<Sprite>();
        var a = Resources.LoadAll<Sprite>("VFX/vfx_star5");
        if (a != null) list.AddRange(a);
        var b = Resources.LoadAll<Sprite>("VFX/vfx_star6");
        if (b != null) list.AddRange(b);
        return list.ToArray();
    }

    private static string SafeLoc(string key, string fallback, params object[] args)
    {
        string val = Loc.Get(key);
        if (string.IsNullOrEmpty(val) || val == key) val = fallback;
        if (args != null && args.Length > 0)
        {
            try { val = string.Format(val, args); } catch { /* swallow */ }
        }
        return val;
    }
}
