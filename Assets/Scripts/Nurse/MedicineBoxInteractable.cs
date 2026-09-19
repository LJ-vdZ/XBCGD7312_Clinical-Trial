using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Nurse-only: unpack purchased medicine box. Plays open anim, then fills MedRow slots.
/// Does not start the organizer mini-game (that begins at MedicineGame_Rack).
/// </summary>
public class MedicineBoxInteractable : MonoBehaviour, IInteractable
{
    public static Action OnBoxUnpacked;

    public bool CanInteractWhenLocked => false;

    bool unpacked;
    bool unpacking;

    void Awake()
    {
        // Ensure open clip does not run until Nurse presses E.
        foreach (var anim in GetComponentsInChildren<Animator>(true))
        {
            anim.enabled = false;
            anim.speed = 0f;
        }
    }

    public void Interact(GameObject player, RoleType role)
    {
        if (role != RoleType.Nurse)
        {
            Debug.Log("Only the Nurse can unpack medicine boxes.");
            return;
        }

        if (unpacked || unpacking) return;
        unpacking = true;

        StartCoroutine(UnpackRoutine());
    }

    IEnumerator UnpackRoutine()
    {
        Animator anim = FindOpenAnimator();
        if (anim != null)
        {
            anim.enabled = true;
            anim.speed = 1f;
            anim.Play(0, 0, 0f);
            anim.Update(0f);

            if (AudioManager.Instance != null)
                AudioManager.Instance.Play("CardboardBox");

            // Wait until the current clip finishes (controller has a single open state).
            yield return null;
            float timeout = 12f;
            float elapsed = 0f;
            while (elapsed < timeout)
            {
                var info = anim.GetCurrentAnimatorStateInfo(0);
                if (info.length > 0f && info.normalizedTime >= 1f && !anim.IsInTransition(0))
                    break;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            // No animator — short beat so spawn still feels sequential.
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play("CardboardBox");
            yield return new WaitForSeconds(0.35f);
        }

        if (MedicineSupplyManager.Instance != null)
            MedicineSupplyManager.Instance.SpawnMedicinesOnShelfSlots();

        unpacked = true;
        unpacking = false;
        OnBoxUnpacked?.Invoke();

        // Box is spent — stop further interaction.
        var trigger = GetComponent<InteractableTrigger>();
        if (trigger != null) trigger.enabled = false;
        foreach (var col in GetComponents<Collider>())
        {
            if (col != null && col.isTrigger)
                col.enabled = false;
        }
    }

    Animator FindOpenAnimator()
    {
        // Prefer the MedicineBox_v1 controller (root), skip unrelated child controllers.
        Animator[] anims = GetComponentsInChildren<Animator>(true);
        foreach (var a in anims)
        {
            if (a == null || a.runtimeAnimatorController == null) continue;
            if (a.runtimeAnimatorController.name.IndexOf("MedicineBox", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return a;
        }
        return anims.Length > 0 ? anims[0] : null;
    }
}
