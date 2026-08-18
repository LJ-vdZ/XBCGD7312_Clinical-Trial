using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum PatientSeverity { NotCritical, Critical }
public enum InfectionTag { Unknown, Uninfected, Infected }
public enum PatientQueue { None, Nurse, Doctor }

[System.Serializable]
public class PatientRecord
{
    public string patientName;
    public string symptoms;
    public string correctDiagnosis;
    public PatientSeverity severity;
    public bool severityTagged;
    public InfectionTag infection;
    public PatientQueue queue;
    public bool recovered;
    public bool treatedByNurse;
    public GameObject worldObject;
    public TextMeshPro statusWorldText;

    public string SeverityLabel =>
        !severityTagged ? "Not attended"
        : severity == PatientSeverity.Critical ? "Critical"
        : "Not Critical";
}

/// <summary>
/// Binds scene Patient props, critical / non-critical status, nurse 50% treat, doctor 100% treat, redirect.
/// </summary>
public class PatientCareSystem : MonoBehaviour
{
    public static PatientCareSystem Instance;

    public int capacity = 10;
    public int incomingPatients;

    public readonly List<PatientRecord> patients = new List<PatientRecord>();

    TextMeshProUGUI capacityLabel;
    bool outbreakActive;

    static readonly string[] NamePool =
    {
        "Amina", "Thabo", "Lerato", "Johan", "Fatima", "Sipho", "Naledi", "Erik", "Zanele", "Chris", "Priya"
    };

    public static readonly string[] DiagnosisOptions =
    {
        "Flu", "Heart Issue", "Migraine", "Infection", "Fracture"
    };

    struct DiagnosisCase
    {
        public string diagnosis;
        public string symptoms;
    }

    static readonly DiagnosisCase[] DiagnosisCases =
    {
        new DiagnosisCase
        {
            diagnosis = "Flu",
            symptoms = "Fever, dry cough, body aches, and fatigue for several days."
        },
        new DiagnosisCase
        {
            diagnosis = "Heart Issue",
            symptoms = "Chest pain when breathing deeply, shortness of breath, and weakness."
        },
        new DiagnosisCase
        {
            diagnosis = "Migraine",
            symptoms = "Severe headache, light sensitivity, and nausea. Needs to lie down."
        },
        new DiagnosisCase
        {
            diagnosis = "Infection",
            symptoms = "Open wound with redness, swelling, warmth, and rising fever."
        },
        new DiagnosisCase
        {
            diagnosis = "Fracture",
            symptoms = "Swollen wrist after a fall, sharp pain on movement, stable vitals."
        }
    };

