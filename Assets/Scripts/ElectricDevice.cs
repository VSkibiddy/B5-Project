using UnityEngine;

public class ElectricDevice : Interactable
{
    // Device
    [Header("Gerät")]
    public string deviceLabel = "device";

    [Tooltip("Optional: Objekte, die ausgeschaltet werden, wenn der Stecker gezogen ist (z. B. ein Standby-Lämpchen)")]
    public GameObject[] disableWhenUnplugged;

    // Minigame
    [Header("Minispiel")]
    [Tooltip("An: Zum Ausstecken muss erst das Schiebe-Minispiel gelöst werden. Aus: Stecker wird sofort gezogen.")]
    public bool useMinigame = true;

    // Texts
    [Header("Texte (Englisch)")]
    [TextArea] public string unplugText = "Plug pulled. This protects the device from a power surge when the electricity returns.";

    // State
    private bool unplugged;

    // Unity
    void Start()
    {
        if (BlackoutManager.Instance != null)
        {
            BlackoutManager.Instance.devicesTotal++;
        }
    }

    // Interaction
    public override bool CanInteract(PlayerController player)
    {
        return !unplugged;
    }

    public override string GetPrompt()
    {
        return "[E] Unplug " + deviceLabel;
    }

    public override void Interact(PlayerController player)
    {
        if (useMinigame) PlugMinigame.Open(deviceLabel, Unplug);
        else Unplug();
    }

    // Unplug
    void Unplug()
    {
        BlackoutManager bm = BlackoutManager.Instance;
        if (bm == null || bm.IsFinished || unplugged) return;

        unplugged = true;
        bm.devicesUnplugged++;

        foreach (GameObject go in disableWhenUnplugged)
        {
            if (go != null) go.SetActive(false);
        }

        bm.ShowMessage(unplugText + " (" + bm.devicesUnplugged + "/" + bm.devicesTotal + ")");
    }
}
