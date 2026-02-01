using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}

public class APINetworkManager : MonoBehaviour
{
    public static APINetworkManager Instance { get; private set; }

    private const string BASE_URL = "https://yarlkot.ru:9087/gameapi";
    public static string AuthToken { get; private set; }
    public static void SetToken(string token) => AuthToken = token;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public async Task<TResult> PostRequestAsync<TResult>(string endpoint, object payload)
    {
        string url = BASE_URL + endpoint;
        string json = JsonUtility.ToJson(payload);

        Debug.Log($"[API] POST Request to IP: {url}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            request.certificateHandler = new BypassCertificate();
            request.disposeCertificateHandlerOnDispose = true;

            var operation = request.SendWebRequest();

            float timer = 0f;
            while (!operation.isDone)
            {
                await Task.Yield();
                timer += Time.unscaledDeltaTime;

                if (timer % 1.0f < Time.unscaledDeltaTime)
                    Debug.Log($"[API] Waiting... {timer:F1}s");

                if (timer > 10f)
                {
                    Debug.LogError($"[API] HARD TIMEOUT on {url}. Aborting.");
                    request.Abort();
                    throw new Exception("Hard Timeout: Server did not respond.");
                }
            }

            Debug.Log($"[API RAW RESPONSE]: {request.downloadHandler.text}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                return JsonUtility.FromJson<TResult>(request.downloadHandler.text);
            }
            else
            {
                HandleError(request);
                return default;
            }
        }
    }

    public async Task<TResult> GetRequestAsync<TResult>(string endpoint, string queryParams = null)
    {
        string url = BASE_URL + endpoint;
        if (!string.IsNullOrEmpty(queryParams)) url += "?" + queryParams;

        Debug.Log($"[API] GET Request to IP: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(AuthToken))
                request.SetRequestHeader("Authorization", "Bearer " + AuthToken);

            request.certificateHandler = new BypassCertificate();
            request.disposeCertificateHandlerOnDispose = true;

            var operation = request.SendWebRequest();

            float timer = 0f;
            while (!operation.isDone)
            {
                await Task.Yield();
                timer += Time.unscaledDeltaTime;

                if (timer > 20f)
                {
                    Debug.LogError($"[API] HARD TIMEOUT on {url}. Aborting.");
                    request.Abort();
                    throw new Exception("Hard Timeout");
                }
            }

            Debug.Log($"[API RAW RESPONSE]: {request.downloadHandler.text}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                return JsonUtility.FromJson<TResult>(request.downloadHandler.text);
            }
            else
            {
                HandleError(request);
                return default;
            }
        }
    }

    private void HandleError(UnityWebRequest request)
    {
        string errorMsg = request.error;
        try
        {
            var errorResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
            if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.error))
                errorMsg = errorResponse.error;
        }
        catch { }

        Debug.LogError($"[API ERROR]: {errorMsg} | Body: {request.downloadHandler.text}");
        throw new Exception(errorMsg);
    }
}