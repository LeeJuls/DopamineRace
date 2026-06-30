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

        // 오버레이 격리 — 메인캔버스(100) 위로 (Finish 요약 위에 단독 표시). 모달 패밀리(Spec028UI) 통일.
        var ovCanvas = modalRoot.AddComponent<Canvas>();
        ovCanvas.overrideSorting = true;
        ovCanvas.sortingOrder = 1000;
        modalRoot.AddComponent<GraphicRaycaster>();

        // 백드롭 — 전체화면, raycastTarget=true로 뒤 클릭(GameOver 전체버튼) 차단. 불투명 0.92로 뒤 Finish 가림.
        var backdrop = MkImage(modalRoot.transform, "Backdrop",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.92f));
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

    /// <summary>
    /// 제출 후 rank 결과를 모달 팝업으로 표시(선택적) — 터치 시 onClosed.
    /// S5: 등수의 1차 표시 수단은 요약 화면의 배지(RefreshMyRankBadge). 이 팝업은 보조.
    /// rank=0(원격없음/실패)이면 로컬 폴백 등수를 계산해 표기(배지와 동일 정직 표기).
    /// </summary>
    private void ShowRankResultInModal(int rank, System.Action onClosed)
    {
        if (_nameEntryModal == null) { onClosed?.Invoke(); return; }
        // 흰 BG_01 배경 가독성 — 결과색 다크 앰버 계열 (프리팹 패밀리 통일, NameEntryModalDecorator 참조)
        string text;
        Color  color;
        if (rank > 0)
        {
            bool qualified = rank <= 100;
            string key = qualified ? "str.nameentry.rank_qualified" : "str.nameentry.rank_disqualified";
            text  = Loc.Get(key, rank);
            color = qualified ? new Color(0.55f, 0.30f, 0f) : new Color(0.70f, 0.35f, 0f);
            Debug.Log($"[Leaderboard] rank={rank} ({(qualified ? "등재" : "미등재")}) — {text}");
        }
        else
        {
            // 원격 rank 없음 → 로컬 폴백 등수 (배지의 RefreshMyRankBadge(0) 경로와 일치)
            int score     = ScoreManager.Instance != null ? ScoreManager.Instance.LeaderboardScore : 0;
            int localRank = LeaderboardData.GetRank(score);
            text  = localRank > 0 ? Loc.Get("str.summary.local_rank", localRank)
                                  : Loc.Get("str.summary.rank_out");
            color = new Color(0.70f, 0.35f, 0f);
            Debug.Log($"[Leaderboard] 원격 rank 없음 → 로컬 폴백 등수={localRank} — {text}");
        }
        _nameEntryModal.ShowResult(text, color, onClosed);   // 팝업 → 터치 시 onClosed
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
    /// S5: 요약(Finish) 화면이 이미 떠 있는 상태에서 호출됨.
    /// 자격(Top100 && 기록존재 && 모달존재)이면 이름 입력 모달을 요약 위에 띄워 등록 → 확정 후
    /// onRankResolved(rank)로 요약의 순위 배지를 글로벌등수로 갱신.
    /// 미자격/기록없음/오프라인/제출실패 4분기 모두 onRankResolved(rank) 1회 보장
    /// (rank=0이면 RefreshMyRankBadge가 GetRank 로컬 폴백으로 등수 표시 — 핵심 버그 해소).
    /// </summary>
    private void TryNameEntryThen(System.Action<int> onRankResolved)
    {
        var sm = ScoreManager.Instance;
        int score = sm != null ? sm.LeaderboardScore : 0;   // 리더보드 = 누적 스톤 (SPEC-028 R5)
        bool hasRecord = sm != null && sm.RoundHistory.Count > 0;

        // ── 미자격/기록없음/모달없음 → 모달 생략. 요약은 이미 RefreshMyRankBadge(0)로 로컬등수 표시 중.
        if (!(hasRecord && LeaderboardData.Qualifies(score) && _nameEntryModal != null))
        {
            onRankResolved?.Invoke(0);   // 로컬 폴백 등수로 배지 확정(멱등)
            return;
        }

        // ── 자격: 요약 위에 등록 모달 → 확정 콜백
        _nameEntryModal.Show(score, (name) =>
        {
            var sm2 = ScoreManager.Instance;
            sm2?.SaveToLeaderboard(name);   // 로컬 캐시(멱등) — 항상 성공·즉시

            var svc = LeaderboardService.Instance;

            // 모달 닫고 배지 갱신(요약은 이미 떠 있으므로 여기서는 등수만 확정)
            System.Action<int> resolve = (rank) => {
                _nameEntryModal?.ForceClose();
                onRankResolved?.Invoke(rank);   // rank>0 글로벌 / 0이면 배지가 로컬 폴백
            };

            if (sm2 != null && svc != null && svc.RemoteEnabled)
            {
                var entry = sm2.BuildLeaderboardEntry(name);
                svc.SubmitScore(entry, sm2.GameNonce, (ok, rank) =>
                {
                    if (!ok)
                    {
                        // ── 제출 실패 → 로컬 폴백 등수로 배지 채움(요약 항상 노출)
                        Debug.LogWarning("[Leaderboard] " + Loc.Get("str.leaderboard.submit_failed"));
                        resolve(0);
                        return;
                    }
                    // 캐시 갱신은 백그라운드 — 배지 갱신을 막지 않음
                    svc.ForceRefetch(100, _ => { }, _ => { });
                    // 보조 rank 팝업(선택) → 터치 시 배지 갱신. rank=0이어도 폴백 표기.
                    ShowRankResultInModal(rank, () => resolve(rank));
                });
            }
            else
            {
                // ── 오프라인(원격 비활성) → 로컬 폴백 등수로 배지 채움
                resolve(0);
            }
        });
    }
}
