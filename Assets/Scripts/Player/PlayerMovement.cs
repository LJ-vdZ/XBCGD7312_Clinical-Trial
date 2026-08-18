using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimplePlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] bool rotateOnlyWhenMovingForward = true;


    [Header("Gravity")]
    [SerializeField] float gravity = -20f;

    [Header("References")]
    [SerializeField] Camera cam;

    //[Header("Camera Control")]
    //[SerializeField] bool useInternalCameraCode = true;

    //[SerializeField] float mouseSensitivity = 2f;
    //[SerializeField] float minPitch = -80f;
    //[SerializeField] float maxPitch = 80f;
    //float pitch;

    CharacterController controller;
    Vector3 velocity;

    bool controlsEnabled = true;


    //---------------------------
    private CharacterController cc;
    public float speed = 7f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        //// fallback if not assigned
        ////if (cam == null)
        ////    cam = Camera.main;

        SetControlsEnabled(true);

        //cc = GetComponent<CharacterController>();
        cam = Camera.main;
    }

    void Update()
    {
        if (!controlsEnabled) return;
        

        //if (useInternalCameraCode)
        //    HandleMouseLook();

        // these should always run
        //AlignCharacterToCameraYaw();
        ApplyGravity();
        HandleMovement();
    }

    void HandleMovement()
    {
        //if (cam == null)
        //{
        //    Debug.LogWarning("SimplePlayerMovement: cam is not assigned.");
        //    return;
        //}
        //float h = Input.GetAxisRaw("Horizontal");
        //float v = Input.GetAxisRaw("Vertical");
        //Vector3 input = new Vector3(h, 0f, v);
        //if (input.sqrMagnitude > 1f)
        //    input.Normalize();
        //// Camera-relative basis (flattened)
        //Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        //Vector3 camRight = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
        //Vector3 moveDir = camForward * input.z + camRight * input.x;
        //Vector3 frameMove = moveDir * moveSpeed;
        //frameMove.y = velocity.y;
        //controller.Move(frameMove * Time.deltaTime);

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0, v).normalized;

        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
            Vector3 moveDir = camForward * input.z + camRight * input.x;

            controller.Move(moveDir * speed * Time.deltaTime);

            if (moveDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), rotationSpeed * Time.deltaTime);
            }
        }

        if (!controller.isGrounded) 
        {
            controller.Move(Vector3.down * 25f * Time.deltaTime);
        }
            
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0f) 
        {
            velocity.y = -2f;

        }
            
        velocity.y += gravity * Time.deltaTime;
    }

    
    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;

        if (enabled)
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

    public bool AreControlsEnabled() => controlsEnabled;

    public void SetCamera(Camera newCam)
    {
        if (newCam != null)
            cam = newCam;
    }

    //public void HandleMouseLook()
    //{
    //    if (!useInternalCameraCode) return;

    //    float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
    //    float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
    //    transform.Rotate(0f, mouseX, 0f);
    //    pitch -= mouseY;
    //    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    //    if (cam != null)
    //        cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

    //}

    void AlignCharacterToCameraYaw()
    {
        if (cam == null) return;
        Vector3 flatForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.001f) return;
        Quaternion targetRot = Quaternion.LookRotation(flatForward.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }
}