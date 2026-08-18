using UnityEngine;

public class PushObjects : MonoBehaviour
{
    [Tooltip("How strong the player can push objects")]
    public float pushPower = 4.0f;

    [Tooltip("Minimum Y direction to apply push. prevent pushing objects below)")]
    public float minimumPushY = -0.3f;

    private CharacterController controller;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        // No rigidbody or kinematic object
        if (body == null || body.isKinematic)
            return;

        // Don't push objects below player like ground
        if (hit.moveDirection.y < minimumPushY)
            return;

        // Calculate push direction horixontally 
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        // Apply force
        body.AddForce(pushDir * pushPower, ForceMode.Force);
    }
}
