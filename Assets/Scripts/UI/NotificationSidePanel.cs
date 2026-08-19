using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum NotificationType
{
    MoneyCorruption,
    SuppliesLost,
    InfectionOutbreak,
    DisasterInflux
}

/// <summary>
/// Side panel with randomized hospital event notifications that apply stat effects on appear.
/// Scene object: NotificationSidePanel under NewFeatureUICanvas.
/// </summary>
public class NotificationSidePanel : MonoBehaviour
{
    public static NotificationSidePanel Instance;

    [SerializeField] float intervalMin = 45f;
    [SerializeField] float intervalMax = 90f;

    GameObject root;
    TextMeshProUGUI title;
    TextMeshProUGUI body;
    float nextAt;

    void Awake() => Instance = this;

    void Start()
    {
        BindScenePanel();
        ScheduleNext();
        Invoke(nameof(FireTooFullOnce), 25f);
    }

    void FireTooFullOnce()
    {
        if (PatientCareSystem.Instance != null)
            PatientCareSystem.Instance.TriggerTooFullEvent();
    }

    void BindScenePanel()
    {
        if (root != null) return;

        root = ClinicalUIFactory.FindByName("NotificationSidePanel");
        if (root == null)
        {
            Debug.LogError("NotificationSidePanel: missing scene object 'NotificationSidePanel'.");
            return;
        }

        title = ClinicalUIFactory.FindLabel(root.transform, "TitleLabel");
        body = ClinicalUIFactory.FindLabel(root.transform, "BodyLabel");
        ClinicalUIFactory.BindButton(root.transform, "Dismiss (Esc)Button", Dismiss);
        root.SetActive(false);
    }

    public void Dismiss()
    {
        if (root != null)
            root.SetActive(false);
    }

    void ScheduleNext()
    {
        nextAt = Time.time + Random.Range(intervalMin, intervalMax);
    }

    void Update()
    {
        if (Time.time >= nextAt)
        {
            if (GameManager.Instance != null && GameManager.Instance.isPowerOut)
            {
                nextAt = Time.time + 1f;
                return;
            }

            FireRandom();
            ScheduleNext();
        }
    }

    void FireRandom()
    {
        var values = (NotificationType[])System.Enum.GetValues(typeof(NotificationType));
        Fire(values[Random.Range(0, values.Length)]);
    }

    public void Fire(NotificationType type)
    {
        var stats = HospitalStatsManager.Instance;
        string t;
        string msg;

        switch (type)
        {
            case NotificationType.MoneyCorruption:
                t = "Budget Leak";
                msg = "Funds were siphoned from the accounts. -R150";
                if (stats != null)
                {
                    stats.money = Mathf.Max(0, stats.money - 150);
                    HospitalStatsManager.OnStatsChanged?.Invoke();
                }
                break;
            case NotificationType.SuppliesLost:
                t = "Supplies Missing";
                msg = "Medicine went missing from storage. -3 supply";
                MedicineSupplyManager.Instance?.RaidRandomMedicines(3);
                MedicineSupplyManager.Instance?.AddMedicineCount(-3);
                break;
            case NotificationType.InfectionOutbreak:
                t = "Infection Outbreak";
                msg = "Hygiene breach spreading. Hygiene, comfort and morale drop.";
                stats?.ChangeSanitation(-15);
                stats?.ChangeComfort(-12);
                stats?.ChangeMorale(-12);
                PatientCareSystem.Instance?.SetOutbreak(true);
                break;
            default:
                t = "External Disaster";
                msg = "Bus accident / collapse nearby. Incoming patients increase.";
                PatientCareSystem.Instance?.AddIncomingPatients(4);
                break;
        }

        ShowRaw(msg, t);
    }

    public void ShowRaw(string message, string heading = "Hospital Alert")
    {
        if (root == null) BindScenePanel();
        if (title != null) title.text = heading;
        if (body != null) body.text = message;
        if (root != null)
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("PopUpNontification");
    }

    public void ShowPowerOutage(int secondsRemaining)
    {
        ShowRaw(PowerOutageBody(secondsRemaining), "Power Outage");
    }

    public void UpdatePowerOutageCountdown(int secondsRemaining)
    {
        if (root == null || !root.activeSelf) return;
        if (title != null) title.text = "Power Outage";
        if (body != null) body.text = PowerOutageBody(secondsRemaining);
    }

    public void ShowPowerRestored()
    {
        ShowRaw(
            "Power has been restored. Hospital stats have returned to their normal pace.",
            "Power Restored");
    }

    static string PowerOutageBody(int secondsRemaining)
    {
        return "Hospital power is down. Hygiene, comfort and morale are dropping faster.\n" +
               $"Power returns in {Mathf.Max(0, secondsRemaining)}s.";
    }
}
