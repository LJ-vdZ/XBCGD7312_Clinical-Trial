using UnityEngine;

public class IVStation : MonoBehaviour, IInteractable
{
    public IVMinigame minigame;

    public bool CanInteractWhenLocked => false;

    public void Interact(GameObject player, RoleType role)
    {
        if (role != RoleType.Nurse)
        {
            Debug.Log("Only a Nurse can do this!");
            return;
        }

        minigame.StartGame(player);
    }
}