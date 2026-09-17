using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public DialogueScriptable dialogue;

    private int currentDialogueIndex = 0;
    private bool waitingForGate = false;

    public TMP_Text dialogueField;
    public Button Btnnext;
    public Button Btngate;


    private void Start()
    {
        StartDialogue();

        Btnnext.onClick.AddListener(NextDialogue);
        Btngate.onClick.AddListener(() => PassGate(true));
    }


    public void StartDialogue()
    {
        currentDialogueIndex = 0;
        waitingForGate = false;

        ShowCurrentDialogue();
    }


    public void NextDialogue()
    {
        // Don't progress while waiting for a gate
        if (waitingForGate)
        {
            return;
        }

        currentDialogueIndex++;

        // Check if dialogue has finished
        if (currentDialogueIndex >= dialogue.dialogue.Count)
        {
            EndDialogue();
            return;
        }

        ShowCurrentDialogue();
    }


    private void ShowCurrentDialogue()
    {
        DialogueScriptable.DialogueEntry currentDialogue =
            dialogue.dialogue[currentDialogueIndex];

        // Show dialogue in the temporary text field
        dialogueField.text = currentDialogue.dialogueText;

        Debug.Log(currentDialogue.dialogueText);

        // Check if this entry is a gate
        if (currentDialogue.type == DialogueScriptable.DialogueType.Gate)
        {
            waitingForGate = true;
        }
        else
        {
            waitingForGate = false;
        }
    }


    public void PassGate(bool completed)
    {
        // Make sure we are currently waiting for a gate
        if (!waitingForGate)
        {
            return;
        }

        // Don't continue if the objective isn't complete
        if (!completed)
        {
            return;
        }

        // Gate has been passed
        waitingForGate = false;

        // Continue to the next dialogue
        currentDialogueIndex++;

        // Check if dialogue has finished
        if (currentDialogueIndex >= dialogue.dialogue.Count)
        {
            EndDialogue();
            return;
        }

        ShowCurrentDialogue();
    }


    private void EndDialogue()
    {
        dialogueField.text = "";

        Debug.Log("Dialogue finished.");
    }
}