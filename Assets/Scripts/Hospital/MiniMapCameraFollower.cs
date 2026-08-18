using UnityEngine;

/// <summary>
/// Moves the Mini-Map Camera so its X/Z match the active playable character.
/// Y (height) is preserved. Snaps on role switch and follows while moving.
/// </summary>
public class MiniMapCameraFollower : MonoBehaviour
{
    public Transform player;
    public bool followContinuously = true;
    public bool keepOwnHeight = true;

    void OnEnable()
    {
        CharacterSwitchManager.OnCharacterChanged += OnCharacterChanged;
        TryBindActiveCharacter();
    }

    void OnDisable()
    {
        CharacterSwitchManager.OnCharacterChanged -= OnCharacterChanged;
    }

    void Start()
    {
        TryBindActiveCharacter();
        SnapToPlayer();
    }

    void LateUpdate()
    {
        if (!followContinuously) return;
        SnapToPlayer();
    }

    void OnCharacterChanged(PlayableCharacter next)
    {
        if (next == null) return;
        player = next.transform;
        SnapToPlayer();
    }

    void TryBindActiveCharacter()
    {
        if (player != null) return;

        if (CharacterSwitchManager.Instance != null &&
            CharacterSwitchManager.Instance.ActiveCharacter != null)
        {
            player = CharacterSwitchManager.Instance.ActiveCharacter.transform;
        }
    }

    public void SetPlayer(Transform target)
    {
        player = target;
        SnapToPlayer();
    }

    public void SnapToPlayer()
    {
        if (player == null) return;

        Vector3 pos = transform.position;
        pos.x = player.position.x;
        pos.z = player.position.z;
        if (!keepOwnHeight)
            pos.y = player.position.y;
        transform.position = pos;
    }
}
