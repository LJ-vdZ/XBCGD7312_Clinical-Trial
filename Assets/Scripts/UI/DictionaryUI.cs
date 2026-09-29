using UnityEngine;
using UnityEngine.UI;

public class DictionaryUI : MonoBehaviour
{
    public Button dictionaryBtn;
    
    public Button closeBtn;

    public Button nextBtn;
    public Button backBtn;

    public Image dictionaryImage;
    public GameObject dictionaryPage;

    public Sprite[] dictionaryImages;
    int currentImage = 0;

    

    void Start()
    {
        dictionaryBtn.onClick.AddListener(OpenDictionary);
        closeBtn.onClick.AddListener(CloseDictionary);

        nextBtn.onClick.AddListener(NextImage);
        backBtn.onClick.AddListener(PreviousImage);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            OpenDictionary();
        }
    }

    void OpenDictionary()
    {
        dictionaryBtn.gameObject.SetActive(false);
        dictionaryPage.SetActive(true);
        SetCursorFree(true);
    }

    void CloseDictionary()
    {
        dictionaryBtn.gameObject.SetActive(true);
        dictionaryPage.SetActive(false);
        SetCursorFree(false);
    }

    void SetCursorFree(bool free)
    {
        var active = CharacterSwitchManager.Instance != null
            ? CharacterSwitchManager.Instance.ActiveCharacter
            : null;

        if (active != null && active.movement != null)
            active.movement.SetControlsEnabled(!free);

        var cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null)
            cam.LockCursor(!free);

        if (free || UiPointerState.ShouldKeepCursorFree())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void NextImage()
    {
        if (currentImage < dictionaryImages.Length - 1)
        {
            currentImage++;
            ShowImage();
        }
    }

    void PreviousImage()
    {
        if (currentImage > 0)
        {
            currentImage--;
            ShowImage();
        }
    }

    void ShowImage()
    {
        dictionaryImage.sprite = dictionaryImages[currentImage];
    }
}
