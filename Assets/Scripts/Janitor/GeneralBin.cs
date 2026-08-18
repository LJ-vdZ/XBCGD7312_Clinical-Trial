using UnityEngine;

public class GeneralBin : MonoBehaviour, IInteractable
{
    public bool CanInteractWhenLocked => false;

    public void Interact(GameObject player, RoleType role)
    {
        // Only janitor can use bin
        if (role != RoleType.Janitor)
        {
            Debug.Log("Only a Janitor can use this!");
            return;
        }

        var janitor = player.GetComponent<JanitorAbilities>();

        if (janitor == null)
        {
            Debug.LogError("JanitorAbilities not found on player!");
            return;
        }

        janitor.DisposeTrash(TrashType.General);
    }
}