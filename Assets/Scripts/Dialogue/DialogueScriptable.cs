using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "Dialogue/Dialogue")]
public class DialogueScriptable : ScriptableObject
{
    public enum DialogueType
    {
        Simple,
        Gate
    }

    [System.Serializable]
    public class DialogueEntry
    {
        public DialogueType type;

        [TextArea(3, 5)]
        public string dialogueText;
    }

    public List<DialogueEntry> dialogue = new List<DialogueEntry>();
}
