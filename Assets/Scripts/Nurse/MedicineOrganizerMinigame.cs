using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// First-person shelf sorting on MedicineGame_Rack: click medicine A then B to swap.
/// Selected med lifts; swap animates both toward each other's spawn points.
/// Complete when each shelf row contains only one medicine type.
/// </summary>
public class MedicineOrganizerMinigame : MonoBehaviour
{
    public static MedicineOrganizerMinigame Instance;

    public enum MedType { Bottle1, Bottle2, PillBox }

    [System.Serializable]
    public class ShelfSlot
    {
        public int row;
        public int col;
        public Transform anchor;
        public MedType type;
        public GameObject view;
        public Vector3 baseScale;
        public Vector3 restLocalPos;
    }

    [Header("View")]
    public Transform firstPersonAnchor;
    public Camera gameplayCamera;
    public Vector3 rackCameraPosition = new Vector3(-88.40164f, 1.127f, -14.525f);

    [Header("Select / Swap")]
    public float selectLiftHeight = 0.12f;
    public float selectLiftSeconds = 0.12f;
    public float swapMoveSeconds = 0.35f;

    const int Rows = 3;
    const int Cols = 4;

    static readonly string[] SlotNames =
    {
        "MedRow1C1", "MedRow1C2", "MedRow1C3", "MedRow1C4",
        "MedRow2C1", "MedRow2C2", "MedRow2C3", "MedRow2C4",
        "MedRow3C1", "MedRow3C2", "MedRow3C3", "MedRow3C4"
    };

    readonly List<ShelfSlot> slots = new List<ShelfSlot>();
    ShelfSlot selected;
    bool active;
    bool isAnimating;
    GameObject playerRef;
    Vector3 savedCamPos;
    Quaternion savedCamRot;
    Transform savedCamParent;
    bool savedCamEnabled;
    CameraFollow camFollow;
    readonly List<Renderer> hiddenPlayerRenderers = new List<Renderer>();

    void Awake()
    {
        Instance = this;
    }

    public bool HasShelfMedsReady()
    {
        if (slots.Count == 0)
            RebuildSlotsFromShelf();
        return slots.Count > 0;
    }

    public void RebuildSlotsFromShelf()
    {
        slots.Clear();
        for (int i = 0; i < SlotNames.Length; i++)
        {
            int row = i / Cols;
            int col = i % Cols;
            var anchorGo = GameObject.Find(SlotNames[i]);
            if (anchorGo == null) continue;

            Transform anchor = anchorGo.transform;
            MedicineClickable click = anchor.GetComponentInChildren<MedicineClickable>(true);
            if (click == null) continue;

            click.Setup(this, row, col, click.type);
            // Rest pose is flush on the spawn point.
            click.transform.localPosition = Vector3.zero;
            EnsureMedicineClickCollider(click.gameObject);

            slots.Add(new ShelfSlot
            {
                row = row,
                col = col,
                anchor = anchor,
                type = click.type,
                view = click.gameObject,
                baseScale = click.transform.localScale,
                restLocalPos = Vector3.zero
            });
        }
    }

    /// <summary>
    /// Builds a reliable click volume from the visible mesh bounds.
    /// Prefab colliders are often offset / oversized and the rack blocks single Raycasts.
    /// </summary>
    public static void EnsureMedicineClickCollider(GameObject view)
    {
        if (view == null) return;

        Transform existing = view.transform.Find("ClickProxy");
        if (existing != null)
        {
            // Keep proxy enabled and sized.
            FitClickProxy(existing.gameObject, view);
            return;
        }

        // Prefab root colliders are often badly offset — disable them so they don't steal/miss hits.
        foreach (var col in view.GetComponentsInChildren<Collider>(true))
        {
            if (col == null) continue;
            if (col.gameObject.name == "ClickProxy") continue;
            col.enabled = false;
        }

        var proxy = new GameObject("ClickProxy");
        proxy.transform.SetParent(view.transform, false);
        proxy.layer = view.layer;
        FitClickProxy(proxy, view);
    }

