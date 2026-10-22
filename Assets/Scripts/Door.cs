using UnityEngine;

public class Door : Interactable
{
    // Settings
    [Header("Door Settings")]
    public string doorName = "Door";
    public int opensOnDay = 1;
    public bool requiresKey = false;
    public string keyId = "Key1";

    // Rotation
    [Header("Rotation Setup")]
    [Tooltip("Verschiebt den Drehpunkt (Scharnier) von der Mitte zum Rand. Meistens ca. 0.5 oder -0.5 auf X oder Z.")]
    public Vector3 hingeOffset = new Vector3(-0.5f, 0f, 0f);

    [Tooltip("Um wie viel Grad soll die Tür schwingen? (z.B. 90 oder -90)")]
    public float openAngle = 90f;

    // Texts
    [Header("Texts (English)")]
    public string openPrompt = "[E] Open {0}";
    public string closePrompt = "[E] Close {0}";
    public string lockedDayText = "The {0} is locked. It might open on Day {1}.";
    public string lockedKeyText = "The {0} is locked. You need a specific key.";

    // State
    private bool isOpen = false;

    // Unity
    void Start()
    {
        if (BlackoutManager.Instance != null)
        {
            BlackoutManager.Instance.doors.Add(this);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 hingePoint = transform.TransformPoint(hingeOffset);
        Gizmos.DrawSphere(hingePoint, 0.1f);
    }

    // Interaction
    public override string GetPrompt()
    {
        if (isOpen)
        {
            return string.Format(closePrompt, doorName);
        }

        BlackoutManager bm = BlackoutManager.Instance;
        if (bm != null && bm.DayNumber < opensOnDay)
        {
            return $"[Locked] {doorName}";
        }
        if (requiresKey)
        {
            return $"[Locked] {doorName} (Needs Key)";
        }

        return string.Format(openPrompt, doorName);
    }

    public override void Interact(PlayerController player)
    {
        if (isOpen)
        {
            CloseDoor();
            return;
        }

        BlackoutManager bm = BlackoutManager.Instance;
        if (bm != null && bm.DayNumber < opensOnDay)
        {
            bm.ShowMessage(string.Format(lockedDayText, doorName, opensOnDay));
            return;
        }

        if (requiresKey && !player.HasKey(keyId))
        {
            bm.ShowMessage(string.Format(lockedKeyText, doorName));
            return;
        }

        OpenDoor();
    }

    // Open / close
    void OpenDoor()
    {
        isOpen = true;
        Vector3 hingePoint = transform.TransformPoint(hingeOffset);
        transform.RotateAround(hingePoint, Vector3.up, openAngle);

        BlackoutManager.Say($"You opened the {doorName}.");
    }

    void CloseDoor()
    {
        isOpen = false;
        Vector3 hingePoint = transform.TransformPoint(hingeOffset);
        transform.RotateAround(hingePoint, Vector3.up, -openAngle);

        BlackoutManager.Say($"You closed the {doorName}.");
    }
}
