using System.Collections.Generic;
using UnityEngine;

public class TridentProjectile : MonoBehaviour
{
    [Header("Player Reference")]
    public GameObject player;

    [Header("Animation")]
    public List<Sprite> animationFrames;
    public float animationFPS = 12f;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float rotationSpeed = 720f; // degrees per second

    [Header("Follow Target")]
    public Transform followTarget;          
    public Vector2 positionOffset = Vector2.zero;

    private SpriteRenderer sr;
    private int animIndex = 0;
    private float animTimer = 0f;
    private bool animationComplete = false;

    private Rigidbody2D rb;
    private Collider2D thisCollider;

    private bool rotatingToPlayer = false;
    private Quaternion targetRotation;
    private bool movingToPlayer = false;
    private bool lodgedInWall = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        thisCollider = GetComponent<Collider2D>();
        if (thisCollider == null) thisCollider = gameObject.AddComponent<BoxCollider2D>();
        thisCollider.enabled = false; // Start disabled
        thisCollider.isTrigger = true;
    }

    void Start()
    {
        if (animationFrames.Count > 0)
            sr.sprite = animationFrames[0];
    }

    void Update()
    {
        HandleAnimation();

        if (!animationComplete)
        {
            AlignWithMerfolk(); // Only snap during animation
        }
        else
        {
            if (!rotatingToPlayer && !movingToPlayer)
            {
                PrepareRotationToPlayer();
            }

            if (rotatingToPlayer)
            {
                RotateTowardsPlayer();
            }
            else if (movingToPlayer)
            {
                MoveForward();
            }
        }
    }

    void HandleAnimation()
    {
        if (animationComplete || animationFrames.Count == 0) return;

        animTimer += Time.deltaTime;
        if (animTimer >= 1f / animationFPS)
        {
            animTimer = 0f;
            animIndex++;

            if (animIndex >= animationFrames.Count)
            {
                animIndex = animationFrames.Count - 1;
                animationComplete = true;
                thisCollider.enabled = true; // Enable collider after animation
            }

            sr.sprite = animationFrames[animIndex];
        }
    }

    void AlignWithMerfolk()
    {
        if (followTarget == null) return;

        Vector3 offset = positionOffset;

        // Flip X offset if merfolk is flipped
        SpriteRenderer targetSR = followTarget.GetComponent<SpriteRenderer>();
        if (targetSR != null && targetSR.flipX)
        {
            offset.x = -offset.x;
            sr.flipX = true;
        }
        else if (targetSR != null)
        {
            sr.flipX = false;
        }

        Vector3 targetPos = followTarget.position + (Vector3)offset;
        targetPos.z = transform.position.z;
        transform.position = targetPos;

        // Ignore collisions with merfolk
        Collider2D targetCol = followTarget.GetComponent<Collider2D>();
        if (targetCol != null && thisCollider != null)
        {
            Physics2D.IgnoreCollision(thisCollider, targetCol, true);
        }
    }

    void PrepareRotationToPlayer()
    {
        if (player == null) return;

        Vector2 dir = (Vector2)(player.transform.position - transform.position);
        if (sr.flipX) dir = -dir;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        targetRotation = Quaternion.Euler(0f, 0f, angle);

        rotatingToPlayer = true;
    }

    void RotateTowardsPlayer()
    {
        if (player == null) return;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // When rotation is close enough, start moving
        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            transform.rotation = targetRotation;
            rotatingToPlayer = false;
            movingToPlayer = true;
        }
    }

    void MoveForward()
    {
        if (lodgedInWall || player == null) return;
        rb.linearVelocity = transform.right * moveSpeed * (sr.flipX ? -1f : 1f);
    }

    public void ForceStartAnimation()
    {
        animIndex = 0;
        animTimer = 0f;
        animationComplete = false;
        rotatingToPlayer = false;
        movingToPlayer = false;
        thisCollider.enabled = false;
        if (animationFrames.Count > 0)
            sr.sprite = animationFrames[0];
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (lodgedInWall) return;
        if (collision.gameObject.CompareTag("intangible")) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") ||
            collision.gameObject == player ||
            (collision.TryGetComponent<FishController>(out FishController fish) && fish.isControlled))
        {
            rb.linearVelocity = Vector2.zero;
            transform.position += transform.right * moveSpeed * Time.deltaTime * (sr.flipX ? -1f : 1f);
            lodgedInWall = true;
        }
    }
}
