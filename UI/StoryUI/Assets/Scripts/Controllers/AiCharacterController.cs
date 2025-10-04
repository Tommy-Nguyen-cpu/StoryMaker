using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class AiCharacterController : MonoBehaviour
{
    #region Movement Fields
    private const float speed = 3f;
    private const float rotationSpeed = 10f;
    private const float arriveDistance = 0.05f;

    private event Action OnArrived;
    private bool hasTarget = false;
    private Vector3 targetPosition;
    #endregion

    #region Name Tag Fields
    Camera mainCamera;

    public GameObject nameTagInstance;
    public TMP_Text nameText;
    public bool onlyRotateY = true; // whether to lock X/Z tilt
    #endregion

    public Character CharacterInfo { get; set; }

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
        RotateNameTag();
    }

    private void RotateNameTag()
    {
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
            hasTarget = false;
            OnArrived?.Invoke();
        }
    }

    public void SetTarget(Vector3 pos, Action onArriveParam)
    {
        targetPosition = pos;
        OnArrived = onArriveParam;
    }
}
