using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    public bool isSwimming = true;
    public GameObject control;
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
    public float turnSpeed = 6f;

    [Header("Camera Settings")]
    public Camera cam;

    private Rigidbody2D rb;
    private Vector3 spawnPoint;
    private Vector2 targetVelocity;
    private float currentAngle;
    private bool facingRight;
    private float currentTilt;
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

        if (isSwimming)
        {
            if (distance <= stopRadius)
            {
                targetVelocity = Vector2.zero;
            }
            else
            {
                float speedFactor = (distance / (slowRadius - stopRadius)) - (stopRadius / (slowRadius - stopRadius));

                float speed = speedFactor * moveSpeed;

                targetVelocity = toMouse.normalized * speed;
                targetVelocity = Vector2.ClampMagnitude(targetVelocity, maxSpeed);
            }
        }
        else
        {
            targetVelocity = Vector2.zero;
            rb.position = control.GetComponent<Rigidbody2D>().position;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (isSwimming)
            {
                control = CheckPossess();
                if (control != null)
                {
                    control.GetComponent<FishController>().isControlled = true;
                    isSwimming = false;
                    sprite.GetComponent<SpriteRenderer>().enabled = false;
                    //audioSource.PlayOneShot(possessSound);
                }
            }
            else
            {
                GameObject tempControl = CheckPossess();

                isSwimming = true;
                sprite.GetComponent<SpriteRenderer>().enabled = true;
                //audioSource.PlayOneShot(depossessSound);
                control.GetComponent<FishController>().isControlled = false;

                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, moveDrag * Time.fixedDeltaTime);

        if (targetVelocity.magnitude < 0.05f)
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, stopDrag * Time.fixedDeltaTime);

        Vector2 vel = rb.linearVelocity;

        if (vel.sqrMagnitude > 0.01f)
        {
            vel.Normalize();

            bool facingRight_ = vel.x >= 0f;
            if (facingRight_ != facingRight)
            {
                currentTilt *= -1f;
                facingRight = facingRight_;
            }

            sprite.transform.localScale = new Vector3(facingRight ? 1f : -1f, 1f, 1f);

            float targetTilt = Mathf.Clamp(vel.y * rotationMultiplier * 80f, -80f, 80f) * (facingRight ? 1f : -1f);

            currentTilt = Mathf.Lerp(currentTilt, targetTilt, turnSpeed * Time.fixedDeltaTime);
        }
        else
        {
            currentTilt = Mathf.Lerp(currentTilt, 0f, turnSpeed * Time.fixedDeltaTime);
        }

        sprite.transform.rotation = Quaternion.Euler(0f, 0f, currentTilt);
    }

    private void OnTriggerStay2D(Collider2D other)
    {

    }

    private GameObject CheckPossess()
    {
        GameObject controlN = null;

        // Get all colliders overlapping the player's BoxCollider2D
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        Collider2D[] results = Physics2D.OverlapBoxAll(
            transform.position,
            box.size,
            0f
        );

        foreach (Collider2D collider in results)
        {
            if (collider.gameObject.tag == "possess")
            {
                if (controlN == null) controlN = collider.gameObject;
                else
                {
                    Vector3 distanceC = controlN.transform.position - transform.position;
                    Vector3 distanceN = collider.gameObject.transform.position - transform.position;
                    if (distanceN.magnitude < distanceC.magnitude)
                    {
                        controlN = collider.gameObject;
                    }
                }
            }
        }

        return controlN;
    }
}
