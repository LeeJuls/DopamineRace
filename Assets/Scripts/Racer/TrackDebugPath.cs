using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 트랙 경로 디버그 표시 (별도 컴포넌트)
/// 
/// 사용법:
/// 1. SceneBootstrapper.cs에서 주석 해제:
///    track.AddComponent<TrackDebugPath>();
/// 2. Play 실행
/// 3. D키: 경로 표시 ON/OFF
/// 
/// 테스트 완료 후 주석 처리하면 됩니다.
/// </summary>
public class TrackDebugPath : MonoBehaviour
{
    private bool showing = false;
    private GameObject pathRoot;

    private void Start()
    {
        Debug.Log("═══ 경로 디버그 준비됨 ═══");
        Debug.Log("D키: 경로 표시 ON/OFF");
        Debug.Log("═══════════════════════");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            showing = !showing;
            if (showing)
                ShowPath();
            else
                HidePath();
            Debug.Log(showing ? "경로 표시 ON" : "경로 표시 OFF");
        }
    }

    private void ShowPath()
    {
        if (pathRoot != null) return;

        var wpParent = GameObject.Find("Waypoints");
        if (wpParent == null) return;

        pathRoot = new GameObject("DebugPathRoot");

        // 경로 선
        LineRenderer lr = pathRoot.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.green;
        lr.endColor = Color.green;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.useWorldSpace = true;
        lr.sortingOrder = 50;

        var wps = new List<Transform>();
        foreach (Transform child in wpParent.transform)
            wps.Add(child);

        lr.positionCount = wps.Count + 1;
        for (int i = 0; i < wps.Count; i++)
        {
            lr.SetPosition(i, wps[i].position + new Vector3(0, 0, -0.1f));
            Debug.Log("WP " + i + ": " + wps[i].position);

            // 번호 라벨
            GameObject label = new GameObject("WPLabel_" + i);
            label.transform.SetParent(pathRoot.transform);
            label.transform.position = wps[i].position + new Vector3(0, 0.2f, 0);
            TextMesh tm = label.AddComponent<TextMesh>();
            tm.text = i.ToString();
            tm.characterSize = 0.15f;
            tm.fontSize = 48;
            tm.fontStyle = FontStyle.Bold;
            tm.color = Color.yellow;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            label.GetComponent<MeshRenderer>().sortingOrder = 51;
        }
        lr.SetPosition(wps.Count, wps[0].position + new Vector3(0, 0, -0.1f));
    }

    private void HidePath()
    {
        if (pathRoot != null)
        {
            Destroy(pathRoot);
            pathRoot = null;
        }
    }
}
