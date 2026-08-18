using UnityEngine;

public class RoleSwitchStation : MonoBehaviour, IInteractable
{
    public RoleType roleToSwitchTo;

    public bool CanInteractWhenLocked => true;

    public void Interact(GameObject player, RoleType role)
    {
        var roleManager = player.GetComponent<PlayerRoleManager>();

        if (roleManager != null)
        {
            roleManager.SwitchRole(roleToSwitchTo);
        }
    }
}