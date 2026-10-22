using UnityEngine;

// Keep order
public enum ItemType
{
    Battery,
    Water,
    Radio,      // Unused
    Canned,
    Food,
    Spoiled,
    Generator
}

public static class ItemNames
{
    public static string Full(ItemType item)
    {
        switch (item)
        {
            case ItemType.Battery: return "Battery";
            case ItemType.Water: return "Bottle of water";
            case ItemType.Radio: return "Radio";
            case ItemType.Canned: return "Canned food";
            case ItemType.Food: return "Fresh food";
            case ItemType.Spoiled: return "Spoiled food";
            case ItemType.Generator: return "Generator battery";
        }
        return item.ToString();
    }
}

public class PickupItem : Interactable
{
    // Item
    [Header("Gegenstand")]
    public ItemType itemType = ItemType.Battery;

    // Texts
    [Header("Texte (Englisch)")]
    [TextArea] public string pickedUpText = "";
    [TextArea] public string inventoryFullText = "Inventory full! Use or drop something first (X + number key).";

    // Interaction
    public override string GetPrompt()
    {
        return "[E] Pick up " + ItemNames.Full(itemType);
    }

    public override void Interact(PlayerController player)
    {
        if (itemType == ItemType.Radio)
        {
            Debug.LogWarning("PickupItem mit Item Type 'Radio' wird nicht mehr benutzt. Das Radio ist jetzt fest in der Szene: nimm das Skript Radio.");
            return;
        }

        if (itemType == ItemType.Generator)
        {
            Debug.LogWarning("PickupItem mit Item Type 'Generator' funktioniert nicht richtig. Benutze stattdessen das Skript GeneratorBattery.");
        }

        if (!player.AddItem(itemType))
        {
            BlackoutManager.Say(inventoryFullText);
            return;
        }

        BlackoutManager.Say(string.IsNullOrEmpty(pickedUpText) ? "Picked up: " + ItemNames.Full(itemType) + "." : pickedUpText);
        gameObject.SetActive(false);
    }
}
