using TMPro;
using UnityEngine;

public static class StatsSideHudPlacement
{
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
            return go.GetComponent<TextMeshProUGUI>();
        }

        var parent = GetParent();

        var child = ClinicalUIFactory.FindChild(parent, name);

        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
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
