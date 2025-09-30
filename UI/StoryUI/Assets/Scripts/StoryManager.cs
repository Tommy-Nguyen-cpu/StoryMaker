using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using System.Threading.Tasks;
using System;

public class StoryManager : MonoBehaviour
{
    Config apiConfig;

    async void Start()
    {
        apiConfig = ConfigLoader.LoadConfig();

        var enhanced = await EnhanceDescription("A story about a robot who really wants to dance.");
    }

    async Task<string> EnhanceDescription(string description)
    {
        var payload = new Dictionary<string, string> {{"prompt", description}};
        string json = JsonConvert.SerializeObject(payload);
        try
        {
            var response = await AiRequestAsync(apiConfig.server_url + Constants.EnhanceDescApi, json);
            return response["enhanced_description"];
        }
        catch (Exception ex)
        {
            Debug.Log($"Failed to enhance description: {ex}");
            return null;
        }
    }

    async Task<string> CreateCharacter(string description, string existingCharacters = null)
    {
        var payload = new Dictionary<string, string> { 
            { "story_description", description }
        };
        
        // API will handle if existing characters is not found.
        if (!string.IsNullOrEmpty(existingCharacters))
        {
            payload["existing_characters"] = existingCharacters;
        }

        string json = JsonConvert.SerializeObject(payload);
        try
        {
            var response = await AiRequestAsync(apiConfig.server_url + Constants.CreateCharacterApi, json);
            return response["character"];
        }
        catch(Exception ex)
        {
            Debug.Log($"Failed to create character: {ex}");
            return null;
        }
    }

    async Task<string> CreateCharacterResponse(string prompt, string story_description, string character, string personality, string conversationHistory = null)
    {
        var payload = new Dictionary<string, string> {
            { "prompt", prompt },
            {"story_description", story_description },
            {"character", character },
            {"personality", personality }
        };

        // API will handle if conversation history is missing.
        if (!string.IsNullOrEmpty(conversationHistory))
        {
            payload["conversation_history"] = conversationHistory;
        }

        string json = JsonConvert.SerializeObject(payload);
        try
        {
            var response = await AiRequestAsync(apiConfig.server_url + Constants.CreateCharacterApi, json);
            return response["character_response"];
        }
        catch (Exception e)
        {
            Debug.Log($"Failed to get character response: {e}");
            return null;
        }
    }

    public Task<Dictionary<string, string>> AiRequestAsync(string url, string jsonData)
    {
        var tcs = new TaskCompletionSource<Dictionary<string, string>>();

        StartCoroutine(AiRequestCoroutine(url, jsonData, tcs));

        return tcs.Task;
    }

    private IEnumerator AiRequestCoroutine(string url, string jsonData, TaskCompletionSource<Dictionary<string, string>> tcs)
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
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(respText);
                tcs.SetResult(dict);
            }
            else
            {
                tcs.SetException(new Exception(request.error));
            }
        }
    }

}
