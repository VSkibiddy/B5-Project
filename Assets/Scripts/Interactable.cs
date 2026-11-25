using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Generisches klickbares Objekt. Legt man auf ein UI-Objekt mit Button.
/// Prüft Bedingungen (Flags, Item) und führt dann Aktionen aus
/// (Item geben, Flags setzen, Nachricht, Szene laden, eigene Events).
/// </summary>
public class Interactable : MonoBehaviour
{
    [Header("Sichtbarkeit (wird beim Laden der Szene geprüft)")]
    [Tooltip("Objekt ist nur da, wenn ALLE diese Flags gesetzt sind")]
    public string[] showOnlyIfFlags;
    [Tooltip("Objekt verschwindet, wenn EINES dieser Flags gesetzt ist")]
    public string[] hideIfFlags;
    [Tooltip("Nach erfolgreichem Klick verschwindet das Objekt (auch nach Szenenwechsel)")]
    public bool hideAfterUse;
    [Tooltip("Optional. Leer = Szenenname/Objektname wird benutzt")]
    public string uniqueId;

    [Header("Bedingungen")]
    [Tooltip("ALLE diese Flags müssen gesetzt sein")]
    public string[] requiredFlags;
    [Tooltip("KEINES dieser Flags darf gesetzt sein")]
    public string[] forbiddenFlags;
    [Tooltip("Dieses Item muss im Inventar sein")]
    public ItemData requiredItem;
    [Tooltip("Das benötigte Item wird bei Erfolg aus dem Inventar entfernt (z.B. beim Tausch)")]
    public bool consumeRequiredItem;

    [Header("Bei Erfolg")]
    [TextArea] public string successMessage;
    public string[] setFlags;
    public ItemData giveItem;
    [Tooltip("Optional: Szene, die danach geladen wird")]
    public string loadScene;
    public UnityEvent onSuccess;

    [Header("Bei Misserfolg")]
    [TextArea] public string failMessage = "Das geht gerade nicht.";
    public UnityEvent onFail;

    /// <summary>
    /// Später abonniert die Dialogbox dieses Event. Bis dahin landet alles in der Console.
    /// </summary>
    public static event Action<string> OnMessage;

    string Id => string.IsNullOrEmpty(uniqueId)
        ? SceneManager.GetActiveScene().name + "/" + name
        : uniqueId;

    string UsedFlag => "used:" + Id;

    void Awake()
    {
        // Hängt sich automatisch an einen Button auf demselben Objekt
        Button b = GetComponent<Button>();
        if (b != null) b.onClick.AddListener(Interact);
    }

    void Start()
    {
        if (hideAfterUse && GameState.Has(UsedFlag)) { gameObject.SetActive(false); return; }
        if (hideIfFlags != null && GameState.HasAny(hideIfFlags)) { gameObject.SetActive(false); return; }
        if (showOnlyIfFlags != null && showOnlyIfFlags.Length > 0 && !GameState.HasAll(showOnlyIfFlags))
            gameObject.SetActive(false);
    }

    bool ConditionsMet()
    {
        if (!GameState.HasAll(requiredFlags)) return false;
        if (GameState.HasAny(forbiddenFlags)) return false;
        if (requiredItem != null && !InventoryData.Has(requiredItem)) return false;
        return true;
    }

    // Auch von Hand aufrufbar (z.B. von einem anderen Skript oder Button-OnClick)
    public void Interact()
    {
        if (!ConditionsMet())
        {
            Say(failMessage);
            onFail?.Invoke();
            return;
        }

        if (requiredItem != null && consumeRequiredItem) InventoryData.Remove(requiredItem);

        if (giveItem != null) InventoryData.Add(giveItem);

        if (setFlags != null)
            foreach (var f in setFlags) GameState.Set(f);

        if (hideAfterUse) GameState.Set(UsedFlag);

        Say(successMessage);
        onSuccess?.Invoke();

        if (hideAfterUse) gameObject.SetActive(false);

        if (!string.IsNullOrEmpty(loadScene)) SceneManager.LoadScene(loadScene);
    }

    static void Say(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;
        Debug.Log("[Interactable] " + msg);
        OnMessage?.Invoke(msg);
    }
}
