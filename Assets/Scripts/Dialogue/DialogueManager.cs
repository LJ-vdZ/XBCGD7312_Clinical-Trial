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
    private Action<int> onLineShown;
    private bool nextButtonBound;

    public bool IsWaitingForGate => waitingForGate;
    public bool IsPlaying => dialoguePanel != null && dialoguePanel.activeSelf;
    public int CurrentDialogueIndex => currentDialogueIndex;

    private void Start()
    {
        EnsureDialogueUI();
        ApplyLowerThirdLayout();
        BindNextButton();

        if (Btngate != null)
            Btngate.onClick.AddListener(() => PassGate(true));

        if (dialogue != null)
            StartDialogue();
        else if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    public void RebindContinueButton()
    {
        EnsureDialogueUI();
        BindNextButton(force: true);
    }

    void BindNextButton(bool force = false)
    {
        if (Btnnext == null)
            return;

        if (nextButtonBound && !force)
            return;

        // VisitorInteractable also uses ContinueButton and clears listeners via BindButton.
        // Always reclaim the button when starting tutorial / DialogueScriptable playback.
        Btnnext.onClick.RemoveAllListeners();
        Btnnext.onClick.AddListener(NextDialogue);
        nextButtonBound = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.HookButton(Btnnext);
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

        // Always prefer the BodyLabel under the dialogue panel (not a button label).
        if (dialoguePanel != null)
        {
            var body = dialoguePanel.transform.Find("BodyLabel");
            if (body != null)
                dialogueField = body.GetComponent<TMP_Text>();

            if (dialogueField == null)
                dialogueField = ClinicalUIFactory.FindLabel(dialoguePanel.transform, "BodyLabel");

            // Re-resolve the button each time — another system may have rebound it.
            var btnT = dialoguePanel.transform.Find("ContinueButton");
            if (btnT != null)
                Btnnext = btnT.GetComponent<Button>();

            if (Btnnext == null)
                Btnnext = dialoguePanel.GetComponentInChildren<Button>(true);
        }

        BindNextButton();
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
        rt.sizeDelta = new Vector2(980f, 220f);

        if (dialogueField != null)
        {
            var textRt = dialogueField.GetComponent<RectTransform>();
            if (textRt != null)
            {
                textRt.anchorMin = new Vector2(0.04f, 0.32f);
                textRt.anchorMax = new Vector2(0.96f, 0.94f);
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;
                textRt.localScale = Vector3.one;
            }

            dialogueField.gameObject.SetActive(true);
            dialogueField.enabled = true;
            dialogueField.fontSize = 26f;
            dialogueField.enableWordWrapping = true;
            dialogueField.overflowMode = TextOverflowModes.Overflow;
            dialogueField.alignment = TextAlignmentOptions.Center;
            dialogueField.color = Color.white;
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
        rt.sizeDelta = new Vector2(980f, 220f);
        panel.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.22f, 0.94f);

        var textGo = new GameObject("BodyLabel", typeof(RectTransform));
        textGo.transform.SetParent(panel.transform, false);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.fontSize = 26f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableWordWrapping = true;
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.04f, 0.32f);
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
        btnLabel.text = "Continue";
        btnLabel.fontSize = 20f;
        btnLabel.alignment = TextAlignmentOptions.Center;
        var blrt = btnLabelGo.GetComponent<RectTransform>();
        blrt.anchorMin = Vector2.zero;
        blrt.anchorMax = Vector2.one;
        blrt.offsetMin = blrt.offsetMax = Vector2.zero;

        dialogueField = text;
        Btnnext = btnGo.GetComponent<Button>();
        return panel;
    }

    /// <summary>Play a DialogueScriptable, then run a callback when it finishes.</summary>
    public void Play(DialogueScriptable data, Action finished = null, bool showNextButton = true, Action<int> lineShown = null)
    {
        dialogue = data;
        onFinished = finished;
        onLineShown = lineShown;
        EnsureDialogueUI();
        BindNextButton(force: true);
        ApplyLowerThirdLayout();

        if (Btnnext != null)
        {
            Btnnext.gameObject.SetActive(showNextButton);
            Btnnext.interactable = true;
        }

        StartDialogue();

        // Gate lines never use Continue — hide even if caller forgot.
        if (IsWaitingForGate && Btnnext != null)
            Btnnext.gameObject.SetActive(false);
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
        {
            dialoguePanel.SetActive(true);
            dialoguePanel.transform.SetAsLastSibling();
        }

        ShowCurrentDialogue();
    }

    public void NextDialogue()
    {
        if (waitingForGate)
            return;

        currentDialogueIndex++;

        if (dialogue == null || currentDialogueIndex >= dialogue.dialogue.Count)
        {
            EndDialogue();
            return;
        }

        ShowCurrentDialogue();
    }

    private void ShowCurrentDialogue()
    {
        EnsureDialogueUI();

        DialogueScriptable.DialogueEntry currentDialogue =
            dialogue.dialogue[currentDialogueIndex];

        string line = currentDialogue.dialogueText ?? "";

        if (dialogueField != null)
        {
            dialogueField.gameObject.SetActive(true);
            dialogueField.enabled = true;
            dialogueField.text = line;
            dialogueField.ForceMeshUpdate();
        }

        Debug.Log(line);

        if (currentDialogue.type == DialogueScriptable.DialogueType.Gate)
        {
            waitingForGate = true;
            if (Btnnext != null)
                Btnnext.gameObject.SetActive(false);
        }
        else
        {
            waitingForGate = false;
        }

        onLineShown?.Invoke(currentDialogueIndex);
    }

    public void PassGate(bool completed)
    {
        if (!waitingForGate)
            return;

        if (!completed)
            return;

        waitingForGate = false;
        currentDialogueIndex++;

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

        onLineShown = null;
        var finished = onFinished;
        onFinished = null;
        finished?.Invoke();
    }
}
