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
    public DialogueScriptable managerReturnWelcomeDialogue;
    public DialogueScriptable managerRedirectTipDialogue;
    public DialogueScriptable managerRedirectDoneDialogue;
    public DialogueScriptable managerVisitorTipDialogue;
    public DialogueScriptable managerTutorialCompleteDialogue;

    [Header("Nurse dialogue")]
    public DialogueScriptable nurseWelcomeDialogue;
    public DialogueScriptable nurseAfterAssessDialogue;
    public DialogueScriptable nurseAfterBoxDialogue;
    public DialogueScriptable nurseOrganizerHowToDialogue;
    public DialogueScriptable nurseAfterOrganizeDialogue;
    public DialogueScriptable nurseToDoctorDialogue;

    [Header("Doctor dialogue")]
    public DialogueScriptable doctorWelcomeDialogue;
    public DialogueScriptable doctorToJanitorDialogue;

    [Header("Janitor dialogue")]
    public DialogueScriptable janitorWelcomeDialogue;
    public DialogueScriptable janitorAfterCartDialogue;
    public DialogueScriptable janitorNearRedTrashDialogue;
    public DialogueScriptable janitorAfterPickupDialogue;
    public DialogueScriptable janitorAfterDisposeDialogue;
    public DialogueScriptable janitorToManagerDialogue;

    [Header("Rooms")]
    public string managerRoomName = "TutRoom1_manager";
    public string nurseRoomName = "TutRoom2_nurse";
    public string doctorRoomName = "TutRoom3_doctor";
    public string janitorRoomName = "TutRoom4_janitor";

    [Header("Janitor tutorial")]
    public float redTrashNearDistance = 3.5f;

    [Header("Fade")]
    public float fadeSeconds = 0.6f;
    public string hubLevelSceneName = "HospitalHubLevel";
    public AudioClip ambulanceMusic;
    [Tooltip("Scene AudioSource on the Ambulence object — preferred playback for the outro.")]
    public AudioSource ambulanceSource;
    public string ambulanceObjectName = "Ambulence";
    public float incomingPatientsHoldSeconds = 2.5f;
    [Range(0.5f, 1f)] public float ambulanceVolume = 1f;
    [Range(1f, 8f)] public float ambulanceGain = 4f;
    public float ambulanceSoftVolume = 0.45f;
    public float ambulanceFadeSeconds = 1.5f;

    enum PSwitchTarget
    {
        None,
        Nurse,
        Doctor,
        Janitor,
        Manager
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
    bool doctorIntroShown;
    bool doctorToJanitorShown;
    bool janitorIntroShown;
    bool janitorAfterCartShown;
    bool janitorNearRedShown;
    bool janitorAfterPickupShown;
    bool janitorAfterDisposeShown;
    bool janitorCleanupDone;
    bool janitorToManagerShown;
    bool managerRedirectPhaseStarted;
    bool managerReturnWelcomeShown;
    bool managerRedirectTipShown;
    bool managerRedirectDoneShown;
    bool managerVisitorTipShown;
    bool tutorialComplete;
    bool tutorialEnding;
    bool waitingForRedirectPanel;
    bool waitingForNonCriticalRedirect;
    bool waitingForVisitorDonation;
    bool waitingForCartInteract;
    bool waitingNearRedTrash;
    bool waitingRedTrashPickup;
    bool waitingTrashDispose;
    bool waitingJanitorCleanup;
    bool janitorDroppedCart;
    bool janitorPickedRedTrash;
    readonly System.Collections.Generic.HashSet<string> janitorCleanupRemaining =
        new System.Collections.Generic.HashSet<string>();

    static readonly string[] JanitorCleanupNames =
    {
        "SpillOne", "TrashGroup1", "GreenTrash (1)", "Syringe", "SpillThree",
        "Bandage", "SpillTwo", "BlueTrash", "BoxTwo", "RedTrash"
    };

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
        AudioManager.EnsureAudioListener();
        SetupDialogueUi();
        SetupFadeOverlay();
        HideTutorialVisitorUntilManagerReturn();
        PlaceCharacterInRoom(RoleType.Manager, managerRoomName);

        StartCoroutine(SetupManagerButtonsNextFrame());

        if (CharacterSwitchManager.Instance != null)
            CharacterSwitchManager.Instance.allowOpenPanel = false;

        StartCoroutine(SetupRoleButtonsNextFrame());

        MedicineSupplyManager.OnBoxPurchased += OnMedicinePurchased;
        PatientCareSystem.OnSeverityTagged += OnSeverityTagged;
        PatientCareSystem.OnNurseTreated += OnNurseTreated;
        PatientCareSystem.OnDoctorTreated += OnDoctorTreated;
        MedicineBoxInteractable.OnBoxUnpacked += OnMedicineBoxUnpacked;
        MedicineOrganizerMinigame.OnOrganizerStarted += OnOrganizerStarted;
        MedicineOrganizerMinigame.OnOrganizerCompleted += OnOrganizerCompleted;
        JanitorCartController.OnCartAttached += OnJanitorCartAttached;
        JanitorCartController.OnCartDetached += OnJanitorCartDetached;
        JanitorAbilities.OnTrashPickedUp += OnJanitorTrashPickedUp;
        JanitorAbilities.OnTrashDisposed += OnJanitorTrashDisposed;
        DirtPile.OnDirtCleaned += OnJanitorDirtCleaned;
        ManagerStationHub.OnRedirectPanelOpened += OnRedirectPanelOpened;
        PatientCareSystem.OnPatientRedirected += OnPatientRedirected;
        VisitorInteractable.OnDonationAccepted += OnVisitorDonationAccepted;

        StartCoroutine(ShowWelcomeNextFrame());
    }

    void OnDestroy()
    {
        MedicineSupplyManager.OnBoxPurchased -= OnMedicinePurchased;
        PatientCareSystem.OnSeverityTagged -= OnSeverityTagged;
        PatientCareSystem.OnNurseTreated -= OnNurseTreated;
        PatientCareSystem.OnDoctorTreated -= OnDoctorTreated;
        MedicineBoxInteractable.OnBoxUnpacked -= OnMedicineBoxUnpacked;
        MedicineOrganizerMinigame.OnOrganizerStarted -= OnOrganizerStarted;
        MedicineOrganizerMinigame.OnOrganizerCompleted -= OnOrganizerCompleted;
        JanitorCartController.OnCartAttached -= OnJanitorCartAttached;
        JanitorCartController.OnCartDetached -= OnJanitorCartDetached;
        JanitorAbilities.OnTrashPickedUp -= OnJanitorTrashPickedUp;
        JanitorAbilities.OnTrashDisposed -= OnJanitorTrashDisposed;
        DirtPile.OnDirtCleaned -= OnJanitorDirtCleaned;
        ManagerStationHub.OnRedirectPanelOpened -= OnRedirectPanelOpened;
        PatientCareSystem.OnPatientRedirected -= OnPatientRedirected;
        VisitorInteractable.OnDonationAccepted -= OnVisitorDonationAccepted;

        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // Keep dialogue input locked every frame while a line is up.
        if (dialogueBusy || tutorialEnding)
            LockPlayer(true);
        // Keep the mouse free while waiting to click Redirect on a patient.
        else if (waitingForNonCriticalRedirect || IsManagerStationUiOpen())
            KeepUiCursorVisible();

        TickJanitorTutorial();

        // Fallback: if all doctor patients are done, show janitor handoff even if an event was missed.
        bool playingAsDoctor = CharacterSwitchManager.Instance != null
            && CharacterSwitchManager.Instance.ActiveRole == RoleType.Doctor;
        if ((doctorIntroShown || playingAsDoctor) && !doctorToJanitorShown && AllTutorialDoctorPatientsTreated())
        {
            doctorIntroShown = true;
            doctorToJanitorShown = true;
            StartCoroutine(ShowDoctorToJanitorWhenReady());
        }

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

        dialogueBusy = false;

        // Wait one frame so CharacterSwitchManager does not also handle this same P press.
        StartCoroutine(OpenSwitchMenuNextFrame(target));
    }

    IEnumerator OpenSwitchMenuNextFrame(PSwitchTarget target)
    {
        if (target == PSwitchTarget.Nurse)
            UnlockRoleSwitch(RoleType.Nurse, OnNurseButtonPressed);
        else if (target == PSwitchTarget.Doctor)
            UnlockRoleSwitch(RoleType.Doctor, OnDoctorButtonPressed);
        else if (target == PSwitchTarget.Janitor)
            UnlockRoleSwitch(RoleType.Janitor, OnJanitorButtonPressed);
        else if (target == PSwitchTarget.Manager)
            UnlockRoleSwitch(RoleType.Manager, OnManagerButtonPressed);

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

    void OnManagerButtonPressed()
    {
        if (switchingRole)
            return;
        StartCoroutine(FadeSwitchToRole(RoleType.Manager, managerRoomName, AfterArrivedAsManager));
    }

    void AfterArrivedAsManager()
    {
        // Only the post-janitor return unlocks redirect + the visitor.
        if (!janitorToManagerShown || managerRedirectPhaseStarted)
            return;

        managerRedirectPhaseStarted = true;
        EnsureRedirectTutorialPatients();
        StartCoroutine(SetupRedirectOnlyButtonsNextFrame());
        ActivateTutorialVisitor(interactable: false);

        if (dialogueManager != null)
        {
            dialogueManager.EnsureDialogueUI();
            dialogueManager.ApplyLowerThirdLayout();
            if (dialogueManager.Btnnext != null)
                dialogueManager.Btnnext.gameObject.SetActive(true);
            if (dialogueManager.dialoguePanel != null)
                dialogueManager.dialoguePanel.transform.SetAsLastSibling();
        }

        if (managerReturnWelcomeDialogue != null)
        {
            managerReturnWelcomeShown = true;
            ShowDialogue(managerReturnWelcomeDialogue, () =>
            {
                waitingForRedirectPanel = true;
            });
        }
        else
        {
            managerReturnWelcomeShown = true;
            waitingForRedirectPanel = true;
            Debug.LogWarning("TutorialManager: managerReturnWelcomeDialogue is not assigned.");
        }
    }

    IEnumerator SetupRedirectOnlyButtonsNextFrame()
    {
        yield return null;
        yield return null;

        if (ManagerStationHub.Instance == null)
            yield break;

        ManagerStationHub.Instance.EnsureBuilt();
        ManagerStationHub.Instance.SetNavButtonEnabled("Shift AllocationButton", false);
        ManagerStationHub.Instance.SetNavButtonEnabled("Online StoreButton", false);
        ManagerStationHub.Instance.SetNavButtonEnabled("Redirect PatientsButton", true);
    }

    void OnRedirectPanelOpened()
    {
        if (!waitingForRedirectPanel || managerRedirectTipShown || dialogueBusy)
            return;

        waitingForRedirectPanel = false;
        managerRedirectTipShown = true;

        if (dialogueManager != null)
        {
            dialogueManager.EnsureDialogueUI();
            dialogueManager.ApplyLowerThirdLayout();
            if (dialogueManager.Btnnext != null)
                dialogueManager.Btnnext.gameObject.SetActive(true);
            if (dialogueManager.dialoguePanel != null)
                dialogueManager.dialoguePanel.transform.SetAsLastSibling();
        }

        if (managerRedirectTipDialogue != null)
        {
            ShowDialogue(managerRedirectTipDialogue, () =>
            {
                waitingForNonCriticalRedirect = true;
            });
        }
        else
        {
            waitingForNonCriticalRedirect = true;
            Debug.LogWarning("TutorialManager: managerRedirectTipDialogue is not assigned.");
        }
    }

    void OnPatientRedirected(PatientRecord record)
    {
        if (!waitingForNonCriticalRedirect || managerRedirectDoneShown || dialogueBusy)
            return;

        if (record == null || record.severity != PatientSeverity.NotCritical)
            return;

        waitingForNonCriticalRedirect = false;
        managerRedirectDoneShown = true;
        StartCoroutine(ShowRedirectDoneThenVisitorTip());
    }

    IEnumerator ShowRedirectDoneThenVisitorTip()
    {
        while (dialogueBusy)
            yield return null;

        yield return null;

        if (managerRedirectDoneDialogue != null)
        {
            bool doneClosed = false;
            ShowDialogue(managerRedirectDoneDialogue, () => { doneClosed = true; });
            while (!doneClosed)
                yield return null;
        }

        yield return null;

        if (managerVisitorTipDialogue != null)
        {
            bool tipClosed = false;
            managerVisitorTipShown = true;
            ShowDialogue(managerVisitorTipDialogue, () => { tipClosed = true; });
            while (!tipClosed)
                yield return null;
        }
        else
        {
            managerVisitorTipShown = true;
            Debug.LogWarning("TutorialManager: managerVisitorTipDialogue is not assigned.");
        }

        if (ManagerStationHub.Instance != null)
            ManagerStationHub.Instance.CloseAll(false);

        // Visitor is visible earlier, but only interactable after a successful redirect.
        SetTutorialVisitorInteractable(true);
        waitingForVisitorDonation = true;
    }

    void OnVisitorDonationAccepted(VisitorInteractable visitor)
    {
        if (!waitingForVisitorDonation || tutorialComplete)
            return;

        waitingForVisitorDonation = false;
        tutorialComplete = true;
        StartCoroutine(ShowTutorialCompleteThenHubLevel());
    }

    IEnumerator ShowTutorialCompleteThenHubLevel()
    {
        while (dialogueBusy)
            yield return null;

        // Let the visitor panel finish closing before reclaiming ContinueButton.
        yield return null;
        yield return null;

        if (dialogueManager == null)
            SetupDialogueUi();

        if (dialogueManager != null)
        {
            dialogueManager.EnsureDialogueUI();
            dialogueManager.RebindContinueButton();
            dialogueManager.ApplyLowerThirdLayout();
            if (dialogueManager.Btnnext != null)
            {
                dialogueManager.Btnnext.gameObject.SetActive(true);
                dialogueManager.Btnnext.interactable = true;
            }
            if (dialogueManager.dialoguePanel != null)
                dialogueManager.dialoguePanel.transform.SetAsLastSibling();
        }

        if (managerTutorialCompleteDialogue == null)
        {
            Debug.LogWarning("TutorialManager: managerTutorialCompleteDialogue is not assigned.");
            yield return FadeOutToHubLevel();
            yield break;
        }

        bool endingStarted = false;
        bool ambulanceStarted = false;
        dialogueBusy = true;
        LockPlayer(true);

        dialogueManager.Play(managerTutorialCompleteDialogue, null, showNextButton: true, lineShown: index =>
        {
            // Second line is on screen — keep siren going, then hold and fade out.
            if (index != 1 || endingStarted)
                return;

            endingStarted = true;
            tutorialEnding = true;

            if (!ambulanceStarted)
            {
                ambulanceStarted = true;
                PlayAmbulanceMusic();
            }

            if (dialogueManager != null)
            {
                if (dialogueManager.dialoguePanel != null)
                {
                    dialogueManager.dialoguePanel.SetActive(true);
                    dialogueManager.dialoguePanel.transform.SetAsLastSibling();
                }

                if (dialogueManager.Btnnext != null)
                    dialogueManager.Btnnext.gameObject.SetActive(false);
            }

            StartCoroutine(HoldIncomingPatientsThenFadeToHub());
        });

        // Play() binds Continue → NextDialogue. Replace so Continue on line 0 starts the siren first.
        if (dialogueManager.Btnnext != null)
        {
            dialogueManager.Btnnext.onClick.RemoveAllListeners();
            dialogueManager.Btnnext.onClick.AddListener(() =>
            {
                if (dialogueManager == null)
                    return;

                if (dialogueManager.CurrentDialogueIndex == 0 && !ambulanceStarted)
                {
                    ambulanceStarted = true;
                    PlayAmbulanceMusic();
                }

                dialogueManager.NextDialogue();
            });

            if (AudioManager.Instance != null)
                AudioManager.Instance.HookButton(dialogueManager.Btnnext);
        }

        LockPlayer(true);
    }

    IEnumerator HoldIncomingPatientsThenFadeToHub()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, incomingPatientsHoldSeconds));
        yield return FadeOutToHubLevel();
    }

    IEnumerator FadeOutToHubLevel()
    {
        tutorialEnding = true;

        if (dialogueManager != null)
            dialogueManager.ForceClose();

        dialogueBusy = false;
        LockPlayer(true);

        if (fadeImage != null)
            fadeImage.transform.SetAsLastSibling();

        // Screen fades while ambulance keeps playing.
        yield return Fade(0f, 1f);

        // Then soften the ambulance audio on the Ambulence source.
        yield return FadeAmbulanceVolume(ambulanceSoftVolume, ambulanceFadeSeconds);

        yield return new WaitForSecondsRealtime(0.35f);

        // Restore normal hub BGM under the black screen before the scene swap.
        if (AudioManager.Instance != null && AudioManager.Instance.backgroundMusic != null)
            AudioManager.Instance.PlayMusic(AudioManager.Instance.backgroundMusic);

        FeatureBootstrap.PrepareForSceneRestart();
        UnityEngine.SceneManagement.SceneManager.LoadScene(hubLevelSceneName);
    }

    void PlayAmbulanceMusic()
    {
        var clip = ambulanceMusic != null ? ambulanceMusic : ResolveAmbulanceClip();
        var source = ResolveAmbulanceSource();

        if (clip == null && source != null)
            clip = source.clip;

        if (clip == null)
        {
            Debug.LogWarning("TutorialManager: Ambulence clip missing. Assign ambulanceMusic (Ambulence Sound).");
            return;
        }

        // Fully mute BGM so the siren is clear.
        if (AudioManager.Instance != null && AudioManager.Instance.musicSource != null)
        {
            AudioManager.Instance.musicSource.Stop();
            AudioManager.Instance.musicSource.volume = 0f;
        }

        AudioManager.EnsureAudioListener();

        if (source == null)
        {
            var go = new GameObject("TutorialAmbulanceAudio");
            DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
        }

        source.gameObject.SetActive(true);
        source.enabled = true;
        source.Stop();
        source.clip = clip;
        source.spatialBlend = 0f;
        source.bypassListenerEffects = true;
        source.bypassReverbZones = true;
        source.bypassEffects = true;
        source.mute = false;
        source.priority = 0;
        source.pitch = 1f;
        source.loop = true;
        source.volume = Mathf.Clamp01(ambulanceVolume);
        source.Play();
        ambulanceSource = source;

        // Extra oneshot so the siren is heard even if looping Play fails on some clips.
        source.PlayOneShot(clip, Mathf.Clamp01(ambulanceVolume));

        if (!source.isPlaying)
            Debug.LogWarning("TutorialManager: Ambulence AudioSource failed to play.");
    }

    AudioSource ResolveAmbulanceSource()
    {
        if (ambulanceSource != null)
            return ambulanceSource;

        string[] names = { ambulanceObjectName, "Ambulence", "Amburence", "Ambulance" };
        for (int i = 0; i < names.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(names[i]))
                continue;

            var go = GameObject.Find(names[i]);
            if (go == null)
                continue;

            var src = go.GetComponent<AudioSource>();
            if (src != null)
                return src;
        }

        // Include inactive objects under the Audio root.
        var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in transforms)
        {
            if (t == null)
                continue;

            for (int i = 0; i < names.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(names[i]) || t.name != names[i])
                    continue;

                var src = t.GetComponent<AudioSource>();
                if (src != null)
                    return src;
            }
        }

        return null;
    }

    IEnumerator FadeAmbulanceVolume(float toVolume, float duration)
    {
        var source = ambulanceSource != null ? ambulanceSource : ResolveAmbulanceSource();
        if (source == null)
            yield break;

        float from = source.volume;
        float to = Mathf.Clamp01(toVolume);
        if (duration <= 0.01f)
        {
            source.volume = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }

        source.volume = to;
    }

    static AudioClip ResolveAmbulanceClip()
    {
        string[] names =
        {
            "Ambulence Sound", "Ambulance Sound", "Abulance Sound",
            "Ambulance", "AmbulanceMusic", "ambulance", "Siren"
        };

        for (int i = 0; i < names.Length; i++)
        {
            var fromResources = Resources.Load<AudioClip>("Sounds/" + names[i]);
            if (fromResources != null)
                return fromResources;

#if UNITY_EDITOR
            string[] paths =
            {
                $"Assets/Audio/{names[i]}.mp3",
                $"Assets/Audio/{names[i]}.wav",
                $"Assets/Audio/{names[i]}.ogg",
                $"Assets/Resources/Sounds/{names[i]}.mp3",
                $"Assets/Resources/Sounds/{names[i]}.wav",
                $"Assets/Resources/Sounds/{names[i]}.ogg"
            };
            for (int p = 0; p < paths.Length; p++)
            {
                var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(paths[p]);
                if (clip != null)
                    return clip;
            }
#endif
        }

        return null;
    }

    /// <summary>
    /// After nurse/doctor loops, patients may be recovered. Re-seed one Not Critical
    /// and one Critical tagged patient so Redirect Patients has a clear teaching example.
    /// </summary>
    void EnsureRedirectTutorialPatients()
    {
        if (PatientCareSystem.Instance == null)
            return;

        PatientRecord notCritical = null;
        PatientRecord critical = null;

        foreach (var p in PatientCareSystem.Instance.patients)
        {
            if (p == null)
                continue;

            if (notCritical == null && (p.tutorialNurseTarget || p.severity == PatientSeverity.NotCritical))
                notCritical = p;
            else if (critical == null && (p.tutorialDoctorTarget || p.severity == PatientSeverity.Critical))
                critical = p;
        }

        if (notCritical == null)
        {
            foreach (var p in PatientCareSystem.Instance.patients)
            {
                if (p != null)
                {
                    notCritical = p;
                    break;
                }
            }
        }

        if (critical == null)
        {
            foreach (var p in PatientCareSystem.Instance.patients)
            {
                if (p != null && p != notCritical)
                {
                    critical = p;
                    break;
                }
            }
        }

        PrepareRedirectPatient(notCritical, PatientSeverity.NotCritical);
        PrepareRedirectPatient(critical, PatientSeverity.Critical);
    }

    static void PrepareRedirectPatient(PatientRecord record, PatientSeverity severity)
    {
        if (record == null)
            return;

        record.severity = severity;
        record.severityTagged = true;
        record.recovered = false;
        if (record.worldObject != null && !record.worldObject.activeSelf)
            record.worldObject.SetActive(true);
    }

    void HideTutorialVisitorUntilManagerReturn()
    {
        var visitor = FindNamedObjectIncludingInactive("Male Character (1)");
        if (visitor != null)
            visitor.SetActive(false);
    }

    void ActivateTutorialVisitor(bool interactable)
    {
        var visitor = FindNamedObjectIncludingInactive("Male Character (1)");
        if (visitor == null)
        {
            Debug.LogWarning("TutorialManager: could not find 'Male Character (1)' to activate.");
            return;
        }

        visitor.SetActive(true);

        var visitorInteractable = visitor.GetComponent<VisitorInteractable>();
        if (visitorInteractable == null)
            visitorInteractable = visitor.AddComponent<VisitorInteractable>();

        if (string.IsNullOrWhiteSpace(visitorInteractable.visitorName) || visitorInteractable.visitorName == "Visitor")
            visitorInteractable.visitorName = "Visitor";

        visitorInteractable.canDonate = true;
        if (visitorInteractable.donationAmount <= 0)
            visitorInteractable.donationAmount = 250;

        if (string.IsNullOrWhiteSpace(visitorInteractable.backstory))
        {
            visitorInteractable.backstory =
                "Thank you for taking the time to listen. I'd like to donate to support the hospital.";
        }

        if (visitor.GetComponent<Collider>() == null)
        {
            var col = visitor.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.height = 2f;
        }
        else
        {
            visitor.GetComponent<Collider>().isTrigger = true;
        }

        if (visitor.GetComponent<InteractableTrigger>() == null)
            visitor.AddComponent<InteractableTrigger>();

        SetTutorialVisitorInteractable(interactable);
    }

    void SetTutorialVisitorInteractable(bool enabled)
    {
        var visitor = FindNamedObjectIncludingInactive("Male Character (1)");
        if (visitor == null)
            return;

        var visitorInteractable = visitor.GetComponent<VisitorInteractable>();
        if (visitorInteractable != null)
        {
            visitorInteractable.enabled = enabled;
            if (enabled)
            {
                // Allow a fresh talk/donation after redirect unlocks this step.
                visitorInteractable.hasBeenSpokenTo = false;
                visitorInteractable.donated = false;
            }
        }

        var trigger = visitor.GetComponent<InteractableTrigger>();
        if (trigger != null)
            trigger.enabled = enabled;

        var prompt = visitor.GetComponent<InteractPrompt>();
        if (prompt != null)
            prompt.enabled = enabled;
    }

    static GameObject FindNamedObjectIncludingInactive(string objectName)
    {
        var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in transforms)
        {
            if (t != null && t.name == objectName)
                return t.gameObject;
        }

        return null;
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
        StartCoroutine(FadeSwitchToRole(RoleType.Doctor, doctorRoomName, AfterArrivedAsDoctor));
    }

    void OnJanitorButtonPressed()
    {
        if (switchingRole)
            return;
        StartCoroutine(FadeSwitchToRole(RoleType.Janitor, janitorRoomName, AfterArrivedAsJanitor));
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

    void AfterArrivedAsDoctor()
    {
        if (doctorIntroShown)
            return;

        doctorIntroShown = true;

        if (PatientCareSystem.Instance != null)
            PatientCareSystem.Instance.EnableTutorialDoctorPatients();

        // Same lower-third dialogue UI as the manager / nurse intros.
        if (dialogueManager != null)
        {
            dialogueManager.EnsureDialogueUI();
            dialogueManager.ApplyLowerThirdLayout();
            if (dialogueManager.Btnnext != null)
                dialogueManager.Btnnext.gameObject.SetActive(true);
            if (dialogueManager.dialoguePanel != null)
                dialogueManager.dialoguePanel.transform.SetAsLastSibling();
        }

        if (doctorWelcomeDialogue != null)
            ShowDialogue(doctorWelcomeDialogue, null);
        else
            Debug.LogWarning("TutorialManager: doctorWelcomeDialogue is not assigned.");
    }

    void AfterArrivedAsJanitor()
    {
        if (janitorIntroShown)
            return;

        janitorIntroShown = true;
        PrepareJanitorCleanupTargets();

        if (dialogueManager != null)
        {
            dialogueManager.EnsureDialogueUI();
            dialogueManager.ApplyLowerThirdLayout();
            if (dialogueManager.Btnnext != null)
                dialogueManager.Btnnext.gameObject.SetActive(true);
            if (dialogueManager.dialoguePanel != null)
                dialogueManager.dialoguePanel.transform.SetAsLastSibling();
        }

        var welcome = janitorWelcomeDialogue;
        if (welcome != null)
        {
            ShowDialogue(welcome, () =>
            {
                waitingForCartInteract = true;
                if (JanitorCartController.Instance != null && JanitorCartController.Instance.isCarried)
                    OnJanitorCartAttached();
            });
        }
        else
        {
            waitingForCartInteract = true;
            Debug.LogWarning("TutorialManager: janitorWelcomeDialogue is not assigned.");
        }
    }

    void TickJanitorTutorial()
    {
        if (!janitorIntroShown || dialogueBusy)
            return;

        if (waitingNearRedTrash && !janitorNearRedShown && IsNearRedTrash())
        {
            janitorNearRedShown = true;
            waitingNearRedTrash = false;
            var nearDlg = janitorNearRedTrashDialogue;
            if (nearDlg != null)
            {
                ShowDialogue(nearDlg, () =>
                {
                    waitingRedTrashPickup = true;
                    janitorDroppedCart = JanitorCartController.Instance == null
                        || !JanitorCartController.Instance.isCarried;
                });
            }
            else
            {
                waitingRedTrashPickup = true;
            }
        }

        if (waitingRedTrashPickup && !janitorAfterPickupShown
            && janitorDroppedCart && janitorPickedRedTrash)
        {
            janitorAfterPickupShown = true;
            waitingRedTrashPickup = false;
            var pickupDlg = janitorAfterPickupDialogue;
            if (pickupDlg != null)
            {
                ShowDialogue(pickupDlg, () =>
                {
                    waitingTrashDispose = true;
                });
            }
            else
            {
                waitingTrashDispose = true;
            }
        }

        if (waitingJanitorCleanup && !janitorCleanupDone && janitorCleanupRemaining.Count == 0)
        {
            janitorCleanupDone = true;
            waitingJanitorCleanup = false;
            StartCoroutine(ShowJanitorToManagerWhenReady());
        }
    }

    void OnJanitorCartAttached()
    {
        if (!waitingForCartInteract || janitorAfterCartShown || dialogueBusy)
            return;

        waitingForCartInteract = false;
        janitorAfterCartShown = true;
        var afterCart = janitorAfterCartDialogue;
        if (afterCart != null)
        {
            ShowDialogue(afterCart, () =>
            {
                waitingNearRedTrash = true;
            });
        }
        else
        {
            waitingNearRedTrash = true;
        }
    }

    void OnJanitorCartDetached()
    {
        if (!waitingRedTrashPickup)
            return;

        janitorDroppedCart = true;
    }

    void OnJanitorTrashPickedUp(TrashItem trash)
    {
        if (trash == null)
            return;

        if (waitingRedTrashPickup
            && trash.gameObject != null
            && trash.gameObject.name.Equals("RedTrash", System.StringComparison.OrdinalIgnoreCase))
        {
            janitorPickedRedTrash = true;
            if (JanitorCartController.Instance != null && JanitorCartController.Instance.isCarried)
                JanitorCartController.Instance.Detach();
            janitorDroppedCart = true;
        }
    }

    void OnJanitorTrashDisposed(string trashName)
    {
        MarkJanitorCleanupDone(trashName);

        if (waitingTrashDispose && !janitorAfterDisposeShown)
        {
            waitingTrashDispose = false;
            janitorAfterDisposeShown = true;
            var afterDispose = janitorAfterDisposeDialogue;
            if (afterDispose != null)
            {
                ShowDialogue(afterDispose, () =>
                {
                    waitingJanitorCleanup = true;
                });
            }
            else
            {
                waitingJanitorCleanup = true;
            }
        }
    }

    void OnJanitorDirtCleaned(string dirtName)
    {
        MarkJanitorCleanupDone(dirtName);
    }

    void MarkJanitorCleanupDone(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return;

        // Match exact names and also "GreenTrash (1)" style variants.
        if (janitorCleanupRemaining.Remove(objectName))
            return;

        foreach (var key in new System.Collections.Generic.List<string>(janitorCleanupRemaining))
        {
            if (objectName.Equals(key, System.StringComparison.OrdinalIgnoreCase)
                || objectName.StartsWith(key, System.StringComparison.OrdinalIgnoreCase))
            {
                janitorCleanupRemaining.Remove(key);
                return;
            }
        }
    }

    bool IsNearRedTrash()
    {
        var red = GameObject.Find("RedTrash");
        if (red == null)
            return false;

        var janitor = FindCharacter(RoleType.Janitor);
        if (janitor == null)
            return false;

        float dist = Vector3.Distance(janitor.transform.position, red.transform.position);
        return dist <= redTrashNearDistance;
    }

    void PrepareJanitorCleanupTargets()
    {
        janitorCleanupRemaining.Clear();
        for (int i = 0; i < JanitorCleanupNames.Length; i++)
        {
            string n = JanitorCleanupNames[i];
            var go = GameObject.Find(n);
            if (go == null)
                continue;

            janitorCleanupRemaining.Add(n);
            EnsureJanitorCleanupInteractable(go);
        }

        Debug.Log($"TutorialManager: janitor cleanup targets ready ({janitorCleanupRemaining.Count}).");
    }

    static void EnsureJanitorCleanupInteractable(GameObject go)
    {
        if (go == null) return;

        // Spills / groups are mop targets.
        string n = go.name;
        bool isSpillOrGroup = n.StartsWith("Spill", System.StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("TrashGroup", System.StringComparison.OrdinalIgnoreCase);

        if (isSpillOrGroup)
        {
            if (go.GetComponent<DirtPile>() == null)
                go.AddComponent<DirtPile>();
        }
        else if (go.GetComponent<TrashItem>() == null)
        {
            var item = go.AddComponent<TrashItem>();
            if (n.IndexOf("Red", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Syringe", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Bandage", System.StringComparison.OrdinalIgnoreCase) >= 0)
                item.type = TrashType.Medical;
            else if (n.IndexOf("Blue", System.StringComparison.OrdinalIgnoreCase) >= 0)
                item.type = TrashType.Recycle;
            else
                item.type = TrashType.General;
        }

        if (go.GetComponent<Collider>() == null)
        {
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
        }

        if (go.GetComponent<InteractableTrigger>() == null)
            go.AddComponent<InteractableTrigger>();
    }

    IEnumerator ShowJanitorToManagerWhenReady()
    {
        if (janitorToManagerShown)
            yield break;

        janitorToManagerShown = true;

        while (dialogueBusy)
            yield return null;

        yield return null;

        if (janitorToManagerDialogue != null)
        {
            ShowDialogue(janitorToManagerDialogue, null);
            StartCoroutine(WatchForPressPGate(PSwitchTarget.Manager));
        }
        else
        {
            Debug.LogWarning("TutorialManager: janitorToManagerDialogue is not assigned.");
            BeginWaitForP(PSwitchTarget.Manager);
        }
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

    void OnDoctorTreated(PatientRecord record)
    {
        NotifyDoctorPatientRecovered(record);
    }

    /// <summary>Called from PatientCareSystem when a doctor recovery completes.</summary>
    public void NotifyDoctorPatientRecovered(PatientRecord record)
    {
        if (doctorToJanitorShown)
            return;

        doctorIntroShown = true;

        if (!AllTutorialDoctorPatientsTreated())
        {
            Debug.Log("TutorialManager: waiting for remaining doctor patients before janitor dialogue.");
            return;
        }

        doctorToJanitorShown = true;
        Debug.Log("TutorialManager: all doctor patients treated — showing janitor handoff dialogue.");
        StartCoroutine(ShowDoctorToJanitorWhenReady());
    }

    IEnumerator ShowDoctorToJanitorWhenReady()
    {
        PatientInteractable.CloseOpenCarePanels();
        yield return null;
        yield return null;

        if (dialogueManager == null)
            SetupDialogueUi();

        if (dialogueManager != null)
        {
            dialogueManager.EnsureDialogueUI();
            dialogueManager.ApplyLowerThirdLayout();
        }

        if (doctorToJanitorDialogue != null)
        {
            ShowDialogue(doctorToJanitorDialogue, null, showNextButton: false);
            BeginWaitForP(PSwitchTarget.Janitor);
            if (dialogueManager != null && dialogueManager.Btnnext != null)
                dialogueManager.Btnnext.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("TutorialManager: doctorToJanitorDialogue is not assigned.");
            BeginWaitForP(PSwitchTarget.Janitor);
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
            if (p == null || !p.tutorialNurseTarget)
                continue;

            total++;
            if (p.treatedByNurse || p.recovered)
                treated++;
        }

        return total > 0 && treated >= total;
    }

    bool AllTutorialDoctorPatientsTreated()
    {
        if (PatientCareSystem.Instance == null)
            return false;

        return PatientCareSystem.Instance.AllTutorialDoctorPatientsRecovered();
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

    void ShowDialogue(DialogueScriptable data, Action onFinished, bool showNextButton = true)
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
            // Run callbacks first so wait-flags (e.g. redirect) are set before unlock.
            onFinished?.Invoke();
            LockPlayer(false);
        }, showNextButton);

        // Re-assert lock after Play in case another system unlocked this frame.
        LockPlayer(true);
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

        if (tutorialEnding)
        {
            if (activeChar != null && activeChar.movement != null)
                activeChar.movement.SetControlsEnabled(false);
            KeepUiCursorVisible();
            return;
        }

        // Keep the cursor free during the shelf sorting mini-game.
        bool organizerActive = MedicineOrganizerMinigame.Instance != null
            && MedicineOrganizerMinigame.Instance.IsActive;

        // Keep the cursor free while the manager is mid-redirect tutorial
        // (dialogue continued, station/redirect UI still open, patient not redirected yet).
        bool managerUiCursor = organizerActive
            || waitingForNonCriticalRedirect
            || IsManagerStationUiOpen();

        if (managerUiCursor)
        {
            if (IsManagerStationUiOpen() && activeChar != null && activeChar.movement != null)
                activeChar.movement.SetControlsEnabled(false);

            if (cam != null)
                cam.LockCursor(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (cam != null)
            cam.LockCursor(true);
    }

    static bool IsManagerStationUiOpen()
    {
        var hub = ManagerStationHub.Instance;
        if (hub == null)
            return false;

        return (hub.navPanel != null && hub.navPanel.activeInHierarchy)
            || (hub.redirectPanel != null && hub.redirectPanel.activeInHierarchy)
            || (hub.storePanel != null && hub.storePanel.activeInHierarchy);
    }

    static void KeepUiCursorVisible()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null)
            cam.LockCursor(false);
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
