using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoleTimerSystem : MonoBehaviour
{
    public static RoleTimerSystem Instance;

    public float timePerMinute = 60f;

    private float currentTime;
    private bool isTimerRunning;
    private RoleType runningRole;

    private PlayerRoleManager roleManager;
    private PlayerInteractionHandler interactionHandler;

    readonly HashSet<RoleType> exhaustedRoles = new HashSet<RoleType>();
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

    public void StartRoleTimer(RoleType role, int minutes)
    {
        StopAllCoroutines();

        currentTime = minutes * timePerMinute;
        runningRole = role;
        startedRoles.Add(role);

        var interaction = GetActiveInteraction();

        if (minutes <= 0)
        {
            Debug.Log("No time allocated for this role!");
            exhaustedRoles.Add(role);
            if (interaction != null)
                interaction.SetInteractionEnabled(false);
            return;
        }

        Debug.Log($"Starting {role} timer: {minutes} minutes");

        isTimerRunning = true;
        if (interaction != null)
            interaction.SetInteractionEnabled(true);

        StartCoroutine(TimerRoutine(role));
    }

    IEnumerator TimerRoutine(RoleType role)
    {
        while (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            yield return null;
        }

        TimerEnded(role);
    }

    void TimerEnded(RoleType role)
    {
        Debug.Log($"{role} time is up!");
        isTimerRunning = false;
        exhaustedRoles.Add(role);

        var interaction = GetActiveInteraction();
        if (interaction != null)
            interaction.SetInteractionEnabled(false);
    }

    PlayerInteractionHandler GetActiveInteraction()
    {
        if (CharacterSwitchManager.Instance?.ActiveCharacter?.interaction != null)
            return CharacterSwitchManager.Instance.ActiveCharacter.interaction;
        return interactionHandler != null ? interactionHandler : FindAnyObjectByType<PlayerInteractionHandler>();
    }

    public float GetRemainingTime()
    {
        return currentTime;
    }

    public bool IsTimerRunning => isTimerRunning;

    /// <summary>
    /// True once Nurse, Doctor and Janitor have each started and exhausted their timers.
    /// </summary>
    public bool AllRoleTimersExhausted()
    {
        return exhaustedRoles.Contains(RoleType.Nurse)
               && exhaustedRoles.Contains(RoleType.Doctor)
               && exhaustedRoles.Contains(RoleType.Janitor);
    }

    public void MarkRoleExhausted(RoleType role) => exhaustedRoles.Add(role);
}
