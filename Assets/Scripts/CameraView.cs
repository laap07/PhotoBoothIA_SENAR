using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class CameraView : MonoBehaviour
{
    public RawImage rawImage;
    private WebCamTexture webcamTexture;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("Nenhuma câmera detectada");
            return;
        }

        // Usa a primeira câmera disponível
        webcamTexture = new WebCamTexture(devices[0].name,1280,720);

        // Atribui direto no RawImage (sem tocar no material)
        rawImage.texture = webcamTexture;

        webcamTexture.Play();

        rawImage.rectTransform.localScale = new Vector3(-1, 1, 1);
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.K))
        {
            //SavePhoto();    
        }
    }

    public Texture2D CapturePhoto()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying)
        {
            Debug.LogError("Câmera não está ativa");
            return null;
        }

        // Captura a imagem atual
        Texture2D photo = new Texture2D(webcamTexture.width, webcamTexture.height);
        photo.SetPixels(webcamTexture.GetPixels());
        photo.Apply();
        return photo;
    }

    private void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
            webcamTexture.Stop();
    }
    public Texture2D SavePhoto()
    {
        Texture2D photo = CapturePhoto();
        if (photo == null)
        {
            Debug.LogWarning("Nenhuma foto capturada.");
            return null;
        }

        byte[] bytes = photo.EncodeToPNG();
        string path;

        path = Path.Combine(Application.persistentDataPath, "CameraPhoto.png");

        File.WriteAllBytes(path, bytes);
        Debug.Log("Foto salva em: " + path);
        return photo;
    }

    public void StopCamera()
    {
        webcamTexture.Stop();
    }
}
