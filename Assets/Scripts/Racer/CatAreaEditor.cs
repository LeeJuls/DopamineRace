using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 고양이 배회 영역(폴리곤) 편집기 — WaypointEditor 패턴.
/// C키: 편집 ON/OFF | 마우스 드래그: 꼭짓점 이동 | +키: 정점 추가 | -키: 정점 삭제 | K키: 저장
/// (저장키는 WaypointEditor/SpawnEditor의 S와 충돌 회피 위해 K 사용)
/// 저장: Assets/StreamingAssets/cat_area.json
/// </summary>
public class CatAreaEditor : MonoBehaviour
{
    private bool editMode = false;
    private bool initialized = false;
    private readonly List<GameObject> markers = new List<GameObject>();
    private GameObject editorRoot;
    private GameObject dragging = null;
    private Vector3 dragOffset;
    private Camera cam;
    private LineRenderer line;

    private void Start()
    {
        cam = Camera.main;
        Debug.Log("═══ 고양이 영역 편집기 준비됨 ═══");
        Debug.Log("C키: 편집 ON/OFF | 드래그: 이동 | +: 정점추가 | -: 정점삭제 | K: 저장");
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.C))
        {
            editMode = !editMode;
            if (editMode && !initialized) InitEditor();
            if (editorRoot != null) editorRoot.SetActive(editMode);
            Debug.Log(editMode ? "고양이 영역 편집 ON" : "고양이 영역 편집 OFF");
        }

        if (!editMode) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = -0.5f;

        if (Input.GetMouseButtonDown(0))
        {
            Collider2D hit = Physics2D.OverlapPoint(mouseWorld);
            if (hit != null && hit.gameObject.name.StartsWith("CatAreaMarker_"))
            {
                dragging = hit.gameObject;
                dragOffset = dragging.transform.position - mouseWorld;
            }
        }
        if (Input.GetMouseButton(0) && dragging != null)
            dragging.transform.position = mouseWorld + dragOffset;
        if (Input.GetMouseButtonUp(0))
            dragging = null;

        // 정점 추가 (+ 또는 키패드 +)
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            AddVertex(mouseWorld);
        // 정점 삭제 (- 또는 키패드 -) — 최소 3개 유지
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            RemoveLastVertex();

        if (Input.GetKeyDown(KeyCode.K))
            SaveToFile();

        UpdateLine();
#endif
    }

    private void InitEditor()
    {
        if (editorRoot != null) { Destroy(editorRoot); markers.Clear(); }

        editorRoot = new GameObject("CatAreaEditorRoot");
        var data = CatAreaData.Load();
        for (int i = 0; i < data.Count; i++)
        {
            Vector2 p = data.GetPoint(i);
            markers.Add(CreateMarker(i, new Vector3(p.x, p.y, -0.5f)));
        }

        line = editorRoot.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = line.endColor = new Color(0f, 1f, 1f, 0.7f);
        line.startWidth = line.endWidth = 0.06f;
        line.loop = true;
        line.sortingOrder = 59;
        line.useWorldSpace = true;

        initialized = true;
        UpdateLine();
    }

    private void AddVertex(Vector3 worldPos)
    {
        worldPos.z = -0.5f;
        markers.Add(CreateMarker(markers.Count, worldPos));
        RenumberLabels();
        Debug.Log("정점 추가 → " + markers.Count + "개");
    }

    private void RemoveLastVertex()
    {
        if (markers.Count <= 3) { Debug.LogWarning("정점은 최소 3개 유지"); return; }
        var last = markers[markers.Count - 1];
        markers.RemoveAt(markers.Count - 1);
        Destroy(last);
        Debug.Log("정점 삭제 → " + markers.Count + "개");
    }

    private GameObject CreateMarker(int index, Vector3 pos)
    {
        GameObject marker = new GameObject("CatAreaMarker_" + index);
        marker.transform.SetParent(editorRoot.transform);
        marker.transform.position = pos;

        var sr = marker.AddComponent<SpriteRenderer>();
        var tex = new Texture2D(32, 32);
        float r = 16f;
        for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
                tex.SetPixel(x, y, d < r - 1 ? Color.cyan : Color.clear);
            }
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 64f);
        sr.sortingOrder = 60;

        var lb = new GameObject("Label");
        lb.transform.SetParent(marker.transform);
        lb.transform.localPosition = new Vector3(0, 0.3f, 0);
        var tm = lb.AddComponent<TextMesh>();
        tm.text = (index + 1).ToString();
        tm.characterSize = 0.15f;
        tm.fontSize = 48;
        tm.fontStyle = FontStyle.Bold;
        tm.color = Color.red;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        lb.GetComponent<MeshRenderer>().sortingOrder = 61;

        var col = marker.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        return marker;
    }

    private void RenumberLabels()
    {
        for (int i = 0; i < markers.Count; i++)
        {
            markers[i].name = "CatAreaMarker_" + i;
            var tm = markers[i].GetComponentInChildren<TextMesh>();
            if (tm != null) tm.text = (i + 1).ToString();
        }
    }

    private void UpdateLine()
    {
        if (line == null) return;
        line.positionCount = markers.Count;
        for (int i = 0; i < markers.Count; i++)
        {
            var p = markers[i].transform.position;
            line.SetPosition(i, new Vector3(p.x, p.y, 0f));
        }
    }

    private void SaveToFile()
    {
        var transforms = new Transform[markers.Count];
        for (int i = 0; i < markers.Count; i++) transforms[i] = markers[i].transform;
        CatAreaData.FromTransforms(transforms).Save();
    }
}
