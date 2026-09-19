using System.Collections.Generic;
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
/// Each shown notification lasts 240s with a circular countdown; new ones wait until it finishes.
/// </summary>
public class NotificationSidePanel : MonoBehaviour
{
    public static NotificationSidePanel Instance;

    [SerializeField] float intervalMin = 45f;
    [SerializeField] float intervalMax = 90f;
    [SerializeField] float displayDuration = 240f;

    GameObject root;
    TextMeshProUGUI title;
    TextMeshProUGUI body;
    float nextAt;

    bool isDisplaying;
    float displayRemaining;
    Image timerFill;
    Image timerBackground;
    static Sprite circleSprite;

    readonly Queue<(string heading, string message)> pending = new Queue<(string, string)>();

    void Awake() => Instance = this;

    void Start()
    {
        BindScenePanel();

        if (TutorialMode.IsActive)
        {
            if (root != null)
                root.SetActive(false);
            enabled = false;
            return;
        }

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

        var dismissBtn = ClinicalUIFactory.BindButton(root.transform, "Dismiss (Tab)Button", Dismiss);
        if (dismissBtn == null)
            dismissBtn = ClinicalUIFactory.BindButton(root.transform, "Dismiss (Esc)Button", Dismiss);

        if (dismissBtn != null)
        {
            dismissBtn.gameObject.name = "Dismiss (Tab)Button";
            var label = dismissBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = "Dismiss (Tab)";
        }

        EnsureTimerUi();
        root.SetActive(false);
    }

    void EnsureTimerUi()
    {
        if (timerFill != null || root == null) return;

        if (circleSprite == null)
            circleSprite = CreateCircleSprite(64);

        timerBackground = CreateTimerImage("NotificationTimerBg", root.transform, new Color(0.15f, 0.2f, 0.25f, 0.9f), false);
        timerFill = CreateTimerImage("NotificationTimerFill", root.transform, new Color(0.35f, 0.75f, 0.85f, 1f), true);
        timerFill.fillAmount = 1f;
    }

    static Image CreateTimerImage(string name, Transform parent, Color color, bool filled)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-14f, -14f);
        rt.sizeDelta = new Vector2(42f, 42f);

        var img = go.GetComponent<Image>();
        img.sprite = circleSprite;
        img.color = color;
        img.raycastTarget = false;
        img.preserveAspect = true;

        if (filled)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Top;
            img.fillClockwise = false;
            img.fillAmount = 1f;
        }

        return img;
    }

    static Sprite CreateCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) * 0.5f;
        float radius = center - 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    public void Dismiss()
    {
        // Manual dismiss still ends the current alert; queued ones wait their turn.
        FinishCurrentAndShowNext();
    }

    void ScheduleNext()
    {
        nextAt = Time.time + Random.Range(intervalMin, intervalMax);
    }

    void Update()
    {
        if (isDisplaying && Input.GetKeyDown(KeyCode.Tab))
        {
            Dismiss();
            return;
        }

        if (isDisplaying)
        {
            displayRemaining -= Time.deltaTime;
            if (timerFill != null)
                timerFill.fillAmount = Mathf.Clamp01(displayRemaining / displayDuration);

            if (displayRemaining <= 0f)
                FinishCurrentAndShowNext();
        }

        if (Time.time >= nextAt)
        {
            if (GameManager.Instance != null && GameManager.Instance.isPowerOut)
            {
                nextAt = Time.time + 1f;
                return;
            }

            // Random events wait until the active notification timer finishes.
            if (isDisplaying)
            {
                nextAt = Time.time + 1f;
                return;
            }

            FireRandom();
            ScheduleNext();
        }
    }

    void FinishCurrentAndShowNext()
    {
        isDisplaying = false;
        displayRemaining = 0f;

        if (timerFill != null)
            timerFill.fillAmount = 0f;

        if (root != null)
            root.SetActive(false);

        if (pending.Count > 0)
        {
            var next = pending.Dequeue();
            DisplayNow(next.message, next.heading);
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
                    stats.AddMoney(-150);
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
        if (TutorialMode.IsActive)
            return;

        if (root == null) BindScenePanel();

        if (isDisplaying)
        {
            pending.Enqueue((heading, message));
            return;
        }

        DisplayNow(message, heading);
    }

    void DisplayNow(string message, string heading)
    {
        EnsureTimerUi();

        if (title != null) title.text = heading;
        if (body != null) body.text = message;

        displayRemaining = displayDuration;
        isDisplaying = true;

        if (timerFill != null)
            timerFill.fillAmount = 1f;

        if (root != null)
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("PopUpNontification");
    }
}
