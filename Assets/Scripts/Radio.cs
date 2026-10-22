using UnityEngine;

public class Radio : Interactable
{
    // Texts
    [Header("Texts (English)")]
    public string promptText = "[E] Tune Radio";

    // Interaction
    public override string GetPrompt()
    {
        return promptText;
    }

    public override void Interact(PlayerController player)
    {
        BlackoutManager bm = BlackoutManager.Instance;
        if (bm == null || bm.IsFinished) return;

        if (RadioMinigame.Instance != null)
        {
            RadioMinigame.Instance.Open(OnMinigameSolved);
        }
        else
        {
            Debug.LogError("RadioMinigame is missing in the scene!");
        }
    }

    // Minigame result
    private void OnMinigameSolved()
    {
        BlackoutManager bm = BlackoutManager.Instance;
        if (bm == null) return;

        bm.radioUsed = true;

        string currentTime = bm.ClockString();
        string infoMessage;

        if (bm.InDaylightWindow)
        {
            string timeUntilDark = BlackoutManager.FormatDuration(bm.HoursUntilDarkness);
            infoMessage = $"Radio broadcast intercepted... The time is {currentTime}. Daylight will hold for another {timeUntilDark}.";
        }
        else
        {
            string timeUntilSunlight = BlackoutManager.FormatDuration(bm.HoursUntilDaylight);
            infoMessage = $"Radio broadcast intercepted... The time is {currentTime}. Sunlight will return in {timeUntilSunlight}.";
        }

        bm.ShowMessage(infoMessage, 8f);
    }
}
