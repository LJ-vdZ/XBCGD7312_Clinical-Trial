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

    void Update()
    {
        var stats = HospitalStatsManager.Instance;

        if (stats != null && stats.IsDeficitTimerRunning)
        {
            UpdateMoneyText(stats);
        }
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

        UpdateMoneyText(stats);
    }

    void UpdateMoneyText(HospitalStatsManager stats)
    {
        if (moneyText == null)
        {
            moneyText = FindMoneyLabel();
        }

        if (moneyText == null)
        {
            return;
        }

        string amount = HospitalStatsManager.FormatMoney(stats.money);

        if (stats.IsDeficitTimerRunning)
        {
            int remaining = Mathf.CeilToInt(stats.GetDeficitTimeRemaining());
            int minutes = remaining / 60;
            int seconds = remaining % 60;
            moneyText.text = $"{amount}  {minutes}:{seconds:00}";
            moneyText.color = Color.red;
        }
        else if (stats.money < 0)
        {
            moneyText.text = amount;
            moneyText.color = Color.red;
        }
        else
        {
            moneyText.text = amount;
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