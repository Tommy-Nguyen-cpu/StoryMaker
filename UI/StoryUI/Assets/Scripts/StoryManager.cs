using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;

public class ApiClientNewtonsoft : MonoBehaviour
{
    IEnumerator Start()
    {
        var config = ConfigLoader.LoadConfig();
        var payload = new Dictionary<string, string> {
            {"title", "My Title"},
            {"prompt", "Once upon a time..."}
        };

        string json = JsonConvert.SerializeObject(payload);
        yield return StartCoroutine(AiRequestCoroutine(config.server_url + "/create_story", json));
    }

    IEnumerator AiRequestCoroutine(string url, string jsonData)
    {
        using (var request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string respText = request.downloadHandler.text;
                Debug.Log("Response: " + respText);

                // Deserialize to dictionary or typed object
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(respText);
                Debug.Log("Status: " + (dict.ContainsKey("status") ? dict["status"] : "<no status>"));
            }
            else
            {
                Debug.LogError($"POST failed: {request.error} (HTTP {(int)request.responseCode})");
                Debug.LogError("Response body: " + request.downloadHandler.text);
            }
        }
    }
}
