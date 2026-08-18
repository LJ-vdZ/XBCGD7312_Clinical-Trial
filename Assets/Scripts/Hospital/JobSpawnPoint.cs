using UnityEngine;


public enum LocationType
{
    Janitor,
    Nurse,
    Doctor,
    Manager
}

public class JobSpawnPoint : MonoBehaviour
{

    public Vector3 position;
    public LocationType locationType;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        position = transform.position;
    }
}
