using System.Collections.Generic;
using UnityEngine;

public class MerfolkController : MonoBehaviour, IHasPlayer
{
    public GameObject player;
    GameObject IHasPlayer.player => player;

    [Header("Sprite Settings")]
    public Transform spriteObject;
    public float rotationMultiplier = 1.5f;

    private Transform spriteTransform;
    private SpriteRenderer sr;

    [Header("Animation")]
    public List<Sprite> idleFrames;
    public List<Sprite> attackFrames;
    public float animationFPS = 12f;
    private float animTimer;
    private int animIndex;
    private bool isAttacking = false;

    [Header("Swimming Movement")]
    public float swimAcceleration = 6f;
    public float maxSwimSpeed = 3f;
    public float turnResponsiveness = 2f;
    public float roamRadius = 4f;
    public float nodeReachDistance = 0.3f;
    public float idlePauseTime = 1.2f;

    [Header("Roaming Mode")]
    public bool useRoamNode = false;
    public Transform roamNode;

    [Header("Obstacle Avoidance")]
    public LayerMask groundLayer;
    public float obstacleCheckPadding = 0.2f;
    public int maxTargetAttempts = 10;

    [Header("Swim Motion Polish")]
    public float swayAmplitude = 0.5f;
    public float swayFrequency = 2f;

    private Rigidbody2D rb;
    private Vector2 swimTarget;
    private float idleTimer = 0f;
    private bool isIdling = false;
    private float swayTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 1.5f;

        spriteTransform = spriteObject;
        sr = spriteObject.GetComponent<SpriteRenderer>();

        PickNewSwimTarget();
        SetupCollisionIgnores();
    }

    void Update()
    {
        HandleAnimation();
        UpdateSpriteRotation();
    }

    void FixedUpdate()
    {
        HandleSwimmingMovement();
    }

    void HandleAnimation()
    {
        animTimer += Time.deltaTime;

        if (animTimer >= 1f / animationFPS)
        {
            animTimer = 0f;
            animIndex++;

            List<Sprite> currentAnim = isAttacking ? attackFrames : idleFrames;
            if (currentAnim.Count == 0) return;

            if (animIndex >= currentAnim.Count)
            {
                if (isAttacking) isAttacking = false;
                animIndex = 0;
            }

            sr.sprite = currentAnim[animIndex];
        }
    }

    public void TriggerAttack()
    {
        if (attackFrames.Count == 0) return;
        isAttacking = true;
        animIndex = 0;
        animTimer = 0f;
    }

    void HandleSwimmingMovement()
    {
        if (isIdling)
        {
            idleTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 2f * Time.fixedDeltaTime);

            if (idleTimer <= 0f)
            {
                isIdling = false;
                PickNewSwimTarget();
            }
            return;
        }

        Vector2 toTarget = swimTarget - rb.position;
        float dist = toTarget.magnitude;

        if (dist < nodeReachDistance)
        {
            isIdling = true;
            idleTimer = idlePauseTime;
            return;
        }

        Vector2 dir = toTarget.normalized;

        swayTimer += Time.fixedDeltaTime * swayFrequency;
        Vector2 perpendicular = new Vector2(-dir.y, dir.x);
        dir += perpendicular * Mathf.Sin(swayTimer) * swayAmplitude * 0.1f;
        dir.Normalize();

        Vector2 desiredVelocity = dir * maxSwimSpeed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desiredVelocity, swimAcceleration * Time.fixedDeltaTime);
    }

    void PickNewSwimTarget()
    {
        for (int i = 0; i < maxTargetAttempts; i++)
        {
            Vector2 basePos = useRoamNode && roamNode != null ? (Vector2)roamNode.position : rb.position;
            Vector2 candidate = basePos + Random.insideUnitCircle * roamRadius;

            if (!PathBlocked(rb.position, candidate))
            {
                swimTarget = candidate;
                return;
            }
        }

        swimTarget = rb.position;
    }

    bool PathBlocked(Vector2 from, Vector2 to)
    {
        Vector2 dir = (to - from);
        float dist = dir.magnitude;
        dir.Normalize();

        RaycastHit2D hit = Physics2D.Raycast(from, dir, dist + obstacleCheckPadding, groundLayer);
        return hit.collider != null;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        PickNewSwimTarget();
    }

    void UpdateSpriteRotation()
    {
        Vector2 vel = rb.linearVelocity;
        float speed = vel.magnitude;

        if (speed < 0.01f) return;

        bool facingLeft = vel.x < 0;
        sr.flipX = facingLeft;

        float tiltZ = speed * rotationMultiplier;

        if (!facingLeft) tiltZ *= -1f;

        tiltZ = Mathf.Clamp(tiltZ, -15f, 15f);

        spriteTransform.rotation = Quaternion.Lerp(
            spriteTransform.rotation,
            Quaternion.Euler(0f, 0f, tiltZ),
            3f * Time.deltaTime
        );
    }

    void SetupCollisionIgnores()
    {
        Collider2D thisCollider = GetComponent<Collider2D>();
        if (thisCollider == null) return;

        if (player != null)
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider != null)
                Physics2D.IgnoreCollision(playerCollider, thisCollider);
        }

        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("possess"))
        {
            Collider2D otherCollider = obj.GetComponent<Collider2D>();
            if (otherCollider != null)
                Physics2D.IgnoreCollision(thisCollider, otherCollider);
        }

        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("intangible"))
        {
            if (obj == gameObject) continue;
            Collider2D otherCollider = obj.GetComponent<Collider2D>();
            if (otherCollider != null)
                Physics2D.IgnoreCollision(thisCollider, otherCollider);
        }
    }
}
