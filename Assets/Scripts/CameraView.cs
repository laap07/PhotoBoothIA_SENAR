using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class CameraView : MonoBehaviour
{
    [SerializeField] private LeonardoAPI api;
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
        Texture2D borderPhoto = PaintBorder(photo, 400, false, true);

        byte[] bytes = borderPhoto.EncodeToPNG();
        string path;

        path = Path.Combine(Application.persistentDataPath, api.destFile);

        File.WriteAllBytes(path, bytes);
        Debug.Log("Foto salva em: " + path);
        return photo;
    }

    public void StopCamera()
    {
        webcamTexture.Stop();
    }

    private Texture2D PaintBorder(
        Texture2D sourceTex,
        int borderThickness,
        bool drawHorizontal,
        bool drawVertical
    )
    {
        // 1. Validação
        if (sourceTex == null)
        {
            Debug.LogError("A textura de origem é nula.");
            return null;
        }

        int width = sourceTex.width;
        int height = sourceTex.height;

        // 2. Criar e Copiar Textura
        Texture2D newTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        newTex.SetPixels(sourceTex.GetPixels()); // Copia todos os pixels da original
        Color black = Color.black;

        // --- 3. Pintar Bordas Horizontais (Superior e Inferior) ---
        if (drawHorizontal)
        {
            for (int y = 0; y < borderThickness; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Borda Superior
                    newTex.SetPixel(x, height - 1 - y, black);

                    // Borda Inferior
                    newTex.SetPixel(x, y, black);
                }
            }
        }

        // --- 4. Pintar Bordas Verticais (Esquerda e Direita) ---
        if (drawVertical)
        {
            for (int x = 0; x < borderThickness; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Borda Direita
                    newTex.SetPixel(width - 1 - x, y, black);

                    // Borda Esquerda
                    newTex.SetPixel(x, y, black);
                }
            }
        }

        // 5. Aplicar e Retornar
        newTex.Apply();
        return newTex;
    }
}