    static DiagnosisCase PickCase(int seedIndex)
    {
        return DiagnosisCases[seedIndex % DiagnosisCases.Length];
    }

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        BuildFromScenePatients();
        BuildCapacityUI();
        UpdateCapacityUI();
        RefreshAllStatusLabels();
    }

    /// <summary>
    /// Binds care records to hospital beds (PatientDiagnosisStation) and named patient props.
    /// Beds are the real E-interact targets in HospitalHubLevel; patient meshes are children.
    /// </summary>
    public void BuildFromScenePatients()
    {
        patients.Clear();

        var roots = new List<Transform>();
        var claimed = new HashSet<Transform>();

        // Primary: beds / props that already have the legacy doctor station + InteractableTrigger.
        var stations = FindObjectsByType<PatientDiagnosisStation>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var station in stations)
        {
            if (station == null) continue;
            var t = station.transform;
            if (!claimed.Add(t)) continue;
            roots.Add(t);
        }

        // Fallback: named patient meshes not already under a station bed.
        var all = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var t in all)
        {
            if (!IsPatientWorldObject(t)) continue;
            if (t.parent != null && IsPatientWorldObject(t.parent)) continue;
            if (t.GetComponentInParent<PatientDiagnosisStation>(true) != null) continue;
            if (!claimed.Add(t)) continue;
            roots.Add(t);
        }

        // Stable order by name then position
        roots.Sort((a, b) =>
        {
            int c = string.CompareOrdinal(a.name, b.name);
            if (c != 0) return c;
            return a.GetInstanceID().CompareTo(b.GetInstanceID());
        });

        for (int i = 0; i < roots.Count; i++)
        {
            var t = roots[i];
            var diagnosisCase = PickCase(i);
            var record = new PatientRecord
            {
                patientName = NamePool[i % NamePool.Length],
                symptoms = diagnosisCase.symptoms,
                correctDiagnosis = diagnosisCase.diagnosis,
                severity = PatientSeverity.NotCritical,
                severityTagged = false,
                infection = InfectionTag.Unknown,
                queue = PatientQueue.Nurse,
                recovered = false,
                treatedByNurse = false,
                worldObject = t.gameObject
            };
            patients.Add(record);
            WirePatientInteractable(t.gameObject, record);
        }

        incomingPatients = 0;
        Debug.Log($"PatientCareSystem: bound {patients.Count} scene patient(s).");
    }

    static bool IsPatientWorldObject(Transform t)
    {
        if (t == null) return false;
        if (t.GetComponent<RectTransform>() != null) return false;
        if (t.GetComponentInParent<Canvas>() != null) return false;

        string n = t.name;
        // "Patient", "Patient (1)", "Male Patient", "Female_Patient", etc.
        if (n.StartsWith("Patient", System.StringComparison.OrdinalIgnoreCase))
            return true;
        if (n.IndexOf("Patient", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        return false;
    }

    void WirePatientInteractable(GameObject go, PatientRecord record)
    {
        // Kill legacy DoctorUI stations on this bed/patient hierarchy.
        foreach (var legacy in go.GetComponentsInChildren<PatientDiagnosisStation>(true))
        {
            if (legacy == null) continue;
            legacy.enabled = false;
        }
        var parentStation = go.GetComponentInParent<PatientDiagnosisStation>(true);
        if (parentStation != null) parentStation.enabled = false;

        var pi = go.GetComponent<PatientInteractable>();
        if (pi == null) pi = go.AddComponent<PatientInteractable>();
        pi.Bind(record);

        EnsurePatientTriggerVolume(go);

        // Beds already have InteractableTrigger — force them onto PatientInteractable.
        foreach (var trigger in go.GetComponentsInChildren<InteractableTrigger>(true))
        {
            if (trigger != null) trigger.ForceResolve();
        }
        var selfTrigger = go.GetComponent<InteractableTrigger>();
        if (selfTrigger == null) selfTrigger = go.AddComponent<InteractableTrigger>();
        selfTrigger.ForceResolve();

        if (record.statusWorldText == null)
        {
            var existing = go.transform.Find("StatusLabel");
            TextMeshPro tmp;
            GameObject labelGo;
            if (existing != null)
            {
                labelGo = existing.gameObject;
                tmp = existing.GetComponent<TextMeshPro>();
                if (tmp == null) tmp = labelGo.AddComponent<TextMeshPro>();
            }
            else
            {
                labelGo = new GameObject("StatusLabel");
                tmp = labelGo.AddComponent<TextMeshPro>();
            }

            // World-fixed above the bed so cough/idle animation does not bounce the text.
            Vector3 anchor = ResolvePatientLabelWorldPosition(go);
            labelGo.transform.SetParent(null, true);
            labelGo.transform.position = anchor;
            labelGo.transform.rotation = Quaternion.identity;

            tmp.fontSize = 2.4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.text = "";
            record.statusWorldText = tmp;
            pi.statusWorldText = tmp;
            pi.SetStatusWorldAnchor(anchor);
        }
        else
        {
            pi.SetStatusWorldAnchor(record.statusWorldText.transform.position);
        }
    }

    static Vector3 ResolvePatientLabelWorldPosition(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    b.Encapsulate(renderers[i].bounds);
            }
            // Sit above the bed/patient bounds (a bit higher than before).
            return new Vector3(b.center.x, b.max.y + 0.85f, b.center.z);
        }

        return go.transform.position + Vector3.up * 2.4f;
    }

    /// <summary>
    /// Always add a dedicated trigger volume so Nurse/Doctor can press E near the patient.
    /// Mesh colliders on low-poly props are often non-trigger and too tight.
    /// </summary>
    static void EnsurePatientTriggerVolume(GameObject go)
    {
        const string volumeName = "PatientInteractTrigger";
        Transform existing = go.transform.Find(volumeName);
        GameObject volumeGo;
        if (existing != null)
        {
            volumeGo = existing.gameObject;
        }
        else
        {
            volumeGo = new GameObject(volumeName);
            volumeGo.transform.SetParent(go.transform, false);
            volumeGo.transform.localPosition = Vector3.zero;
            volumeGo.transform.localRotation = Quaternion.identity;
            volumeGo.transform.localScale = Vector3.one;
        }

        var box = volumeGo.GetComponent<BoxCollider>();
        if (box == null) box = volumeGo.AddComponent<BoxCollider>();
        box.isTrigger = true;

        // Size from renderers when possible; otherwise a generous standing volume.
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    b.Encapsulate(renderers[i].bounds);
            }

            // Convert world bounds into this volume's local space.
            Vector3 localCenter = go.transform.InverseTransformPoint(b.center);
            Vector3 lossy = go.transform.lossyScale;
            Vector3 localSize = new Vector3(
                b.size.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
                b.size.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
                b.size.z / Mathf.Max(0.0001f, Mathf.Abs(lossy.z)));

            // Expand so the player can stand beside the bed/patient and still trigger.
            localSize.x = Mathf.Max(localSize.x * 1.4f, 1.6f);
            localSize.y = Mathf.Max(localSize.y * 1.2f, 2.0f);
            localSize.z = Mathf.Max(localSize.z * 1.4f, 1.6f);

            volumeGo.transform.localPosition = localCenter;
            box.center = Vector3.zero;
            box.size = localSize;
        }
        else
        {
            volumeGo.transform.localPosition = new Vector3(0f, 1f, 0f);
            box.center = Vector3.zero;
            box.size = new Vector3(2f, 2.2f, 2f);
        }

        // Trigger messages need a Rigidbody on one side — CharacterController counts,
        // but also put a kinematic RB on the volume for reliability with scaled props.
        var rb = volumeGo.GetComponent<Rigidbody>();
        if (rb == null) rb = volumeGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Forward trigger events from the child volume to the patient root InteractableTrigger.
        var relay = volumeGo.GetComponent<PatientTriggerRelay>();
        if (relay == null) relay = volumeGo.AddComponent<PatientTriggerRelay>();
        relay.patientRoot = go;
    }

    void BuildCapacityUI()
    {
        capacityLabel = StatsSideHudPlacement.CreatePatientsLabel();
    }

    public int CurrentPatientCount
    {
        get
        {
            int live = 0;
            foreach (var p in patients)
            {
                if (p != null && !p.recovered) live++;
            }
            return live + incomingPatients;
        }
    }

    public void UpdateCapacityUI()
    {
        if (capacityLabel == null) return;
        int cur = CurrentPatientCount;
        capacityLabel.text = $"Patients {cur}/{capacity}";
        capacityLabel.color = cur > capacity ? ClinicalUIFactory.GetAccentRed() : ClinicalUIFactory.GetTextColor();
    }

    public void RefreshAllStatusLabels()
    {
        foreach (var p in patients)
            RefreshStatusLabel(p);
    }

    public void RefreshStatusLabel(PatientRecord record)
    {
        if (record?.statusWorldText == null) return;

        string infection = record.infection == InfectionTag.Unknown
            ? ""
            : $"\n{record.infection}";

        // Name stays on top. Untagged / Needs tag both read as "Not attended".
        string body;
        if (record.recovered)
            body = $"{record.SeverityLabel}\nRecovering";
        else if (!record.severityTagged)
            body = "Not attended";
        else if (record.queue == PatientQueue.Doctor)
            body = $"{record.SeverityLabel}\nNeeds doctor";
        else if (record.treatedByNurse)
            body = $"{record.SeverityLabel}\nChecked";
        else
            body = $"{record.SeverityLabel}\nAwaiting treat";

        record.statusWorldText.text = $"{record.patientName}\n{body}{infection}";
        record.statusWorldText.color = !record.severityTagged
            ? new Color(1f, 0.9f, 0.4f)
            : record.severity == PatientSeverity.Critical
                ? new Color(1f, 0.35f, 0.35f)
                : new Color(0.85f, 0.95f, 0.85f);
    }

    public void AddIncomingPatients(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            var diagnosisCase = PickCase(Random.Range(0, DiagnosisCases.Length));
            patients.Add(new PatientRecord
            {
                patientName = NamePool[Random.Range(0, NamePool.Length)] + " N" + patients.Count,
                symptoms = diagnosisCase.symptoms,
                correctDiagnosis = diagnosisCase.diagnosis,
                severity = PatientSeverity.NotCritical,
                severityTagged = false,
                infection = outbreakActive ? InfectionTag.Unknown : InfectionTag.Uninfected,
                queue = PatientQueue.Nurse
            });
        }
        incomingPatients = 0;
        UpdateCapacityUI();
    }

    public void SetOutbreak(bool active) => outbreakActive = active;
    public bool IsOutbreakActive => outbreakActive;

    /// <summary>Nurse treatment: 50% chance to recover. Requires Critical / Not Critical tag first.</summary>
    public void NurseTreat(PatientRecord record)
    {
        if (record == null || record.recovered) return;

        if (!record.severityTagged)
        {
            if (NotificationSidePanel.Instance != null)
                NotificationSidePanel.Instance.ShowRaw("Read the symptoms, then tag Critical or Not Critical before treating.");
            return;
        }

        if (MedicineSupplyManager.Instance != null)
        {
            if (MedicineSupplyManager.Instance.medicineCount < 1)
            {
                if (NotificationSidePanel.Instance != null)
                    NotificationSidePanel.Instance.ShowRaw("No medicine left. Buy / unpack supplies first.");
                return;
            }
            MedicineSupplyManager.Instance.TryConsumeMedicine(1);
        }

        record.treatedByNurse = true;
        bool recovered = Random.value < 0.5f;

        if (recovered)
        {
            record.recovered = true;
            record.queue = PatientQueue.None;
            if (HospitalStatsManager.Instance != null)
            {
                HospitalStatsManager.Instance.ChangeComfort(+6f);
                HospitalStatsManager.Instance.ChangeMorale(+4f);
            }
            CharacterMoraleSystem.NotifyPatientOutcome(true);
            if (AudioManager.Instance != null) AudioManager.Instance.Play("success");
            if (NotificationSidePanel.Instance != null)
                NotificationSidePanel.Instance.ShowRaw($"{record.patientName} is recovering (nurse care).");
        }
        else
        {
            record.queue = PatientQueue.Doctor;
            if (record.severity == PatientSeverity.Critical && HospitalStatsManager.Instance != null)
                HospitalStatsManager.Instance.ChangeMorale(-3f);

            CharacterMoraleSystem.NotifyPatientOutcome(false);
            if (AudioManager.Instance != null) AudioManager.Instance.Play("fail");
            if (NotificationSidePanel.Instance != null)
                NotificationSidePanel.Instance.ShowRaw($"{record.patientName} not improving — send to doctor.");
        }

        RefreshStatusLabel(record);
        UpdateCapacityUI();
    }

    public void DoctorTreat(PatientRecord record, bool consumeMedicine = true)
    {
        if (record == null || record.recovered) return;

        if (consumeMedicine && MedicineSupplyManager.Instance != null)
            MedicineSupplyManager.Instance.TryConsumeMedicine(1);

        CompleteDoctorRecovery(record);
    }

    /// <summary>
    /// Doctor selects a diagnosis. Costs 1 medicine per attempt.
    /// Correct diagnosis recovers the patient; wrong diagnosis requires another attempt.
    /// </summary>
    public bool TryDoctorDiagnosis(PatientRecord record, string chosenDiagnosis, out string message)
    {
        message = "";
        if (record == null)
        {
            message = "No patient selected.";
            return false;
        }

        if (record.recovered)
        {
            message = $"{record.patientName} is already recovering.";
            return false;
        }

        if (MedicineSupplyManager.Instance == null || MedicineSupplyManager.Instance.medicineCount < 1)
        {
            message = "No medicine left. Buy / unpack supplies first.";
            return false;
        }

        MedicineSupplyManager.Instance.TryConsumeMedicine(1);

        bool correct = !string.IsNullOrEmpty(record.correctDiagnosis)
                       && string.Equals(chosenDiagnosis, record.correctDiagnosis, System.StringComparison.OrdinalIgnoreCase);

        if (correct)
        {
            CompleteDoctorRecovery(record);
            message = $"Correct diagnosis ({chosenDiagnosis}). {record.patientName} is stabilizing.";
            return true;
        }

        message = $"Incorrect diagnosis ({chosenDiagnosis}). Reassess and try again — 1 medicine used.";
        if (AudioManager.Instance != null) AudioManager.Instance.Play("fail");
        CharacterMoraleSystem.NotifyPatientOutcome(false);
        return false;
    }

    void CompleteDoctorRecovery(PatientRecord record)
    {
        record.recovered = true;
        record.queue = PatientQueue.None;
        if (HospitalStatsManager.Instance != null)
        {
            HospitalStatsManager.Instance.ChangeComfort(+10f);
            HospitalStatsManager.Instance.ChangeMorale(+6f);
        }
        CharacterMoraleSystem.NotifyPatientOutcome(true);
        if (AudioManager.Instance != null) AudioManager.Instance.Play("success");
        if (NotificationSidePanel.Instance != null)
            NotificationSidePanel.Instance.ShowRaw($"{record.patientName} stabilized by doctor.");

        RefreshStatusLabel(record);
        UpdateCapacityUI();
    }

    public void TagPatient(PatientRecord record, InfectionTag infection, PatientSeverity severity)
    {
        if (record == null) return;
        record.infection = infection;
        record.severity = severity;
        record.severityTagged = true;
        RefreshStatusLabel(record);
        if (NotificationSidePanel.Instance != null)
            NotificationSidePanel.Instance.ShowRaw($"{record.patientName} tagged: {record.SeverityLabel}.");
    }

    public void TagSeverity(PatientRecord record, PatientSeverity severity)
    {
        if (record == null) return;
        record.severity = severity;
        record.severityTagged = true;
        RefreshStatusLabel(record);
        if (NotificationSidePanel.Instance != null)
            NotificationSidePanel.Instance.ShowRaw($"{record.patientName} tagged: {record.SeverityLabel}.");
    }

    public bool RedirectPatient(PatientRecord record, out string message)
    {
        message = "";
        if (record == null)
        {
            message = "No patient selected.";
            return false;
        }

        if (!record.severityTagged)
        {
            message = $"{record.patientName} is Not attended — a nurse must tag Critical or Not Critical before redirect.";
            return false;
        }

        bool critical = record.severity == PatientSeverity.Critical;
        string name = record.patientName;

        patients.Remove(record);
        if (record.worldObject != null)
            Object.Destroy(record.worldObject);

        if (critical)
        {
            message = $"{name} didn't survive the trip to the neighboring hospital.";
            if (HospitalStatsManager.Instance != null)
            {
                HospitalStatsManager.Instance.ChangeMorale(-20f);
                HospitalStatsManager.Instance.ChangeComfort(-15f);
            }
            CharacterMoraleSystem.NotifyPatientOutcome(false);
            if (AudioManager.Instance != null) AudioManager.Instance.Play("fail");
        }
        else
        {
            // Not Critical: only patient count drops — no morale/comfort penalty.
            message = $"{name} was redirected to the neighboring hospital.";
            if (AudioManager.Instance != null) AudioManager.Instance.Play("success");
        }

        UpdateCapacityUI();
        return true;
    }

    public void TriggerTooFullEvent()
    {
        AddIncomingPatients(5);
        if (NotificationSidePanel.Instance != null)
            NotificationSidePanel.Instance.ShowRaw(
                "Disaster influx: Clinic overflow. +5 patients. Hospital over capacity.");
    }
}