    static void FitClickProxy(GameObject proxy, GameObject view)
    {
        var renderers = view.GetComponentsInChildren<Renderer>(true);
        var box = proxy.GetComponent<BoxCollider>();
        if (box == null) box = proxy.AddComponent<BoxCollider>();
        box.isTrigger = false;
        box.enabled = true;

        if (renderers == null || renderers.Length == 0)
        {
            proxy.transform.localPosition = Vector3.zero;
            proxy.transform.localRotation = Quaternion.identity;
            proxy.transform.localScale = Vector3.one;
            box.center = Vector3.zero;
            box.size = Vector3.one * 0.25f;
            return;
        }

        Bounds world = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].enabled)
                world.Encapsulate(renderers[i].bounds);
        }

        // Place proxy in world at bounds center, then keep as child.
        proxy.transform.SetParent(null, true);
        proxy.transform.position = world.center;
        proxy.transform.rotation = Quaternion.identity;
        proxy.transform.localScale = Vector3.one;
        proxy.transform.SetParent(view.transform, true);

        Vector3 lossy = proxy.transform.lossyScale;
        box.center = Vector3.zero;
        box.size = new Vector3(
            world.size.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
            world.size.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
            world.size.z / Mathf.Max(0.0001f, Mathf.Abs(lossy.z)));

        // Slightly enlarge so clicks are forgiving.
        box.size *= 1.15f;
    }

    public void BeginAtRack(GameObject player)
    {
        if (active) return;

        RebuildSlotsFromShelf();
        if (slots.Count == 0)
        {
            Debug.LogWarning("No medicines on MedRow slots — unpack a Nurse_MedicineBox first.");
            return;
        }

        playerRef = player;
        EnterFirstPerson();
        active = true;
        isAnimating = false;

        if (MiniGameTimerUI.Instance != null)
            MiniGameTimerUI.Instance.StartTimer("Organize Medicine", 90f, ForceExit);
    }

    public void OnMedicineClicked(MedicineClickable click)
    {
        if (!active || isAnimating || click == null) return;

        ShelfSlot slot = FindSlotForClick(click);
        if (slot == null || slot.view == null) return;

        if (selected == null)
        {
            selected = slot;
            StartCoroutine(LiftSelected(slot, true));
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play("Medicine Select");
            return;
        }

        if (selected == slot)
        {
            StartCoroutine(DeselectRoutine(slot));
            return;
        }

        StartCoroutine(SwapRoutine(selected, slot));
        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("Medicine Swap");
    }

    ShelfSlot FindSlotForClick(MedicineClickable click)
    {
        if (click == null) return null;

        // Prefer exact component / view match (row/col can desync after swaps).
        foreach (var s in slots)
        {
            if (s?.view == null) continue;
            if (s.view == click.gameObject) return s;
            var onView = s.view.GetComponent<MedicineClickable>();
            if (onView == click) return s;
            if (click.transform.IsChildOf(s.view.transform)) return s;
        }

        return slots.Find(s => s.row == click.row && s.col == click.col);
    }

    IEnumerator LiftSelected(ShelfSlot slot, bool up)
    {
        if (slot?.view == null) yield break;

        Transform t = slot.view.transform;
        Vector3 from = t.localPosition;
        Vector3 to = up
            ? slot.restLocalPos + Vector3.up * selectLiftHeight
            : slot.restLocalPos;

        isAnimating = true;
        float dur = Mathf.Max(0.01f, selectLiftSeconds);
        float elapsed = 0f;
        while (elapsed < dur)
        {
            if (slot.view == null) break;
            elapsed += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, elapsed / dur);
            t.localPosition = Vector3.Lerp(from, to, u);
            yield return null;
        }
        if (slot.view != null)
            t.localPosition = to;
        isAnimating = false;
    }

    IEnumerator DeselectRoutine(ShelfSlot slot)
    {
        yield return LiftSelected(slot, false);
        selected = null;
    }

    IEnumerator SwapRoutine(ShelfSlot slotA, ShelfSlot slotB)
    {
        if (slotA?.view == null || slotB?.view == null)
        {
            selected = null;
            yield break;
        }

        isAnimating = true;

        GameObject viewA = slotA.view;
        GameObject viewB = slotB.view;
        Transform anchorA = slotA.anchor;
        Transform anchorB = slotB.anchor;
        MedType typeA = slotA.type;
        MedType typeB = slotB.type;
        Vector3 scaleA = slotA.baseScale;
        Vector3 scaleB = slotB.baseScale;

        // World-space travel: A → B's spawn, B → A's spawn.
        Vector3 startA = viewA.transform.position;
        Vector3 startB = viewB.transform.position;
        Vector3 endA = anchorB.position;
        Vector3 endB = anchorA.position;
        Quaternion rotStartA = viewA.transform.rotation;
        Quaternion rotStartB = viewB.transform.rotation;
        Quaternion rotEndA = anchorB.rotation;
        Quaternion rotEndB = anchorA.rotation;

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("interact");

        float dur = Mathf.Max(0.01f, swapMoveSeconds);
        float elapsed = 0f;
        while (elapsed < dur)
        {
            if (viewA == null || viewB == null) break;
            elapsed += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, elapsed / dur);
            viewA.transform.position = Vector3.Lerp(startA, endA, u);
            viewB.transform.position = Vector3.Lerp(startB, endB, u);
            viewA.transform.rotation = Quaternion.Slerp(rotStartA, rotEndA, u);
            viewB.transform.rotation = Quaternion.Slerp(rotStartB, rotEndB, u);
            yield return null;
        }

        // Re-parent to the destination spawn points and settle at rest.
        if (viewA != null)
        {
            viewA.transform.SetParent(anchorB, true);
            viewA.transform.localPosition = Vector3.zero;
            viewA.transform.localRotation = Quaternion.identity;
        }
        if (viewB != null)
        {
            viewB.transform.SetParent(anchorA, true);
            viewB.transform.localPosition = Vector3.zero;
            viewB.transform.localRotation = Quaternion.identity;
        }

        // Slot A now holds what was B, and vice versa.
        slotA.type = typeB;
        slotA.view = viewB;
        slotA.baseScale = scaleB;
        slotA.restLocalPos = Vector3.zero;

        slotB.type = typeA;
        slotB.view = viewA;
        slotB.baseScale = scaleA;
        slotB.restLocalPos = Vector3.zero;

        var ca = slotA.view != null ? slotA.view.GetComponent<MedicineClickable>() : null;
        var cb = slotB.view != null ? slotB.view.GetComponent<MedicineClickable>() : null;
        if (ca != null) ca.Setup(this, slotA.row, slotA.col, slotA.type);
        if (cb != null) cb.Setup(this, slotB.row, slotB.col, slotB.type);

        selected = null;
        isAnimating = false;

        if (IsOrganized())
            CompleteSuccess();
    }

    bool IsOrganized()
    {
        for (int r = 0; r < Rows; r++)
        {
            MedType? rowType = null;
            foreach (var s in slots)
            {
                if (s.row != r) continue;
                if (rowType == null) rowType = s.type;
                else if (s.type != rowType.Value) return false;
            }
        }

        var used = new HashSet<MedType>();
        for (int r = 0; r < Rows; r++)
        {
            MedType t = MedType.Bottle1;
            foreach (var s in slots)
                if (s.row == r) { t = s.type; break; }
            if (!used.Add(t)) return false;
        }
        return true;
    }

    void CompleteSuccess()
    {
        if (HospitalStatsManager.Instance != null)
        {
            HospitalStatsManager.Instance.ChangeMorale(+12f);
            HospitalStatsManager.Instance.ChangeComfort(+10f);
        }
        if (MedicineSupplyManager.Instance != null)
            MedicineSupplyManager.Instance.AddMedicineCount(4);

        CharacterMoraleSystem.NotifyOrganizerSuccess();
        ExitMinigame();
    }

    public void AbortIfActive()
    {
        ForceExit();
    }

    void ForceExit()
    {
        if (active) ExitMinigame();
    }

    static Vector3 GetRackCenter(Transform rack)
    {
        var renderers = rack.GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b.center;
        }

        var colliders = rack.GetComponentsInChildren<Collider>();
        if (colliders != null && colliders.Length > 0)
        {
            Bounds b = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    b.Encapsulate(colliders[i].bounds);
            }
            return b.center;
        }

        return rack.position;
    }

    void EnterFirstPerson()
    {
        camFollow = FindFirstObjectByType<CameraFollow>();
        gameplayCamera = Camera.main;
        if (gameplayCamera == null) return;

        savedCamPos = gameplayCamera.transform.position;
        savedCamRot = gameplayCamera.transform.rotation;
        savedCamParent = gameplayCamera.transform.parent;
        if (camFollow != null)
        {
            savedCamEnabled = camFollow.enabled;
            camFollow.enabled = false;
            camFollow.LockCursor(false);
        }

        Transform rack = null;
        if (MedicineSupplyManager.Instance != null)
        {
            MedicineSupplyManager.Instance.AutoFindReferencesPublic();
            rack = MedicineSupplyManager.Instance.medicineRack;
        }
        if (rack == null)
        {
            var go = GameObject.Find(MedicineSupplyManager.MedicineRackName);
            if (go != null) rack = go.transform;
        }

        if (firstPersonAnchor != null)
        {
            gameplayCamera.transform.SetParent(null);
            gameplayCamera.transform.position = firstPersonAnchor.position;
            gameplayCamera.transform.rotation = firstPersonAnchor.rotation;
        }
        else
        {
            Vector3 lookPos = rack != null ? GetRackCenter(rack) : rackCameraPosition + Vector3.forward;
            gameplayCamera.transform.SetParent(null);
            gameplayCamera.transform.position = rackCameraPosition;
            gameplayCamera.transform.LookAt(lookPos);
        }

        if (playerRef != null)
        {
            var move = playerRef.GetComponent<SimplePlayerMovement>();
            if (move != null) move.SetControlsEnabled(false);
            SetPlayerVisible(false);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void SetPlayerVisible(bool visible)
    {
        if (visible)
        {
            foreach (var r in hiddenPlayerRenderers)
            {
                if (r != null) r.enabled = true;
            }
            hiddenPlayerRenderers.Clear();
            return;
        }

        hiddenPlayerRenderers.Clear();
        if (playerRef == null) return;

        var renderers = playerRef.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r == null || !r.enabled) continue;
            r.enabled = false;
            hiddenPlayerRenderers.Add(r);
        }
    }

    void ExitMinigame()
    {
        StopAllCoroutines();
        isAnimating = false;
        active = false;

        if (selected != null && selected.view != null)
            selected.view.transform.localPosition = selected.restLocalPos;
        selected = null;

        if (MiniGameTimerUI.Instance != null)
            MiniGameTimerUI.Instance.StopTimer();

        if (gameplayCamera != null)
        {
            gameplayCamera.transform.SetParent(savedCamParent);
            gameplayCamera.transform.position = savedCamPos;
            gameplayCamera.transform.rotation = savedCamRot;
        }
        if (camFollow != null)
            camFollow.enabled = savedCamEnabled;

        if (playerRef != null)
        {
            SetPlayerVisible(true);
            var move = playerRef.GetComponent<SimplePlayerMovement>();
            if (move != null) move.SetControlsEnabled(true);
        }
    }

    void Update()
    {
        if (!active) return;

        // Keep cursor usable even if another system re-locks it.
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (!isAnimating && Input.GetMouseButtonDown(0))
        {
            var click = PickMedicineUnderCursor();
            if (click != null) OnMedicineClicked(click);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
            ForceExit();
    }

    MedicineClickable PickMedicineUnderCursor()
    {
        Camera cam = gameplayCamera != null ? gameplayCamera : Camera.main;
        if (cam == null) return null;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Rack / shelf colliders often sit in front of meds — scan all hits.
        RaycastHit[] hits = Physics.RaycastAll(ray, 40f, ~0, QueryTriggerInteraction.Ignore);
        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                var click = hit.collider.GetComponentInParent<MedicineClickable>();
                if (click != null) return click;
            }
        }

        // Fallback: nearest medicine whose screen bounds contain the cursor.
        Vector3 mouse = Input.mousePosition;
        MedicineClickable best = null;
        float bestDist = float.MaxValue;
        foreach (var slot in slots)
        {
            if (slot?.view == null) continue;
            var click = slot.view.GetComponent<MedicineClickable>();
            if (click == null) continue;

            var rend = slot.view.GetComponentInChildren<Renderer>();
            if (rend == null) continue;

            Bounds b = rend.bounds;
            Vector3 screenCenter = cam.WorldToScreenPoint(b.center);
            if (screenCenter.z <= 0f) continue;

            Vector3 min = cam.WorldToScreenPoint(b.min);
            Vector3 max = cam.WorldToScreenPoint(b.max);
            float minX = Mathf.Min(min.x, max.x) - 20f;
            float maxX = Mathf.Max(min.x, max.x) + 20f;
            float minY = Mathf.Min(min.y, max.y) - 20f;
            float maxY = Mathf.Max(min.y, max.y) + 20f;

            if (mouse.x < minX || mouse.x > maxX || mouse.y < minY || mouse.y > maxY)
                continue;

            float d = Vector2.Distance(new Vector2(mouse.x, mouse.y), new Vector2(screenCenter.x, screenCenter.y));
            if (d < bestDist)
            {
                bestDist = d;
                best = click;
            }
        }
        return best;
    }
}

public class MedicineClickable : MonoBehaviour
{
    public int row;
    public int col;
    public MedicineOrganizerMinigame.MedType type;
    MedicineOrganizerMinigame owner;

    public void Setup(MedicineOrganizerMinigame o, int r, int c, MedicineOrganizerMinigame.MedType t)
    {
        owner = o;
        row = r;
        col = c;
        type = t;
    }
}
