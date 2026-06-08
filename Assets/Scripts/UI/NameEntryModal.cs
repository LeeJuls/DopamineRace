using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 아케이드 스타일 이름 입력 모달 (3자리 이니셜).
/// ↑↓: 글자 A-Z 순환 / ←→: 자리 이동(비순환) / Enter·확인: 저장. 취소 없음, 기본 AAA.
/// Top100 자격 시에만 표시 (SceneBootstrapper.OnStateChanged에서 분기).
/// 빌드: SceneBootstrapper.NameEntry.cs (코드 폴백) → SetReferences 주입.
/// </summary>
public class NameEntryModal : MonoBehaviour
{
    private GameObject _backdrop;
    private Text _titleText, _scoreText, _guideText, _rankResultText;
    private Text[] _slotTexts;
    private Image[] _slotHighlights;
    private Button[] _upButtons, _downButtons;
    private Button _confirmButton;

    private readonly int[] _letters = new int[3];   // 0-25 (A-Z)
    private int _currentSlot;
    private Action<string> _onConfirmed;
    private bool _isOpen;

    private bool _resultMode;          // 결과 팝업 표시 중(입력 UI 숨김, 터치 대기)
    private Action _onResultClosed;    // 결과 팝업 터치로 닫힌 뒤 호출

    /// <summary>모달 표시 중 여부 — SceneBootstrapper.Update에서 ESC/입력 가드용(결과 팝업 포함).</summary>
    public bool IsOpen => _isOpen || _resultMode;

    /// <summary>현재 조합된 이름 (테스트/검증용).</summary>
    public string CurrentName => "" + (char)('A' + _letters[0]) + (char)('A' + _letters[1]) + (char)('A' + _letters[2]);

    private void Awake() => gameObject.SetActive(false);

    /// <summary>SceneBootstrapper 코드 빌드에서 UI 참조 주입 + 버튼 배선.</summary>
    public void SetReferences(GameObject backdrop, Text title, Text score, Text guide, Text rankResult,
        Text[] slots, Image[] highlights, Button[] ups, Button[] downs, Button confirm)
    {
        _backdrop = backdrop; _titleText = title; _scoreText = score; _guideText = guide;
        _rankResultText = rankResult;
        _slotTexts = slots; _slotHighlights = highlights; _confirmButton = confirm;
        _upButtons = ups; _downButtons = downs;

        for (int i = 0; i < 3; i++)
        {
            int idx = i;   // 클로저 캡처
            if (ups != null && ups[i] != null) { ups[i].onClick.RemoveAllListeners(); ups[i].onClick.AddListener(() => ChangeLetter(idx, +1)); }
            if (downs != null && downs[i] != null) { downs[i].onClick.RemoveAllListeners(); downs[i].onClick.AddListener(() => ChangeLetter(idx, -1)); }
        }
        if (confirm != null) { confirm.onClick.RemoveAllListeners(); confirm.onClick.AddListener(Confirm); }
    }

    /// <summary>점수 표시 + 콜백 등록 후 모달 표시. 초기값 AAA.</summary>
    public void Show(int score, Action<string> onConfirmed)
    {
        _onConfirmed = onConfirmed;
        _letters[0] = _letters[1] = _letters[2] = 0;   // AAA
        _currentSlot = 0;
        _isOpen = true;
        _resultMode = false;
        SetInputActive(true);   // 결과 모드에서 재사용될 때 입력 UI 복원

        if (_titleText    != null) _titleText.text    = Loc.Get("str.nameentry.title");
        if (_guideText    != null) _guideText.text    = Loc.Get("str.nameentry.guide");
        if (_scoreText    != null) _scoreText.text    = Loc.Get("str.nameentry.score", score);
        if (_rankResultText != null) _rankResultText.text = "";   // 초기화
        if (_confirmButton != null)
        {
            var ct = _confirmButton.GetComponentInChildren<Text>();
            if (ct != null) ct.text = Loc.Get("str.nameentry.confirm");
            _confirmButton.interactable = true;
        }
        if (_backdrop != null) _backdrop.SetActive(true);
        gameObject.SetActive(true);
        RefreshSlots();
    }

