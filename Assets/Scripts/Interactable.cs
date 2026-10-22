using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    // Interaction
    public virtual bool CanInteract(PlayerController player)
    {
        return true;
    }

    public abstract string GetPrompt();

    public abstract void Interact(PlayerController player);
}
