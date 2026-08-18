using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DiagnosisMinigame : MonoBehaviour
{
    [Header("UI")]
    public GameObject minigameUI;
    public TextMeshProUGUI dialogueText;
    public Button nextButton;

    [Header("Diagnosis Buttons")]
    public Button[] diagnosisButtons;

    private string correctDiagnosis;
    private GameObject currentPlayer;
    private bool isPlaying = false;

    private List<string> currentDialogue;
    private int dialogueIndex = 0;

    public void StartGame(GameObject player)
    {
        // Legacy DoctorUI - patient care now uses PatientInteractable / NewUIRoot panel.
        Debug.LogWarning("DiagnosisMinigame is deprecated. Use the new Doctor Patient Care panel.");
        if (minigameUI != null)
            minigameUI.SetActive(false);
    }

    void GenerateCase()
    {
        int caseIndex = Random.Range(0, 3);

        switch (caseIndex)
        {
            case 0:
                currentDialogue = new List<string>
                {
                    "Patient: Hi doctor� I�ve been feeling really tired.",
                    "Doctor: How long have you felt this way?",
                    "Patient: A few days now.",
                    "Patient: I also have a fever and a cough."
                };
                correctDiagnosis = "Flu";
                break;

            case 1:
                currentDialogue = new List<string>
                {
                    "Patient: Doctor, I have this pain in my chest.",
                    "Doctor: When does it happen?",
                    "Patient: Mostly when I move or breathe deeply.",
                    "Patient: I also feel short of breath."
                };
                correctDiagnosis = "Heart Issue";
                break;

            case 2:
                currentDialogue = new List<string>
                {
                    "Patient: I�ve had a terrible headache all day.",
                    "Doctor: Anything making it worse?",
                    "Patient: Bright lights really hurt my eyes.",
                    "Patient: I feel like I need to lie down."
                };
                correctDiagnosis = "Migraine";
                break;
        }

        dialogueIndex = 0;

        // Hide diagnosis buttons until dialogue finishes
        SetDiagnosisButtonsActive(false);
    }

    void ShowDialogue()
    {
        if (dialogueIndex < currentDialogue.Count)
        {
            dialogueText.text = currentDialogue[dialogueIndex];
        }
    }

    void NextDialogue()
    {
        dialogueIndex++;

        if (dialogueIndex < currentDialogue.Count)
        {
            ShowDialogue();
        }
        else
        {
            
            dialogueText.text = "What is your diagnosis?";
            nextButton.gameObject.SetActive(false);
            SetDiagnosisButtonsActive(true);
        }
    }

    void SetDiagnosisButtonsActive(bool state)
    {
        foreach (Button btn in diagnosisButtons)
        {
            btn.gameObject.SetActive(state);
        }
    }

    public void SelectDiagnosis(string chosenDiagnosis)
    {
        if (!isPlaying) return;

        EndGame(chosenDiagnosis == correctDiagnosis);
    }

    void EndGame(bool success)
    {
        isPlaying = false;
        minigameUI.SetActive(false);

        if (currentPlayer != null)
        {
            var movement = currentPlayer.GetComponent<SimplePlayerMovement>();
            if (movement != null)
                movement.SetControlsEnabled(true);
        }

        if (success)
        {
            Debug.Log("Correct diagnosis!");
            HospitalStatsManager.Instance.ChangeComfort(+10);
        }
        else
        {
            Debug.Log("Wrong diagnosis!");
            HospitalStatsManager.Instance.ChangeComfort(-10);
            HospitalStatsManager.Instance.ChangeMorale(-5);
        }
    }
}