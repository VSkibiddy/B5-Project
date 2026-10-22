using UnityEngine;

public class Fridge : Interactable
{
    // Cooling
    [Header("Kühlung")]
    public float coldMinutesMax = 240f;
    public float openDrainMinutesPerSecond = 12f;
    [Range(0f, 1f)] public float poweredOpenDrainFactor = 0.25f;

    // Food
    [Header("Essen")]
    public int foodPortions = 2;
    [Range(2, 9)] public int slotCount = 6;

    // Texts
    [Header("Texte (Englisch)")]
    public string promptText = "[E] Open fridge";
    [TextArea] public string spoiledText = "The cold chain is broken. You should throw away perishable food now.";
    [TextArea] public string warmText = "The fridge is warm. Everything inside is spoiled, you can't take anything out anymore.";
    [TextArea] public string warmedWhileOpenText = "The fridge warmed up while you were searching. The food inside is no longer safe!";
    [TextArea] public string freshFoodText = "You took the fresh food. Eat it soon: without cooling it only keeps about 4 hours.";
    [TextArea] public string emptyText = "There is no good food left in the fridge. Better keep it closed!";

    // State
    public float ColdMinutes { get; private set; }
    public bool Spoiled { get; private set; }

    public float ColdFraction
    {
        get { return coldMinutesMax > 0f ? ColdMinutes / coldMinutesMax : 0f; }
    }

    private int foodLeft;

    // Unity
    void Start()
    {
        ColdMinutes = coldMinutesMax;
        foodLeft = foodPortions;

        if (BlackoutManager.Instance != null)
        {
            BlackoutManager.Instance.fridge = this;
        }
    }

    void Update()
    {
        BlackoutManager bm = BlackoutManager.Instance;
        if (bm == null || bm.IsFinished || Spoiled) return;

        if (bm.PowerOn) return;

        ColdMinutes = Mathf.Max(0f, ColdMinutes - bm.GameMinutes(Time.deltaTime));

        if (ColdMinutes <= 0f)
        {
            MarkSpoiled();
            bm.ShowMessage(spoiledText, 7f);
        }
    }

    // Interaction
    public override string GetPrompt()
    {
        return promptText;
    }

    public override void Interact(PlayerController player)
    {
        BlackoutManager bm = BlackoutManager.Instance;

        if (Spoiled)
        {
            bm.ShowMessage(warmText, 6f);
            return;
        }

        if (foodLeft <= 0)
        {
            bm.ShowMessage(emptyText);
            return;
        }

        if (FridgeMinigame.Instance == null)
        {
            Debug.LogError("FridgeMinigame fehlt. Es wird automatisch mit dem BlackoutManager hinzugefügt. Ist ein BlackoutManager in der Szene?");
            return;
        }

        FridgeMinigame.Instance.Open(this);
    }

    // Minigame
    public bool TickOpen(float realSeconds)
    {
        BlackoutManager bm = BlackoutManager.Instance;
        if (bm == null || bm.IsFinished) return false;

        float factor = bm.PowerOn ? poweredOpenDrainFactor : 1f;
        ColdMinutes = Mathf.Max(0f, ColdMinutes - openDrainMinutesPerSecond * factor * realSeconds);

        if (ColdMinutes <= 0f && !Spoiled)
        {
            MarkSpoiled();
            bm.ShowMessage(warmedWhileOpenText, 7f);
            return false;
        }
        return !Spoiled;
    }

    public void OnGoodFoodTaken()
    {
        foodLeft = Mathf.Max(0, foodLeft - 1);
        BlackoutManager.Say(freshFoodText, 6f);
    }

    public void OnFoodThrownAway(bool wasGood)
    {
        if (!wasGood) return;
        foodLeft = Mathf.Max(0, foodLeft - 1);
    }

    // Day reset
    public void ResetFridge()
    {
        ColdMinutes = coldMinutesMax;
        Spoiled = false;
        foodLeft = foodPortions;
    }

    // Spoil
    void MarkSpoiled()
    {
        Spoiled = true;
        ColdMinutes = 0f;
    }
}
