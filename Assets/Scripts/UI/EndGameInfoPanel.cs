using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Shown when all role shift timers have expired.
/// Scene object: EndGameInfoPanel under NewFeatureUICanvas.
/// </summary>
public class EndGameInfoPanel : MonoBehaviour
{
    public static EndGameInfoPanel Instance;

    GameObject panel;
    bool shown;

    const string MsfUrl = "https://www.msf.org/";
    const string HjiUrl = "https://www.healthjusticeinitiative.org.za/";
    const string GotgUrl = "https://giftofthegivers.org/";
    const string MainMenuSceneName = "MainMenu";

    void Awake() => Instance = this;

    void Update()
    {
        if (shown) return;
        if (RoleTimerSystem.Instance == null) return;
        if (!RoleTimerSystem.Instance.AllRoleTimersExhausted()) return;
        Show();
    }

    public void Show()
    {
        if (shown) return;
        shown = true;
        BindScenePanel();
        if (panel != null)
            panel.SetActive(true);

        var active = CharacterSwitchManager.Instance?.ActiveCharacter;
        if (active?.movement != null) active.movement.SetControlsEnabled(false);
        var cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null) cam.LockCursor(false);

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("End Screen Appears");
    }

    void BindScenePanel()
    {
        if (panel != null) return;
        panel = ClinicalUIFactory.FindByName("EndGameInfoPanel");
        if (panel == null)
        {
            Debug.LogError("EndGameInfoPanel: missing scene object 'EndGameInfoPanel'.");
            return;
        }

        ClinicalUIFactory.BindButton(panel.transform, "MsfButton", () => Open(MsfUrl));
        ClinicalUIFactory.BindButton(panel.transform, "HjiButton", () => Open(HjiUrl));
        ClinicalUIFactory.BindButton(panel.transform, "GotgButton", () => Open(GotgUrl));
        ClinicalUIFactory.BindButton(panel.transform, "CloseButton", () => panel.SetActive(false));
        ClinicalUIFactory.BindButton(panel.transform, "Main MenuButton", GoToMainMenu);
        panel.SetActive(false);
    }

    void Open(string url)
    {
        Application.OpenURL(url);
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        FeatureBootstrap.PrepareForSceneRestart();
        SceneManager.LoadScene(MainMenuSceneName);
    }
}
