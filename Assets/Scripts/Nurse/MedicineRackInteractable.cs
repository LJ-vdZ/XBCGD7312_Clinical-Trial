using UnityEngine;

/// <summary>
/// Nurse-only: start the medicine organizing mini-game while looking at MedicineGame_Rack.
/// Requires medicines already spawned on MedRow*C* (after unpacking a box).
/// </summary>
public class MedicineRackInteractable : MonoBehaviour, IInteractable
{
    public bool CanInteractWhenLocked => false;

    public void Interact(GameObject player, RoleType role)
    {
        if (role != RoleType.Nurse)
        {
            Debug.Log("Only the Nurse can organize the medicine rack.");
            return;
        }

        var supply = MedicineSupplyManager.Instance;
        if (supply == null || !supply.ShelfHasMedicines)
        {
            Debug.Log("Unpack a medicine box first — medicines appear on the rack after the box opens.");
            return;
        }

        var mg = MedicineOrganizerMinigame.Instance ?? FindFirstObjectByType<MedicineOrganizerMinigame>();
        if (mg == null)
        {
            Debug.LogWarning("MedicineOrganizerMinigame missing.");
            return;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("interact");

        mg.BeginAtRack(player);
    }
}
