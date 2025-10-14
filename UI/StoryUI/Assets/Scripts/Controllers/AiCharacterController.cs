using System.Collections;
using UnityEngine;
using System;
using TMPro;

public class AiCharacterController : MonoBehaviour
{
    #region Movement Fields
    private const float speed = 3f;
    private const float rotationSpeed = 10f;
    private const float arriveDistance = 5f;

    private event Action OnArrived;
    private bool hasTarget = false;
    private Vector3 targetPosition;
    private float cameraDistance = 10f;
    #endregion

    #region Name Tag Fields
    Camera mainCamera;

    public GameObject nameTagInstance;
    public TMP_Text nameText;
    public bool onlyRotateY = true; // whether to lock X/Z tilt
    #endregion

    private Character characterInfo;
    public Character CharacterInfo { 
        get {
            return characterInfo;
        } 
        set {
            characterInfo = value;
            if (characterInfo != null)
            {
                nameText.text = characterInfo.name;
            }
        } 
    }
    public string CharacterVoice { get; set; }

    [SerializeField]
    private AudioSource audioSource;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        MoveToTarget();
    }

    void LateUpdate()
    {
        MoveAndRotateNameTag();
    }

    private void MoveAndRotateNameTag()
    {
        // Move x and z, but keep y since we want the name tag to be above the character object.
        Vector3 worldPos = new Vector3(transform.position.x, nameTagInstance.transform.position.y, transform.position.z);
        nameTagInstance.transform.position = worldPos;

        if (onlyRotateY)
        {
            Vector3 dir = nameTagInstance.transform.position - mainCamera.transform.position;
            dir.y = 0;  // ignore vertical component so it doesn't tilt
            if (dir.sqrMagnitude > 0.001f)
            {
                nameTagInstance.transform.rotation = Quaternion.LookRotation(dir);
            }
        }
        else
        {
            // full billboard (including tilt)
            nameTagInstance.transform.LookAt(mainCamera.transform);
            // if text is backwards, you might rotate 180°:
            nameTagInstance.transform.Rotate(0, 180f, 0);
        }
    }

    private void MoveToTarget()
    {
        if (!hasTarget)
        {
            return;
        }

        // Move
        Vector3 pos = transform.position;
        Vector3 next = Vector3.MoveTowards(pos, targetPosition, speed * Time.deltaTime);
        transform.position = next;


        // Move camera to be "distance" units away from the target, in the chosen direction
        Camera.main.transform.position = gameObject.transform.position + Vector3.back * cameraDistance;
        Camera.main.transform.LookAt(gameObject.transform);

        // Rotate to face direction of movement.
        Vector3 dir = (targetPosition - pos).normalized;
        dir.y = 0; // Don't zip through floor, always rotate horizontally.

        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);

            transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationSpeed * Time.deltaTime);
        }

        // Check to see if we have arrived.
        if (Vector3.Distance(transform.position, targetPosition) <= arriveDistance)
        {
            Debug.Log("Reached target!");
            hasTarget = false;
            OnArrived?.Invoke();
        }
    }

    public void SetTarget(Vector3 pos, Action onArriveParam)
    {
        Debug.Log("Setted target!");
        targetPosition = pos;
        hasTarget = true;
        OnArrived = onArriveParam;
    }

    // Example PlaySpeech coroutine (assumes you have an AudioClip or TTS that produces one)
    // Replace the TTS/clip-fetching with your TTS implementation.
    public IEnumerator PlaySpeech()
    {
        if (audioSource.clip == null)
        {
            Debug.Log("Audio clip is null...");
            yield break;
        }

        audioSource.Stop();
        audioSource.Play();

        // Wait until finished
        yield return new WaitWhile(() => audioSource.isPlaying);
    }

    // Overload where you have text-based TTS — this stub shows pattern:
    public IEnumerator PlaySpeech(string text, ApiManager apiManager)
    {
        yield return StartCoroutine(apiManager.SetTTS(text, CharacterVoice, audioSource));
        yield return PlaySpeech();
    }
}
