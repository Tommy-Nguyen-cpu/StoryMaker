using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;
using System.Threading.Tasks;

public class StoryManager : MonoBehaviour
{
    public ApiManager apiManager;

    private GetAvailableVoicesResponse availableVoices;
    private HashSet<string> usedVoices = new HashSet<string>();
    private Dictionary<string, GameObject> characterMapper = new Dictionary<string, GameObject>();

    private bool enhancePrompt = true;

    #region Input Prompt UIs
    [SerializeField]
    GameObject inputPanel;

    [SerializeField]
    TMP_InputField promptInputField;

    [SerializeField]
    TMP_Text loadingTextInfo;
    #endregion

    #region Prefabs
    [SerializeField]
    private GameObject characterPrefab;
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
            var character = (await apiManager.CreateCharacter(storyPrompt, charactersSet)).character;
            characters.Add(character);
            charactersSet.Add(characters[i].name);
            Debug.Log($"Name: {characters[i].name}\nGender: {characters[i].gender}\nPersonality: {characters[i].personality}\nDescription: {characters[i].description}");

            characterMapper.Add(character.name, InstantiateCharacterPrefab(character));
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
            history.Add(newResponse.character_response.response);
        }

        return history;
    }

    private string GetUniqueVoice(List<string> voices)
    {
        foreach(var voice in voices)
        {
            if (usedVoices.Contains(voice))
            {
                continue;
            }

            usedVoices.Add(voice);
            return voice;
        }

        return voices[0]; // TODO: There is a much better way of doing this.
    }

    private GameObject InstantiateCharacterPrefab(Character charInfo)
    {
        var instantiatedGameObj = Instantiate(characterPrefab, new Vector3(UnityEngine.Random.Range(0, 20), 10), Quaternion.identity);

        Debug.Log("Got to retrieving script");
        var movementScript = instantiatedGameObj.GetComponent<AiCharacterController>();
        movementScript.CharacterInfo = charInfo;
        Debug.Log("Setted character info.");
        movementScript.CharacterVoice = GetUniqueVoice(charInfo.gender == "male" ? availableVoices.male_voices : availableVoices.female_voices);
        Debug.Log("Setted character voice.");

        return instantiatedGameObj;
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
        Debug.Log($"Enhancing prompt: {enhancePrompt}");
    }
}
