using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Verbindet Yarn Spinner mit dem GameState.
/// In Yarn-Skripten:
///   <<setflag "willHelfen">>      -> GameState.Set("willHelfen")
///   <<clearflag "willHelfen">>    -> Flag wieder entfernen
/// </summary>
public class DialogueManager : MonoBehaviour
{
    [Tooltip("Leer lassen: wird automatisch in der Szene gesucht")]
    public DialogueRunner runner;

    // Wird von Yarn einmalig fürs ganze Projekt registriert, nicht pro Szene.
    [YarnCommand("setflag")]
    public static void SetFlag(string flag)
    {
        GameState.Set(flag);
    }

    [YarnCommand("clearflag")]
    public static void ClearFlag(string flag)
    {
        GameState.Set(flag, false);
    }

    void Awake()
    {
        if (runner == null) runner = FindFirstObjectByType<DialogueRunner>();

        if (runner == null)
            Debug.LogError("[DialogueManager] Kein DialogueRunner in der Szene. Dialogue System hinzufügen!");
    }

    public void StartNode(string nodeName)
    {
        if (runner == null) return;
        if (runner.IsDialogueRunning) return;   // nicht doppelt starten

        runner.StartDialogue(nodeName);
    }
}