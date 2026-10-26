using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause overlay for HospitalHubLevel and TutorialScene.
/// Esc opens/closes; freezes gameplay via timeScale.
/// Hub: Resume / Restart / Home / Learn More / Quit.
/// Tutorial: Resume / Home / Learn More / Quit.
/// Learn More swaps to an NPO links panel; Back (or Esc) returns to the pause buttons.
/// Temporarily hides open gameplay UI and restores on resume.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance;

    [Header("Scenes")]
    public string hubSceneName = "HospitalHubLevel";
    public string mainMenuSceneName = "MainMenu";

    static readonly string[] OverlayPanelNames =
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

    const string MsfUrl = "https://www.msf.org/";
    const string HjiUrl = "https://www.healthjusticeinitiative.org.za/";
    const string GotgUrl = "https://giftofthegivers.org/";

    GameObject root;
    GameObject panel;
    GameObject learnMorePanel;
    bool isPaused;
    float previousTimeScale = 1f;
    readonly List<GameObject> hiddenForPause = new List<GameObject>();
    bool restoreNeedsFreeCursor;

    public bool IsPaused => isPaused;

    bool IsTutorialLayout => TutorialMode.IsActive;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (isPaused)
            Time.timeScale = previousTimeScale > 0.01f ? previousTimeScale : 1f;
    }

    void Start()
    {
        EnsureBuilt();
        HideImmediate();
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        // Close character select first; next Esc opens pause.
        if (CharacterSwitchManager.Instance != null && CharacterSwitchManager.Instance.IsSelectionPanelOpen)
        {
            CharacterSwitchManager.Instance.ClosePanel();
            return;
        }

        if (isPaused)
        {
            if (learnMorePanel != null && learnMorePanel.activeSelf)
            {
                CloseLearnMore();
                return;
            }

            Resume();
            return;
        }

        if (ShouldIgnoreEscape())
            return;

        Pause();
    }

    bool ShouldIgnoreEscape()
    {
        if (MedicineOrganizerMinigame.Instance != null && MedicineOrganizerMinigame.Instance.IsActive)
            return true;

        var iv = FindFirstObjectByType<IVMinigame>();
        if (iv != null && iv.minigameUI != null && iv.minigameUI.activeSelf)
            return true;

        return false;
    }

    public void Pause()
    {
        if (isPaused)
            return;

        EnsureBuilt();
        isPaused = true;
        previousTimeScale = Time.timeScale > 0.01f ? Time.timeScale : 1f;
        Time.timeScale = 0f;

        HideOpenUiForPause();

        if (CharacterSwitchManager.Instance != null)
            CharacterSwitchManager.Instance.ClosePanel();

        ApplyCursorState(freeCursor: true, movementEnabled: false);

        if (root != null)
            root.SetActive(true);
        if (learnMorePanel != null)
            learnMorePanel.SetActive(false);
        if (panel != null)
        {
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("open");
    }

    void OpenLearnMore()
    {
        if (!isPaused || learnMorePanel == null)
            return;

        if (panel != null)
            panel.SetActive(false);

        // Keep the overlay above any HUD or notification that was added after the pause root.
        if (root != null)
            root.transform.SetAsLastSibling();

        learnMorePanel.SetActive(true);
        learnMorePanel.transform.SetAsLastSibling();

        ApplyCursorState(freeCursor: true, movementEnabled: false);
    }

    void CloseLearnMore()
    {
        if (learnMorePanel != null)
            learnMorePanel.SetActive(false);

        if (panel != null)
        {
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
        }

        ApplyCursorState(freeCursor: true, movementEnabled: false);
    }

    static void OpenUrl(string url)
    {
        Application.OpenURL(url);
    }

    public void Resume()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = previousTimeScale > 0.01f ? previousTimeScale : 1f;
        HideImmediate();
        RestoreUiAfterPause();

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("close");
    }

    void HideOpenUiForPause()
    {
        hiddenForPause.Clear();
        restoreNeedsFreeCursor = false;

        for (int i = 0; i < OverlayPanelNames.Length; i++)
            StashIfActive(ClinicalUIFactory.FindByName(OverlayPanelNames[i]));

        var mainUi = FindFirstObjectByType<MainSceneUIManager>();
        if (mainUi != null)
        {
            StashIfActive(mainUi.jobPanel);
            StashIfActive(mainUi.jobSelectionPanel);
            StashIfActive(mainUi.infoPanel);
            StashIfActive(mainUi.infoManagerPanel);
            StashIfActive(mainUi.infoNursePanel);
            StashIfActive(mainUi.infoJanitorPanel);
            StashIfActive(mainUi.infoDoctorPanel);
        }

        var diagnosis = FindFirstObjectByType<DiagnosisMinigame>();
        if (diagnosis != null)
            StashIfActive(diagnosis.minigameUI);

        var manual = FindFirstObjectByType<ManualScript>();
        if (manual != null)
            StashIfActive(manual.dictionaryPage);
    }

    void StashIfActive(GameObject go)
    {
        if (go == null || !go.activeSelf)
            return;

        if (go == root || go == panel || (root != null && go.transform.IsChildOf(root.transform)))
            return;

        // Any restored overlay UI needs a visible / unlocked cursor.
        restoreNeedsFreeCursor = true;

        go.SetActive(false);
        if (!hiddenForPause.Contains(go))
            hiddenForPause.Add(go);
    }

    void RestoreUiAfterPause()
    {
        for (int i = 0; i < hiddenForPause.Count; i++)
        {
            var go = hiddenForPause[i];
            if (go != null)
                go.SetActive(true);
        }

        hiddenForPause.Clear();

        if (restoreNeedsFreeCursor)
        {
            // Keep mouse free for care / manager / info panels that came back.
            ApplyCursorState(freeCursor: true, movementEnabled: false);
        }
        else
        {
            // Back to normal FPS look.
            ApplyCursorState(freeCursor: false, movementEnabled: true);
        }

        restoreNeedsFreeCursor = false;
    }

    void ClearHiddenUi()
    {
        hiddenForPause.Clear();
        restoreNeedsFreeCursor = false;
    }

    void RestartLevel()
    {
        Time.timeScale = 1f;
        isPaused = false;
        ClearHiddenUi();
        FeatureBootstrap.PrepareForSceneRestart();
        UnityEngine.SceneManagement.SceneManager.LoadScene(hubSceneName);
    }

    void GoHome()
    {
        Time.timeScale = 1f;
        isPaused = false;
        ClearHiddenUi();
        FeatureBootstrap.PrepareForSceneRestart();
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }

    void QuitGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        ClearHiddenUi();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void HideImmediate()
    {
        if (learnMorePanel != null)
            learnMorePanel.SetActive(false);
        if (panel != null)
            panel.SetActive(false);
        if (root != null)
            root.SetActive(false);
    }

    void LockPlayer(bool freeze)
    {
        ApplyCursorState(freeCursor: freeze, movementEnabled: !freeze);
    }

    void ApplyCursorState(bool freeCursor, bool movementEnabled)
    {
        var active = CharacterSwitchManager.Instance != null
            ? CharacterSwitchManager.Instance.ActiveCharacter
            : null;

        if (active != null && active.movement != null)
            active.movement.SetControlsEnabled(movementEnabled);

        var cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null)
            cam.LockCursor(!freeCursor);

        if (freeCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void EnsureBuilt()
    {
        if (panel != null)
            return;

        var canvas = NewUIRoot.Ensure();
        if (canvas == null)
        {
            Debug.LogError("PauseMenu: SystemsUI canvas missing.");
            return;
        }

        root = new GameObject("PauseMenuRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(canvas.transform, false);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;
        var dim = root.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.65f);
        dim.raycastTarget = true;

        bool tutorial = IsTutorialLayout;
        Vector2 panelSize = tutorial ? new Vector2(420f, 410f) : new Vector2(420f, 490f);
        panel = ClinicalUIFactory.CreatePanel(root.transform, "PauseMenuPanel", panelSize);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchoredPosition = Vector2.zero;

        float titleY = tutorial ? 145f : 185f;
        ClinicalUIFactory.CreateLabel(panel.transform, "Paused", 36, new Vector2(0f, titleY));

        float y = tutorial ? 75f : 105f;
        float step = -70f;
        ClinicalUIFactory.CreateButton(panel.transform, "Resume", Resume, new Vector2(0f, y), new Vector2(300f, 56f));
        y += step;

        if (!tutorial)
        {
            ClinicalUIFactory.CreateButton(panel.transform, "Restart", RestartLevel, new Vector2(0f, y), new Vector2(300f, 56f));
            y += step;
        }

        ClinicalUIFactory.CreateButton(panel.transform, "Home", GoHome, new Vector2(0f, y), new Vector2(300f, 56f));
        y += step;
        ClinicalUIFactory.CreateButton(panel.transform, "Learn More", OpenLearnMore, new Vector2(0f, y), new Vector2(300f, 56f));
        y += step;
        ClinicalUIFactory.CreateButton(panel.transform, "Quit", QuitGame, new Vector2(0f, y), new Vector2(300f, 56f));

        BuildLearnMorePanel();

        root.SetActive(false);
        panel.SetActive(false);
    }

    void BuildLearnMorePanel()
    {
        learnMorePanel = ClinicalUIFactory.CreatePanel(root.transform, "LearnMorePanel", new Vector2(520f, 500f));
        learnMorePanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        ClinicalUIFactory.CreateLabel(learnMorePanel.transform, "Learn More", 36, new Vector2(0f, 190f));

        var body = ClinicalUIFactory.CreateLabel(learnMorePanel.transform,
            "Learn more about the organisations working to improve healthcare and support communities in need:",
            20, new Vector2(0f, 115f));
        body.GetComponent<RectTransform>().sizeDelta = ClinicalUIFactory.Scale(new Vector2(460f, 90f));
        body.enableWordWrapping = true;

        Vector2 buttonSize = new Vector2(380f, 56f);
        SetButtonFontSize(ClinicalUIFactory.CreateButton(learnMorePanel.transform, "Medecins Sans Frontieres (MSF)",
            () => OpenUrl(MsfUrl), new Vector2(0f, 35f), buttonSize));
        SetButtonFontSize(ClinicalUIFactory.CreateButton(learnMorePanel.transform, "Health Justice Initiative",
            () => OpenUrl(HjiUrl), new Vector2(0f, -35f), buttonSize));
        SetButtonFontSize(ClinicalUIFactory.CreateButton(learnMorePanel.transform, "Gift of the Givers Foundation",
            () => OpenUrl(GotgUrl), new Vector2(0f, -105f), buttonSize));

        SetButtonFontSize(ClinicalUIFactory.CreateButton(learnMorePanel.transform, "Back", CloseLearnMore,
            new Vector2(0f, -185f), new Vector2(300f, 56f)));

        learnMorePanel.SetActive(false);
    }

    static void SetButtonFontSize(Button button)
    {
        if (button == null)
            return;

        var label = button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (label != null)
            label.fontSize = ClinicalUIFactory.ScaleFont(24);
    }
}
