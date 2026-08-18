using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Lightweight UI helpers matching the project's flat panel + TMP style.
/// Scene panels live under UI / NewFeatureUICanvas; use Find/Bind at runtime.
/// Create* methods are only for dynamic list rows (redirect patients).
/// </summary>
public static class ClinicalUIFactory
{
    /// <summary>Modest global scale for new prototype UI (panels + default control sizes).</summary>
    public const float UiScale = 1.12f;

    static readonly Color PanelColor = new Color(0.12f, 0.18f, 0.22f, 0.94f);
    static readonly Color ButtonColor = new Color(0.22f, 0.45f, 0.55f, 1f);
    static readonly Color ButtonHover = new Color(0.28f, 0.55f, 0.65f, 1f);
    static readonly Color TextColor = new Color(0.95f, 0.96f, 0.94f, 1f);
    static readonly Color AccentRed = new Color(0.75f, 0.22f, 0.22f, 1f);

    public static Vector2 Scale(Vector2 size) => size * UiScale;

    public static int ScaleFont(int fontSize) => Mathf.RoundToInt(fontSize * 1.22f);

    public static GameObject FindByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        var go = GameObject.Find(name);
        if (go != null) return go;

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.name == name)
                return t.gameObject;
        }

        return null;
    }

    public static Transform FindChild(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;

        var direct = root.Find(name);
        if (direct != null) return direct;

        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && t.name == name)
                return t;
        }

        return null;
    }

    public static TextMeshProUGUI FindLabel(Transform root, string name)
    {
        var t = FindChild(root, name);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    public static Button BindButton(Transform root, string objectName, UnityAction onClick)
    {
        var t = FindChild(root, objectName);
        if (t == null) return null;

        var btn = t.GetComponent<Button>();
        if (btn == null) return null;

        btn.onClick.RemoveAllListeners();
        if (onClick != null)
            btn.onClick.AddListener(onClick);
        if (AudioManager.Instance != null)
            AudioManager.Instance.HookButton(btn);
        return btn;
    }

    public static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Scale(size);
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = PanelColor;
        return go;
    }

    public static TextMeshProUGUI CreateLabel(Transform parent, string text, int fontSize, Vector2 anchoredPos)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Scale(new Vector2(540, 64));
        rt.anchoredPosition = anchoredPos * UiScale;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = ScaleFont(fontSize);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = TextColor;
        return tmp;
    }

    public static Button CreateButton(Transform parent, string label, UnityAction onClick,
        Vector2? anchoredPos = null, Vector2? size = null)
    {
        var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Scale(size ?? new Vector2(380, 52));
        rt.anchoredPosition = (anchoredPos ?? Vector2.zero) * UiScale;

        var img = go.GetComponent<Image>();
        img.color = ButtonColor;

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = ButtonHover;
        colors.pressedColor = ButtonColor * 0.85f;
        btn.colors = colors;
        if (onClick != null) btn.onClick.AddListener(onClick);
        if (AudioManager.Instance != null)
            AudioManager.Instance.HookButton(btn);

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = ScaleFont(26);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = TextColor;

        return btn;
    }

    public static TextMeshProUGUI CreateCornerLabel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, Vector2? size = null, int fontSize = 30)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(anchorMin.x > 0.5f ? 1f : 0f, anchorMin.y > 0.5f ? 1f : 0f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = Scale(size ?? new Vector2(300, 52));
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = ScaleFont(fontSize);
        tmp.color = TextColor;
        tmp.alignment = anchorMin.x > 0.5f ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    public static Color GetAccentRed() => AccentRed;
    public static Color GetPanelColor() => PanelColor;
    public static Color GetTextColor() => TextColor;
    public static Color GetButtonColor() => ButtonColor;
}
