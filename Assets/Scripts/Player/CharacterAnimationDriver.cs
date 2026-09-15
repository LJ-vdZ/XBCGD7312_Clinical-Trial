using UnityEngine;

/// <summary>
/// Drives playable-character Animator states from movement / janitor actions.
/// Controllers already contain the clips; this hooks Idle ↔ Walk (+ janitor variants).
/// </summary>
[RequireComponent(typeof(PlayableCharacter))]
public class CharacterAnimationDriver : MonoBehaviour
{
    [SerializeField] float crossFade = 0.12f;
    [SerializeField] float sprintAnimSpeed = 1.45f;
    [SerializeField] float sweepDuration = 1.35f;
    [SerializeField] float pickupFallbackDuration = 1.1f;

    Animator animator;
    SimplePlayerMovement movement;
    PlayableCharacter playable;
    JanitorAbilities janitor;

    string idleState;
    string walkState;
    string currentState;
    bool playingPickup;
    float pickupEndsAt;
    float sweepEndsAt;

    void Awake()
    {
        playable = GetComponent<PlayableCharacter>();
        movement = GetComponent<SimplePlayerMovement>();
        janitor = GetComponent<JanitorAbilities>();
        animator = GetComponentInChildren<Animator>(true);
        ResolveStateNames();
    }

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        ResolveStateNames();
        PlayState(idleState, 0f);
    }

    void ResolveStateNames()
    {
        RoleType role = playable != null ? playable.role : RoleType.Manager;
        switch (role)
        {
            case RoleType.Doctor:
                idleState = "DoctorIdle";
                walkState = "DoctorWalk";
                break;
            case RoleType.Nurse:
                idleState = "NurseIdle";
                walkState = "NurseWalk";
                break;
            case RoleType.Janitor:
                idleState = "JanitorIdle_anim";
                walkState = "JanitorWalk_anim";
                break;
            default:
                idleState = "ManagerIdle";
                walkState = "ManagerWalk";
                break;
        }
    }

    void Update()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (movement == null)
            movement = GetComponent<SimplePlayerMovement>();
        if (janitor == null)
            janitor = GetComponent<JanitorAbilities>();

        if (animator == null || !animator.isActiveAndEnabled)
            return;

        bool moving = movement != null && movement.AreControlsEnabled() && movement.IsMoving;
        bool sprinting = moving && movement != null && movement.IsSprinting;

        string desired = ResolveDesiredState(moving);

        // One-shot pickup: hold until clip finishes, then fall through to hold/walk.
        if (playingPickup)
        {
            if (janitor == null || janitor.heldTrash == null)
            {
                playingPickup = false;
                desired = ResolveDesiredState(moving);
            }
            else if (Time.time < pickupEndsAt && !HasPickupFinished())
            {
                desired = "JanitorPickup_anim";
            }
            else
            {
                playingPickup = false;
                desired = ResolveDesiredState(moving);
            }
        }

        if (!string.IsNullOrEmpty(desired))
            PlayState(desired, crossFade);

        bool locomotion = desired == walkState
            || desired == "JanitorPickupWalk_anim"
            || desired == "JanitorPushCart_anim";
        animator.speed = (locomotion && sprinting) ? sprintAnimSpeed : 1f;
    }

    string ResolveDesiredState(bool moving)
    {
        if (playable != null && playable.role == RoleType.Janitor)
            return ResolveJanitorState(moving);

        return moving ? walkState : idleState;
    }

    string ResolveJanitorState(bool moving)
    {
        if (Time.time < sweepEndsAt)
            return "JanitorSweeping";

        bool holdingTrash = janitor != null && janitor.heldTrash != null;
        bool carryingCart = false;
        if (JanitorCartController.Instance != null && JanitorCartController.Instance.isCarried)
        {
            var cart = JanitorCartController.Instance;
            carryingCart = cart.attachToPlayer == transform
                || cart.transform.IsChildOf(transform);
        }

        if (carryingCart)
            return moving ? "JanitorPushCart_anim" : "JanitorCartIdle";

        if (holdingTrash)
            return moving ? "JanitorPickupWalk_anim" : "JanitorPickupIdle";

        return moving ? walkState : idleState;
    }

    public void NotifyTrashPickedUp()
    {
        if (playable == null || playable.role != RoleType.Janitor)
            return;
        if (animator == null)
            return;

        playingPickup = true;
        pickupEndsAt = Time.time + GetClipLength("JanitorPickup_anim", pickupFallbackDuration);
        PlayState("JanitorPickup_anim", 0.05f);
    }

    public void NotifySweeping()
    {
        if (playable == null || playable.role != RoleType.Janitor)
            return;

        float duration = GetClipLength("JanitorSweeping", sweepDuration);
        sweepEndsAt = Time.time + duration;
        playingPickup = false;
        PlayState("JanitorSweeping", 0.05f);
    }

    public void NotifyCartAttached()
    {
        if (playable == null || playable.role != RoleType.Janitor)
            return;
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (animator == null)
            return;

        playingPickup = false;
        sweepEndsAt = 0f;

        bool moving = movement != null && movement.AreControlsEnabled() && movement.IsMoving;
        PlayState(moving ? "JanitorPushCart_anim" : "JanitorCartIdle", 0.05f);
    }

    bool HasPickupFinished()
    {
        if (animator == null) return true;
        var info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName("JanitorPickup_anim"))
            return Time.time >= pickupEndsAt;
        return info.normalizedTime >= 0.95f && !animator.IsInTransition(0);
    }

    float GetClipLength(string stateName, float fallback)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return fallback;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name.IndexOf(stateName.Replace("_anim", ""), System.StringComparison.OrdinalIgnoreCase) >= 0)
                return Mathf.Max(0.15f, clip.length);
        }

        // Exact / partial match by common naming
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip == null) continue;
            if (stateName.Contains("Pickup") && clip.name.IndexOf("Pickup", System.StringComparison.OrdinalIgnoreCase) >= 0
                && clip.name.IndexOf("Idle", System.StringComparison.OrdinalIgnoreCase) < 0
                && clip.name.IndexOf("Walk", System.StringComparison.OrdinalIgnoreCase) < 0)
                return Mathf.Max(0.15f, clip.length);
            if (stateName.Contains("Sweep") && clip.name.IndexOf("Sweep", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return Mathf.Max(0.15f, clip.length);
        }

        return fallback;
    }

    void PlayState(string stateName, float fade)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
            return;
        if (currentState == stateName && fade > 0f)
            return;

        if (fade <= 0f)
            animator.Play(stateName, 0, 0f);
        else
            animator.CrossFadeInFixedTime(stateName, fade, 0);

        currentState = stateName;
    }

    bool IsActiveCharacter()
    {
        if (CharacterSwitchManager.Instance == null)
            return movement == null || movement.AreControlsEnabled();
        var active = CharacterSwitchManager.Instance.ActiveCharacter;
        return active != null && active.gameObject == gameObject;
    }
}
