using UnityEngine;
using System;

public class HospitalStatsManager : MonoBehaviour
{
    //singleton
    public static HospitalStatsManager Instance;

    //stats
    public float sanitation = 50f;
    public float comfort = 50f;
    public float morale = 50f;
    public int money = 1000;

    [Header("Budget Overdraft")]
    [Tooltip("Exactly this value is allowed. Going below it is an immediate loss.")]
    public int NegativeMoneyLimit = -500;

    [Tooltip("Seconds the hospital may remain below R0 before losing.")]
    public float NegativeMoneyTimeLimit = 300f;

    bool deficitTimerRunning;
    float deficitElapsed;
    bool deficitTimeExpired;
    bool budgetLossTriggered;

    //event for UI updates
    public static Action OnStatsChanged;

    public bool IsDeficitTimerRunning => deficitTimerRunning;

    public bool HasBudgetCollapsed => budgetLossTriggered || deficitTimeExpired || money < NegativeMoneyLimit;

    private void Awake()
    {
        //singleton setup
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
        SyncDeficitTimerWithBalance();
    }

    void Update()
    {
        if (budgetLossTriggered || deficitTimeExpired || !deficitTimerRunning)
        {
            return;
        }

        //stop counting if another hospital stat already collapsed
        if (sanitation <= 0f || comfort <= 0f || morale <= 0f)
        {
            return;
        }

        deficitElapsed += Time.deltaTime;

        if (deficitElapsed >= NegativeMoneyTimeLimit)
        {
            deficitTimeExpired = true;
            deficitTimerRunning = false;
            budgetLossTriggered = true;
            OnStatsChanged?.Invoke();
        }
    }

    //methods to modify stats
    public void ChangeSanitation(float amount)
    {
        sanitation += amount;

        sanitation = Mathf.Clamp(sanitation, 0, 100);

        OnStatsChanged?.Invoke();
    }

    public void ChangeComfort(float amount)
    {
        comfort += amount;

        comfort = Mathf.Clamp(comfort, 0, 100);

        OnStatsChanged?.Invoke();
    }

    public void ChangeMorale(float amount)
    {
        morale += amount;

        morale = Mathf.Clamp(morale, 0, 100);

        OnStatsChanged?.Invoke();
    }

    public bool SpendMoney(int amount)
    {
        money -= amount;

        NotifyMoneyChanged();

        return true;
    }

    public void AddMoney(int amount)
    {
        money += amount;

        NotifyMoneyChanged();
    }

    public static string FormatMoney(int amount)
    {
        if (amount < 0)
        {
            return "-R" + Mathf.Abs(amount);
        }

        return "R" + amount;
    }

    public float GetDeficitTimeRemaining()
    {
        if (!deficitTimerRunning)
        {
            return 0f;
        }

        return Mathf.Max(0f, NegativeMoneyTimeLimit - deficitElapsed);
    }

    void NotifyMoneyChanged()
    {
        if (!budgetLossTriggered && !deficitTimeExpired)
        {
            SyncDeficitTimerWithBalance();
        }

        OnStatsChanged?.Invoke();
    }

    void SyncDeficitTimerWithBalance()
    {
        if (budgetLossTriggered || deficitTimeExpired)
        {
            return;
        }

        if (money < NegativeMoneyLimit)
        {
            deficitTimerRunning = false;
            budgetLossTriggered = true;
            return;
        }

        if (money < 0)
        {
            if (!deficitTimerRunning)
            {
                deficitTimerRunning = true;
                deficitElapsed = 0f;
            }

            return;
        }

        deficitTimerRunning = false;
        deficitElapsed = 0f;
    }

    //return true if any stat reaches zero, or budget overdraft rules fail
    public bool TryGetCollapsedStat(out string statName)
    {
        if (sanitation <= 0f)
        {
            statName = "Sanitation/Hygiene";

            return true;
        }

        if (comfort <= 0f)
        {
            statName = "Comfort";

            return true;
        }

        if (morale <= 0f)
        {
            statName = "Morale";

            return true;
        }

        if (money < NegativeMoneyLimit || deficitTimeExpired)
        {
            statName = "Budget";

            return true;
        }

        statName = null;

        return false;
    }
}