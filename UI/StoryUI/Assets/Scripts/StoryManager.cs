using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using System.Threading.Tasks;
using System;
using System.Security.Cryptography;

public class StoryManager : MonoBehaviour
{
    Config apiConfig;

    async void Start()
    {
        apiConfig = ConfigLoader.LoadConfig();

        var enhanced = await EnhanceDescription("A story about a robot who really wants to dance.");
        Debug.Log($"Enhanced description: {enhanced.enhanced_description}\n\nThinking: {enhanced.thinking_content}");

        var characters = new List<CreateCharacterResponse>();
        string existingCharacters = "";
        for(int i = 0; i < 2; i++)
        {
            characters.Add(await CreateCharacter(enhanced.enhanced_description, existingCharacters));
            existingCharacters += $"{characters[i].character.name},";
            Debug.Log($"\n\nName: {characters[i].character.name}\nGender:{characters[i].character.gender}\nPersonality:{characters[i].character.personality}\nDescription:{characters[i].character.description}\n\nThinking: {characters[i].thinking_content}");
        }

        var initResponse = await CreateCharacterTalk("", enhanced.enhanced_description, characters[0].character.name, characters[0].character.personality);

        string res = $"{initResponse.character_response.character}: {initResponse.character_response.response}";
        Debug.Log(res);

        var history = $"{res}";
        for (int i = 1; i < 5; i++)
        {
            var newResponse = await CreateCharacterTalk("Create a response that matches the characters personality and story.", enhanced.enhanced_description, characters[i].character.name, characters[i].character.personality, history);
            var newRes = $"{newResponse.character_response.character}: {newResponse.character_response.response}";
            Debug.Log(newRes);
            history += $"\n{newRes}";
        }
    }

    async Task<EnhancedStoryDescResponse> EnhanceDescription(string description)
    {
        var payload = new Dictionary<string, string> {{"prompt", description}};
        string json = JsonConvert.SerializeObject(payload);
        try
        {
            var response = await AiRequestAsync<EnhancedStoryDescResponse>(apiConfig.server_url + Constants.EnhanceDescApi, json);
            return response;
        }
        catch (Exception ex)
        {
            Debug.Log($"Failed to enhance description: {ex}");
            return null;
        }
    }

    async Task<CreateCharacterResponse> CreateCharacter(string description, string existingCharacters = null)
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
            var response = await AiRequestAsync<CreateCharacterResponse>(apiConfig.server_url + Constants.CreateCharacterApi, json);
            return response;
        }
        catch(Exception ex)
        {
            Debug.Log($"Failed to create character: {ex}");
            return null;
        }
    }

    async Task<GetCharacterTalkResponse> CreateCharacterTalk(string prompt, string story_description, string character, string personality, string conversationHistory = null)
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
            var response = await AiRequestAsync<GetCharacterTalkResponse>(apiConfig.server_url + Constants.GenerateCharacterResponseApi, json);
            return response;
        }
        catch (Exception e)
        {
            Debug.Log($"Failed to get character response: {e}");
            return null;
        }
    }

    public Task<T> AiRequestAsync<T>(string url, string jsonData)
    {
        var tcs = new TaskCompletionSource<T>();

        StartCoroutine(AiRequestCoroutine(url, jsonData, tcs));

        return tcs.Task;
    }

    private IEnumerator AiRequestCoroutine<T>(string url, string jsonData, TaskCompletionSource<T> tcs)
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
