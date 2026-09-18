using UnityEngine;

/// <summary>
/// Simple flag so other scripts know we are in the tutorial.
/// HospitalHubLevel is never named TutorialScene, so it stays normal.
/// </summary>
public static class TutorialMode
{
    public static bool IsActive =>
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "TutorialScene";
}
