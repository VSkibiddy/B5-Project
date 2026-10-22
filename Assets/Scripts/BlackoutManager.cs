using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(GameHUD), typeof(FuseMinigame), typeof(RadioMinigame))]
[RequireComponent(typeof(FridgeMinigame))]
public class BlackoutManager : MonoBehaviour
{
    public static BlackoutManager Instance { get; private set; }

    // Nights
    [Header("Nächte (FNaF-Style)")]
    [Min(1)] public int nightsToSurvive = 5;
    public float gameStartHour = 18f;
    public float gracePeriodHours = 1f;
    public float gameMinutesPerSecond = 3f;

    // Daylight
    [Header("Tageslicht")]
    public float daylightStartHour = 8f;
    public float daylightEndHour = 19f;
    public float twilightHours = 0.5f;
    public Light sunLight;
    public Color dayAmbientColor = new Color(0.55f, 0.55f, 0.6f);

    // Lights
    [Header("Lampen und Umgebungslicht")]
    public Light[] lightsOffDuringBlackout;
    public Light[] generatorLights;
    public Color darkAmbientColor = new Color(0.03f, 0.03f, 0.05f);
    public Color generatorAmbientColor = new Color(0.25f, 0.25f, 0.22f);
    public Color powerBackAmbientColor = new Color(0.8f, 0.8f, 0.8f);

    // Texts
    [Header("Texte (Englisch)")]
    [TextArea] public string introText = "18:00. The evening begins. You have some time before it gets dark.";
    [TextArea] public string outageText = "19:00. The power just went out and the sun has set! Survive until 08:00.";
    [TextArea] public string newDayText = "Day {0} begins. 18:00 again. Be prepared for the next night.";
    [TextArea] public string[] dayTips =
    {
        "Keep a stock of drinking water. Plan on about 2 liters per person per day.",
        "Eat perishable food first: without cooling it is only safe for about 4 hours.",
        "A battery-powered or hand-crank radio keeps you informed when mobile networks fail.",
        "Never use grills, camping stoves or generators indoors. Carbon monoxide is deadly.",
        "Use flashlights instead of candles. Candles are a fire risk."
    };

    // Stats
    [HideInInspector] public bool fuseChecked;
    [HideInInspector] public int devicesTotal;
    [HideInInspector] public int devicesUnplugged;
    [HideInInspector] public int stumbles;
    [HideInInspector] public bool radioUsed;
    [HideInInspector] public bool wasLowOnSupplies;
    [HideInInspector] public bool ateSpoiled;
    [HideInInspector] public int inventoryFoodSpoiled;

    // References
    [HideInInspector] public Fridge fridge;
    [HideInInspector] public GeneratorBattery generator;
    [HideInInspector] public List<Door> doors = new List<Door>();

    // State
    public bool IsFinished { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsPaused { get; private set; }
    public bool GridDown { get; private set; }
    public bool PowerOn { get { return !GridDown || generatorPower; } }
    public int DayNumber { get { return nightsSurvived + 1; } }

    public bool AnyMinigameOpen
    {
        get { return FuseMinigame.IsBusy || PlugMinigame.IsBusy || RadioMinigame.IsBusy || FridgeMinigame.IsBusy; }
    }

    // Texts (runtime)
    public string EndText { get; private set; }
    public string DayReportText { get; private set; }
    public string CurrentMessage { get; private set; }
    public float MessageTimer { get; private set; }

    // Internal
    private float gameMinutesElapsed;
    private int nightsSurvived;
    private bool dayEndPending;
    private bool generatorPower;
    private float baseSunIntensity = 1f;
    private readonly List<Light> lamps = new List<Light>();

    // Unity
    void Awake()
    {
        Instance = this;
        EndText = "";
        DayReportText = "";
        CurrentMessage = "";
        Time.timeScale = 1f;
        Ensure<GameHUD>();
        Ensure<FuseMinigame>();
        Ensure<RadioMinigame>();
        Ensure<FridgeMinigame>();
        BuildLampList();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    void Start()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        if (sunLight != null) baseSunIntensity = sunLight.intensity;
        RefreshLighting();

        if (gracePeriodHours <= 0f) StartOutage();
        else ShowMessage(introText, 8f);
    }

    void Update()
    {
        if (MessageTimer > 0f) MessageTimer -= Time.deltaTime;

        if (IsFinished)
        {
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            return;
        }

        if (IsPaused)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) ContinueNextDay();
            return;
        }

        gameMinutesElapsed += GameMinutes(Time.deltaTime);

        // Outage
        if (!GridDown && gameMinutesElapsed >= gracePeriodHours * 60f)
        {
            StartOutage();
        }

        // Dawn
        float minutesToDawn = 14f * 60f;

