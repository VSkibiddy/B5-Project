using UnityEngine;

public class EmergencyGenerator : Interactable
{
    // Texts
    [Header("Texte (Englisch)")]
    [TextArea] public string prompt = "[E] Recharge flashlight";
    [TextArea] public string rechargedText = "Flashlight fully recharged.";
    [TextArea] public string alreadyFullText = "The flashlight is already fully charged.";

    // Interaction
    public override bool CanInteract(PlayerController player)
    {
        return true;
    }

    public override string GetPrompt()
    {
        return prompt;
    }

    public override void Interact(PlayerController player)
    {
        if (player.currentBattery >= player.maxBattery)
        {
            BlackoutManager.Say(alreadyFullText, 3f);
            return;
        }

        bool wasDead = player.currentBattery <= 0f;
        player.currentBattery = player.maxBattery;

        if (wasDead && player.flashlight != null) player.flashlight.enabled = true;

        BlackoutManager.Say(rechargedText, 3f);
    }
}
