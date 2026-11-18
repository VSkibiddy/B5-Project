using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    public ItemData item;

    void Start()
    {
        // Schon im Inventar? Dann liegt es nicht mehr hier
        if (InventoryData.Has(item))
            gameObject.SetActive(false);
    }

    // Wird vom Button (OnClick) aufgerufen
    public void Pickup()
    {
        InventoryData.Add(item);
        gameObject.SetActive(false);
    }
}