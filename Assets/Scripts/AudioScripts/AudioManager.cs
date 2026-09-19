using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//used hashmap approach
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public enum SoundId
    {
        ButtonClick,
        EndScreenAppears,
        JanitorCart,
        MedicineSelect,
        MedicineSwap,
        PopUpNotification,
        Walking,
        CardboardBox
    }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Music (unchanged)")]
    public AudioClip backgroundMusic;

    [Header("SFX")]
    public AudioClip buttonClickSFX;
    public AudioClip endScreenAppearsClip;
    public AudioClip janitorCartClip;
    public AudioClip medicineSelectClip;
    public AudioClip medicineSwapClip;
    public AudioClip popUpNotificationClip;
    public AudioClip walkingClip;
    public AudioClip MedicineBox;

    //change pitch and speed of walking audio. walking sfx was too slow for character speed
    [Range(0.5f, 2f)] public float walkingPitch = 1.2f;
    [Range(1f, 8f)] public float medicineSwapVolume = 5f;

    readonly Dictionary<SoundId, AudioClip> soundMap = new Dictionary<SoundId, AudioClip>();
    readonly Dictionary<string, SoundId> aliasMap = new Dictionary<string, SoundId>();

    AudioSource walkLoopSource;
    AudioSource cartLoopSource;
    AudioSource buttonSource;
    AudioSource swapSource;

    float buttonStartTime;

    AudioClip amplifiedSwapClip;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        EnsureLoopSources();

        EnsureAudioListener();

        LoadClipsFromAudioFolder();

        BuildSoundMap();

        BuildAliases();

        PrepareButtonClickPlayback();

        PrepareMedicineSwapPlayback();

        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        EnsureAudioListener();
        PlayMusic(backgroundMusic);

        HookAllButtons();
    }

    //if already exists, descroy onlod
    void OnDestroy()
    {
        if (Instance == this) 
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
            
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureAudioListener();
        HookAllButtons();
    }

    /// <summary>
    /// Tutorial Main Camera shipped with AudioListener disabled — without one, all audio is silent.
    /// </summary>
    public static void EnsureAudioListener()
    {
        var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        AudioListener active = null;
        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] == null)
                continue;

            if (!listeners[i].enabled)
                listeners[i].enabled = true;

            if (listeners[i].enabled && listeners[i].gameObject.activeInHierarchy)
                active = listeners[i];
        }

        if (active != null)
            return;

        var cam = Camera.main;
        if (cam == null)
            cam = FindFirstObjectByType<Camera>();

        if (cam != null)
        {
            var existing = cam.GetComponent<AudioListener>();
            if (existing != null)
                existing.enabled = true;
            else
                cam.gameObject.AddComponent<AudioListener>();
            return;
        }

        var go = new GameObject("AudioListener");
        go.AddComponent<AudioListener>();
    }

    void EnsureLoopSources()
    {
        walkLoopSource = CreateLoopSource("WalkLoop");
        cartLoopSource = CreateLoopSource("CartLoop");
        buttonSource = CreateOneShotSource("ButtonClickSource");
        swapSource = CreateOneShotSource("MedicineSwapSource");

        swapSource.volume = 1f;
    }

    AudioSource CreateOneShotSource(string sourceName)
    {
        var src = CreateLoopSource(sourceName);

        src.loop = false;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.priority = 0;

        return src;
    }

    AudioSource CreateLoopSource(string sourceName)
    {
        var child = transform.Find(sourceName);

        AudioSource src = child != null ? child.GetComponent<AudioSource>() : null;

        if (src == null)
        {

            var go = new GameObject(sourceName);

            go.transform.SetParent(transform, false);

            src = go.AddComponent<AudioSource>();
        }

        src.playOnAwake = false;

        src.loop = true;

        src.spatialBlend = 0f;

        return src;
    }

    void LoadClipsFromAudioFolder()
    {
        if (buttonClickSFX == null)
        {
            buttonClickSFX = LoadAudio("ButtonClick");
        }

        if (endScreenAppearsClip == null)
        {
            endScreenAppearsClip = LoadAudio("EndScreenAppears");
        }

        if (janitorCartClip == null)
        {
            janitorCartClip = LoadAudio("JanitorCartMoving");
        }

        if (medicineSelectClip == null) 
        {
            medicineSelectClip = LoadAudio("Medicine Select");
        }

        if (medicineSwapClip == null)
        {
            medicineSwapClip = LoadAudio("Medicine Swap");
        }

        if (popUpNotificationClip == null)
        {
            popUpNotificationClip = LoadAudio("PopUpNotification");
        }

        if (walkingClip == null)
        {
            walkingClip = LoadAudio("Walking");
        }

        if (MedicineBox == null)
        {
            MedicineBox = LoadAudio("CardboardBox");
        }
    }

    static AudioClip LoadAudio(string fileNameWithoutExtension)
    {
#if UNITY_EDITOR
        string[] paths =
        {
            $"Assets/Audio/{fileNameWithoutExtension}.mp3",
            $"Assets/Audio/{fileNameWithoutExtension}.wav",
            $"Assets/Audio/{fileNameWithoutExtension}.ogg"
        };
        foreach (var path in paths)
        {
            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null) return clip;
        }
