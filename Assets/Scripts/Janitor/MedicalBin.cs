using UnityEngine;

public class MedicalBin : MonoBehaviour, IInteractable
{
    public bool CanInteractWhenLocked => false;

    public void Interact(GameObject player, RoleType role)
    {
        if (role != RoleType.Janitor)
        {
            Debug.Log("Only a Janitor can use this!");
            return;
        }

        var janitor = player.GetComponent<JanitorAbilities>();

        if (janitor == null)
        {
            Debug.LogError("JanitorAbilities missing!");
            return;
        }

        janitor.DisposeTrash(TrashType.Medical);
    }
}