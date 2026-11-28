using UnityEngine;

/// <summary>
/// Kommt auf jeden Orts-Button der Karte (zusammen mit Interactable).
/// Liest die Bedingungen (Required/Forbidden Flags) aus dem Interactable und
/// zeigt oder verbirgt den Ort jedes Mal, wenn die Karte geöffnet wird.
/// Das Objekt bleibt dabei aktiv, deshalb wird neu geprüft, auch wenn
/// sich die Flags zwischendurch geändert haben.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class MapLocation : MonoBehaviour
{
    [Range(0f, 1f)]
    [Tooltip("Aussehen, solange der Ort gesperrt ist. 0 = unsichtbar, 0.4 = abgedunkelt (Klick zeigt dann die Fail Message)")]
    public float lockedAlpha = 0f;

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        Interactable it = GetComponent<Interactable>();

        bool unlocked = it == null ||
                        (GameState.HasAll(it.requiredFlags) && !GameState.HasAny(it.forbiddenFlags));

        cg.alpha = unlocked ? 1f : lockedAlpha;
        cg.blocksRaycasts = unlocked || lockedAlpha > 0f;
        cg.interactable = true;
    }
}
