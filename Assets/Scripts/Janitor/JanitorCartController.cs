using UnityEngine;

/// <summary>
/// Carryable janitor cart with three bin types + mop.
/// Attach/detach with Q. E on the cart still disposes held trash into bins.
/// </summary>
public class JanitorCartController : MonoBehaviour, IInteractable
{
    public static JanitorCartController Instance;

    public Transform attachToPlayer;
    public Transform mopTransform;
    public Collider greenBin;
    public Collider redBin;
    public Collider blueBin;

    [Header("Carry pose")]
    [Tooltip("Optional override. Otherwise looks for CartAttach on the janitor.")]
    public Transform cartAttachOverride;
    public Vector3 defaultAttachLocalPosition = new Vector3(0f, 0.35f, 0.45f);
    public Vector3 carriedLocalPosition = new Vector3(0f, -0.381f, 0.781f);
    public Vector3 carriedLocalEuler = Vector3.zero;

    public bool isCarried;
    public bool mopEquipped;

    Vector3 worldRestPos;
    Quaternion worldRestRot;
    Vector3 worldRestScale;
    Transform originalParent;
    bool restSaved;
    Transform activeAttachPoint;

    static readonly string[] CartAttachNames =
    {
        "CartAttach",
        "CartAttachPoint",
        "AttachCart",
        "JanitorCartAttach",
        "CartHold"
    };

    public bool CanInteractWhenLocked => false;

    void Awake() => Instance = this;

    void Start()
    {
        AutoWire();
    }

    void AutoWire()
    {
        if (mopTransform == null)
        {
            var mop = transform.Find("Mop") ?? FindChildRecursive(transform, "Mop");
            if (mop != null) mopTransform = mop;
        }

        WireBin(ref greenBin, "TrashBin (1)", "TrashbinGreen");
        WireBin(ref redBin, "TrashBin (2)", "TrashbinRed");
        WireBin(ref blueBin, "TrashBin (3)", "TrashbinBlue");
    }

    void WireBin(ref Collider slot, params string[] names)
    {
        if (slot != null) return;
        foreach (var n in names)
        {
            var t = FindChildRecursive(transform, n) ?? GameObject.Find(n)?.transform;
            if (t != null)
            {
                slot = t.GetComponent<Collider>();
                if (slot == null) slot = t.gameObject.AddComponent<BoxCollider>();
                return;
            }
        }
    }

