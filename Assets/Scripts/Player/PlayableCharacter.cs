using UnityEngine;

/// <summary>
/// Marks a scene character as a controllable role body (Manager/Doctor/Nurse/Janitor).
/// </summary>
public class PlayableCharacter : MonoBehaviour
{
    public RoleType role = RoleType.Manager;
    public string displayName;

    [HideInInspector] public SimplePlayerMovement movement;
    [HideInInspector] public PlayerInteractionHandler interaction;
    [HideInInspector] public CharacterController characterController;
    [HideInInspector] public PlayerRoleManager roleManager;

    public string DisplayName =>
        string.IsNullOrEmpty(displayName) ? role.ToString() : displayName;

    public void CacheComponents()
    {
        movement = GetComponent<SimplePlayerMovement>();
        interaction = GetComponent<PlayerInteractionHandler>();
        characterController = GetComponent<CharacterController>();
        roleManager = GetComponent<PlayerRoleManager>();
    }

    public void SetControlled(bool controlled)
    {
        CacheComponents();

        if (characterController != null)
            characterController.enabled = controlled;

        if (movement != null)
        {
            movement.enabled = controlled;
            movement.SetControlsEnabled(controlled);
        }

        if (interaction != null)
            interaction.enabled = controlled;

        // Only the active body uses the Player tag for trigger interactables.
        try
        {
            gameObject.tag = controlled ? "Player" : "Untagged";
        }
        catch (UnityException)
        {
            // Tag missing in TagManager — ignore
        }
    }
}
