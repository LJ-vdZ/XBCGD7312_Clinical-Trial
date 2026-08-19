using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Failure end screen when sanitation, comfort, morale, or budget hits 0.
/// Scene object: StatsCollapseEndScreen under NewFeatureUICanvas.
/// </summary>
public class StatsCollapseEndScreen : MonoBehaviour
{
    public static StatsCollapseEndScreen Instance;

    GameObject panel;
    TextMeshProUGUI titleLabel;
    TextMeshProUGUI bodyLabel;
    bool shown;

    const string MsfUrl = "https://www.msf.org/";
    const string HjiUrl = "https://www.healthjusticeinitiative.org.za/";
    const string GotgUrl = "https://giftofthegivers.org/";

    void Awake() => Instance = this;

    void OnEnable()
    {
        HospitalStatsManager.OnStatsChanged += CheckStats;
    }

    void OnDisable()
    {
        HospitalStatsManager.OnStatsChanged -= CheckStats;
    }

    void Start()
    {
        BindScenePanel();
        CheckStats();
    }

    void CheckStats()
    {
        if (shown) return;
        if (HospitalStatsManager.Instance == null) return;

        if (!HospitalStatsManager.Instance.TryGetCollapsedStat(out string statName))
            return;

        Show(statName);
    }

    public void Show(string collapsedStat)
    {
        if (shown) return;
        shown = true;

        BindScenePanel();

        if (titleLabel != null)
            titleLabel.text = "Hospital Collapse";
        if (bodyLabel != null)
        {
            bodyLabel.text =
                $"Critical failure: <b>{collapsedStat}</b> reached zero.\n\n" +
                "Without sanitation, comfort, morale, or funding, the hospital can no longer care for its patients.\n\n" +
                "Learn more about organisations working on health justice:";
        }

        if (panel != null)
        {
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
        }

        FreezeGameplay();

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("End Screen Appears");
    }

    void FreezeGameplay()
    {
        var active = CharacterSwitchManager.Instance?.ActiveCharacter;
        if (active?.movement != null)
            active.movement.SetControlsEnabled(false);
        else
        {
            var move = FindFirstObjectByType<SimplePlayerMovement>();
            if (move != null) move.SetControlsEnabled(false);
        }

        var cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null) cam.LockCursor(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (GameManager.Instance != null)
            GameManager.Instance.canPerformTasks = false;
    }

    void BindScenePanel()
    {
        if (panel != null) return;

        panel = ClinicalUIFactory.FindByName("StatsCollapseEndScreen");
        if (panel == null)
        {
            Debug.LogError("StatsCollapseEndScreen: missing scene object 'StatsCollapseEndScreen'.");
            return;
        }

        titleLabel = ClinicalUIFactory.FindLabel(panel.transform, "TitleLabel");
        bodyLabel = ClinicalUIFactory.FindLabel(panel.transform, "BodyLabel");
        ClinicalUIFactory.BindButton(panel.transform, "MsfButton", () => Open(MsfUrl));
        ClinicalUIFactory.BindButton(panel.transform, "HjiButton", () => Open(HjiUrl));
        ClinicalUIFactory.BindButton(panel.transform, "GotgButton", () => Open(GotgUrl));
        ClinicalUIFactory.BindButton(panel.transform, "Restart LevelButton", RestartLevel);
        panel.SetActive(false);
    }

    void Open(string url)
    {
        Application.OpenURL(url);
    }

    void RestartLevel()
    {
        Time.timeScale = 1f;
        FeatureBootstrap.PrepareForSceneRestart();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
