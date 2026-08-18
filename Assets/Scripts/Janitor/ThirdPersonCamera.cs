using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour  //rpg third person camera vibe
{
    public Transform target;                  //player object 
    public float distance = 6f;
    public float heightOffset = 2.2f;         //height above player center
    public float mouseSensitivity = 3f;
    public float collisionPadding = 0.2f;
    public float minCameraDistance = 0.4f;

    private float yaw = 0f;
    private float pitch = 20f;                //start slightly tilted down

    static readonly RaycastHit[] hitBuffer = new RaycastHit[24];

    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    void LateUpdate()
    {
        if (target == null) return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -30f, 70f);

        Quaternion camRotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 pivot = target.position + Vector3.up * heightOffset;
        Vector3 desiredPos = pivot - camRotation * Vector3.forward * distance;

        Vector3 dir = desiredPos - pivot;
        float castDist = dir.magnitude;
        if (castDist > 0.001f)
        {
            int hitCount = Physics.RaycastNonAlloc(
                pivot,
                dir.normalized,
                hitBuffer,
                castDist,
                ~0,
                QueryTriggerInteraction.Ignore);

            float bestDist = castDist;
            for (int i = 0; i < hitCount; i++)
            {
                var hit = hitBuffer[i];
                if (!CameraFollow.IsWallOrFloorCollider(hit.collider))
                    continue;
                if (hit.distance < bestDist)
                    bestDist = hit.distance;
            }

            if (bestDist < castDist)
            {
                float safeDist = Mathf.Max(minCameraDistance, bestDist - collisionPadding);
                desiredPos = pivot + dir.normalized * safeDist;
            }
        }

        transform.position = desiredPos;
        transform.LookAt(target.position + Vector3.up * 1.8f);
    }
}
