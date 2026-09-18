using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TutorialScene only — welcome the player, buy medicine, then switch to nurse.
/// Keep this beginner-friendly: one step at a time.
/// </summary>
[DefaultExecutionOrder(-50)]
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Dialogue (DialogueScriptable assets)")]
    public DialogueScriptable welcomeDialogue;
    public DialogueScriptable afterMedicineDialogue;

    [Header("Rooms")]
    public string managerRoomName = "TutRoom1_manager";
    public string nurseRoomName = "TutRoom2_nurse";

    [Header("Fade")]
    public float fadeSeconds = 0.6f;

    DialogueManager dialogueManager;
    GameObject dialoguePanel;
    Image fadeImage;

    bool didMedicine;
    bool waitingForPToSwitch;
    bool switchingToNurse;

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

        // Manager station: Online Store only for this step (redirect stays off).
        StartCoroutine(SetupManagerButtonsNextFrame());

        // Character select locked until the "press P" step.
        if (CharacterSwitchManager.Instance != null)
            CharacterSwitchManager.Instance.allowOpenPanel = false;

        StartCoroutine(SetupRoleButtonsNextFrame());

        MedicineSupplyManager.OnBoxPurchased += OnMedicinePurchased;

        // Greeting dialogue before the player does anything.
        StartCoroutine(ShowWelcomeNextFrame());
    }

    void OnDestroy()
    {
        MedicineSupplyManager.OnBoxPurchased -= OnMedicinePurchased;

        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!waitingForPToSwitch)
            return;

        // Last tutorial line asks the player to press P.
        if (Input.GetKeyDown(KeyCode.P))
        {
            waitingForPToSwitch = false;

            if (dialogueManager != null && dialogueManager.IsWaitingForGate)
                dialogueManager.PassGate(true);
            else if (dialogueManager != null)
                dialogueManager.ForceClose();

            // Wait one frame so CharacterSwitchManager does not also handle this same P press.
            StartCoroutine(OpenSwitchMenuNextFrame());
        }
    }

    IEnumerator OpenSwitchMenuNextFrame()
    {
        UnlockNurseSwitch();
        yield return null;

        if (CharacterSwitchManager.Instance != null)
            CharacterSwitchManager.Instance.OpenPanel();
    }

    IEnumerator ShowWelcomeNextFrame()
    {
        // Wait a moment so UI / CharacterSwitchManager finish Start().
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

        // Tutorial starts with no medicine on the shelf.
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
            // Shift allocation not required in this intro step.
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

        // Close manager hub UI so the dialogue is clear.
        if (ManagerStationHub.Instance != null)
            ManagerStationHub.Instance.CloseAll(false);

        if (afterMedicineDialogue != null)
        {
            ShowDialogue(afterMedicineDialogue, () =>
            {
                // If the last line was a Gate, EndDialogue already ran via P.
                // If somehow finished without gate, still unlock.
                if (!waitingForPToSwitch)
                    UnlockNurseSwitch();
            });

            // After the first two Next presses, the Gate line stays open until P.
            StartCoroutine(WatchForPressPGate());
        }
        else
        {
            UnlockNurseSwitch();
        }
    }

    IEnumerator WatchForPressPGate()
    {
        // Wait until DialogueManager reaches the Gate line ("Click P...").
        while (dialogueManager != null && dialogueManager.dialoguePanel != null
               && dialogueManager.dialoguePanel.activeSelf
               && !dialogueManager.IsWaitingForGate)
        {
            yield return null;
        }

        if (dialogueManager != null && dialogueManager.IsWaitingForGate)
        {
            waitingForPToSwitch = true;
            // Hide Next on the gate line — player must press P.
            if (dialogueManager.Btnnext != null)
                dialogueManager.Btnnext.gameObject.SetActive(false);
        }
    }

    void UnlockNurseSwitch()
    {
        if (CharacterSwitchManager.Instance == null)
            return;

        CharacterSwitchManager.Instance.allowOpenPanel = true;
        CharacterSwitchManager.Instance.SetRoleButtonEnabled("ManagerButton", false);
        CharacterSwitchManager.Instance.SetRoleButtonEnabled("DoctorButton", false);
        CharacterSwitchManager.Instance.SetRoleButtonEnabled("JanitorButton", false);
        CharacterSwitchManager.Instance.SetRoleButtonEnabled("NurseButton", true);
        CharacterSwitchManager.Instance.BindRoleButtonOverride("NurseButton", OnNurseButtonPressed);

        if (dialogueManager != null && dialogueManager.Btnnext != null)
            dialogueManager.Btnnext.gameObject.SetActive(true);
    }

    void OnNurseButtonPressed()
    {
        if (switchingToNurse)
            return;

        StartCoroutine(FadeSwitchToNurse());
    }

    IEnumerator FadeSwitchToNurse()
    {
        switchingToNurse = true;

        if (CharacterSwitchManager.Instance != null)
            CharacterSwitchManager.Instance.ClosePanel();

        // Fully black before any move / role switch.
        yield return Fade(0f, 1f);

        PlaceCharacterInRoom(RoleType.Nurse, nurseRoomName);

        var nurse = FindCharacter(RoleType.Nurse);
        if (nurse != null && CharacterSwitchManager.Instance != null)
            CharacterSwitchManager.Instance.SwitchTo(nurse, playSound: false);

        // CameraFollow normally lerps — snap while still black so fade-in is already on the nurse.
        var cam = CharacterSwitchManager.Instance != null
            ? CharacterSwitchManager.Instance.cameraFollow
            : FindFirstObjectByType<CameraFollow>();
        if (cam != null)
            cam.SnapToTarget();

        yield return null;
        if (cam != null)
            cam.SnapToTarget();

        yield return Fade(1f, 0f);

        switchingToNurse = false;
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

        LockPlayer(true);
        dialogueManager.Play(data, () =>
        {
            LockPlayer(false);
            onFinished?.Invoke();
        });
    }

    void LockPlayer(bool freeze)
    {
        var active = CharacterSwitchManager.Instance != null
            ? CharacterSwitchManager.Instance.ActiveCharacter
            : null;

        if (active != null && active.movement != null)
            active.movement.SetControlsEnabled(!freeze);

        var cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null)
            cam.LockCursor(!freeze);

        if (freeze)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
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
