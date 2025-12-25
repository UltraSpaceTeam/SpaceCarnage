using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
namespace Network
{
    public class APINetworkManager : MonoBehaviour
    {
        public static APINetworkManager Instance { get; private set; }

        // Потом надо указать адрес реального сервера
        private const string BASE_URL = "https://yarlkot.isgood.host:9087/gameapi";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task<TResult> PostRequestAsync<TResult>(string endpoint, object payload)
        {
            string url = BASE_URL + endpoint;
            string json = JsonUtility.ToJson(payload);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                var operation = request.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return JsonUtility.FromJson<TResult>(request.downloadHandler.text);
                }
                else
                {
                    string errorMsg = "Unknown Error";
                    try
                    {
                        var errorResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                        if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.error))
                            errorMsg = errorResponse.error;
                    }
                    catch
                    {
                        errorMsg = request.error;
                    }

                    throw new Exception(errorMsg);
                }
            }
        }
    }
}