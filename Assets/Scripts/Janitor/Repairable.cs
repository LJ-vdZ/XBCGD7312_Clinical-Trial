using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;

public class Repairable : MonoBehaviour, IInteractable
{
    [Header("Repair Settings")]
    public int resourceCost = 25;
    public float comfortGain = 18f;

    [Header("Visual Replacement (Optional)")]
    public GameObject repairedPrefab;

    [Header("Repair Feedback")]
    public float repairDelay = 1.2f;

    //public int benchX;
    //public int benchY;
    //public int benchZ;

    public int rotationX;
    public int rotationY;   
    public int rotationZ;

    public float offsetY;

    Quaternion Rotation;
    Quaternion benchRotation;

    public bool CanInteractWhenLocked => false;

    public void Interact(GameObject player, RoleType role)
    {
        if (role != RoleType.Janitor)
        {
            Debug.Log("Only a Janitor can repair this!");
            return;
        }

        if (HospitalStatsManager.Instance == null)
        {
            Debug.LogError("HospitalStatsManager missing!");
            return;
        }

        if (HospitalStatsManager.Instance.SpendMoney(resourceCost))
        {
            StartCoroutine(DoRepair());
        }
        else
        {
            Debug.Log("Not enough money. Ask the Manager.");
        }
    }

    private IEnumerator DoRepair()
    {
        yield return new WaitForSeconds(repairDelay);

        HospitalStatsManager.Instance.ChangeComfort(comfortGain);

        Debug.Log($"Repair completed! +{comfortGain} Comfort");

        ReplaceWithRepairedVersion();
    }

    private void ReplaceWithRepairedVersion()
    {
        Rotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
        transform.position = new Vector3(transform.position.x,transform.position.y + offsetY,transform.position.z);

        if (repairedPrefab != null)
            Instantiate(repairedPrefab, transform.position, Rotation);

        Destroy(gameObject);
    }
}