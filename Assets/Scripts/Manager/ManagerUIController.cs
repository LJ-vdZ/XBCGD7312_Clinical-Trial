using UnityEngine;

public class ManagerUIController : MonoBehaviour
{
    public GameObject uiPanel;
    public GameObject player;

    public void CloseUI()
    {
        uiPanel.SetActive(false);

        var movement = player.GetComponent<SimplePlayerMovement>();
        if (movement != null)
            movement.SetControlsEnabled(true);
    }
}