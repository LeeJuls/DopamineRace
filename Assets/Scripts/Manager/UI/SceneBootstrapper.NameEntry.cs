using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아케이드 이름 입력 모달 빌드 (코드 폴백). BetAmountModal/Spec028UI 패턴 준수.
/// 백드롭(raycast 차단) + 중앙 패널 + 3슬롯(A-Z) + ▲▼ + 확인. 키보드는 NameEntryModal.Update 폴링.
/// </summary>
public partial class SceneBootstrapper
{
    [SerializeField] private NameEntryModal _nameEntryModalPrefab;
    private NameEntryModal _nameEntryModal;

    private void BuildNameEntryModal(Transform root)
    {
        // 프리팹 우선 — Inspector에서 NameEntryModal.prefab 할당 시 사용
        if (_nameEntryModalPrefab != null)
        {
            var go = Instantiate(_nameEntryModalPrefab.gameObject);
            go.name = "NameEntryModal";
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
            _nameEntryModal = go.GetComponent<NameEntryModal>();
            return;
        }

        // 코드 폴백 — 프리팹 미할당 시 기존 방식으로 생성
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

        // 타이틀 / 점수 / 순위결과 / 가이드
        var title = MkText(panel.transform, "NEW RECORD!",
            new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 60),
            40, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f));
        var score = MkText(panel.transform, "",
            new Vector2(0.5f, 0.73f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 44),
            28, TextAnchor.MiddleCenter, Color.white);
        // 순위 결과 (제출 후 rank 표시 — 평소 빈 문자열)
        var rankResult = MkText(panel.transform, "",
            new Vector2(0.5f, 0.625f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 40),
            18, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f));
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
        _nameEntryModal.SetReferences(backdrop.gameObject, title, score, guide, rankResult, slots, highlights, ups, downs, confirm);
        modalRoot.SetActive(false);
    }

    /// <summary>제출 후 rank 결과를 팝업으로 표시 — 터치 시 onClosed(종료화면 진입).</summary>
    private void ShowRankResultInModal(int rank, System.Action onClosed)
    {
        if (_nameEntryModal == null) { onClosed?.Invoke(); return; }
        bool qualified = rank <= 100;
        string key   = qualified ? "str.nameentry.rank_qualified" : "str.nameentry.rank_disqualified";
        string text  = Loc.Get(key, rank);
        Color  color = qualified ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 0.7f, 0.2f);
        _nameEntryModal.ShowResult(text, color, onClosed);   // 팝업 → 터치 시 onClosed
        Debug.Log($"[Leaderboard] rank={rank} ({(qualified ? "등재" : "미등재")}) — {text}");
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
        int score = sm != null ? sm.LeaderboardScore : 0;   // 리더보드 = 누적 스톤 (SPEC-028 R5)
        bool hasRecord = sm != null && sm.RoundHistory.Count > 0;
        if (hasRecord && LeaderboardData.Qualifies(score) && _nameEntryModal != null)
        {
            _nameEntryModal.Show(score, (name) =>
            {
                var sm2 = ScoreManager.Instance;
                sm2?.SaveToLeaderboard(name);   // 로컬 캐시(멱등) — 항상 성공·즉시

                // 원격 제출 — 응답 rank 기준 결과 메시지 + ForceRefetch 후 종료화면 진입.
                // 실패/rank=0 시에도 showEndScreen 보장(블로킹 없음).
                var svc = LeaderboardService.Instance;

                // 모달 닫고 종료화면 진입 (항상 이 경로로 종료)
                System.Action finishEntry = () => {
                    _nameEntryModal?.ForceClose();
                    showEndScreen();
                };

                if (sm2 != null && svc != null && svc.RemoteEnabled)
                {
                    var entry = sm2.BuildLeaderboardEntry(name);
                    svc.SubmitScore(entry, sm2.GameNonce, (ok, rank) =>
                    {
                        if (!ok)
                        {
                            Debug.LogWarning("[Leaderboard] " + Loc.Get("str.leaderboard.submit_failed"));
                            finishEntry();
                            return;
                        }
                        // 캐시 갱신은 백그라운드로 — 종료화면 진입을 막지 않음
                        svc.ForceRefetch(100, _ => { }, _ => { });
                        // rank 팝업 → 사용자가 터치하면 종료화면 진입. rank 없으면 바로 종료.
                        if (rank > 0) ShowRankResultInModal(rank, showEndScreen);
                        else          finishEntry();
                    });
                }
                else
                {
                    finishEntry();
                }
            });
        }
        else
        {
            showEndScreen();   // 자격 미달/기록 없음 → 바로 종료화면 (미저장)
        }
    }
}
