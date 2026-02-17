using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 트랙 웨이포인트 편집기
/// E키: 편집 모드 ON/OFF
/// 마우스 드래그: 웨이포인트 이동
/// S키: 파일로 저장 (현재 트랙 기준)
/// 
/// 저장 파일:
///   normal (기본): track_waypoints.json
///   기타 트랙:     track_waypoints_{trackId}.json
/// </summary>
public class WaypointEditor : MonoBehaviour
{
    private bool editMode = false;
    private bool initialized = false;
    private List<GameObject> markers = new List<GameObject>();
    private GameObject editorRoot;
    private GameObject dragging = null;
    private Vector3 dragOffset;
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        Debug.Log("═══ 트랙 편집기 준비됨 ═══");
        Debug.Log("E키: 편집 ON/OFF | S키: 파일 저장");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            editMode = !editMode;
            if (editMode && !initialized) InitEditor();
            if (editorRoot != null) editorRoot.SetActive(editMode);

            string trackLabel = GetCurrentTrackId() ?? "기본";
            Debug.Log(editMode
                ? "트랙 편집 ON [" + trackLabel + "]"
                : "트랙 편집 OFF");
        }

        if (!editMode) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = -0.5f;

        if (Input.GetMouseButtonDown(0))
        {
            Collider2D hit = Physics2D.OverlapPoint(mouseWorld);
            if (hit != null && hit.gameObject.name.StartsWith("WPMarker_"))
            {
                dragging = hit.gameObject;
                dragOffset = dragging.transform.position - mouseWorld;
            }
        }

        if (Input.GetMouseButton(0) && dragging != null)
            dragging.transform.position = mouseWorld + dragOffset;

        if (Input.GetMouseButtonUp(0))
            dragging = null;

        if (Input.GetKeyDown(KeyCode.S))
            SaveToFile();
    }

    /// <summary>
    /// 현재 트랙 ID 가져오기 (TrackDatabase 연동)
    /// </summary>
    private string GetCurrentTrackId()
    {
        if (TrackDatabase.Instance != null && TrackDatabase.Instance.CurrentTrackInfo != null)
            return TrackDatabase.Instance.CurrentTrackInfo.trackId;
        return null;
    }

    private void InitEditor()
    {
        if (editorRoot != null)
        {
            Destroy(editorRoot);
            markers.Clear();
        }

        editorRoot = new GameObject("TrackEditorRoot");
        var data = TrackPathData.Load(GetCurrentTrackId());

        for (int i = 0; i < data.Count; i++)
        {
            Vector2 pos = data.GetPoint(i);
            markers.Add(CreateMarker(i, new Vector3(pos.x, pos.y, -0.5f)));
        }
        initialized = true;
    }

    /// <summary>
    /// 편집 모드 리로드 (트랙 변경 시 호출)
    /// </summary>
    public void ReloadForTrack()
    {
        if (!editMode) return;
        initialized = false;
        InitEditor();
    }

    private GameObject CreateMarker(int index, Vector3 pos)
    {
        GameObject marker = new GameObject("WPMarker_" + index);
        marker.transform.SetParent(editorRoot.transform);
        marker.transform.position = pos;

        SpriteRenderer sr = marker.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(32, 32);
        float r = 16f;
        for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
                tex.SetPixel(x, y, d < r - 1 ? Color.yellow : Color.clear);
            }
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 64f);
        sr.sortingOrder = 60;

        GameObject lb = new GameObject("Label");
        lb.transform.SetParent(marker.transform);
        lb.transform.localPosition = new Vector3(0, 0.3f, 0);
        TextMesh tm = lb.AddComponent<TextMesh>();
        tm.text = (index + 1).ToString();
        tm.characterSize = 0.15f;
        tm.fontSize = 48;
        tm.fontStyle = FontStyle.Bold;
        tm.color = Color.red;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        lb.GetComponent<MeshRenderer>().sortingOrder = 61;

        CircleCollider2D col = marker.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;

        return marker;
    }

    private void SaveToFile()
    {
        Transform[] transforms = new Transform[markers.Count];
        for (int i = 0; i < markers.Count; i++)
            transforms[i] = markers[i].transform;

        string trackId = GetCurrentTrackId();
        var data = TrackPathData.FromTransforms(transforms);
        data.Save(trackId);
    }
}
