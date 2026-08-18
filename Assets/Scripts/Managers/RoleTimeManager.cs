using UnityEngine;

public class RoleTimeManager : MonoBehaviour
{
    public int totalMinutes = 6;

    public int nurseMinutes;
    public int doctorMinutes;
    public int janitorMinutes;

    public JobSpawner jobSpawner;

    public void SetAllocation(int nurse, int doctor, int janitor)
    {
        if (nurse + doctor + janitor > totalMinutes)
        {
            Debug.Log("Too many minutes allocated!");
            return;
        }

        nurseMinutes = nurse;
        doctorMinutes = doctor;
        janitorMinutes = janitor;

        Debug.Log($"Allocated: Nurse {nurse}, Doctor {doctor}, Janitor {janitor}");
    }

    public void ConfirmAllocation()
    {
        Debug.Log("Spawning jobs from allocation...");

        SpawnJobs(LocationType.Nurse, nurseMinutes);
        SpawnJobs(LocationType.Doctor, doctorMinutes);
        SpawnJobs(LocationType.Janitor, janitorMinutes);
    }

    void SpawnJobs(LocationType type, int count)
    {
        for (int i = 0; i < count; i++)
        {
            jobSpawner.SpawnAtRandom(type);
        }
    }
}