using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;

public class ApiManager : MonoBehaviour
{
    #region Fields
    public AudioSource audioSource;
    Config apiConfig;
    BasicApiHandler apiHandler;
    MultiMediaApiHandler multiMediaApiHandler;
    #endregion

    public void Awake()
    {
        apiConfig = ConfigLoader.LoadConfig();
        apiHandler = BasicApiHandler.Instance;
        multiMediaApiHandler = MultiMediaApiHandler.Instance;
    }

    #region LLM Methods
    public async Task<EnhancedStoryDescResponse> EnhanceDescription(string description)
    {
        var payload = new Dictionary<string, string> { { "prompt", description } };
        string json = JsonConvert.SerializeObject(payload);
        var response = await AiRequestHandler<EnhancedStoryDescResponse>(apiConfig.server_url + Constants.EnhanceDescApi, json);
        return response;
    }

    public async Task<CreateCharacterResponse> CreateCharacter(string description, HashSet<string> existingCharacters = null)
    {
        var payload = new Dictionary<string, string> {
            { "story_description", description }
        };

        if (existingCharacters != null && existingCharacters.Count > 0)
        {
            var existingCharactersString = string.Join(",", existingCharacters.ToArray());
            payload["existing_characters"] = existingCharactersString;
        }

        for (int i = 0; i < Constants.MAX_RETRY; i++)
        {
            string json = JsonConvert.SerializeObject(payload);
            var response = await AiRequestHandler<CreateCharacterResponse>(apiConfig.server_url + Constants.CreateCharacterApi, json);

            if (response != null && existingCharacters != null && existingCharacters.Contains(response.character.name))
            {
                Debug.Log($"(Attempt {i}) AI generated existing character. Generating again...");
                continue;
            }

            return response;
        }

        return null;
    }

    public async Task<GetCharacterTalkResponse> CreateCharacterTalk(string additional_notes, string story_description, string character, string personality, HashSet<string> conversationHistory = null)
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
            var response = await AiRequestHandler<GetCharacterTalkResponse>(apiConfig.server_url + Constants.GenerateCharacterResponseApi, json, "GET");

            if (response != null && conversationHistory != null && conversationHistory.Contains(response.character_response.response))
            {
                Debug.Log($"(Attempt {i} AI generated existing dialogue. Generating again...");
                continue;
            }

            return response;
        }

        return null;
    }
    #endregion

    #region TTS Methods
    public async Task<GetAvailableVoicesResponse> GetAvailableVoices()
    {
        return await AiRequestAsync<GetAvailableVoicesResponse>(apiConfig.server_url + Constants.GetAvailableVoicesApi, "", "GET");
    }

    public void PlayTTS(string text, string voice)
    {
        // Build the URL, e.g. encode text & voice
        string url = $"{apiConfig.server_url}{Constants.ttsApi}?text={UnityWebRequest.EscapeURL(text)}&voice={voice}";
        StartCoroutine(multiMediaApiHandler.PlayFromUrl(url, audioSource));
    }
    #endregion

    #region Helper Methods

    public async Task<T> AiRequestHandler<T>(string url, string jsonData, string httpMethod = "POST")
    {
        for (int i = 0; i < Constants.MAX_RETRY; i++)
        {
            try
            {
                return await AiRequestAsync<T>(url, jsonData, httpMethod);
            }
            catch (Exception e)
            {
                Debug.Log($"(Attempt {i}) API Failed With Error: {e} ");
            } // If it succeeds, it will break out of the loop, otherwise we will try again.
        }
        return default(T);
    }

    public Task<T> AiRequestAsync<T>(string url, string jsonData, string httpMethod)
    {
        var tcs = new TaskCompletionSource<T>();

        StartCoroutine(apiHandler.AiRequestCoroutine(url, jsonData, tcs, httpMethod));

        return tcs.Task;
    }
    #endregion
}
