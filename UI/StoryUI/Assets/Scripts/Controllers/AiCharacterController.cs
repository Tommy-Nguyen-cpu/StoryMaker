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
    private const float arriveDistance = 3f;

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

    #region Obstacle Avoidance
    private Vector3 lastMoveCheckPos;
    private float lastMoveCheckTime;
    [SerializeField] private float stuckCheckInterval = 0.5f; // how often to check movement
    [SerializeField] private float stuckThreshold = 0.05f;    // considered moved if > this
    [SerializeField] private float stuckTimeout = 2.0f;       // time without effective movement -> stuck
    [SerializeField] private float obstacleDetectDistance = 1.0f; // how far ahead to look for obstacles
    [SerializeField] private float obstacleSphereRadius = 0.3f; // for spherecast
    [SerializeField] private float sidestepDistance = 0.8f;   // how far to try sidestepping
    private bool tryingToSidestep = false;
    private float sidestepTryTime = 0f;
    private float sidestepMaxTry = 1.0f; // seconds to keep sidestepping before giving up
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
        if (!hasTarget) return;

        RunAnimation("IsWalking");

        Vector3 pos = transform.position;
        Vector3 toTarget = targetPosition - pos;
        Vector3 dir = toTarget;
        dir.y = 0;
        float distToTarget = dir.magnitude;

        // arrival check (quick)
        if (distToTarget <= arriveDistance)
        {
            OnArriveReached();
            return;
        }

        Vector3 forwardDir = dir.normalized;

        // obstacle detection: spherecast forward from slightly above ground
        RaycastHit hit;
        Vector3 sphereOrigin = pos + Vector3.up * 0.5f;
        bool obstacleAhead = Physics.SphereCast(sphereOrigin, obstacleSphereRadius, forwardDir, out hit, obstacleDetectDistance);

        // we are stuck logic: check position change periodically
        if (Time.time - lastMoveCheckTime >= stuckCheckInterval)
        {
            float moved = Vector3.Distance(transform.position, lastMoveCheckPos);
            if (moved <= stuckThreshold)
            {
                // no meaningful movement detected since last check
                // if we've been stuck for too long, treat as stuck
                if (Time.time - lastMoveCheckTime >= stuckTimeout)
                {
                    tryingToSidestep = true;
                    sidestepTryTime = Time.time;
                }
            }
            else
            {
                // made progress -> reset stuck monitor
                tryingToSidestep = false;
                lastMoveCheckPos = transform.position;
                lastMoveCheckTime = Time.time;
            }
            lastMoveCheckPos = transform.position;
            lastMoveCheckTime = Time.time;
        }

        // Default movement target vector
        Vector3 desiredMove = forwardDir * (speed * Time.deltaTime);

        // If obstacle ahead or we think we're stuck, attempt sidestep
        if (obstacleAhead || tryingToSidestep)
        {
            // compute perpendiculars
            Vector3 right = Vector3.Cross(Vector3.up, forwardDir).normalized; // right direction
            Vector3 left = -right;

            // try right then left. We cast from candidate sidestep start to target to see if path looks clear
            Vector3 rightCheckPoint = pos + right * sidestepDistance;
            Vector3 leftCheckPoint = pos + left * sidestepDistance;

            bool canRight = !Physics.SphereCast(rightCheckPoint + Vector3.up * 0.5f, obstacleSphereRadius, forwardDir, out _, obstacleDetectDistance);
            bool canLeft = !Physics.SphereCast(leftCheckPoint + Vector3.up * 0.5f, obstacleSphereRadius, forwardDir, out _, obstacleDetectDistance);

            if (canRight)
            {
                desiredMove = (right + forwardDir * 0.2f).normalized * (speed * Time.deltaTime); // nudge right-forward
            }
            else if (canLeft)
            {
                desiredMove = (left + forwardDir * 0.2f).normalized * (speed * Time.deltaTime); // nudge left-forward
            }
            else
            {
                // both sides blocked: try backing up slightly, or rotate to find a gap
                desiredMove = -forwardDir * (speed * 0.3f * Time.deltaTime); // small back up
                                                                             // if sidestepping for too long, give up and invoke arrival to avoid permanent loop
                if (tryingToSidestep && Time.time - sidestepTryTime > sidestepMaxTry)
                {
                    Debug.LogWarning("Stuck trying to reach target — aborting and firing OnArrived fallback.");
                    OnArriveReached(); // either notify arrived or call an alternate callback
                    return;
                }
            }
        }

        // actually move (use transform.position or CharacterController.Move if using CharacterController)
        transform.position += desiredMove;

        // LookAtCharacter (keep this or replace with smoothing)
        LookAtCharacter();

        // rotate to face movement direction
        Vector3 moveDir = (transform.position - pos).normalized;
        moveDir.y = 0;
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationSpeed * Time.deltaTime);
        }

        // arrival check again after moving
        if (Vector3.Distance(transform.position, targetPosition) <= arriveDistance)
        {
            OnArriveReached();
        }
    }

    private void OnArriveReached()
    {
        RunAnimation(UnityEngine.Random.Range(0f, 1f) > 0.5f ? "IsTalking" : "IsDancing");
        Debug.Log("Reached target!");
        hasTarget = false;
        tryingToSidestep = false;
        OnArrived?.Invoke();
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
