using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class LeonardoAPI : MonoBehaviour
{
    public string apiToken = "SEU_TOKEN_AQUI";
    public string idInitImage = "";
    public string photoURL = "";
    public string urlfinalImage = "";
    public string finalPrompt = "";
    public string generationID = "";
    public Image FinalImage;

    #region Init Image
    public void StartUpload()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "CameraPhoto.png");
        StartCoroutine(UploadCoroutine(filePath));
    }

    private IEnumerator UploadCoroutine(string filePath)
    {
        // 1. Requisita os dados de upload (presigned POST)
        var requestData = new { extension = "png" };
        string json = JsonConvert.SerializeObject(requestData);

        using (UnityWebRequest request = new UnityWebRequest("https://cloud.leonardo.ai/api/rest/v1/init-image", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiToken}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Erro ao criar init image: " + request.error);
                yield break;
            }

            Debug.Log("Resposta Init Image: " + request.downloadHandler.text);

            // 2. Parseia corretamente o JSON
            var root = JsonConvert.DeserializeObject<InitImageRoot>(request.downloadHandler.text);
            var uploadData = root.uploadInitImage;
            idInitImage = uploadData.id;
            photoURL = uploadData.url;

            Debug.Log("Init Image ID: " + idInitImage);

            // 3. Converte os 'fields' (que vêm como string JSON) em dicionário
            var fields = JsonConvert.DeserializeObject<Dictionary<string, string>>(uploadData.fields);

            // 4. Monta o formulário multipart POST
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            foreach (var kvp in fields)
                formData.Add(new MultipartFormDataSection(kvp.Key, kvp.Value));

            // adiciona o arquivo
            byte[] fileData = File.ReadAllBytes(filePath);
            formData.Add(new MultipartFormFileSection("file", fileData, Path.GetFileName(filePath), "image/png"));

            // 5. Faz upload via POST para o S3
            using (UnityWebRequest uploadRequest = UnityWebRequest.Post(uploadData.url, formData))
            {
                yield return uploadRequest.SendWebRequest();

                if (uploadRequest.result != UnityWebRequest.Result.Success)
                    Debug.LogError("Erro ao enviar arquivo: " + uploadRequest.error);
                else
                    Debug.Log("Upload concluído com sucesso!");
            }
        }
    }
// Classes para o JSON retornado
[System.Serializable]
public class InitImageRoot
{
    public UploadInitImage uploadInitImage;
}

[System.Serializable]
public class UploadInitImage
{
    public string id;
    public string url;
    public string fields;
    public string key;
}

    #endregion


    #region Generate Image
    // ------------------- GERAÇÃO -------------------
    [System.Serializable]
    public class SDJobResponse
    {
        public SDGenerationJob sdGenerationJob;
    }

    [System.Serializable]
    public class SDGenerationJob
    {
        public string generationId;
        public int apiCreditCost;
    }

    [System.Serializable]
    public class GeneratedImage
    {
        public string url;
        public string id;
    }

    [System.Serializable]
    public class GenerationsByPK
    {
        public List<GeneratedImage> generated_images;
        public string status;
        public string id;
    }

    [System.Serializable]
    public class GenerationResponse
    {
        public GenerationsByPK generations_by_pk;
    }

    // ---------- FUNÇÃO PÚBLICA PARA GERAR IMAGEM ----------
    public void GenerateImage()
    {
        StartCoroutine(GenerateCoroutine(finalPrompt, idInitImage));
    }

    // ---------- COROUTINE DE GERAÇÃO ----------
    private IEnumerator GenerateCoroutine(string _prompt, string initImageId)
    {  
        var requestData = new
        {
            // --- CORREÇÃO DO PROMPT E MODELO ---
            prompt = _prompt, // Usamos o prompt detalhado para o close-up
            modelId = "1e60896f-3c26-4296-8ecc-53e2afecc132", // Trocamos para o modelo de melhor fotorrealismo

            // --- FIDELIDADE FACIAL (CORREÇÃO CRÍTICA) ---
            init_image_id = string.IsNullOrEmpty(initImageId) ? null : initImageId,

            // REDUÇÃO CRÍTICA: 0.5 é muito alto. Usamos 0.05 (ou 0.01) para manter o rosto.
            init_strength = 0.5f,
            // ---------------------------------------------

            num_images = 1,

            // --- AUMENTO DA RESOLUÇÃO PARA QUALIDADE (Retrato) ---
            width = 1024,
            height = 1536,

            // --- Otimizações de Qualidade ---
            alchemy = true,
            guidance_scale = 7,
            @public = false
        };

        string json = JsonConvert.SerializeObject(requestData);

        using (UnityWebRequest request = new UnityWebRequest("https://cloud.leonardo.ai/api/rest/v1/generations", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiToken}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Erro ao iniciar geração: " + request.downloadHandler.text);
                yield break;
            }

            SDJobResponse jobResponse = JsonConvert.DeserializeObject<SDJobResponse>(request.downloadHandler.text);
            string generationID = jobResponse.sdGenerationJob.generationId;
            Debug.Log("Geração iniciada: " + generationID);

            // Iniciar polling até a imagem estar pronta
            yield return StartCoroutine(PollGeneration(generationID));
        }
    }

    // ---------- COROUTINE DE POLLING ----------
    private IEnumerator PollGeneration(string generationId, int maxAttempts = 30, float intervalSeconds = 3f)
    {
        int attempt = 0;

        while (attempt < maxAttempts)
        {
            attempt++;
            Debug.Log($"Verificando status da geração ({attempt}/{maxAttempts})...");

            using (UnityWebRequest request = UnityWebRequest.Get($"https://cloud.leonardo.ai/api/rest/v1/generations/{generationId}"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {apiToken}");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Erro ao checar status da geração: {request.downloadHandler.text}");
                    yield break;
                }

                GenerationResponse response = JsonConvert.DeserializeObject<GenerationResponse>(request.downloadHandler.text);

                if (response.generations_by_pk != null &&
                    response.generations_by_pk.generated_images != null &&
                    response.generations_by_pk.generated_images.Count > 0 &&
                    response.generations_by_pk.status == "COMPLETE")
                {
                    Debug.Log("Imagem pronta! Baixando...");
                    foreach (var img in response.generations_by_pk.generated_images)
                    {
                        Debug.Log("URL da imagem: " + img.url);
                        StartCoroutine(DownloadImage(img.url));
                    }
                    yield break; // imagem pronta, sair do loop
                }
                else
                {
                    Debug.Log("Imagem ainda não gerada, aguardando...");
                }
            }

            yield return new WaitForSeconds(intervalSeconds);
        }

        Debug.LogError($"Tentativas esgotadas ({maxAttempts}) e a imagem não foi gerada.");
    }

    // ---------- DOWNLOAD DA IMAGEM ----------
    private IEnumerator DownloadImage(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Erro ao baixar imagem: " + request.error);
            }
            else
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(request);
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                FinalImage.sprite = sprite;
                Debug.Log($"Imagem baixada com sucesso! Tamanho: {tex.width}x{tex.height}");
            }
        }
    }

    #endregion


    #region TechUnity
    public void SetPrompt(string _prompt)
    {
        finalPrompt = _prompt;
    }
    #endregion
}
