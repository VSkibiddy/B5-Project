using System.Collections.Generic;
using UnityEngine;

public class FridgeMinigame : MonoBehaviour
{
    public static FridgeMinigame Instance { get; private set; }
    public static bool IsBusy { get; private set; }

    // Constants
    const int Cols = 3;

    static readonly string[] FoodNames =
    {
        "Milk", "Cheese", "Yogurt", "Sausage", "Salad", "Leftovers", "Eggs", "Butter", "Chicken"
    };

    // Slot data
    class Slot
    {
        public string name;
        public bool spoiled;
        public bool gone;
        public int seed;
        public Color color;
    }

    // State
    private readonly List<Slot> slots = new List<Slot>();
    private Fridge fridge;
    private bool open;

    // Layout
    float u, cellW, cellH, gap, headerH;
    Rect panel;

    // Styles
    private GUIStyle titleStyle;
    private GUIStyle hintStyle;
    private GUIStyle nameStyle;
    private GUIStyle smallStyle;
    private GUIStyle buttonStyle;

    // Unity
    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        IsBusy = false;
    }

    void Update()
    {
        if (!open) return;

        BlackoutManager bm = BlackoutManager.Instance;
        if (bm == null || bm.IsFinished || fridge == null)
        {
            Close();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        if (!fridge.TickOpen(Time.deltaTime)) Close();
    }

    // Open / close
    public void Open(Fridge f)
    {
        if (open || f == null) return;

        fridge = f;
        Build(f.slotCount);
        open = true;
        IsBusy = true;
    }

    void Close()
    {
        open = false;
        IsBusy = false;
    }

    // Content
    void Build(int count)
    {
        slots.Clear();

        // Shuffle names
        List<string> names = new List<string>(FoodNames);
        for (int i = names.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            string tmp = names[i];
            names[i] = names[j];
            names[j] = tmp;
        }

        // Create slots
        int goodIndex = UnityEngine.Random.Range(0, count);
        for (int i = 0; i < count; i++)
        {
            Slot s = new Slot();
            s.name = names[i % names.Count];
            s.spoiled = i != goodIndex;
            s.seed = UnityEngine.Random.Range(1, 10000);

            if (s.spoiled)
            {
                s.color = Color.HSVToRGB(UnityEngine.Random.Range(0.14f, 0.27f), UnityEngine.Random.Range(0.35f, 0.55f), UnityEngine.Random.Range(0.38f, 0.5f));
            }
            else
            {
                float[] hues = { 0.02f, 0.08f, 0.12f, 0.55f, 0.6f, 0.95f };
                s.color = Color.HSVToRGB(hues[UnityEngine.Random.Range(0, hues.Length)], 0.5f, 0.9f);
            }
            slots.Add(s);
        }
    }

    // Actions
    void Take(Slot s)
    {
        PlayerController player = PlayerController.Instance;
        if (player == null) return;

        ItemType item = s.spoiled ? ItemType.Spoiled : ItemType.Food;
        if (!player.AddItem(item))
        {
            BlackoutManager.Say("Your inventory is full! Use or drop something first (X + number key).");
            return;
        }

        s.gone = true;
        if (s.spoiled) BlackoutManager.Say("Ugh, that " + s.name.ToLower() + " was spoiled! Throw it away (X + number key).", 5f);
        else fridge.OnGoodFoodTaken();

        CheckDone();
    }

    void Throw(Slot s)
    {
        s.gone = true;
        fridge.OnFoodThrownAway(!s.spoiled);

        if (s.spoiled) BlackoutManager.Say("Spoiled " + s.name.ToLower() + " thrown away.", 3f);
        else BlackoutManager.Say("Oops, that " + s.name.ToLower() + " was still good!", 5f);

        CheckDone();
    }

    void CheckDone()
    {
        foreach (Slot s in slots)
        {
            if (!s.gone) return;
        }
        Close();
    }

    // Layout
    void CalcLayout()
    {
        float w = Screen.width;
        float h = Screen.height;
        u = Mathf.Min(h * 0.06f, w * 0.04f);
        cellW = u * 3.4f;
        cellH = u * 2.5f;
        gap = u * 0.4f;
        headerH = u * 4.4f;

        int rows = Mathf.CeilToInt(slots.Count / (float)Cols);
        float panelW = Cols * cellW + (Cols + 1) * gap;
        float panelH = headerH + rows * cellH + (rows + 1) * gap + u * 1.2f;
        panel = new Rect((w - panelW) / 2f, (h - panelH) / 2f, panelW, panelH);
    }

    Rect SlotRect(int i)
    {
        int c = i % Cols;
        int r = i / Cols;
        return new Rect(panel.x + gap + c * (cellW + gap), panel.y + headerH + gap + r * (cellH + gap), cellW, cellH);
    }

    int SlotAt(Vector2 m)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].gone && SlotRect(i).Contains(m)) return i;
        }
        return -1;
    }

    // Drawing
    void InitStyles()
    {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.alignment = TextAnchor.MiddleLeft;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;

        hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.alignment = TextAnchor.UpperLeft;
        hintStyle.wordWrap = true;
        hintStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

        nameStyle = new GUIStyle(GUI.skin.label);
        nameStyle.alignment = TextAnchor.MiddleCenter;
        nameStyle.fontStyle = FontStyle.Bold;
        nameStyle.normal.textColor = Color.black;

        smallStyle = new GUIStyle(GUI.skin.label);
        smallStyle.alignment = TextAnchor.MiddleCenter;
        smallStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button);
    }

    void DrawRect(Rect r, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    static float Hash(int seed, int i)
    {
        float v = Mathf.Sin(seed * 12.9898f + i * 78.233f) * 43758.5453f;
        return v - Mathf.Floor(v);
    }

    void OnGUI()
    {
        if (!open) return;

        GUI.depth = -100;
        InitStyles();
        CalcLayout();

        titleStyle.fontSize = Mathf.RoundToInt(u * 0.75f);
        hintStyle.fontSize = Mathf.RoundToInt(u * 0.45f);
        nameStyle.fontSize = Mathf.RoundToInt(u * 0.55f);
        smallStyle.fontSize = Mathf.RoundToInt(u * 0.42f);
        buttonStyle.fontSize = Mathf.RoundToInt(u * 0.5f);

        Event e = Event.current;

        // Click
        if (e.type == EventType.MouseDown && (e.button == 0 || e.button == 1))
        {
            int idx = SlotAt(e.mousePosition);
            if (idx >= 0)
            {
                if (e.button == 0) Take(slots[idx]);
                else Throw(slots[idx]);
                e.Use();
            }
            if (!open) return;
        }

        // Background
        DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f));
        DrawRect(new Rect(panel.x - 3f, panel.y - 3f, panel.width + 6f, panel.height + 6f), new Color(0.55f, 0.65f, 0.7f, 1f));
        DrawRect(panel, new Color(0.1f, 0.13f, 0.15f, 1f));

        // Header
        GUI.Label(new Rect(panel.x + gap, panel.y + u * 0.2f, panel.width * 0.6f, u * 1f), "Fridge", titleStyle);
        if (GUI.Button(new Rect(panel.xMax - gap - u * 4.2f, panel.y + u * 0.25f, u * 4.2f, u * 0.9f), "Close [Esc]", buttonStyle))
        {
            Close();
            return;
        }

        // Cold bar
        float cold = Mathf.Clamp01(fridge.ColdFraction);
        Rect barBg = new Rect(panel.x + gap, panel.y + u * 1.45f, panel.width - 2f * gap, u * 0.7f);
        DrawRect(barBg, new Color(0.04f, 0.05f, 0.06f, 1f));
        DrawRect(new Rect(barBg.x + 2f, barBg.y + 2f, (barBg.width - 4f) * cold, barBg.height - 4f), Color.Lerp(new Color(0.9f, 0.25f, 0.2f), new Color(0.3f, 0.8f, 1f), cold));
        GUI.Label(barBg, "Cold " + Mathf.RoundToInt(cold * 100f) + "%", smallStyle);

        // Hint
        GUI.Label(new Rect(panel.x + gap, panel.y + u * 2.3f, panel.width - 2f * gap, u * 2f),
            "Left click: take. Right click: throw away. Only ONE item is still good: throw out the spoiled ones. The cold escapes while the door is open, so hurry!", hintStyle);

        // Food
        int hovered = SlotAt(e.mousePosition);

        for (int i = 0; i < slots.Count; i++)
        {
            Slot s = slots[i];
            Rect r = SlotRect(i);

            if (s.gone)
            {
                DrawRect(r, new Color(0.07f, 0.09f, 0.1f, 1f));
                continue;
            }

            DrawRect(r, s.color);

            if (s.spoiled)
            {
                for (int k = 0; k < 5; k++)
                {
                    float sx = r.x + Hash(s.seed, k * 2) * (r.width - u * 0.5f);
                    float sy = r.y + Hash(s.seed, k * 2 + 1) * (r.height - u * 0.5f);
                    float size = u * (0.2f + 0.25f * Hash(s.seed, k + 20));
                    DrawRect(new Rect(sx, sy, size, size), new Color(0.12f, 0.2f, 0.1f, 0.85f));
                }
                GUI.Label(new Rect(r.x, r.y - u * 0.1f, r.width, u * 0.7f), "~ ~ ~", smallStyle);
            }

            GUI.Label(r, s.name, nameStyle);

            if (i == hovered) DrawRect(r, new Color(1f, 1f, 1f, 0.22f));
        }
    }
}
