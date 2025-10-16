using System.Collections;
using UnityEngine;
using System;
using TMPro;
using System.Linq;

public class AiCharacterController : MonoBehaviour
{
    #region Movement Fields
    private const float speed = 3f;
    private const float rotationSpeed = 10f;
    private const float arriveDistance = 5f;

    private event Action OnArrived;
    private bool hasTarget = false;
    private Vector3 targetPosition;
    private float cameraDistance = 5f;
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

    #region Animation & Audio
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private Animator characterAnimator;
    #endregion

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
            return;

        RunAnimation("IsWalking");

        Vector3 pos = transform.position;
        Vector3 dir = (targetPosition - pos).normalized;
        dir.y = 0;

        LookAtCharacter();

        // --- obstacle detection ---
        float rayDist = 1.0f; // how far ahead to check
        bool blocked = Physics.Raycast(pos + Vector3.up * 0.5f, dir, rayDist);

        if (blocked)
        {
            // Try sidestepping
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            bool rightClear = !Physics.Raycast(pos + Vector3.up * 0.5f, right, 0.6f);
            bool leftClear = !Physics.Raycast(pos + Vector3.up * 0.5f, -right, 0.6f);

            if (rightClear)
                dir = (dir + right * 0.7f).normalized;
            else if (leftClear)
                dir = (dir - right * 0.7f).normalized;
            else
                dir = -dir; // completely blocked, back up a bit
        }

        // Move and rotate
        Vector3 next = pos + dir * (speed * Time.deltaTime);
        transform.position = next;

        Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationSpeed * Time.deltaTime);

        // Arrival check
        if (Vector3.Distance(transform.position, targetPosition) <= arriveDistance)
        {
            RunAnimation(UnityEngine.Random.value > 0.5f ? "IsTalking" : "IsDancing");
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

    public void LookAtCharacter()
    {
        var x = gameObject.transform.position.x;
        var y = gameObject.transform.position.y + 1.5f; // Slight vertical offset so we can see the character and ground.
        var z = gameObject.transform.position.z;
        // Move camera to be "distance" units away from the target, in the chosen direction
        Camera.main.transform.position = new Vector3(x, y, z) + Vector3.forward * cameraDistance;
        Camera.main.transform.LookAt(gameObject.transform);
    }

    private void RunAnimation(string animation)
    {
        if(Enum.TryParse(typeof(Constants.AnimationTrigger), animation, out var trigger) && characterAnimator != null)
        {
            var triggeredAnimation = ((Constants.AnimationTrigger)trigger).ToString();
            var animations = Enum.GetValues(typeof(Constants.AnimationTrigger));
            foreach( var anim in animations)
            {
                bool activateAnimation = false;

                if(anim.ToString().Equals(triggeredAnimation, StringComparison.OrdinalIgnoreCase))
                {
                    activateAnimation = true;
                }

                characterAnimator.SetBool(anim.ToString(), activateAnimation);
            }
        }
    }
}