    static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            var f = FindChildRecursive(c, name);
            if (f != null) return f;
        }
        return null;
    }

    public void Interact(GameObject player, RoleType role)
    {
        if (role != RoleType.Janitor) return;

        if (MiniGameGate.Instance != null)
            MiniGameGate.Instance.Unlock(RoleType.Janitor);
        if (MiniGameTimerUI.Instance != null)
            MiniGameTimerUI.Instance.StartTimer("Janitor Shift Tasks", 120f, null);

        // E = dispose held trash into the cart bins. Attach/detach is Q only.
        var janitor = player.GetComponent<JanitorAbilities>();
        if (janitor != null && janitor.heldTrash != null)
        {
            TryDisposeHeldTrash(janitor, janitor.heldTrash.type);
            return;
        }

        if (NotificationSidePanel.Instance != null)
            NotificationSidePanel.Instance.ShowRaw("Press Q to grab or drop the cart.");
    }

    void Update()
    {
        UpdateCartMoveAudio();

        if (!Input.GetKeyDown(KeyCode.Q)) return;

        GameObject player = ResolveJanitorPlayer();
        if (player == null) return;

        var janitor = player.GetComponent<JanitorAbilities>();
        if (janitor != null && janitor.heldTrash != null)
            return;

        if (isCarried)
        {
            Detach();
            return;
        }

        // Grab only when standing near / targeting the cart.
        if (!IsPlayerNearOrTargeting(player))
            return;

        Attach(player);
    }

    GameObject ResolveJanitorPlayer()
    {
        if (CharacterSwitchManager.Instance != null)
        {
            if (CharacterSwitchManager.Instance.ActiveRole != RoleType.Janitor)
                return null;
            var active = CharacterSwitchManager.Instance.ActiveCharacter;
            return active != null ? active.gameObject : null;
        }

        if (attachToPlayer != null)
            return attachToPlayer.gameObject;

        var tagged = GameObject.FindGameObjectsWithTag("Janitor");
        foreach (var t in tagged)
        {
            if (t.GetComponent<RectTransform>() == null)
                return t;
        }
        return null;
    }

    bool IsPlayerNearOrTargeting(GameObject player)
    {
        if (player == null) return false;

        var handler = player.GetComponent<PlayerInteractionHandler>();
        if (handler != null && ReferenceEquals(handler.GetCurrentTarget(), this))
            return true;

        // Fallback distance check if trigger targeting failed.
        float dist = Vector3.Distance(player.transform.position, transform.position);
        return dist <= 3.5f;
    }

    public void Attach(GameObject player)
    {
        if (player == null) return;

        if (!restSaved)
        {
            worldRestPos = transform.position;
            worldRestRot = transform.rotation;
            worldRestScale = transform.lossyScale;
            originalParent = transform.parent;
            restSaved = true;
        }

        attachToPlayer = player.transform;
        activeAttachPoint = ResolveCartAttachPoint(player);

        transform.SetParent(activeAttachPoint, false);
        transform.localPosition = carriedLocalPosition;
        transform.localRotation = Quaternion.Euler(carriedLocalEuler);
        // Keep world size stable when parented under a scaled character (Janitor is often ×4).
        ApplyCounterScale(activeAttachPoint);
        isCarried = true;
        Debug.Log("Cart attached to " + activeAttachPoint.name + ". Press Q to drop.");
    }

    Transform ResolveCartAttachPoint(GameObject player)
    {
        if (cartAttachOverride != null)
            return cartAttachOverride;

        var abilities = player.GetComponent<JanitorAbilities>();
        if (abilities != null && abilities.cartAttachPoint != null)
            return abilities.cartAttachPoint;

        foreach (var n in CartAttachNames)
        {
            var found = FindChildRecursive(player.transform, n);
            if (found != null)
                return found;
        }

        // Create a centered middle attach point (not side / AttachObject trash point).
        var go = new GameObject("CartAttach");
        go.transform.SetParent(player.transform, false);

        Vector3 localPos = defaultAttachLocalPosition;
        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            localPos = new Vector3(0f, cc.center.y * 0.35f, Mathf.Max(0.25f, cc.radius + 0.1f));
        }

        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        if (abilities != null)
            abilities.cartAttachPoint = go.transform;

        return go.transform;
    }

    void ApplyCounterScale(Transform parent)
    {
        if (parent == null) return;
        Vector3 lossy = parent.lossyScale;
        transform.localScale = new Vector3(
            worldRestScale.x / Mathf.Max(0.0001f, lossy.x),
            worldRestScale.y / Mathf.Max(0.0001f, lossy.y),
            worldRestScale.z / Mathf.Max(0.0001f, lossy.z));
    }

    public void Detach()
    {
        // Keep the cart where it is in the world — do not snap back to spawn/front.
        Vector3 dropPos = transform.position;
        Quaternion dropRot = transform.rotation;

        transform.SetParent(originalParent, true);
        transform.position = dropPos;
        transform.rotation = dropRot;

        if (restSaved)
            transform.localScale = worldRestScale;

        isCarried = false;
        activeAttachPoint = null;
        Debug.Log("Cart dropped in place.");
    }

    void OnDisable()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetJanitorCartMoving(false);
    }

    void UpdateCartMoveAudio()
    {
        if (AudioManager.Instance == null) return;

        bool moving = false;
        if (isCarried)
        {
            GameObject player = ResolveJanitorPlayer();
            if (player != null)
            {
                var move = player.GetComponent<SimplePlayerMovement>();
                bool controlsOn = move == null || move.AreControlsEnabled();
                if (controlsOn)
                {
                    float h = Input.GetAxisRaw("Horizontal");
                    float v = Input.GetAxisRaw("Vertical");
                    moving = h * h + v * v > 0.01f;
                }
            }
        }

        AudioManager.Instance.SetJanitorCartMoving(moving);
    }

    public void EquipMop(bool equipped)
    {
        mopEquipped = equipped;
        if (mopTransform != null)
            mopTransform.gameObject.SetActive(true);
    }

    public bool TryDisposeHeldTrash(JanitorAbilities janitor, TrashType type)
    {
        if (janitor == null || janitor.heldTrash == null) return false;
        janitor.DisposeTrash(type);
        return true;
    }
}

/// <summary>Interact with mop child to equip for dirt cleaning.</summary>
public class MopInteractable : MonoBehaviour, IInteractable
{
    public bool CanInteractWhenLocked => false;

    public void Interact(GameObject player, RoleType role)
    {
        if (role != RoleType.Janitor) return;
        if (JanitorCartController.Instance != null)
        {
            JanitorCartController.Instance.EquipMop(true);
            if (NotificationSidePanel.Instance != null)
                NotificationSidePanel.Instance.ShowRaw("Mop equipped. Clean dirt piles.");
        }
    }
}
