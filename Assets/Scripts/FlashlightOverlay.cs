using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Image))]
public class FlashlightOverlay : MonoBehaviour
{
    [Header("Lichtkegel")]
    public float radius = 350f;                 // Radius des Lichts (in Canvas-Einheiten)
    [Range(0.05f, 1f)] public float softness = 0.6f;   // 0.05 = harte Kante, 1 = sehr weich
    [Range(0f, 1f)] public float darkness = 0.96f;     // 1 = komplett schwarz
    [Range(0f, 0.2f)] public float flicker = 0.03f;    // leichtes Flackern, 0 = aus

    [Header("Technik (meist nicht ändern)")]
    public float coverSize = 8192f;             // Größe der schwarzen Fläche
    public int textureSize = 1024;

    RectTransform rt;
    RectTransform canvasRect;

    void Awake()
    {
        rt = (RectTransform)transform;
        canvasRect = (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;

        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(coverSize, coverSize);

        Image img = GetComponent<Image>();
        img.sprite = BuildSprite();
        img.color = Color.white;
        img.raycastTarget = false;   // WICHTIG: Klicks sollen durchgehen
    }

    void Update()
    {
        Vector2 screenPos = GetMousePosition();

        // Bildschirmposition -> Position im Canvas (Screen Space - Overlay)
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, null, out Vector2 local))
        {
            rt.anchoredPosition = local;
        }

        float f = 1f + (Mathf.PerlinNoise(Time.time * 6f, 0f) - 0.5f) * 2f * flicker;
        rt.localScale = new Vector3(f, f, 1f);
    }

    static Vector2 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    Sprite BuildSprite()
    {
        int n = textureSize;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] px = new Color32[n * n];
        float unitsPerPixel = coverSize / n;
        float inner = radius * (1f - softness);
        float c = (n - 1) * 0.5f;

        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * unitsPerPixel;

                float t = Mathf.Clamp01(Mathf.InverseLerp(inner, radius, d));
                t = t * t * (3f - 2f * t); // weicher Übergang

                px[y * n + x] = new Color32(0, 0, 0, (byte)(t * darkness * 255f));
            }
        }

        tex.SetPixels32(px);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f),
                             100f, 0, SpriteMeshType.FullRect);
    }
}