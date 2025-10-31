using PrintLib;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

public class ImprimirNativo : MonoBehaviour
{
    Printer printer;

    public ImprimirNativo()
    {
        if (!IsVCRedistInstalled())
        {
            string installerPath = Path.Combine(Application.streamingAssetsPath, "vc_redist.x64.exe");
            if (File.Exists(installerPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/Q",
                    UseShellExecute = true
                });
            }
            else
            {
                UnityEngine.Debug.Log("Instalador do VC++ não encontrado!");
            }
        }
        else
        {
            UnityEngine.Debug.Log("VC++ já instalado");
        }
    }

    bool IsVCRedistInstalled()
    {
        string systemDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.System);
        string vcruntimeDll = Path.Combine(systemDir, "vcruntime140.dll");
        return File.Exists(vcruntimeDll);
    }

    public void PrintOptions(Sprite _image)
    {
        printer = new Printer();
        printer.ShowPrinterDialog();
        PrintImage(_image);
    }

    public void PrintFix(Sprite _image)
    {
        printer = new Printer();

        printer.SetPrinterSettings(
            Orientation.Portrait,
            75f, 100f,
            300,
            ColorMode.Color,
            1,
            DuplexMode.NoDuplex);

        PrintImage(_image);
    }

    private void PrintImage(Sprite _image)
    {
        try
        {
            printer.StartDocument();

            Texture2D tex = _image.texture;

            // Converte Sprite se estiver em atlas
            if (_image.packed && _image.rect != new Rect(0, 0, tex.width, tex.height))
            {
                tex = SpriteToTexture2D(_image);
            }

            float width = 0, height = 0;
            printer.GetPageSize(ref width, ref height); // deve retornar 75 x 100 agora
            UnityEngine.Debug.Log($"Tamanho pagina: {width}x{height} mm");
            UnityEngine.Debug.Log($"Tamanho texture: {tex.width}x{tex.height}");

            float aspectRatio = (float)tex.width / tex.height;
            float printWidth, printHeight;

            // Ajustando para 75 x 100 portrait
            if (aspectRatio > 1f) // imagem mais larga
            {
                printWidth = width;                 // 75
                printHeight = width / aspectRatio;  // proporcional
            }
            else
            {
                printHeight = height;                // 100
                printWidth = height * aspectRatio;   // proporcional
            }

            // REDUZ TAMANHO EM 8%
            const float scaleFactor = 0.92f;
            printWidth *= scaleFactor;
            printHeight *= scaleFactor;

            // Centraliza
            float posX = (width - printWidth) / 2f;
            float posY = (height - printHeight) / 2f;

            UnityEngine.Debug.Log($"Impressao final: {printWidth}x{printHeight} mm | Pos: ({posX}, {posY})");

            printer.SetPrintPosition(posX, posY);
            printer.PrintTexture(tex, printWidth, printHeight);

            printer.EndDocument();
            printer.ResetPrinterSettings();
            printer = null;

            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            SaveLocal(tex);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Erro na Impressao: {e.Message}");
        }
    }

    private Texture2D SpriteToTexture2D(Sprite sprite)
    {
        Texture2D originalTexture = sprite.texture;
        Rect spriteRect = sprite.rect;

        Texture2D newTexture = new Texture2D((int)spriteRect.width, (int)spriteRect.height);

        Color[] pixels = originalTexture.GetPixels(
            (int)spriteRect.x,
            (int)spriteRect.y,
            (int)spriteRect.width,
            (int)spriteRect.height
        );

        newTexture.SetPixels(pixels);
        newTexture.Apply();

        return newTexture;
    }

    private void SaveLocal(Texture2D tex)
    {
        try
        {
            byte[] bytes = tex.EncodeToPNG();
            string folderPath = Path.Combine(Application.dataPath, "..", "ImagensSalvas");
            Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, "imagem_" + System.DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".png");
            File.WriteAllBytes(filePath, bytes);

            UnityEngine.Debug.Log("Imagem salva em: " + filePath);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Erro ao salvar imagem: {e.Message}");
        }
    }

    // Método alternativo para rotacionar se necessário
    Texture2D RotateTexture(Texture2D original, bool clockwise = true)
    {
        Texture2D rotated = new Texture2D(original.height, original.width);

        for (int x = 0; x < original.width; x++)
        {
            for (int y = 0; y < original.height; y++)
            {
                if (clockwise)
                    rotated.SetPixel(original.height - 1 - y, x, original.GetPixel(x, y));
                else
                    rotated.SetPixel(y, original.width - 1 - x, original.GetPixel(x, y));
            }
        }
        rotated.Apply();
        return rotated;
    }
}
