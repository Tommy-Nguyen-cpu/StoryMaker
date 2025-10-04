using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Security.Cryptography;
using UnityEngine.TextCore.Text;

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

    string CreateStoryPrompt(string prompt)
    {
        if (enhancePrompt)
        {
            var enhancePromptResponse = apiManager.EnhanceDescription(prompt);
            return enhancePromptResponse.Result.enhanced_description;
        }

        return prompt;
    }

    List<Character> CreateUniqueCharacters(string storyPrompt, out HashSet<string> uniqueChars)
    {
        var characters = new List<Character>();
        HashSet<string> charactersSet = new HashSet<string>();
        for (int i = 0; i < 2; i++)
        {
            characters.Add(apiManager.CreateCharacter(storyPrompt, charactersSet).Result.character);
            charactersSet.Add(characters[i].name);
            Debug.Log($"Name: {characters[i].name}\nGender: {characters[i].gender}\nPersonality: {characters[i].personality}\nDescription: {characters[i].description}");
        }

        uniqueChars= charactersSet;
        return characters;
    }

    HashSet<string> GetCharacterConversations(List<Character> characters, string storyDescription)
    {
        var history = new HashSet<string>();

        var conversationLength = Random.Range(0, Constants.MaxConvLength);
        for (int i = 0; i < conversationLength; i++)
        {
            var randCharacterIdx = Random.Range(0, characters.Count);
            var newResponse = apiManager.CreateCharacterTalk("Create a response that matches the characters personality and story.", storyDescription, characters[randCharacterIdx].name, characters[randCharacterIdx].personality, history).Result;
            var newRes = $"{newResponse.character_response.character}: {newResponse.character_response.response}\nAction: {newResponse.character_response.action}";
            Debug.Log(newRes);
            history.Add(newRes);
        }

        return history;
    }

    void GenerateStory(string prompt)
    {
        var storyPrompt = CreateStoryPrompt(prompt);

        var characters = CreateUniqueCharacters(storyPrompt, out var uniqueChars);

        var conversationHistory = GetCharacterConversations(characters, storyPrompt);
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
        Debug.Log($"Recived input: {promptInputField.text}");

        inputPanel.SetActive(false); // Disable UI, since we are now playing the story.

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
