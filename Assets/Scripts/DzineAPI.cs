using System;
using System.Collections;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Net;

public class DzineAPI : MonoBehaviour
{
    private string apiKey;

    //private string sourceFile = "RodrigoSantoro.png"; // dentro de StreamingAssets
    //private string destFile = "Dest.png";
    private string destFile = "CameraPhoto.png";

    private FaceData sourceFace;
    private FaceData destFace;
    string taskId = "";
    public List<string> faceSwapUrls = new List<string>();
    public List<Image> images = new List<Image>();
    [SerializeField] private List<string> bgsMan = new List<string>();
    [SerializeField] private List<string> bgsWomen = new List<string>();
    [HideInInspector] public bool isImageReady = false;
    [SerializeField] private TextMeshProUGUI feedbackTemp;
    public bool isMan = false;
    

    [System.Serializable]
    public class FaceData
    {
        public float[] bbox;
        public float[][] kps;
    }

    private void Awake()
    {
        //destFile = Path.Combine(Application.persistentDataPath, "CameraPhoto.png");
        isImageReady = false;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        apiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJPcGVuQUlEIjoxMzgsIk9wZW5VSUQiOjExMzkzOTI0MTMwNTEsIk1JRCI6ODM4NDAyNywiQ3JlYXRlVGltZSI6MTc1OTgzNjAzNiwiaXNzIjoiZHppbmUiLCJzdWIiOiJvcGVuIn0.1fROPj_gIZ4ZNvgnskPN1_R6bGj4KtEfQcxSpq89Tzo";
    }

