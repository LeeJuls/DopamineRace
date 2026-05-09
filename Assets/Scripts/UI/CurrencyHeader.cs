using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 베팅 화면 최상단 헤더의 도파민 젤리·스톤 보유량 표시.
/// WalletManager.OnChanged 이벤트 구독 → 즉시 갱신.
///
/// 레이아웃: 🟦 100  💎 0  형태로 좌→우 배치.
/// 디자이너 아이콘은 CurrencyItem.icon 통해 교체 가능.
///
/// SPEC-028 Step 2.1
/// </summary>
public class CurrencyHeader : MonoBehaviour
{
    [Header("표시 라벨")]
    [SerializeField] private Text jellyText;
    [SerializeField] private Text stoneText;

    [Header("아이콘 (선택, 디자이너 PNG 적용 시)")]
    [SerializeField] private Image jellyIcon;
    [SerializeField] private Image stoneIcon;

    [Header("CurrencyItem 참조 (Resources/Items/)")]
    [SerializeField] private CurrencyItem jellyItem;
    [SerializeField] private CurrencyItem stoneItem;

    private void OnEnable()
    {
        if (WalletManager.Instance != null)
        {
            WalletManager.Instance.OnChanged += Refresh;
        }
        TryAutoLoadCurrencyItems();
        Refresh();
    }

    private void OnDisable()
    {
        if (WalletManager.Instance != null)
        {
            WalletManager.Instance.OnChanged -= Refresh;
        }
    }

    /// <summary>
    /// CurrencyItem이 Inspector로 주입되지 않은 경우 Resources에서 자동 로드.
    /// </summary>
    private void TryAutoLoadCurrencyItems()
    {
        if (jellyItem == null)
            jellyItem = Resources.Load<CurrencyItem>("Items/DopamineJelly");
        if (stoneItem == null)
            stoneItem = Resources.Load<CurrencyItem>("Items/DopamineStone");

        // 아이콘 자동 적용 (디자이너 제공 sprite가 있으면)
        if (jellyIcon != null && jellyItem != null && jellyItem.icon != null)
            jellyIcon.sprite = jellyItem.icon;
        if (stoneIcon != null && stoneItem != null && stoneItem.icon != null)
            stoneIcon.sprite = stoneItem.icon;
    }

    /// <summary>
    /// Wallet 값 → 라벨 즉시 갱신.
    /// 외부에서 직접 호출 가능 (디버그/테스트용).
    /// </summary>
    public void Refresh()
    {
        var wallet = WalletManager.Instance;
        int jelly = wallet != null ? wallet.Jelly : 0;
        int stone = wallet != null ? wallet.Stone : 0;

        if (jellyText != null) jellyText.text = jelly.ToString();
        if (stoneText != null) stoneText.text = stone.ToString();
    }

    /// <summary>
    /// 외부 코드 (SceneBootstrapper)가 라벨/아이콘을 동적으로 주입할 때 사용.
    /// </summary>
    public void SetReferences(Text jellyText, Text stoneText, Image jellyIcon = null, Image stoneIcon = null)
    {
        this.jellyText = jellyText;
        this.stoneText = stoneText;
        this.jellyIcon = jellyIcon;
        this.stoneIcon = stoneIcon;
        TryAutoLoadCurrencyItems();
        Refresh();
    }
}
