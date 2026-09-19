using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HospitalHubLevel pause overlay. Esc opens/closes; freezes gameplay via timeScale.
/// Temporarily hides open gameplay UI (care panels, manager hub, info panels, etc.) and restores on resume.
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

    GameObject root;
    GameObject panel;
    bool isPaused;
    float previousTimeScale = 1f;
    readonly List<GameObject> hiddenForPause = new List<GameObject>();
    bool restoreNeedsFreeCursor;

    public bool IsPaused => isPaused;

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
        if (TutorialMode.IsActive)
            return;

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
        if (panel != null)
        {
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("open");
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

        panel = ClinicalUIFactory.CreatePanel(root.transform, "PauseMenuPanel", new Vector2(420f, 420f));
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchoredPosition = Vector2.zero;

        ClinicalUIFactory.CreateLabel(panel.transform, "Paused", 36, new Vector2(0f, 150f));

        float y = 70f;
        float step = -70f;
        ClinicalUIFactory.CreateButton(panel.transform, "Resume", Resume, new Vector2(0f, y), new Vector2(300f, 56f));
        y += step;
        ClinicalUIFactory.CreateButton(panel.transform, "Restart", RestartLevel, new Vector2(0f, y), new Vector2(300f, 56f));
        y += step;
        ClinicalUIFactory.CreateButton(panel.transform, "Home", GoHome, new Vector2(0f, y), new Vector2(300f, 56f));
        y += step;
        ClinicalUIFactory.CreateButton(panel.transform, "Quit", QuitGame, new Vector2(0f, y), new Vector2(300f, 56f));

        root.SetActive(false);
        panel.SetActive(false);
    }
}
