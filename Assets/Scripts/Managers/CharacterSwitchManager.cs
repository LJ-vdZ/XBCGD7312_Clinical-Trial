using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Press P to open character select. Click a role body to transfer camera + controls.
/// Configures Doctor/Nurse/Janitor/Manager scene objects as playable characters.
/// </summary>
public class CharacterSwitchManager : MonoBehaviour
{
    public static CharacterSwitchManager Instance;

    public static Action<PlayableCharacter> OnCharacterChanged;

    [Header("References")]
    public CameraFollow cameraFollow;
    public PlayableCharacter[] characters;

    [Header("UI")]
    public GameObject selectionPanel;
    public Transform buttonContainer;

    PlayableCharacter active;
    bool panelOpen;

    public PlayableCharacter ActiveCharacter => active;
    public RoleType ActiveRole => active != null ? active.role : RoleType.Manager;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EnsureCharactersConfigured();
        BuildSelectionUIIfNeeded();

        // Start controlling Manager / Player
        PlayableCharacter manager = FindByRole(RoleType.Manager);
        if (manager == null && characters != null && characters.Length > 0)
            manager = characters[0];

        if (manager != null)
            SwitchTo(manager, playSound: false);
        else
            Debug.LogWarning("CharacterSwitchManager: no Manager character found.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            TogglePanel();

        if (panelOpen && Input.GetKeyDown(KeyCode.Escape))
            ClosePanel();
    }

    public void TogglePanel()
    {
        if (panelOpen) ClosePanel();
        else OpenPanel();
    }

    public void ClosePanel()
    {
        if (selectionPanel == null) return;
        panelOpen = false;
        selectionPanel.SetActive(false);

        if (active != null && active.movement != null)
            active.movement.SetControlsEnabled(true);

        if (cameraFollow != null)
            cameraFollow.LockCursor(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("close");
    }

    public void SwitchTo(PlayableCharacter next, bool playSound = true)
    {
        if (next == null) return;

        foreach (var c in characters)
        {
            if (c == null) continue;
            c.SetControlled(false);
        }

        active = next;
        active.SetControlled(true);

        if (active.roleManager != null)
            active.roleManager.ApplyRoleWithoutTimerReset(active.role);
        else if (PlayerRoleManager.ActiveInstance != null)
            PlayerRoleManager.ActiveInstance.ApplyRoleWithoutTimerReset(active.role);

        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(active.transform);
            if (active.movement != null)
                active.movement.SetCamera(cameraFollow.GetComponent<Camera>());
        }

        // Snap mini-map camera X/Z to the newly active body.
        var miniMap = FindFirstObjectByType<MiniMapCameraFollower>();
        if (miniMap == null)
        {
            var miniCamGo = GameObject.Find("Mini-Map Camera");
            if (miniCamGo != null)
                miniMap = miniCamGo.GetComponent<MiniMapCameraFollower>()
                          ?? miniCamGo.AddComponent<MiniMapCameraFollower>();
        }
        if (miniMap != null)
            miniMap.SetPlayer(active.transform);

        ClosePanel();
        OnCharacterChanged?.Invoke(active);

        if (playSound && AudioManager.Instance != null)
            AudioManager.Instance.Play("success");

        Debug.Log("Now controlling: " + active.DisplayName);
    }

    public void SwitchToRole(RoleType role)
    {
        var c = FindByRole(role);
        if (c != null) SwitchTo(c);
    }

    PlayableCharacter FindByRole(RoleType role)
    {
        if (characters == null) return null;
        foreach (var c in characters)
            if (c != null && c.role == role)
                return c;
        return null;
    }

    void EnsureCharactersConfigured()
    {
        var list = new List<PlayableCharacter>();

        TryConfigure("Player", RoleType.Manager, "Manager", list);
        TryConfigure("Manager", RoleType.Manager, "Manager", list);
        TryConfigure("Doctor", RoleType.Doctor, "Doctor", list);
        TryConfigure("Nurse", RoleType.Nurse, "Nurse", list);
        TryConfigure("Janitor", RoleType.Janitor, "Janitor", list);

        // Prefer Manager-named over duplicate Player if both exist with same role
        var unique = new List<PlayableCharacter>();
        var seen = new HashSet<RoleType>();
        // Prefer Manager name for Manager role
        foreach (var c in list)
        {
            if (c.role == RoleType.Manager && c.gameObject.name == "Manager")
            {
                unique.Add(c);
                seen.Add(RoleType.Manager);
            }
        }
        foreach (var c in list)
        {
            if (seen.Contains(c.role)) continue;
            unique.Add(c);
            seen.Add(c.role);
        }

        characters = unique.ToArray();

        // Rename Player -> Manager for clarity
        foreach (var c in characters)
        {
            if (c != null && c.role == RoleType.Manager && c.gameObject.name == "Player")
                c.gameObject.name = "Manager";
        }

        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<CameraFollow>();
    }

