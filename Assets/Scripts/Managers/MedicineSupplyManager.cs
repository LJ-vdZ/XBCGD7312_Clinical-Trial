using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Tracks medicine box purchases and shelf bottle supply count for Doctor/Nurse treatment UI.
/// Manager purchase spawns Nurse_MedicineBox at MedBoxSpawnPoint / MedBoxSpawnPoint (1), etc.
/// Nurse unpacks the box (after open anim) to fill MedRow*C* slots with 4 of each med type.
/// Usable medicineCount only increases after the nurse finishes the sorting mini-game.
/// </summary>
public class MedicineSupplyManager : MonoBehaviour
{
    public static MedicineSupplyManager Instance;

    public const int BoxPrice = 500;
    public const string MedBoxSpawnPointName = "MedBoxSpawnPoint";
    public const string MedicineRackName = "MedicineGame_Rack";
    public const int MedicinesPerSortedBox = 12;
    const string PropsFolder = "Assets/Prefabs/NurseMiniGameProps";

    static readonly string[] SlotNames =
    {
        "MedRow1C1", "MedRow1C2", "MedRow1C3", "MedRow1C4",
        "MedRow2C1", "MedRow2C2", "MedRow2C3", "MedRow2C4",
        "MedRow3C1", "MedRow3C2", "MedRow3C3", "MedRow3C4"
    };

    [Header("Prefabs / Spawns")]
    public GameObject medicineBoxPrefab;
    public Transform storageSpawnPoint;
    public Transform medicineRack;

    public GameObject medBottle1Prefab;
    public GameObject medBottle2Prefab;
    public GameObject pillBoxPrefab;

    [Header("State")]
    public int medicineCount = 6;
    public int maxMedicineCount = 24;

    public static System.Action OnSupplyChanged;
    public static System.Action OnBoxPurchased;

    readonly List<GameObject> shelfMedicines = new List<GameObject>();
    readonly List<Transform> medBoxSpawnPoints = new List<Transform>();

    /// <summary>True after unpack until the nurse completes sorting (grants supply once).</summary>
    bool pendingSortReward;

    public bool ShelfHasMedicines
    {
        get
        {
            shelfMedicines.RemoveAll(m => m == null);
            return shelfMedicines.Count > 0;
        }
    }

