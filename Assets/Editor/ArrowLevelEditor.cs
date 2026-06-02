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

    private static readonly Color[] COLORS = new Color[]
    {
        new Color(0.26f, 0.39f, 0.96f), new Color(0.96f, 0.60f, 0.00f),
        new Color(0.26f, 0.63f, 0.28f), new Color(0.90f, 0.22f, 0.21f),
        new Color(0.61f, 0.15f, 0.69f), new Color(0.00f, 0.68f, 0.76f),
        new Color(1.00f, 0.44f, 0.00f), new Color(0.36f, 0.42f, 0.71f),
    };

    [MenuItem("Tool/箭头关卡编辑器")]
    public static void ShowWindow()
    {
        var win = GetWindow<ArrowLevelEditor>("箭头关卡编辑器");
        win.minSize = new Vector2(750, 500);
    }

    private void OnEnable() { SyncFromScene(); }
    private void OnFocus() { SyncFromScene(); }

    private void SyncFromScene()
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

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawPreview();
        DrawParameters();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPreview()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.55f));

        GUILayout.Label("关卡预览", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        showArrows = GUILayout.Toggle(showArrows, "箭头显隐", EditorStyles.miniButton, GUILayout.Width(80));
        showEmptyDots = GUILayout.Toggle(showEmptyDots, "空点显隐", EditorStyles.miniButton, GUILayout.Width(80));
        GUILayout.FlexibleSpace();
        if (hasResult) GUILayout.Label($"{result.Count} 条蛇", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        var previewRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f));

        float cellSize = Mathf.Min((previewRect.width - 20) / levelWidth, (previewRect.height - 20) / levelHeight, 36f);
        if (cellSize < 6f) cellSize = 6f;
        float gridW = cellSize * levelWidth;
        float gridH = cellSize * levelHeight;
        float ox = previewRect.x + (previewRect.width - gridW) / 2f;
        float oy = previewRect.y + (previewRect.height - gridH) / 2f;

        for (int y = 0; y < levelHeight; y++)
            for (int x = 0; x < levelWidth; x++)
                EditorGUI.DrawRect(new Rect(ox + x * cellSize, oy + y * cellSize, cellSize, cellSize), new Color(0.22f, 0.22f, 0.22f));
        for (int y = 0; y <= levelHeight; y++)
            EditorGUI.DrawRect(new Rect(ox, oy + y * cellSize, gridW, 1), new Color(0.12f, 0.12f, 0.12f));
        for (int x = 0; x <= levelWidth; x++)
            EditorGUI.DrawRect(new Rect(ox + x * cellSize, oy, 1, gridH), new Color(0.12f, 0.12f, 0.12f));

        if (hasResult && showEmptyDots)
        {
            var occ = new HashSet<Vector2Int>();
            foreach (var a in result) foreach (var p in a.path) occ.Add(p);
            float r = cellSize * 0.12f;
            for (int y = 0; y < levelHeight; y++)
                for (int x = 0; x < levelWidth; x++)
                    if (!occ.Contains(new Vector2Int(x, y)))
                        EditorGUI.DrawRect(new Rect(ox + x * cellSize + cellSize / 2 - r, oy + y * cellSize + cellSize / 2 - r, r * 2, r * 2), new Color(0.8f, 0.8f, 0.8f));
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
                    Vector2Int p0 = a.path[pi];
                    Vector2Int p1 = a.path[pi + 1];
                    float x0 = ox + p0.x * cellSize + cellSize / 2, y0 = oy + p0.y * cellSize + cellSize / 2;
                    float x1 = ox + p1.x * cellSize + cellSize / 2, y1 = oy + p1.y * cellSize + cellSize / 2;
                    float lx = Mathf.Min(x0, x1), ly = Mathf.Min(y0, y1);
                    float lw = Mathf.Abs(x1 - x0), lh = Mathf.Abs(y1 - y0);
                    if (lw == 0) { lw = 3; lx -= 1.5f; }
                    if (lh == 0) { lh = 3; ly -= 1.5f; }
                    EditorGUI.DrawRect(new Rect(lx, ly, lw, lh), c);
                }
                var head = a.Head;
                float hx = ox + head.x * cellSize + cellSize / 2;
                float hy = oy + head.y * cellSize + cellSize / 2;
                float hr = cellSize * 0.3f;
                EditorGUI.DrawRect(new Rect(hx - hr, hy - hr, hr * 2, hr * 2), Color.black);
            }
        }

        GUIStyle ls = new GUIStyle(EditorStyles.miniLabel);
        ls.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
        ls.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(ox + gridW / 2 - 30, oy - 14, 60, 14), $"{levelWidth} × {levelHeight}", ls);
        EditorGUILayout.EndVertical();
    }

    private void DrawParameters()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true));

        GUILayout.Box("棋盘", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
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

        GUILayout.Box("箭头生成", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
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
            Generate();

        EditorGUILayout.EndScrollView();
    }

    // ============================================================
    //  生成 — 直接调用已验证的场景组件逻辑
    // ============================================================

    private void Generate()
    {
        Debug.Log("=== 开始生成 ===");
        var gen = ArrowGenerator.Instance;
        var level = LevelGenerator.Instance;

        if (gen == null || level == null)
        {
            Debug.LogWarning("请先打开 Main 场景（确保 ArrowGenerator 和 LevelGenerator 在场景中）");
            return;
        }

        Debug.Log($"gen={gen.name}, level={level.name}");

        // 应用参数
        level.levelWidth = levelWidth;
        level.levelHeight = levelHeight;
        gen.arrowMinLength = arrowMinLength;
        gen.arrowMaxLength = arrowMaxLength;
        gen.arrowGenerationChangeDirectionChance = changeDirectionChance;

        // 尺寸变了就重建网格
        var grid = level.GetGridList();
        Debug.Log($"grid before: {grid?.GetLength(0)}x{grid?.GetLength(1)}");

        if (grid == null || grid.GetLength(0) != levelWidth || grid.GetLength(1) != levelHeight)
        {
            level.RebuildGrid();
            grid = level.GetGridList();
            Debug.Log($"grid after rebuild: {grid?.GetLength(0)}x{grid?.GetLength(1)}");
        }

        if (grid == null) { Debug.LogError("grid is null after RebuildGrid!"); return; }

        // 清空格子
        for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
                grid[x, y].type = TileType.Empty;

        // 跳过 Prefab 视觉创建，只做数据生成
        gen.skipVisualCreation = true;

        // 调用原本的 GenerateAll
        gen.GenerateAll();

        gen.skipVisualCreation = false;

        // 读取结果
        result.Clear();
        foreach (var a in gen.GetArrayList())
            result.Add(new ArrowData { direction = (Dir)a.direction, path = new List<Vector2Int>(a.path) });

        hasResult = result.Count > 0;
        Debug.Log($"生成完成: {result.Count} 条蛇");
        Repaint();
    }
}