public class PatientInteractable : MonoBehaviour, IInteractable
{
    public PatientRecord record;
    public TextMeshPro statusWorldText;
    public bool CanInteractWhenLocked => false;

    Vector3 statusWorldAnchor;
    bool hasStatusWorldAnchor;

    static GameObject sharedCarePanel;
    static GameObject sharedDoctorPanel;
    static TextMeshProUGUI sharedNurseTagStatusLabel;
    static TextMeshProUGUI sharedDoctorStatusLabel;
    static bool nurseUiBound;
    static bool doctorUiBound;
    static PatientInteractable activeCareUi;

    GameObject carePanel;
    GameObject doctorPanel;
    TextMeshProUGUI nurseTagStatusLabel;
    TextMeshProUGUI doctorStatusLabel;
    PatientRecord panelRecord;

    public void Bind(PatientRecord r)
    {
        record = r;
        if (r != null && r.statusWorldText != null)
            statusWorldText = r.statusWorldText;
    }

    public void SetStatusWorldAnchor(Vector3 worldPosition)
    {
        statusWorldAnchor = worldPosition;
        hasStatusWorldAnchor = true;
        if (statusWorldText != null)
            statusWorldText.transform.position = worldPosition;
    }

    void LateUpdate()
    {
        if (statusWorldText == null || Camera.main == null) return;

        // Keep label pinned in world space (ignore patient/bed animation).
        if (hasStatusWorldAnchor)
            statusWorldText.transform.position = statusWorldAnchor;

        statusWorldText.transform.rotation = Quaternion.LookRotation(
            statusWorldText.transform.position - Camera.main.transform.position);
    }

