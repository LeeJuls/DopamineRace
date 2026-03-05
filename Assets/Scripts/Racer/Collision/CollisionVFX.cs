using UnityEngine;

// ══════════════════════════════════════════
//  충돌 VFX 타입
// ══════════════════════════════════════════

public enum CollisionVFXType
{
    Hit,        // 충돌
    Dodge,      // 회피
    Slingshot,  // 슬링샷 (추격)
    Crit        // Lucky 크리티컬
}

// ══════════════════════════════════════════
//  충돌 VFX 표시 컴포넌트
//
//  인스턴스별 스프라이트 생성 — static 캐싱 없음
//  CreateVFXObjects()에서 모든 스프라이트 즉시 할당
// ══════════════════════════════════════════

public class CollisionVFX : MonoBehaviour
{
    private GameObject vfxRoot;
    private SpriteRenderer bgSprite;
    private SpriteRenderer iconSprite;
    private TextMesh label;

    private float timer = 0f;
    private float totalDuration = 0f;
    private Vector3 startLocalPos;
    private float floatSpeed = 1.5f;

    // ── 인스턴스별 아이콘 스프라이트 ──
    private Sprite _circleSprite;
    private Sprite _starSprite;
    private Sprite _star5Sprite;
    private Sprite _arrowSprite;
    private Sprite _shieldSprite;

    private CollisionVFXType currentType;

    private static int GetPriority(CollisionVFXType type)
    {
        switch (type)
        {
            case CollisionVFXType.Crit:      return 3;
            case CollisionVFXType.Hit:       return 2;
            case CollisionVFXType.Dodge:     return 1;
            case CollisionVFXType.Slingshot: return 0;
            default: return -1;
        }
    }

    public void Show(CollisionVFXType type, float duration)
    {
        // 우선순위 보호: 높은 VFX 표시 중이면 낮은 건 스킵
        if (timer > 0f && GetPriority(type) < GetPriority(currentType))
            return;

        currentType = type;

        var gs = GameSettings.Instance;

        if (vfxRoot == null)
            CreateVFXObjects(gs);

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
                icon = gs.vfxHitIcon != null ? gs.vfxHitIcon : _starSprite;
                text = "HIT!";
                break;

            case CollisionVFXType.Dodge:
                bgColor = new Color(0.2f, 0.6f, 1f, 0.9f);
                iconColor = new Color(0.8f, 1f, 1f, 1f);
                icon = gs.vfxDodgeIcon != null ? gs.vfxDodgeIcon : _shieldSprite;
                text = "DODGE!";
                break;

            case CollisionVFXType.Slingshot:
                bgColor = new Color(0.1f, 0.9f, 0.3f, 0.9f);
                iconColor = new Color(1f, 1f, 0.5f, 1f);
                icon = gs.vfxSlingshotIcon != null ? gs.vfxSlingshotIcon : _arrowSprite;
                text = "CHASE!";
                break;

            case CollisionVFXType.Crit:
                bgColor = new Color(0.5f, 0.1f, 0.8f, 0.9f);
                iconColor = new Color(1f, 1f, 0.3f, 1f);
                icon = _star5Sprite;
                text = "BOOST!";
                break;

            default:
                bgColor = Color.white;
                iconColor = Color.white;
                icon = _circleSprite;
                text = "?";
                break;
        }

        // ── 커스텀 아이콘이면 tint 안 먹이기 ──
        bool isCustomIcon = (type == CollisionVFXType.Hit && gs.vfxHitIcon != null)
            || (type == CollisionVFXType.Dodge && gs.vfxDodgeIcon != null)
            || (type == CollisionVFXType.Slingshot && gs.vfxSlingshotIcon != null);

        // ── 적용 ──
        bgSprite.sprite = _circleSprite;
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

    private void CreateVFXObjects(GameSettings gs)
    {
        float height = gs != null ? gs.vfxHeight : 1.8f;
        float labelCharSize = gs != null ? gs.vfxLabelSize : 0.08f;

        // ── 스프라이트 즉시 생성 (인스턴스별) ──
        _circleSprite = CollisionSpriteFactory.CreateCircleSprite(32, Color.white);
        _starSprite = CollisionSpriteFactory.CreateStarSprite(32, Color.white, 6);
        _star5Sprite = CollisionSpriteFactory.CreateStarSprite(32, Color.white, 5);
        _arrowSprite = CollisionSpriteFactory.CreateArrowSprite(32, Color.white);
        _shieldSprite = CollisionSpriteFactory.CreateShieldSprite(32, Color.white);

        vfxRoot = new GameObject("CollisionVFX");
        vfxRoot.transform.SetParent(transform);
        vfxRoot.transform.localPosition = new Vector3(0, height, 0);

        // 배경 원
        var bgObj = new GameObject("BG");
        bgObj.transform.SetParent(vfxRoot.transform);
        bgObj.transform.localPosition = Vector3.zero;
        bgSprite = bgObj.AddComponent<SpriteRenderer>();
        bgSprite.sortingOrder = 100;
        bgSprite.sprite = _circleSprite;

        // 아이콘
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(vfxRoot.transform);
        iconObj.transform.localPosition = new Vector3(0, 0, -0.01f);
        iconSprite = iconObj.AddComponent<SpriteRenderer>();
        iconSprite.sortingOrder = 101;
        iconSprite.sprite = _starSprite;

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

    private void FixFlip()
    {
        if (vfxRoot == null) return;
        float parentScaleX = Mathf.Sign(transform.localScale.x);
        Vector3 ls = vfxRoot.transform.localScale;
        ls.x = Mathf.Abs(ls.x) * parentScaleX;
        vfxRoot.transform.localScale = ls;
    }
}
