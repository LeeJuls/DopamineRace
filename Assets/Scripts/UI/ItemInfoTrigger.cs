using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 아이템 아이콘 클릭 시 <see cref="ItemInfoPopup"/>을 띄우는 모듈식 트리거.
/// 도파민 젤리·스톤 등 설명이 필요한 모든 아이템 UI 노드에 부착 가능.
/// nameKey·descKey만 지정하면 동작 → 코드 수정 없이 새 아이템에 재사용.
///
/// ※ 클릭 감지를 위해 같은 GameObject에 raycastTarget=true 인 Graphic(Image 등)이 필요.
///    (CurrencyHeader의 JellyContainer/StoneContainer에는 투명 raycast Image를 부착)
/// SPEC-050
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ItemInfoTrigger : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("아이템 이름 StringTable 키 (예: str.dopamine.item.name.dopaminejelly)")]
    [SerializeField] private string itemNameKey;

    [Tooltip("아이템 설명 StringTable 키 (예: str.dopamine.item.desc.dopaminejelly)")]
    [SerializeField] private string itemDescKey;

    /// <summary>외부(에디터 스크립트·코드)에서 키 주입.</summary>
    public void SetKeys(string nameKey, string descKey)
    {
        itemNameKey = nameKey;
        itemDescKey = descKey;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 버튼과 동일한 클릭 효과음 (SFXManager는 Button만 자동 등록하므로 수동 호출)
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayClick();

        var popup = ItemInfoPopup.Instance;
        if (popup == null)
        {
            Debug.LogWarning("[ItemInfoTrigger] ItemInfoPopup.Instance 없음 — 씬에 ItemInfoPopup 프리팹 미배치");
            return;
        }

        // 같은 아이템 재클릭 → 토글로 닫기
        if (popup.IsShowing && popup.CurrentNameKey == itemNameKey)
            popup.Hide();
        else
            popup.Show(itemNameKey, itemDescKey);
    }
}
