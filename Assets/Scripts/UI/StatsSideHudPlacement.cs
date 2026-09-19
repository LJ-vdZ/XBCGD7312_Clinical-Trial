using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class StatsSideHudPlacement
{
    static readonly Color HudLabelBgColor = new Color(0.12f, 0.18f, 0.22f, 0.88f);

    //find parent transform for hud elements to attach to
    public static Transform GetParent()
    {
        var stats = FindStatsTransform();

        if (stats != null && stats.parent != null)
        {
            return stats.parent;
        }

        return NewUIRoot.Transform;
    }

    //find patient capacity label
    public static TextMeshProUGUI CreatePatientsLabel()
    {
        return FindHudLabel("CapacityLabel");
    }

    //find medicine count label
    public static TextMeshProUGUI CreateMedicineLabel()
    {
        return FindHudLabel("MedicineCountHUD");
    }

    //find HUD label by name. fall back parent search
    static TextMeshProUGUI FindHudLabel(string name)
    {
        var go = ClinicalUIFactory.FindByName(name);

        if (go != null)
        {
            var label = go.GetComponent<TextMeshProUGUI>();
            EnsureLabelBackground(label);
            return label;
        }

        var parent = GetParent();

        var child = ClinicalUIFactory.FindChild(parent, name);

        var found = child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        EnsureLabelBackground(found);
        return found;
    }

    /// <summary>Adds a dark panel sibling behind CapacityLabel / MedicineCountHUD text.</summary>
    static void EnsureLabelBackground(TextMeshProUGUI label)
    {
        if (label == null) return;

        Transform parent = label.transform.parent;
        if (parent == null) return;

        string bgName = label.gameObject.name + "Background";
        Transform existing = parent.Find(bgName);
        if (existing == null)
        {
            var bg = new GameObject(bgName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(parent, false);

            var src = label.rectTransform;
            var rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = src.anchorMin;
            rt.anchorMax = src.anchorMax;
            rt.pivot = src.pivot;
            rt.anchoredPosition = src.anchoredPosition;
            rt.sizeDelta = src.sizeDelta;
            rt.localRotation = src.localRotation;
            rt.localScale = src.localScale;

            var img = bg.GetComponent<Image>();
            img.color = HudLabelBgColor;
            img.raycastTarget = false;

            // Draw behind the label.
            bg.transform.SetSiblingIndex(label.transform.GetSiblingIndex());
        }

        // Keep text readable against the panel edges.
        if (label.margin == Vector4.zero)
            label.margin = new Vector4(14f, 4f, 14f, 4f);
    }

    //locate "Stats" transform in game scene. check if active or not
    static Transform FindStatsTransform()
    {
        var go = GameObject.Find("Stats");

        if (go != null)
        {
            return go.transform;
        }

        //fallback search include inactive objects
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "Stats" && t.GetComponent<RectTransform>() != null)
            {
                return t;
            }
        }

        return null;
    }
}
