using UnityEngine;

public class Generator : MonoBehaviour, IInteractable
{
    public bool CanInteractWhenLocked => false;

    public void Interact(GameObject player, RoleType role)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found in scene!");
            return;
        }

        if (!GameManager.Instance.isPowerOut)
        {
            Debug.Log("Power is already on.");
            return;
        }

        Debug.Log("Power outage is automatic — wait for the countdown to restore power.");
    }

    public void CompletePuzzle()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.EndPowerOutage();

        if (HospitalStatsManager.Instance != null)
            HospitalStatsManager.Instance.ChangeMorale(+10);
    }
}
