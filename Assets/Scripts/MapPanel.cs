using UnityEngine;

/// <summary>
/// Karte als Overlay über der aktuellen Szene (wie das Inventar).
/// Liegt auf dem Wurzelobjekt "MapCanvas", das immer aktiv bleibt.
/// "panel" ist das Kindobjekt, das ein- und ausgeblendet wird.
/// </summary>
public class MapPanel : MonoBehaviour
{
    public static MapPanel Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [Tooltip("Das Kindobjekt mit Kartenbild und Orts-Buttons")]
    public GameObject panel;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Close(); // Karte ist beim Start zu
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            IsOpen = false;
        }
    }

    public void Toggle()
    {
        if (panel.activeSelf) Close(); else Open();
    }

    public void Open()
    {
        panel.SetActive(true);
        IsOpen = true;
    }

    public void Close()
    {
        panel.SetActive(false);
        IsOpen = false;
    }
}
