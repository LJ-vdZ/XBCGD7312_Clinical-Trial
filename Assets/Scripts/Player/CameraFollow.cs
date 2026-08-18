using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Distance Settings")]
    public float distance = 6f;
    public float heightOffset = 2.2f;

    [Header("Rotation Settings")]
    public float mouseSensitivity = 3f;
    public float minPitch = -30f;
    public float maxPitch = 70f;

    [Header("Smoothing")]
    public float smoothSpeed = 10f;

    [Header("Collision")]
    [Tooltip("Pull the camera this far off walls/floors along the look ray.")]
    public float collisionPadding = 0.2f;
    public float minCameraDistance = 0.4f;

    float yaw = 0f;
    float pitch = 25f;

    // Reused to avoid GC from RaycastAll each frame.
    static readonly RaycastHit[] hitBuffer = new RaycastHit[24];

    void Start()
    {
        if (target != null)
            yaw = target.eulerAngles.y;
        LockCursor(true);
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleMouseLook();
        HandleCameraPosition();
    }

    void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void HandleCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + Vector3.up * heightOffset;
        Vector3 desiredPosition = pivot + rotation * (Vector3.back * distance);

        Vector3 dir = desiredPosition - pivot;
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
                if (!IsWallOrFloorCollider(hit.collider))
                    continue;
                if (hit.distance < bestDist)
                    bestDist = hit.distance;
            }

            if (bestDist < castDist)
            {
                float safeDist = Mathf.Max(minCameraDistance, bestDist - collisionPadding);
                desiredPosition = pivot + dir.normalized * safeDist;
            }
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.8f);
    }

    /// <summary>
    /// Only structural hospital walls/floors (OVector Wall_* / Floor_* pieces).
    /// Skips props, shelves, lights, light-ray triggers, etc.
    /// </summary>
    public static bool IsWallOrFloorCollider(Collider col)
    {
        if (col == null || !col.enabled || col.isTrigger)
            return false;

        Transform t = col.transform;
        while (t != null)
        {
            if (IsWallOrFloorObjectName(t.name))
                return true;
            t = t.parent;
        }
        return false;
    }

    public static bool IsWallOrFloorObjectName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return false;

        // Explicit exclusions that also start with "Wall" / contain floor-like words.
        if (ContainsIgnoreCase(objectName, "Light")) return false;
        if (ContainsIgnoreCase(objectName, "Ray")) return false;
        if (ContainsIgnoreCase(objectName, "Shelf")) return false;
        if (ContainsIgnoreCase(objectName, "Painting")) return false;
        if (ContainsIgnoreCase(objectName, "Prop")) return false;
        if (ContainsIgnoreCase(objectName, "Trigger")) return false;
        if (ContainsIgnoreCase(objectName, "Decal")) return false;

        if (StartsWithIgnoreCase(objectName, "Wall")) return true;
        if (StartsWithIgnoreCase(objectName, "Floor")) return true;

        // Ceiling geometry only — not "Ceiling Lights".
        if (StartsWithIgnoreCase(objectName, "Ceiling")) return true;

        return false;
    }

    static bool StartsWithIgnoreCase(string value, string prefix) =>
        value.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase);

    static bool ContainsIgnoreCase(string value, string token) =>
        value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;

    public void LockCursor(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
            yaw = target.eulerAngles.y;
    }
}
