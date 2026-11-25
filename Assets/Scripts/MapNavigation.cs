using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Öffnet die Karte und merkt sich, woher der Spieler kam.
/// - In jeder normalen Szene: Button "Karte" -> OpenMap()
/// - In der Map-Szene: Button "Zurück" -> CloseMap()
/// Die Orte auf der Karte sind normale Buttons mit Interactable
/// (loadScene + requiredFlags / showOnlyIfFlags).
/// </summary>
public class MapNavigation : MonoBehaviour
{
    public string mapSceneName = "Map";
    [Tooltip("Falls die Map ohne Vorgänger geöffnet wird (z.B. beim Testen)")]
    public string fallbackScene = "Room";

    static string previousScene;

    public void OpenMap()
    {
        previousScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(mapSceneName);
    }

    public void CloseMap()
    {
        string target = string.IsNullOrEmpty(previousScene) ? fallbackScene : previousScene;
        SceneManager.LoadScene(target);
    }
}
