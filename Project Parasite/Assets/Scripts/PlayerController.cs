using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    public bool isFloating = true;
    //public GameObject control;
    //public AudioClip hitSound;

    [Header("Sprite Settings")]
    public GameObject sprite;
    public float rotationMultiplier = 1.5f;

    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float maxSpeed = 7f;
    public float stopDrag = 5f;
    public float moveDrag = 8f;
    public float slowRadius = 2.5f;
    public float stopRadius = 0.5f;

    [Header("Camera Settings")]
    public Camera cam;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector3 spawnPoint;
    private Vector2 targetVelocity;
    private float currentAngle;
    public float turnSpeed = 6f;
    //private AudioSource audioSource;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        spawnPoint = transform.position;

        if (cam == null) cam = Camera.main;

        //audioSource = gameObject.AddComponent<AudioSource>();
        //audioSource.spatialBlend = 0f;
    }

    void Update()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        Vector2 toMouse = mouseWorldPos - transform.position;
        float distance = toMouse.magnitude;

        if (isFloating)
        {
            if (distance < stopRadius)
            {
                targetVelocity = Vector2.zero;
            }
            else
            {
                float speedFactor = 1f;

                if (distance < slowRadius)
                    speedFactor = distance / slowRadius;

                Vector2 desiredVelocity = toMouse.normalized * moveSpeed * speedFactor;
                targetVelocity = Vector2.ClampMagnitude(desiredVelocity, maxSpeed);
            }
        }
        else
        {
            targetVelocity = Vector2.zero;
        }
    }


    void FixedUpdate()
    {
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, moveDrag * Time.fixedDeltaTime);

        if (targetVelocity.magnitude < 0.05f)
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, stopDrag * Time.fixedDeltaTime);

        // Circular swimming-style rotation
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, turnSpeed * Time.fixedDeltaTime);
            sprite.transform.rotation = Quaternion.Euler(0, 0, currentAngle);
        }
    }


    private void OnTriggerStay2D(Collider2D other)
    {

    }
}
