using UnityEngine;
using System;

public class PlayerRoleManager : MonoBehaviour
{
    public static PlayerRoleManager ActiveInstance { get; private set; }

    public RoleTimeManager roleTimeManager;
    public RoleType CurrentRole { get; private set; }

    [SerializeField] 
    RoleType fixedRole = RoleType.Manager;

    [SerializeField] 
    bool useFixedRole;

    public static Action<RoleType> OnRoleChanged;

    void OnEnable()
    {
        ActiveInstance = this;
    }

    void Start()
    {
        //multi-caharacter control owned by CharacterSwitchManager
        if (FindFirstObjectByType<CharacterSwitchManager>() != null)
        {
            if (useFixedRole) 
            {
                CurrentRole = fixedRole;
            }
                
            return;
        }

        if (useFixedRole) 
        {
            ApplyRoleWithoutTimerReset(fixedRole);
        }
        else 
        {
            SwitchRole(RoleType.Manager);
        }
            
    }

    public void SetFixedRole(RoleType role)
    {
        fixedRole = role;

        useFixedRole = true;

        CurrentRole = role;
    }

    //used by character object switching
    //updates role event without restarting timers
    public void ApplyRoleWithoutTimerReset(RoleType newRole)
    {
        ActiveInstance = this;

        CurrentRole = newRole;

        OnRoleChanged?.Invoke(CurrentRole);
    }

    public void SwitchRole(RoleType newRole)
    {
        CurrentRole = newRole;

        ActiveInstance = this;

        Debug.Log("Switched to: " + CurrentRole);

        OnRoleChanged?.Invoke(CurrentRole);

        var interaction = GetComponent<PlayerInteractionHandler>();

        if (interaction == null) 
        {
            interaction = FindAnyObjectByType<PlayerInteractionHandler>();
        }
            

        if (newRole == RoleType.Manager)
        {
            if (interaction != null) 
            {
                interaction.SetInteractionEnabled(true);
            }
                
            return;
        }

        if (roleTimeManager == null) 
        {
            roleTimeManager = FindFirstObjectByType<RoleTimeManager>();
        }
            

        int minutes = GetMinutesForRole(newRole);

        if (RoleTimerSystem.Instance != null) 
        {
            RoleTimerSystem.Instance.StartRoleTimer(newRole, minutes);
        }
            
    }

    int GetMinutesForRole(RoleType role)
    {
        if (roleTimeManager == null)
        {
            return 0;
        }

        switch (role)
        {
            case RoleType.Nurse:
                return roleTimeManager.nurseMinutes;
            case RoleType.Doctor:
                return roleTimeManager.doctorMinutes;
            case RoleType.Janitor:
                return roleTimeManager.janitorMinutes;
            default:
                return 0;
        }
    }
}
