using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TutorialScene only — manager intro, nurse care loop, then switch to doctor.
/// Keep this beginner-friendly: one step at a time.
/// </summary>
[DefaultExecutionOrder(-50)]
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Manager dialogue")]
    public DialogueScriptable welcomeDialogue;
    public DialogueScriptable afterMedicineDialogue;

    [Header("Nurse dialogue")]
    public DialogueScriptable nurseWelcomeDialogue;
    public DialogueScriptable nurseAfterAssessDialogue;
    public DialogueScriptable nurseAfterBoxDialogue;
    public DialogueScriptable nurseOrganizerHowToDialogue;
    public DialogueScriptable nurseAfterOrganizeDialogue;
    public DialogueScriptable nurseToDoctorDialogue;

    [Header("Rooms")]
    public string managerRoomName = "TutRoom1_manager";
    public string nurseRoomName = "TutRoom2_nurse";
    public string doctorRoomName = "TutRoom3_doctor";

    [Header("Fade")]
    public float fadeSeconds = 0.6f;

    enum PSwitchTarget
    {
        None,
        Nurse,
        Doctor
    }

    DialogueManager dialogueManager;
    GameObject dialoguePanel;
    Image fadeImage;

    bool didMedicine;
    bool nurseIntroShown;
    bool nurseAfterAssessShown;
    bool nurseAfterBoxShown;
    bool nurseOrganizerHowToShown;
    bool nurseAfterOrganizeShown;
    bool nurseToDoctorShown;
    bool waitingForPToSwitch;
    bool switchingRole;
    bool dialogueBusy;
    PSwitchTarget pendingPTarget = PSwitchTarget.None;

    public bool IsDialogueBusy => dialogueBusy;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (!TutorialMode.IsActive)
        {
            enabled = false;
            return;
        }

        SetupStaticStats();
        DisableUnwantedSystems();
        SetupDialogueUi();
        SetupFadeOverlay();
        PlaceCharacterInRoom(RoleType.Manager, managerRoomName);

        StartCoroutine(SetupManagerButtonsNextFrame());

        if (CharacterSwitchManager.Instance != null)
            CharacterSwitchManager.Instance.allowOpenPanel = false;

        StartCoroutine(SetupRoleButtonsNextFrame());

        MedicineSupplyManager.OnBoxPurchased += OnMedicinePurchased;
        PatientCareSystem.OnSeverityTagged += OnSeverityTagged;
        PatientCareSystem.OnNurseTreated += OnNurseTreated;
        MedicineBoxInteractable.OnBoxUnpacked += OnMedicineBoxUnpacked;
        MedicineOrganizerMinigame.OnOrganizerStarted += OnOrganizerStarted;
        MedicineOrganizerMinigame.OnOrganizerCompleted += OnOrganizerCompleted;

        StartCoroutine(ShowWelcomeNextFrame());
    }

    void OnDestroy()
    {
        MedicineSupplyManager.OnBoxPurchased -= OnMedicinePurchased;
        PatientCareSystem.OnSeverityTagged -= OnSeverityTagged;
        PatientCareSystem.OnNurseTreated -= OnNurseTreated;
        MedicineBoxInteractable.OnBoxUnpacked -= OnMedicineBoxUnpacked;
        MedicineOrganizerMinigame.OnOrganizerStarted -= OnOrganizerStarted;
        MedicineOrganizerMinigame.OnOrganizerCompleted -= OnOrganizerCompleted;

        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!waitingForPToSwitch || pendingPTarget == PSwitchTarget.None)
            return;

        if (!Input.GetKeyDown(KeyCode.P))
            return;

        waitingForPToSwitch = false;
        var target = pendingPTarget;
        pendingPTarget = PSwitchTarget.None;

        if (dialogueManager != null && dialogueManager.IsWaitingForGate)
            dialogueManager.PassGate(true);
        else if (dialogueManager != null)
            dialogueManager.ForceClose();

        // Wait one frame so CharacterSwitchManager does not also handle this same P press.
        StartCoroutine(OpenSwitchMenuNextFrame(target));
    }

    IEnumerator OpenSwitchMenuNextFrame(PSwitchTarget target)
    {
        if (target == PSwitchTarget.Nurse)
            UnlockRoleSwitch(RoleType.Nurse, OnNurseButtonPressed);
        else if (target == PSwitchTarget.Doctor)
            UnlockRoleSwitch(RoleType.Doctor, OnDoctorButtonPressed);

        yield return null;

        if (CharacterSwitchManager.Instance != null)
            CharacterSwitchManager.Instance.OpenPanel();
    }

    IEnumerator ShowWelcomeNextFrame()
    {
        yield return null;
        yield return null;

        if (welcomeDialogue != null)
            ShowDialogue(welcomeDialogue, null);
    }

    void SetupStaticStats()
    {
        var stats = HospitalStatsManager.Instance;
        if (stats == null)
            return;

        stats.sanitation = 50f;
        stats.comfort = 50f;
        stats.morale = 50f;
        HospitalStatsManager.OnStatsChanged?.Invoke();

        if (MedicineSupplyManager.Instance != null)
        {
            MedicineSupplyManager.Instance.medicineCount = 0;
            MedicineSupplyManager.OnSupplyChanged?.Invoke();
        }
    }

    void DisableUnwantedSystems()
    {
        var notes = FindFirstObjectByType<NotificationSidePanel>();
        if (notes != null)
        {
            notes.enabled = false;
            var panel = GameObject.Find("NotificationSidePanel");
            if (panel != null)
                panel.SetActive(false);
        }

        var filth = FindFirstObjectByType<FilthSpawnSystem>();
        if (filth != null)
            filth.enabled = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CancelInvoke();
            GameManager.Instance.isPowerOut = false;
            GameManager.Instance.enabled = false;
            GameManager.Instance.canPerformTasks = true;
        }

        var outagePanel = GameObject.Find("PowerOutagePanel");
        if (outagePanel != null)
            outagePanel.SetActive(false);

        var endScreen = FindFirstObjectByType<StatsCollapseEndScreen>();
        if (endScreen != null)
            endScreen.enabled = false;
    }

    void SetupDialogueUi()
    {
        dialogueManager = FindFirstObjectByType<DialogueManager>();
        if (dialogueManager == null)
        {
            var go = new GameObject("DialogueManager");
            dialogueManager = go.AddComponent<DialogueManager>();
        }

        dialogueManager.EnsureDialogueUI();
        dialogueManager.ApplyLowerThirdLayout();
        dialoguePanel = dialogueManager.dialoguePanel;
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    void SetupFadeOverlay()
    {
        var canvas = NewUIRoot.Ensure();
        var fadeGo = new GameObject("TutorialFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fadeGo.transform.SetParent(canvas.transform, false);
        var rt = fadeGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        fadeImage = fadeGo.GetComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;
        fadeGo.transform.SetAsLastSibling();
    }

    IEnumerator SetupManagerButtonsNextFrame()
    {
        yield return null;
        yield return null;

        if (ManagerStationHub.Instance != null)
        {
            ManagerStationHub.Instance.EnsureBuilt();
            ManagerStationHub.Instance.SetNavButtonEnabled("Shift AllocationButton", false);
            ManagerStationHub.Instance.SetNavButtonEnabled("Online StoreButton", true);
            ManagerStationHub.Instance.SetNavButtonEnabled("Redirect PatientsButton", false);
        }
    }

    IEnumerator SetupRoleButtonsNextFrame()
    {
        yield return null;
        yield return null;
        SetAllRoleButtons(false);
    }

    void SetAllRoleButtons(bool enabled)
    {
        if (CharacterSwitchManager.Instance == null)
            return;

        CharacterSwitchManager.Instance.SetRoleButtonEnabled("ManagerButton", enabled);
        CharacterSwitchManager.Instance.SetRoleButtonEnabled("DoctorButton", enabled);
        CharacterSwitchManager.Instance.SetRoleButtonEnabled("NurseButton", enabled);
        CharacterSwitchManager.Instance.SetRoleButtonEnabled("JanitorButton", enabled);
    }

    void OnMedicinePurchased()
    {
        if (didMedicine)
            return;

        didMedicine = true;

        if (ManagerStationHub.Instance != null)
            ManagerStationHub.Instance.CloseAll(false);

        if (afterMedicineDialogue != null)
        {
            ShowDialogue(afterMedicineDialogue, null);
            StartCoroutine(WatchForPressPGate(PSwitchTarget.Nurse));
        }
        else
        {
            BeginWaitForP(PSwitchTarget.Nurse);
        }
    }

    IEnumerator WatchForPressPGate(PSwitchTarget target)
    {
        while (dialogueManager != null && dialogueManager.dialoguePanel != null
               && dialogueManager.dialoguePanel.activeSelf
               && !dialogueManager.IsWaitingForGate)
        {
            yield return null;
        }

        if (dialogueManager != null && dialogueManager.IsWaitingForGate)
        {
            BeginWaitForP(target);
            if (dialogueManager.Btnnext != null)
                dialogueManager.Btnnext.gameObject.SetActive(false);
        }
    }

    void BeginWaitForP(PSwitchTarget target)
    {
        pendingPTarget = target;
        waitingForPToSwitch = true;
    }

    void UnlockRoleSwitch(RoleType role, Action onPressed)
    {
        if (CharacterSwitchManager.Instance == null)
            return;

        CharacterSwitchManager.Instance.allowOpenPanel = true;
        SetAllRoleButtons(false);

        string buttonName = role switch
        {
            RoleType.Nurse => "NurseButton",
            RoleType.Doctor => "DoctorButton",
            RoleType.Janitor => "JanitorButton",
            _ => "ManagerButton"
        };

        CharacterSwitchManager.Instance.SetRoleButtonEnabled(buttonName, true);
        CharacterSwitchManager.Instance.BindRoleButtonOverride(buttonName, onPressed);

        if (dialogueManager != null && dialogueManager.Btnnext != null)
            dialogueManager.Btnnext.gameObject.SetActive(true);
    }

    void OnNurseButtonPressed()
    {
        if (switchingRole)
            return;
        StartCoroutine(FadeSwitchToRole(RoleType.Nurse, nurseRoomName, AfterArrivedAsNurse));
    }

    void OnDoctorButtonPressed()
    {
        if (switchingRole)
            return;
        StartCoroutine(FadeSwitchToRole(RoleType.Doctor, doctorRoomName, null));
    }

    IEnumerator FadeSwitchToRole(RoleType role, string roomName, Action afterFadeIn)
    {
        switchingRole = true;

        if (CharacterSwitchManager.Instance != null)
        {
            CharacterSwitchManager.Instance.ClosePanel();
            CharacterSwitchManager.Instance.allowOpenPanel = false;
        }

        yield return Fade(0f, 1f);

        PlaceCharacterInRoom(role, roomName);

        var character = FindCharacter(role);
        if (character != null && CharacterSwitchManager.Instance != null)
            CharacterSwitchManager.Instance.SwitchTo(character, playSound: false);

        var cam = CharacterSwitchManager.Instance != null
            ? CharacterSwitchManager.Instance.cameraFollow
            : FindFirstObjectByType<CameraFollow>();
        if (cam != null)
            cam.SnapToTarget();

        yield return null;
        if (cam != null)
            cam.SnapToTarget();

        yield return Fade(1f, 0f);

        switchingRole = false;

        // Show nurse/doctor tutorial UI the moment the screen is visible again.
        if (afterFadeIn != null)
            StartCoroutine(RunAfterFadeIn(afterFadeIn));
    }

    IEnumerator RunAfterFadeIn(Action afterFadeIn)
    {
        // One frame so the new role / camera are fully active first.
        yield return null;
        afterFadeIn?.Invoke();
    }

    void AfterArrivedAsNurse()
    {
        if (nurseIntroShown)
            return;

        nurseIntroShown = true;

        // Treat stays locked until medicine has been organised.
        PatientInteractable.BlockNurseTreat = true;

        // Same lower-third dialogue UI as the manager intro.
        if (dialogueManager != null)
        {
            dialogueManager.EnsureDialogueUI();
            dialogueManager.ApplyLowerThirdLayout();
            if (dialogueManager.Btnnext != null)
                dialogueManager.Btnnext.gameObject.SetActive(true);
            if (dialogueManager.dialoguePanel != null)
                dialogueManager.dialoguePanel.transform.SetAsLastSibling();
        }

        if (nurseWelcomeDialogue != null)
            ShowDialogue(nurseWelcomeDialogue, null);
        else
            Debug.LogWarning("TutorialManager: nurseWelcomeDialogue is not assigned.");
    }

    void OnSeverityTagged(PatientRecord record)
    {
        if (!nurseIntroShown || nurseAfterAssessShown || dialogueBusy)
            return;

        // Guide to the medicine box after the first assessment.
        // Keep Treat locked until the organizer is completed.
        nurseAfterAssessShown = true;
        PatientInteractable.BlockNurseTreat = true;
        StartCoroutine(ShowAfterAssessGuided());
    }

    IEnumerator ShowAfterAssessGuided()
    {
        yield return null;

        SetNurseTreatButtonInteractable(false);

        if (dialogueManager != null)
        {
            dialogueManager.EnsureDialogueUI();
            if (dialogueManager.dialoguePanel != null)
                dialogueManager.dialoguePanel.transform.SetAsLastSibling();
        }

        if (nurseAfterAssessDialogue != null)
            ShowDialogue(nurseAfterAssessDialogue, CloseNurseAssessmentWithDialogue);
        else
            CloseNurseAssessmentWithDialogue();
    }

    void CloseNurseAssessmentWithDialogue()
    {
        // Do not unlock Treat here — medicine must be organised first.
        PatientInteractable.CloseOpenCarePanels();
    }

    static void SetNurseTreatButtonInteractable(bool enabled)
    {
        var panel = ClinicalUIFactory.FindByName("NursePatientCarePanel");
        if (panel == null)
            return;

        var treatT = ClinicalUIFactory.FindChild(panel.transform, "TreatButton");
        if (treatT == null)
            return;

        var treatButton = treatT.GetComponent<Button>();
        if (treatButton != null)
            treatButton.interactable = enabled;
    }

    void OnMedicineBoxUnpacked()
    {
        if (!nurseAfterAssessShown || nurseAfterBoxShown)
            return;

        nurseAfterBoxShown = true;
        StartCoroutine(ShowAfterBoxNextFrame());
    }

    IEnumerator ShowAfterBoxNextFrame()
    {
        while (dialogueBusy)
            yield return null;

        // Let the box unpack / shelf spawn finish before locking the player in dialogue.
        yield return null;
        yield return null;
        if (nurseAfterBoxDialogue != null)
            ShowDialogue(nurseAfterBoxDialogue, null);
    }

    void OnOrganizerStarted()
    {
        if (!nurseAfterBoxShown || nurseOrganizerHowToShown)
            return;

        nurseOrganizerHowToShown = true;
        StartCoroutine(ShowOrganizerHowToNextFrame());
    }

    IEnumerator ShowOrganizerHowToNextFrame()
    {
        // Wait until any prior dialogue has closed, then teach the swap controls.
        while (dialogueBusy)
            yield return null;

        yield return null;
        if (nurseOrganizerHowToDialogue != null)
            ShowDialogue(nurseOrganizerHowToDialogue, null);
    }

    void OnOrganizerCompleted()
    {
        if (!nurseOrganizerHowToShown || nurseAfterOrganizeShown)
            return;

        nurseAfterOrganizeShown = true;
        // Medicine is available — Treat can be used on assessed patients.
        PatientInteractable.BlockNurseTreat = false;
        StartCoroutine(ShowAfterOrganizeNextFrame());
    }

    IEnumerator ShowAfterOrganizeNextFrame()
    {
        // Let the minigame exit first so the dialogue is readable.
        while (dialogueBusy)
            yield return null;

        yield return null;
        yield return null;
        if (nurseAfterOrganizeDialogue != null)
            ShowDialogue(nurseAfterOrganizeDialogue, null);
    }

    void OnNurseTreated(PatientRecord record)
    {
        if (!nurseAfterOrganizeShown || nurseToDoctorShown)
            return;

        if (!AllTutorialPatientsTreated())
            return;

        nurseToDoctorShown = true;
        StartCoroutine(ShowNurseToDoctorWhenReady());
    }

    IEnumerator ShowNurseToDoctorWhenReady()
    {
        while (dialogueBusy)
            yield return null;

        yield return null;
        if (nurseToDoctorDialogue != null)
        {
            ShowDialogue(nurseToDoctorDialogue, null);
            StartCoroutine(WatchForPressPGate(PSwitchTarget.Doctor));
        }
        else
        {
            BeginWaitForP(PSwitchTarget.Doctor);
        }
    }

    bool AllTutorialPatientsTreated()
    {
        if (PatientCareSystem.Instance == null)
            return false;

        int treated = 0;
        int total = 0;
        foreach (var p in PatientCareSystem.Instance.patients)
        {
            if (p == null || p.worldObject == null) continue;
            if (!IsTutorialNursePatientObject(p.worldObject))
                continue;

            total++;
            if (p.treatedByNurse || p.recovered)
                treated++;
        }

        return total > 0 && treated >= total;
    }

    static bool IsTutorialNursePatientObject(GameObject go)
    {
        if (go == null) return false;
        string n = go.name;
        return n.Equals("NursePatient1", System.StringComparison.OrdinalIgnoreCase)
            || n.Equals("NursePatient2", System.StringComparison.OrdinalIgnoreCase);
    }

    IEnumerator Fade(float from, float to)
    {
        if (fadeImage == null)
            yield break;

        fadeImage.raycastTarget = true;
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeSeconds));
            fadeImage.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }

        fadeImage.color = new Color(0f, 0f, 0f, to);
        fadeImage.raycastTarget = to > 0.01f;
    }

    void ShowDialogue(DialogueScriptable data, Action onFinished)
    {
        if (dialogueManager == null || data == null)
        {
            onFinished?.Invoke();
            return;
        }

        dialogueBusy = true;
        LockPlayer(true);
        dialogueManager.Play(data, () =>
        {
            dialogueBusy = false;
            LockPlayer(false);
            onFinished?.Invoke();
        });
    }

    void LockPlayer(bool freeze)
    {
        var activeChar = CharacterSwitchManager.Instance != null
            ? CharacterSwitchManager.Instance.ActiveCharacter
            : null;

        if (activeChar != null && activeChar.movement != null)
            activeChar.movement.SetControlsEnabled(!freeze);

        var cam = FindFirstObjectByType<CameraFollow>();

        if (freeze)
        {
            if (cam != null)
                cam.LockCursor(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        // Keep the cursor free during the shelf sorting mini-game.
        bool organizerActive = MedicineOrganizerMinigame.Instance != null
            && MedicineOrganizerMinigame.Instance.IsActive;
        if (organizerActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (cam != null)
            cam.LockCursor(true);
    }

    void PlaceCharacterInRoom(RoleType role, string roomName)
    {
        var character = FindCharacter(role);
        var room = GameObject.Find(roomName);
        if (character == null || room == null)
            return;

        Vector3 pos = room.transform.position;
        pos.y = character.transform.position.y;
        character.transform.position = pos;
    }

    PlayableCharacter FindCharacter(RoleType role)
    {
        if (CharacterSwitchManager.Instance == null)
            return null;

        foreach (var c in CharacterSwitchManager.Instance.characters)
        {
            if (c != null && c.role == role)
                return c;
        }

        return null;
    }
}
