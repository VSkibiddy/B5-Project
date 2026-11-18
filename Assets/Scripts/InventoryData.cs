using System;
using System.Collections.Generic;
using UnityEngine;

public static class InventoryData
{
    public static readonly List<ItemData> Items = new List<ItemData>();
    public static event Action OnChanged;

    // Setzt alles beim Start des Spiels zurück (wichtig im Editor)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Items.Clear();
        OnChanged = null;
    }

    public static bool Has(ItemData item) => Items.Contains(item);

    public static void Add(ItemData item)
    {
        if (item == null || Has(item)) return;
        Items.Add(item);
        OnChanged?.Invoke();
    }

    public static void Remove(ItemData item)
    {
        if (Items.Remove(item))
            OnChanged?.Invoke();
    }
}