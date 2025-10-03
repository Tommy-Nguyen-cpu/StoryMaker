using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

public class StoryManager : MonoBehaviour
{
    Config apiConfig;

    async void Start()
    {
        apiConfig = ConfigLoader.LoadConfig();

        var enhanced = await EnhanceDescription("A story about a robot who really wants to dance.");
        Debug.Log($"Enhanced description: {enhanced.enhanced_description}\n\nThinking: {enhanced.thinking_content}");

        var characters = new List<CreateCharacterResponse>();
        HashSet<string> charactersSet = new HashSet<string>();
        for(int i = 0; i < 2; i++)
        {
            characters.Add(await CreateCharacter(enhanced.enhanced_description, charactersSet));
            charactersSet.Add(characters[i].character.name);
            Debug.Log($"Name: {characters[i].character.name}\nGender: {characters[i].character.gender}\nPersonality: {characters[i].character.personality}\nDescription: {characters[i].character.description}\n\nThinking: {characters[i].thinking_content}");
        }

        var initResponse = await CreateCharacterTalk("", enhanced.enhanced_description, characters[0].character.name, characters[0].character.personality);

        string res = $"{initResponse.character_response.character}: {initResponse.character_response.response}";
        Debug.Log(res);

        var history = new HashSet<string>();
        history.Add(res);
        for (int i = 1; i < 5; i++)
        {
            var randCharacterIdx = UnityEngine.Random.Range(0, characters.Count);
            var newResponse = await CreateCharacterTalk("Create a response that matches the characters personality and story.", enhanced.enhanced_description, characters[randCharacterIdx].character.name, characters[randCharacterIdx].character.personality, history);
            var newRes = $"{newResponse.character_response.character}: {newResponse.character_response.response}";
            Debug.Log(newRes);
            history.Add(newRes);
        }
    }

    async Task<EnhancedStoryDescResponse> EnhanceDescription(string description)
    {
        var payload = new Dictionary<string, string> {{"prompt", description}};
        string json = JsonConvert.SerializeObject(payload);
        var response = await AiRequestHandler<EnhancedStoryDescResponse>(apiConfig.server_url + Constants.EnhanceDescApi, json);
        return response;
    }

    async Task<CreateCharacterResponse> CreateCharacter(string description, HashSet<string> existingCharacters = null)
    {
        var payload = new Dictionary<string, string> { 
            { "story_description", description }
        };
        
        if(existingCharacters != null && existingCharacters.Count > 0)
        {
            var existingCharactersString = string.Join(",", existingCharacters.ToArray());
            payload["existing_characters"] = existingCharactersString;
        }

        for (int i = 0; i < Constants.MAX_RETRY; i++)
        {
            string json = JsonConvert.SerializeObject(payload);
            var response = await AiRequestHandler<CreateCharacterResponse>(apiConfig.server_url + Constants.CreateCharacterApi, json);

            if(response != null && existingCharacters != null && existingCharacters.Contains(response.character.name))
            {
                Debug.Log($"(Attempt {i}) AI generated existing character. Generating again...");
                continue;
            }

            return response;
        }

        return null;
    }

    async Task<GetCharacterTalkResponse> CreateCharacterTalk(string additional_notes, string story_description, string character, string personality, HashSet<string> conversationHistory = null)
    {
        var payload = new Dictionary<string, string> {
            { "additional_notes", additional_notes },
            {"story_description", story_description },
            {"character", character },
            {"personality", personality }
        };


        if (conversationHistory != null && conversationHistory.Count > 0)
        {
            // API will handle if conversation history is missing.
            var existingCharactersString = string.Join(",", conversationHistory.ToArray());
            payload["existing_characters"] = existingCharactersString;
        }

        for (int i = 0; i < Constants.MAX_RETRY; i++)
        {
            string json = JsonConvert.SerializeObject(payload);
            var response = await AiRequestHandler<GetCharacterTalkResponse>(apiConfig.server_url + Constants.GenerateCharacterResponseApi, json);

            if (response != null && conversationHistory != null && conversationHistory.Contains(response.character_response.response))
            {
                Debug.Log($"(Attempt {i} AI generated existing dialogue. Generating again...");
                continue;
            }

            return response;
        }

        return null;
    }

    public async Task<T> AiRequestHandler<T>(string url, string jsonData)
    {
        for (int i = 0; i < Constants.MAX_RETRY; i++)
        {
            try
            {
                return await AiRequestAsync<T>(url, jsonData);
            }
            catch (Exception e)
            {
                Debug.Log($"(Attempt {i}) API Failed With Error: {e} ");
            } // If it succeeds, it will break out of the loop, otherwise we will try again.
        }

        return default(T);
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
