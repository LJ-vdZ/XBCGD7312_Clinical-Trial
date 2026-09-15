using UnityEngine;
using UnityEngine.UI;

public class ManualScript : MonoBehaviour
{
    public Button dictionaryBtn;
    public GameObject playBtn;
    public GameObject ExitBtn;
    public GameObject overlayPanel;
    public Button closeBtn;

    public Button nextBtn;
    public Button backBtn;

    public Image dictionaryImage;
    public GameObject dictionaryPage;

    public Sprite[] dictionaryImages;
    int currentImage = 0;

    public MainSceneUIManager mainSceneUIManager;

    void Start()
    {
        dictionaryBtn.onClick.AddListener(OpenDictionary);
        closeBtn.onClick.AddListener(CloseDictionary);

        nextBtn.onClick.AddListener(NextImage);
        backBtn.onClick.AddListener(PreviousImage);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            OpenDictionary();
        }
    }

    void OpenDictionary()
    {
        if (playBtn != null)
            playBtn.SetActive(false);

        if (ExitBtn != null)
            ExitBtn.SetActive(false);

        dictionaryBtn.gameObject.SetActive(false);
        dictionaryPage.SetActive(true);

        if (overlayPanel != null)
            overlayPanel.SetActive(false);

        mainSceneUIManager.CloseAllUI();
        mainSceneUIManager.SetPlayerControl(false);
    }

    void CloseDictionary()
    {
        if (playBtn != null)
            playBtn.SetActive(true);

        if (ExitBtn != null)
            ExitBtn.SetActive(true);

        dictionaryBtn.gameObject.SetActive(true);
        dictionaryPage.SetActive(false);

        if (overlayPanel != null)
            overlayPanel.SetActive(true);
        mainSceneUIManager.SetPlayerControl(true);
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
