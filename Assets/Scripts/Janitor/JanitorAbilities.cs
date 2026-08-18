using UnityEngine;

public class JanitorAbilities : MonoBehaviour
{
    [Header("Holding")]
    public Transform attachPoint;
    [Tooltip("Centered attach for JanitorCart_Final. Separate from trash AttachObject.")]
    public Transform cartAttachPoint;

    public TrashItem heldTrash = null;

    // --- TRASH SYSTEM ---

    public void PickUpTrash(TrashItem trash)
    {
        if (heldTrash != null) return;

        if (attachPoint == null)
        {
            var ap = new GameObject("TrashAttach");
            ap.transform.SetParent(transform);
            ap.transform.localPosition = new Vector3(0.4f, 1f, 0.5f);
            attachPoint = ap.transform;
        }

        heldTrash = trash;
        trash.transform.SetParent(attachPoint);
        trash.transform.localPosition = Vector3.zero;
        trash.transform.localRotation = Quaternion.identity;

        Debug.Log($"Picked up {trash.type}");
    }

    public void DisposeTrash(TrashType requiredType)
    {
        if (heldTrash == null) return;

        if (heldTrash.type == requiredType)
            HospitalStatsManager.Instance.ChangeSanitation(+12f);
        else
            HospitalStatsManager.Instance.ChangeSanitation(-20f);

        Destroy(heldTrash.gameObject);
        heldTrash = null;

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("drop");

        Debug.Log("Trash disposed");
    }

    public void DropTrash()
    {
        if (heldTrash == null) return;

        heldTrash.transform.SetParent(null);
        heldTrash.transform.position = transform.position + transform.forward * 1.5f;

        heldTrash = null;
    }
}
