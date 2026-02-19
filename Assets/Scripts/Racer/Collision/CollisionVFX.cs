using UnityEngine;

// ══════════════════════════════════════════
//  충돌 VFX 타입
// ══════════════════════════════════════════

public enum CollisionVFXType
{
    Hit,        // 💥 충돌
    Dodge,      // 🛡️ 회피
    Slingshot,  // 🚀 슬링샷 (추격)
    Crit        // ⭐ Lucky 크리티컬
}

// ══════════════════════════════════════════
//  충돌 VFX 표시 컴포넌트 (SpriteRenderer 기반)
//
//  TextMesh는 이모지를 렌더링할 수 없으므로
//  런타임 생성 스프라이트 + 텍스트로 대체
//
//  스프라이트 생성은 CollisionSpriteFactory에 위임
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

    // ── 캐싱된 텍스처/스프라이트 (static으로 1번만 생성) ──
    private static Sprite circleSprite;
    private static Sprite starSprite;      // 6각 별 (Hit용)
    private static Sprite star5Sprite;     // 5각 별 (Crit용)
    private static Sprite arrowSprite;
    private static Sprite shieldSprite;
    private static bool spritesCreated = false;

    private static void EnsureSprites()
    {
        if (spritesCreated) return;
        circleSprite = CollisionSpriteFactory.CreateCircleSprite(32, Color.white);
        starSprite = CollisionSpriteFactory.CreateStarSprite(32, Color.white, 6);
        star5Sprite = CollisionSpriteFactory.CreateStarSprite(32, Color.white, 5);
        arrowSprite = CollisionSpriteFactory.CreateArrowSprite(32, Color.white);
        shieldSprite = CollisionSpriteFactory.CreateShieldSprite(32, Color.white);
        spritesCreated = true;
    }

    // ── 현재 표시 중인 VFX 타입 ──
    private CollisionVFXType currentType;

    // 우선순위: Crit > Hit > Dodge > Slingshot
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
        // ★ 우선순위 보호: 높은 VFX 표시 중이면 낮은 건 스킵
        if (timer > 0f && GetPriority(type) < GetPriority(currentType))
            return;

        currentType = type;

        EnsureSprites();

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
                bgColor = new Color(1f, 0.2f, 0.1f, 0.9f);    // 빨강
                iconColor = new Color(1f, 1f, 0.3f, 1f);        // 노랑
                icon = gs.vfxHitIcon != null ? gs.vfxHitIcon : starSprite;
                text = "HIT!";
                break;

            case CollisionVFXType.Dodge:
                bgColor = new Color(0.2f, 0.6f, 1f, 0.9f);     // 파랑
                iconColor = new Color(0.8f, 1f, 1f, 1f);        // 연하늘
                icon = gs.vfxDodgeIcon != null ? gs.vfxDodgeIcon : shieldSprite;
                text = "DODGE!";
                break;

            case CollisionVFXType.Slingshot:
                bgColor = new Color(0.1f, 0.9f, 0.3f, 0.9f);   // 초록
                iconColor = new Color(1f, 1f, 0.5f, 1f);        // 연노랑
                icon = gs.vfxSlingshotIcon != null ? gs.vfxSlingshotIcon : arrowSprite;
                text = "CHASE!";
                break;

            case CollisionVFXType.Crit:
                bgColor = new Color(0.5f, 0.1f, 0.8f, 0.9f);   // 보라
                iconColor = new Color(1f, 1f, 0.3f, 1f);        // 노랑
                icon = star5Sprite;                               // ⭐ 5각 별
                text = "BOOST!";
                break;

            default:
                bgColor = Color.white;
                iconColor = Color.white;
                icon = circleSprite;
                text = "?";
                break;
        }

        // ── 커스텀 아이콘이면 tint 안 먹이기 ──
        bool isCustomIcon = (type == CollisionVFXType.Hit && gs.vfxHitIcon != null)
            || (type == CollisionVFXType.Dodge && gs.vfxDodgeIcon != null)
            || (type == CollisionVFXType.Slingshot && gs.vfxSlingshotIcon != null);

        // ── 적용 ──
        bgSprite.sprite = circleSprite;
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

        // 부모 스케일 반전 보정
        FixFlip();
    }

    private void CreateVFXObjects(GameSettings gs)
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

        // 폰트 적용 (GameSettings에 설정된 경우)
        if (gs != null && gs.mainFont != null)
            label.font = gs.mainFont;

        // 텍스트도 sorting 맞추기
        var labelRenderer = labelObj.GetComponent<MeshRenderer>();
        if (labelRenderer != null)
        {
            labelRenderer.sortingOrder = 102;
            // TextMesh는 font.material도 설정해야 글자가 보임
            if (label.font != null && label.font.material != null)
                labelRenderer.material = label.font.material;
        }
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

        float progress = 1f - (timer / totalDuration); // 0 → 1

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

        // 부모 반전 보정
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
