using System;
using System.Collections.Generic;
using UnityEngine;

public class PlugMinigame : MonoBehaviour
{
    public static bool IsBusy { get; private set; }

    static PlugMinigame instance;

    // Constants
    const int Rows = 5;
    const int Cols = 7;
    const int StartRow = 2;
    const float RunSpeed = 8f;

    // Block data
    class Piece
    {
        public int row;
        public int col;
        public int len;
        public Color color;
    }

    // State
    private List<Piece> pieces = new List<Piece>();
    private int[,] occ = new int[Rows, Cols];

    private bool open;
    private bool solving;
    private string deviceLabel = "";
    private Action onSolved;
    private List<Vector2> runPath;
    private float runProgress;

    // Layout
    float cell, gx, gy;
    Rect panel;

    // Styles
    private GUIStyle titleStyle;
    private GUIStyle hintStyle;
    private GUIStyle buttonStyle;
    private GUIStyle exitStyle;

    // Unity
    void OnDestroy()
    {
        if (instance == this) instance = null;
        IsBusy = false;
    }

    void Update()
    {
        if (!open) return;

        BlackoutManager bm = BlackoutManager.Instance;
        if (bm == null || bm.IsFinished)
        {
            Close();
            return;
        }

        if (solving)
        {
            runProgress += RunSpeed * Time.deltaTime;
            if (runProgress >= runPath.Count - 1)
            {
                Action cb = onSolved;
                Close();
                if (cb != null) cb();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    // Open / close
    public static void Open(string label, Action solvedCallback)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("PlugMinigame");
            instance = go.AddComponent<PlugMinigame>();
        }
        instance.Begin(label, solvedCallback);
    }

    void Begin(string label, Action solvedCallback)
    {
        if (open) return;

        deviceLabel = label;
        onSolved = solvedCallback;
        Generate();

        solving = false;
        open = true;
        IsBusy = true;
    }

    void Close()
    {
        open = false;
        solving = false;
        IsBusy = false;
    }

    // Puzzle
    void Generate()
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            BuildSolvedLayout();
            Scramble(300);
            if (FindPath() == null) return;
        }
    }

    void BuildSolvedLayout()
    {
        bool[,] empty = new bool[Rows, Cols];

        // Free path
        int row = StartRow;
        for (int c = 0; c < Cols; c++)
        {
            empty[row, c] = true;
            if (UnityEngine.Random.value < 0.45f)
            {
                int nr = row + (UnityEngine.Random.value < 0.5f ? -1 : 1);
                if (nr >= 0 && nr < Rows)
                {
                    empty[nr, c] = true;
                    row = nr;
                }
            }
        }

        // Fill blocks
        pieces.Clear();
        for (int c = 0; c < Cols; c++)
        {
            int r = 0;
            while (r < Rows)
            {
                if (empty[r, c]) { r++; continue; }

                int runEnd = r;
                while (runEnd < Rows && !empty[runEnd, c]) runEnd++;

                int p = r;
                while (p < runEnd)
                {
                    int maxLen = Mathf.Min(3, runEnd - p);
                    int len = UnityEngine.Random.Range(1, maxLen + 1);
                    Piece piece = new Piece();
                    piece.row = p;
                    piece.col = c;
                    piece.len = len;
                    float hue = UnityEngine.Random.value < 0.5f ? UnityEngine.Random.Range(0f, 0.12f) : UnityEngine.Random.Range(0.55f, 0.85f);
                    piece.color = Color.HSVToRGB(hue, 0.4f, 0.7f);
                    pieces.Add(piece);
                    p += len;
                }
                r = runEnd;
            }
        }

        RebuildOcc();
    }

    void Scramble(int tries)
    {
        for (int i = 0; i < tries; i++)
        {
            Piece p = pieces[UnityEngine.Random.Range(0, pieces.Count)];
            TryMove(p, UnityEngine.Random.value < 0.5f ? -1 : 1);
        }
    }

    void RebuildOcc()
    {
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                occ[r, c] = -1;

        for (int i = 0; i < pieces.Count; i++)
        {
            Piece p = pieces[i];
            for (int k = 0; k < p.len; k++) occ[p.row + k, p.col] = i;
        }
    }

    // Movement
    bool CanMove(Piece p, int dir)
    {
        int newRow = p.row + dir;
        if (newRow < 0 || newRow + p.len > Rows) return false;
        int check = dir < 0 ? newRow : p.row + p.len;
        return occ[check, p.col] == -1;
    }

    bool TryMove(Piece p, int dir)
    {
        if (!CanMove(p, dir)) return false;
        p.row += dir;
        RebuildOcc();
        return true;
    }

    // Pathfinding
    List<Vector2> FindPath()
    {
        if (occ[StartRow, 0] != -1) return null;

        int[,] parent = new int[Rows, Cols];
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                parent[r, c] = -2;

        Queue<int> queue = new Queue<int>();
        parent[StartRow, 0] = -1;
        queue.Enqueue(StartRow * Cols + 0);

        int goal = -1;
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, 1, -1 };

        while (queue.Count > 0)
        {
            int cur = queue.Dequeue();
            int cr = cur / Cols;
            int cc = cur % Cols;

            if (cc == Cols - 1) { goal = cur; break; }

            for (int d = 0; d < 4; d++)
            {
                int nr = cr + dr[d];
                int nc = cc + dc[d];
                if (nr < 0 || nr >= Rows || nc < 0 || nc >= Cols) continue;
                if (occ[nr, nc] != -1 || parent[nr, nc] != -2) continue;
                parent[nr, nc] = cur;
                queue.Enqueue(nr * Cols + nc);
            }
        }

        if (goal < 0) return null;

        List<Vector2> path = new List<Vector2>();
        int node = goal;
        while (node != -1)
        {
            path.Add(new Vector2(node % Cols, node / Cols));
            node = parent[node / Cols, node % Cols];
        }
        path.Reverse();
        return path;
    }

    // Green block
    void StartRun(List<Vector2> cells)
    {
        runPath = new List<Vector2>();
        runPath.Add(new Vector2(-1, StartRow));
        runPath.AddRange(cells);
        runPath.Add(new Vector2(Cols, cells[cells.Count - 1].y));
        runProgress = 0f;
        solving = true;
    }

    Vector2 GreenPos()
    {
        if (!solving) return new Vector2(-1, StartRow);

        int i = Mathf.Clamp(Mathf.FloorToInt(runProgress), 0, runPath.Count - 1);
        int j = Mathf.Min(i + 1, runPath.Count - 1);
        return Vector2.Lerp(runPath[i], runPath[j], runProgress - i);
    }

    // Layout
    void CalcLayout()
    {
        float w = Screen.width;
        float h = Screen.height;
        cell = Mathf.Min(h * 0.1f, w * 0.075f);
        float panelW = (Cols + 3f) * cell;
        float panelH = (Rows + 2.5f) * cell;
        panel = new Rect((w - panelW) / 2f, (h - panelH) / 2f, panelW, panelH);
        gx = panel.x + 1.5f * cell;
        gy = panel.y + 2f * cell;
    }

    Rect PieceRect(Piece p)
    {
        float pad = cell * 0.05f;
        return new Rect(gx + p.col * cell + pad, gy + p.row * cell + pad, cell - 2f * pad, p.len * cell - 2f * pad);
    }

    Piece PieceAt(Vector2 m, out bool up)
    {
        up = false;
        int c = Mathf.FloorToInt((m.x - gx) / cell);
        int r = Mathf.FloorToInt((m.y - gy) / cell);
        if (c < 0 || c >= Cols || r < 0 || r >= Rows) return null;

        int idx = occ[r, c];
        if (idx < 0) return null;

        Piece p = pieces[idx];
        up = m.y < gy + (p.row + p.len / 2f) * cell;
        return p;
    }

    // Drawing
    void InitStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleLeft;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;

            hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.alignment = TextAnchor.UpperLeft;
            hintStyle.wordWrap = true;
            hintStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            buttonStyle = new GUIStyle(GUI.skin.button);

            exitStyle = new GUIStyle(GUI.skin.label);
            exitStyle.alignment = TextAnchor.MiddleCenter;
            exitStyle.fontStyle = FontStyle.Bold;
            exitStyle.normal.textColor = new Color(0.6f, 1f, 0.7f);
        }
    }

    void DrawRect(Rect r, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawArrow(Rect half, bool up, float alpha)
    {
        float size = Mathf.Min(half.width, half.height) * 0.45f;
        int steps = 4;
        float bar = size / steps;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        for (int i = 0; i < steps; i++)
        {
            float bw = size * (i + 1) / steps;
            float y = up ? half.center.y - size / 2f + i * bar : half.center.y + size / 2f - (i + 1) * bar;
            GUI.DrawTexture(new Rect(half.center.x - bw / 2f, y, bw, bar), Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
    }

    void OnGUI()
    {
        if (!open) return;

        GUI.depth = -100;
        InitStyles();
        CalcLayout();

        titleStyle.fontSize = Mathf.RoundToInt(cell * 0.4f);
        hintStyle.fontSize = Mathf.RoundToInt(cell * 0.27f);
        buttonStyle.fontSize = Mathf.RoundToInt(cell * 0.28f);
        exitStyle.fontSize = Mathf.RoundToInt(cell * 0.26f);

        Event e = Event.current;

        // Click
        if (e.type == EventType.MouseDown && e.button == 0 && !solving)
        {
            bool up;
            Piece p = PieceAt(e.mousePosition, out up);
            if (p != null)
            {
                if (TryMove(p, up ? -1 : 1))
                {
                    List<Vector2> path = FindPath();
                    if (path != null) StartRun(path);
                }
                e.Use();
            }
        }

        // Background
        DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f));
        DrawRect(new Rect(panel.x - 3f, panel.y - 3f, panel.width + 6f, panel.height + 6f), new Color(0.35f, 0.35f, 0.4f, 1f));
        DrawRect(panel, new Color(0.08f, 0.08f, 0.1f, 1f));

        // Header
        GUI.Label(new Rect(panel.x + 0.5f * cell, panel.y + 0.2f * cell, panel.width - 4.5f * cell, 0.7f * cell), "Unplugging: " + deviceLabel, titleStyle);
        string hint = solving ? "Path clear!" : "Click the top half of a block to slide it up, the bottom half to slide it down. Make a path for the green block.";
        GUI.Label(new Rect(panel.x + 0.5f * cell, panel.y + 0.9f * cell, panel.width - cell, 1f * cell), hint, hintStyle);

        if (!solving)
        {
            if (GUI.Button(new Rect(panel.xMax - 3.6f * cell, panel.y + 0.25f * cell, 3.1f * cell, 0.6f * cell), "Cancel [Esc]", buttonStyle))
            {
                Close();
                return;
            }
        }

        // Grid
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                DrawRect(new Rect(gx + c * cell + 1f, gy + r * cell + 1f, cell - 2f, cell - 2f), new Color(0.16f, 0.16f, 0.2f, 1f));
            }
        }

        // Start / exit
        DrawRect(new Rect(gx - cell + 1f, gy + StartRow * cell + 1f, cell - 2f, cell - 2f), new Color(0.12f, 0.25f, 0.14f, 1f));
        DrawRect(new Rect(gx + Cols * cell + 1f, gy, cell - 2f, Rows * cell), new Color(0.1f, 0.3f, 0.15f, 0.55f));
        GUI.Label(new Rect(gx + Cols * cell, gy + (Rows * cell) / 2f - 0.4f * cell, cell, 0.8f * cell), "EXIT", exitStyle);

        // Blocks
        bool hoverIsUp = false;
        Piece hovered = null;
        if (!solving) hovered = PieceAt(e.mousePosition, out hoverIsUp);

        foreach (Piece p in pieces)
        {
            Rect r = PieceRect(p);
            DrawRect(r, p.color);

            if (p == hovered)
            {
                Rect half = hoverIsUp ? new Rect(r.x, r.y, r.width, r.height / 2f) : new Rect(r.x, r.center.y, r.width, r.height / 2f);
                DrawRect(half, new Color(1f, 1f, 1f, 0.2f));
                DrawArrow(half, hoverIsUp, CanMove(p, hoverIsUp ? -1 : 1) ? 0.95f : 0.25f);
            }
        }

        // Green block
        Vector2 g = GreenPos();
        float gp = cell * 0.12f;
        DrawRect(new Rect(gx + g.x * cell + gp, gy + g.y * cell + gp, cell - 2f * gp, cell - 2f * gp), new Color(0.2f, 0.9f, 0.3f, 1f));
    }
}
