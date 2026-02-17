using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 출발 위치 편집기
/// R키: 편집 모드 ON/OFF
/// 마우스 드래그: 출발 위치 이동
/// S키: 파일로 저장 (Assets/StreamingAssets/spawn_positions.json)
/// </summary>
public class SpawnEditor : MonoBehaviour
{
    private bool editMode = false;
    private bool initialized = false;
    private List<GameObject> spawnMarkers = new List<GameObject>();
    private GameObject editorRoot;
    private GameObject dragging = null;
    private Vector3 dragOffset;
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        Debug.Log("═══ 출발 위치 편집기 준비됨 ═══");
        Debug.Log("R키: 편집 ON/OFF | S키: 파일 저장");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            editMode = !editMode;
            if (editMode && !initialized) InitEditor();
            if (editorRoot != null) editorRoot.SetActive(editMode);
            Debug.Log(editMode ? "출발 위치 편집 ON" : "출발 위치 편집 OFF");
        }

        if (!editMode) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = -0.5f;

        if (Input.GetMouseButtonDown(0))
        {
            Collider2D hit = Physics2D.OverlapPoint(mouseWorld);
            if (hit != null && hit.gameObject.name.StartsWith("SpawnMarker_"))
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

    private void InitEditor()
    {
        editorRoot = new GameObject("SpawnEditorRoot");
        var data = SpawnPositionData.Load();

        for (int i = 0; i < data.Count; i++)
        {
            Vector2 pos = data.GetPoint(i);
            spawnMarkers.Add(CreateMarker(i, new Vector3(pos.x, pos.y, -0.5f)));
        }
        initialized = true;
    }

    private GameObject CreateMarker(int index, Vector3 pos)
    {
        GameObject marker = new GameObject("SpawnMarker_" + index);
        marker.transform.SetParent(editorRoot.transform);
        marker.transform.position = pos;

        SpriteRenderer sr = marker.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(32, 32);
        float r = 16f;
        for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
                tex.SetPixel(x, y, d < r - 1 ? Color.green : Color.clear);
            }
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 64f);
        sr.sortingOrder = 60;
        marker.transform.localScale = new Vector3(1.0f, 1.0f, 1f);

        GameObject lb = new GameObject("Label");
        lb.transform.SetParent(marker.transform);
        lb.transform.localPosition = new Vector3(0, 0.3f, 0);
        TextMesh tm = lb.AddComponent<TextMesh>();
        tm.text = "S" + (index + 1);
        tm.characterSize = 0.15f;
        tm.fontSize = 48;
        tm.fontStyle = FontStyle.Bold;
        tm.color = Color.white;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        lb.GetComponent<MeshRenderer>().sortingOrder = 61;

        CircleCollider2D col = marker.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;

        return marker;
    }

    private void SaveToFile()
    {
        Transform[] transforms = new Transform[spawnMarkers.Count];
        for (int i = 0; i < spawnMarkers.Count; i++)
            transforms[i] = spawnMarkers[i].transform;

        var data = SpawnPositionData.FromTransforms(transforms);
        data.Save();
    }
}