#endif
        return Resources.Load<AudioClip>("Sounds/" + fileNameWithoutExtension);
    }

    void BuildSoundMap()
    {
        soundMap.Clear();

        Assign(SoundId.ButtonClick, buttonClickSFX);

        Assign(SoundId.EndScreenAppears, endScreenAppearsClip);

        Assign(SoundId.JanitorCart, janitorCartClip);

        Assign(SoundId.MedicineSelect, medicineSelectClip);

        Assign(SoundId.MedicineSwap, medicineSwapClip);

        Assign(SoundId.PopUpNotification, popUpNotificationClip);

        Assign(SoundId.Walking, walkingClip);

        Assign(SoundId.CardboardBox, MedicineBox);
    }

    void Assign(SoundId id, AudioClip clip)
    {
        if (clip != null) 
        {
            soundMap[id] = clip;
        }
            
    }

    void BuildAliases()
    {
        aliasMap.Clear();

        MapAlias(SoundId.ButtonClick, "button click", "buttonclick", "button", "click");

        MapAlias(SoundId.EndScreenAppears, "end screen appears", "endscreenappears", "endscreen");

        MapAlias(SoundId.JanitorCart, "janitorcart", "janitor cart", "janitorcartmoving");

        MapAlias(SoundId.MedicineSelect, "medicine select", "medicineselect");

        MapAlias(SoundId.MedicineSwap, "medicine swap", "medicineswap");

        MapAlias(SoundId.PopUpNotification, "popupnontification", "popupnotification", "pop up notification", "notification");
        
        MapAlias(SoundId.Walking, "walking", "walk", "footstep");
        
        MapAlias(SoundId.CardboardBox, "cardboardbox", "cardboard box", "medicinebox", "medicine box");
    }

    void MapAlias(SoundId id, params string[] keys)
    {
        foreach (var key in keys) 
        {
            aliasMap[NormalizeKey(key)] = id;
        }
            
    }

    static string NormalizeKey(string key)
    {
        return string.IsNullOrEmpty(key) ? string.Empty : key.Trim().ToLowerInvariant().Replace(" ", "");
    }

    public void PlayMusic(AudioClip clip)
    {
        EnsureAudioListener();

        if (clip == null || musicSource == null)
        {
            return;
        }

        musicSource.clip = clip;

        musicSource.loop = true;

        if (musicSource.volume < 0.2f)
            musicSource.volume = 0.65f;

        musicSource.Play();
    }

    /// <summary>Lerp musicSource volume over time (unscaled).</summary>
    public IEnumerator FadeMusicVolume(float toVolume, float duration)
    {
        if (musicSource == null)
            yield break;

        float from = musicSource.volume;
        float to = Mathf.Clamp01(toVolume);
        if (duration <= 0.01f)
        {
            musicSource.volume = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }

        musicSource.volume = to;
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        EnsureAudioListener();

        if (clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayButtonClick()
    {
        AudioClip clip = buttonClickSFX;

        if (soundMap.TryGetValue(SoundId.ButtonClick, out AudioClip mapped) && mapped != null) 
        {
            clip = mapped;
        }

        if (clip == null)
        {
            return;
        }

        if (buttonSource == null)
        {
            PlaySFX(clip);

            return;
        }

        buttonSource.Stop();
        buttonSource.clip = clip;
        buttonSource.spatialBlend = 0f;

        float start = Mathf.Clamp(buttonStartTime, 0f, Mathf.Max(0f, clip.length - 0.02f));

        buttonSource.time = start;

        buttonSource.Play();
    }

    void PlayMedicineSwap()
    {
        AudioClip clip = amplifiedSwapClip != null ? amplifiedSwapClip : medicineSwapClip;

        if (clip == null && soundMap.TryGetValue(SoundId.MedicineSwap, out AudioClip mapped)) 
        {
            clip = mapped;
        }

        if (clip == null)
        {
            return;
        }

        if (swapSource != null)
        {
            swapSource.Stop();

            swapSource.clip = clip;

            swapSource.spatialBlend = 0f;

            swapSource.volume = 1f;

            swapSource.Play();

            return;
        }

        PlaySFX(clip);
    }

    void PrepareMedicineSwapPlayback()
    {
        AudioClip source = medicineSwapClip;

        if (source == null && soundMap.TryGetValue(SoundId.MedicineSwap, out AudioClip mapped)) 
        {
            source = mapped;
        }

        if (source == null)
        {
            return;
        }

        source.LoadAudioData();

        amplifiedSwapClip = AmplifyClip(source, medicineSwapVolume);
    }

    static AudioClip AmplifyClip(AudioClip source, float gain)
    {
        if (source == null || source.samples <= 0 || source.channels <= 0)
            return source;

        var data = new float[source.samples * source.channels];

        try
        {
            source.GetData(data, 0);
        }
        catch (System.Exception)
        {
            return source;
        }

        float g = Mathf.Max(1f, gain);

        for (int i = 0; i < data.Length; i++)
            data[i] = Mathf.Clamp(data[i] * g, -1f, 1f);

        var amplified = AudioClip.Create(
            source.name + "_Loud",
            source.samples,
            source.channels,
            source.frequency,
            false);
        amplified.SetData(data, 0);
        return amplified;
    }

    void PrepareButtonClickPlayback()
    {
        AudioClip clip = buttonClickSFX;
        if (clip == null) return;

        clip.LoadAudioData();
        buttonStartTime = FindFirstAudibleTime(clip);
    }

    static float FindFirstAudibleTime(AudioClip clip)
    {
        if (clip == null || clip.samples <= 0 || clip.channels <= 0)
            return 0f;

        int samplesToScan = Mathf.Min(clip.samples, clip.frequency);
        var data = new float[samplesToScan * clip.channels];
        try
        {
            clip.GetData(data, 0);
        }
        catch (System.Exception)
        {
            return 0.02f;
        }

        const float threshold = 0.02f;
        for (int i = 0; i < data.Length; i++)
        {
            if (Mathf.Abs(data[i]) >= threshold)
                return (i / (float)clip.channels) / clip.frequency;
        }

        return 0f;
    }

    public void Play(SoundId id)
    {
        if (id == SoundId.Walking)
        {
            SetWalking(true);
            return;
        }

        if (id == SoundId.JanitorCart)
        {
            SetJanitorCartMoving(true);
            return;
        }

        if (soundMap.TryGetValue(id, out AudioClip clip) && clip != null)
        {
            if (id == SoundId.ButtonClick)
            {
                PlayButtonClick();
                return;
            }
            if (id == SoundId.MedicineSwap)
            {
                PlayMedicineSwap();
                return;
            }
            PlaySFX(clip);
        }
        else if (id == SoundId.ButtonClick)
            PlayButtonClick();
    }

    /// <summary>Play by string key (hashmap / dictionary lookup).</summary>
    public void Play(string actionOrObjectKey)
    {
        if (string.IsNullOrEmpty(actionOrObjectKey)) return;

        string key = NormalizeKey(actionOrObjectKey);
        if (aliasMap.TryGetValue(key, out SoundId id))
        {
            Play(id);
            return;
        }

        if (System.Enum.TryParse(actionOrObjectKey.Replace(" ", ""), true, out SoundId parsed))
            Play(parsed);
    }

    public void HookButton(Button button)
    {
        if (button == null) return;
        if (button.GetComponent<UiButtonPressSfx>() == null)
            button.gameObject.AddComponent<UiButtonPressSfx>();
    }

    public void HookAllButtons()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
            HookButton(buttons[i]);
    }

    public void SetWalking(bool walking)
    {
        if (walkLoopSource != null)
            walkLoopSource.pitch = walkingPitch;
        SetLoop(walkLoopSource, SoundId.Walking, walkingClip, walking);
    }

    public void SetJanitorCartMoving(bool moving)
    {
        SetLoop(cartLoopSource, SoundId.JanitorCart, janitorCartClip, moving);
    }

    void SetLoop(AudioSource source, SoundId id, AudioClip fallback, bool shouldPlay)
    {
        if (source == null) return;

        AudioClip clip = fallback;
        if (soundMap.TryGetValue(id, out AudioClip mapped) && mapped != null)
            clip = mapped;

        if (!shouldPlay || clip == null)
        {
            if (source.isPlaying) source.Stop();
            return;
        }

        if (source.clip != clip) source.clip = clip;
        source.loop = true;
        if (!source.isPlaying) source.Play();
    }

    public void RegisterClip(SoundId id, AudioClip clip)
    {
        if (clip == null) return;
        soundMap[id] = clip;
    }

    public void RegisterAlias(string key, SoundId id)
    {
        if (string.IsNullOrEmpty(key)) return;
        aliasMap[NormalizeKey(key)] = id;
    }
}