    public void Interact(GameObject player, RoleType role)
    {
        var sys = PatientCareSystem.Instance;
        if (sys == null || record == null) return;

        if (role == RoleType.Nurse)
        {
            if (MiniGameGate.Instance != null)
                MiniGameGate.Instance.Unlock(RoleType.Nurse);

            OpenNurseCarePanel(record);
        }
        else if (role == RoleType.Doctor)
        {
            if (MiniGameGate.Instance != null)
                MiniGameGate.Instance.Unlock(RoleType.Doctor);

            if (record.recovered)
            {
                if (NotificationSidePanel.Instance != null)
                    NotificationSidePanel.Instance.ShowRaw($"{record.patientName} is already recovering.");
                return;
            }

            // New doctor assessment UI — scene DoctorUI / DiagnosisMinigame is left unused.
            OpenDoctorCarePanel(record);
        }
        else if (role == RoleType.Manager)
        {
            if (NotificationSidePanel.Instance != null)
            {
                NotificationSidePanel.Instance.ShowRaw(
                    $"{record.patientName}: {record.SeverityLabel} — use Redirect at the computer.");
            }
        }
    }

    void OpenNurseCarePanel(PatientRecord r)
    {
        BindNurseCarePanel();
        activeCareUi = this;
        panelRecord = r;
        carePanel = sharedCarePanel;
        nurseTagStatusLabel = sharedNurseTagStatusLabel;

        if (carePanel == null) return;

        RefreshNurseCarePanel();
        carePanel.SetActive(true);
        carePanel.transform.SetAsLastSibling();
        LockPlayer(true);
    }

