using System;
using UnityEngine;

public class JanitorAbilities : MonoBehaviour
{
    public static Action<TrashItem> OnTrashPickedUp;
    public static Action<string> OnTrashDisposed;

    // Matches Manager AttachObject in HospitalHubLevel (designer-tuned pose).
    static readonly Vector3 DefaultAttachLocalPos = new Vector3(-0.017f, 1.126f, 0.468f);
    static readonly Quaternion DefaultAttachLocalRot = new Quaternion(0f, -0.043081846f, 0f, 0.9990716f);
    static readonly Vector3 DefaultAttachLocalScale = new Vector3(1.230769f, 1.2307689f, 1.230769f);

    [Header("Holding")]
    public Transform attachPoint;

    [Tooltip("Centered attach for JanitorCart_Final. Separate from trash AttachObject.")]
    public Transform cartAttachPoint;

    public TrashItem heldTrash = null;

    void Awake()
    {
        EnsureAttachPoint();
    }

    void Start()
    {
        // Manager may be renamed Player → Manager during CharacterSwitchManager.Start;
        // re-sync after that so we copy the scene AttachObject the designer placed.
        EnsureAttachPoint();
    }

    /// <summary>
    /// Ensures this character has an AttachObject using the same local pose as Manager/Player.
    /// </summary>
    public void EnsureAttachPoint()
    {
        if (attachPoint == null)
        {
            Transform named = FindChildRecursive(transform, "AttachObject")
                ?? FindChildRecursive(transform, "TrashAttach");
            if (named != null)
                attachPoint = named;
        }

        if (attachPoint == null)
        {
            var go = new GameObject("AttachObject");
            go.transform.SetParent(transform, false);
            attachPoint = go.transform;
        }
        else if (attachPoint.name == "TrashAttach")
        {
            attachPoint.name = "AttachObject";
        }

        ApplyManagerAttachPose(attachPoint);
    }

    void ApplyManagerAttachPose(Transform target)
    {
        if (target == null) return;

        Transform source = FindManagerAttachObject();
        if (source != null && source != target)
        {
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
            return;
        }

        target.localPosition = DefaultAttachLocalPos;
        target.localRotation = DefaultAttachLocalRot;
        target.localScale = DefaultAttachLocalScale;
    }

    Transform FindManagerAttachObject()
    {
        // Prefer the live Manager/Player AttachObject the designer positioned in-scene.
        foreach (var rootName in new[] { "Manager", "Player" })
        {
            var root = GameObject.Find(rootName);
            if (root == null || root == gameObject) continue;

            var attach = FindChildRecursive(root.transform, "AttachObject");
            if (attach != null)
                return attach;
        }

        if (CharacterSwitchManager.Instance != null)
        {
            var chars = CharacterSwitchManager.Instance.characters;
            if (chars != null)
            {
                foreach (var c in chars)
                {
                    if (c == null || c.gameObject == gameObject) continue;
                    if (c.role != RoleType.Manager) continue;
                    var attach = FindChildRecursive(c.transform, "AttachObject");
                    if (attach != null)
                        return attach;
                }
            }
        }

        return null;
    }

    static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindChildRecursive(root.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }

    //trash system for janitor
    public void PickUpTrash(TrashItem trash)
    {
        if (heldTrash != null)
        {
            return;
        }

        EnsureAttachPoint();

        heldTrash = trash;

        trash.transform.SetParent(attachPoint);

        trash.transform.localPosition = Vector3.zero;

        trash.transform.localRotation = Quaternion.identity;

        var anim = GetComponent<CharacterAnimationDriver>();
        if (anim != null)
            anim.NotifyTrashPickedUp();

        OnTrashPickedUp?.Invoke(trash);
        Debug.Log($"Picked up {trash.type}");
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Q))
        {
            return;
        }

        if (CharacterSwitchManager.Instance == null)
        {
            return;
        }

        var active = CharacterSwitchManager.Instance.ActiveCharacter;

        if (active == null || active.gameObject != gameObject)
        {
            return;
        }

        if (heldTrash != null)
        {
            DropTrash();

            return;
        }

        if (JanitorCartController.Instance != null && JanitorCartController.Instance.isCarried)
        {
            JanitorCartController.Instance.Detach();
        }
    }

    public void DisposeTrash(TrashType requiredType)
    {
        if (heldTrash == null) return;

        string disposedName = heldTrash.gameObject != null ? heldTrash.gameObject.name : "";

        if (heldTrash.type == requiredType)
        {
            HospitalStatsManager.Instance.ChangeSanitation(+12f);
        }
        else
        {
            HospitalStatsManager.Instance.ChangeSanitation(-20f);
        }

        Destroy(heldTrash.gameObject);

        heldTrash = null;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play("drop");
        }

        OnTrashDisposed?.Invoke(disposedName);
        Debug.Log("Trash disposed");
    }

    public void DropTrash()
    {
        if (heldTrash == null)
        {
            return;
        }

        heldTrash.transform.SetParent(null);

        heldTrash.transform.position = transform.position + transform.forward * 1.5f;

        heldTrash = null;
    }
}
