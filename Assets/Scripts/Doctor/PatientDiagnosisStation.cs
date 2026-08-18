using UnityEngine;

/// <summary>
/// Legacy scene component on hospital beds. Disabled at runtime; E is handled by PatientInteractable.
/// </summary>
public class PatientDiagnosisStation : MonoBehaviour, IInteractable
{
    public DiagnosisMinigame minigame;

    public bool CanInteractWhenLocked => false;

    void Awake()
    {
        enabled = false;
    }

    public void Interact(GameObject player, RoleType role)
    {
        if (role != RoleType.Doctor && role != RoleType.Nurse)
        {
            Debug.Log("Only Nurse/Doctor can assess patients here.");
            return;
        }

        var patient = GetComponent<PatientInteractable>()
                      ?? GetComponentInChildren<PatientInteractable>(true)
                      ?? GetComponentInParent<PatientInteractable>();
        if (patient != null)
        {
            patient.Interact(player, role);
            return;
        }

        Debug.LogWarning("PatientDiagnosisStation: no PatientInteractable on this bed — legacy DoctorUI stays closed.");
    }
}
