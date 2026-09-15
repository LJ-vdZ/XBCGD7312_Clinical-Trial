using UnityEngine;
using UnityEngine.UI;

public class ManualScript : MonoBehaviour
{
    public Button dictionaryBtn;
    public GameObject playBtn;
    public GameObject ExitBtn;
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

    void OpenDictionary()
    {
        playBtn.SetActive(false);
        ExitBtn.SetActive(false);
        dictionaryBtn.gameObject.SetActive(false);
        dictionaryPage.SetActive(true);
    }

    void CloseDictionary()
    {
        playBtn.SetActive(true);
        ExitBtn.SetActive(true);
        dictionaryBtn.gameObject.SetActive(true);
        dictionaryPage.SetActive(false);
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
