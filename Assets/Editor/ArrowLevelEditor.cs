using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ArrowLevelEditor : EditorWindow
{
    // 棋盘参数
    private int levelWidth = 9;
    private int levelHeight = 9;

    // 箭头参数
    private int arrowMinLength = 5;
    private int arrowMaxLength = 10;
    private float changeDirectionChance = 0.3f;

    // 预览开关
    private bool showArrows = true;
    private bool showEmptyDots = true;

    // 生成结果
    private class SnakeData
    {
        public List<Vector2Int> path;
        public ArrowDirection direction;
    }
    private List<SnakeData> generatedSnakes = new List<SnakeData>();
    private bool hasResult = false;

    private Vector2 scrollPos;

    private static readonly Color[] snakeColors = new Color[]
    {
        new Color(0.26f, 0.39f, 0.96f),
        new Color(0.96f, 0.60f, 0.00f),
        new Color(0.26f, 0.63f, 0.28f),
        new Color(0.90f, 0.22f, 0.21f),
        new Color(0.61f, 0.15f, 0.69f),
        new Color(0.00f, 0.68f, 0.76f),
        new Color(1.00f, 0.44f, 0.00f),
        new Color(0.36f, 0.42f, 0.71f),
    };

    [MenuItem("Tool/箭头关卡编辑器")]
    public static void ShowWindow()
    {
        var win = GetWindow<ArrowLevelEditor>("箭头关卡编辑器");
        win.minSize = new Vector2(750, 500);
    }

    private void OnEnable()
    {
        SyncFromScene();
    }

    private void OnFocus()
    {
        SyncFromScene();
    }

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

        // ========== 左侧：关卡预览 ==========
        DrawPreview();

        // ========== 右侧：参数区 ==========
        DrawParameters();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawPreview()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.55f));
        GUILayout.Label("关卡预览", EditorStyles.boldLabel);

        // 工具栏
        EditorGUILayout.BeginHorizontal();
        showArrows = GUILayout.Toggle(showArrows, "箭头显隐", EditorStyles.miniButton, GUILayout.Width(80));
        showEmptyDots = GUILayout.Toggle(showEmptyDots, "空点显隐", EditorStyles.miniButton, GUILayout.Width(80));
        GUILayout.FlexibleSpace();
        if (hasResult)
            GUILayout.Label($"{generatedSnakes.Count} 条蛇", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        var previewRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f));

        float cellSize = Mathf.Min(
            (previewRect.width - 20) / levelWidth,
            (previewRect.height - 20) / levelHeight,
            36f
        );
        if (cellSize < 6f) cellSize = 6f;

        float gridW = cellSize * levelWidth;
        float gridH = cellSize * levelHeight;
        float ox = previewRect.x + (previewRect.width - gridW) / 2f;
        float oy = previewRect.y + (previewRect.height - gridH) / 2f;

        // 画网格
        for (int y = 0; y < levelHeight; y++)
            for (int x = 0; x < levelWidth; x++)
                EditorGUI.DrawRect(new Rect(ox + x * cellSize, oy + y * cellSize, cellSize, cellSize), new Color(0.22f, 0.22f, 0.22f));

        for (int y = 0; y <= levelHeight; y++)
            EditorGUI.DrawRect(new Rect(ox, oy + y * cellSize, gridW, 1), new Color(0.12f, 0.12f, 0.12f));
        for (int x = 0; x <= levelWidth; x++)
            EditorGUI.DrawRect(new Rect(ox + x * cellSize, oy, 1, gridH), new Color(0.12f, 0.12f, 0.12f));

        // 空点显隐
        if (hasResult && showEmptyDots)
        {
            var occupied = new HashSet<Vector2Int>();
            foreach (var snake in generatedSnakes)
                foreach (var p in snake.path)
                    occupied.Add(p);

            float dotR = cellSize * 0.12f;
            for (int y = 0; y < levelHeight; y++)
            {
                for (int x = 0; x < levelWidth; x++)
                {
                    if (!occupied.Contains(new Vector2Int(x, y)))
                    {
                        float dx = ox + x * cellSize + cellSize / 2 - dotR;
                        float dy = oy + y * cellSize + cellSize / 2 - dotR;
                        EditorGUI.DrawRect(new Rect(dx, dy, dotR * 2, dotR * 2), new Color(0.8f, 0.8f, 0.8f));
                    }
                }
            }
        }

        // 画蛇
        if (hasResult && showArrows)
        {
            for (int si = 0; si < generatedSnakes.Count; si++)
            {
                var snake = generatedSnakes[si];
                if (snake.path.Count < 2) continue;
                Color color = snakeColors[si % snakeColors.Length];

                for (int pi = 0; pi < snake.path.Count - 1; pi++)
                {
                    Vector2Int p0 = snake.path[pi];
                    Vector2Int p1 = snake.path[pi + 1];
                    float x0 = ox + p0.x * cellSize + cellSize / 2;
                    float y0 = oy + p0.y * cellSize + cellSize / 2;
                    float x1 = ox + p1.x * cellSize + cellSize / 2;
                    float y1 = oy + p1.y * cellSize + cellSize / 2;

                    float lx = Mathf.Min(x0, x1);
                    float ly = Mathf.Min(y0, y1);
                    float lw = Mathf.Abs(x1 - x0);
                    float lh = Mathf.Abs(y1 - y0);
                    if (lw == 0) { lw = 3; lx -= 1.5f; }
                    if (lh == 0) { lh = 3; ly -= 1.5f; }
                    EditorGUI.DrawRect(new Rect(lx, ly, lw, lh), color);
                }

                // 蛇头大黑点
                Vector2Int head = snake.path[0];
                float hx = ox + head.x * cellSize + cellSize / 2;
                float hy = oy + head.y * cellSize + cellSize / 2;
                float hr = cellSize * 0.3f;
                EditorGUI.DrawRect(new Rect(hx - hr, hy - hr, hr * 2, hr * 2), Color.black);
            }
        }

        // 尺寸标注
        GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
        labelStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(ox + gridW / 2 - 30, oy - 14, 60, 14), $"{levelWidth} × {levelHeight}", labelStyle);

        EditorGUILayout.EndVertical();
    }

    private void DrawParameters()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true));

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("棋盘", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("宽", GUILayout.Width(25));
        int newW = EditorGUILayout.IntSlider(levelWidth, 1, 50);
        if (newW != levelWidth) { levelWidth = newW; hasResult = false; }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("高", GUILayout.Width(25));
        int newH = EditorGUILayout.IntSlider(levelHeight, 1, 50);
        if (newH != levelHeight) { levelHeight = newH; hasResult = false; }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
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
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("操作", EditorStyles.boldLabel);

        if (GUILayout.Button("🎲 生成", GUILayout.Height(35)))
        {
            GenerateLevel();
        }

        if (hasResult)
        {
            GUILayout.Label("点击「生成」重新生成", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

    // ============================================================
    //  生成算法（移植自 ArrowGenerator）
    // ============================================================

    private void GenerateLevel()
    {
        // 应用到场景组件（如果有）
        if (LevelGenerator.Instance != null)
        {
            LevelGenerator.Instance.levelWidth = levelWidth;
            LevelGenerator.Instance.levelHeight = levelHeight;
            LevelGenerator.Instance.RebuildGrid();
        }

        if (ArrowGenerator.Instance != null)
        {
            ArrowGenerator.Instance.arrowMinLength = arrowMinLength;
            ArrowGenerator.Instance.arrowMaxLength = arrowMaxLength;
            ArrowGenerator.Instance.arrowGenerationChangeDirectionChance = changeDirectionChance;
        }

        // 在编辑器内独立生成
        generatedSnakes.Clear();
        GenerateBoard();
        hasResult = true;

        Debug.Log($"生成完成: {generatedSnakes.Count} 条蛇");
        Repaint();
    }

    private void GenerateBoard()
    {
        int w = levelWidth;
        int h = levelHeight;
        bool[,] grid = new bool[w, h];

        int safety = 5000;
        while (safety-- > 0)
        {
            var exits = FindExits(grid, w, h);
            if (exits.Count == 0) break;

            var exit = exits[Random.Range(0, exits.Count)];
            ArrowDirection dir = exit.directions[Random.Range(0, exit.directions.Count)];
            Vector2Int pos = exit.pos;

            grid[pos.x, pos.y] = true;

            SnakeData snake = new SnakeData();
            snake.direction = dir;
            snake.path = new List<Vector2Int> { pos };

            Vector2Int currentPos = pos;
            ArrowDirection currentDir = dir;

            int bodyLength = Random.Range(arrowMinLength - 1, arrowMaxLength);
            for (int i = 0; i < bodyLength; i++)
            {
                var forward = currentPos + DirToVec(currentDir);
                var leftDir = RotateLeft(currentDir);
                var left = currentPos + DirToVec(leftDir);
                var rightDir = RotateRight(currentDir);
                var right = currentPos + DirToVec(rightDir);

                var available = new List<(Vector2Int pos, ArrowDirection dir)>();
                if (IsValid(forward, w, h) && !grid[forward.x, forward.y]) available.Add((forward, currentDir));
                if (IsValid(left, w, h) && !grid[left.x, left.y])       available.Add((left, leftDir));
                if (IsValid(right, w, h) && !grid[right.x, right.y])   available.Add((right, rightDir));

                if (available.Count == 0) break;

                Vector2Int nextPos;
                ArrowDirection nextDir;

                var forwardOpt = available.Find(a => a.dir == currentDir);
                bool canGoForward = forwardOpt.pos != Vector2Int.zero;

                if (canGoForward && Random.value >= changeDirectionChance)
                {
                    // 直走
                    nextPos = forwardOpt.pos;
                    nextDir = currentDir;
                }
                else if (available.Count > 1 && (canGoForward || available.Count >= 2))
                {
                    // 选不往前的方向
                    var turnOpts = available.FindAll(a => a.dir != currentDir);
                    if (turnOpts.Count > 0)
                    {
                        var pick = turnOpts[Random.Range(0, turnOpts.Count)];
                        nextPos = pick.pos;
                        nextDir = pick.dir;
                    }
                    else
                    {
                        var pick = available[Random.Range(0, available.Count)];
                        nextPos = pick.pos;
                        nextDir = pick.dir;
                    }
                }
                else
                {
                    var pick = available[Random.Range(0, available.Count)];
                    nextPos = pick.pos;
                    nextDir = pick.dir;
                }

                grid[nextPos.x, nextPos.y] = true;
                snake.path.Add(nextPos);
                currentPos = nextPos;
                currentDir = nextDir;
            }

            if (snake.path.Count >= 2)
                generatedSnakes.Add(snake);
        }
    }

    private struct ExitInfo
    {
        public Vector2Int pos;
        public List<ArrowDirection> directions;
    }

    private List<ExitInfo> FindExits(bool[,] grid, int w, int h)
    {
        var exits = new List<ExitInfo>();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (grid[x, y]) continue;

                var dirs = new List<ArrowDirection>();

                // 边缘格子
                if (x == 0) dirs.Add(ArrowDirection.Left);
                if (x == w - 1) dirs.Add(ArrowDirection.Right);
                if (y == 0) dirs.Add(ArrowDirection.Down);
                if (y == h - 1) dirs.Add(ArrowDirection.Up);

                // 非边缘格子：直线到边缘之间全是占用的 → 可以出口
                // 向左
                if (!dirs.Contains(ArrowDirection.Left))
                {
                    bool blocked = true;
                    for (int i = x - 1; i >= 0; i--)
                        if (!grid[i, y]) { blocked = false; break; }
                    if (blocked) dirs.Add(ArrowDirection.Left);
                }
                // 向右
                if (!dirs.Contains(ArrowDirection.Right))
                {
                    bool blocked = true;
                    for (int i = x + 1; i < w; i++)
                        if (!grid[i, y]) { blocked = false; break; }
                    if (blocked) dirs.Add(ArrowDirection.Right);
                }
                // 向下
                if (!dirs.Contains(ArrowDirection.Down))
                {
                    bool blocked = true;
                    for (int j = y - 1; j >= 0; j--)
                        if (!grid[x, j]) { blocked = false; break; }
                    if (blocked) dirs.Add(ArrowDirection.Down);
                }
                // 向上
                if (!dirs.Contains(ArrowDirection.Up))
                {
                    bool blocked = true;
                    for (int j = y + 1; j < h; j++)
                        if (!grid[x, j]) { blocked = false; break; }
                    if (blocked) dirs.Add(ArrowDirection.Up);
                }

                if (dirs.Count > 0)
                    exits.Add(new ExitInfo { pos = new Vector2Int(x, y), directions = dirs });
            }
        }
        return exits;
    }

    private Vector2Int DirToVec(ArrowDirection d)
    {
        switch (d)
        {
            case ArrowDirection.Right: return Vector2Int.right;
            case ArrowDirection.Left: return Vector2Int.left;
            case ArrowDirection.Up: return Vector2Int.up;
            case ArrowDirection.Down: return Vector2Int.down;
        }
        return Vector2Int.zero;
    }

    private ArrowDirection RotateLeft(ArrowDirection d)
    {
        switch (d)
        {
            case ArrowDirection.Right: return ArrowDirection.Up;
            case ArrowDirection.Left: return ArrowDirection.Down;
            case ArrowDirection.Up: return ArrowDirection.Left;
            case ArrowDirection.Down: return ArrowDirection.Right;
        }
        return ArrowDirection.Right;
    }

    private ArrowDirection RotateRight(ArrowDirection d)
    {
        switch (d)
        {
            case ArrowDirection.Right: return ArrowDirection.Down;
            case ArrowDirection.Left: return ArrowDirection.Up;
            case ArrowDirection.Up: return ArrowDirection.Right;
            case ArrowDirection.Down: return ArrowDirection.Left;
        }
        return ArrowDirection.Right;
    }

    private bool IsValid(Vector2Int p, int w, int h)
    {
        return p.x >= 0 && p.x < w && p.y >= 0 && p.y < h;
    }
}
