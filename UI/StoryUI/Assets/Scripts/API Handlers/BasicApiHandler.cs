using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class BasicApiHandler
{
    #region Singleton
    private static BasicApiHandler instance = new BasicApiHandler();
    public static BasicApiHandler Instance { get { return instance; } }

    private BasicApiHandler() { }
    #endregion

    public IEnumerator AiRequestCoroutine<T>(string url, string jsonData, TaskCompletionSource<T> tcs, string httpMethod)
    {
        using (var request = new UnityWebRequest(url, httpMethod))
        {
            if (!string.IsNullOrEmpty(jsonData))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string respText = request.downloadHandler.text;
                Debug.Log($"Response from Server: {respText}");
                var dict = JsonConvert.DeserializeObject<T>(respText);
                tcs.SetResult(dict);
            }
            else
            {
                tcs.SetException(new Exception(request.error));
            }
        }
    }
}
