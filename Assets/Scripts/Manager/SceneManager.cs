using UnityEngine;
using UnityEngine.UI;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class SceneManager : MonoBehaviour
{
    void Start()
    {
        BindButton("StartButton", PlayGame);

        BindButton("ExitButton", Quit);

        BindButton("TutorialButton", PlayTutorial);
    }

    static void BindButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        var gameObject = GameObject.Find(objectName);

        if (gameObject == null) 
        {
            return;
        }

        var button = gameObject.GetComponent<Button>();

        if (button == null) 
        {
            return;
        }

        button.onClick.AddListener(action);
    }

    public void PlayGame()
    {
        UnitySceneManager.LoadScene("HospitalHubLevel");
    }

    public void PlayTutorial()
    {
        UnitySceneManager.LoadScene("TutorialScene");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
