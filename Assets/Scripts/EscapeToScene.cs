using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EscapeToScene : MonoBehaviour
{
    public string sceneName = "Room";

    void Update()
    {
        if (!EscPressed()) return;

        if (InventoryUI.IsOpen)
            InventoryUI.Instance.Close();
        else
            SceneManager.LoadScene(sceneName);
    }

    static bool EscPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}