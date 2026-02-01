using System.Collections.Generic;
using UnityEngine;

public class TridentProjectile : MonoBehaviour
{
    [Header("Player Reference")]
    public GameObject player;

    [Header("Animation")]
    public List<Sprite> animationFrames;
    public float animationFPS = 12f;

    [Header("Movement")]
    public float moveSpeed = 10f;

    [Header("Prediction")]
    public float maxPredictionTime = 2f;

    [Header("Homing")]
    public float homingDuration = 0.6f;
    public float maxTurnRate = 180f;

    [Header("Follow Target")]
    public Transform followTarget;
    public Vector2 positionOffsetRight = Vector2.zero;
    public Vector2 positionOffsetLeft = Vector2.zero;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Collider2D thisCollider;

    private int animIndex = 0;
    private float animTimer = 0f;
    private bool animationComplete = false;

    private bool moving = false;
    private bool lodged = false;

    private Vector2 velocity;
    private float homingTimer = 0f;

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
        thisCollider.enabled = false;
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
            AlignWithMerfolk();
        }
        else if (!moving)
        {
            Launch();
        }
    }

    void FixedUpdate()
    {
        if (!moving || lodged) return;

        if (homingTimer < homingDuration)
        {
            HomeTowardsPredictedPosition();
            homingTimer += Time.fixedDeltaTime;
        }

        rb.linearVelocity = velocity;

        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
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
                thisCollider.enabled = true;
            }

            sr.sprite = animationFrames[animIndex];
        }
    }

    void AlignWithMerfolk()
    {
        if (followTarget == null) return;

        SpriteRenderer targetSR = followTarget.GetComponent<SpriteRenderer>();
        if (targetSR != null)
            sr.flipX = targetSR.flipX;

        Vector3 offset = sr.flipX ? positionOffsetLeft : positionOffsetRight;

        Vector3 targetPos = followTarget.position + offset;
        targetPos.z = transform.position.z;
        transform.position = targetPos;
    }

    void Launch()
    {
        if (player == null) return;

        sr.flipX = false;

        Vector2 dir = GetPredictedDirection();
        velocity = dir * moveSpeed;

        moving = true;
        homingTimer = 0f;
    }

    void HomeTowardsPredictedPosition()
    {
        Vector2 desiredDir = GetPredictedDirection();
        Vector2 currentDir = velocity.normalized;

        float angleDiff = Vector2.SignedAngle(currentDir, desiredDir);
        float maxStep = maxTurnRate * Time.fixedDeltaTime;
        float clamped = Mathf.Clamp(angleDiff, -maxStep, maxStep);

        Vector2 newDir = Quaternion.Euler(0, 0, clamped) * currentDir;
        velocity = newDir.normalized * moveSpeed;
    }

    Vector2 GetPredictedDirection()
    {
        if (player == null) return transform.right;

        Vector2 shooterPos = transform.position;
        Vector2 targetPos = player.transform.position;
        Vector2 targetVel = Vector2.zero;

        PlayerController pc = player.GetComponent<PlayerController>();

        if (pc != null)
        {
            if (pc.isSwimming)
            {
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                    targetVel = playerRb.linearVelocity;
            }
            else if (pc.control != null)
            {
                Rigidbody2D controlRb = pc.control.GetComponent<Rigidbody2D>();
                if (controlRb != null)
                {
                    targetPos = controlRb.position;
                    targetVel = controlRb.linearVelocity;
                }
            }
        }

        Vector2 toTarget = targetPos - shooterPos;
        float t = Mathf.Clamp(toTarget.magnitude / moveSpeed, 0f, maxPredictionTime);
        Vector2 futurePos = targetPos + targetVel * t;

        return (futurePos - shooterPos).normalized;
    }

    void StickIntoTarget(Transform target)
    {
        lodged = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        thisCollider.enabled = false;

        transform.SetParent(target);

        Vector3 localPos = transform.localPosition;
        localPos.z = 0.01f;
        transform.localPosition = localPos;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (lodged) return;
        if (collision.CompareTag("intangible")) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            lodged = true;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (collision.gameObject == player)
        {
            StickIntoTarget(player.transform);
            return;
        }

        FishController fish = collision.GetComponent<FishController>();
        if (fish != null && fish.isControlled)
        {
            StickIntoTarget(fish.transform);
            return;
        }
    }

    public void ForceStartAnimation()
    {
        animIndex = 0;
        animTimer = 0f;
        animationComplete = false;
        moving = false;
        lodged = false;
        thisCollider.enabled = false;
        rb.simulated = true;
        transform.SetParent(null);

        if (animationFrames.Count > 0)
            sr.sprite = animationFrames[0];
    }
}
