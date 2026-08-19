using UnityEngine;
using UnityEngine.UI;

public class MainSceneUIManager : MonoBehaviour
{
    public RoleTimeManager roleTimeManager;

    [Header("Job Panel")]
    public GameObject jobPanel;
    public GameObject jobSelectionPanel;
    public Button nurseButton;
    public Button doctorButton;
    public Button janitorButton;
    public Button jobCloseButton;
    public Button jobSelectionCloseButton;

    [Header("Info Panel")]
    public GameObject infoPanel;
    public GameObject infoManagerPanel;
    public GameObject infoNursePanel;
    public GameObject infoJanitorPanel;
    public GameObject infoDoctorPanel;
    public Button infoCloseButton;
    public Button infoJanitorCloseButton;
    public Button infoNurseCloseButton;
    public Button infoDoctorCloseButton;
    public Button infoManagerCloseButton;

    [Header("References")]
    public SimplePlayerMovement playerMovement;

    void Start()
    {
        // Button listeners 
        if (nurseButton != null) nurseButton.onClick.AddListener(OnNurseClicked);
        if (doctorButton != null) doctorButton.onClick.AddListener(OnDoctorClicked);
        if (janitorButton != null) janitorButton.onClick.AddListener(OnJanitorClicked);
        if (jobCloseButton != null) jobCloseButton.onClick.AddListener(CloseAllUI);
        if (jobSelectionCloseButton != null) jobSelectionCloseButton.onClick.AddListener(CloseAllUI);
        if (infoCloseButton != null) infoCloseButton.onClick.AddListener(CloseAllUI);
        if (infoManagerCloseButton != null) infoManagerCloseButton.onClick.AddListener(CloseAllUI);
        if (infoJanitorCloseButton != null) infoJanitorCloseButton.onClick.AddListener(CloseAllUI);
        if (infoNurseCloseButton != null) infoNurseCloseButton.onClick.AddListener(CloseAllUI);

        // Ensure panels start closed
        if (jobPanel != null) jobPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        if (infoJanitorPanel != null) infoJanitorPanel.SetActive(false);
        if (infoManagerPanel != null) infoManagerPanel.SetActive(false);
        if (infoNursePanel != null) infoNursePanel.SetActive(false);
        if (infoDoctorPanel != null) infoDoctorPanel.SetActive(false);
    }

    
    // job buttons

    void OnNurseClicked()
    {
        if (roleTimeManager.nurseMinutes + roleTimeManager.doctorMinutes + roleTimeManager.janitorMinutes >= roleTimeManager.totalMinutes)
        {
            Debug.Log("No minutes left!");
            return;
        }
        Debug.Log("Added 1 minute to Nurse");

        roleTimeManager.SetAllocation(
            roleTimeManager.nurseMinutes + 1,
            roleTimeManager.doctorMinutes,
            roleTimeManager.janitorMinutes
        );
    }

    void OnDoctorClicked()
    {
        if (roleTimeManager.nurseMinutes + roleTimeManager.doctorMinutes + roleTimeManager.janitorMinutes >= roleTimeManager.totalMinutes)
        {
            Debug.Log("No minutes left!");
            return;
        }
        Debug.Log("Added 1 minute to Doctor");

        roleTimeManager.SetAllocation(
            roleTimeManager.nurseMinutes,
            roleTimeManager.doctorMinutes + 1,
            roleTimeManager.janitorMinutes
        );
    }

    void OnJanitorClicked()
    {
        if (roleTimeManager.nurseMinutes + roleTimeManager.doctorMinutes + roleTimeManager.janitorMinutes >= roleTimeManager.totalMinutes)
        {
            Debug.Log("No minutes left!");
            return;
        }
        Debug.Log("Added 1 minute to Janitor");

        roleTimeManager.SetAllocation(roleTimeManager.nurseMinutes,roleTimeManager.doctorMinutes,roleTimeManager.janitorMinutes + 1);
    }

    //ui controls
    public void OpenJobPanel()
    {
        Debug.Log("Opening Job Panel");

        if (jobPanel != null) jobPanel.SetActive(true);
        if (infoPanel != null) infoPanel.SetActive(false);

        SetPlayerControl(false);
    }

    public void OpenInfoPanel()
    {
        Debug.Log("Opening Info Panel");

        if (infoPanel != null) infoPanel.SetActive(true);
        if (jobPanel != null) jobPanel.SetActive(false);

        SetPlayerControl(false);
    }

    public void OpenInfoManagerPanel()
    {
        Debug.Log("Opening Manager Info Panel");

        if (infoManagerPanel != null) infoManagerPanel.SetActive(true);
        if (jobPanel != null) jobPanel.SetActive(false);

        SetPlayerControl(false);
    }

    public void OpenInfoJanitorPanel()
    {
        Debug.Log("Opening Janitor Info Panel");

        if (infoManagerPanel != null) infoJanitorPanel.SetActive(true);
        if (jobPanel != null) jobPanel.SetActive(false);

        SetPlayerControl(false);
    }

    public void OpenInfoNursePanel()
    {
        Debug.Log("Opening Janitor Info Panel");

        if (infoNursePanel != null) infoNursePanel.SetActive(true);
        if (jobPanel != null) jobPanel.SetActive(false);

        SetPlayerControl(false);
    }

    public void CloseAllUI()
    {
        Debug.Log("Closing UI");

        if (jobPanel != null) jobPanel.SetActive(false);
        if (jobSelectionPanel != null) jobSelectionPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        if (infoManagerPanel != null) infoManagerPanel.SetActive(false);
        if (infoJanitorPanel != null) infoJanitorPanel.SetActive(false);
        if (infoNursePanel != null) infoNursePanel.SetActive(false);
        if (infoDoctorPanel != null) infoDoctorPanel.SetActive(false);

        SetPlayerControl(true);
    }

    void SetPlayerControl(bool enabled)
    {
        var active = CharacterSwitchManager.Instance != null
            ? CharacterSwitchManager.Instance.ActiveCharacter
            : null;

        if (active != null && active.movement != null)
            active.movement.SetControlsEnabled(enabled);
        else if (playerMovement != null)
            playerMovement.SetControlsEnabled(enabled);
        else
        {
            var move = FindFirstObjectByType<SimplePlayerMovement>();
            if (move != null)
                move.SetControlsEnabled(enabled);
        }

        var cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null)
            cam.LockCursor(enabled);
    }


    //inputs
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            OpenInfoPanel();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            OpenInfoManagerPanel();
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            OpenInfoJanitorPanel();
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            OpenInfoNursePanel();
        }
    }

    public void OnConfirmAllocation()
    {
        Debug.Log("Confirming allocation...");

        roleTimeManager.ConfirmAllocation();

        CloseAllUI();
    }
}