using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 블록 디졸브/빌드업 씬 전환 매니저
/// - DontDestroyOnLoad 싱글톤
/// - 16×10 격자 Image 기반
/// - Canvas sortingOrder = 10000 (TrackTransition 9999 위)
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    // ══════════════════════════════════════
    //  설정
    // ══════════════════════════════════════
    private const int GRID_COLS = 20; // 화면 전환 가로 블록
    private const int GRID_ROWS = 12; // 화면 전환 세로 블록
    private const int TOTAL_BLOCKS = GRID_COLS * GRID_ROWS; // 160
    private const float DISSOLVE_DURATION = 1.2f;
    private const float BUILDUP_DURATION = 1.2f;
    private const float BLACK_HOLD_TIME = 0.25f;
    private const int CANVAS_SORT_ORDER = 10000;

    // ══════════════════════════════════════
    //  내부 참조
    // ══════════════════════════════════════
    private Canvas canvas;
    private Image[] blocks;
    private bool isTransitioning = false;

    // ══════════════════════════════════════
    //  초기화
    // ══════════════════════════════════════
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateCanvas();
        CreateGrid();
        canvas.gameObject.SetActive(false);
    }

    private void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("TransitionCanvas");
        canvasObj.transform.SetParent(transform, false);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CANVAS_SORT_ORDER;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void CreateGrid()
    {
        blocks = new Image[TOTAL_BLOCKS];

        // ceil 처리: 빈틈 없이 화면 커버
        float blockW = Mathf.Ceil(1920f / GRID_COLS);
        float blockH = Mathf.Ceil(1080f / GRID_ROWS);

        for (int i = 0; i < TOTAL_BLOCKS; i++)
        {
            int col = i % GRID_COLS;
            int row = i / GRID_COLS;

            GameObject blockObj = new GameObject($"Block_{col}_{row}");
            blockObj.transform.SetParent(canvas.transform, false);

            Image img = blockObj.AddComponent<Image>();
            img.color = Color.black;

            RectTransform rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(col * blockW, row * blockH);
            rt.sizeDelta = new Vector2(blockW, blockH);

            blocks[i] = img;
        }
    }

    // ══════════════════════════════════════
    //  공개 API
    // ══════════════════════════════════════

    /// <summary>
    /// 블록 디졸브 전환으로 씬 로드
    /// </summary>
    public void TransitionToScene(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionSequence(sceneName));
    }

    public bool IsTransitioning => isTransitioning;

    // ══════════════════════════════════════
    //  전환 시퀀스
    // ══════════════════════════════════════
    private IEnumerator TransitionSequence(string sceneName)
    {
        isTransitioning = true;

        // ① 블록 디졸브 아웃 (타이틀 화면이 블록 단위로 가려짐)
        yield return StartCoroutine(DissolveOut());

        // ② 검은 화면 유지
        yield return new WaitForSeconds(BLACK_HOLD_TIME);

        // ③ 씬 로드
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;

        // ④ 한 프레임 대기 (새 씬 Start() 실행 보장)
        yield return null;

        // ⑤ 블록 빌드업 인 (새 화면이 블록 단위로 드러남)
        yield return StartCoroutine(BuildupIn());

        // ⑥ 완료 후 게임 BGM 시작
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.PlayBGM("Audio/BGM");
            BGMManager.Instance.SetVolume(0f);
            float bgmVol = GameSettings.Instance != null ? GameSettings.Instance.bgmVolume : 0.5f;
            BGMManager.Instance.FadeIn(bgmVol, 1.5f);
        }

        canvas.gameObject.SetActive(false);
        isTransitioning = false;

        Debug.Log($"[SceneTransition] 전환 완료 → {sceneName}");
    }

    /// <summary>
    /// 블록 디졸브 아웃: 검은 블록이 랜덤 순서로 나타남 (화면을 가림)
    /// </summary>
    private IEnumerator DissolveOut()
    {
        // 모든 블록 투명으로 시작
        SetAllBlocksAlpha(0f);
        canvas.gameObject.SetActive(true);

        // 랜덤 순서 생성
        int[] order = GetShuffledIndices();

        // 시간 간격 계산
        float interval = DISSOLVE_DURATION / TOTAL_BLOCKS;

        for (int i = 0; i < TOTAL_BLOCKS; i++)
        {
            // 블록을 불투명하게 (팍 나타남)
            blocks[order[i]].color = Color.black;
            yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>
    /// 블록 빌드업 인: 검은 블록이 랜덤 순서로 사라짐 (화면이 드러남)
    /// </summary>
    private IEnumerator BuildupIn()
    {
        // 모든 블록 불투명으로 시작
        SetAllBlocksAlpha(1f);
        canvas.gameObject.SetActive(true);

        // 랜덤 순서 (디졸브와 다른 시드)
        int[] order = GetShuffledIndices();

        float interval = BUILDUP_DURATION / TOTAL_BLOCKS;

        for (int i = 0; i < TOTAL_BLOCKS; i++)
        {
            // 블록을 투명하게 (팍 사라짐)
            blocks[order[i]].color = Color.clear;
            yield return new WaitForSeconds(interval);
        }
    }

    // ══════════════════════════════════════
    //  유틸
    // ══════════════════════════════════════
    private void SetAllBlocksAlpha(float alpha)
    {
        Color c = alpha > 0.5f ? Color.black : Color.clear;
        for (int i = 0; i < TOTAL_BLOCKS; i++)
        {
            blocks[i].color = c;
        }
    }

    private int[] GetShuffledIndices()
    {
        int[] indices = new int[TOTAL_BLOCKS];
        for (int i = 0; i < TOTAL_BLOCKS; i++)
            indices[i] = i;

        // Fisher-Yates 셔플
        for (int i = TOTAL_BLOCKS - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = indices[i];
            indices[i] = indices[j];
            indices[j] = temp;
        }

        return indices;
    }
}
