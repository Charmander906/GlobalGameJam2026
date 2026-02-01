using System.Collections.Generic;
using UnityEngine;

public class TridentProjectile : MonoBehaviour
{
    [Header("Player Reference")]
    public GameObject player;

    [Header("Animation")]
    public List<Sprite> animationFrames;
    public float animationFPS = 12f;

    [Header("Rotation Settings")]
    public float initialSpinSpeed = 1080f;
    public float rotationEaseTime = 0.5f;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Collider2D thisCollider;

    private float animTimer;
    private int animIndex;
    private bool animationComplete = false;
    private bool animationStarted = false;

    private bool rotatingToPlayer = false;
    private float currentRotationSpeed;
    private Quaternion targetRotation;

    private bool movingTowardsPlayer = false;
    private bool lodgedInWall = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        sr = GetComponent<SpriteRenderer>();
        thisCollider = GetComponent<Collider2D>();

        InitializeIntangibility();

        currentRotationSpeed = initialSpinSpeed;

        if (animationFrames.Count > 0)
        {
            sr.sprite = animationFrames[0];
            animationStarted = true;
        }
    }

    void Update()
    {
        HandleAnimation();

        if (animationComplete && !rotatingToPlayer && !movingTowardsPlayer)
        {
            rotatingToPlayer = true;
            SetTargetRotation();
        }

        if (rotatingToPlayer)
            RotateTowardsPlayer();

        if (movingTowardsPlayer)
            MoveTowardsPlayer();
    }

    void HandleAnimation()
    {
        if (!animationStarted || animationComplete || animationFrames.Count == 0) return;

        animTimer += Time.deltaTime;
        if (animTimer >= 1f / animationFPS)
        {
            animTimer = 0f;
            animIndex++;
            if (animIndex >= animationFrames.Count)
            {
                animIndex = animationFrames.Count - 1;
                animationComplete = true;
            }

            sr.sprite = animationFrames[animIndex];
        }
    }

    public void ForceStartAnimation()
    {
        if (animationFrames.Count == 0) return;

        animIndex = 0;
        animTimer = 0f;
        animationStarted = true;
        animationComplete = false;
        sr.sprite = animationFrames[0];
    }

    void InitializeIntangibility()
    {
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("intangible"))
        {
            if (obj == gameObject) continue;
            Collider2D otherCol = obj.GetComponent<Collider2D>();
            if (otherCol != null && thisCollider != null)
                Physics2D.IgnoreCollision(thisCollider, otherCol);
        }

        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("possess"))
        {
            Collider2D otherCol = obj.GetComponent<Collider2D>();
            if (otherCol != null && thisCollider != null)
                Physics2D.IgnoreCollision(thisCollider, otherCol);
        }

        if (player != null)
        {
            Collider2D playerCol = player.GetComponent<Collider2D>();
            if (playerCol != null && thisCollider != null)
                Physics2D.IgnoreCollision(thisCollider, playerCol);
        }
    }

    void SetTargetRotation()
    {
        if (player == null) return;
        Vector2 dirToPlayer = player.transform.position - transform.position;
        float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
        targetRotation = Quaternion.Euler(0f, 0f, angle);
    }

    void RotateTowardsPlayer()
    {
        if (player == null)
        {
            rotatingToPlayer = false;
            movingTowardsPlayer = true;
            rb.linearVelocity = transform.right * moveSpeed;
            return;
        }

        float angleDiff = Quaternion.Angle(transform.rotation, targetRotation);
        float rotationStep = currentRotationSpeed * Time.deltaTime;

        if (rotationStep >= angleDiff || angleDiff < 0.1f)
        {
            transform.rotation = targetRotation;
            rotatingToPlayer = false;
            movingTowardsPlayer = true;
            rb.linearVelocity = (Vector2)transform.right * moveSpeed;
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationStep);
            currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, 0f, Time.deltaTime / rotationEaseTime);
        }
    }

    void MoveTowardsPlayer()
    {
        if (lodgedInWall) return;
        rb.linearVelocity = (Vector2)transform.right * moveSpeed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (lodgedInWall) return;

        if (collision.gameObject.CompareTag("intangible")) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            rb.linearVelocity = Vector2.zero;
            lodgedInWall = true;
            return;
        }

        if (collision.gameObject == player ||
            (collision.gameObject.TryGetComponent<FishController>(out var fish) && fish.isControlled))
        {
            lodgedInWall = true;
            rb.linearVelocity = Vector2.zero;
        }
    }
}
