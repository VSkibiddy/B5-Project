using System;
using UnityEngine;

public class RadioMinigame : MonoBehaviour
{
    public static RadioMinigame Instance { get; private set; }
    public static bool IsBusy { get; private set; }

    // Constants
    static readonly float[] ZoneWidths = { 0.16f, 0.11f };
    static readonly float[] NeedleSpeeds = { 0.8f, 1.15f };
    const float MinFreq = 88f;
    const float MaxFreq = 108f;

    // State
    private bool open;
    private Action onSuccess;
    private float needle;
    private float direction = 1f;
    private float zoneCenter;
    private int hits;
    private bool success;
    private float successTimer;
    private float flashTimer;
    private int misses;

    // Styles
    private GUIStyle titleStyle;
    private GUIStyle hintStyle;
    private GUIStyle smallStyle;
    private GUIStyle buttonStyle;

    // Stage values
    int Stage { get { return Mathf.Min(hits, ZoneWidths.Length - 1); } }
    float ZoneWidth { get { return ZoneWidths[Stage]; } }
    float NeedleSpeed { get { return NeedleSpeeds[Stage]; } }

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
        if (bm == null || bm.IsFinished)
        {
            Close();
            return;
        }

        if (flashTimer > 0f) flashTimer -= Time.deltaTime;

        // Success
        if (success)
        {
            successTimer -= Time.deltaTime;
            if (successTimer <= 0f)
            {
                Action cb = onSuccess;
                Close();
                if (cb != null) cb();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        // Needle
        needle += direction * NeedleSpeed * Time.deltaTime;
        if (needle >= 1f) { needle = 1f; direction = -1f; }
        if (needle <= 0f) { needle = 0f; direction = 1f; }

        if (Input.GetKeyDown(KeyCode.Space)) Tune();
    }

    // Open / close
    public void Open(Action solvedCallback)
    {
        if (open) return;

        onSuccess = solvedCallback;
        hits = 0;
        misses = 0;
        success = false;
        flashTimer = 0f;
        needle = 0f;
        direction = 1f;
        NewZone();

        open = true;
        IsBusy = true;
    }

    void Close()
    {
        open = false;
        success = false;
        IsBusy = false;
    }

    // Tuning
    void Tune()
    {
        if (success) return;

        if (InZone())
        {
            hits++;
            if (hits >= ZoneWidths.Length)
            {
                success = true;
                successTimer = 1.2f;
            }
            else
            {
                NewZone();
            }
        }
        else
        {
            misses++;
            flashTimer = 0.35f;
        }
    }

    void NewZone()
    {
        float half = ZoneWidth / 2f;
        zoneCenter = UnityEngine.Random.Range(0.1f + half, 0.9f - half);
    }

    bool InZone()
    {
        return Mathf.Abs(needle - zoneCenter) <= ZoneWidth / 2f;
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

    void OnGUI()
    {
        if (!open) return;

        GUI.depth = -100;
        InitStyles();

        float w = Screen.width;
        float h = Screen.height;
        float u = Mathf.Min(h * 0.06f, w * 0.04f);

        float panelW = u * 18f;
        float panelH = u * 9f;
        Rect panel = new Rect((w - panelW) / 2f, (h - panelH) / 2f, panelW, panelH);

        titleStyle.fontSize = Mathf.RoundToInt(u * 0.75f);
        hintStyle.fontSize = Mathf.RoundToInt(u * 0.45f);
        smallStyle.fontSize = Mathf.RoundToInt(u * 0.42f);
        buttonStyle.fontSize = Mathf.RoundToInt(u * 0.55f);

        // Background
        DrawRect(new Rect(0f, 0f, w, h), new Color(0f, 0f, 0f, 0.6f));
        DrawRect(new Rect(panel.x - 3f, panel.y - 3f, panel.width + 6f, panel.height + 6f), new Color(0.5f, 0.45f, 0.3f, 1f));
        DrawRect(panel, new Color(0.12f, 0.1f, 0.08f, 1f));

        // Header
        GUI.Label(new Rect(panel.x + u * 0.5f, panel.y + u * 0.2f, panel.width * 0.6f, u * 1f), "Radio", titleStyle);

        if (!success)
        {
            if (GUI.Button(new Rect(panel.xMax - u * 4.7f, panel.y + u * 0.25f, u * 4.2f, u * 0.9f), "Cancel [Esc]", buttonStyle))
            {
                Close();
                return;
            }
        }

        string hint = success
            ? "Signal found! Listen..."
            : "Press [Space] when the needle is inside the green zone to find the station.";
        GUI.Label(new Rect(panel.x + u * 0.5f, panel.y + u * 1.4f, panel.width - u, u * 1.2f), hint, hintStyle);

        // Dial
        Rect dial = new Rect(panel.x + u * 1f, panel.y + u * 3.4f, panel.width - u * 2f, u * 1.6f);
        Color dialColor = flashTimer > 0f ? new Color(0.5f, 0.1f, 0.1f, 1f) : new Color(0.06f, 0.06f, 0.06f, 1f);
        DrawRect(dial, dialColor);

        for (int i = 0; i <= 20; i++)
        {
            float x = dial.x + dial.width * (i / 20f);
            float len = (i % 5 == 0) ? dial.height * 0.35f : dial.height * 0.18f;
            DrawRect(new Rect(x - 1f, dial.yMax - len, 2f, len), new Color(0.6f, 0.6f, 0.6f, 1f));
        }

        // Zone
        float zw = dial.width * ZoneWidth;
        Rect zone = new Rect(dial.x + dial.width * zoneCenter - zw / 2f, dial.y, zw, dial.height);
        DrawRect(zone, new Color(0.2f, 0.8f, 0.3f, 0.45f));

        // Needle
        float nx = dial.x + dial.width * needle;
        Color needleColor = success ? new Color(0.4f, 1f, 0.5f, 1f) : new Color(1f, 0.35f, 0.25f, 1f);
        DrawRect(new Rect(nx - 2f, dial.y - u * 0.25f, 4f, dial.height + u * 0.5f), needleColor);

        // Frequency
        float freq = Mathf.Lerp(MinFreq, MaxFreq, needle);
        GUI.Label(new Rect(panel.x, dial.yMax + u * 0.2f, panel.width, u * 0.8f), freq.ToString("0.0") + " MHz", smallStyle);

        // Progress
        string status = success
            ? "Station locked"
            : "Signal " + hits + " / " + ZoneWidths.Length + (misses > 0 ? "   |   Static... (misses: " + misses + ")" : "");
        GUI.Label(new Rect(panel.x, panel.y + u * 6.9f, panel.width, u * 0.8f), status, smallStyle);

        if (!success)
        {
            if (GUI.Button(new Rect(panel.center.x - u * 2.5f, panel.yMax - u * 1.5f, u * 5f, u * 1f), "Tune [Space]", buttonStyle))
            {
                Tune();
            }
        }
    }
}
