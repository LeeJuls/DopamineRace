using UnityEngine;

/// <summary>
/// 도파민 통화 정의 (젤리·스톤).
/// 이름·아이콘은 디자이너가 별도 asset 인스턴스에서 관리.
/// SPEC-028 Step 1.1
/// </summary>
[CreateAssetMenu(fileName = "Currency", menuName = "DopamineRace/CurrencyItem")]
public class CurrencyItem : ScriptableObject
{
    [Tooltip("내부 식별자: 'jelly' / 'stone' 등")]
    public string id;

    [Tooltip("StringTable UID (예: 'str.currency.jelly.name')")]
    public string nameKey;

    [Tooltip("StringTable UID (예: 'str.currency.jelly.desc')")]
    public string descKey;

    [Tooltip("디자이너 제공 아이콘 — Phase 2에서 디자이너 PNG로 교체")]
    public Sprite icon;

    [Tooltip("UI 강조 색상 (보유량 라벨, 미리보기 등)")]
    public Color defaultColor = Color.white;

    /// <summary>
    /// 다국어 표시 이름 — nameKey가 비어있으면 id 반환
    /// </summary>
    public string GetDisplayName()
    {
        if (string.IsNullOrEmpty(nameKey)) return id;
        return Loc.Get(nameKey);
    }

    /// <summary>
    /// 다국어 설명 — descKey가 비어있으면 빈 문자열
    /// </summary>
    public string GetDescription()
    {
        if (string.IsNullOrEmpty(descKey)) return "";
        return Loc.Get(descKey);
    }
}
