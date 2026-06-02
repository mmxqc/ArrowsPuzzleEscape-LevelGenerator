using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class ArrowLevelEditor : EditorWindow
{
    private enum Dir { Up, Down, Left, Right }

    private class ArrowData
    {
        public Dir direction;
        public List<Vector2Int> path = new List<Vector2Int>();
        public Vector2Int Head => path[0];
    }

    private int levelWidth = 9;
    private int levelHeight = 9;
    private int arrowMinLength = 5;
    private int arrowMaxLength = 10;
    private float changeDirectionChance = 0.3f;

    private bool showArrows = true;
    private bool showEmptyDots = true;

    private List<ArrowData> result = new List<ArrowData>();
    private bool hasResult = false;

    private Vector2 scrollPos;

    private static readonly Color[] COLORS = {
        new Color(0.26f,0.39f,0.96f), new Color(0.96f,0.60f,0.00f),
        new Color(0.26f,0.63f,0.28f), new Color(0.90f,0.22f,0.21f),
        new Color(0.61f,0.15f,0.69f), new Color(0.00f,0.68f,0.76f),
        new Color(1.00f,0.44f,0.00f), new Color(0.36f,0.42f,0.71f),
    };

    [MenuItem("Tool/箭头关卡编辑器")]
    public static void ShowWindow()
    {
        var win = GetWindow<ArrowLevelEditor>("箭头关卡编辑器");
        win.minSize = new Vector2(750, 500);
    }

    void OnEnable() { LoadSceneParams(); }
    void OnFocus() { LoadSceneParams(); }

    void LoadSceneParams()
    {
        if (LevelGenerator.Instance != null)
        {
            levelWidth = LevelGenerator.Instance.levelWidth;
            levelHeight = LevelGenerator.Instance.levelHeight;
        }
        if (ArrowGenerator.Instance != null)
        {
            arrowMinLength = ArrowGenerator.Instance.arrowMinLength;
            arrowMaxLength = ArrowGenerator.Instance.arrowMaxLength;
            changeDirectionChance = ArrowGenerator.Instance.arrowGenerationChangeDirectionChance;
        }
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawPreview();
        DrawParams();
        EditorGUILayout.EndHorizontal();
    }

    void DrawPreview()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.55f));

        GUILayout.Label("关卡预览", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        showArrows = GUILayout.Toggle(showArrows, "箭头显隐", EditorStyles.miniButton, GUILayout.Width(80));
        showEmptyDots = GUILayout.Toggle(showEmptyDots, "空点显隐", EditorStyles.miniButton, GUILayout.Width(80));
        if (hasResult) GUILayout.Label($"{result.Count} 条蛇", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        var r = EditorGUILayout.GetControlRect(false, position.height - 60);
        EditorGUI.DrawRect(r, new Color(0.15f, 0.15f, 0.15f));

        float cs = Mathf.Min((r.width - 20) / levelWidth, (r.height - 20) / levelHeight, 36f);
        if (cs < 6f) cs = 6f;
        float gw = cs * levelWidth, gh = cs * levelHeight;
        float ox = r.x + (r.width - gw) / 2f, oy = r.y + (r.height - gh) / 2f;

        for (int y = 0; y < levelHeight; y++)
            for (int x = 0; x < levelWidth; x++)
                EditorGUI.DrawRect(new Rect(ox + x * cs, oy + y * cs, cs, cs), new Color(0.22f, 0.22f, 0.22f));
        for (int y = 0; y <= levelHeight; y++)
            EditorGUI.DrawRect(new Rect(ox, oy + y * cs, gw, 1), new Color(0.12f, 0.12f, 0.12f));
        for (int x = 0; x <= levelWidth; x++)
            EditorGUI.DrawRect(new Rect(ox + x * cs, oy, 1, gh), new Color(0.12f, 0.12f, 0.12f));

        if (hasResult && showEmptyDots)
        {
            var occ = new HashSet<Vector2Int>();
            foreach (var a in result) foreach (var p in a.path) occ.Add(p);
            float dr = cs * 0.12f;
            for (int y = 0; y < levelHeight; y++)
                for (int x = 0; x < levelWidth; x++)
                    if (!occ.Contains(new Vector2Int(x, y)))
                        EditorGUI.DrawRect(new Rect(ox + x * cs + cs / 2 - dr, oy + y * cs + cs / 2 - dr, dr * 2, dr * 2), new Color(0.8f, 0.8f, 0.8f));
        }

        if (hasResult && showArrows)
        {
            for (int si = 0; si < result.Count; si++)
            {
                var a = result[si];
                if (a.path.Count < 2) continue;
                Color c = COLORS[si % COLORS.Length];
                for (int pi = 0; pi < a.path.Count - 1; pi++)
                {
                    Vector2Int p0 = a.path[pi], p1 = a.path[pi + 1];
                    float x0 = ox + p0.x * cs + cs / 2, y0 = oy + p0.y * cs + cs / 2;
                    float x1 = ox + p1.x * cs + cs / 2, y1 = oy + p1.y * cs + cs / 2;
                    float lx = Mathf.Min(x0, x1), ly = Mathf.Min(y0, y1);
                    float lw = Mathf.Abs(x1 - x0), lh = Mathf.Abs(y1 - y0);
                    if (lw == 0) { lw = 3; lx -= 1.5f; }
                    if (lh == 0) { lh = 3; ly -= 1.5f; }
                    EditorGUI.DrawRect(new Rect(lx, ly, lw, lh), c);
                }
                var h = a.Head;
                float hx = ox + h.x * cs + cs / 2, hy = oy + h.y * cs + cs / 2;
                float hr = cs * 0.3f;
                EditorGUI.DrawRect(new Rect(hx - hr, hy - hr, hr * 2, hr * 2), Color.black);
            }
        }

        GUIStyle ls = new GUIStyle(EditorStyles.miniLabel);
        ls.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
        ls.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(ox + gw / 2 - 30, oy - 14, 60, 14), $"{levelWidth} × {levelHeight}", ls);
        EditorGUILayout.EndVertical();
    }

    void DrawParams()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true));

        GUILayout.Label("棋盘", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("宽", GUILayout.Width(25));
        int nw = EditorGUILayout.IntSlider(levelWidth, 1, 50);
        if (nw != levelWidth) { levelWidth = nw; hasResult = false; }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("高", GUILayout.Width(25));
        int nh = EditorGUILayout.IntSlider(levelHeight, 1, 50);
        if (nh != levelHeight) { levelHeight = nh; hasResult = false; }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(8);

        GUILayout.Label("箭头生成", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("最短", GUILayout.Width(35));
        arrowMinLength = EditorGUILayout.IntSlider(arrowMinLength, 1, arrowMaxLength);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("最长", GUILayout.Width(35));
        arrowMaxLength = EditorGUILayout.IntSlider(arrowMaxLength, arrowMinLength, 20);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("拐弯概率", GUILayout.Width(65));
        changeDirectionChance = EditorGUILayout.Slider(changeDirectionChance, 0f, 1f);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(8);

        if (GUILayout.Button("🎲 生成", GUILayout.Height(35)))
            EditorApplication.delayCall += Generate;
        EditorGUILayout.EndScrollView();
    }

    // ============================================================
    //  生成 — 100% 场景组件委托
    // ============================================================

    void Generate()
    {
        var gen = ArrowGenerator.Instance;
        var level = LevelGenerator.Instance;

        if (gen == null || level == null) { Debug.LogWarning("请先打开 Main 场景"); return; }

        level.levelWidth = levelWidth;
        level.levelHeight = levelHeight;
        gen.arrowMinLength = arrowMinLength;
        gen.arrowMaxLength = arrowMaxLength;
        gen.arrowGenerationChangeDirectionChance = changeDirectionChance;

        gen.skipVisualCreation = true;
        gen.GenerateAll();
        gen.skipVisualCreation = false;

        result.Clear();
        foreach (var a in gen.GetArrayList())
            result.Add(new ArrowData { direction = (Dir)a.direction, path = new List<Vector2Int>(a.path) });

        hasResult = result.Count > 0;
        EditorApplication.delayCall += Repaint;
    }
}
