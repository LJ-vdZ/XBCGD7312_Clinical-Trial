using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// When Manager is active, shows morale bars above Doctor/Nurse/Janitor heads.
/// </summary>
public class CharacterMoraleSystem : MonoBehaviour
{
    public static CharacterMoraleSystem Instance;

    class Bar
    {
        public PlayableCharacter character;
        public Slider slider;
        public Canvas worldCanvas;
        public float morale = 70f;
    }

    readonly List<Bar> bars = new List<Bar>();

    void Awake() => Instance = this;

    void Start()
    {
        CharacterSwitchManager.OnCharacterChanged += HandleSwitch;
        HospitalStatsManager.OnStatsChanged += SyncFromHospital;
        CreateBars();
        RefreshVisibility();
    }

    void OnDestroy()
    {
        CharacterSwitchManager.OnCharacterChanged -= HandleSwitch;
        HospitalStatsManager.OnStatsChanged -= SyncFromHospital;
    }

    void CreateBars()
    {
        bars.Clear();
        if (CharacterSwitchManager.Instance == null) return;
        foreach (var c in CharacterSwitchManager.Instance.characters)
        {
            if (c == null || c.role == RoleType.Manager) continue;
            bars.Add(CreateBarFor(c));
        }
    }

    Bar CreateBarFor(PlayableCharacter c)
    {
        var canvasGo = new GameObject("MoraleBar", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(c.transform, false);
        canvasGo.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1.6f, 0.25f);
        canvasGo.transform.localScale = Vector3.one * 0.01f;

        var bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvasGo.transform, false);
        var bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = bgrt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(bg.transform, false);
        var frt = fillGo.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = new Vector2(0.7f, 1f);
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        fillGo.GetComponent<Image>().color = new Color(0.3f, 0.8f, 0.4f);

        var slider = bg.AddComponent<Slider>();
        slider.fillRect = frt;
        slider.targetGraphic = fillGo.GetComponent<Image>();
        slider.minValue = 0; slider.maxValue = 100; slider.value = 70;
        slider.interactable = false;

        return new Bar { character = c, slider = slider, worldCanvas = canvas, morale = 70f };
    }

    void LateUpdate()
    {
        foreach (var b in bars)
        {
            if (b.worldCanvas == null) continue;
            if (Camera.main != null)
                b.worldCanvas.transform.rotation = Quaternion.LookRotation(
                    b.worldCanvas.transform.position - Camera.main.transform.position);
        }
    }

    void HandleSwitch(PlayableCharacter c) => RefreshVisibility();

    void RefreshVisibility()
    {
        bool show = CharacterSwitchManager.Instance != null
                    && CharacterSwitchManager.Instance.ActiveRole == RoleType.Manager;
        foreach (var b in bars)
        {
            if (b.worldCanvas != null)
                b.worldCanvas.gameObject.SetActive(show);
        }
    }

    void SyncFromHospital()
    {
        if (HospitalStatsManager.Instance == null) return;
        float hygiene = HospitalStatsManager.Instance.sanitation;
        foreach (var b in bars)
        {
            // Hygiene pulls staff morale toward hospital sanitation
            b.morale = Mathf.Clamp(b.morale * 0.85f + hygiene * 0.15f, 0f, 100f);
            if (b.slider != null) b.slider.value = b.morale;
        }
    }

    public static void NotifyOrganizerSuccess()
    {
        if (Instance == null) return;
        Instance.AdjustAll(+8f);
    }

    public static void NotifyPatientOutcome(bool recovered)
    {
        if (Instance == null) return;
        Instance.AdjustAll(recovered ? +5f : -10f);
    }

    public void AdjustAll(float delta)
    {
        foreach (var b in bars)
        {
            b.morale = Mathf.Clamp(b.morale + delta, 0f, 100f);
            if (b.slider != null) b.slider.value = b.morale;
        }
    }
}
