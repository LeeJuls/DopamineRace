using UnityEngine;

// ══════════════════════════════════════════
//  충돌 VFX 타입
// ══════════════════════════════════════════

public enum CollisionVFXType
{
    Hit,        // 충돌
    Dodge,      // 회피
    Slingshot,  // 슬링샷 (추격)
    Crit,       // Lucky 크리티컬
    Slipstream  // 슬립스트림 가속 발동
}

// ══════════════════════════════════════════
//  충돌 VFX 표시 컴포넌트 (v2 — Resources 기반)
//
//  스프라이트를 Resources/VFX/ PNG 에셋에서 로드.
//  런타임 Texture2D/Sprite.Create() 사용하지 않음.
//  Editor 도구 DopamineRace > Generate Collision VFX Sprites 로
//  PNG 에셋을 사전 생성해야 함.
// ══════════════════════════════════════════

public class CollisionVFX : MonoBehaviour
{
    // ── Resources에서 로드한 스프라이트 (static 캐시) ──
    private static Sprite s_circle;
    private static Sprite s_star6;
    private static Sprite s_star5;
    private static Sprite s_arrow;
    private static Sprite s_shield;

    // ── 인스턴스 오브젝트 ──
    private GameObject vfxRoot;
    private SpriteRenderer bgSprite;
    private SpriteRenderer iconSprite;
    private TextMesh label;

    // ── 애니메이션 상태 ──
    private float timer;
    private float totalDuration;
    private Vector3 startLocalPos;
    private float floatSpeed = 1.5f;
    private CollisionVFXType currentType;

    // ── 우선순위 (높을수록 우선) ──
    private static int GetPriority(CollisionVFXType type)
    {
        switch (type)
        {
            case CollisionVFXType.Crit:       return 3;
            case CollisionVFXType.Hit:        return 2;
            case CollisionVFXType.Dodge:      return 1;
            case CollisionVFXType.Slingshot:  return 0;
            case CollisionVFXType.Slipstream: return -1;
            default: return -2;
        }
    }

    // ══════════════════════════════════════════
    //  스프라이트 로드 (Resources 에셋 — GC 안전)
    // ══════════════════════════════════════════

    private static void LoadSprites()
    {
        // Resources.Load 결과는 Unity 에셋 파이프라인 관리 → Reload Scene에서도 유지
        if (s_circle != null) return;

        s_circle = Resources.Load<Sprite>("VFX/vfx_circle");
        s_star6  = Resources.Load<Sprite>("VFX/vfx_star6");
        s_star5  = Resources.Load<Sprite>("VFX/vfx_star5");
        s_arrow  = Resources.Load<Sprite>("VFX/vfx_arrow");
        s_shield = Resources.Load<Sprite>("VFX/vfx_shield");

        if (s_circle == null)
            Debug.LogError("[CollisionVFX] Resources/VFX/ 스프라이트 없음! " +
                           "DopamineRace > Generate Collision VFX Sprites 실행 필요");
    }

    // ══════════════════════════════════════════
    //  VFX 표시
    // ══════════════════════════════════════════

    public void Show(CollisionVFXType type, float duration)
    {
        // 우선순위 보호: 높은 VFX 표시 중이면 낮은 건 스킵
        if (timer > 0f && GetPriority(type) < GetPriority(currentType))
            return;

        currentType = type;
        LoadSprites();

        var gs = GameSettings.Instance;

        // vfxRoot 없으면 생성
        if (vfxRoot == null)
            BuildVFX(gs);

        // ── GameSettings에서 크기 읽기 ──
        float iconScale = gs.vfxIconScale;
        float labelCharSize = gs.vfxLabelSize;
        float height = gs.vfxHeight;
        floatSpeed = gs.vfxFloatSpeed;

        // ── 타입별 설정 ──
        Color bgColor;
        Color iconColor;
        Sprite icon;
        string text;

        switch (type)
        {
            case CollisionVFXType.Hit:
                bgColor = new Color(1f, 0.2f, 0.1f, 0.9f);
                iconColor = new Color(1f, 1f, 0.3f, 1f);
                icon = gs.vfxHitIcon != null ? gs.vfxHitIcon : s_star6;
                text = "HIT!";
                break;

            case CollisionVFXType.Dodge:
                bgColor = new Color(0.2f, 0.6f, 1f, 0.9f);
                iconColor = new Color(0.8f, 1f, 1f, 1f);
                icon = gs.vfxDodgeIcon != null ? gs.vfxDodgeIcon : s_shield;
                text = "DODGE!";
                break;

            case CollisionVFXType.Slingshot:
                bgColor = new Color(0.1f, 0.9f, 0.3f, 0.9f);
                iconColor = new Color(1f, 1f, 0.5f, 1f);
                icon = gs.vfxSlingshotIcon != null ? gs.vfxSlingshotIcon : s_arrow;
                text = "CHASE!";
                break;

            case CollisionVFXType.Crit:
                bgColor = new Color(0.5f, 0.1f, 0.8f, 0.9f);
                iconColor = new Color(1f, 1f, 0.3f, 1f);
                icon = s_star5;
                text = "BOOST!";
                break;

            case CollisionVFXType.Slipstream:
                bgColor = new Color(0.2f, 0.6f, 1f, 0.85f);
                iconColor = new Color(0.8f, 1f, 1f, 1f);
                icon = s_arrow;
                text = "DRAFT!";
                break;

            default:
                bgColor = Color.white;
                iconColor = Color.white;
                icon = s_circle;
                text = "?";
                break;
        }

        // ── 커스텀 아이콘이면 tint 안 먹이기 ──
        bool isCustomIcon = (type == CollisionVFXType.Hit && gs.vfxHitIcon != null)
            || (type == CollisionVFXType.Dodge && gs.vfxDodgeIcon != null)
            || (type == CollisionVFXType.Slingshot && gs.vfxSlingshotIcon != null);

        // ── 적용 ──
        bgSprite.sprite = s_circle;
        bgSprite.color = bgColor;
        bgSprite.transform.localScale = Vector3.one * iconScale;

        iconSprite.sprite = icon;
        iconSprite.color = isCustomIcon ? Color.white : iconColor;
        iconSprite.transform.localScale = Vector3.one * (iconScale * 0.6f);
        iconSprite.transform.localPosition = new Vector3(0, 0, -0.01f);

        label.text = text;
        label.color = Color.white;
        label.characterSize = labelCharSize;

        startLocalPos = new Vector3(0, height, 0);
        vfxRoot.transform.localPosition = startLocalPos;
        vfxRoot.SetActive(true);

        timer = duration;
        totalDuration = duration;

        FixFlip();
    }

