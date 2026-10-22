using UnityEngine;

public class FuseMinigame : MonoBehaviour
{
    public static FuseMinigame Instance { get; private set; }
    public static bool IsBusy { get { return Instance != null && Instance.IsOpen; } }

    public bool IsOpen { get; private set; }

    // Constants
    private const int WireCount = 4;

    private static readonly Color[] wireColors =
    {
        new Color(0.9f, 0.15f, 0.15f),
        new Color(0.2f, 0.4f, 1f),
        new Color(0.95f, 0.85f, 0.2f),
        new Color(0.95f, 0.35f, 0.8f)
    };

    // State
    private readonly int[] rightOrder = new int[WireCount];
    private readonly bool[] connected = new bool[WireCount];
    private int dragging = -1;
    private bool solved;
    private float solvedTimer;
    private float wrongTimer;
    private System.Action onSolved;

    // Styles
    private GUIStyle titleStyle;
    private GUIStyle textStyle;

    // Unity
    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!IsOpen) return;

        BlackoutManager bm = BlackoutManager.Instance;
        if (bm != null && bm.IsFinished)
        {
            Close();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        if (wrongTimer > 0f) wrongTimer -= Time.deltaTime;

        if (solved)
        {
            solvedTimer -= Time.deltaTime;
            if (solvedTimer <= 0f)
            {
                Close();
                if (onSolved != null) onSolved();
            }
        }
    }

    // Open / close
    public void Open(System.Action solvedCallback)
    {
        onSolved = solvedCallback;
        IsOpen = true;
        solved = false;
        solvedTimer = 0f;
        wrongTimer = 0f;
        dragging = -1;
        for (int i = 0; i < WireCount; i++) connected[i] = false;
        Shuffle();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Close()
    {
        IsOpen = false;
        dragging = -1;
    }

    // Puzzle
    void Shuffle()
    {
        for (int i = 0; i < WireCount; i++) rightOrder[i] = i;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            for (int i = WireCount - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = rightOrder[i];
                rightOrder[i] = rightOrder[j];
                rightOrder[j] = tmp;
            }

            bool identity = true;
            for (int i = 0; i < WireCount; i++)
            {
                if (rightOrder[i] != i) identity = false;
            }
            if (!identity) break;
        }
    }

    void CheckSolved()
    {
        for (int i = 0; i < WireCount; i++)
        {
            if (!connected[i]) return;
        }
        solved = true;
        solvedTimer = 0.9f;
    }

    int SlotOfColor(int color)
    {
        for (int j = 0; j < WireCount; j++)
        {
            if (rightOrder[j] == color) return j;
        }
        return -1;
    }

    // Geometry
    Rect LeftRect(Rect panel, float u, int index)
    {
        return new Rect(panel.x + 6f * u, panel.y + 20f * u + index * 10f * u, 6f * u, 7f * u);
    }

    Rect RightRect(Rect panel, float u, int slot)
    {
        return new Rect(panel.xMax - 12f * u, panel.y + 20f * u + slot * 10f * u, 6f * u, 7f * u);
    }

    Vector2 Center(Rect r)
    {
        return new Vector2(r.x + r.width / 2f, r.y + r.height / 2f);
    }

    // Drawing
    void InitStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;

            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.wordWrap = true;
            textStyle.normal.textColor = Color.white;
        }
        titleStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.04f);
        textStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.026f);
    }

    void DrawLine(Vector2 a, Vector2 b, Color color, float thickness)
    {
        Vector2 d = b - a;
        float len = d.magnitude;
        if (len < 0.01f) return;

        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        Matrix4x4 savedMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, a);
        GUI.color = color;
        GUI.DrawTexture(new Rect(a.x, a.y - thickness / 2f, len, thickness), Texture2D.whiteTexture);
        GUI.matrix = savedMatrix;
        GUI.color = Color.white;
    }

    void OnGUI()
    {
        if (!IsOpen) return;

        GUI.depth = -10;
        InitStyles();

        Event e = Event.current;
        float w = Screen.width;
        float h = Screen.height;
        float u = h / 100f;
        Rect panel = new Rect((w - 80f * u) / 2f, 14f * u, 80f * u, 66f * u);

        // Mouse down
        if (e.type == EventType.MouseDown && e.button == 0 && !solved)
        {
            for (int i = 0; i < WireCount; i++)
            {
                if (!connected[i] && LeftRect(panel, u, i).Contains(e.mousePosition))
                {
                    dragging = i;
                    e.Use();
                    break;
                }
            }
        }
        // Mouse up
        else if (e.type == EventType.MouseUp && e.button == 0 && dragging >= 0)
        {
            for (int j = 0; j < WireCount; j++)
            {
                if (RightRect(panel, u, j).Contains(e.mousePosition))
                {
                    if (rightOrder[j] == dragging)
                    {
                        connected[dragging] = true;
                        CheckSolved();
                    }
                    else
                    {
                        wrongTimer = 1.2f;
                    }
                    break;
                }
            }
            dragging = -1;
            e.Use();
        }

        // Draw
        if (e.type == EventType.Repaint)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(0.14f, 0.14f, 0.17f, 1f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(panel.x, panel.y + 1f * u, panel.width, 7f * u), "FUSE BOX", titleStyle);

            // Status
            string status;
            Color statusColor = Color.white;
            if (solved)
            {
                status = "Wiring fixed!";
                statusColor = new Color(0.4f, 1f, 0.4f);
            }
            else if (wrongTimer > 0f)
            {
                status = "Wrong connection!";
                statusColor = new Color(1f, 0.4f, 0.4f);
            }
            else
            {
                status = "Drag each wire from the left to the matching color on the right.";
            }
            textStyle.normal.textColor = statusColor;
            GUI.Label(new Rect(panel.x + 4f * u, panel.y + 8f * u, panel.width - 8f * u, 8f * u), status, textStyle);
            textStyle.normal.textColor = Color.white;

            // Wires
            for (int i = 0; i < WireCount; i++)
            {
                if (!connected[i]) continue;
                int slot = SlotOfColor(i);
                DrawLine(Center(LeftRect(panel, u, i)), Center(RightRect(panel, u, slot)), wireColors[i], 1.4f * u);
            }

            if (dragging >= 0)
            {
                DrawLine(Center(LeftRect(panel, u, dragging)), e.mousePosition, wireColors[dragging], 1.4f * u);
            }

            // Sockets
            for (int i = 0; i < WireCount; i++)
            {
                GUI.color = wireColors[i];
                GUI.DrawTexture(LeftRect(panel, u, i), Texture2D.whiteTexture);
                GUI.color = wireColors[rightOrder[i]];
                GUI.DrawTexture(RightRect(panel, u, i), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;

            GUI.Label(new Rect(panel.x, panel.yMax - 6f * u, panel.width, 5f * u), "Esc = leave", textStyle);
        }
    }
}
