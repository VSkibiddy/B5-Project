using UnityEngine;

public class FuseBox : Interactable
{
    // Texts
    [Header("Texte (Englisch)")]
    public string promptText = "[E] Open fuse box";
    public string generatorRunningPromptText = "[E] Check generator";
    [TextArea] public string powerOnText = "The power is on and all fuses look fine. Nothing to do here right now.";
    [TextArea] public string inspectedText = "All fuses are on. The outage is not just in your home. There is nothing to repair here, so leave the main switch alone.";
    [TextArea] public string generatorHintText = " Maybe you can find a generator battery somewhere in the apartment.";
    [TextArea] public string alreadyCheckedText = "You already checked the fuse box. Everything is fine.";
    [TextArea] public string generatorStartedText = "The generator is running and the lights are back on! Warning: in real life never run a generator indoors (carbon monoxide) and never plug it into the house wiring yourself.";
    [TextArea] public string generatorRunningText = "The generator is running. About {0} minutes of fuel left.";

    // Interaction
    public override string GetPrompt()
    {
        BlackoutManager bm = BlackoutManager.Instance;
        if (bm != null && bm.generator != null && bm.generator.Running)
        {
            return generatorRunningPromptText;
        }
        return promptText;
    }

    public override void Interact(PlayerController player)
    {
        BlackoutManager bm = BlackoutManager.Instance;

        if (!bm.GridDown)
        {
            bm.ShowMessage(powerOnText);
            return;
        }

        if (bm.generator != null && bm.generator.Running)
        {
            bm.ShowMessage(string.Format(generatorRunningText, Mathf.CeilToInt(bm.generator.RemainingMinutes)));
            return;
        }

        if (FuseMinigame.Instance == null)
        {
            Debug.LogError("FuseMinigame fehlt. Es wird automatisch mit dem BlackoutManager hinzugefügt. Ist ein BlackoutManager in der Szene?");
            return;
        }

        FuseMinigame.Instance.Open(() => OnMinigameSolved(player));
    }

    // Minigame result
    void OnMinigameSolved(PlayerController player)
    {
        BlackoutManager bm = BlackoutManager.Instance;
        if (bm == null || bm.IsFinished) return;

        bool firstCheck = !bm.fuseChecked;
        bm.fuseChecked = true;

        // Generator
        GeneratorBattery gen = bm.generator;
        if (gen != null && !gen.Running && player.HasItem(ItemType.Generator))
        {
            player.RemoveItem(ItemType.Generator);
            gen.StartRunning();
            bm.ShowMessage(generatorStartedText, 9f);
            return;
        }

        // Fuse check
        if (firstCheck)
        {
            string text = inspectedText;
            if (gen != null && !gen.Carried) text += generatorHintText;
            bm.ShowMessage(text, 8f);
        }
        else
        {
            bm.ShowMessage(alreadyCheckedText);
        }
    }
}
