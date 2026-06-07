using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아케이드 이름 입력 모달 빌드 (코드 폴백). BetAmountModal/Spec028UI 패턴 준수.
/// 백드롭(raycast 차단) + 중앙 패널 + 3슬롯(A-Z) + ▲▼ + 확인. 키보드는 NameEntryModal.Update 폴링.
/// </summary>
public partial class SceneBootstrapper
{
    private NameEntryModal _nameEntryModal;

    private void BuildNameEntryModal(Transform root)
    {
        GameObject modalRoot = new GameObject("NameEntryModal");
        modalRoot.transform.SetParent(root, false);
        AddFullRect(modalRoot);

        // 백드롭 — 전체화면, raycastTarget=true로 뒤 클릭(GameOver 전체버튼) 차단
        var backdrop = MkImage(modalRoot.transform, "Backdrop",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.7f));
        backdrop.raycastTarget = true;

        // 중앙 패널
        var panel = MkImage(modalRoot.transform, "Panel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 440),
            new Color(0.12f, 0.10f, 0.18f, 0.95f));

        // 타이틀 / 점수 / 가이드
        var title = MkText(panel.transform, "NEW RECORD!",
            new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 60),
            40, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f));
        var score = MkText(panel.transform, "",
            new Vector2(0.5f, 0.73f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 44),
            28, TextAnchor.MiddleCenter, Color.white);
        var guide = MkText(panel.transform, "",
            new Vector2(0.5f, 0.13f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 36),
            20, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f));

        // 3슬롯 (강조 Image + 글자 Text + ▲▼)
        Text[] slots = new Text[3];
        Image[] highlights = new Image[3];
        Button[] ups = new Button[3];
        Button[] downs = new Button[3];
        float[] xs = { -130f, 0f, 130f };
        for (int i = 0; i < 3; i++)
        {
            highlights[i] = MkImage(panel.transform, "SlotHL" + i,
                new Vector2(0.5f, 0.47f), new Vector2(0.5f, 0.47f), new Vector2(xs[i], 0), new Vector2(86, 108),
                new Color(1f, 0.85f, 0.4f, 0.45f));   // 금색 강조 (선택 슬롯만 enabled)
            slots[i] = MkText(panel.transform, "A",
                new Vector2(0.5f, 0.47f), new Vector2(0.5f, 0.5f), new Vector2(xs[i], 0), new Vector2(78, 96),
                66, TextAnchor.MiddleCenter, Color.white);
            ups[i]   = MkBtnAt(panel.transform, "▲", new Vector2(xs[i],  78), new Vector2(64, 40));
            downs[i] = MkBtnAt(panel.transform, "▼", new Vector2(xs[i], -78), new Vector2(64, 40));
        }

        // 확인 버튼 (초록)
        var confirm = MkBtnAt(panel.transform, "확인", new Vector2(0, -180), new Vector2(200, 52));
        if (confirm.image != null) confirm.image.color = new Color(0.3f, 0.6f, 0.3f);

        _nameEntryModal = modalRoot.AddComponent<NameEntryModal>();
        _nameEntryModal.SetReferences(backdrop.gameObject, title, score, guide, slots, highlights, ups, downs, confirm);
        modalRoot.SetActive(false);
    }

    /// <summary>중앙 기준 픽셀 위치·크기 버튼 (MkSimpleButton은 offset=0이라 rt 후처리).</summary>
    private Button MkBtnAt(Transform parent, string label, Vector2 pos, Vector2 size)
    {
        var b = MkSimpleButton(parent, label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var rt = b.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return b;
    }

    /// <summary>
    /// 종료(Finish/GameOver) 시 점수가 Top100 자격이면 이름 입력 모달 → 저장 → 종료화면.
    /// 미자격/기록없음이면 바로 종료화면(미저장). 모달 비동기 → 종료화면 진입을 콜백까지 지연.
    /// </summary>
    private void TryNameEntryThen(System.Action showEndScreen)
    {
        var sm = ScoreManager.Instance;
        int score = sm != null ? sm.CurrentGameScore : 0;
        bool hasRecord = sm != null && sm.RoundHistory.Count > 0;
        if (hasRecord && LeaderboardData.Qualifies(score) && _nameEntryModal != null)
        {
            _nameEntryModal.Show(score, (name) =>
            {
                ScoreManager.Instance?.SaveToLeaderboard(name);
                showEndScreen();
            });
        }
        else
        {
            showEndScreen();   // 자격 미달/기록 없음 → 바로 종료화면 (미저장)
        }
    }
}
