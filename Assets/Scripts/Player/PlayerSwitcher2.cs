using UnityEngine;

public class PlayerSwitcher2 : MonoBehaviour
{
    public JanitorController janitor;
    // Drag the other three controllers here when your teammates make them:
    // public NurseController nurse;
    // public DoctorController doctor;
    // public ManagerController manager;

    private MonoBehaviour currentActive;

    void Start()
    {
        // Start with Janitor (or change to 0)
        SwitchTo(janitor);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchTo(janitor);   // 1 = Janitor
        //if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchTo(nurse);
        //if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchTo(doctor);
        //if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchTo(manager);
    }

    public void SwitchTo(MonoBehaviour newController)
    {
        if (currentActive != null)
        {
            currentActive.enabled = false;
            if (currentActive is JanitorController j) j.isControlled = false;
            // Add similar lines for other characters
        }

        currentActive = newController;
        newController.enabled = true;

        if (newController is JanitorController jan) jan.isControlled = true;

        // Make camera follow the new character
        var camFollow = Camera.main.GetComponent<CameraFollow>();
        if (camFollow) camFollow.target = newController.transform;
    }
}
