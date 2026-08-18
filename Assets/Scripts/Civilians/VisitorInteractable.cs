using UnityEngine;

/// <summary>
/// Manager-only visitor dialogue. Uses Inspector Backstory / Visitor Name as authored.
/// One-time interactable: after the first conversation, this visitor can no longer be used.
/// </summary>
public class VisitorInteractable : MonoBehaviour, IInteractable
{
    [Header("Dialogue (set in Inspector)")]
    [Tooltip("Shown as the visitor's spoken dialogue.")]
    [TextArea(3, 12)]
    public string backstory;

    [Tooltip("Display name shown above the dialogue.")]
    public string visitorName = "Visitor";

    [Header("Optional donation")]
    public bool canDonate;
    public int donationAmount = 250;

    [Header("State")]
    public bool donated;
    public bool hasBeenSpokenTo;

    public bool CanInteractWhenLocked => true;

    GameObject dialoguePanel;
    TMPro.TextMeshProUGUI body;
    GameObject currentPlayer;

    void Awake()
    {
        // Visitors must not also act as the manager computer station.
        var computer = GetComponent<ManagerComputer>();
        if (computer != null)
            computer.enabled = false;
    }

    public void Interact(GameObject player, RoleType role)
    {
        if (hasBeenSpokenTo)
        {
            Debug.Log($"{visitorName} has already been spoken to.");
            return;
        }

        if (role != RoleType.Manager)
        {
            Debug.Log("Only the Manager can speak with visitors.");
            return;
        }

        currentPlayer = player;
        OpenDialogue(player);

        // Consume this visitor immediately so they cannot be interacted with again
        MarkAsSpokenTo(player);
    }

    void MarkAsSpokenTo(GameObject player)
    {
        hasBeenSpokenTo = true;

        var trigger = GetComponent<InteractableTrigger>();
        if (trigger != null)
            trigger.enabled = false;

        var prompt = GetComponent<InteractPrompt>();
        if (prompt != null)
            prompt.enabled = false;

        // Clear current target so E does nothing until another interactable is entered
        if (player != null)
        {
            var handler = player.GetComponent<PlayerInteractionHandler>();
            if (handler != null)
                handler.ClearTarget();
        }

        // Keep component enabled so the open dialogue UI still works until closed
        enabled = false;
    }

    void OpenDialogue(GameObject player)
    {
        if (dialoguePanel == null)
            BindScenePanel();
        if (dialoguePanel == null)
            return;

        string displayName = string.IsNullOrWhiteSpace(visitorName) ? name : visitorName;
        string dialogue = string.IsNullOrWhiteSpace(backstory)
            ? "(No dialogue set — fill in the Backstory field on VisitorInteractable.)"
            : backstory;

        if (body != null)
            body.text = $"<b>{displayName}</b>\n\n{dialogue}";

        dialoguePanel.SetActive(true);

        var move = player.GetComponent<SimplePlayerMovement>();
        if (move != null) move.SetControlsEnabled(false);
        var cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null) cam.LockCursor(false);

        if (AudioManager.Instance != null) AudioManager.Instance.Play("open");
    }

    void BindScenePanel()
    {
        dialoguePanel = ClinicalUIFactory.FindByName("VisitorDialogue");
        if (dialoguePanel == null)
        {
            Debug.LogError("VisitorInteractable: missing scene object 'VisitorDialogue'.");
            return;
        }

        body = ClinicalUIFactory.FindLabel(dialoguePanel.transform, "BodyLabel");
        ClinicalUIFactory.BindButton(dialoguePanel.transform, "ContinueButton", OnContinue);
        dialoguePanel.SetActive(false);
    }

    void OnContinue()
    {
        if (canDonate && !donated)
        {
            donated = true;
            if (HospitalStatsManager.Instance != null)
                HospitalStatsManager.Instance.AddMoney(donationAmount);
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play("purchase");
            if (NotificationSidePanel.Instance != null)
            {
                string displayName = string.IsNullOrWhiteSpace(visitorName) ? name : visitorName;
                NotificationSidePanel.Instance.ShowRaw($"{displayName} donated R{donationAmount} to the hospital.");
            }
        }

        Close();
    }

    void Close()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        var active = CharacterSwitchManager.Instance?.ActiveCharacter;
        if (active?.movement != null)
            active.movement.SetControlsEnabled(true);
        else if (currentPlayer != null)
        {
            var move = currentPlayer.GetComponent<SimplePlayerMovement>();
            if (move != null) move.SetControlsEnabled(true);
        }

        var cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null) cam.LockCursor(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("close");
    }
}
