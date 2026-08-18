using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manager computer hub: navigation panel with Shift Allocation / Online Store / Redirect Patients.
/// Panels are authored in the scene under NewFeatureUICanvas and shown with SetActive.
/// </summary>
public class ManagerStationHub : MonoBehaviour
{
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
            Instance = null;
    }

    void Start()
    {
        if (mainSceneUIManager == null)
            mainSceneUIManager = FindFirstObjectByType<MainSceneUIManager>();
        EnsureBuilt();
        MedicineSupplyManager.OnSupplyChanged -= RefreshStore;
        MedicineSupplyManager.OnSupplyChanged += RefreshStore;
    }

    public void EnsureBuilt()
    {
        if (built && navPanel != null && storePanel != null && redirectPanel != null)
            return;
        BindScenePanels();
    }

    public void OpenHub()
    {
        EnsureBuilt();
        if (navPanel == null) return;

        if (NewUIRoot.Canvas != null && !NewUIRoot.Canvas.gameObject.activeInHierarchy)
            NewUIRoot.Canvas.gameObject.SetActive(true);

        navPanel.SetActive(true);
        navPanel.transform.SetAsLastSibling();
        HideContentPanels();
        LockPlayer(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (AudioManager.Instance != null) AudioManager.Instance.Play("open");
    }

    void HideContentPanels()
    {
        if (storePanel != null) storePanel.SetActive(false);
        if (redirectPanel != null) redirectPanel.SetActive(false);
        if (mainSceneUIManager != null && mainSceneUIManager.jobPanel != null)
            mainSceneUIManager.jobPanel.SetActive(false);
    }

    public void CloseAll()
    {
        if (navPanel != null) navPanel.SetActive(false);
        HideContentPanels();
        LockPlayer(false);
        if (AudioManager.Instance != null) AudioManager.Instance.Play("close");
    }

    void LockPlayer(bool freeze)
    {
        var active = CharacterSwitchManager.Instance != null
            ? CharacterSwitchManager.Instance.ActiveCharacter
            : null;
        if (active != null && active.movement != null)
            active.movement.SetControlsEnabled(!freeze);
        else
        {
            var move = FindFirstObjectByType<SimplePlayerMovement>();
            if (move != null) move.SetControlsEnabled(!freeze);
        }

        var cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null) cam.LockCursor(!freeze);
    }

    void BindScenePanels()
    {
        if (navPanel == null)
            navPanel = ClinicalUIFactory.FindByName("ManagerNavPanel");
        if (storePanel == null)
            storePanel = ClinicalUIFactory.FindByName("OnlineStorePanel");
        if (redirectPanel == null)
            redirectPanel = ClinicalUIFactory.FindByName("RedirectPatientsPanel");

        if (navPanel == null || storePanel == null || redirectPanel == null)
        {
            Debug.LogError("ManagerStationHub: missing ManagerNavPanel / OnlineStorePanel / RedirectPatientsPanel in the scene.");
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
        ClinicalUIFactory.BindButton(storePanel.transform, "BackButton", () =>
        {
            storePanel.SetActive(false);
            navPanel.SetActive(true);
        });

        var scrollT = ClinicalUIFactory.FindChild(redirectPanel.transform, "RedirectScroll");
        if (scrollT != null)
            redirectScroll = scrollT.GetComponent<ScrollRect>();
        var contentT = ClinicalUIFactory.FindChild(redirectPanel.transform, "Content");
        if (contentT != null)
            redirectListRoot = contentT;
        ClinicalUIFactory.BindButton(redirectPanel.transform, "BackButton", () =>
        {
            redirectPanel.SetActive(false);
            navPanel.SetActive(true);
        });

        built = true;
        RefreshStore();
    }

    void OpenShiftAllocation()
    {
        if (navPanel != null) navPanel.SetActive(false);
        if (mainSceneUIManager == null)
            mainSceneUIManager = FindFirstObjectByType<MainSceneUIManager>();
        if (mainSceneUIManager != null)
            mainSceneUIManager.OpenJobPanel();
        else
            Debug.LogWarning("MainSceneUIManager missing for shift allocation.");
    }

    void OpenStore()
    {
        if (navPanel != null) navPanel.SetActive(false);
        if (storePanel != null) storePanel.SetActive(true);
        RefreshStore();
    }

    void OpenRedirect()
    {
        if (navPanel != null) navPanel.SetActive(false);
        if (redirectPanel != null) redirectPanel.SetActive(true);
        RebuildRedirectList();
    }

    void BuyMedicine()
    {
        if (MedicineSupplyManager.Instance == null) return;
        if (MedicineSupplyManager.Instance.TryPurchaseBox())
            RefreshStore();
        else if (storeInfo != null)
            storeInfo.text = "Not enough budget for a medicine box (R500).";
    }

    void RefreshStore()
    {
        if (medicineCountLabel == null) return;
        int count = MedicineSupplyManager.Instance != null ? MedicineSupplyManager.Instance.medicineCount : 0;
        int money = HospitalStatsManager.Instance != null ? HospitalStatsManager.Instance.money : 0;
        medicineCountLabel.text = $"Supply on shelves: {count}   |   Budget: R{money}";
        if (storeInfo != null)
            storeInfo.text = $"Medicine Box  —  R{MedicineSupplyManager.BoxPrice}\nOne purchase = one box at MedBoxSpawnPoint (Nurse unpacks, then organizes on the rack).";
    }

    void RebuildRedirectList()
    {
        if (redirectListRoot == null || PatientCareSystem.Instance == null) return;
        for (int i = redirectListRoot.childCount - 1; i >= 0; i--)
            Destroy(redirectListRoot.GetChild(i).gameObject);

        int listed = 0;
        foreach (var p in PatientCareSystem.Instance.patients)
        {
            if (p == null || p.recovered) continue;
            CreateRedirectRow(p);
            listed++;
        }

        if (listed == 0)
        {
            ClinicalUIFactory.CreateLabel(redirectListRoot, "No patients available to redirect.", 22, Vector2.zero);
        }

        if (redirectScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            redirectScroll.verticalNormalizedPosition = 1f;
        }
    }

    void CreateRedirectRow(PatientRecord patient)
    {
        var captured = patient;

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
        if (patient.severityTagged && patient.severity == PatientSeverity.Critical)
            severityText = "Critical";
        else if (patient.severityTagged)
            severityText = "Not Critical";

        var infoGo = new GameObject("Info", typeof(RectTransform), typeof(LayoutElement));
        infoGo.transform.SetParent(row.transform, false);
        var infoLe = infoGo.GetComponent<LayoutElement>();
        infoLe.flexibleWidth = 1f;
        infoLe.minWidth = 280f;

        var info = infoGo.AddComponent<TextMeshProUGUI>();
        info.text = $"{patient.patientName}  —  {severityText}";
        info.fontSize = ClinicalUIFactory.ScaleFont(22);
        info.color = !patient.severityTagged
            ? new Color(1f, 0.9f, 0.4f)
            : patient.severity == PatientSeverity.Critical
                ? ClinicalUIFactory.GetAccentRed()
                : ClinicalUIFactory.GetTextColor();
        info.alignment = TextAlignmentOptions.MidlineLeft;
        info.enableWordWrapping = false;
        info.overflowMode = TextOverflowModes.Ellipsis;

        bool canRedirect = patient.severityTagged;
        var redirectBtn = ClinicalUIFactory.CreateButton(row.transform, canRedirect ? "Redirect" : "Locked", () =>
        {
            if (PatientCareSystem.Instance == null) return;
            if (!PatientCareSystem.Instance.RedirectPatient(captured, out string msg))
            {
                if (NotificationSidePanel.Instance != null)
                    NotificationSidePanel.Instance.ShowRaw(msg);
                return;
            }

            if (NotificationSidePanel.Instance != null)
                NotificationSidePanel.Instance.ShowRaw(msg);
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

        if (!canRedirect)
        {
            var btn = redirectBtn.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }
    }
}
