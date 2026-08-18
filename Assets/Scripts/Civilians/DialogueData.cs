using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public class Character
    {
        public string characterName;
        public Sprite portrait;
    }

    [System.Serializable]
    public struct DialogueLine
    {
        public Character character;

        [TextArea(3, 10)]
        public string text;
    }

    public DialogueLine[] lines;
}
