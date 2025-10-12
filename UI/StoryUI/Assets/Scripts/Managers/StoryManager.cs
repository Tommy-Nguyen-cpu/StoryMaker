using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;
using System.Threading.Tasks;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine.UI;

using Random = UnityEngine.Random;
using System.Linq;

public class StoryManager : MonoBehaviour
{
    public ApiManager apiManager;

    private GetAvailableVoicesResponse availableVoices;
    private HashSet<string> usedVoices = new HashSet<string>();
    private Dictionary<string, GameObject> characterMapper = new Dictionary<string, GameObject>();

    #region UI
    #region Input Prompt UIs
    [SerializeField]
    GameObject inputPanel;

    [SerializeField]
    TMP_InputField promptInputField;

    [SerializeField]
    TMP_Text loadingTextInfo;

    [SerializeField]
    Slider slider;
    private int numberOfCharacters;

    private bool enhancePrompt = true;
    #endregion

    [SerializeField]
    TMP_Text spokeText;

    [SerializeField]
    GameObject scrollView;

    #endregion

    #region Prefabs
    [SerializeField]
    private GameObject characterPrefab;
    #endregion

    #region Camera Parameters
    private float cameraDistance = 10f;
    private Vector3 originalPos;
    private Quaternion originalOrientation;
    #endregion

    async void Start()
    {
        availableVoices = await apiManager.GetAvailableVoices();

        originalPos = Camera.main.transform.position;
        originalOrientation = Camera.main.transform.rotation;
        numberOfCharacters = (int)slider.value;
    }

    private async Task<string> EnhanceStoryPrompt(string prompt)
    {
        if (enhancePrompt)
        {
            var enhancePromptResponse = await apiManager.EnhanceDescription(prompt);
            return enhancePromptResponse.enhanced_description;
        }

        return prompt;
    }

    private async Task<List<Character>> CreateUniqueCharacters(string storyPrompt)
    {
        var characters = new List<Character>();
        var charactersSet = new HashSet<string>();
        var characterRoleMapper = new HashSet<string>();
        for (int i = 0; i < numberOfCharacters; i++)
        {
            var character = (await apiManager.CreateCharacter(storyPrompt, charactersSet, characterRoleMapper)).character;
            characters.Add(character);
            charactersSet.Add(characters[i].name);
            characterRoleMapper.Add($"({characters[i].name}, {characters[i].role})");
            Debug.Log($"Name: {characters[i].name}\nGender: {characters[i].gender}\nPersonality: {characters[i].personality}\nDescription: {characters[i].description}");

            characterMapper.Add(character.name.ToLower(), InstantiateCharacterPrefab(character));
        }

        return characters;
    }

    private async Task<List<CharacterTalk>> GetCharacterConversations(List<Character> characters, string storyDescription)
    {
        var uniqueConvMapper = new Dictionary<string, CharacterTalk>();
        var conversationLength = Random.Range(2, Constants.MaxConvLength);

        Debug.Log($"Generating {conversationLength} conversations.");
        for (int i = 0; i < conversationLength; i++)
        {
            var randCharacterIdx = Random.Range(0, characters.Count);
            var talkingCharacter = characters[randCharacterIdx];
            var availableActions = GetAvailableActions(characters, talkingCharacter.name);

            var newResponse = await apiManager.CreateCharacterTalk("Create a response that matches the characters personality and story.", storyDescription, talkingCharacter.name, talkingCharacter.personality, uniqueConvMapper.Keys.ToHashSet(), availableActions);
            var newRes = $"{newResponse.character_response.character}: {newResponse.character_response.response}";
            Debug.Log(newRes + $"\nAction: {newResponse.character_response.action}");

            uniqueConvMapper.TryAdd(newRes, newResponse.character_response);
        }

        return uniqueConvMapper.Values.ToList();
    }

