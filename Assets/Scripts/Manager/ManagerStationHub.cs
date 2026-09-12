using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManagerStationHub : MonoBehaviour
{
    //singleton 
    public static ManagerStationHub Instance;

    public MainSceneUIManager mainSceneUIManager;

    public GameObject navPanel;

    public GameObject storePanel;
    
    public GameObject redirectPanel;

    TextMeshProUGUI storeInfo;

    TextMeshProUGUI medicineCountLabel;

    Transform redirectListRoot;

    ScrollRect redirectScroll;

   
    bool built;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        MedicineSupplyManager.OnSupplyChanged -= RefreshStore;

        if (Instance == this)
        {
            Instance = null;
        }

    }

    void Start()
    {
        if (mainSceneUIManager == null)
        {
            mainSceneUIManager = FindFirstObjectByType<MainSceneUIManager>();
        }

        EnsureBuilt();

        //remove then add again to prevent duplicates
        MedicineSupplyManager.OnSupplyChanged -= RefreshStore;

        MedicineSupplyManager.OnSupplyChanged += RefreshStore;
    }

    //ensure panels are in scene
    public void EnsureBuilt()
    {
        //skip binding if panel already exist and is valid
        if (built && navPanel != null && storePanel != null && redirectPanel != null)
        {
            return;
        }

        BindScenePanels();
    }

    //open manager hub navigation panel and lock active player chracter movement
    public void OpenHub()
    {
        EnsureBuilt();

        if (navPanel == null)
        {
            return;
        }

        //make sure canvas is visible before showing panel
        if (NewUIRoot.Canvas != null && !NewUIRoot.Canvas.gameObject.activeInHierarchy)
        {
            NewUIRoot.Canvas.gameObject.SetActive(true);
        }


        navPanel.SetActive(true);

        navPanel.transform.SetAsLastSibling();

        HideContentPanels();

        LockPlayer(true);

        //unlock cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;

        Cursor.visible = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play("open");
        }
    }

    //hides sub-panels under the hub
    void HideContentPanels()
    {
        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }

        if (redirectPanel != null)
        {
            redirectPanel.SetActive(false);
        }

        if (mainSceneUIManager != null && mainSceneUIManager.jobPanel != null)
        {
            mainSceneUIManager.jobPanel.SetActive(false);
        }

    }

    //close entire hub and restores player control
    public void CloseAll()
    {
        CloseAll(true);
    }

    public void CloseAll(bool playCloseSound)
    {
        if (navPanel != null)
        {
            navPanel.SetActive(false);
        }

        HideContentPanels();

        LockPlayer(false);

        if (playCloseSound && AudioManager.Instance != null)
        {
            AudioManager.Instance.Play("close");
        }
    }

    //freezes or unfreezes active player character's movement and camera
    void LockPlayer(bool freeze)
    {
        var active = CharacterSwitchManager.Instance != null ? CharacterSwitchManager.Instance.ActiveCharacter : null;

        if (active != null && active.movement != null)
        {
            active.movement.SetControlsEnabled(!freeze);
        }

        else
        {
            //if there is no active character. safety net
            var move = FindFirstObjectByType<SimplePlayerMovement>();
            if (move != null) move.SetControlsEnabled(!freeze);
        }

        var cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null)
        {
            cam.LockCursor(!freeze);
        }
    }

    //finds panels in scene and wires buttons and labels
    void BindScenePanels()
    {
        if (navPanel == null)
        {
            navPanel = ClinicalUIFactory.FindByName("ManagerNavPanel");
        }

        if (storePanel == null)
        {
            storePanel = ClinicalUIFactory.FindByName("OnlineStorePanel");
        }

        if (redirectPanel == null)
        {
            redirectPanel = ClinicalUIFactory.FindByName("RedirectPatientsPanel");
        }


        //log error if a panel is missing
        if (navPanel == null || storePanel == null || redirectPanel == null)
        {
            Debug.LogError("ManagerStationHub missing ManagerNavPanel, OnlineStorePanel or RedirectPatientsPanel panels in scene");

            built = false;

            return;
        }

        navPanel.SetActive(false);

        storePanel.SetActive(false);

        redirectPanel.SetActive(false);

        ClinicalUIFactory.BindButton(navPanel.transform, "Shift AllocationButton", OpenShiftAllocation);
        ClinicalUIFactory.BindButton(navPanel.transform, "Online StoreButton", OpenStore);
        ClinicalUIFactory.BindButton(navPanel.transform, "Redirect PatientsButton", OpenRedirect);
        ClinicalUIFactory.BindButton(navPanel.transform, "CloseButton", CloseAll);

        storeInfo = ClinicalUIFactory.FindLabel(storePanel.transform, "StoreInfoLabel");
        medicineCountLabel = ClinicalUIFactory.FindLabel(storePanel.transform, "MedicineCountLabel");
        ClinicalUIFactory.BindButton(storePanel.transform, "Buy Medicine BoxButton", BuyMedicine);

        ClinicalUIFactory.BindButton(storePanel.transform, "BackButton", () => { storePanel.SetActive(false); navPanel.SetActive(true); });

        //locate scroll view and content root in the scene for redirect list
        var scrollT = ClinicalUIFactory.FindChild(redirectPanel.transform, "RedirectScroll");

        if (scrollT != null)
        {
            redirectScroll = scrollT.GetComponent<ScrollRect>();
        }

        var contentT = ClinicalUIFactory.FindChild(redirectPanel.transform, "Content");

        if (contentT != null)
        {
            redirectListRoot = contentT;
        }

        ClinicalUIFactory.BindButton(redirectPanel.transform, "BackButton", () => { redirectPanel.SetActive(false); navPanel.SetActive(true); });

        built = true;

        RefreshStore();
    }

    //switch from navigation panel to shift allocation panel
    void OpenShiftAllocation()
    {
        if (navPanel != null)
        {
            navPanel.SetActive(false);
        }


        if (mainSceneUIManager == null)
        {
            mainSceneUIManager = FindFirstObjectByType<MainSceneUIManager>();
        }


        if (mainSceneUIManager != null)
        {
            mainSceneUIManager.OpenJobPanel();
        }
        else
        {
            Debug.LogWarning("MainSceneUIManager missing for shift allocation");
        }

    }

    //switches from navigation panel to online store panel
    void OpenStore()
    {
        if (navPanel != null)
        {
            navPanel.SetActive(false);
        }

        if (storePanel != null)
        {
            storePanel.SetActive(true);
        }

        RefreshStore();
    }

    //switch from navigation panel to redirect panel
    void OpenRedirect()
    {
        if (navPanel != null)
        {
            navPanel.SetActive(false);
        }

        if (redirectPanel != null)
        {
            redirectPanel.SetActive(true);
        }

        RebuildRedirectList();
    }

    //check if can purchase medicine box 
    void BuyMedicine()
    {
        if (MedicineSupplyManager.Instance == null)
        {
            return;
        }

        if (MedicineSupplyManager.Instance.TryPurchaseBox())
        {
            RefreshStore();
        }
        else if (storeInfo != null)
        {
            // Show message if budget is not enough to pruchase medicine
            storeInfo.text = "Not enough budget for a medicine box (R500)";
        }
    }

    //updates store labels with updated supply and budget if values changed
    void RefreshStore()
    {
        if (medicineCountLabel == null)
        {
            return;
        }

        int count = MedicineSupplyManager.Instance != null ? MedicineSupplyManager.Instance.medicineCount : 0;
        int money = HospitalStatsManager.Instance != null ? HospitalStatsManager.Instance.money : 0;

        medicineCountLabel.text = $"Supply on shelves: {count},  Budget now: {HospitalStatsManager.FormatMoney(money)}";

        if (storeInfo != null)
        {
            storeInfo.text = $"Medicine Box:  R{MedicineSupplyManager.BoxPrice}\nOne purchase = one box at Storage room (Nurse unpacks and organizes on rack)";
        }

    }

    //clears and rebuilds list of redirectable patients for manager hub
    void RebuildRedirectList()
    {
        if (redirectListRoot == null || PatientCareSystem.Instance == null)
        {
            return;
        }

        //destroy old rows before building. starting new
        for (int i = redirectListRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(redirectListRoot.GetChild(i).gameObject);
        }


        int listed = 0;

        foreach (var p in PatientCareSystem.Instance.patients)
        {
            //skip null or already recovered patients
            if ((p == null) || (p.recovered))
            {
                continue;
            }

            CreateRedirectRow(p);

            listed++;
        }

        //show placeholder text in scroll view if list is empty
        if (listed == 0)
        {
            ClinicalUIFactory.CreateLabel(redirectListRoot, "No patients available to redirect.", 22, Vector2.zero);
        }

        if (redirectScroll != null)
        {
            Canvas.ForceUpdateCanvases();

            //scroll to top of scroll view after building layout
            redirectScroll.verticalNormalizedPosition = 1f;
        }
    }

    //build UI row for a single patient for manager redrect screen
    void CreateRedirectRow(PatientRecord patient)
    {
        var captured = patient;

        //create row container 
        var row = new GameObject("RedirectRow_" + patient.patientName, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));

        row.transform.SetParent(redirectListRoot, false);

        var rowImg = row.GetComponent<Image>();

        rowImg.color = new Color(0.16f, 0.22f, 0.26f, 0.95f);

        var le = row.GetComponent<LayoutElement>();

        le.minHeight = ClinicalUIFactory.Scale(new Vector2(0, 48)).y;

        le.preferredHeight = ClinicalUIFactory.Scale(new Vector2(0, 48)).y;

        var h = row.GetComponent<HorizontalLayoutGroup>();

        h.padding = new RectOffset(10, 10, 6, 6);
        h.spacing = 10;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        h.childControlWidth = true;
        h.childControlHeight = true;

        string severityText = patient.SeverityLabel;

        //override label text based on tag, critical or not critical
        if (patient.severityTagged && patient.severity == PatientSeverity.Critical)
        {
            severityText = "Critical";
        }
        else if (patient.severityTagged)
        {
            severityText = "Not Critical";
        }


        //info text in scene above patient showing name and if attened, critical or not critical
        var infoGo = new GameObject("Info", typeof(RectTransform), typeof(LayoutElement));

        infoGo.transform.SetParent(row.transform, false);

        var infoLe = infoGo.GetComponent<LayoutElement>();

        infoLe.flexibleWidth = 1f;

        infoLe.minWidth = 280f;

        var info = infoGo.AddComponent<TextMeshProUGUI>();

        info.text = $"{patient.patientName}  >  {severityText}";

        info.fontSize = ClinicalUIFactory.ScaleFont(22);

        //color code text above patients. yellow if untagged, red if tagged critical
        info.color = !patient.severityTagged ? new Color(1f, 0.9f, 0.4f) : patient.severity == PatientSeverity.Critical ? ClinicalUIFactory.GetAccentRed() : ClinicalUIFactory.GetTextColor();
        info.alignment = TextAlignmentOptions.MidlineLeft;
        info.enableWordWrapping = false;
        info.overflowMode = TextOverflowModes.Ellipsis;

        //only allow redirect if patient has been tagged. otherwise cant redirect
        bool canRedirect = patient.severityTagged;

        var redirectBtn = ClinicalUIFactory.CreateButton(row.transform, canRedirect ? "Redirect" : "Locked", () =>
        {
            if (PatientCareSystem.Instance == null)
            {
                return;
            }

            //try redirect patient. show message if fails
            if (!PatientCareSystem.Instance.RedirectPatient(captured, out string msg))
            {
                if (NotificationSidePanel.Instance != null)
                {
                    NotificationSidePanel.Instance.ShowRaw(msg);
                }

                return;
            }

            if (NotificationSidePanel.Instance != null)
            {
                NotificationSidePanel.Instance.ShowRaw(msg);
            }

            //refreshes list and amount display after success
            RebuildRedirectList();

            PatientCareSystem.Instance.UpdateCapacityUI();

        }, Vector2.zero, new Vector2(120, 36));

        var btnRt = redirectBtn.GetComponent<RectTransform>();

        btnRt.anchorMin = new Vector2(0, 0);
        btnRt.anchorMax = new Vector2(0, 1);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = Vector2.zero;
        btnRt.sizeDelta = ClinicalUIFactory.Scale(new Vector2(120, 36));

        var btnLe = redirectBtn.gameObject.AddComponent<LayoutElement>();

        btnLe.preferredWidth = ClinicalUIFactory.Scale(new Vector2(120, 0)).x;
        btnLe.minWidth = ClinicalUIFactory.Scale(new Vector2(100, 0)).x;
        btnLe.flexibleWidth = 0f;

        //disable button interaction if patient not redirectable
        if (!canRedirect)
        {
            var btn = redirectBtn.GetComponent<Button>();

            if (btn != null)
            {
                btn.interactable = false;
            }
        }
    }
}