    // ══════════════════════════════════════════
    //  VFX 오브젝트 생성
    // ══════════════════════════════════════════

    private void BuildVFX(GameSettings gs)
    {
        float height = gs != null ? gs.vfxHeight : 1.8f;
        float labelCharSize = gs != null ? gs.vfxLabelSize : 0.08f;

        vfxRoot = new GameObject("CollisionVFX");
        vfxRoot.transform.SetParent(transform);
        vfxRoot.transform.localPosition = new Vector3(0, height, 0);

        // 배경 원
        var bgObj = new GameObject("BG");
        bgObj.transform.SetParent(vfxRoot.transform);
        bgObj.transform.localPosition = Vector3.zero;
        bgSprite = bgObj.AddComponent<SpriteRenderer>();
        bgSprite.sortingOrder = 100;
        bgSprite.sprite = s_circle;

        // 아이콘
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(vfxRoot.transform);
        iconObj.transform.localPosition = new Vector3(0, 0, -0.01f);
        iconSprite = iconObj.AddComponent<SpriteRenderer>();
        iconSprite.sortingOrder = 101;

        // 텍스트 라벨
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(vfxRoot.transform);
        labelObj.transform.localPosition = new Vector3(0, -0.35f, -0.02f);
        label = labelObj.AddComponent<TextMesh>();
        label.alignment = TextAlignment.Center;
        label.anchor = TextAnchor.MiddleCenter;
        label.characterSize = labelCharSize;
        label.fontSize = 36;
        label.fontStyle = FontStyle.Bold;
        label.color = Color.white;

        FontHelper.ApplyToTextMesh(label, 102);
    }

    // ══════════════════════════════════════════
    //  업데이트 (떠오르기 + 페이드 + 스케일 팝)
    // ══════════════════════════════════════════

    private void Update()
    {
        if (vfxRoot == null || !vfxRoot.activeSelf) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            vfxRoot.SetActive(false);
            return;
        }

        float progress = 1f - (timer / totalDuration);

        // ── 위로 떠오르기 ──
        Vector3 pos = startLocalPos;
        pos.y += progress * floatSpeed;
        vfxRoot.transform.localPosition = pos;

        // ── 페이드아웃 (후반 40%에서) ──
        float alpha = 1f;
        if (progress > 0.6f)
            alpha = 1f - ((progress - 0.6f) / 0.4f);

        Color bgCol = bgSprite.color;
        bgCol.a = alpha * 0.9f;
        bgSprite.color = bgCol;

        Color iconCol = iconSprite.color;
        iconCol.a = alpha;
        iconSprite.color = iconCol;

        Color lblCol = label.color;
        lblCol.a = alpha;
        label.color = lblCol;

        // ── 스케일 팝 효과 (처음 20%에서 확대 후 축소) ──
        float scale = 1f;
        if (progress < 0.2f)
        {
            float t = progress / 0.2f;
            scale = 1f + 0.3f * Mathf.Sin(t * Mathf.PI);
        }
        vfxRoot.transform.localScale = Vector3.one * scale;

        FixFlip();
    }

    // ── 부모 flip 보정 ──
    private void FixFlip()
    {
        if (vfxRoot == null) return;
        float parentScaleX = Mathf.Sign(transform.localScale.x);
        Vector3 ls = vfxRoot.transform.localScale;
        ls.x = Mathf.Abs(ls.x) * parentScaleX;
        vfxRoot.transform.localScale = ls;
    }
}
