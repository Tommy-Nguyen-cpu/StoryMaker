using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Security.Cryptography;
using UnityEngine.TextCore.Text;
using System;
using System.Threading.Tasks;

public class StoryManager : MonoBehaviour
{
    public ApiManager apiManager;

    private GetAvailableVoicesResponse availableVoices;
    private bool enhancePrompt;

    #region Input Prompt UIs
    [SerializeField]
    GameObject inputPanel;

    [SerializeField]
    TMP_InputField promptInputField;

    [SerializeField]
    TMP_Text loadingTextInfo;
    #endregion

    async void Start()
    {
        availableVoices = await apiManager.GetAvailableVoices();

        /*if(availableVoices != null && availableVoices.male_voices.Count > 0)
        {
            apiManager.PlayTTS("Hello world! This is a text to speak generation from a python API!", availableVoices.male_voices[0]);
        }

        test();*/
    }

    async Task<string> EnhanceStoryPrompt(string prompt)
    {
        if (enhancePrompt)
        {
            var enhancePromptResponse = await apiManager.EnhanceDescription(prompt);
            return enhancePromptResponse.enhanced_description;
        }

        return prompt;
    }

    async Task<List<Character>> CreateUniqueCharacters(string storyPrompt)
    {
        var characters = new List<Character>();
        HashSet<string> charactersSet = new HashSet<string>();
        for (int i = 0; i < 2; i++)
        {
            characters.Add((await apiManager.CreateCharacter(storyPrompt, charactersSet)).character);
            charactersSet.Add(characters[i].name);
            Debug.Log($"Name: {characters[i].name}\nGender: {characters[i].gender}\nPersonality: {characters[i].personality}\nDescription: {characters[i].description}");
        }

        return characters;
    }

    async Task<HashSet<string>> GetCharacterConversations(List<Character> characters, string storyDescription)
    {
        var history = new HashSet<string>();

        var conversationLength = UnityEngine.Random.Range(0, Constants.MaxConvLength);
        for (int i = 0; i < conversationLength; i++)
        {
            var randCharacterIdx = UnityEngine.Random.Range(0, characters.Count);
            var newResponse = await apiManager.CreateCharacterTalk("Create a response that matches the characters personality and story.", storyDescription, characters[randCharacterIdx].name, characters[randCharacterIdx].personality, history);
            var newRes = $"{newResponse.character_response.character}: {newResponse.character_response.response}\nAction: {newResponse.character_response.action}";
            Debug.Log(newRes);
            history.Add(newRes);
        }

        return history;
    }

    async void GenerateStory(string prompt)
    {
        try
        {
            loadingTextInfo.text = "Enhancing Prompt if requested...";
            var storyPrompt = await EnhanceStoryPrompt(prompt);

            loadingTextInfo.text = "Creating unique characters...";
            var characters = await CreateUniqueCharacters(storyPrompt);

            loadingTextInfo.text = "Creating character conversations...";
            var conversationHistory = await GetCharacterConversations(characters, storyPrompt);
            loadingTextInfo.gameObject.SetActive(false);
        }
        catch (Exception e)
        {
            loadingTextInfo.text = "Generation failed, please try again!: " + e.Message;
            inputPanel.SetActive(true);
        }
    }

    async void test()
    {
        var enhanced = await apiManager.EnhanceDescription("A short story about a crow that learned how to swear.");
        Debug.Log($"Enhanced description: {enhanced.enhanced_description}\n\nThinking: {enhanced.thinking_content}");

        var characters = new List<CreateCharacterResponse>();
        HashSet<string> charactersSet = new HashSet<string>();
        for (int i = 0; i < 2; i++)
        {
            characters.Add(await apiManager.CreateCharacter(enhanced.enhanced_description, charactersSet));
            charactersSet.Add(characters[i].character.name);
            Debug.Log($"Name: {characters[i].character.name}\nGender: {characters[i].character.gender}\nPersonality: {characters[i].character.personality}\nDescription: {characters[i].character.description}\n\nThinking: {characters[i].thinking_content}");
        }

        var initResponse = await apiManager.CreateCharacterTalk("", enhanced.enhanced_description, characters[0].character.name, characters[0].character.personality);

        string res = $"{initResponse.character_response.character}: {initResponse.character_response.response}\nAction: {initResponse.character_response.action}";
        Debug.Log(res);

        var history = new HashSet<string>();
        history.Add(res);
        for (int i = 1; i < 5; i++)
        {
            var randCharacterIdx = UnityEngine.Random.Range(0, characters.Count);
            var newResponse = await apiManager.CreateCharacterTalk("Create a response that matches the characters personality and story.", enhanced.enhanced_description, characters[randCharacterIdx].character.name, characters[randCharacterIdx].character.personality, history);
            var newRes = $"{newResponse.character_response.character}: {newResponse.character_response.response}\nAction: {newResponse.character_response.action}";
            Debug.Log(newRes);
            history.Add(newRes);
        }
    }

    public void OnEnterForPromptInputField()
    {
        Debug.Log($"Received input: {promptInputField.text}");

        inputPanel.SetActive(false); // Disable UI, since we are now playing the story.
        loadingTextInfo.gameObject.SetActive(true);

        if (string.IsNullOrEmpty(promptInputField.text))
        {
            Debug.LogWarning("No story prompt was provided. Please provide a prompt.");
            return;
        }

        GenerateStory(promptInputField.text);
    }

    public void OnEnhancePromptToggleChange()
    {
        enhancePrompt = !enhancePrompt;
    }
}
