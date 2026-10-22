using UnityEngine;

public class GeneratorBattery : Interactable
{
    // Generator
    [Header("Generator")]
    [Tooltip("So viele SPIELSTUNDEN läuft er. Bei 3 Spielminuten pro echter Sekunde (BlackoutManager) sind 2 Stunden = 40 echte Sekunden.")]
    public float runtimeHours = 2f;

    // Texts
    [Header("Texte (Englisch)")]
    [TextArea] public string pickedUpText = "You picked up the generator battery. Take it to the fuse box and press E there.";
    [TextArea] public string inventoryFullText = "Your inventory is full! Use or drop something first (X + number key).";
    [TextArea] public string emptyText = "The generator ran out of fuel. The power is gone again and it is dark.";

    // State
    public bool Carried { get; private set; }
    public bool Running { get; private set; }
    public float RemainingMinutes { get; private set; }

    public float Fraction
    {
        get { return runtimeHours > 0f ? RemainingMinutes / (runtimeHours * 60f) : 0f; }
    }

    // Unity
    void Start()
    {
        if (BlackoutManager.Instance != null)
        {
            BlackoutManager.Instance.generator = this;
        }
    }

    void Update()
    {
        if (!Running) return;

        BlackoutManager bm = BlackoutManager.Instance;
        if (bm == null || bm.IsFinished) return;

        RemainingMinutes -= bm.GameMinutes(Time.deltaTime);
        if (RemainingMinutes <= 0f)
        {
            RemainingMinutes = 0f;
            Running = false;
            bm.SetPower(false);
            bm.ShowMessage(emptyText, 7f);
        }
    }

    // Interaction
    public override bool CanInteract(PlayerController player)
    {
        return !Carried;
    }

    public override string GetPrompt()
    {
        return "[E] Pick up generator battery";
    }

    public override void Interact(PlayerController player)
    {
        if (!player.AddItem(ItemType.Generator))
        {
            BlackoutManager.Say(inventoryFullText);
            return;
        }

        Carried = true;
        BlackoutManager.Say(pickedUpText, 7f);

        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
    }

    // Fuse box
    public void StartRunning()
    {
        Running = true;
        RemainingMinutes = runtimeHours * 60f;

        if (BlackoutManager.Instance != null)
        {
            BlackoutManager.Instance.SetPower(true);
        }
    }
}
