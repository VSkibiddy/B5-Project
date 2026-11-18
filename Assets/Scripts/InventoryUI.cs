using UnityEngine;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [Header("Referenzen")]
    public GameObject panel;            // das ganze Overlay (Panel)
    public Transform slotParent;        // Objekt mit Grid Layout Group
    public InventorySlot slotPrefab;    // Slot-Prefab
    public TMP_Text descriptionText;    // Text für Name + Beschreibung

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        InventoryData.OnChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        InventoryData.OnChanged -= Refresh;
    }

    void Start()
    {
        Close(); // Inventar ist beim Start zu
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

    public void ShowDescription(ItemData item)
    {
        descriptionText.text = "<b>" + item.itemName + "</b>\n" + item.description;
    }

    void Refresh()
    {
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        foreach (ItemData item in InventoryData.Items)
        {
            InventorySlot slot = Instantiate(slotPrefab, slotParent);
            slot.Setup(item, this);
        }

        descriptionText.text = "";
    }
}