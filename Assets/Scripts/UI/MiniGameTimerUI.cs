using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGameTimerUI : MonoBehaviour
{
    //singleton
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

    //finds and caches timer UI in game scene
    void BindScenePanel()
    {
        if (root != null)
        {
            return;
        }

        root = ClinicalUIFactory.FindByName("MiniGameTimer");

        if (root == null)
        {
            Debug.LogError("MiniGameTimerUI missing scene object MiniGameTimer");

            return;
        }

        label = ClinicalUIFactory.FindLabel(root.transform, "Label");

        root.SetActive(false);
    }

    //start countdown with relevnat title, duration, and callback
    public void StartTimer(string title, float seconds, Action expired = null)
    {
        if (root == null)
        {
            BindScenePanel();
        }

        remaining = seconds;

        onExpired = expired;

        running = true;

        if (!string.IsNullOrEmpty(title))
        {
            currentTitle = title;
        }

        if (root != null)
        {
            root.SetActive(true);
        }

        UpdateLabel();
    }

    //stop timer and hide panel
    public void StopTimer()
    {
        running = false;

        onExpired = null;

        if (root != null)
        {
            root.SetActive(false);
        }
    }

    void Update()
    {
        if (!running)
        {
            return;
        }

        remaining -= Time.deltaTime;

        UpdateLabel();

        //if timer hits zero, fire callback and stop
        if (remaining <= 0f)
        {
            running = false;

            if (root != null)
            {
                root.SetActive(false);
            }

            onExpired?.Invoke();

            onExpired = null;
        }
    }

    //refresh countdown label text
    void UpdateLabel()
    {
        if (label != null) 
        {
            label.text = $"{currentTitle}  {Mathf.CeilToInt(Mathf.Max(0, remaining))}s";
        }
            
    }
}
