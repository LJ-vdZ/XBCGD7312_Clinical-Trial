using UnityEngine;

/// <summary>
/// Shared Screen Space Overlay canvas for prototype panels.
/// The canvas is authored in HospitalHubLevel under the scene "UI" object
/// (sibling of DoctorUI / NurseUI / JobSelectionOverlayUI). Runtime code only finds it.
/// </summary>
public static class NewUIRoot
{
    public const string RootName = "SystemsUI";
    const string UiObjectName = "UI";

    static Canvas cached;

    public static Transform Transform => Ensure()?.transform;
    public static Canvas Canvas => Ensure();

    public static void ClearCache()
    {
        cached = null;
    }

    public static Canvas Ensure()
    {
        if (cached != null)
            return cached;

        cached = FindCanvas(RootName);
        if (cached != null)
            return cached;

        Debug.LogError(
            "NewUIRoot: scene is missing '" + RootName + "' under UI. Prototype panels must be authored in the Editor, not created at runtime.");
        return null;
    }

    static Canvas FindCanvas(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            var canvas = existing.GetComponent<Canvas>();
            if (canvas != null) return canvas;
        }

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || t.name != name) continue;
            var canvas = t.GetComponent<Canvas>();
            if (canvas != null) return canvas;
        }

        return null;
    }

    public static Transform FindUiParent()
    {
        var ui = GameObject.Find(UiObjectName);
        if (ui != null) return ui.transform;

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == UiObjectName)
                return t;
        }

        return null;
    }
}
