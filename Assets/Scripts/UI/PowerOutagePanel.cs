using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dedicated rectangular power-outage alert in the upper-right, toward screen center.
/// Separate from the side NotificationSidePanel queue.
/// </summary>
public class PowerOutagePanel : MonoBehaviour
{
    public static PowerOutagePanel Instance;

    GameObject root;
    TextMeshProUGUI title;
    TextMeshProUGUI body;
    float hideRestoredAt = -1f;

    void Awake() => Instance = this;

    void Start()
    {
        EnsurePanel();
        if (root != null)
            root.SetActive(false);
    }

    void Update()
    {
        if (hideRestoredAt > 0f && Time.time >= hideRestoredAt)
        {
            hideRestoredAt = -1f;
            if (root != null)
                root.SetActive(false);
        }
    }

    void EnsurePanel()
    {
        if (root != null) return;

        var parent = NewUIRoot.Transform;
        if (parent == null)
        {
            Debug.LogError("PowerOutagePanel: SystemsUI canvas missing.");
            return;
        }

        var existing = ClinicalUIFactory.FindByName("PowerOutagePanel");
        if (existing != null)
        {
            root = existing;
            title = ClinicalUIFactory.FindLabel(root.transform, "TitleLabel");
            body = ClinicalUIFactory.FindLabel(root.transform, "BodyLabel");
            ApplyPanelPlacement(root.GetComponent<RectTransform>());
            return;
        }

        root = ClinicalUIFactory.CreatePanel(parent, "PowerOutagePanel", new Vector2(440f, 150f));
        ApplyPanelPlacement(root.GetComponent<RectTransform>());

        title = ClinicalUIFactory.CreateLabel(root.transform, "Power Outage", 28, new Vector2(0f, 42f));
        title.gameObject.name = "TitleLabel";
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;

        body = ClinicalUIFactory.CreateLabel(
            root.transform,
            "",
            20,
            new Vector2(0f, -12f));
        body.gameObject.name = "BodyLabel";
        body.alignment = TextAlignmentOptions.Center;
        body.enableWordWrapping = true;
        body.rectTransform.sizeDelta = ClinicalUIFactory.Scale(new Vector2(400f, 90f));
    }

    static void ApplyPanelPlacement(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        // Sit just left of the right-side tablet / mini-map UI.
        rt.anchoredPosition = ClinicalUIFactory.Scale(new Vector2(-280f, -36f));
    }

    public void ShowOutage(int secondsRemaining)
    {
        EnsurePanel();
        hideRestoredAt = -1f;

        if (root != null)
            ApplyPanelPlacement(root.GetComponent<RectTransform>());

        if (title != null) title.text = "Power Outage";
        if (body != null) body.text = OutageBody(secondsRemaining);

        if (root != null)
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("PopUpNontification");
    }

    public void UpdateCountdown(int secondsRemaining)
    {
        if (root == null || !root.activeSelf) return;
        if (title == null || title.text != "Power Outage") return;
        if (body != null) body.text = OutageBody(secondsRemaining);
    }

    public void ShowRestored()
    {
        EnsurePanel();

        if (title != null) title.text = "Power Restored";
        if (body != null)
        {
            body.text = "Power has been restored. Hospital stats have returned to their normal pace.";
        }

        if (root != null)
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }

        hideRestoredAt = Time.time + 5f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("PopUpNontification");
    }

    static string OutageBody(int secondsRemaining)
    {
        return "Hygiene, comfort and morale are dropping faster.\n" +
               $"Power returns in {Mathf.Max(0, secondsRemaining)}s.";
    }
}
