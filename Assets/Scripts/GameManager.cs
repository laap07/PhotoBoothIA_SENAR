using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;
using ZXing;
using ZXing.QrCode;

public class GameManager : MonoBehaviour
{
    //[SerializeField] private DzineAPI api;
    [SerializeField] private LeonardoAPI api;
    [SerializeField] private CameraView cam;
    [SerializeField] private Image photoTaked;
    [SerializeField] private List<GameObject> screens = new List<GameObject>();
    int index = 0;
    int index_BG = 0;
    [SerializeField] Image finalPhoto;
    public bool menuPrinting;
    private bool preparingPhoto = false;
    [SerializeField] private float timePreparingPhoto = 8;
    private float currentTimePreparingPhoto = 8;
    [SerializeField] private TextMeshProUGUI timeCounter;
    [SerializeField] private RawImage qrCodeImage;
    [SerializeField] private Animation fade;
    private int cheatRestart = 0;
    private int cheatClose = 0;
    public BlinkPhoto blink;

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        Screen.SetResolution(1080, 1920, true);
        currentTimePreparingPhoto = timePreparingPhoto;
        timeCounter.text = currentTimePreparingPhoto.ToString();
        cheatRestart = 0;
        cheatClose = 0;
    }
    private void Update()
    {
        if(preparingPhoto)
        {
            currentTimePreparingPhoto -= Time.deltaTime;
            timeCounter.text = ((int)currentTimePreparingPhoto).ToString();
        }
    }
    public void StartTimerToTakePhoto()
    {
        StartCoroutine(TakePhoto());
    }

    IEnumerator TakePhoto()
    {
        preparingPhoto=true;

        yield return new WaitForSeconds(timePreparingPhoto); // Delay pra tirar foto

        preparingPhoto = false;
        currentTimePreparingPhoto = timePreparingPhoto;
        timeCounter.text = currentTimePreparingPhoto.ToString();

        Texture2D tex =  cam.SavePhoto();

        Rect rect = new Rect(0, 0, tex.width, tex.height);
        Sprite sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f));
        photoTaked.sprite = sprite;

        yield return new WaitForSeconds(0f); // Delay pra ir pra proxima tela

        blink.gameObject.SetActive(true);
        blink.Ativar();
        yield return new WaitForSeconds(1f); // Delay pra ir pra proxima tela
        AdvanceAnimation();
        cam.rawImage.gameObject.SetActive(false);
        // Colocar animacao de foto tic branco tela
    }
    public void AdvanceScreen()
    {
        fade.Play("FadeIn");
    }
    public void AdvanceAnimation()
    {
        index++;
        foreach (var screen in screens)
        {
            screen.SetActive(false);
        }
        screens[index].gameObject.SetActive(true);
    }
    public void BackScreen()
    {
        index--;
        foreach (var screen in screens)
        {
            screen.SetActive(false);
        }
        screens[index].gameObject.SetActive(true);
    }
    public void GenerateImages()
    {
        api.StartUpload();
    }
    public void SetIndex(int _index)
    {
        index_BG = _index;
    }
    /*public void StartVerify()
    {      
        StartCoroutine(FirstVerifyImagesLoop());
    }
    private IEnumerator VerifyImagesLoop()
    {
        yield return new WaitForSeconds(10f);
        api.GetFaceSwap();
        if (api.isImageReady)
        {
            AdvanceScreen();
            api.DownloadResults();
            yield return null;
        }
        else
        {
            StartCoroutine(VerifyImagesLoop());
        }
    }
    private IEnumerator FirstVerifyImagesLoop()
    {
        yield return new WaitForSeconds(15f);
        api.GetFaceSwap();
        if (api.isImageReady)
        {
            AdvanceScreen();
            api.DownloadResults();
            yield return null;
        }
        else
        {
            StartCoroutine(VerifyImagesLoop());
        }
    }*/
    public void SetImageFinal(GameObject _image)
    {
        finalPhoto.sprite = _image.GetComponent<Image>().sprite;
    }
    public void PrintImage()
    {
        StartCoroutine(DelayPrint());
    }
    private void PrintImageCall()
    {
        if (menuPrinting)
        {
            ImprimirNativo printer = new ImprimirNativo();
            printer.PrintOptions(finalPhoto.sprite);
            printer = null;
        }
        else
        {
            ImprimirNativo printer = new ImprimirNativo();
            printer.PrintFix(finalPhoto.sprite);
            printer = null;
        }
    }
    private IEnumerator DelayPrint()
    {
        yield return new WaitForSeconds(2f);
        PrintImageCall();
    }

    public void RestartGame()
    {
        cam.StopCamera();
        SceneManager.LoadScene(0);
    }
    public void GenerateQRCode()
    {
        Texture2D qrTexture = GenerateQRCode(api.urlfinalImage, 256, 256);
        qrCodeImage.texture = qrTexture;
    }

    Texture2D GenerateQRCode(string text, int width, int height)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 0
            }
        };

        Color32[] pixels = writer.Write(text);
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    public void CheatClose()
    {
        cheatClose++;
        if(cheatClose > 5)
        {
            cam.StopCamera();
            Application.Quit();
        }
    }
    public void CheatRestart()
    {
        cheatRestart++;
        if (cheatRestart > 5)
        {
            cam.StopCamera();
            SceneManager.LoadScene(0);
        }
    }
}
