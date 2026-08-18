using UnityEngine;

public class TrashItem : MonoBehaviour, IInteractable
{
    public TrashType type;
    public bool CanInteractWhenLocked => false;

    public void Interact(GameObject player, RoleType role)
    {
        if (role != RoleType.Janitor)
            return;

        var janitor = player.GetComponent<JanitorAbilities>();
        if (janitor == null)
        {
            Debug.LogError("JanitorAbilities missing!");
            return;
        }

        // If holding trash and near cart, dispose into matching cart bin
        if (janitor.heldTrash != null && JanitorCartController.Instance != null && JanitorCartController.Instance.isCarried)
        {
            JanitorCartController.Instance.TryDisposeHeldTrash(janitor, janitor.heldTrash.type);
            return;
        }

        janitor.PickUpTrash(this);
        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("pickup");

        Debug.Log("Picked up: " + type);
    }
}
