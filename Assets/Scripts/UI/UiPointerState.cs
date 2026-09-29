using UnityEngine;

/// <summary>
/// Keeps the mouse free while a modal UI panel is open so buttons stay clickable.
/// </summary>
public static class UiPointerState
{
    static readonly string[] ModalPanelNames =
    {
        "NursePatientCarePanel",
        "DoctorPatientCarePanel",
        "ManagerNavPanel",
        "OnlineStorePanel",
        "RedirectPatientsPanel",
        "CharacterSelectPanel",
        "VisitorDialogue",
        "JobSelectionOverlayUI",
        "NurseUI-IV",
        "DictionaryUI",
        "Dictionary"
    };

    public static bool ShouldKeepCursorFree()
    {
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsPaused)
            return true;

        if (CharacterSwitchManager.Instance != null && CharacterSwitchManager.Instance.IsSelectionPanelOpen)
            return true;

        if (MedicineOrganizerMinigame.Instance != null && MedicineOrganizerMinigame.Instance.IsActive)
            return true;

        var dialogue = Object.FindFirstObjectByType<DialogueManager>();
        if (dialogue != null && dialogue.IsPlaying)
            return true;

        var iv = Object.FindFirstObjectByType<IVMinigame>();
        if (iv != null && iv.minigameUI != null && iv.minigameUI.activeInHierarchy)
            return true;

        for (int i = 0; i < ModalPanelNames.Length; i++)
        {
            var go = ClinicalUIFactory.FindByName(ModalPanelNames[i]);
            if (go != null && go.activeInHierarchy)
                return true;
        }

        return false;
    }

    public static void ApplyFreeCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
