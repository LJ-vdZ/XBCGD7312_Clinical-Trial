using UnityEngine;
using System;

public class HospitalStatsManager : MonoBehaviour
{
    public static HospitalStatsManager Instance;

    // Stats
    public float sanitation = 50f;
    public float comfort = 50f;
    public float morale = 50f;
    public int money = 1000;

    // Event for UI updates
    public static Action OnStatsChanged;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // Methods to modify stats
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
        if (money < amount)
        {
            Debug.Log("Not enough money!");
            return false;
        }

        money -= amount;
        OnStatsChanged?.Invoke();
        return true;
    }

    public void AddMoney(int amount)
    {
        money += amount;
        if (money < 0) money = 0;
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// Returns true if any core hospital stat has collapsed to zero.
    /// </summary>
    public bool TryGetCollapsedStat(out string statName)
    {
        if (sanitation <= 0f)
        {
            statName = "Sanitation / Hygiene";
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
        if (money <= 0)
        {
            statName = "Budget";
            return true;
        }

        statName = null;
        return false;
    }
}