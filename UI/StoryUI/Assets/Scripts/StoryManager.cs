using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

public class StoryManager : MonoBehaviour
{
    public AudioSource audioSource;
    Config apiConfig;

    #region Fields
    BasicApiHandler apiHandler;
    MultiMediaApiHandler multiMediaApiHandler;
    #endregion

    async void Start()
    {
        apiHandler = BasicApiHandler.Instance;
        multiMediaApiHandler = MultiMediaApiHandler.Instance;

        apiConfig = ConfigLoader.LoadConfig();

        var availableVoices = await GetAvailableVoices();

        if(availableVoices != null && availableVoices.male_voices.Count > 0)
        {
            PlayTTS("Hello world! This is a text to speak generation from a python API!", availableVoices.male_voices[0]);
        }

        test();
    }

    async void test()
    {
        var enhanced = await EnhanceDescription("A story about a robot who really wants to dance.");
        Debug.Log($"Enhanced description: {enhanced.enhanced_description}\n\nThinking: {enhanced.thinking_content}");

        var characters = new List<CreateCharacterResponse>();
        HashSet<string> charactersSet = new HashSet<string>();
        for (int i = 0; i < 2; i++)
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

    #region LLM Methods
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
    async Task<GetAvailableVoicesResponse> GetAvailableVoices()
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