    public bool HasPendingSortReward => pendingSortReward;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        AutoFindReferences();
        OnSupplyChanged?.Invoke();
    }

    public void AutoFindReferencesPublic() => AutoFindReferences();

    void AutoFindReferences()
    {
        CollectMedBoxSpawnPoints();

        if (storageSpawnPoint == null && medBoxSpawnPoints.Count > 0)
            storageSpawnPoint = medBoxSpawnPoints[0];

        if (medicineRack == null)
        {
            var rack = GameObject.Find(MedicineRackName);
            if (rack != null) medicineRack = rack.transform;
        }

        if (medicineBoxPrefab == null)
            medicineBoxPrefab = LoadNurseProp("Nurse_MedicineBox");
        if (medBottle1Prefab == null)
            medBottle1Prefab = LoadNurseProp("MedBottle1");
        if (medBottle2Prefab == null)
            medBottle2Prefab = LoadNurseProp("MedBottle2");
        if (pillBoxPrefab == null)
            pillBoxPrefab = LoadNurseProp("PillBox");
    }

    void CollectMedBoxSpawnPoints()
    {
        medBoxSpawnPoints.Clear();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            // MedBoxSpawnPoint, MedBoxSpawnPoint (1), …
            if (t.name == MedBoxSpawnPointName || t.name.StartsWith(MedBoxSpawnPointName + " "))
                medBoxSpawnPoints.Add(t);
        }

        medBoxSpawnPoints.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
    }

    Transform FindFreeMedBoxSpawnPoint()
    {
        CollectMedBoxSpawnPoints();
        if (medBoxSpawnPoints.Count == 0)
            return storageSpawnPoint;

        foreach (var point in medBoxSpawnPoints)
        {
            if (point == null) continue;
            if (!SpawnPointIsOccupied(point))
                return point;
        }

        // All occupied — fall back to first point rather than failing the purchase.
        return medBoxSpawnPoints[0];
    }

    static bool SpawnPointIsOccupied(Transform point)
    {
        if (point == null) return true;
        for (int i = 0; i < point.childCount; i++)
        {
            var child = point.GetChild(i);
            if (child != null && child.GetComponent<MedicineBoxInteractable>() != null)
                return true;
        }
        return false;
    }

    GameObject LoadNurseProp(string prefabName)
    {
        var fromResources = Resources.Load<GameObject>("NurseMiniGameProps/" + prefabName);
        if (fromResources != null) return fromResources;

        fromResources = Resources.Load<GameObject>(prefabName);
        if (fromResources != null) return fromResources;

#if UNITY_EDITOR
        string path = $"{PropsFolder}/{prefabName}.prefab";
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset != null) return asset;
#endif
        return null;
    }

    public bool CanAffordBox() => HospitalStatsManager.Instance != null;

    public bool TryPurchaseBox()
    {
        if (HospitalStatsManager.Instance == null) return false;
        if (!HospitalStatsManager.Instance.SpendMoney(BoxPrice))
            return false;

        SpawnPurchasedBox();
        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("purchase");
        OnSupplyChanged?.Invoke();
        OnBoxPurchased?.Invoke();
        return true;
    }

    public void SpawnPurchasedBox()
    {
        AutoFindReferences();
        if (medicineBoxPrefab == null)
        {
            Debug.LogWarning("Nurse_MedicineBox prefab missing (Assets/Prefabs/NurseMiniGameProps).");
            return;
        }

        Transform spawn = FindFreeMedBoxSpawnPoint();
        if (spawn == null)
        {
            Debug.LogWarning($"Scene is missing empty GameObject '{MedBoxSpawnPointName}'.");
            return;
        }

        GameObject box = Instantiate(
            medicineBoxPrefab,
            spawn.position,
            spawn.rotation,
            spawn);
        box.name = "Nurse_MedicineBox";

        // Box open anim must wait for Nurse E — keep animators off until then.
        foreach (var anim in box.GetComponentsInChildren<Animator>(true))
        {
            anim.enabled = false;
            anim.speed = 0f;
        }

        if (box.GetComponent<MedicineBoxInteractable>() == null)
            box.AddComponent<MedicineBoxInteractable>();

        EnsureInteractTrigger(box);

        if (box.GetComponent<InteractableTrigger>() == null)
            box.AddComponent<InteractableTrigger>();
    }

    /// <summary>
    /// Spawns 4 of each medicine type across MedRow*C* points (shuffled).
    /// Called after the medicine box open animation finishes.
    /// Does not change medicineCount — that happens in GrantSortedSupplyAfterOrganizer.
    /// </summary>
    public void SpawnMedicinesOnShelfSlots()
    {
        AutoFindReferences();
        ClearShelfMedicines();
        pendingSortReward = true;

        var anchors = ResolveSlotAnchors();
        if (anchors.Count < SlotNames.Length)
        {
            Debug.LogWarning(
                "Missing MedRow*C* spawn points. Expected MedRow1C1..MedRow3C4 in the scene.");
        }

        var pool = new List<MedicineOrganizerMinigame.MedType>(12);
        for (int i = 0; i < 4; i++)
        {
            pool.Add(MedicineOrganizerMinigame.MedType.Bottle1);
            pool.Add(MedicineOrganizerMinigame.MedType.Bottle2);
            pool.Add(MedicineOrganizerMinigame.MedType.PillBox);
        }

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int count = Mathf.Min(pool.Count, anchors.Count);
        for (int i = 0; i < count; i++)
        {
            var type = pool[i];
            var anchor = anchors[i];
            int row = i / 4;
            int col = i % 4;

            GameObject prefab = GetPrefab(type);
            GameObject view;
            if (prefab != null)
            {
                view = Instantiate(prefab, anchor.position, anchor.rotation, anchor);
                view.transform.localPosition = Vector3.zero;
                view.transform.localRotation = Quaternion.identity;
            }
            else
            {
                view = GameObject.CreatePrimitive(PrimitiveType.Cube);
                view.transform.SetParent(anchor, false);
                view.transform.localPosition = Vector3.zero;
                view.transform.localScale = Vector3.one * 0.2f;
            }

            view.name = $"{type}_R{row + 1}C{col + 1}";

            var click = view.GetComponent<MedicineClickable>();
            if (click == null) click = view.AddComponent<MedicineClickable>();
            click.Setup(null, row, col, type);
            MedicineOrganizerMinigame.EnsureMedicineClickCollider(view);

            RegisterShelfMedicine(view);
        }

        if (MedicineOrganizerMinigame.Instance != null)
            MedicineOrganizerMinigame.Instance.RebuildSlotsFromShelf();
    }

    /// <summary>
    /// Called when the nurse finishes sorting. Grants +12 supply once per unpacked box.
    /// Sorted medicines stay on the shelf until treatments consume them.
    /// </summary>
    public void GrantSortedSupplyAfterOrganizer()
    {
        if (!pendingSortReward)
            return;

        pendingSortReward = false;
        AddMedicineCount(MedicinesPerSortedBox);
    }

    List<Transform> ResolveSlotAnchors()
    {
        var list = new List<Transform>(SlotNames.Length);
        foreach (var name in SlotNames)
        {
            var go = GameObject.Find(name);
            if (go != null)
                list.Add(go.transform);
            else
                Debug.LogWarning($"Missing medicine slot spawn point: {name}");
        }
        return list;
    }

    GameObject GetPrefab(MedicineOrganizerMinigame.MedType type)
    {
        switch (type)
        {
            case MedicineOrganizerMinigame.MedType.Bottle1: return medBottle1Prefab;
            case MedicineOrganizerMinigame.MedType.Bottle2: return medBottle2Prefab;
            default: return pillBoxPrefab;
        }
    }

    public static void EnsureInteractTrigger(GameObject go)
    {
        if (go == null) return;

        // Prefer a dedicated trigger volume so MeshColliders (non-convex) still work with E.
        var existing = go.GetComponents<Collider>();
        bool hasTrigger = false;
        foreach (var c in existing)
        {
            if (c != null && c.isTrigger)
            {
                hasTrigger = true;
                break;
            }
        }

        if (!hasTrigger)
        {
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = Vector3.one * 1.4f;
            box.center = Vector3.up * 0.4f;
        }
    }

    public void ClearShelfMedicines()
    {
        shelfMedicines.RemoveAll(m => m == null);
        for (int i = shelfMedicines.Count - 1; i >= 0; i--)
        {
            var go = shelfMedicines[i];
            shelfMedicines.RemoveAt(i);
            if (go != null) Destroy(go);
        }
        // Do not change medicineCount here — usable supply only goes up after the nurse finishes sorting.
        OnSupplyChanged?.Invoke();
    }

    public void RegisterShelfMedicine(GameObject med)
    {
        if (med != null && !shelfMedicines.Contains(med))
            shelfMedicines.Add(med);
        // Shelf props are unsorted stock. Usable medicineCount is added in MedicineOrganizerMinigame.
        OnSupplyChanged?.Invoke();
    }

    public void UnregisterShelfMedicine(GameObject med)
    {
        shelfMedicines.Remove(med);
        OnSupplyChanged?.Invoke();
    }

    public void RaidRandomMedicines(int amount = 3)
    {
        shelfMedicines.RemoveAll(m => m == null);
        int toDestroy = Mathf.Min(amount, shelfMedicines.Count);
        for (int i = 0; i < toDestroy; i++)
        {
            int idx = Random.Range(0, shelfMedicines.Count);
            var go = shelfMedicines[idx];
            shelfMedicines.RemoveAt(idx);
            if (go != null) Destroy(go);
        }

        // Also remove from usable supply when stock is stolen.
        medicineCount = Mathf.Max(0, medicineCount - amount);
        OnSupplyChanged?.Invoke();
    }

    public bool TryConsumeMedicine(int amount = 1)
    {
        if (medicineCount < amount) return false;
        medicineCount -= amount;
        for (int i = 0; i < amount; i++)
        {
            shelfMedicines.RemoveAll(m => m == null);
            if (shelfMedicines.Count == 0) break;
            var go = shelfMedicines[shelfMedicines.Count - 1];
            shelfMedicines.RemoveAt(shelfMedicines.Count - 1);
            if (go != null) Destroy(go);
        }
        OnSupplyChanged?.Invoke();
        return true;
    }

    public void AddMedicineCount(int amount)
    {
        medicineCount = Mathf.Clamp(medicineCount + amount, 0, maxMedicineCount);
        OnSupplyChanged?.Invoke();
    }
}
