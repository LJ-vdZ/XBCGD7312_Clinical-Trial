using UnityEngine;

public interface IInteractable
{
    void Interact(GameObject player, RoleType role);

    bool CanInteractWhenLocked { get; }
}