    void TryConfigure(string objectName, RoleType role, string display, List<PlayableCharacter> into)
    {
        GameObject go = FindWorldCharacter(objectName, role);
        if (go == null) return;

        var pc = go.GetComponent<PlayableCharacter>();
        if (pc == null) pc = go.AddComponent<PlayableCharacter>();
        pc.role = role;
        pc.displayName = display;

        // Disable old station behaviour — switching is via P panel now
        var station = go.GetComponent<RoleSwitchStation>();
        if (station != null) station.enabled = false;
        var trigger = go.GetComponent<InteractableTrigger>();
        if (trigger != null) trigger.enabled = false;

        EnsureControlComponents(go, role);
        pc.CacheComponents();
        into.Add(pc);
    }

    GameObject FindWorldCharacter(string objectName, RoleType role)
    {
        // Prefer tagged world avatars
        string tag = role switch
        {
            RoleType.Manager => "Player",
            RoleType.Doctor => "Dr",
            RoleType.Nurse => "Nurse",
            RoleType.Janitor => "Janitor",
            _ => null
        };

        if (!string.IsNullOrEmpty(tag))
        {
            try
            {
                var tagged = GameObject.FindGameObjectsWithTag(tag);
                foreach (var t in tagged)
                {
                    if (t.GetComponent<RectTransform>() != null) continue;
                    if (t.name == objectName || role == RoleType.Manager)
                        return t;
                }
                // any tagged non-UI
                foreach (var t in tagged)
                {
                    if (t.GetComponent<RectTransform>() == null)
                        return t;
                }
            }
            catch (UnityException)
            {
                // Tag may not exist in tag manager
            }
        }

        // Fallback: all transforms named correctly that are NOT under a Canvas
        var all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t in all)
        {
            if (t.name != objectName) continue;
            if (t.GetComponent<RectTransform>() != null) continue;
            if (t.GetComponentInParent<Canvas>() != null) continue;
            return t.gameObject;
        }

        return null;
    }

    void EnsureControlComponents(GameObject go, RoleType role)
    {
        var cc = go.GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = go.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 1f, 0f);
        }

        if (go.GetComponent<SimplePlayerMovement>() == null)
            go.AddComponent<SimplePlayerMovement>();

        if (go.GetComponent<PlayerInteractionHandler>() == null)
            go.AddComponent<PlayerInteractionHandler>();

        // Keep role tags consistent so trigger volumes can detect any playable body.
        try
        {
            string tag = role switch
            {
                RoleType.Manager => "Player",
                RoleType.Doctor => "Dr",
                RoleType.Nurse => "Nurse",
                RoleType.Janitor => "Janitor",
                _ => "Player"
            };
            if (!string.IsNullOrEmpty(tag) && !go.CompareTag(tag))
                go.tag = tag;
        }
        catch (UnityException)
        {
            // Tag may not exist in TagManager yet.
        }

        var prm = go.GetComponent<PlayerRoleManager>();
        if (prm == null) prm = go.AddComponent<PlayerRoleManager>();
        prm.SetFixedRole(role);

        if (go.GetComponent<FootstepAudio>() == null)
            go.AddComponent<FootstepAudio>();

        if (role == RoleType.Janitor && go.GetComponent<JanitorAbilities>() == null)
            go.AddComponent<JanitorAbilities>();
    }

    void BuildSelectionUIIfNeeded()
    {
        if (selectionPanel != null) return;

        selectionPanel = ClinicalUIFactory.FindByName("CharacterSelectPanel");
        if (selectionPanel == null)
        {
            Debug.LogError("CharacterSwitchManager: missing scene object 'CharacterSelectPanel'.");
            return;
        }

        selectionPanel.SetActive(false);

        var buttonsT = ClinicalUIFactory.FindChild(selectionPanel.transform, "Buttons");
        if (buttonsT != null)
            buttonContainer = buttonsT;

        BindRoleButton("ManagerButton", RoleType.Manager);
        BindRoleButton("DoctorButton", RoleType.Doctor);
        BindRoleButton("NurseButton", RoleType.Nurse);
        BindRoleButton("JanitorButton", RoleType.Janitor);
        ClinicalUIFactory.BindButton(selectionPanel.transform, "CloseButton", ClosePanel);
    }

    void BindRoleButton(string buttonName, RoleType role)
    {
        var root = buttonContainer != null ? buttonContainer : selectionPanel.transform;
        ClinicalUIFactory.BindButton(root, buttonName, () =>
        {
            var character = FindByRole(role);
            if (character != null)
                SwitchTo(character);
        });
    }

    public void OpenPanel()
    {
        if (selectionPanel == null)
            BuildSelectionUIIfNeeded();
        if (selectionPanel == null) return;

        if (NewUIRoot.Canvas != null && !NewUIRoot.Canvas.gameObject.activeSelf)
            NewUIRoot.Canvas.gameObject.SetActive(true);

        panelOpen = true;
        selectionPanel.SetActive(true);
        selectionPanel.transform.SetAsLastSibling();

        if (active != null && active.movement != null)
            active.movement.SetControlsEnabled(false);

        if (cameraFollow != null)
            cameraFollow.LockCursor(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("open");
    }
}
