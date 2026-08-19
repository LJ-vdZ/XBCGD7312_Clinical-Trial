using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoleTimerSystem : MonoBehaviour
{
    //singleton instance
    public static RoleTimerSystem Instance;

    public float timePerMinute = 60f;

    private float currentTime;

    private bool isTimerRunning;

    private RoleType runningRole;

    private PlayerRoleManager roleManager;

    private PlayerInteractionHandler interactionHandler;

    //roles that have used all their allotted time
    readonly HashSet<RoleType> exhaustedRoles = new HashSet<RoleType>();

    //roles that have had their timer started atleast once
    readonly HashSet<RoleType> startedRoles = new HashSet<RoleType>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        roleManager = FindAnyObjectByType<PlayerRoleManager>();

        interactionHandler = FindAnyObjectByType<PlayerInteractionHandler>();
    }

    //start countdown timer for given role shift when interacted with relevant prop
    public void StartRoleTimer(RoleType role, int minutes)
    {
        StopAllCoroutines();

        currentTime = minutes * timePerMinute;

        runningRole = role;

        startedRoles.Add(role);

        var interaction = GetActiveInteraction();

        //time not given or ran out, shift over
        if (minutes <= 0)
        {
            Debug.Log("No time allocated for this role");

            exhaustedRoles.Add(role);

            if (interaction != null) 
            {
                interaction.SetInteractionEnabled(false);
            }
                
            return;
        }

        Debug.Log($"Starting {role} timer: {minutes} minutes");

        isTimerRunning = true;

        if (interaction != null) 
        {
            interaction.SetInteractionEnabled(true);
        }
            

        StartCoroutine(TimerRoutine(role));
    }

    //enumerator counts down until time runs out. End the timer
    IEnumerator TimerRoutine(RoleType role)
    {
        while (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            yield return null;
        }

        TimerEnded(role);
    }

    //Shift ended, disable interaction
    void TimerEnded(RoleType role)
    {
        Debug.Log($"{role} time is up");

        isTimerRunning = false;

        exhaustedRoles.Add(role);

        var interaction = GetActiveInteraction();

        if (interaction != null) 
        {
            interaction.SetInteractionEnabled(false);
        }
            
    }

    //find interaction handler for which character is active
    PlayerInteractionHandler GetActiveInteraction()
    {
        if (CharacterSwitchManager.Instance?.ActiveCharacter?.interaction != null) 
        {
            return CharacterSwitchManager.Instance.ActiveCharacter.interaction;
        }
            
        return interactionHandler != null ? interactionHandler : FindAnyObjectByType<PlayerInteractionHandler>();
    }

    public float GetRemainingTime()
    {
        return currentTime;
    }

    public bool IsTimerRunning => isTimerRunning;

    //characters timers/shifts reached zero
    public bool AllRoleTimersExhausted()
    {
        return exhaustedRoles.Contains(RoleType.Nurse) && exhaustedRoles.Contains(RoleType.Doctor) && exhaustedRoles.Contains(RoleType.Janitor);
    }

    public void MarkRoleExhausted(RoleType role) => exhaustedRoles.Add(role);
}
