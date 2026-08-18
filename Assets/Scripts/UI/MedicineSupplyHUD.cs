using TMPro;
using UnityEngine;

/// <summary>Shows remaining medicine supply under the Stats panel.</summary>
public class MedicineSupplyHUD : MonoBehaviour
{
    TextMeshProUGUI label;

    void Start()
    {
        label = StatsSideHudPlacement.CreateMedicineLabel();
        if (label != null)
            label.gameObject.SetActive(true);

        MedicineSupplyManager.OnSupplyChanged += Refresh;
        CharacterSwitchManager.OnCharacterChanged += _ => Refresh();
        Refresh();
    }

    void OnDestroy()
    {
        MedicineSupplyManager.OnSupplyChanged -= Refresh;
    }

    void Refresh()
    {
        if (label == null) return;

        label.gameObject.SetActive(true);
        int count = MedicineSupplyManager.Instance != null ? MedicineSupplyManager.Instance.medicineCount : 0;
        label.text = $"Medicine: {count}";
    }
}
