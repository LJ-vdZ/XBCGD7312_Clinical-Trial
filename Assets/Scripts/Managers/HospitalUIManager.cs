using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class HospitalUIManager : MonoBehaviour
{
    [Header("Sliders")]
    public Slider sanitationSlider;
    public Slider comfortSlider;
    public Slider moraleSlider;

    [Header("Money UI")]
    public TextMeshProUGUI moneyText; 

    private void OnEnable()
    {
        HospitalStatsManager.OnStatsChanged += UpdateUI;
    }

    private void OnDisable()
    {
        HospitalStatsManager.OnStatsChanged -= UpdateUI;
    }

    private void Start()
    {
        UpdateUI(); //update ui at start
    }

    void UpdateUI()
    {
        var stats = HospitalStatsManager.Instance;

        //update sliders
        sanitationSlider.value = stats.sanitation;
        comfortSlider.value = stats.comfort;
        moraleSlider.value = stats.morale;

        //update money - not a slider
        moneyText.text = "R" + stats.money.ToString();
    }
}