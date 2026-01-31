using UnityEngine;
using UnityEngine.InputSystem;

public class FishController : MonoBehaviour
{
    [Header("Fish Settings")]
    public bool isControlled = false;
    public GameObject player;
    //public AudioClip depossessSound;

    [Header("Base Movement Settings")]
    public float moveSpeed = 6f;
    public float maxSpeed = 7f;
    public float stopDrag = 5f;
    public float moveDrag = 8f;
    public float slowRadius = 2.5f;
    public float stopRadius = 0.5f;
    public float turnSpeed = 6f;

    [Header("Follower Settings")]
    public GameObject followTarget;
    public GameObject fishSchool;
    public float followVariance = 0.2f;

    [Header("Sprite Settings")]
    /*public Sprite[] inanimateSprite;
    public Sprite[] idleSprite;
    public Sprite[] runSprite;
    public Sprite[] jumpSprite;
    public Sprite[] landSprite;
    public Sprite backgroundSprite;
    public float animDelay = 0.1f;*/
    public GameObject sprite;
    public float rotationMultiplier = 1.5f;

    [Header("Blend Scores")]
    public float maxAcceptableDistance = 5f;
    [Range(0f, 1f)] public float mainTargetWeight = 0.65f;
    [Range(0f, 1f)] public float schoolWeight = 0.35f;

    [HideInInspector]
    public float mimicEfficiency = 0f;

    private Camera cam;
    private Rigidbody2D rb;
    private Vector3 spawnPoint;
    private Vector2 targetVelocity;
    private float currentAngle;
    private bool facingRight;
    private Vector2 moveInput;
    private float currentTilt;
    private float sineTime = 0f;
    /*private float animTimer = 0f;
    private int lastFrameIndex = -1;
    private int spriteOffset = 0;
    private float spriteOffsetTimer = 0f;*/
    //public AudioSource audioSource;

    void Start()
    {
        moveSpeed = player.GetComponent<PlayerController>().moveSpeed;
        maxSpeed = player.GetComponent<PlayerController>().maxSpeed;
        stopDrag = player.GetComponent<PlayerController>().stopDrag;
        moveDrag = player.GetComponent<PlayerController>().moveDrag;
        slowRadius = player.GetComponent<PlayerController>().slowRadius;
        stopRadius = player.GetComponent<PlayerController>().stopRadius;
        turnSpeed = player.GetComponent<PlayerController>().turnSpeed;

        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        cam = player.GetComponent<PlayerController>().cam;
        sineTime = Random.value * 6;

        //audioSource = gameObject.AddComponent<AudioSource>();
        //audioSource.spatialBlend = 0f;

        Collider2D thisCollider = GetComponent<Collider2D>();
        if (thisCollider == null)
        {
            Debug.LogWarning("oops done fuked up");
            return;
        }

        if (player != null)
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, thisCollider);
            }
        }

        GameObject[] possessObjects = GameObject.FindGameObjectsWithTag("possess");
        foreach (GameObject obj in possessObjects)
        {
            if (obj == gameObject) continue;

            Collider2D otherCollider = obj.GetComponent<Collider2D>();
            if (otherCollider != null)
            {
                Physics2D.IgnoreCollision(thisCollider, otherCollider);
            }
        }
    }

    void Update()
    {
        sineTime += Time.deltaTime;

        if (isControlled)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
            mouseWorldPos.z = 0f;

            Vector2 toMouse = mouseWorldPos - transform.position;
            float distance = toMouse.magnitude;

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

            float distToMain = Vector3.Distance(transform.position, followTarget.transform.position);
            float distToSchool = Vector3.Distance(transform.position, fishSchool.transform.position);

            float mainScore = Mathf.Clamp01(1f - (distToMain / maxAcceptableDistance));
            float schoolScore = Mathf.Clamp01(1f - (distToSchool / maxAcceptableDistance));

            mimicEfficiency = (mainScore * mainTargetWeight) + (schoolScore * schoolWeight);
        }
        else if (followTarget != null)
        {
            Vector3 targetPos = followTarget.transform.position;
            targetPos.z = 0f;

            targetPos += new Vector3(
                Random.Range(-followVariance, followVariance),
                Random.Range(-followVariance, followVariance) + (Mathf.Sin(sineTime) * 1.5f),
                0f
            );

            Vector2 toTarget = targetPos - transform.position;
            float distance = toTarget.magnitude;

            float speedFactor = 1f;

            if (distance < slowRadius)
                speedFactor = distance / slowRadius;

            Vector2 desiredVelocity = toTarget.normalized * moveSpeed * speedFactor;
            targetVelocity = Vector2.ClampMagnitude(desiredVelocity, maxSpeed);
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

    void OnCollisionEnter2D(Collision2D collision) { }

    void OnCollisionStay2D(Collision2D collision) { }

    private void OnTriggerStay2D(Collider2D other) { }

    public void Depossess()
    {
        //audioSource.PlayOneShot(depossessSound);
        //audioSource.PlayOneShot(dieSound);
        if (player.GetComponent<PlayerController>().control == gameObject && !player.GetComponent<PlayerController>().isSwimming)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

            pc.isSwimming = true;

            Vector2 mouseWorldPos = pc.cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            Vector2 direction = (mouseWorldPos - (Vector2)player.transform.position).normalized;

            rb.linearVelocity = direction * pc.moveSpeed;

            isControlled = false;
        }
    }
}