using UnityEngine;
using UnityEngine.UI;

public class ElectricityFlowVisualizer : MonoBehaviour
{
    [Header("All Flow Segments in Order")]
    public Image[] allSegments;

    [Header("Animation")]
    public float fillSpeed = 2.5f;        

    private float targetProgress = 0f;    
    private float currentProgress = 0f;   

    public void ResetFlow()
    {
        targetProgress = 0f;
        currentProgress = 0f;
        UpdateVisual();
    }

    //lock knob when correctly turned
    public void OnKnobLocked(int knobIndex)
    {
        //number of segments each knob unlocks
        int[] segmentsPerKnob = { 3, 2, 1, 1 };   // Knob 0 unlocks 3 segments etc.

        if (knobIndex < 0 || knobIndex >= segmentsPerKnob.Length) return;

        int segmentsToAdd = segmentsPerKnob[knobIndex];
        targetProgress += (float)segmentsToAdd / allSegments.Length;
        targetProgress = Mathf.Clamp01(targetProgress);
    }

    void Update()
    {
        //animate electric flow towards target
        if (Mathf.Abs(currentProgress - targetProgress) > 0.001f)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, fillSpeed * Time.deltaTime);
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        if (allSegments == null) return;

        float segmentProgress = currentProgress * allSegments.Length;

        for (int i = 0; i < allSegments.Length; i++)
        {
            if (allSegments[i] == null) continue;

            float fill = Mathf.Clamp01(segmentProgress - i);
            allSegments[i].fillAmount = fill;
        }
    }

    public void CompleteFlow()
    {
        targetProgress = 1f;
    }
}
