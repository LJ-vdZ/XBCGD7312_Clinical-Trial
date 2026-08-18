using TMPro;
using UnityEngine;

/// <summary>
/// Resolves medicine / patient HUD labels authored under MainScreenOverlayCanvas
/// (left side, below the scene Stats panel).
/// </summary>
public static class StatsSideHudPlacement
{
    public static Transform GetParent()
    {
        var stats = FindStatsTransform();
        if (stats != null && stats.parent != null)
            return stats.parent;

        return NewUIRoot.Transform;
    }

    public static TextMeshProUGUI CreatePatientsLabel()
    {
        return FindHudLabel("CapacityLabel");
    }

    public static TextMeshProUGUI CreateMedicineLabel()
    {
        return FindHudLabel("MedicineCountHUD");
    }

    static TextMeshProUGUI FindHudLabel(string name)
    {
        var go = ClinicalUIFactory.FindByName(name);
        if (go != null)
            return go.GetComponent<TextMeshProUGUI>();

        var parent = GetParent();
        var child = ClinicalUIFactory.FindChild(parent, name);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }

    static Transform FindStatsTransform()
    {
        var go = GameObject.Find("Stats");
        if (go != null) return go.transform;

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "Stats" && t.GetComponent<RectTransform>() != null)
                return t;
        }
        return null;
    }
}
