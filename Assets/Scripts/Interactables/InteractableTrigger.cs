using UnityEngine;

public class InteractableTrigger : MonoBehaviour
{
    private IInteractable interactableComponent;

    void Awake()
    {
        interactableComponent = ResolveInteractable();
    }

    void OnEnable()
    {
        // Re-resolve in case components were enabled/disabled at runtime
        if (interactableComponent == null)
            interactableComponent = ResolveInteractable();
    }

    /// <summary>
    /// When multiple IInteractable scripts share an object (e.g. Visitor + ManagerComputer),
    /// prefer the visitor dialogue over the manager station.
    /// </summary>
    IInteractable ResolveInteractable()
    {
        var visitor = GetComponent<VisitorInteractable>();
        if (visitor != null && visitor.enabled && !visitor.hasBeenSpokenTo)
            return visitor;

        // Prefer PatientInteractable (new nurse/doctor care UI) over legacy stations.
        var patient = GetComponent<PatientInteractable>()
                      ?? GetComponentInParent<PatientInteractable>()
                      ?? GetComponentInChildren<PatientInteractable>(true);
        if (patient != null && patient.enabled)
            return patient;

        var all = GetComponents<MonoBehaviour>();
        IInteractable fallback = null;
        foreach (var mb in all)
        {
            if (mb == null || !mb.enabled) continue;
            if (mb is not IInteractable interactable) continue;

            // Skip disabled legacy doctor station.
            if (mb is PatientDiagnosisStation) continue;

            if (mb is ManagerComputer && GetComponent<VisitorInteractable>() != null)
                continue;

            if (mb is VisitorInteractable spoken && spoken.hasBeenSpokenTo)
                continue;

            fallback = interactable;
            break;
        }

        return fallback;
    }

    public void ForceResolve()
    {
        interactableComponent = ResolveInteractable();
    }

    static bool IsPlayableCollider(Collider other)
    {
        if (other == null) return false;

        // Prefer component check — Nurse/Doctor/Janitor are not always tagged "Player".
        if (other.GetComponent<PlayerInteractionHandler>() != null)
            return true;
        if (other.GetComponentInParent<PlayerInteractionHandler>() != null)
            return true;

        if (other.CompareTag("Player")) return true;
        if (other.CompareTag("Nurse")) return true;
        if (other.CompareTag("Dr")) return true;
        if (other.CompareTag("Janitor")) return true;
        if (other.CompareTag("Manager")) return true;
        return false;
    }

    static GameObject ResolvePlayerRoot(Collider other)
    {
        var handler = other.GetComponent<PlayerInteractionHandler>()
                      ?? other.GetComponentInParent<PlayerInteractionHandler>();
        return handler != null ? handler.gameObject : other.gameObject;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayableCollider(other)) return;

        interactableComponent = ResolveInteractable();
        if (interactableComponent == null) return;

        GameObject playerGo = ResolvePlayerRoot(other);
        PlayerRoleManager roleManager = playerGo.GetComponent<PlayerRoleManager>();
        PlayerInteractionHandler player = playerGo.GetComponent<PlayerInteractionHandler>();

        if (roleManager != null && player != null)
        {
            RoleType role = roleManager.CurrentRole;
            if (CharacterSwitchManager.Instance != null)
                role = CharacterSwitchManager.Instance.ActiveRole;

            player.SetCurrentTarget(interactableComponent, role);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayableCollider(other)) return;

        GameObject playerGo = ResolvePlayerRoot(other);
        PlayerInteractionHandler player = playerGo.GetComponent<PlayerInteractionHandler>();

        if (player != null)
            player.ClearTarget();
    }
}
