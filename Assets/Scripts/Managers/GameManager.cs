using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private PlayerRoleManager roleManager;

    [Header("Role Timer")]
    public float roleTimeLimit = 300f; //5 minutes
    private float currentRoleTime;
    public bool canPerformTasks = true;

    [Header("Power Outage Settings")]
    public bool isPowerOut = false;

    public float timeUntilFirstOutage = 70f;
    public float outageMaxTime = 15f;

    private float currentOutageTime;
    int lastNotifiedSeconds = -1;

    [Header("UI")]
    public TextMeshProUGUI outageTimerText;

    [Header("Lights")]
    public Light[] hospitalLights;

    void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }
            
    }

    void Start()
    {
        roleManager = FindFirstObjectByType<PlayerRoleManager>();

        PlayerRoleManager.OnRoleChanged += HandleRoleChanged;

        ResetRoleTimer();

        ScheduleNextOutage();
    }

    void OnDestroy()
    {
        PlayerRoleManager.OnRoleChanged -= HandleRoleChanged;
    }

    void Update()
    {
        HandleRoleTimer();
        HandlePowerOutage();
    }


    //role timer system

    void HandleRoleTimer()
    {
        if (!canPerformTasks)
        {
            return;
        }

        currentRoleTime -= Time.deltaTime;

        if (currentRoleTime <= 0f)
        {
            currentRoleTime = 0f;

            canPerformTasks = false;

            Debug.Log("Time zero. Change roles");
        }
    }

    public void ResetRoleTimer()
    {
        currentRoleTime = roleTimeLimit;

        canPerformTasks = true;

        Debug.Log("Role timer reset");
    }

    public float GetRemainingRoleTime()
    {
        return currentRoleTime;
    }


    //role change handling

    void HandleRoleChanged(RoleType newRole)
    {
        Debug.Log("Role changed to: " + newRole);
    }

    //power outage system

    void ScheduleNextOutage()
    {
        CancelInvoke(nameof(StartPowerOutage));
        Invoke(nameof(StartPowerOutage), timeUntilFirstOutage);
        Debug.Log($"Outage scheduled in {timeUntilFirstOutage}s...");
    }

    void StartPowerOutage()
    {
        if (isPowerOut)
        {
            return;
        }

        isPowerOut = true;

        currentOutageTime = 0f;

        lastNotifiedSeconds = -1;

        foreach (var light in hospitalLights)
        {
            if (light != null) 
            {
                light.enabled = false;
            }
        }

        int secs = Mathf.CeilToInt(outageMaxTime);

        if (NotificationSidePanel.Instance != null) 
        {
            NotificationSidePanel.Instance.ShowPowerOutage(secs);
        }
            

        Debug.Log("Power outage started");
    }

    void HandlePowerOutage()
    {
        if (!isPowerOut)
        {
            if (outageTimerText != null) 
            {
                outageTimerText.gameObject.SetActive(false);
            }
                

            return;
        }

        currentOutageTime += Time.deltaTime;

        //outage drain works same as when it was part of puzzle. returns to normal stat decrease when power returns.
        if (HospitalStatsManager.Instance != null)
        {
            HospitalStatsManager.Instance.ChangeSanitation(-1.0f * Time.deltaTime);
            HospitalStatsManager.Instance.ChangeComfort(-1.3f * Time.deltaTime);
            HospitalStatsManager.Instance.ChangeMorale(-1.8f * Time.deltaTime);
        }

        float timeLeft = Mathf.Max(0, outageMaxTime - currentOutageTime);

        int secsLeft = Mathf.CeilToInt(timeLeft);

        if (outageTimerText != null)
        {
            outageTimerText.text = $"Power outage\n{secsLeft}s";

            outageTimerText.gameObject.SetActive(true);
        }

        if (secsLeft != lastNotifiedSeconds)
        {
            lastNotifiedSeconds = secsLeft;

            if (NotificationSidePanel.Instance != null) 
            {
                NotificationSidePanel.Instance.UpdatePowerOutageCountdown(secsLeft);
            }
                
        }

        if (currentOutageTime >= outageMaxTime) 
        {
            EndPowerOutage();
        }
            
    }

    public void EndPowerOutage()
    {
        if (!isPowerOut)
        {
            return;
        }

        isPowerOut = false;
        lastNotifiedSeconds = -1;

        foreach (var light in hospitalLights)
        {
            if (light != null) 
            {
                light.enabled = true;
            }
                
        }

        if (outageTimerText != null) 
        {
            outageTimerText.gameObject.SetActive(false);
        }
            

        if (NotificationSidePanel.Instance != null) 
        {
            NotificationSidePanel.Instance.ShowPowerRestored();
        }
            

        Debug.Log("Power restored");

        ScheduleNextOutage();
    }
}
