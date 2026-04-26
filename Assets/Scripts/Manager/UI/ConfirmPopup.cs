using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 재사용 가능한 확인 팝업.
/// Show(message, onConfirm) 으로 열고, 예/아니오 버튼으로 닫힘.
/// </summary>
public class ConfirmPopup : MonoBehaviour
{
    [SerializeField] private Text messageLabel;
    [SerializeField] private Text yesLabel;
    [SerializeField] private Text noLabel;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private System.Action _onConfirm;

    private void Start()
    {
        if (yesButton != null)
            yesButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                _onConfirm?.Invoke();
            });

        if (noButton != null)
            noButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    /// <summary>
    /// 확인 팝업 표시.
    /// </summary>
    /// <param name="message">본문 메시지</param>
    /// <param name="onConfirm">예 버튼 콜백</param>
    public void Show(string message, System.Action onConfirm)
    {
        if (messageLabel != null)
            messageLabel.text = message;
        if (yesLabel != null)
            yesLabel.text = Loc.Get("str.ui.option.yes");
        if (noLabel != null)
            noLabel.text = Loc.Get("str.ui.option.no");

        _onConfirm = onConfirm;
        gameObject.SetActive(true);
    }
}
