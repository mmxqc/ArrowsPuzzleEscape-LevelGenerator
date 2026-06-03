using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ArrowLevelEditor : EditorWindow
{
    // ============================================================
    //  数据类型
    // ============================================================
    private enum Dir { Up, Down, Left, Right }

    private class Snake
    {
        public Dir direction;
        public List<Vector2Int> path = new List<Vector2Int>();
        public Vector2Int Head => path[0];
    }

    // ============================================================
    //  参数
    // ============================================================
    private int levelWidth = 9, levelHeight = 9;
    private int arrowMinLength = 5, arrowMaxLength = 10;
    private float changeDirectionChance = 0.3f;

    private bool showArrows = true, showEmptyDots = true;
    private bool playMode = false, playWin = false;
    private int playSteps = 0;

    private List<Snake> result = new List<Snake>();
    private List<Snake> playSnakes = new List<Snake>();
    private bool hasResult = false;
    private string solverMsg = "";
    private List<int> stuckIds = new List<int>();
    private Rect lastBounds;
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
        { levelWidth = LevelGenerator.Instance.levelWidth; levelHeight = LevelGenerator.Instance.levelHeight; }
        if (ArrowGenerator.Instance != null)
        { arrowMinLength = ArrowGenerator.Instance.arrowMinLength; arrowMaxLength = ArrowGenerator.Instance.arrowMaxLength; changeDirectionChance = ArrowGenerator.Instance.arrowGenerationChangeDirectionChance; }
    }

    // ============================================================
    //  GUI
    // ============================================================

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
        if (hasResult)
        {
            string btn = playMode && !playWin ? "⏹ 退出" : "🎮 试玩";
            if (GUILayout.Button(btn, EditorStyles.miniButton, GUILayout.Width(60)))
            { if (playMode) { playMode = false; playWin = false; playSteps = 0; } else EditorApplication.delayCall += StartPlay; }

            string info = playMode
                ? $"🎮 {playSteps}步 | 剩{playSnakes.Count(s => s.path.Count > 0)}蛇"
                : $"{result.Count} 条蛇";
            GUILayout.Label(info, EditorStyles.miniLabel);
        }
        EditorGUILayout.EndHorizontal();

        var r = EditorGUILayout.GetControlRect(false, position.height - 60);
        if (Event.current.type == EventType.Repaint) GLDraw(r);
        HandlePreviewClick(r);
        EditorGUILayout.EndVertical();
    }

    // ============================================================
    //  预览绘制 (GL 即时模式，点击在后处理)
    // ============================================================

    void GLDraw(Rect bounds)
    {
        var snakes = playMode ? playSnakes : result;
        lastBounds = bounds;

        float cs = Mathf.Min((bounds.width - 20) / levelWidth, (bounds.height - 20) / levelHeight, 36f);
        if (cs < 6f) cs = 6f;
        float gw = cs * levelWidth, gh = cs * levelHeight;
        float ox = bounds.x + (bounds.width - gw) / 2f;
        float oy = bounds.y + (bounds.height - gh) / 2f;

        // Background
        EditorGUI.DrawRect(bounds, new Color(0.15f, 0.15f, 0.15f));

        // Grid cells
        for (int y = 0; y < levelHeight; y++)
            for (int x = 0; x < levelWidth; x++)
                EditorGUI.DrawRect(new Rect(ox + x * cs, oy + y * cs, cs, cs), new Color(0.22f, 0.22f, 0.22f));

        // Empty dots
        if (showEmptyDots)
        {
            var occ = new HashSet<Vector2Int>();
            foreach (var a in snakes) foreach (var p in a.path) occ.Add(p);
            float dr = cs * 0.12f;
            for (int y = 0; y < levelHeight; y++)
                for (int x = 0; x < levelWidth; x++)
                    if (!occ.Contains(new Vector2Int(x, y)))
                        EditorGUI.DrawRect(new Rect(ox + x * cs + cs / 2 - dr, oy + y * cs + cs / 2 - dr, dr * 2, dr * 2), new Color(0.8f, 0.8f, 0.8f));
        }

        // Snakes
        if (showArrows) for (int si = 0; si < snakes.Count; si++)
        {
            var a = snakes[si];
            if (a.path.Count < 2) continue;

            bool movable = playMode && !playWin && IsPathClear(si, snakes);

            Color c = COLORS[si % COLORS.Length];
            if (playMode && !movable) { c.r *= 0.4f; c.g *= 0.4f; c.b *= 0.4f; c.a = 0.5f; }

            for (int pi = 0; pi < a.path.Count - 1; pi++)
            {
                Vector2Int p0 = a.path[pi], p1 = a.path[pi + 1];
                float x0 = ox + p0.x * cs + cs / 2, y0 = oy + p0.y * cs + cs / 2;
                float x1 = ox + p1.x * cs + cs / 2, y1 = oy + p1.y * cs + cs / 2;
                float lx = Mathf.Min(x0, x1), ly = Mathf.Min(y0, y1);
                float lw = Mathf.Abs(x1 - x0) > 0 ? Mathf.Abs(x1 - x0) : 3;
                float lh = Mathf.Abs(y1 - y0) > 0 ? Mathf.Abs(y1 - y0) : 3;
                if (lw == 3) lx -= 1.5f;
                if (lh == 3) ly -= 1.5f;
                EditorGUI.DrawRect(new Rect(lx, ly, lw, lh), c);
            }

            var h = a.Head;
            float hx = ox + h.x * cs + cs / 2, hy = oy + h.y * cs + cs / 2;
            float hr = cs * 0.3f;
            Color hc = stuckIds.Contains(si) ? Color.red :
                       (playMode && movable ? Color.green : Color.black);
            EditorGUI.DrawRect(new Rect(hx - hr, hy - hr, hr * 2, hr * 2), hc);
        }

        GUIStyle ls = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(ox + gw / 2 - 30, oy - 14, 60, 14), $"{levelWidth} × {levelHeight}", ls);
    }

    // ============================================================
    //  预览区点击处理
    // ============================================================

    void HandlePreviewClick(Rect bounds)
    {
        if (!playMode || playWin) return;
        var evt = Event.current;
        if (evt.type != EventType.MouseDown || evt.button != 0) return;

        // Recompute same as GLDraw
        float cs = Mathf.Min((bounds.width - 20) / levelWidth, (bounds.height - 20) / levelHeight, 36f);
        if (cs < 6f) cs = 6f;
        float ox = bounds.x + (bounds.width - cs * levelWidth) / 2f;
        float oy = bounds.y + (bounds.height - cs * levelHeight) / 2f;

        float mx = evt.mousePosition.x, my = evt.mousePosition.y;
        float hitR = cs * 0.45f; // click within half a cell

        for (int si = 0; si < playSnakes.Count; si++)
        {
            var a = playSnakes[si];
            if (a.path.Count < 2) continue;
            if (!IsPathClear(si, playSnakes)) continue;

            foreach (var p in a.path)
            {
                float cx = ox + p.x * cs + cs / 2;
                float cy = oy + p.y * cs + cs / 2;
                if ((mx - cx) * (mx - cx) + (my - cy) * (my - cy) < hitR * hitR)
                {
                    evt.Use();
                    DoPlayStep(si);
                    EditorApplication.delayCall += Repaint;
                    return;
                }
            }
        }
    }

    // ============================================================
    //  试玩
    // ============================================================

    void StartPlay()
    {
        playMode = true;
        playWin = false;
        playSteps = 0;
        playSnakes = result.Select(s => new Snake { direction = s.direction, path = new List<Vector2Int>(s.path) }).ToList();
        EditorApplication.delayCall += Repaint;
    }

    bool IsPathClear(int idx, List<Snake> snakes)
    {
        var s = snakes[idx];
        if (s.path.Count == 0) return true;
        var head = s.Head;
        var d = DirVec(s.direction);
        int cx = head.x + d.x, cy = head.y + d.y;

        // Build occupancy of OTHER snakes
        var occ = new HashSet<Vector2Int>();
        for (int i = 0; i < snakes.Count; i++)
        {
            if (i == idx) continue;
            foreach (var p in snakes[i].path) occ.Add(p);
        }

        while (cx >= 0 && cx < levelWidth && cy >= 0 && cy < levelHeight)
        {
            if (occ.Contains(new Vector2Int(cx, cy))) return false;
            cx += d.x; cy += d.y;
        }
        return true;
    }

    void DoPlayStep(int idx)
    {
        var s = playSnakes[idx];
        if (s.path.Count == 0) return;

        // Remove this snake entirely (slide out)
        playSnakes[idx] = new Snake { direction = s.direction, path = new List<Vector2Int>() };
        playSteps++;

        // Check win
        if (playSnakes.All(sn => sn.path.Count == 0))
        {
            playWin = true;
            Debug.Log($"🎉 通关！用了 {playSteps} 步");
        }
    }

    // ============================================================
    //  参数面板
    // ============================================================

    void DrawParams()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true));

        // ===== 棋盘 =====
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("棋盘", EditorStyles.boldLabel);
        int nw = EditorGUILayout.IntSlider("宽", levelWidth, 1, 50);
        if (nw != levelWidth) { levelWidth = nw; hasResult = false; playMode = false; }
        int nh = EditorGUILayout.IntSlider("高", levelHeight, 1, 50);
        if (nh != levelHeight) { levelHeight = nh; hasResult = false; playMode = false; }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        // ===== 箭头生成 =====
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("箭头生成", EditorStyles.boldLabel);
        arrowMinLength = EditorGUILayout.IntSlider("最短", arrowMinLength, 1, arrowMaxLength);
        arrowMaxLength = EditorGUILayout.IntSlider("最长", arrowMaxLength, arrowMinLength, 20);
        changeDirectionChance = EditorGUILayout.Slider("拐弯概率", changeDirectionChance, 0f, 1f);
        if (GUILayout.Button("🎲 生成", GUILayout.Height(30)))
            EditorApplication.delayCall += Generate;
        if (hasResult) GUILayout.Label($"已生成 {result.Count} 条蛇", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        // ===== 求解器 =====
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("求解器", EditorStyles.boldLabel);
        if (hasResult)
        {
            if (GUILayout.Button("🔍 检查是否有解", GUILayout.Height(28)))
                EditorApplication.delayCall += SolverCheck;
            if (!string.IsNullOrEmpty(solverMsg))
            {
                GUILayout.Label(solverMsg, EditorStyles.wordWrappedLabel);
                if (stuckIds.Count > 0)
                    GUILayout.Label("堵死蛇: " + string.Join(", ", stuckIds.ConvertAll(i => $"#{i}")), EditorStyles.miniLabel);
            }
        }
        else GUILayout.Label("先生成关卡", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

    // ============================================================
    //  生成
    // ============================================================

    void Generate()
    {
        var gen = ArrowGenerator.Instance;
        var level = LevelGenerator.Instance;
        if (gen == null || level == null) { Debug.LogWarning("请先打开 Main 场景"); return; }

        level.levelWidth = levelWidth; level.levelHeight = levelHeight;
        gen.arrowMinLength = arrowMinLength; gen.arrowMaxLength = arrowMaxLength;
        gen.arrowGenerationChangeDirectionChance = changeDirectionChance;

        gen.skipVisualCreation = true;
        gen.GenerateAll();
        gen.skipVisualCreation = false;

        result = gen.GetArrayList().Select(a => new Snake { direction = (Dir)a.direction, path = new List<Vector2Int>(a.path) }).ToList();
        hasResult = result.Count > 0;
        solverMsg = ""; stuckIds.Clear();
        playMode = false; playWin = false; playSteps = 0;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorApplication.delayCall += Repaint;
    }

    // ============================================================
    //  求解器
    // ============================================================

    void SolverCheck()
    {
        int n = result.Count;
        if (n == 0) { solverMsg = "无蛇"; stuckIds.Clear(); Repaint(); return; }

        var occ = new HashSet<Vector2Int>();
        for (int i = 0; i < n; i++) foreach (var p in result[i].path) occ.Add(p);

        bool CanExit(int i)
        {
            var a = result[i];
            if (a.path.Count == 0) return true;
            var h = a.Head; var d = DirVec(a.direction);
            int x = h.x + d.x, y = h.y + d.y;
            while (x >= 0 && x < levelWidth && y >= 0 && y < levelHeight)
            { if (occ.Contains(new Vector2Int(x, y))) return false; x += d.x; y += d.y; }
            return true;
        }

        var exited = new HashSet<int>();
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int i = 0; i < n; i++)
            {
                if (exited.Contains(i)) continue;
                if (CanExit(i)) { foreach (var p in result[i].path) occ.Remove(p); exited.Add(i); changed = true; break; }
            }
        }

        if (exited.Count == n) { solverMsg = "✅ 有解"; stuckIds.Clear(); }
        else { solverMsg = $"❌ 无解 — {n - exited.Count} 条蛇被堵死"; stuckIds.Clear(); for (int i = 0; i < n; i++) if (!exited.Contains(i)) stuckIds.Add(i); }

        EditorApplication.delayCall += Repaint;
    }

    // ============================================================
    //  工具
    // ============================================================

    static Vector2Int DirVec(Dir d)
    {
        if (d == Dir.Right) return Vector2Int.right;
        if (d == Dir.Left) return Vector2Int.left;
        if (d == Dir.Up) return Vector2Int.up;
        return Vector2Int.down;
    }
}