    void OpenDoctorCarePanel(PatientRecord r)
    {
        BindDoctorCarePanel();
        activeCareUi = this;
        panelRecord = r;
        doctorPanel = sharedDoctorPanel;
        doctorStatusLabel = sharedDoctorStatusLabel;

        if (doctorPanel == null) return;

        RefreshDoctorCarePanel();
        doctorPanel.SetActive(true);
        doctorPanel.transform.SetAsLastSibling();
        LockPlayer(true);

        if (MiniGameTimerUI.Instance != null)
            MiniGameTimerUI.Instance.StartTimer("Doctor Assessment", 60f, null);
    }

    void BindDoctorCarePanel()
    {
        if (sharedDoctorPanel == null)
            sharedDoctorPanel = ClinicalUIFactory.FindByName("DoctorPatientCarePanel");
        if (sharedDoctorPanel == null)
        {
            Debug.LogError("PatientInteractable: missing scene object 'DoctorPatientCarePanel'.");
            return;
        }

        doctorPanel = sharedDoctorPanel;
        sharedDoctorStatusLabel = ClinicalUIFactory.FindLabel(sharedDoctorPanel.transform, "DoctorStatusLabel");
        doctorStatusLabel = sharedDoctorStatusLabel;

        if (doctorUiBound) return;
        doctorUiBound = true;

        foreach (var diagnosis in PatientCareSystem.DiagnosisOptions)
        {
            string captured = diagnosis;
            ClinicalUIFactory.BindButton(sharedDoctorPanel.transform, captured + "Button", () =>
            {
                if (activeCareUi != null)
                    activeCareUi.AttemptDoctorDiagnosis(captured);
            });
        }

        ClinicalUIFactory.BindButton(sharedDoctorPanel.transform, "CloseButton", () =>
        {
            if (activeCareUi == null) return;
            if (activeCareUi.doctorPanel != null)
                activeCareUi.doctorPanel.SetActive(false);
            activeCareUi.LockPlayer(false);
            if (MiniGameTimerUI.Instance != null)
                MiniGameTimerUI.Instance.StopTimer();
        });
        sharedDoctorPanel.SetActive(false);
    }

