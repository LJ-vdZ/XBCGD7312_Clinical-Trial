using UnityEngine;

/// <summary>
/// Loops the Walking SFX while the active, controlled character is moving.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FootstepAudio : MonoBehaviour
{
    SimplePlayerMovement movement;

    void Awake()
    {
        movement = GetComponent<SimplePlayerMovement>();
    }

    void OnDisable()
    {
        if (IsActiveCharacter() && AudioManager.Instance != null)
            AudioManager.Instance.SetWalking(false);
    }

    void Update()
    {
        if (!IsActiveCharacter())
            return;

        bool walking = false;
        if (movement == null || movement.AreControlsEnabled())
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            walking = h * h + v * v > 0.01f;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetWalking(walking);
    }

    bool IsActiveCharacter()
    {
        if (CharacterSwitchManager.Instance == null)
            return movement == null || movement.AreControlsEnabled();

        var active = CharacterSwitchManager.Instance.ActiveCharacter;
        return active != null && active.gameObject == gameObject;
    }
}
