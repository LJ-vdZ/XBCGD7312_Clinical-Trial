using UnityEngine;

/// <summary>
/// Scene-placed host for prototype systems in HospitalHubLevel.
/// Place an active GameObject named FeatureBootstrap as a scene-root sibling of
/// GameManager / Managers / UI (not under UI — overlays start inactive).
/// Attach this script; Unity RequireComponent adds the companion systems on the same object.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterSwitchManager))]
[RequireComponent(typeof(MedicineSupplyManager))]
[RequireComponent(typeof(MedicineOrganizerMinigame))]
[RequireComponent(typeof(PatientCareSystem))]
[RequireComponent(typeof(MiniGameGate))]
[RequireComponent(typeof(MiniGameTimerUI))]
[RequireComponent(typeof(ManagerStationHub))]
[RequireComponent(typeof(NotificationSidePanel))]
[RequireComponent(typeof(EndGameInfoPanel))]
[RequireComponent(typeof(StatsCollapseEndScreen))]
[RequireComponent(typeof(CharacterMoraleSystem))]
[RequireComponent(typeof(FilthSpawnSystem))]
[RequireComponent(typeof(MedicineSupplyHUD))]

public class FeatureBootstrap : MonoBehaviour
{
    /// <summary>Call before SceneManager.LoadScene so UI systems re-init cleanly.</summary>
    public static void PrepareForSceneRestart()
    {
        NewUIRoot.ClearCache();
        ManagerStationHub.Instance = null;
        CharacterSwitchManager.Instance = null;
    }

    void Start()
    {
        // Prototype panels are authored under UI / NewFeatureUICanvas in the scene.
        NewUIRoot.Ensure();

        RequireExisting<CharacterSwitchManager>();

        RequireExisting<MedicineSupplyManager>();

        RequireExisting<MedicineOrganizerMinigame>();

        RequireExisting<PatientCareSystem>();

        RequireExisting<MiniGameGate>();

        RequireExisting<MiniGameTimerUI>();

        RequireExisting<ManagerStationHub>();

        RequireExisting<NotificationSidePanel>();

        RequireExisting<EndGameInfoPanel>();

        RequireExisting<StatsCollapseEndScreen>();

        RequireExisting<CharacterMoraleSystem>();

        RequireExisting<FilthSpawnSystem>();

        RequireExisting<MedicineSupplyHUD>();

        SetupMedicineOrganizerProps();

        SetupVisitors();

        SetupJanitorCart();

        SetupColoredTrash();

        SetupCartPropGate();

        WireFootstepsOnManager();

        SetupMiniMapCamera();

        var hub = ManagerStationHub.Instance ?? RequireExisting<ManagerStationHub>();
        if (hub != null)
            hub.EnsureBuilt();
    }

    T RequireExisting<T>() where T : Component
    {
        var existing = GetComponent<T>() ?? FindFirstObjectByType<T>();

        if (existing != null)
        {
            return existing;
        }

        // RequireComponent does not retrofit an already-placed FeatureBootstrap,
        // and Unity cannot add a MonoBehaviour that lived in another script's file.
        existing = gameObject.AddComponent<T>();
        Debug.LogWarning($"[FeatureBootstrap] Added missing {typeof(T).Name} on '{name}' at runtime. " + "Add it in the Inspector so it is saved on the scene object.");
        
        return existing;
    }

    void SetupMiniMapCamera()
    {
        var miniCam = GameObject.Find("Mini-Map Camera");
        if (miniCam == null)
        {
            return;
        }

        var follower = miniCam.GetComponent<MiniMapCameraFollower>();

        if (follower == null) 
        {
            follower = miniCam.AddComponent<MiniMapCameraFollower>();
        }
            

        if (CharacterSwitchManager.Instance != null && CharacterSwitchManager.Instance.ActiveCharacter != null)
        {
            follower.SetPlayer(CharacterSwitchManager.Instance.ActiveCharacter.transform);
        }
    }

    void SetupMedicineOrganizerProps()
    {
        if (MedicineSupplyManager.Instance != null) 
        {
            MedicineSupplyManager.Instance.AutoFindReferencesPublic();
        }
            

        var rack = GameObject.Find(MedicineSupplyManager.MedicineRackName);

        if (rack == null)
        {
            return;
        }

        if (rack.GetComponent<MedicineRackInteractable>() == null) 
        {
            rack.AddComponent<MedicineRackInteractable>();
        }
            

        MedicineSupplyManager.EnsureInteractTrigger(rack);

        if (rack.GetComponent<InteractableTrigger>() == null) 
        {
            rack.AddComponent<InteractableTrigger>();
        }
            
    }

