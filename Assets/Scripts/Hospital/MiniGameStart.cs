using UnityEngine;

public class MiniGameStart : MonoBehaviour
{
    public string jobType = "Nurse";

    public MainSceneUIManager mainSceneUIManager;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            if(jobType == "Manager")
            {
                //mainSceneUIManager.OnJobOpen();
                return;
            }
            Debug.Log("transtion to " + jobType + " mini-game");
            Destroy(gameObject);
        }
    }
}