        if (GridDown && !dayEndPending && gameMinutesElapsed >= minutesToDawn)
        {
            gameMinutesElapsed = minutesToDawn;

            // Unplug check
            if (devicesTotal > 0 && devicesUnplugged < devicesTotal)
            {
                if (MessageTimer <= 0f)
                {
                    ShowMessage($"You must unplug all devices to finish the night! ({devicesUnplugged}/{devicesTotal} done)", 4f);
                }
                return;
            }

            if (nightsSurvived + 1 >= nightsToSurvive)
            {
                EndBlackout();
                return;
            }
            dayEndPending = true;
        }

        // Day end
        if (dayEndPending)
        {
            gameMinutesElapsed = minutesToDawn;
            if (!AnyMinigameOpen) BeginDayTransition();
            return;
        }

        RefreshLighting();
    }

    // Setup
    void Ensure<T>() where T : Component
    {
        if (GetComponent<T>() == null) gameObject.AddComponent<T>();
    }

    void BuildLampList()
    {
        lamps.Clear();
        if (lightsOffDuringBlackout == null) return;
        foreach (Light l in lightsOffDuringBlackout)
        {
            if (l == null) continue;
            if (l.type == LightType.Directional)
            {
                if (sunLight == null) sunLight = l;
                continue;
            }
            lamps.Add(l);
        }
    }

    // Day cycle
    void StartOutage()
    {
        GridDown = true;
        ShowMessage(outageText, 9f);
    }

    void BeginDayTransition()
    {
        dayEndPending = false;
        nightsSurvived++;
        DayReportText = BuildDayReport();
        IsPaused = true;
        Time.timeScale = 0f;
    }

    void ContinueNextDay()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        // Reset time
        gameMinutesElapsed = 0f;
        GridDown = false;
        generatorPower = false;

        // Reset player
        PlayerController p = PlayerController.Instance;
        if (p != null)
        {
            p.thirst = p.maxThirst;
            p.hunger = p.maxHunger;
            p.currentStamina = p.maxStamina;
            p.currentBattery = p.maxBattery;
        }

        if (fridge != null) fridge.ResetFridge();

        // Reset stats
        stumbles = 0;
        fuseChecked = false;
        wasLowOnSupplies = false;
        ateSpoiled = false;

        ShowMessage(string.Format(newDayText, DayNumber), 7f);
        RefreshLighting();
    }

    string BuildDayReport()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("NIGHT " + nightsSurvived + " SURVIVED");
        sb.AppendLine();
        sb.AppendLine("Day " + nightsSurvived + " complete. Day " + DayNumber + " begins.");
        sb.AppendLine("Nights left: " + Mathf.Max(0, nightsToSurvive - nightsSurvived));
        sb.AppendLine();

        PlayerController p = PlayerController.Instance;
        if (p != null)
        {
            sb.AppendLine("Hydration " + Mathf.RoundToInt(100f * p.thirst / p.maxThirst) + "%   |   Satiety " + Mathf.RoundToInt(100f * p.hunger / p.maxHunger) + "%   |   Battery " + Mathf.RoundToInt(100f * p.currentBattery / p.maxBattery) + "%");
            sb.AppendLine();
        }

        bool anyNew = false;
        foreach (Door d in doors)
        {
            if (d == null || d.opensOnDay != DayNumber) continue;
            if (!anyNew)
            {
                sb.AppendLine("New today:");
                anyNew = true;
            }
            sb.AppendLine("  - " + d.doorName + " can be opened now" + (d.requiresKey ? " (you need a key)" : ""));
        }
        if (anyNew) sb.AppendLine();

        if (dayTips != null && dayTips.Length > 0)
        {
            sb.AppendLine("Tip: " + dayTips[(nightsSurvived - 1) % dayTips.Length]);
            sb.AppendLine();
        }

        sb.AppendLine("[Space] Start next evening at 18:00");
        return sb.ToString();
    }

    // Time
    float TotalMinutes { get { return gameStartHour * 60f + gameMinutesElapsed; } }
    public float HourOfDay { get { return Mathf.Repeat(TotalMinutes / 60f, 24f); } }

    public float GameMinutes(float realSeconds) { return gameMinutesPerSecond * realSeconds; }
    public float GameHours(float realSeconds) { return gameMinutesPerSecond * realSeconds / 60f; }

    public bool InDaylightWindow
    {
        get
        {
            float h = HourOfDay;
            return h >= daylightStartHour && h < daylightEndHour;
        }
    }

    public float DaylightAmount
    {
        get
        {
            float h = HourOfDay;
            float tw = Mathf.Max(0.01f, twilightHours);
            float up = Mathf.Clamp01((h - daylightStartHour) / tw);
            float down = Mathf.Clamp01((daylightEndHour - h) / tw);
            return up * down;
        }
    }

    public float HoursUntilDaylight { get { return Mathf.Repeat(daylightStartHour - HourOfDay, 24f); } }
    public float HoursUntilDarkness { get { return Mathf.Max(0f, daylightEndHour - HourOfDay); } }

    public string ClockString()
    {
        float total = TotalMinutes;
        int hh = Mathf.FloorToInt(total / 60f) % 24;
        int mm = Mathf.FloorToInt(total) % 60;
        return hh.ToString("00") + ":" + mm.ToString("00");
    }

    public static string FormatDuration(float hours)
    {
        int totalMinutes = Mathf.Max(0, Mathf.RoundToInt(hours * 60f / 5f) * 5);
        int d = totalMinutes / 1440;
        int h = (totalMinutes % 1440) / 60;
        int m = totalMinutes % 60;
        if (d > 0) return h > 0 ? d + " d " + h + " h" : d + " d";
        if (h > 0 && m > 0) return h + " h " + m + " min";
        if (h > 0) return h + " h";
        return m + " min";
    }

    // Messages
    public void ShowMessage(string text, float seconds = 5f)
    {
        CurrentMessage = text;
        MessageTimer = seconds;
    }

    public static void Say(string text, float seconds = 5f)
    {
        if (Instance != null) Instance.ShowMessage(text, seconds);
    }

    // Lighting
    public void SetPower(bool on) { generatorPower = on; }

    bool IsGeneratorLamp(Light l)
    {
        if (generatorLights == null || generatorLights.Length == 0) return true;
        return System.Array.IndexOf(generatorLights, l) >= 0;
    }

    void RefreshLighting()
    {
        float day = DaylightAmount;
        bool grid = !GridDown;

        foreach (Light l in lamps)
        {
            if (l == null) continue;
            bool want = grid || (generatorPower && IsGeneratorLamp(l));
            if (l.enabled != want) l.enabled = want;
        }

        if (sunLight != null)
        {
            sunLight.intensity = baseSunIntensity * day;
            bool sunOn = day > 0.01f;
            if (sunLight.enabled != sunOn) sunLight.enabled = sunOn;
        }

        if (IsFinished)
        {
            RenderSettings.ambientLight = powerBackAmbientColor;
            return;
        }

        Color night;
        if (!PowerOn) night = darkAmbientColor;
        else if (GridDown) night = generatorAmbientColor;
        else night = powerBackAmbientColor;

        RenderSettings.ambientLight = Color.Lerp(night, dayAmbientColor, day);
    }

    // Game end
    void EndBlackout()
    {
        nightsSurvived = nightsToSurvive;
        IsFinished = true;
        GridDown = false;
        Time.timeScale = 1f;
        RefreshLighting();
        EndText = BuildSummary();
    }

    public void PlayerDied(string reason, string tip)
    {
        if (IsFinished) return;
        IsFinished = true;
        IsDead = true;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("YOU DIED");
        sb.AppendLine("\nCause of death: " + reason);
        if (ateSpoiled) sb.AppendLine("The spoiled food also cost you fluids.");
        sb.AppendLine("\nTip: " + tip);
        sb.AppendLine("\nYou made it to " + ClockString() + " on day " + DayNumber + " (" + nightsSurvived + " of " + nightsToSurvive + " nights survived).");
        sb.AppendLine("[R] Restart");
        EndText = sb.ToString();
    }

    string BuildSummary()
    {
        int score = 0;
        int maxScore = 0;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("The power is back! You survived " + nightsToSurvive + (nightsToSurvive == 1 ? " night." : " nights."));
        sb.AppendLine();

        maxScore++;
        if (fuseChecked) { score++; sb.AppendLine("+ Fuse box checked."); }
        else { sb.AppendLine("- Fuse box not checked."); }

        if (devicesTotal > 0)
        {
            maxScore++;
            if (devicesUnplugged >= devicesTotal) { score++; sb.AppendLine("+ All devices unplugged."); }
            else { sb.AppendLine("- Not all devices unplugged."); }
        }

        maxScore++;
        if (!ateSpoiled && inventoryFoodSpoiled == 0) { score++; sb.AppendLine("+ Food handling was good."); }
        else { sb.AppendLine("- Ate spoiled food or let it spoil."); }

        maxScore++;
        if (stumbles == 0) { score++; sb.AppendLine("+ Did not stumble."); }
        else { sb.AppendLine("- Stumbled in the dark."); }

        maxScore++;
        if (radioUsed) { score++; sb.AppendLine("+ Used the radio."); }
        else { sb.AppendLine("- Ignored the radio."); }

        maxScore++;
        if (!wasLowOnSupplies) { score++; sb.AppendLine("+ Kept supplies high."); }
        else { sb.AppendLine("- Ran low on water or food."); }

        sb.AppendLine("\nScore: " + score + " of " + maxScore);
        sb.AppendLine("[R] Restart");
        return sb.ToString();
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
