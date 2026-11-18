using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public Image icon;

    public void Setup(ItemData item, InventoryUI ui)
    {
        icon.sprite = item.icon;
        GetComponent<Button>().onClick.AddListener(() => ui.ShowDescription(item));
    }
}