using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;
using System.Threading.Tasks;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine.UI;

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

    [SerializeField]
    Slider slider;
    private int numberOfCharacters;
    #endregion

    [SerializeField]
    TMP_Text spokeText;

    [SerializeField]
    GameObject scrollView;

    #region Prefabs
    [SerializeField]
    private GameObject characterPrefab;
    #endregion

    #region Camera Parameters
    public float distance = 5000f; // Distance from the object
    public Vector3 offsetDirection = Vector3.back; // Direction relative to the target

    private Vector3 originalPos;
    private Quaternion originalOrientation;
    #endregion

    async void Start()
    {
        availableVoices = await apiManager.GetAvailableVoices();

        originalPos = Camera.main.transform.position;
        originalOrientation = Camera.main.transform.rotation;
        numberOfCharacters = (int)slider.value;

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

    async Task<List<CharacterTalk>> GetCharacterConversations(List<Character> characters, string storyDescription)
    {
        var uniqueConversationHistory = new HashSet<string>();

        var history = new List<CharacterTalk>();
        var conversationLength = UnityEngine.Random.Range(2, Constants.MaxConvLength);
        Debug.Log($"Generating {conversationLength} conversations.");
        for (int i = 0; i < conversationLength; i++)
        {
            var randCharacterIdx = UnityEngine.Random.Range(0, characters.Count);
            var talkingCharacter = characters[randCharacterIdx];

            var availableActions = new List<string>();
            for (int j = 0;j < characters.Count; j++)
            {
                if (characters[j].name.ToLower() != talkingCharacter.name.ToLower())
                {
                    availableActions.Add(Constants.MoveToAction + characters[j].name);
                }
            }

            var newResponse = await apiManager.CreateCharacterTalk("Create a response that matches the characters personality and story.", storyDescription, talkingCharacter.name, talkingCharacter.personality, uniqueConversationHistory, availableActions);
            var newRes = $"{newResponse.character_response.character}: {newResponse.character_response.response}";
            Debug.Log(newRes + $"\nAction: {newResponse.character_response.action}");

            uniqueConversationHistory.Add(newRes);
            history.Add(newResponse.character_response);
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
        var instantiatedGameObj = Instantiate(characterPrefab, new Vector3(UnityEngine.Random.Range(0, 50), 10), Quaternion.identity);

        var movementScript = instantiatedGameObj.GetComponent<AiCharacterController>();
        movementScript.CharacterInfo = charInfo;
        movementScript.CharacterVoice = GetUniqueVoice(charInfo.gender == "male" ? availableVoices.male_voices : availableVoices.female_voices);

        // Get the Renderer component
        Renderer renderer = instantiatedGameObj.GetComponent<Renderer>();
        var randR = UnityEngine.Random.Range(0.0f, 1.0f);
        var randG = UnityEngine.Random.Range(0.0f, 1.0f);
        var randB = UnityEngine.Random.Range(0.0f, 1.0f);
        renderer.material.color = new Color(randR, randG, randB);

        return instantiatedGameObj;
    }

    public IEnumerator RunConversation(List<CharacterTalk> lines)
    {
        Debug.Log($"Conversation length: {lines.Count}");
        foreach (var entry in lines)
        {
            characterMapper.TryGetValue(entry.character.ToLower(), out var sourceGameObj);
            if (sourceGameObj == null)
            {
                Debug.LogWarning("AI generated invalid character in CharacterTalk.");
                continue;
            }

            var sourceController = sourceGameObj.GetComponent<AiCharacterController>();

            // Example: interpret action "move to <characterName>"
            if (!string.IsNullOrEmpty(entry.action) && Regex.IsMatch(entry.action, @"^\s*move(s)?\s*to\b", RegexOptions.IgnoreCase))
            {
                Debug.Log($"Found action! {entry.action}");
                // parse target name
                // expected form: "move to Bob" or "move to: Bob" etc. Adjust parsing as needed.
                string[] parts = entry.action.Split(' ');
                string targetName = parts.Length >= 3 ? parts[2] : null;
                if (targetName != null && characterMapper.TryGetValue(targetName.ToLower(), out GameObject targetGameObject))
                {
                    Debug.Log($"Found character, moving towards them!: {targetName}");
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

            // After the action (or immediately if no action) speak the response
            if (!string.IsNullOrEmpty(entry.response))
            {
                if (!scrollView.activeInHierarchy)
                {
                    scrollView.SetActive(true);
                }

                // Move camera to be "distance" units away from the target, in the chosen direction
                Camera.main.transform.position = sourceGameObj.transform.position + offsetDirection * distance;

                // Make the camera look at the target
                Camera.main.transform.LookAt(sourceGameObj.transform);

                // If you have pre-generated clip lookup, use that. Here we call the AiCharacterController's PlaySpeech coroutine.
                spokeText.text = $"{entry.character}: {entry.response}";
                yield return StartCoroutine(sourceController.PlaySpeech(entry.response, apiManager));
            }

            // optionally add a small pause between lines
            yield return new WaitForSeconds(0.25f);
        }

        Debug.Log("Conversation finished.");
        inputPanel.SetActive(true);
        Camera.main.transform.position = originalPos;
        Camera.main.transform.rotation = originalOrientation;
    }

    async void GenerateStory(string prompt)
    {
        try
        {
            loadingTextInfo.text = "Enhancing Prompt if requested...";
            var storyPrompt = await EnhanceStoryPrompt(prompt);

            loadingTextInfo.text = $"Creating {numberOfCharacters} unique characters...";
            var characters = await CreateUniqueCharacters(storyPrompt);

            loadingTextInfo.text = "Creating character conversations...";
            var conversationHistory = await GetCharacterConversations(characters, storyPrompt);
            loadingTextInfo.gameObject.SetActive(false);

            StartCoroutine(RunConversation(conversationHistory));
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
        Debug.Log("Clearing existing stuff...");
        scrollView.SetActive(false);
        usedVoices.Clear();

        foreach (var charObj in characterMapper.Values)
        {
            Destroy(charObj);
        }

        characterMapper.Clear();

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

    public void OnNumCharacterSliderChange()
    {
        numberOfCharacters = (int)slider.value;
        Debug.Log($"User picked {numberOfCharacters} characters to generate.");
    }
}
