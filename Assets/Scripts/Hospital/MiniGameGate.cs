using UnityEngine;

/// <summary>
/// Mini-games only start from designated props (cart / patient).
/// </summary>
public class MiniGameGate : MonoBehaviour
{
    public static MiniGameGate Instance;

    public bool janitorUnlocked;
    public bool nurseUnlocked;
    public bool doctorUnlocked;

    float janitorUntil;
    float nurseUntil;
    float doctorUntil;

    const float UnlockWindow = 120f;

    void Awake() => Instance = this;

    public void Unlock(RoleType role)
    {
        switch (role)
        {
            case RoleType.Janitor:
                janitorUnlocked = true;
                janitorUntil = Time.time + UnlockWindow;
                break;
            case RoleType.Nurse:
                nurseUnlocked = true;
                nurseUntil = Time.time + UnlockWindow;
                break;
            case RoleType.Doctor:
                doctorUnlocked = true;
                doctorUntil = Time.time + UnlockWindow;
                break;
        }
    }

    public bool CanStart(RoleType role)
    {
        // Patients always allow nurse/doctor checks when interacting with patient props.
        // Cart unlock gates janitor mop/trash cart minigame flow.
        switch (role)
        {
            case RoleType.Janitor:
                return janitorUnlocked && Time.time <= janitorUntil;
            case RoleType.Nurse:
                return true; // patient prop itself is the gate
            case RoleType.Doctor:
                return true;
            default:
                return true;
        }
    }
}

/// <summary>Interact with JanitorCart_Final to unlock janitor mini-game window.</summary>
public class JanitorCartPropGate : MonoBehaviour, IInteractable
{
    public bool CanInteractWhenLocked => false;

    public void Interact(GameObject player, RoleType role)
    {
        if (role != RoleType.Janitor) return;
        if (MiniGameGate.Instance != null)
            MiniGameGate.Instance.Unlock(RoleType.Janitor);
        if (MiniGameTimerUI.Instance != null)
            MiniGameTimerUI.Instance.StartTimer("Janitor Shift Tasks", 120f, null);
        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("equipment");
        Debug.Log("Janitor cart ready — mop and bins available.");
    }
}
