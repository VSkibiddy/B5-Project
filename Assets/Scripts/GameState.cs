using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Merkt sich den Spielzustand als einfache Flags (z.B. "schubladeAuf", "hatTaschenlampe").
/// Static, genau wie InventoryData: muss auf kein GameObject gelegt werden
/// und bleibt bei Szenenwechseln erhalten.
/// </summary>
public static class GameState
{
    static readonly HashSet<string> flags = new HashSet<string>();

    /// <summary>Wird ausgelöst, wenn sich ein Flag ändert (Flagname, neuer Wert).</summary>
    public static event Action<string, bool> OnFlagChanged;

    // Setzt beim Start des Play-Modus alles zurück (wichtig, wenn "Domain Reload" im Editor aus ist)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        flags.Clear();
        OnFlagChanged = null;
    }

    public static bool Has(string flag)
    {
        return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
    }

    /// <summary>True, wenn ALLE Flags gesetzt sind (leere Liste = true).</summary>
    public static bool HasAll(string[] list)
    {
        if (list == null) return true;
        foreach (var f in list)
            if (!string.IsNullOrEmpty(f) && !flags.Contains(f)) return false;
        return true;
    }

    /// <summary>True, wenn mindestens EIN Flag gesetzt ist (leere Liste = false).</summary>
    public static bool HasAny(string[] list)
    {
        if (list == null) return false;
        foreach (var f in list)
            if (!string.IsNullOrEmpty(f) && flags.Contains(f)) return true;
        return false;
    }

    public static void Set(string flag, bool value = true)
    {
        if (string.IsNullOrEmpty(flag)) return;

        bool changed = value ? flags.Add(flag) : flags.Remove(flag);
        if (changed) OnFlagChanged?.Invoke(flag, value);
    }

    public static void Clear(string flag) => Set(flag, false);

    public static void ResetAll() => flags.Clear();

    // --- Für späteres Speichern/Laden ---
    public static IReadOnlyCollection<string> All => flags;

    public static void Load(IEnumerable<string> saved)
    {
        flags.Clear();
        if (saved == null) return;
        foreach (var f in saved) flags.Add(f);
    }
}
