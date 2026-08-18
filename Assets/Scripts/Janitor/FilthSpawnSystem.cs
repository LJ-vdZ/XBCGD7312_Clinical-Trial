using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns TrashGroup1 / TrashGroup2 at scene points SpawnDirtTrash (0)..(7).
/// Piles grow in a compact circular spiral around each point.
/// Sanitation drains faster as a spawn point accumulates more trash.
/// Janitor sweeps groups with E (mop).
/// </summary>
public class FilthSpawnSystem : MonoBehaviour
{
    public static FilthSpawnSystem Instance;

    [Header("Prefabs (Assets/Prefabs/Original)")]
    public GameObject trashGroup1;
    public GameObject trashGroup2;

    [Header("Timing")]
    [Tooltip("Seconds between each new trash group appearing at a random spawn point.")]
    public float spawnInterval = 8f;
    public int maxGroupsPerPoint = 14;

    [Header("Pile shape")]
    [Tooltip("How far later groups sit from the spawn point. Keep small so they pile, not scatter.")]
    public float spiralSpacing = 0.45f;
    [Tooltip("Vertical rise per extra group so the pile grows upward.")]
    public float stackHeight = 0.14f;

    [Header("Sanitation")]
    public float decayPerSecond = 0.12f;
    public float pileDecayBonus = 0.04f;

    class SpawnSite
    {
        public Transform point;
        public readonly List<GameObject> groups = new List<GameObject>();
    }

    readonly List<SpawnSite> sites = new List<SpawnSite>();
    float timer;

    void Awake() => Instance = this;

    void Start()
    {
        LoadPrefabsIfNeeded();
        CollectSpawnPoints();
        timer = 2f;
    }

    void LoadPrefabsIfNeeded()
    {
        if (trashGroup1 == null)
            trashGroup1 = LoadPrefab("TrashGroup1");
        if (trashGroup2 == null)
            trashGroup2 = LoadPrefab("TrashGroup2");

        if (trashGroup1 == null && trashGroup2 == null)
            Debug.LogError("FilthSpawnSystem: could not load TrashGroup1 / TrashGroup2 from Assets/Prefabs/Original.");
    }

    static GameObject LoadPrefab(string name)
    {
#if UNITY_EDITOR
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            $"Assets/Prefabs/Original/{name}.prefab");
        if (prefab != null) return prefab;
#endif
        return Resources.Load<GameObject>(name);
    }

    void CollectSpawnPoints()
    {
        sites.Clear();
        var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var found = new List<Transform>();

        foreach (var t in transforms)
        {
            if (t != null && IsDirtSpawnName(t.name))
                found.Add(t);
        }

        found.Sort((a, b) => ExtractIndex(a.name).CompareTo(ExtractIndex(b.name)));

        foreach (var t in found)
            sites.Add(new SpawnSite { point = t });

        if (sites.Count == 0)
        {
            Debug.LogError("FilthSpawnSystem: no SpawnDirtTrash (0)..(7) objects found.");
            return;
        }

        Debug.Log($"FilthSpawnSystem: using {sites.Count} SpawnDirtTrash points.");
    }

    static bool IsDirtSpawnName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return name == "SpawnDirtTrash"
               || name.StartsWith("SpawnDirtTrash ")
               || name.StartsWith("SpawnDirtTrash(");
    }

    static int ExtractIndex(string name)
    {
        int open = name.LastIndexOf('(');
        int close = name.LastIndexOf(')');
        if (open >= 0 && close > open
            && int.TryParse(name.Substring(open + 1, close - open - 1), out int n))
            return n;
        return 0;
    }

    void Update()
    {
        ApplySanitationDrain();

        if (sites.Count == 0) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = spawnInterval;
        SpawnOne();
    }

    void ApplySanitationDrain()
    {
        if (HospitalStatsManager.Instance == null) return;

        float drain = 0f;
        for (int i = 0; i < sites.Count; i++)
        {
            var site = sites[i];
            site.groups.RemoveAll(g => g == null);
            int n = site.groups.Count;
            if (n <= 0) continue;
            drain += decayPerSecond * n + pileDecayBonus * n * (n - 1);
        }

        if (drain > 0f)
            HospitalStatsManager.Instance.ChangeSanitation(-drain * Time.deltaTime);
    }

    void SpawnOne()
    {
        SpawnSite site = PickSite();
        if (site == null || site.point == null) return;

        GameObject prefab = PickPrefab();
        if (prefab == null) return;

        Vector3 pos = PilePosition(site.point.position, site.groups.Count);
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        var go = Instantiate(prefab, pos, rot);
        go.SetActive(true);
        go.name = $"{prefab.name}_{site.point.name}_{site.groups.Count}";
        go.transform.SetParent(site.point, true);

        EnsureSweepable(go);
        site.groups.Add(go);
    }

    SpawnSite PickSite()
    {
        if (sites.Count == 0) return null;

        var open = new List<SpawnSite>();
        for (int i = 0; i < sites.Count; i++)
        {
            var site = sites[i];
            if (site.point == null) continue;
            site.groups.RemoveAll(g => g == null);
            if (site.groups.Count < maxGroupsPerPoint)
                open.Add(site);
        }

        if (open.Count == 0) return null;
        return open[Random.Range(0, open.Count)];
    }

    GameObject PickPrefab()
    {
        bool has1 = trashGroup1 != null;
        bool has2 = trashGroup2 != null;
        if (has1 && has2)
            return Random.Range(0, 2) == 0 ? trashGroup1 : trashGroup2;
        return has1 ? trashGroup1 : trashGroup2;
    }

    Vector3 PilePosition(Vector3 origin, int index)
    {
        if (index <= 0)
            return origin;

        // Compact ring that grows slowly + stacks upward.
        const float goldenAngle = 2.399963229728653f;
        float radius = spiralSpacing * Mathf.Sqrt(index);
        float angle = index * goldenAngle + Random.Range(-0.25f, 0.25f);
        float y = origin.y + index * stackHeight + Random.Range(0f, 0.03f);
        return new Vector3(
            origin.x + Mathf.Cos(angle) * radius,
            y,
            origin.z + Mathf.Sin(angle) * radius);
    }

    static void EnsureSweepable(GameObject go)
    {
        if (go.GetComponent<DirtPile>() == null)
            go.AddComponent<DirtPile>();

        var col = go.GetComponent<Collider>();
        if (col == null)
        {
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 0.35f, 0f);
            box.size = new Vector3(2.4f, 1.2f, 2.4f);
        }
        else
        {
            col.isTrigger = true;
        }

        if (go.GetComponent<InteractableTrigger>() == null)
            go.AddComponent<InteractableTrigger>();
    }
}
