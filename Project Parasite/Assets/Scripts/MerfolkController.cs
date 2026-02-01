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
    private bool attackInProgress = false;

    [Header("Swimming Movement")]
    public float swimAcceleration = 6f;
    public float maxSwimSpeed = 3f;
    public float chaseSpeedMultiplier = 2f;
    public float nodeReachDistance = 0.3f;
    public float idlePauseTime = 1.2f;

    [Header("Roam Node")]
    public Transform roamNode;
    public float roamRadius = 4f;

    [Header("Obstacle Avoidance")]
    public LayerMask groundLayer;
    public float obstacleCheckPadding = 0.2f;
    public int maxTargetAttempts = 10;

    [Header("Swim Motion Polish")]
    public float swayAmplitude = 0.5f;
    public float swayFrequency = 2f;

    [Header("Detection")]
    public float detectionRange = 6f;
    public float attackDistance = 2f;
    public float suspicionBuildRate = 0.5f;
    public float suspicionDecayRate = 0.2f;
    private float suspicion = 0f;
    private bool playerSpotted = false;

    [Header("Trident")]
    public GameObject tridentPrefab;
    public float tridentZOffset = -0.01f;

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
        HandleDetection();
    }

    void FixedUpdate()
    {
        HandleSwimmingMovement();
    }

    void HandleDetection()
    {
        if (player == null || playerSpotted) return;
        if (!sr.isVisible) return;

        Vector2 toPlayer = player.transform.position - transform.position;
        float dist = toPlayer.magnitude;

        if (dist > detectionRange)
        {
            suspicion = Mathf.Max(0f, suspicion - suspicionDecayRate * Time.deltaTime);
            return;
        }

        bool facingPlayer = (sr.flipX && toPlayer.x < 0) || (!sr.flipX && toPlayer.x > 0);
        if (!facingPlayer) return;

        float mimic = player.GetComponent<FishSchoolMimicEvaluator>().mimicEfficiency;

        if (mimic <= 0.4f)
            suspicion += suspicionBuildRate * 2f * Time.deltaTime;
        else if (mimic < 0.6f)
            suspicion += suspicionBuildRate * Mathf.InverseLerp(0.6f, 0.4f, mimic) * Time.deltaTime;
        else
            suspicion = Mathf.Max(0f, suspicion - suspicionDecayRate * Time.deltaTime);

        if (suspicion >= 1f)
        {
            playerSpotted = true;
            isIdling = false;
        }
    }

    void HandleSwimmingMovement()
    {
        if (attackInProgress) return;

        if (playerSpotted)
        {
            AggressiveBehavior();
            return;
        }

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

        SwimToward(toTarget, maxSwimSpeed);
    }

    void AggressiveBehavior()
    {
        if (player == null) return;

        Vector2 toPlayer = (Vector2)player.transform.position - rb.position;
        float dist = toPlayer.magnitude;

        if (!attackInProgress)
        {
            bool shouldAttack = dist <= attackDistance;
            if (shouldAttack)
            {
                rb.linearVelocity = Vector2.zero;
                TriggerAttack();
            }
            else
            {
                SwimToward(toPlayer, maxSwimSpeed * chaseSpeedMultiplier);
                isAttacking = false;
            }
        }
    }

    void SwimToward(Vector2 direction, float speed)
    {
        Vector2 dir = direction.normalized;
        swayTimer += Time.fixedDeltaTime * swayFrequency;
        Vector2 perpendicular = new Vector2(-dir.y, dir.x);
        dir += perpendicular * Mathf.Sin(swayTimer) * swayAmplitude * 0.1f;
        dir.Normalize();

        Vector2 desiredVelocity = dir * speed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desiredVelocity, swimAcceleration * Time.fixedDeltaTime);
    }

    void PickNewSwimTarget()
    {
        if (roamNode == null) return;

        for (int i = 0; i < maxTargetAttempts; i++)
        {
            Vector2 candidate = (Vector2)roamNode.position + Random.insideUnitCircle * roamRadius;
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
        if (!playerSpotted) PickNewSwimTarget();
    }

    void HandleAnimation()
    {
        animTimer += Time.deltaTime;
        if (animTimer < 1f / animationFPS) return;

        animTimer = 0f;
        animIndex++;

        List<Sprite> currentAnim = isAttacking ? attackFrames : idleFrames;
        if (currentAnim.Count == 0) return;

        if (animIndex >= currentAnim.Count)
        {
            if (isAttacking)
            {
                attackInProgress = false;
                isAttacking = false;
            }
            animIndex = 0;
        }

        sr.sprite = currentAnim[animIndex];
    }

    public void TriggerAttack()
    {
        if (attackFrames.Count == 0) return;
        attackInProgress = true;
        isAttacking = true;
        animIndex = 0;
        animTimer = 0f;

        FacePlayerAtAttackStart();
        SpawnTrident();
    }

    void FacePlayerAtAttackStart()
    {
        if (player == null) return;

        Vector2 toPlayer = player.transform.position - transform.position;
        sr.flipX = toPlayer.x < 0;
    }

    void SpawnTrident()
    {
        if (tridentPrefab == null || player == null) return;

        Vector3 spawnPos = transform.position;
        spawnPos.z += tridentZOffset;
        GameObject tridentObj = Instantiate(tridentPrefab, spawnPos, Quaternion.identity);

        TridentProjectile trident = tridentObj.GetComponent<TridentProjectile>();
        if (trident == null) return;

        trident.player = player;
        trident.followTarget = this.transform;

        SpriteRenderer tridentSR = tridentObj.GetComponent<SpriteRenderer>();
        if (tridentSR != null)
            tridentSR.flipX = sr.flipX;

        trident.ForceStartAnimation();

        Collider2D tridentCol = trident.GetComponent<Collider2D>();
        Collider2D merfolkCol = GetComponent<Collider2D>();

        if (tridentCol != null)
        {
            if (merfolkCol != null)
                Physics2D.IgnoreCollision(tridentCol, merfolkCol);

            foreach (GameObject obj in GameObject.FindGameObjectsWithTag("intangible"))
            {
                if (obj == tridentObj) continue;
                Collider2D otherCol = obj.GetComponent<Collider2D>();
                if (otherCol != null)
                    Physics2D.IgnoreCollision(tridentCol, otherCol);
            }

            foreach (GameObject obj in GameObject.FindGameObjectsWithTag("possess"))
            {
                Collider2D otherCol = obj.GetComponent<Collider2D>();
                if (otherCol != null)
                    Physics2D.IgnoreCollision(tridentCol, otherCol);
            }

            Collider2D playerCol = player.GetComponent<Collider2D>();
            if (playerCol != null)
                Physics2D.IgnoreCollision(tridentCol, playerCol);
        }
    }

    void UpdateSpriteRotation()
    {
        if (attackInProgress)
        {
            spriteTransform.rotation = Quaternion.Lerp(spriteTransform.rotation, Quaternion.Euler(0f, 0f, 0f), 5f * Time.deltaTime);
            return;
        }

        Vector2 vel = rb.linearVelocity;
        float speed = vel.magnitude;
        if (speed < 0.01f) return;

        bool facingLeft = vel.x < 0;
        sr.flipX = facingLeft;

        float tiltZ = speed * rotationMultiplier;
        if (!facingLeft) tiltZ *= -1f;
        tiltZ = Mathf.Clamp(tiltZ, -15f, 15f);

        spriteTransform.rotation = Quaternion.Lerp(spriteTransform.rotation, Quaternion.Euler(0f, 0f, tiltZ), 3f * Time.deltaTime);
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
