using UnityEngine;
using UnityEngine.InputSystem;

public class FishController : MonoBehaviour, IHasPlayer
{
    [Header("Fish Settings")]
    public bool isControlled = false;
    public GameObject player;
    GameObject IHasPlayer.player => player;

    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float maxSpeed = 7f;
    public float stopDrag = 5f;
    public float moveDrag = 8f;
    public float slowRadius = 2.5f;
    public float stopRadius = 0.5f;
    public float turnSpeed = 6f;

    [Header("Dash Settings (Matches Player)")]
    public float dashSpeed = 35f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 1f;
    public float autoDashDistance = 4f;

    private bool dashing = false;
    private bool hasDash = true;

    [Header("Follower Settings")]
    public GameObject followTarget;
    public GameObject fishSchool;
    public float followVariance = 0.2f;

    [Header("Sprite Settings")]
    public GameObject sprite;
    public float rotationMultiplier = 1.5f;

    [Header("Control Blackening")]
    public float controlThreshold = 3f;
    public float blackFadeDuration = 5f;
    [Range(0f, 1f)] public float blackProgress = 0f;
    public float blackMaxDarkness = 0.8f;
    [HideInInspector] public float timeControlled = 0f;
    public float fadeOutSpeed = 0.2f;

    private Camera cam;
    public Rigidbody2D rb;
    private Vector2 targetVelocity;
    private bool facingRight;
    private float currentTilt;
    private float sineTime;

    void Start()
    {
        slowRadius = player.GetComponent<PlayerController>().slowRadius;
        stopRadius = player.GetComponent<PlayerController>().stopRadius;

        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        cam = player.GetComponent<PlayerController>().cam;

        sineTime = Random.value * 6f;

        Collider2D thisCollider = GetComponent<Collider2D>();
        if (thisCollider == null)
        {
            Debug.LogWarning("oops dun fuked up again");
            return;
        }

        if (player != null)
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider != null)
                Physics2D.IgnoreCollision(playerCollider, thisCollider);
        }

        GameObject[] possessObjects = GameObject.FindGameObjectsWithTag("possess");
        foreach (GameObject obj in possessObjects)
        {
            if (obj == this.gameObject) continue;

            Collider2D otherCollider = obj.GetComponent<Collider2D>();
            if (otherCollider != null)
                Physics2D.IgnoreCollision(thisCollider, otherCollider);
        }
    }

    void Update()
    {
        sineTime += Time.deltaTime;
        HandleBlackening();

        if (isControlled)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
            mouseWorldPos.z = 0f;

            Vector2 toMouse = mouseWorldPos - transform.position;
            float distance = toMouse.magnitude;

            if (Mouse.current.leftButton.wasPressedThisFrame && hasDash)
            {
                dashing = true;
                hasDash = false;
                Invoke(nameof(DashingFalse), dashDuration);
            }

            if (distance <= stopRadius)
            {
                targetVelocity = Vector2.zero;
            }
            else
            {
                if (dashing)
                {
                    targetVelocity = toMouse.normalized * dashSpeed;
                }
                else
                {
                    float speedFactor = Mathf.Clamp01((distance - stopRadius) / (slowRadius - stopRadius));
                    targetVelocity = toMouse.normalized * speedFactor * moveSpeed;
                    targetVelocity = Vector2.ClampMagnitude(targetVelocity, maxSpeed);
                }
            }
        }

        else if (followTarget != null)
        {
            Vector3 targetPos = followTarget.transform.position;
            targetPos.z = 0f;

            targetPos += new Vector3(
                Random.Range(-followVariance, followVariance),
                Random.Range(-followVariance, followVariance) + (Mathf.Sin(sineTime) * followVariance),
                0f
            );

            Vector2 toTarget = targetPos - transform.position;
            float distance = toTarget.magnitude;

            if (distance > autoDashDistance && hasDash)
            {
                dashing = true;
                hasDash = false;
                Invoke(nameof(DashingFalse), dashDuration);
            }

            if (dashing)
            {
                targetVelocity = toTarget.normalized * dashSpeed;
            }
            else
            {
                float speedFactor = 1f;
                if (distance < slowRadius)
                    speedFactor = Mathf.Min((distance / slowRadius) + 0.5f, 1f);

                Vector2 desiredVelocity = toTarget.normalized * moveSpeed * speedFactor;
                targetVelocity = Vector2.ClampMagnitude(desiredVelocity, maxSpeed);
            }
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, moveDrag * Time.fixedDeltaTime);

        if (targetVelocity.magnitude < 0.05f)
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, stopDrag * Time.fixedDeltaTime);

        Vector2 vel = rb.linearVelocity;

        if (vel.sqrMagnitude > 0.01f)
        {
            vel.Normalize();
            bool facingRight_ = vel.x >= 0f;
            if (facingRight_ != facingRight) facingRight = facingRight_;

            sprite.transform.localScale = new Vector3(facingRight ? 1f : -1f, 1f, 1f);

            float targetTilt = Mathf.Clamp(vel.y * rotationMultiplier * 45f, -80f, 80f) * (facingRight ? 1f : -1f);
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, turnSpeed * Time.fixedDeltaTime);
        }
        else
        {
            currentTilt = Mathf.Lerp(currentTilt, 0f, turnSpeed * Time.fixedDeltaTime);
        }

        sprite.transform.rotation = Quaternion.Euler(0f, 0f, currentTilt);
    }

    void DashingFalse()
    {
        dashing = false;
        Invoke(nameof(DashCooldown), dashCooldown);
    }

    void DashCooldown()
    {
        hasDash = true;
    }

    void HandleBlackening()
    {
        if (isControlled)
        {
            timeControlled += Time.deltaTime;
            if (timeControlled > controlThreshold)
                blackProgress = Mathf.Clamp01((timeControlled - controlThreshold) / blackFadeDuration);
        }
        else
        {
            if (blackProgress > 0f)
            {
                blackProgress -= fadeOutSpeed * Time.deltaTime;
                blackProgress = Mathf.Max(0f, blackProgress);
            }
        }

        ApplyBlackTint(blackProgress);
    }

    void ApplyBlackTint(float t)
    {
        if (sprite.TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
        {
            float darkness = Mathf.Lerp(1f, 1f - blackMaxDarkness, t);
            sr.color = new Color(darkness, darkness, darkness, 1f);
        }
    }
}
