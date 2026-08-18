using UnityEngine;

public class PlayerInteractionHandler : MonoBehaviour
{
    private IInteractable currentTarget;
    private RoleType currentRole;

    bool interactionEnabled = true;

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
    }
    public void SetCurrentTarget(IInteractable target, RoleType role)
    {
        currentTarget = target;
        currentRole = role;
    }

    public void ClearTarget()
    {
        currentTarget = null;
    }

    void Update()
    {
        if (!interactionEnabled) return;
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentTarget != null)
            {
                if (GameManager.Instance != null &&
                    !GameManager.Instance.canPerformTasks &&
                    (currentTarget == null || !currentTarget.CanInteractWhenLocked))
                {
                    Debug.Log("You must switch roles!");
                    return;
                }

                if (AudioManager.Instance != null)
                    AudioManager.Instance.Play("interact");

                currentTarget.Interact(gameObject, currentRole);
            }
        }
    }

    public RoleType GetCurrentRole() => currentRole;

    public IInteractable GetCurrentTarget() => currentTarget;
}