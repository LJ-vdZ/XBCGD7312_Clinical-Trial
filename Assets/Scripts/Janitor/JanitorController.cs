using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class JanitorController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 7f;
    public float rotationSpeed = 15f;

    [Header("Interaction")]
    public float interactRange = 6f;

    [Header("Holding")]
    public Transform attachPoint;

    public TrashItem heldTrash = null;
    public bool isControlled = false;

    //private IJanitorInteractable currentTarget = null;

    private CharacterController cc;
    private Camera cam;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cam = Camera.main;
    }

    void Update()
    {
        if (!isControlled) return;

        HandleMovement();

        ////E is general interaction (pick up, clean, repair, generator puzzle)
        //if (Input.GetKeyDown(KeyCode.E))
        //{
        //    if (currentTarget != null)
        //        currentTarget.Interact(this);
        //}

        ////Q is to dispose trash into bin (only when holding something)
        //if (Input.GetKeyDown(KeyCode.Q))
        //{
        //    if (heldTrash != null && currentTarget != null)
        //    {
        //        currentTarget.Interact(this);   //call DisposeTrash on the bin
        //    }
        //    else if (heldTrash != null)
        //    {
        //        DropTrash();                    //drop on ground if no bin
        //    }
        //}
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0, v).normalized;

        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
            Vector3 moveDir = camForward * input.z + camRight * input.x;

            cc.Move(moveDir * speed * Time.deltaTime);

            if (moveDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(moveDir), rotationSpeed * Time.deltaTime);
            }
        }

        if (!cc.isGrounded)
            cc.Move(Vector3.down * 25f * Time.deltaTime);
    }

    //public void SetCurrentTarget(IJanitorInteractable target)
    //{
    //    currentTarget = target;
    //}

    //public void ClearCurrentTarget()
    //{
    //    currentTarget = null;
    //}

    ////Trash interactions
    //public void PickUpTrash(TrashItem trash)
    //{
    //    if (heldTrash != null) return;
    //    heldTrash = trash;
    //    trash.transform.SetParent(attachPoint);
    //    trash.transform.localPosition = Vector3.zero;
    //    trash.transform.localRotation = Quaternion.identity;
    //    Debug.Log($"Picked up {trash.type}");
    //}

    //public void DisposeTrash(TrashType requiredType)
    //{
    //    if (heldTrash == null) return;

    //    if (heldTrash.type == requiredType)
    //        GameManager.Instance.ModifyHygiene(12f);
    //    else
    //        GameManager.Instance.ModifyHygiene(-20f);

    //    Destroy(heldTrash.gameObject);
    //    heldTrash = null;
    //    Debug.Log("Trash disposed successfully");
    //}

    //private void DropTrash()
    //{
    //    if (heldTrash == null) return;
    //    heldTrash.transform.SetParent(null);
    //    heldTrash.transform.position = transform.position + transform.forward * 1.5f;
    //    heldTrash = null;
    //}

    // Paste old PlaySweepAnimation and SweepPendulum coroutine here if you want it
    // For now you can call it from DirtPile later
}