    void RefreshDoctorCarePanel()
    {
        if (doctorPanel == null || panelRecord == null) return;

        var nameT = doctorPanel.transform.Find("DoctorNameLabel");
        if (nameT != null)
        {
            var tmp = nameT.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = $"{panelRecord.patientName}  ({panelRecord.SeverityLabel})";
        }

        var symT = doctorPanel.transform.Find("DoctorSymptomsLabel");
        if (symT != null)
        {
            var tmp = symT.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = string.IsNullOrEmpty(panelRecord.symptoms)
                    ? "Assessment notes unavailable."
                    : "Assessment notes:\n" + panelRecord.symptoms;
            }
        }

        if (doctorStatusLabel != null)
        {
            int meds = MedicineSupplyManager.Instance != null ? MedicineSupplyManager.Instance.medicineCount : 0;
            doctorStatusLabel.text =
                $"Select the diagnosis to treat. Each attempt costs 1 medicine.\nMedicine available: {meds}";
        }
    }

    void AttemptDoctorDiagnosis(string diagnosis)
    {
        if (panelRecord == null || PatientCareSystem.Instance == null) return;

        bool ok = PatientCareSystem.Instance.TryDoctorDiagnosis(panelRecord, diagnosis, out string msg);
        if (NotificationSidePanel.Instance != null)
            NotificationSidePanel.Instance.ShowRaw(msg);

        RefreshDoctorCarePanel();

        if (ok)
        {
            doctorPanel.SetActive(false);
            LockPlayer(false);
            if (MiniGameTimerUI.Instance != null)
                MiniGameTimerUI.Instance.StopTimer();
        }
    }

    void BindNurseCarePanel()
    {
        if (sharedCarePanel == null)
            sharedCarePanel = ClinicalUIFactory.FindByName("NursePatientCarePanel");
        if (sharedCarePanel == null)
        {
            Debug.LogError("PatientInteractable: missing scene object 'NursePatientCarePanel'.");
            return;
        }

        carePanel = sharedCarePanel;
        sharedNurseTagStatusLabel = ClinicalUIFactory.FindLabel(sharedCarePanel.transform, "TagStatusLabel");
        nurseTagStatusLabel = sharedNurseTagStatusLabel;

        if (nurseUiBound) return;
        nurseUiBound = true;

        ClinicalUIFactory.BindButton(sharedCarePanel.transform, "CriticalButton", () =>
        {
            var owner = activeCareUi;
            if (owner?.panelRecord == null) return;
            PatientCareSystem.Instance.TagSeverity(owner.panelRecord, PatientSeverity.Critical);
            owner.RefreshNurseCarePanel();
        });

        ClinicalUIFactory.BindButton(sharedCarePanel.transform, "Not CriticalButton", () =>
        {
            var owner = activeCareUi;
            if (owner?.panelRecord == null) return;
            PatientCareSystem.Instance.TagSeverity(owner.panelRecord, PatientSeverity.NotCritical);
            owner.RefreshNurseCarePanel();
        });

        ClinicalUIFactory.BindButton(sharedCarePanel.transform, "TreatButton", () =>
        {
            var owner = activeCareUi;
            if (owner?.panelRecord == null) return;
            if (!owner.panelRecord.severityTagged)
            {
                if (NotificationSidePanel.Instance != null)
                    NotificationSidePanel.Instance.ShowRaw("Tag Critical or Not Critical after reading the symptoms first.");
                return;
            }

            if (owner.panelRecord.recovered)
            {
                if (NotificationSidePanel.Instance != null)
                    NotificationSidePanel.Instance.ShowRaw($"{owner.panelRecord.patientName} is already recovering.");
                owner.carePanel.SetActive(false);
                owner.LockPlayer(false);
                return;
            }

            if (MiniGameTimerUI.Instance != null)
                MiniGameTimerUI.Instance.StartTimer("Patient Check", 45f, null);

            PatientCareSystem.Instance.NurseTreat(owner.panelRecord);
            owner.RefreshNurseCarePanel();
            owner.carePanel.SetActive(false);
            owner.LockPlayer(false);
        });

        ClinicalUIFactory.BindButton(sharedCarePanel.transform, "CloseButton", () =>
        {
            var owner = activeCareUi;
            if (owner == null) return;
            if (owner.carePanel != null)
                owner.carePanel.SetActive(false);
            owner.LockPlayer(false);
        });
        sharedCarePanel.SetActive(false);
    }

    void RefreshNurseCarePanel()
    {
        if (carePanel == null || panelRecord == null) return;

        var nameT = carePanel.transform.Find("NameLabel");
        if (nameT != null)
        {
            var tmp = nameT.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = panelRecord.patientName;
        }

        var symT = carePanel.transform.Find("SymptomsLabel");
        if (symT != null)
        {
            var tmp = symT.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = string.IsNullOrEmpty(panelRecord.symptoms)
                    ? "No symptom notes."
                    : "Symptoms:\n" + panelRecord.symptoms;
        }

        if (nurseTagStatusLabel != null)
            nurseTagStatusLabel.text = "Tag: " + panelRecord.SeverityLabel;
    }

    void LockPlayer(bool freeze)
    {
        var active = CharacterSwitchManager.Instance != null
            ? CharacterSwitchManager.Instance.ActiveCharacter
            : null;
        if (active?.movement != null)
            active.movement.SetControlsEnabled(!freeze);
        var cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null) cam.LockCursor(!freeze);
        Cursor.lockState = freeze ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = freeze;
    }
}

