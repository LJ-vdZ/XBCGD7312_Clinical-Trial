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

    //colours for the slider stats
    Color moneyPositiveColor = Color.white;
    Color sanitationFillDefault = Color.white;
    Color comfortFillDefault = Color.white;
    Color moraleFillDefault = Color.white;
    bool sliderColorsCached;

    //colour for when stats are less than 40%. orange
    static readonly Color StatOrange = new Color(1f, 0.55f, 0.1f, 1f);

    //colour for when stats are less than 25%. red
    static readonly Color StatRed = new Color(0.9f, 0.15f, 0.12f, 1f);

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


        SliderFillDefaults();

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
            ApplyStatFillColor(sanitationSlider, stats.sanitation, sanitationFillDefault);
        }
            
        if (comfortSlider != null)
        {
            comfortSlider.value = stats.comfort;
            ApplyStatFillColor(comfortSlider, stats.comfort, comfortFillDefault);
        }

        if (moraleSlider != null)
        {
            moraleSlider.value = stats.morale;
            ApplyStatFillColor(moraleSlider, stats.morale, moraleFillDefault);
        }

        UpdateMoneyText(stats);
    }

    //set default colours for sliders
    void SliderFillDefaults()
    {
        if (sliderColorsCached)
        {
            return;
        }

        sanitationFillDefault = GetFillColor(sanitationSlider, Color.white);

        comfortFillDefault = GetFillColor(comfortSlider, Color.white);

        moraleFillDefault = GetFillColor(moraleSlider, Color.white);

        sliderColorsCached = true;
    }

    //original colour of slider
    static Color GetFillColor(Slider slider, Color fallback)
    {
        if (slider == null || slider.fillRect == null)
        {
            return fallback;
        }

        var image = slider.fillRect.GetComponent<Image>();

        return image != null ? image.color : fallback;
    }

    //change colour when slider below certain percentage
    static void ApplyStatFillColor(Slider slider, float value, Color defaultColor)
    {
        if (slider == null || slider.fillRect == null)
        {
            return;
        }

        var image = slider.fillRect.GetComponent<Image>();

        if (image == null)
        {
            return;
        }

        //stats are 0–100. orange at/below 40, red at/below 25.
        if (value <= 25f)
        {
            image.color = StatRed;
        }
        else if (value <= 40f)
        {
            image.color = StatOrange;
        }
        else
        {
            image.color = defaultColor;
        }
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