using PrintLib;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

public class ImprimirImagem
{
    Printer printer; // our main printer object

    public ImprimirImagem()
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
                UnityEngine.Debug.Log("Instalador do VC++  encontrado!");
            }
        }
        else
        {
            UnityEngine.Debug.Log("Ja existe");
        }
    }
    bool IsVCRedistInstalled()
    {
        // Um jeito simples: verifica se uma DLL cr�tica existe no sistema
        string systemDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.System);
        string vcruntimeDll = Path.Combine(systemDir, "vcruntime140.dll");

        return File.Exists(vcruntimeDll);
    }
    public void PrintOptions(Sprite _image)
    {
        printer = new Printer();

        printer.ShowPrinterDialog(); // abre a janela padrão do Windows

        // change printer settings here!
       /*printer.SetPrinterSettings(
            Orientation.Landscape, // ou Landscape, se quiser horizontal
            148f,105f,           // largura e altura em milímetros
            300,                  // DPI (resolução típica para fotos)
            ColorMode.Color,      // impressão colorida
            1,                    // 1 cópia
            DuplexMode.NoDuplex);*/

        printer.StartDocument();

        Texture2D tex = _image.texture;

        float width = 0, height = 0;
        printer.GetPageSize(ref width, ref height);
        UnityEngine.Debug.Log($"Tamanho aceito: {width}x{height} mm");
        UnityEngine.Debug.Log("Texture size: " + tex.width + "x" + tex.height);

        printer.SetPrintPosition(0f, 0f);
        printer.PrintTexture(tex, 148f, 105f);

        // end document and send to printer!
        printer.EndDocument();
    }
    public void PrintFix(Sprite _image)
    {
        printer = new Printer();

        //printer.ShowPrinterDialog(); // abre a janela padrão do Windows

        // change printer settings here!
        printer.SetPrinterSettings(
             Orientation.Landscape, // ou Landscape, se quiser horizontal
             148f,105f,           // largura e altura em milímetros
             300,                  // DPI (resolução típica para fotos)
             ColorMode.Color,      // impressão colorida
             1,                    // 1 cópia
             DuplexMode.NoDuplex);

        printer.StartDocument();

        Texture2D tex = _image.texture;

        float width = 0, height = 0;
        printer.GetPageSize(ref width, ref height);
        UnityEngine.Debug.Log($"Tamanho aceito: {width}x{height} mm");
        UnityEngine.Debug.Log("Texture size: " + tex.width + "x" + tex.height);

        printer.SetPrintPosition(0f, 0f);
        if (tex.width < tex.height) // está em portrait
        {
            tex = RotateTexture(tex);
        }
        printer.PrintTexture(tex, 148f, 105f);

        // end document and send to printer!
        printer.EndDocument();

        SaveLocal(tex);
    }

    private void SaveLocal(Texture2D tex)
    {
        byte[] bytes = tex.EncodeToPNG();
        string folderPath = Path.Combine(Application.dataPath, "..", "ImagensSalvas");
        Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, "imagem_" + System.DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".png");
        File.WriteAllBytes(filePath, bytes);

        UnityEngine.Debug.Log("Imagem salva em: " + filePath);
    }

    Texture2D RotateTexture(Texture2D original)
    {
        Texture2D rotated = new Texture2D(original.height, original.width);
        for (int x = 0; x < original.width; x++)
        {
            for (int y = 0; y < original.height; y++)
            {
                rotated.SetPixel(y, original.width - 1 - x, original.GetPixel(x, y));
            }
        }
        rotated.Apply();
        return rotated;
    }
}

