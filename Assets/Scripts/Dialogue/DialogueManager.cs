using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DialogueManager : MonoBehaviour
{
    public DialogueScriptable dialogue;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueField;
    public Button Btnnext;
    public Button Btngate;

    private int currentDialogueIndex = 0;
    private bool waitingForGate = false;
    private Action onFinished;

    public bool IsWaitingForGate => waitingForGate;

    private void Start()
    {
        // Make sure the dialogue UI exists and is hooked up.
        EnsureDialogueUI();
        ApplyLowerThirdLayout();

        if (Btnnext != null)
            Btnnext.onClick.AddListener(NextDialogue);

        if (Btngate != null)
            Btngate.onClick.AddListener(() => PassGate(true));

        // Original behaviour: start if a DialogueScriptable is already assigned.
        if (dialogue != null)
            StartDialogue();
        else if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    /// <summary>
    /// Finds or builds the panel used to show DialogueScriptable text.
    /// </summary>
    public void EnsureDialogueUI()
    {
        if (dialoguePanel == null)
            dialoguePanel = GameObject.Find("VisitorDialogue");

        if (dialoguePanel == null)
            dialoguePanel = CreateDialoguePanel();

        if (dialogueField == null && dialoguePanel != null)
            dialogueField = dialoguePanel.GetComponentInChildren<TMP_Text>(true);

        if (Btnnext == null && dialoguePanel != null)
            Btnnext = dialoguePanel.GetComponentInChildren<Button>(true);
    }

    /// <summary>Places the dialogue box along the bottom of the screen (lower third).</summary>
    public void ApplyLowerThirdLayout()
    {
        if (dialoguePanel == null)
            return;

        var rt = dialoguePanel.GetComponent<RectTransform>();
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 36f);
        rt.sizeDelta = new Vector2(980f, 200f);

        if (dialogueField != null)
        {
            var textRt = dialogueField.GetComponent<RectTransform>();
            if (textRt != null)
            {
                textRt.anchorMin = new Vector2(0.04f, 0.28f);
                textRt.anchorMax = new Vector2(0.96f, 0.94f);
                textRt.offsetMin = textRt.offsetMax = Vector2.zero;
            }
        }

        if (Btnnext != null)
        {
            var btnRt = Btnnext.GetComponent<RectTransform>();
            if (btnRt != null)
            {
                btnRt.anchorMin = new Vector2(0.5f, 0f);
                btnRt.anchorMax = new Vector2(0.5f, 0f);
                btnRt.pivot = new Vector2(0.5f, 0f);
                btnRt.anchoredPosition = new Vector2(0f, 12f);
                btnRt.sizeDelta = new Vector2(180f, 40f);
            }
        }
    }

    GameObject CreateDialoguePanel()
    {
        var canvas = NewUIRoot.Ensure();
        if (canvas == null)
            return null;

        var panel = new GameObject("VisitorDialogue", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 36f);
        rt.sizeDelta = new Vector2(980f, 200f);
        panel.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.22f, 0.94f);

        var textGo = new GameObject("BodyLabel", typeof(RectTransform));
        textGo.transform.SetParent(panel.transform, false);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.fontSize = 22f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.04f, 0.28f);
        trt.anchorMax = new Vector2(0.96f, 0.94f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        var btnGo = new GameObject("ContinueButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(panel.transform, false);
        var brt = btnGo.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0f);
        brt.anchorMax = new Vector2(0.5f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.anchoredPosition = new Vector2(0f, 12f);
        brt.sizeDelta = new Vector2(180f, 40f);
        btnGo.GetComponent<Image>().color = new Color(0.25f, 0.55f, 0.35f, 1f);

        var btnLabelGo = new GameObject("Label", typeof(RectTransform));
        btnLabelGo.transform.SetParent(btnGo.transform, false);
        var btnLabel = btnLabelGo.AddComponent<TextMeshProUGUI>();
        btnLabel.text = "Next";
        btnLabel.fontSize = 20f;
        btnLabel.alignment = TextAlignmentOptions.Center;
        var blrt = btnLabelGo.GetComponent<RectTransform>();
        blrt.anchorMin = Vector2.zero;
        blrt.anchorMax = Vector2.one;
        blrt.offsetMin = blrt.offsetMax = Vector2.zero;

        return panel;
    }

    /// <summary>Play a DialogueScriptable, then run a callback when it finishes.</summary>
    public void Play(DialogueScriptable data, Action finished = null)
    {
        dialogue = data;
        onFinished = finished;
        EnsureDialogueUI();
        ApplyLowerThirdLayout();
        if (Btnnext != null)
            Btnnext.gameObject.SetActive(true);
        StartDialogue();
    }

    public void StartDialogue()
    {
        EnsureDialogueUI();

        currentDialogueIndex = 0;
        waitingForGate = false;

        if (dialogue == null || dialogue.dialogue == null || dialogue.dialogue.Count == 0)
        {
            EndDialogue();
            return;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

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
        if (dialogue == null || currentDialogueIndex >= dialogue.dialogue.Count)
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

        // Show dialogue in the text field
        if (dialogueField != null)
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
        if (dialogue == null || currentDialogueIndex >= dialogue.dialogue.Count)
        {
            EndDialogue();
            return;
        }

        ShowCurrentDialogue();
    }

    /// <summary>Hide the panel immediately (used when P opens character select).</summary>
    public void ForceClose()
    {
        waitingForGate = false;
        EndDialogue();
    }

    private void EndDialogue()
    {
        if (dialogueField != null)
            dialogueField.text = "";

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        Debug.Log("Dialogue finished.");

        var finished = onFinished;
        onFinished = null;
        finished?.Invoke();
    }
}
