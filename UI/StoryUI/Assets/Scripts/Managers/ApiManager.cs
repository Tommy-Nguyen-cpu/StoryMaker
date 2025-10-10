using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ApiManager : MonoBehaviour
{
    #region Fields
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

    public async Task<GetCharacterTalkResponse> CreateCharacterTalk(string additional_notes, string story_description, string character, string personality, HashSet<string> conversationHistory = null, List<string> availableActions = null)
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
            payload["conversation_history"] = existingCharactersString;
        }

        if(availableActions != null && availableActions.Count > 0)
        {
            var availableActionsString = string.Join(",", availableActions.ToArray());
            payload["available_actions"] = availableActionsString;
        }

        for (int i = 0; i < Constants.MAX_RETRY; i++)
        {
            string json = JsonConvert.SerializeObject(payload);
            var response = await AiRequestHandler<GetCharacterTalkResponse>(apiConfig.server_url + Constants.GenerateCharacterResponseApi, json, "GET");

            if (response != null && conversationHistory != null && conversationHistory.Contains($"{response.character_response.character}: {response.character_response.response}"))
            {
                Debug.Log($"(Attempt {i}) AI generated existing dialogue. Generating again...");
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

    // In ApiManager.cs
    public IEnumerator SetTTS(string text, string voice, AudioSource audioSource)
    {
        // Build the TTS URL or POST and get back a url to the wav/mp3.
        // Replace BuildTtsUrlForText with your implementation that returns the audio file URL.
        string url = $"{apiConfig.server_url}{Constants.ttsApi}?text={UnityWebRequest.EscapeURL(text)}&voice={voice}";

        // Optionally: log the URL
        Debug.Log($"Requesting TTS at: {url}");

        // Use your downloader which sets audioSource.clip when done
        yield return MultiMediaApiHandler.Instance.PlayFromUrl(url, audioSource);

        // Sanity checks
        if (audioSource.clip == null)
        {
            Debug.LogError("SetTTSCoroutine: clip is still null after download.");
            yield break;
        }

        // Wait until clip is fully loaded if needed
        float waitStart = Time.time;
        float timeout = 10f;
        while (audioSource.clip.loadState != AudioDataLoadState.Loaded && Time.time - waitStart < timeout)
        {
            yield return null;
        }

        if (audioSource.clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogWarning($"Clip did not reach Loaded in {timeout}s. loadState={audioSource.clip.loadState}");
        }
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
