using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared countdown overlay for role mini-games.
/// Scene object: MiniGameTimer under NewFeatureUICanvas.
/// </summary>
public class MiniGameTimerUI : MonoBehaviour
{
    public static MiniGameTimerUI Instance;

    TextMeshProUGUI label;
    float remaining;
    bool running;
    Action onExpired;
    GameObject root;
    string currentTitle = "Mini-Game";

    void Awake()
    {
        Instance = this;
        BindScenePanel();
    }

    void BindScenePanel()
    {
        if (root != null) return;

        root = ClinicalUIFactory.FindByName("MiniGameTimer");
        if (root == null)
        {
            Debug.LogError("MiniGameTimerUI: missing scene object 'MiniGameTimer'.");
            return;
        }

        label = ClinicalUIFactory.FindLabel(root.transform, "Label");
        root.SetActive(false);
    }

    public void StartTimer(string title, float seconds, Action expired = null)
    {
        if (root == null) BindScenePanel();
        remaining = seconds;
        onExpired = expired;
        running = true;
        if (!string.IsNullOrEmpty(title)) currentTitle = title;
        if (root != null) root.SetActive(true);
        UpdateLabel();
    }

    public void StopTimer()
    {
        running = false;
        onExpired = null;
        if (root != null) root.SetActive(false);
    }

    void Update()
    {
        if (!running) return;
        remaining -= Time.deltaTime;
        UpdateLabel();
        if (remaining <= 0f)
        {
            running = false;
            if (root != null) root.SetActive(false);
            onExpired?.Invoke();
            onExpired = null;
        }
    }

    void UpdateLabel()
    {
        if (label != null)
            label.text = $"{currentTitle}  {Mathf.CeilToInt(Mathf.Max(0, remaining))}s";
    }
}