    void Start()
    {
        //StartCoroutine(ProcessFaceSwap());
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            //StartCoroutine(ProcessFaceSwap(0));
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //StartCoroutine(GetFaceSwapResult(taskId));
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            for (int i = 0; i < faceSwapUrls.Count; i++)
            {
                //StartCoroutine(DownloadImage(faceSwapUrls[i],i));
            }
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            //ImprimirImagem printer = new ImprimirImagem();
            //printer.PrintNew(images[0].sprite);
        }
    }
    public void DownloadResults()
    {
        for (int i = 0; i < faceSwapUrls.Count; i++)
        {
            StartCoroutine(DownloadImage(faceSwapUrls[i], i));
        }
    }
    public void ProcessFace(int _indexBG)
    {
        StartCoroutine(ProcessFaceSwap(_indexBG));
    }
    public void SetMan(bool _isman)
    {
        isMan = _isman; 
    }

    IEnumerator ProcessFaceSwap(int _indexBG)
    {
        List<string> bgs = new List<string>();
        if(isMan)
        {
            foreach(string _bg in bgsMan)
            {
                bgs.Add(_bg);
            }
        }
        else
        {
            foreach (string _bg in bgsWomen)
            {
                bgs.Add(_bg);
            }
        }
        //Upload da imagem source
        string sourceUrl = null;
        yield return StartCoroutine(UploadImage(Path.Combine(Application.streamingAssetsPath, bgs[_indexBG]), result => sourceUrl = result));
        if (string.IsNullOrEmpty(sourceUrl))
        {
            Debug.LogError("Falha ao fazer upload da imagem source.");
            yield break;
        }

        // 2️⃣ Upload da imagem dest
        string destUrl = null;
        yield return StartCoroutine(UploadImage(Path.Combine(Application.persistentDataPath, destFile), result => destUrl = result));
        if (string.IsNullOrEmpty(destUrl))
        {
            Debug.LogError("Falha ao fazer upload da imagem dest.");
            yield break;
        }

        // 3️⃣ Detecta rosto da source via URL
        yield return StartCoroutine(FaceDetectFromUrl(sourceUrl, faceData => sourceFace = faceData));
        if (sourceFace == null)
        {
            Debug.LogError("Nenhuma face detectada na source.");
            yield break;
        }

        // 4️⃣ Detecta rosto da dest via URL
        yield return StartCoroutine(FaceDetectFromUrl(destUrl, faceData => destFace = faceData));
        if (destFace == null)
        {
            Debug.LogError("Nenhuma face detectada na dest.");
            yield break;
        }

        // 5️⃣ Faz Face Swap: source → dest
        yield return StartCoroutine(FaceSwapFromUrls(sourceUrl, destUrl, sourceFace, destFace));
    }

    public IEnumerator FaceDetectFromUrl(string imageUrl, Action<FaceData> callback)
    {
        var requestData = new
        {
            images = new[]
            {
                new { url = imageUrl }
            }
        };

        string jsonBody = JsonConvert.SerializeObject(requestData);

        UnityWebRequest request = new UnityWebRequest("https://papi.dzine.ai/openapi/v1/face_detect", "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonConvert.DeserializeObject<FaceDetectResponseWrapper>(request.downloadHandler.text);
            if (response.data.face_list.Length > 0)
            {
                callback(response.data.face_list[0]);
            }
            else
            {
                Debug.LogWarning("Nenhuma face detectada");
                callback(null);
            }
        }
        else
        {
            Debug.LogError("Erro na requisição: " + request.error);
            callback(null);
        }
    }

    [System.Serializable]
    public class FaceDetectResponseWrapper
    {
        public FaceDetectData data;
    }

    [System.Serializable]
    public class FaceDetectData
    {
        public FaceData[] face_list;
    }

    IEnumerator FaceSwapFromUrls(string sourceUrl, string destUrl, FaceData sourceFace, FaceData destFace)
    {
        if (string.IsNullOrEmpty(sourceUrl) || string.IsNullOrEmpty(destUrl))
        {
            Debug.LogError("URLs inválidas para Face Swap.");
            yield break;
        }

        // Log para conferência
        Debug.Log("Source URL: " + sourceUrl);
        Debug.Log("Dest URL: " + destUrl);

        // Monta o JSON para Face Swap
        var jsonObj = new
        {
            source_face_image = sourceUrl,   // Rosto que será copiado
            dest_face_image = destUrl,       // Imagem que receberá o rosto
            source_face_coordinate = new
            {
                bbox = sourceFace.bbox,
                kps = sourceFace.kps
            },
            dest_face_coordinate = new
            {
                bbox = destFace.bbox,
                kps = destFace.kps
            },
            generate_slots = new int[] { 1, 1, 1, 1 },
            output_format = "jpeg"
        };

        string jsonBody = JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.None);
        Debug.Log("JSON enviado para Face Swap: " + jsonBody);

        UnityWebRequest request = new UnityWebRequest("https://papi.dzine.ai/openapi/v1/create_task_face_swap", "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonConvert.DeserializeObject<FaceSwapTaskResponse>(request.downloadHandler.text);
            if (response != null && response.data != null && !string.IsNullOrEmpty(response.data.task_id))
            {
                taskId = response.data.task_id;
                Debug.Log("Task ID criado: " + taskId);
            }
            else
            {
                Debug.LogError("Não foi possível obter o task_id da resposta.");
            }
        }
        else
        {
            Debug.LogError("Erro Face Swap: " + request.error + " / Response: " + request.downloadHandler.text);
        }
    }

    private IEnumerator UploadImage(string filePath, Action<string> callback)
    {
        byte[] imageData = File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", imageData, Path.GetFileName(filePath), "image/png");

        UnityWebRequest request = UnityWebRequest.Post("https://papi.dzine.ai/openapi/v1/file/upload", form);
        request.SetRequestHeader("Authorization", apiKey);

        yield return request.SendWebRequest();

        Debug.Log("Upload Response: " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            var json = JsonUtility.FromJson<UploadResponseGeneric>(request.downloadHandler.text);
            string fileUrl = json?.data?.file_path ?? json?.data?.url;

            if (!string.IsNullOrEmpty(fileUrl))
                callback?.Invoke(fileUrl);
            else
            {
                Debug.LogError("Upload sem URL válida.");
                callback?.Invoke(null);
            }
        }
        else
        {
            Debug.LogError("Erro Upload: " + request.error);
            callback?.Invoke(null);
        }
    }
    [Serializable]
    private class UploadResponseGeneric
    {
        public UploadDataGeneric data;
    }

    [Serializable]
    private class UploadDataGeneric
    {
        public string file_path;
        public string url;
    }
    public void GetFaceSwap()
    {
        StartCoroutine(GetFaceSwapResult(taskId));
    }
    IEnumerator GetFaceSwapResult(string taskId)
    {
        string url = "https://papi.dzine.ai/openapi/v1/get_task_progress/" + taskId;
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<FaceSwapProgressResponse>(request.downloadHandler.text);

            if (response.data != null && response.data.status == "succeed")
            {
                faceSwapUrls.Clear();
                foreach (var imgUrl in response.data.generate_result_slots)
                {
                    if (!string.IsNullOrEmpty(imgUrl))
                        faceSwapUrls.Add(imgUrl);
                }

                Debug.Log("Links adicionados à lista:");
                foreach (var link in faceSwapUrls)
                    Debug.Log(link);

                isImageReady = true;
            }
            else
            {
                Debug.Log("Task ainda em processamento ou falhou: " + response.data.status);
                feedbackTemp.text = response.data.status;

            }
        }
        else
        {
            Debug.LogError("Erro ao consultar progresso da task: " + request.error);
            feedbackTemp.text = request.error;
        }
    }

    IEnumerator DownloadImage(string imageUrl,int _index)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Erro ao baixar imagem: " + request.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                Rect rect = new Rect(0, 0, texture.width, texture.height);
                Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
                images[_index].gameObject.SetActive(true);
                images[_index].sprite = sprite;

                // Se quiser salvar localmente:
                byte[] bytes = texture.EncodeToPNG();
                string path = Application.persistentDataPath + $"/{_index}imagem_baixada.png";
                System.IO.File.WriteAllBytes(path, bytes);
                Debug.Log("Imagem salva em: " + path);
            }
        }
    }

    [System.Serializable]
    public class FaceSwapProgressResponse
    {
        public FaceSwapProgressData data;
    }

    [System.Serializable]
    public class FaceSwapProgressData
    {
        public string task_id;
        public string status;
        public string[] generate_result_slots;
    }
    [Serializable]
    public class FaceSwapTaskResponse
    {
        public FaceSwapTaskData data;
    }

    [Serializable]
    public class FaceSwapTaskData
    {
        public string task_id;
    }

}
