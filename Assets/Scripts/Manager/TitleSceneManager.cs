using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 타이틀 씬 전체 관리
/// - Phase 1: 빈 껍데기 + 입력 감지 → SampleScene 전환
/// - Phase 2+: 비주얼, 캐릭터, BGM 등 추가 예정
/// </summary>
public class TitleSceneManager : MonoBehaviour
{
    private bool isTransitioning = false;

    /// <summary>씬 로드 직후 입력 방지 (안전장치)</summary>
    private float inputDelay = 0.5f;
    private float elapsedTime = 0f;

    private void Start()
    {
        // 카메라 설정
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.backgroundColor = Color.black;
        }

        Debug.Log("[TitleScene] 타이틀 씬 시작");
    }

    private void Update()
    {
        if (isTransitioning) return;

        elapsedTime += Time.deltaTime;
        if (elapsedTime < inputDelay) return;

        // 아무 입력 감지
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || HasTouchBegan())
        {
            StartTransition();
        }
    }

    private bool HasTouchBegan()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            return true;
        return false;
    }

    private void StartTransition()
    {
        isTransitioning = true;
        Debug.Log("[TitleScene] 전환 시작 → SampleScene");
        StartCoroutine(LoadSampleScene());
    }

    private IEnumerator LoadSampleScene()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync("SampleScene");
        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            yield return null;
        }

        Debug.Log("[TitleScene] SampleScene 로드 완료");
    }
}
