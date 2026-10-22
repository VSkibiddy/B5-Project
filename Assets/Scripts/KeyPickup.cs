using UnityEngine;

public class KeyPickup : Interactable
{
    // Settings
    [Header("Key Settings")]
    [Tooltip("Muss exakt mit der 'Key Id' im Door-Skript übereinstimmen!")]
    public string keyId = "Key1";
    public string keyName = "Rusty Key";

    // Texts
    [Header("Texts (English)")]
    public string promptText = "[E] Pick up {0}";
    public string pickedUpText = "You picked up the {0}.";

    // Interaction
    public override string GetPrompt()
    {
        return string.Format(promptText, keyName);
    }

    public override void Interact(PlayerController player)
    {
        player.AddKey(keyId);

        BlackoutManager.Say(string.Format(pickedUpText, keyName));

        gameObject.SetActive(false);
    }
}
