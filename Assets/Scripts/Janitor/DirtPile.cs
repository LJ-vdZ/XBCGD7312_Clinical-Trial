using UnityEngine;

/// <summary>
/// Janitor sweep target for spawned TrashGroup piles.
/// </summary>
public class DirtPile : MonoBehaviour, IInteractable
{
    public bool CanInteractWhenLocked => false;

    public void Interact(GameObject player, RoleType role)
    {
        if (role != RoleType.Janitor)
            return;

        bool hasMop = JanitorCartController.Instance != null && JanitorCartController.Instance.mopEquipped;
        if (!hasMop && JanitorCartController.Instance != null)
            JanitorCartController.Instance.EquipMop(true);

        if (HospitalStatsManager.Instance != null)
            HospitalStatsManager.Instance.ChangeSanitation(+15);

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("clean");

        var anim = player != null ? player.GetComponent<CharacterAnimationDriver>() : null;
        if (anim != null)
            anim.NotifySweeping();

        Destroy(gameObject);
        Debug.Log("Trash swept! +Sanitation");
    }
}
