using UnityEngine;

public class GameHUD : MonoBehaviour
{
    // Styles
    private GUIStyle centerStyle;
    private GUIStyle summaryStyle;

    // Unity
    void OnGUI()
    {
        BlackoutManager bm = BlackoutManager.Instance;
        PlayerController p = PlayerController.Instance;
        if (bm == null || p == null) return;

        InitStyles();
        float w = Screen.width;
        float h = Screen.height;
        float u = h / 100f;

        Color red = new Color(0.9f, 0.2f, 0.2f, 0.9f);

        // Bars
        DrawBar(new Rect(2f * u, 2f * u, 26f * u, 3.2f * u), p.currentBattery / p.maxBattery, new Color(0.9f, 0.75f, 0.1f, 0.9f), "Battery " + Mathf.CeilToInt(p.currentBattery) + "%");
        DrawBar(new Rect(2f * u, 6f * u, 26f * u, 3.2f * u), p.currentStamina / p.maxStamina, new Color(0.2f, 0.7f, 0.3f, 0.9f), "Stamina");
        DrawBar(new Rect(2f * u, 10f * u, 26f * u, 3.2f * u), p.thirst / p.maxThirst, p.thirst <= p.lowSupplyThreshold ? red : new Color(0.2f, 0.5f, 0.9f, 0.9f), "Hydration");
        DrawBar(new Rect(2f * u, 14f * u, 26f * u, 3.2f * u), p.hunger / p.maxHunger, p.hunger <= p.lowSupplyThreshold ? red : new Color(0.85f, 0.5f, 0.2f, 0.9f), "Satiety");

        // Fridge / generator
        float nextBarY = 18f;
        if (bm.fridge != null)
        {
            string fridgeLabel = bm.fridge.Spoiled ? "Fridge: warm!" : (bm.PowerOn ? "Fridge cold (powered)" : "Fridge cold");
            DrawBar(new Rect(2f * u, nextBarY * u, 26f * u, 3.2f * u), bm.fridge.ColdFraction, new Color(0.5f, 0.85f, 0.9f, 0.9f), fridgeLabel);
            nextBarY += 4f;
        }
        if (bm.generator != null && bm.generator.Running)
        {
            DrawBar(new Rect(2f * u, nextBarY * u, 26f * u, 3.2f * u), bm.generator.Fraction, new Color(0.95f, 0.6f, 0.15f, 0.9f), "Generator " + Mathf.CeilToInt(bm.generator.RemainingMinutes) + " min");
        }

        // Inventory
        float slotW = 11f * u;
        float slotH = 8f * u;
        float gap = 1f * u;
        float totalW = p.maxSlots * slotW + (p.maxSlots - 1) * gap;
        float x0 = (w - totalW) / 2f;
        float y0 = h - slotH - 2f * u;
        for (int i = 0; i < p.maxSlots; i++)
        {
            Rect r = new Rect(x0 + i * (slotW + gap), y0, slotW, slotH);
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            string slotText = (i + 1) + "\n" + (i < p.Inventory.Count ? p.Inventory[i].ToString() : "-");
            GUI.Label(r, slotText, centerStyle);
        }

        // Prompt
        if (!bm.IsFinished && !string.IsNullOrEmpty(p.CurrentPrompt))
        {
            GUI.Label(new Rect(0f, y0 - 6f * u, w, 4f * u), p.CurrentPrompt, centerStyle);
        }

        // Message
        if (bm.MessageTimer > 0f && !bm.IsFinished)
        {
            Rect mr = new Rect(w * 0.2f, 2f * u, w * 0.6f, 11f * u);
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(mr, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(mr, bm.CurrentMessage, centerStyle);
        }

        // Controls
        GUI.Label(new Rect(2f * u, h - 11f * u, 55f * u, 10f * u), "WASD move | Shift sprint | F flashlight | E interact | 1-" + p.maxSlots + " use item | X + number drop item", summaryStyle);

        // End screen
        if (bm.IsFinished || bm.IsPaused)
        {
            GUI.color = bm.IsDead ? new Color(0.25f, 0f, 0f, 0.92f) : new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;

            string screenText = bm.IsFinished ? bm.EndText : bm.DayReportText;
            GUI.Label(new Rect(w * 0.1f, h * 0.08f, w * 0.8f, h * 0.84f), screenText, summaryStyle);
        }
    }

    // Drawing
    void InitStyles()
    {
        if (centerStyle == null)
        {
            centerStyle = new GUIStyle(GUI.skin.label);
            centerStyle.alignment = TextAnchor.MiddleCenter;
            centerStyle.wordWrap = true;
            centerStyle.normal.textColor = Color.white;

            summaryStyle = new GUIStyle(GUI.skin.label);
            summaryStyle.alignment = TextAnchor.UpperLeft;
            summaryStyle.wordWrap = true;
            summaryStyle.normal.textColor = Color.white;
        }

        int fs = Mathf.RoundToInt(Screen.height * 0.026f);
        centerStyle.fontSize = fs;
        summaryStyle.fontSize = fs;
    }

    void DrawBar(Rect r, float value01, Color fill, string label)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = fill;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(value01), r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(r, label, centerStyle);
    }
}
