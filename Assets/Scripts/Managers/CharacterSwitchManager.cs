using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class CharacterSwitchManager : MonoBehaviour
{
    public static CharacterSwitchManager Instance;

    public static Action<PlayableCharacter> OnCharacterChanged;

    [Header("Character and Camera Reference")]
    public CameraFollow cameraFollow;
    public PlayableCharacter[] characters;

    [Header("UI")]
    public GameObject selectionPanel;
    public Transform buttonContainer;

    PlayableCharacter active;

    bool panelOpen;

    /// <summary>When false, pressing P will not open the character select panel.</summary>
    public bool allowOpenPanel = true;

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

        //start as Manager, called player in scene. Controls go here first
        PlayableCharacter manager = FindByRole(RoleType.Manager);

        if (manager == null && characters != null && characters.Length > 0) 
        {
            manager = characters[0];
        }

        if (manager != null) 
        {
            SwitchTo(manager, playSound: false);
        }
        else 
        {
            Debug.LogWarning("No Manager character found.");
        }
            
    }

    void Update()
    {
        //if player presses P, open character select. 
        if (Input.GetKeyDown(KeyCode.P) && allowOpenPanel) 
        {
            TogglePanel();
        }
            

        if (panelOpen && Input.GetKeyDown(KeyCode.Escape)) 
        {
            ClosePanel();
        }
            
    }

    public void TogglePanel()
    {
        if (panelOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    public void ClosePanel()
    {
        if (selectionPanel == null)
        {
            return;
        }
        
        panelOpen = false;
        
        selectionPanel.SetActive(false);

        if (active != null && active.movement != null)
        {
            active.movement.SetControlsEnabled(true);
        }

        if (cameraFollow != null)
        {
            cameraFollow.LockCursor(true);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play("close");
        }
    }

    //method that closes nurse station, doctor station, and manage station UI when player opens switch ui on top of it and switches roles
    //fixes ui stacking bug
    void CloseOpenSystemsUi()
    {
        var canvas = NewUIRoot.Ensure();

        if (canvas != null)
        {
            var root = canvas.transform;

            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i).gameObject;

                if (KeepSystemsUiOnRoleSwitch(child.name))
                {
                    continue;
                }

                child.SetActive(false);
            }
        }

        HideByName("JobSelectionOverlayUI");

        HideByName("NurseUI-IV");

        if (ManagerStationHub.Instance != null)
        {
            ManagerStationHub.Instance.CloseAll(false);
        }

        var mainUi = FindFirstObjectByType<MainSceneUIManager>();

        if (mainUi != null)
        {
            mainUi.CloseAllUI();
        }

        PatientInteractable.CloseOpenCarePanels();

        if (MiniGameTimerUI.Instance != null)
        {
            MiniGameTimerUI.Instance.StopTimer();
        }

        var organizer = FindFirstObjectByType<MedicineOrganizerMinigame>();
        
        if (organizer != null)
        {
            organizer.AbortIfActive();
        }

        var iv = FindFirstObjectByType<IVMinigame>();

        if (iv != null)
        {
            iv.AbortIfPlaying();
        }
    }

    static bool KeepSystemsUiOnRoleSwitch(string panelName)
    {
        return panelName == "NotificationSidePanel"
            || panelName == "PowerOutagePanel"
            || panelName == "EndGameInfoPanel"
            || panelName == "StatsCollapseEndScreen"
            || panelName == "VisitorDialogue"
            || panelName == "TutorialFade";
    }

    static void HideByName(string objectName)
    {
        var go = ClinicalUIFactory.FindByName(objectName);
        
        if (go != null)
        {
            go.SetActive(false);
        }
    }

    public void SwitchTo(PlayableCharacter next, bool playSound = true)
    {
        if (next == null)
        {
            return;
        }

        if (active != null)
        {
            CloseOpenSystemsUi();
        }

        foreach (var character in characters)
        {
            if (character == null)
            {
                continue;
            }

            character.SetControlled(false);
        }

        active = next;

        active.SetControlled(true);

        if (active.roleManager != null) 
        { 
            active.roleManager.ApplyRoleWithoutTimerReset(active.role); 
        }
            
        else if (PlayerRoleManager.ActiveInstance != null) 
        {
            PlayerRoleManager.ActiveInstance.ApplyRoleWithoutTimerReset(active.role); 
        }
            
        //check if camera is in scene and if already attach to character or not
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(active.transform);

            if (active.movement != null) 
            {
                active.movement.SetCamera(cameraFollow.GetComponent<Camera>());
            }
                
        }

        //snap mini-map camera x and z to new active character
        var miniMap = FindFirstObjectByType<MiniMapCameraFollower>();

        if (miniMap == null)
        {
            var miniCamGameObj = GameObject.Find("Mini-Map Camera");

            if (miniCamGameObj != null) 
            {
                miniMap = miniCamGameObj.GetComponent<MiniMapCameraFollower>() ?? miniCamGameObj.AddComponent<MiniMapCameraFollower>();
            }
                
        }
        if (miniMap != null) 
        { 
            miniMap.SetPlayer(active.transform); 
        }
            

        ClosePanel();

        OnCharacterChanged?.Invoke(active);

        if (playSound && AudioManager.Instance != null) 
        { 
            AudioManager.Instance.Play("success"); 
        }
            

        Debug.Log("Now controlling: " + active.DisplayName);
    }

    public void SwitchToRole(RoleType role)
    {
        var character = FindByRole(role);

        if (character != null)
        {
            SwitchTo(character);
        }
    }

    PlayableCharacter FindByRole(RoleType role)
    {
        if (characters == null)
        {
            return null;
        }

        foreach (var character in characters)
        {
            if (character != null && character.role == role)
            {
                return character;
            }
        }
            
        return null;
    }

    void EnsureCharactersConfigured()
    {
        //check characters in list and their tags
        var list = new List<PlayableCharacter>();

        TryConfigure("Player", RoleType.Manager, "Manager", list);
        TryConfigure("Manager", RoleType.Manager, "Manager", list);
        TryConfigure("Doctor", RoleType.Doctor, "Doctor", list);
        TryConfigure("Nurse", RoleType.Nurse, "Nurse", list);
        TryConfigure("Janitor", RoleType.Janitor, "Janitor", list);

        //since current setup Manager has player tag, safety for if character has Manager take, prioritose manager 
        var unique = new List<PlayableCharacter>();
        var seen = new HashSet<RoleType>();

        foreach (var charcter in list)
        {
            if (charcter.role == RoleType.Manager && charcter.gameObject.name == "Manager")
            {
                unique.Add(charcter);

                seen.Add(RoleType.Manager);
            }
        }

        foreach (var charcter in list)
        {
            if (seen.Contains(charcter.role))
            {
                continue;
            }

            unique.Add(charcter);
            seen.Add(charcter.role);
        }

        characters = unique.ToArray();

        //rename tag Player to Manager for clarity if character was set to tag Player
        foreach (var charcter in characters)
        {
            if (charcter != null && charcter.role == RoleType.Manager && charcter.gameObject.name == "Player") 
            {
                charcter.gameObject.name = "Manager";
            }
                
        }

        if (cameraFollow == null) 
        {
            cameraFollow = FindFirstObjectByType<CameraFollow>();
        }
            
    }

    void TryConfigure(string objectName, RoleType role, string display, List<PlayableCharacter> into)
    {
        GameObject gameObject = FindWorldCharacter(objectName, role);

        if (gameObject == null)
        {
            return;
        }

        var playableCharacter = gameObject.GetComponent<PlayableCharacter>();

        if (playableCharacter == null)
        {
            playableCharacter = gameObject.AddComponent<PlayableCharacter>();
        }

        playableCharacter.role = role;

        playableCharacter.displayName = display;

        //disable old way of switching (interacting with other role to switch to) and use new switcing throuhg P panel
        var station = gameObject.GetComponent<RoleSwitchStation>();

        if (station != null)
        {
            station.enabled = false;
        }

        var trigger = gameObject.GetComponent<InteractableTrigger>();

        if (trigger != null)
        {
            trigger.enabled = false;
        }

        EnsureControlComponents(gameObject, role);

        playableCharacter.CacheComponents();

        into.Add(playableCharacter);
    }

    GameObject FindWorldCharacter(string objectName, RoleType role)
    {
        //expected tags on world avatars
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
                    if (t.GetComponent<RectTransform>() != null)
                    {
                        continue;
                    }

                    if (t.name == objectName || role == RoleType.Manager) 
                    { 
                        return t; 
                    }
                        
                }
                // any tagged non-UI
                foreach (var t in tagged)
                {
                    if (t.GetComponent<RectTransform>() == null) 
                    {
                        return t;
                    }
                        
                }
            }
            catch (UnityException)
            {
                Debug.Log("Tag not in tag list");
            }
        }

        //incase, check all transforms named correctly that arent under Canvas
        var all = FindObjectsByType<Transform>(FindObjectsSortMode.None);

        foreach (var t in all)
        {
            if (t.name != objectName)
            {
                continue;
            }

            if (t.GetComponent<RectTransform>() != null)
            {
                continue;
            }

            if (t.GetComponentInParent<Canvas>() != null)
            {
                continue;
            }

            return t.gameObject;
        }

        return null;
    }

    void EnsureControlComponents(GameObject gameObject, RoleType role)
    {
        var characterController = gameObject.GetComponent<CharacterController>();

        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();

            characterController.height = 2f;

            characterController.radius = 0.4f;

            characterController.center = new Vector3(0f, 1f, 0f);
        }

        if (gameObject.GetComponent<SimplePlayerMovement>() == null) 
        {
            gameObject.AddComponent<SimplePlayerMovement>(); 
        }
            

        if (gameObject.GetComponent<PlayerInteractionHandler>() == null) 
        {
            gameObject.AddComponent<PlayerInteractionHandler>(); 
        }
            

        //keep role tags so trigger volumes can detect playable character object
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

            if (!string.IsNullOrEmpty(tag) && !gameObject.CompareTag(tag)) 
            {
                gameObject.tag = tag;
            }
              
        }
        catch (UnityException)
        {
            Debug.Log("Tag no in tag list");
        }

        var playerRoleManager = gameObject.GetComponent<PlayerRoleManager>();

        if (playerRoleManager == null)
            playerRoleManager = gameObject.AddComponent<PlayerRoleManager>();

        if (playerRoleManager != null)
            playerRoleManager.SetFixedRole(role);
        

        if (gameObject.GetComponent<FootstepAudio>() == null) 
        {
            gameObject.AddComponent<FootstepAudio>();
        }

        if (role == RoleType.Janitor && gameObject.GetComponent<JanitorAbilities>() == null) 
        {
            gameObject.AddComponent<JanitorAbilities>();
        }

        if (gameObject.GetComponent<CharacterAnimationDriver>() == null
            && gameObject.GetComponentInChildren<Animator>(true) != null)
        {
            gameObject.AddComponent<CharacterAnimationDriver>();
        }
            
    }

    void BuildSelectionUIIfNeeded()
    {
        if (selectionPanel != null)
        {
            return;
        }

        selectionPanel = ClinicalUIFactory.FindByName("CharacterSelectPanel");

        if (selectionPanel == null)
        {
            Debug.LogError("CharacterSwitchManager missing object CharacterSelectPanel.");

            return;
        }

        selectionPanel.SetActive(false);

        var buttonsT = ClinicalUIFactory.FindChild(selectionPanel.transform, "Buttons");

        if (buttonsT != null) 
        {
            buttonContainer = buttonsT;
        }
            

        //click role button to give control to other plyer character and snap camera there.
        BindRoleButton("ManagerButton", RoleType.Manager);
        BindRoleButton("DoctorButton", RoleType.Doctor);
        BindRoleButton("NurseButton", RoleType.Nurse);
        BindRoleButton("JanitorButton", RoleType.Janitor);

        //close panel button
        ClinicalUIFactory.BindButton(selectionPanel.transform, "CloseButton", ClosePanel);
    }

    void BindRoleButton(string buttonName, RoleType role)
    {
        var root = buttonContainer != null ? buttonContainer : selectionPanel.transform;

        ClinicalUIFactory.BindButton(root, buttonName, () =>
        {
            var character = FindByRole(role);

            if (character != null) 
            {
                SwitchTo(character);
            }
                
        });
    }

    public void OpenPanel()
    {
        if (!allowOpenPanel)
        {
            return;
        }

        if (selectionPanel == null) 
        {
            BuildSelectionUIIfNeeded();
        }

        if (selectionPanel == null)
        {
            return;
        }

        if (NewUIRoot.Canvas != null && !NewUIRoot.Canvas.gameObject.activeSelf) 
        {
            NewUIRoot.Canvas.gameObject.SetActive(true);
        }
            

        panelOpen = true;

        selectionPanel.SetActive(true);

        selectionPanel.transform.SetAsLastSibling();

        if (active != null && active.movement != null) 
        {
            active.movement.SetControlsEnabled(false);
        }
            

        if (cameraFollow != null) 
        {
            cameraFollow.LockCursor(false);
        }
            

        Cursor.lockState = CursorLockMode.None;

        Cursor.visible = true;

        if (AudioManager.Instance != null) 
        {
            AudioManager.Instance.Play("open");
        }
            
    }

    public void SetRoleButtonEnabled(string buttonName, bool enabled)
    {
        BuildSelectionUIIfNeeded();

        var root = buttonContainer != null ? buttonContainer : selectionPanel != null ? selectionPanel.transform : null;
        if (root == null)
            return;

        var t = ClinicalUIFactory.FindChild(root, buttonName);
        if (t == null)
            return;

        var btn = t.GetComponent<Button>();
        if (btn != null)
            btn.interactable = enabled;
    }

    /// <summary>Replace the click action for a role button (used by the tutorial fade).</summary>
    public void BindRoleButtonOverride(string buttonName, System.Action onClick)
    {
        BuildSelectionUIIfNeeded();

        var root = buttonContainer != null ? buttonContainer : selectionPanel != null ? selectionPanel.transform : null;
        if (root == null || onClick == null)
            return;

        var t = ClinicalUIFactory.FindChild(root, buttonName);
        if (t == null)
            return;

        var btn = t.GetComponent<Button>();
        if (btn == null)
            return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick());
    }
}
