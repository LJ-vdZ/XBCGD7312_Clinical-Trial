using UnityEngine;
using TMPro;

public class RoleTimerUI : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    void Update()
    {
        if (RoleTimerSystem.Instance == null || timerText == null)
            return;

        float time = RoleTimerSystem.Instance.GetRemainingTime();

        time = Mathf.Max(0f, time);

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        timerText.text = $"{minutes:00}:{seconds:00}";

        if (time <= 30f)
            timerText.color = Color.red;
        else
            timerText.color = Color.white;
    }
}