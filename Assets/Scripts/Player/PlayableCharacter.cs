using UnityEngine;

public class PlayableCharacter : MonoBehaviour
{
    public RoleType role = RoleType.Manager;

    public string displayName;

    //dont show in inspector. keep neat
    [HideInInspector] 
    public SimplePlayerMovement movement;

    [HideInInspector] 
    public PlayerInteractionHandler interaction;

    [HideInInspector] 
    public CharacterController characterController;

    [HideInInspector] 
    public PlayerRoleManager roleManager;

    //falls back to role name if no display name is set
    public string DisplayName => string.IsNullOrEmpty(displayName) ? role.ToString() : displayName;

    //stores reference to component on current player character
    public void CacheComponents()
    {
        movement = GetComponent<SimplePlayerMovement>();

        interaction = GetComponent<PlayerInteractionHandler>();

        characterController = GetComponent<CharacterController>();

        roleManager = GetComponent<PlayerRoleManager>();
    }

    //enables and disables control and tags current chaeacter as active player character
    public void SetControlled(bool controlled)
    {
        CacheComponents();

        if (characterController != null)
        {
            characterController.enabled = controlled;
        }

        //enable and disable body colliders. when player character is not being controlled, collider should be off
        SetBodyCollidersEnabled(controlled);

        if (movement != null)
        {
            movement.enabled = controlled;

            movement.SetControlsEnabled(controlled);
        }

        if (interaction != null)
        {
            interaction.enabled = controlled;
        }

        //only active character will use Player tag for interactables
        try
        {
            gameObject.tag = controlled ? "Player" : "Untagged";
        }
        catch (UnityException)
        {
            Debug.Log("Tag missing in TagManager");
        }
    }

    //method to enable and disable body colliders based on if the player character is being controlled
    //helps players move around other playable roles without needing to switch roles to move the other character out of the way
    void SetBodyCollidersEnabled(bool enabled)
    {
        var colliders = GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            var col = colliders[i];

            if (col == null)
            {
                continue;
            }

            //carried props stay on the character transform. leave prop colliders alone.
            if (col.GetComponentInParent<JanitorCartController>() != null)
            {
                continue;
            }

            if (col.GetComponentInParent<TrashItem>() != null)
            {
                continue;
            }

            col.enabled = enabled;
        }
    }
}
