using UnityEngine;

/// <summary>
/// Optional: rotates this object to face the player on the XZ plane (e.g. mini-map icons).
/// </summary>
public class MiniMapPlayerFollower : MonoBehaviour
{
    public Transform player;

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;
        }
    }
}
