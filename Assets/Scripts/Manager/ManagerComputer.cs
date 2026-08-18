using UnityEngine;

public class ManagerComputer : MonoBehaviour, IInteractable
{
    public GameObject managerUI;
    public bool CanInteractWhenLocked => true;
    public int civIndex = 0;

    public void Interact(GameObject player, RoleType role)
    {
        if (role != RoleType.Manager)
        {
            Debug.Log("Only the Manager can use this!");
            return;
        }

        Debug.Log("Opening Manager Station Hub...");

        var hub = ManagerStationHub.Instance ?? Object.FindFirstObjectByType<ManagerStationHub>();
        if (hub != null)
        {
            ManagerStationHub.Instance = hub;
            hub.EnsureBuilt();
            hub.OpenHub();
            return;
        }

        Debug.LogError("ManagerStationHub is missing. Add it on the FeatureBootstrap GameObject in HospitalHubLevel.");

        // Fallback to legacy panel
        if (managerUI != null)
            managerUI.SetActive(true);

        var movement = player.GetComponent<SimplePlayerMovement>();
        if (movement != null)
            movement.SetControlsEnabled(false);

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("open");
    }
}