    /// <summary>제출 결과를 팝업으로 표시 — 입력 UI 숨기고 rank 텍스트 중앙 확대, 터치/클릭 시 onClosed.</summary>
    public void ShowResult(string text, Color color, Action onClosed)
    {
        _onResultClosed = onClosed;
        _resultMode = true;
        _isOpen = false;            // 이름 입력 키 처리 중단
        SetInputActive(false);      // 슬롯·▲▼·확인 숨김 → 결과 팝업 느낌

        if (_rankResultText != null)
        {
            var rt = _rankResultText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.46f);   // 패널 중앙으로
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(520, 140);
            _rankResultText.fontSize = 28;
            _rankResultText.text = text;
            _rankResultText.color = color;
        }
        if (_guideText != null) _guideText.text = Loc.Get("str.nameentry.touch_continue");
    }

    /// <summary>입력 UI(슬롯·강조·▲▼·확인) 일괄 표시/숨김 — 결과 팝업 전환용.</summary>
    private void SetInputActive(bool on)
    {
        if (_slotTexts != null)      foreach (var t in _slotTexts)      if (t != null) t.gameObject.SetActive(on);
        if (_slotHighlights != null) foreach (var h in _slotHighlights) if (h != null) h.gameObject.SetActive(on);
        if (_upButtons != null)      foreach (var b in _upButtons)      if (b != null) b.gameObject.SetActive(on);
        if (_downButtons != null)    foreach (var b in _downButtons)    if (b != null) b.gameObject.SetActive(on);
        if (_confirmButton != null)  _confirmButton.gameObject.SetActive(on);
    }

    /// <summary>제출 완료 후 외부에서 명시적으로 닫기 (Confirm이 모달을 닫지 않으므로).</summary>
    public void ForceClose()
    {
        _isOpen = false;
        if (_backdrop != null) _backdrop.SetActive(false);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_resultMode)
        {
            // 결과 팝업: 마우스 클릭·터치·Enter/Space 중 무엇이든 닫기
            if (Input.GetMouseButtonDown(0)
                || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.Space)
                || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                CloseResult();
            return;
        }
        if (!_isOpen) return;
        if (Input.GetKeyDown(KeyCode.UpArrow))         ChangeLetter(_currentSlot, +1);
        else if (Input.GetKeyDown(KeyCode.DownArrow))  ChangeLetter(_currentSlot, -1);
        else if (Input.GetKeyDown(KeyCode.LeftArrow))  MoveSlot(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) MoveSlot(+1);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Confirm();
    }

    private void ChangeLetter(int slot, int delta)
    {
        if (slot < 0 || slot >= 3) return;
        _currentSlot = slot;
        _letters[slot] = ((_letters[slot] + delta) % 26 + 26) % 26;   // A-Z 순환
        RefreshSlots();
    }

    private void MoveSlot(int delta)
    {
        int next = _currentSlot + delta;
        if (next < 0 || next >= 3) return;   // 비순환 경계 (첫칸← / 끝칸→ 무시)
        _currentSlot = next;
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            if (_slotTexts != null && _slotTexts[i] != null)
                _slotTexts[i].text = ((char)('A' + _letters[i])).ToString();
            if (_slotHighlights != null && _slotHighlights[i] != null)
                _slotHighlights[i].enabled = (i == _currentSlot);   // 선택 슬롯 강조
        }
    }

    /// <summary>결과 팝업을 터치로 닫음 — 모달 종료 후 onClosed 콜백(종료화면 진입).</summary>
    private void CloseResult()
    {
        if (!_resultMode) return;
        _resultMode = false;
        var cb = _onResultClosed; _onResultClosed = null;
        ForceClose();
        cb?.Invoke();
    }

    private void Confirm()
    {
        if (!_isOpen) return;
        if (_confirmButton != null) _confirmButton.interactable = false;   // 연타 방지
        string nm = CurrentName;
        _isOpen = false;   // Update() 입력 차단, 모달은 ForceClose()로 닫힘
        if (_guideText != null) _guideText.text = Loc.Get("str.nameentry.submitting");
        _onConfirmed?.Invoke(nm);
    }
}
