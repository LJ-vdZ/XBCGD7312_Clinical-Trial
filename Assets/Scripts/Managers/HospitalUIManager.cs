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

    Color moneyPositiveColor = Color.white;

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
        if (moneyText == null)
        {
            moneyText = FindMoneyLabel();
        }

        if (moneyText != null)
        {
            moneyPositiveColor = moneyText.color;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        var stats = HospitalStatsManager.Instance;

        if (stats == null)
        {
            return;
        }

        if (sanitationSlider != null)
        {
            sanitationSlider.value = stats.sanitation;

        }
            
        if (comfortSlider != null)
        {
            comfortSlider.value = stats.comfort;
        }

        if (moraleSlider != null)
        {
            moraleSlider.value = stats.morale;
        }

        if (moneyText == null)
        {
            moneyText = FindMoneyLabel();
        }
            

        if (moneyText == null)
        {
            return;
        }

        if (stats.money < 0)
        {
            moneyText.text = "-R" + Mathf.Abs(stats.money);

            moneyText.color = Color.red;
        }
        else
        {
            moneyText.text = "R" + stats.money;

            moneyText.color = moneyPositiveColor;
        }
    }

    static TextMeshProUGUI FindMoneyLabel()
    {
        string[] names = { "MoneytTxt", "Moneytxt", "MoneyTxt" };

        foreach (var n in names)
        {
            var go = ClinicalUIFactory.FindByName(n);

            if (go == null)
            {
                continue;
            }

            var tmp = go.GetComponent<TextMeshProUGUI>();

            if (tmp != null)
            {
                return tmp;
            }
        }

        return null;
    }
}