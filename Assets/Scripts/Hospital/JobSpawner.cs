using System.Collections.Generic;
using UnityEngine;

public class JobSpawner : MonoBehaviour
{

    [SerializeField] private GameObject[] spawnPoints;

    [SerializeField] private GameObject nursePrefab;
    [SerializeField] private GameObject janitorPrefab;
    [SerializeField] private GameObject doctorPrefab;
    [SerializeField] private GameObject managerPrefab;

    private List<JobSpawnPoint> nurseList = new List<JobSpawnPoint>();
    private List<JobSpawnPoint> janitorList = new List<JobSpawnPoint>();
    private List<JobSpawnPoint> doctorList = new List<JobSpawnPoint>();
    int nurseIndex = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < spawnPoints.Length; i++) {
            JobSpawnPoint jobSpawnPoint = spawnPoints[i].GetComponent<JobSpawnPoint>();
            if (jobSpawnPoint == null) continue;
            switch (jobSpawnPoint.locationType)
            {
                case LocationType.Nurse:
                    nurseList.Add(jobSpawnPoint);
                    break;
                case LocationType.Janitor:
                    janitorList.Add(jobSpawnPoint);
                    break;
                case LocationType.Doctor:
                    doctorList.Add(jobSpawnPoint);
                    break;
            }
        }
        //SpawnAtRandom(LocationType.Doctor);
        //SpawnAtRandom(LocationType.Nurse);
        //SpawnAtRandom(LocationType.Janitor);
        //int randomNumber = Random.Range(0, nurseList.Count);
        //int randomNum = nurseList.Count;
        //Instantiate(nursePrefab, nurseList[randomNumber].position, Quaternion.identity);
    }

    public void SpawnAtRandom(LocationType locationType)
    {
        List<JobSpawnPoint> listToUse = null;
        GameObject prefabToSpawn = null;

        switch (locationType)
        {
            case LocationType.Nurse:
                listToUse = nurseList;
                prefabToSpawn = nursePrefab;
                break;

            case LocationType.Doctor:
                listToUse = doctorList;
                prefabToSpawn = doctorPrefab;
                break;

            case LocationType.Janitor:
                listToUse = janitorList;
                prefabToSpawn = janitorPrefab;
                break;

            default:
                Debug.LogWarning("Unknown LocationType: " + locationType);
                return;
        }

        if (listToUse == null || listToUse.Count == 0)
        {
            Debug.LogWarning("No spawn points available for " + locationType);
            return;
        }

        int randomIndex = Random.Range(0, listToUse.Count);
        Instantiate(prefabToSpawn, listToUse[randomIndex].transform.position, Quaternion.identity);
    }

}
