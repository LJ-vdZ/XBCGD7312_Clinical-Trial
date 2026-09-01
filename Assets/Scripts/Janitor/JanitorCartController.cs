using UnityEngine;

public class JanitorCartController : MonoBehaviour, IInteractable
{
    //singleton instance
    public static JanitorCartController Instance;

    public Transform attachToPlayer;
    public Transform mopTransform;
    public Collider greenBin;
    public Collider redBin;
    public Collider blueBin;

    [Header("Carry pose")]
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

    //possible attach point names.  So can search for on janitor
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

    //auto-find mop and trash bins if not in inspector
    void AutoWire()
    {
        if (mopTransform == null)
        {
            var mop = transform.Find("Mop") ?? FindChildRecursive(transform, "Mop");

            if (mop != null)
            {
                mopTransform = mop;
            }
        }

        WireBin(ref greenBin, "TrashBin (1)", "TrashbinGreen");

        WireBin(ref redBin, "TrashBin (2)", "TrashbinRed");

        WireBin(ref blueBin, "TrashBin (3)", "TrashbinBlue");
    }

    //find bin by name and assigns collider if doesnt have one. scalable
    void WireBin(ref Collider slot, params string[] names)
    {
        if (slot != null)
        {
            return;
        }

        foreach (var n in names)
        {
            var t = FindChildRecursive(transform, n) ?? GameObject.Find(n)?.transform;

            if (t != null)
            {
                slot = t.GetComponent<Collider>();

                if (slot == null)
                {
                    slot = t.gameObject.AddComponent<BoxCollider>();
                }

                return;
            }
        }
    }

    //searches children for transform by name
    static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        foreach (Transform c in root)
        {
            var f = FindChildRecursive(c, name);

            if (f != null)
            {
                return f;
            }
        }

        return null;
    }

    public void Interact(GameObject player, RoleType role)
    {
        if (role != RoleType.Janitor)
        {
            return;
        }

        if (MiniGameGate.Instance != null)
        {
            MiniGameGate.Instance.Unlock(RoleType.Janitor);
        }

        if (MiniGameTimerUI.Instance != null)
        {
            MiniGameTimerUI.Instance.StartTimer("Janitor Shift Tasks", 120f, null);
        }

        //E grabs the cart, or disposes held trash into a cart bin. Q drops held trash or the cart. 
        var janitor = player.GetComponent<JanitorAbilities>();

        if (janitor != null && janitor.heldTrash != null)
        {
            TryDisposeHeldTrash(janitor, janitor.heldTrash.type);

            return;
        }

        if (!isCarried)
        {
            Attach(player);
        }
    }

    void Update()
    {
        UpdateCartMoveAudio();
    }

    //find active janitor player object
    GameObject ResolveJanitorPlayer()
    {
        if (CharacterSwitchManager.Instance != null)
        {
            if (CharacterSwitchManager.Instance.ActiveRole != RoleType.Janitor)
            {
                return null;
            }

            var active = CharacterSwitchManager.Instance.ActiveCharacter;

            return active != null ? active.gameObject : null;
        }

        if (attachToPlayer != null)
        {
            return attachToPlayer.gameObject;
        }

        var tagged = GameObject.FindGameObjectsWithTag("Janitor");

        foreach (var t in tagged)
        {
            if (t.GetComponent<RectTransform>() == null)
            {
                return t;
            }
        }

        return null;
    }

    //attach cart to player at carry point for cart
    public void Attach(GameObject player)
    {
        if (player == null)
        {
            return;
        }

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

        //keep world size when parented to janitor
        ApplyCounterScale(activeAttachPoint);

        isCarried = true;

        Debug.Log("Cart attached to " + activeAttachPoint.name + ". Press Q to drop");
    }

    //find or create attach point on player for cart
    Transform ResolveCartAttachPoint(GameObject player)
    {
        if (cartAttachOverride != null)
        {
            return cartAttachOverride;
        }

        var abilities = player.GetComponent<JanitorAbilities>();

        if (abilities != null && abilities.cartAttachPoint != null)
        {
            return abilities.cartAttachPoint;
        }

        foreach (var n in CartAttachNames)
        {
            var found = FindChildRecursive(player.transform, n);

            if (found != null)
            {
                return found;
            }
        }

        //create centered middle attach point
        var gameObject = new GameObject("CartAttach");

        gameObject.transform.SetParent(player.transform, false);

        Vector3 localPos = defaultAttachLocalPosition;

        var characterController = player.GetComponent<CharacterController>();

        if (characterController != null)
        {
            localPos = new Vector3(0f, characterController.center.y * 0.35f, Mathf.Max(0.25f, characterController.radius + 0.1f));
        }

        gameObject.transform.localPosition = localPos;
        gameObject.transform.localRotation = Quaternion.identity;
        gameObject.transform.localScale = Vector3.one;

        if (abilities != null)
        {
            abilities.cartAttachPoint = gameObject.transform;
        }

        return gameObject.transform;
    }

    //rescales cart to keep world size under parent
    void ApplyCounterScale(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        Vector3 lossy = parent.lossyScale;
        transform.localScale = new Vector3( worldRestScale.x / Mathf.Max(0.0001f, lossy.x), worldRestScale.y / Mathf.Max(0.0001f, lossy.y), worldRestScale.z / Mathf.Max(0.0001f, lossy.z));
    }

    //drop cart in current world position
    public void Detach()
    {
        //keep the cart where it is in world
        //do not snap back to original position or rotation it had at game start
        Vector3 dropPos = transform.position;

        Quaternion dropRot = transform.rotation;

        transform.SetParent(originalParent, true);

        transform.position = dropPos;
        transform.rotation = dropRot;

        if (restSaved)
        {
            transform.localScale = worldRestScale;
        }

        isCarried = false;

        activeAttachPoint = null;

    }

    void OnDisable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetJanitorCartMoving(false);
        }
    }

    //update cart movement sound
    //only play sound when player/player-cart is moving
    void UpdateCartMoveAudio()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

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

    //show mop on janitor hwen sweeping
    public void EquipMop(bool equipped)
    {
        mopEquipped = equipped;

        if (mopTransform != null)
        {
            mopTransform.gameObject.SetActive(true);
        }
    }

    //dispose trash janitor is holding
    public bool TryDisposeHeldTrash(JanitorAbilities janitor, TrashType type)
    {
        if (janitor == null || janitor.heldTrash == null)
        {
            return false;
        }

        janitor.DisposeTrash(type);

        return true;
    }
}

//interact with mop child object for sweeping
public class MopInteractable : MonoBehaviour, IInteractable
{
    public bool CanInteractWhenLocked => false;

    public void Interact(GameObject player, RoleType role)
    {
        if (role != RoleType.Janitor)
        {
            return;
        }

        if (JanitorCartController.Instance != null)
        {
            JanitorCartController.Instance.EquipMop(true);

            if (NotificationSidePanel.Instance != null)
            {
                NotificationSidePanel.Instance.ShowRaw("Mop equipped. Clean dirt piles");
            }
        }
    }
}
