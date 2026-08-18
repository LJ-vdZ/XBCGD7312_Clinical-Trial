using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class CivPanelClose : MonoBehaviour
{
    public GameObject uiPanel;
    public GameObject player;

    public DialogueData dialogueData;
    public ManagerComputer managerComputer;

    public int lineIndexForNameAndPortrait;
    public int defaultDialogueLineIndex = 1;

    public TextMeshProUGUI textCharacterName;
    public TextMeshProUGUI textDialogueBody;
    public UnityEngine.UI.Image portraitImage;

    public void CloseUI()
    {
        if (uiPanel != null)
            uiPanel.SetActive(false);
        if (player == null)
            return;
        var movement = player.GetComponent<SimplePlayerMovement>();
        var camera = player.GetComponent<CameraFollow>();
        if (movement != null)
            movement.SetControlsEnabled(true);
        if (camera != null)
            camera.LockCursor(false);
    }

    void Start()
    {
        defaultDialogueLineIndex = managerComputer.civIndex;
        PopulateDialogueLine(defaultDialogueLineIndex);
    }

    void OnValidate()
    {
        //if (Application.isPlaying)
        //    PopulateDialogueLine(defaultDialogueLineIndex);
    }

    public void PopulateDialogueLine(int lineIndex)
    {
        if (dialogueData == null || dialogueData.lines == null || dialogueData.lines.Length == 0)
            return;
        int i = Mathf.Clamp(lineIndex, 0, dialogueData.lines.Length - 1);
        var line = dialogueData.lines[i];
        var ch = line.character;
        if (textCharacterName != null)
            textCharacterName.text = ch != null ? ch.characterName : string.Empty;
        if (textDialogueBody != null)
            textDialogueBody.text = line.text;
        if (portraitImage != null)
        {
            portraitImage.sprite = ch != null ? ch.portrait : null;
            portraitImage.gameObject.SetActive(portraitImage.sprite != null);
        }
    }

    public void CloseCanvas()
    {
        CloseUI();
    }

    // Update is called once per frame
    void Update()
    {
        defaultDialogueLineIndex = managerComputer.civIndex;
        PopulateDialogueLine(defaultDialogueLineIndex);
    }
}