    void SetupVisitors()
    {
        var candidates = new System.Collections.Generic.List<GameObject>();

        
        var female = GameObject.Find("Female Character");

        var male = GameObject.Find("Male Character");

        if (female != null)
        {
            candidates.Add(female);
        }

        if (male != null)
        {
            candidates.Add(male);
        }

        if (candidates.Count == 0)
        {
            Transform civParent = GameObject.Find("Civilians")?.transform;

            if (civParent != null)
            {
                foreach (Transform t in civParent) 
                {
                    candidates.Add(t.gameObject);
                }
                    
            }
        }

        if (candidates.Count == 0)
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                string n = t.name.ToLowerInvariant();

                if (n.Contains("civilian") || n.Contains("visitor") || n.Contains("female character") || n.Contains("male character"))
                {
                    if (t.GetComponent<RectTransform>() == null) 
                    {
                        candidates.Add(t.gameObject);
                    }
                        
                }
            }
        }

        if (candidates.Count == 0)
        {
            var manager = GameObject.Find("Manager") ?? GameObject.Find("Player");

            Vector3 basePos = manager != null ? manager.transform.position : Vector3.zero;

            candidates.Add(CreateVisitorProxy("Visitor_Donation", basePos + new Vector3(2f, 0, 2f)));

            candidates.Add(CreateVisitorProxy("Visitor_Grief", basePos + new Vector3(-2f, 0, 2f)));
        }

        if (candidates.Count > 0) 
        {
            ConfigureVisitor(candidates[0], "Nandi Dlamini", "", true, 300);
        }
            

        if (candidates.Count > 1) 
        {
            ConfigureVisitor(candidates[1], "Farah Jacobs", "", false, 0);
        }
            
    }

    GameObject CreateVisitorProxy(string name, Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        var col = go.GetComponent<Collider>();

        if (col != null)
        {
            col.isTrigger = true;
        }
        return go;
    }

    void ConfigureVisitor(GameObject go, string vName, string story, bool donate, int amount)
    {
        // Remove wrong station script from visitor characters
        var computer = go.GetComponent<ManagerComputer>();
        if (computer != null) 
        {
            computer.enabled = false;
        }
            

        var v = go.GetComponent<VisitorInteractable>();
        if (v == null)
        {
            v = go.AddComponent<VisitorInteractable>();
        }

        // Do not overwrite Inspector-authored dialogue / name
        if (string.IsNullOrWhiteSpace(v.visitorName) || v.visitorName == "Visitor")
            v.visitorName = vName;
        if (string.IsNullOrWhiteSpace(v.backstory))
            v.backstory = story;
        // Only set donation defaults if still at component defaults and story was empty (new component)
        // Leave canDonate / donationAmount as authored in the Inspector

        if (go.GetComponent<Collider>() == null)
        {
            var c = go.AddComponent<CapsuleCollider>();
            c.isTrigger = true;
            c.height = 2f;
        }
        else
        {
            var c = go.GetComponent<Collider>();
            c.isTrigger = true;
        }
        if (go.GetComponent<InteractableTrigger>() == null)
            go.AddComponent<InteractableTrigger>();
    }

    void SetupJanitorCart()
    {
        var cart = GameObject.Find("JanitorCart_Final");
        if (cart == null)
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.name.Contains("JanitorCart"))
                {
                    cart = t.gameObject;
                    break;
                }
            }
        }
        if (cart == null) return;

        if (cart.GetComponent<JanitorCartController>() == null)
            cart.AddComponent<JanitorCartController>();
        // JanitorCartController already unlocks mini-game gate on Interact
        if (cart.GetComponent<Collider>() == null)
        {
            var box = cart.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.5f, 1.5f, 2f);
        }
        if (cart.GetComponent<InteractableTrigger>() == null)
            cart.AddComponent<InteractableTrigger>();

        var mop = cart.transform.Find("Mop");
        if (mop == null)
        {
            foreach (Transform t in cart.GetComponentsInChildren<Transform>())
            {
                if (t.name == "Mop") { mop = t; break; }
            }
        }
        if (mop != null)
        {
            if (mop.GetComponent<MopInteractable>() == null)
                mop.gameObject.AddComponent<MopInteractable>();
            if (mop.GetComponent<Collider>() == null)
            {
                var c = mop.gameObject.AddComponent<BoxCollider>();
                c.isTrigger = true;
            }
            if (mop.GetComponent<InteractableTrigger>() == null)
                mop.gameObject.AddComponent<InteractableTrigger>();
        }
    }

    void SetupCartPropGate() { /* handled in SetupJanitorCart */ }

    void SetupColoredTrash()
    {
        MapTrash("RedTrash", TrashType.Medical);
        MapTrash("BlueTrash", TrashType.Recycle);
        MapTrash("GreenTrash", TrashType.General);
        MapTrash("RedTrash (1)", TrashType.Medical);
        MapTrash("BlueTrash (1)", TrashType.Recycle);
        MapTrash("GreenTrash (1)", TrashType.General);
    }

    void MapTrash(string name, TrashType type)
    {
        var go = GameObject.Find(name);
        if (go == null) return;
        var item = go.GetComponent<TrashItem>();
        if (item == null) item = go.AddComponent<TrashItem>();
        item.type = type;
        if (go.GetComponent<Collider>() == null)
        {
            var c = go.AddComponent<BoxCollider>();
            c.isTrigger = true;
        }
        if (go.GetComponent<InteractableTrigger>() == null)
            go.AddComponent<InteractableTrigger>();
    }

    void WireFootstepsOnManager()
    {
        var player = GameObject.Find("Manager") ?? GameObject.Find("Player");
        if (player != null && player.GetComponent<FootstepAudio>() == null)
            player.AddComponent<FootstepAudio>();
    }
}