    public IEnumerator RunConversation(List<CharacterTalk> lines)
    {
        foreach (var entry in lines)
        {
            characterMapper.TryGetValue(entry.character.ToLower(), out var sourceGameObj);
            if (sourceGameObj == null)
            {
                Debug.LogWarning("AI generated invalid character in CharacterTalk.");
                continue;
            }

            var sourceController = sourceGameObj.GetComponent<AiCharacterController>();

            if (!string.IsNullOrEmpty(entry.action))
            {
                var pattern = @"\bmove(?:s)?\s*to[:\-\s]*['""]?(?<target>.+?)['""]?(?=$|\s*[.!?])";
                var match = Regex.Match(entry.action, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string rawTarget = match.Groups["target"].Value.Trim();
                    Debug.Log($"Target retrieved: {rawTarget}");
                    if (characterMapper.TryGetValue(rawTarget.ToLower(), out GameObject targetGameObject))
                    {
                        // Use a closure/lambda so the onArrive callback has the entry in scope
                        bool arrived = false;
                        sourceController.SetTarget(targetGameObject.transform.position, () => { arrived = true; });

                        // Wait until arrival (uses aiController.IsMoving)
                        yield return new WaitUntil(() => arrived);
                    }
                    else
                    {
                        Debug.LogWarning($"No target found for action '{entry.action}'. Skipping move.");
                    }
                }
            }

            // After the action (or immediately if no action) speak the response
            if (!string.IsNullOrEmpty(entry.response))
            {
                // Move camera to be "distance" units away from the target, in the chosen direction
                Camera.main.transform.position = sourceGameObj.transform.position + Vector3.back * cameraDistance;
                Camera.main.transform.LookAt(sourceGameObj.transform);

                scrollView.SetActive(true);
                spokeText.text = $"{entry.character}: {entry.response}";
                yield return StartCoroutine(sourceController.PlaySpeech(entry.response, apiManager));
            }

            // optionally add a small pause between lines
            yield return new WaitForSeconds(0.25f);
        }

        Debug.Log("Conversation finished.");
        inputPanel.SetActive(true);
    }

    async void GenerateStory(string prompt)
    {
        try
        {
            inputPanel.SetActive(false); // Disable UI, since we are now playing the story.
            loadingTextInfo.text = "Enhancing Prompt if requested...";
            var storyPrompt = await EnhanceStoryPrompt(prompt);

            loadingTextInfo.text = $"Creating {numberOfCharacters} unique characters...";
            var characters = await CreateUniqueCharacters(storyPrompt);

            loadingTextInfo.text = "Creating character conversations...";
            var conversationHistory = await GetCharacterConversations(characters, storyPrompt);
            loadingTextInfo.text = string.Empty;

            StartCoroutine(RunConversation(conversationHistory));
        }
        catch (Exception e)
        {
            loadingTextInfo.text = "Generation failed, please try again!: " + e.Message;
            inputPanel.SetActive(true);
        }
    }

    #region Events
    public void OnEnterForPromptInputField()
    {
        Debug.Log($"Received input: {promptInputField.text}");
        if (string.IsNullOrEmpty(promptInputField.text))
        {
            Debug.LogWarning("No story prompt was provided. Please provide a prompt.");
            return;
        }

        ResetSceneComponents();

        GenerateStory(promptInputField.text);
    }

    public void OnEnhancePromptToggleChange()
    {
        enhancePrompt = !enhancePrompt;
        Debug.Log($"Enhancing prompt: {enhancePrompt}");
    }

    public void OnNumCharacterSliderChange()
    {
        numberOfCharacters = (int)slider.value;
        Debug.Log($"User picked {numberOfCharacters} characters to generate.");
    }
    #endregion

    #region Helper
    /// <summary>
    /// This method is used to clear out all collections and reset UI components. We'll only do this on receiving input, so we can still review the story if we wanted to.
    /// </summary>
    private void ResetSceneComponents()
    {
        foreach (var charObj in characterMapper.Values)
        {
            Destroy(charObj);
        }

        usedVoices.Clear();
        characterMapper.Clear();

        scrollView.SetActive(false);
        Camera.main.transform.position = originalPos;
        Camera.main.transform.rotation = originalOrientation;
    }


    private string GetUniqueVoice(List<string> voices)
    {
        foreach (var voice in voices)
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

    private List<string> GetAvailableActions(List<Character> characters, string excludeCharacter)
    {
        var availableActions = new List<string>();
        for (int j = 0; j < characters.Count; j++)
        {
            if (characters[j].name.ToLower() != excludeCharacter.ToLower())
            {
                availableActions.Add(Constants.MoveToAction + characters[j].name);
            }
        }

        return availableActions;
    }

    private GameObject InstantiateCharacterPrefab(Character charInfo)
    {
        var instantiatedGameObj = Instantiate(characterPrefab, new Vector3(Random.Range(0, 50), Random.Range(0, 50)), Quaternion.identity);
        var characterVoice = GetUniqueVoice(charInfo.gender == "male" ? availableVoices.male_voices : availableVoices.female_voices);

        return CharacterFactory.SetUpPrimitiveCharacter(instantiatedGameObj, charInfo, characterVoice);
    }
    #endregion
}