/// <summary>
/// Child trigger volume on patients — forwards enter/exit to the nurse/doctor interaction target.
/// </summary>
public class PatientTriggerRelay : MonoBehaviour
{
    public GameObject patientRoot;

    void OnTriggerEnter(Collider other)
    {
        if (patientRoot == null) return;
        if (!TryGetPlayer(other, out var playerGo, out var handler)) return;

        var patient = patientRoot.GetComponent<PatientInteractable>()
                      ?? patientRoot.GetComponentInChildren<PatientInteractable>(true);
        if (patient == null) return;

        // Ensure legacy doctor stations stay off while using the new UI.
        foreach (var legacy in patientRoot.GetComponentsInChildren<PatientDiagnosisStation>(true))
        {
            if (legacy != null) legacy.enabled = false;
        }

        RoleType role = RoleType.Manager;
        var prm = playerGo.GetComponent<PlayerRoleManager>();
        if (prm != null) role = prm.CurrentRole;
        if (CharacterSwitchManager.Instance != null)
            role = CharacterSwitchManager.Instance.ActiveRole;

        handler.SetCurrentTarget(patient, role);
    }

    void OnTriggerExit(Collider other)
    {
        if (!TryGetPlayer(other, out _, out var handler)) return;

        // Only clear if this patient is the current target.
        if (handler.GetCurrentTarget() is PatientInteractable pi
            && patientRoot != null
            && pi.gameObject == patientRoot)
        {
            handler.ClearTarget();
        }
    }

    static bool TryGetPlayer(Collider other, out GameObject playerGo, out PlayerInteractionHandler handler)
    {
        playerGo = null;
        handler = other != null
            ? other.GetComponent<PlayerInteractionHandler>() ?? other.GetComponentInParent<PlayerInteractionHandler>()
            : null;
        if (handler == null) return false;
        playerGo = handler.gameObject;
        return true;
    }
}
