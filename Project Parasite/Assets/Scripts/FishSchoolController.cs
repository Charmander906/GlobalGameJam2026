using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FishSchoolController : MonoBehaviour, IHasPlayer
{
    [Header("Base Movement")]
    public float baseSpeed = 3f;
    public float maxSpeed = 6f;
    public float acceleration = 2f;
    public float deceleration = 4f;

    [Header("Wave Motion")]
    public float waveAmplitude = 1.5f;
    public float waveFrequency = 1f;

    [Header("Zigzag Motion")]
    public float zigzagStrength = 0f;
    public float zigzagFrequency = 2f;

    [Header("Pulse Motion")]
    public float pulseStrength = 0f;
    public float pulseFrequency = 2f;

    [Header("Peak Boost")]
    public float peakBoostStrength = 0f;
    public float peakBoostSharpness = 6f;

    [Header("Follower")]
    public GameObject player;
    GameObject IHasPlayer.player => player;
    public List<GameObject> followers = new List<GameObject>();
    public float followSlowDistance = 3f;
    public float followStopDistance = 6f;

    [Header("Turning")]
    public float turnCooldown = 0.5f;

    private int moveDir = 1;
    private float currentSpeed;
    private float sineTime;
    private float lastTurnTime;
    private Vector3 patternOffset;

    void Start()
    {
        currentSpeed = baseSpeed;

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

        HandleFollowerSpeed();

        float targetSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

        Vector3 forwardMove = Vector3.right * moveDir * targetSpeed * Time.deltaTime;

        Vector3 newOffset = CalculatePatternOffset(sineTime);
        Vector3 offsetDelta = newOffset - patternOffset;
        patternOffset = newOffset;

        transform.position += forwardMove + offsetDelta;
    }

    void HandleFollowerSpeed()
    {
        if (followers.Count == 0)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, baseSpeed, acceleration * Time.deltaTime);
            return;
        }

        float worstDistance = 0f;

        foreach (GameObject f in followers)
        {
            if (f == null) continue;
            float d = Vector3.Distance(transform.position, f.transform.position);
            if (d > worstDistance) worstDistance = d;
        }

        if (worstDistance > followStopDistance)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
        }
        else if (worstDistance > followSlowDistance)
        {
            float t = Mathf.InverseLerp(followSlowDistance, followStopDistance, worstDistance);
            float slowedSpeed = Mathf.Lerp(baseSpeed, 0f, t);
            currentSpeed = Mathf.MoveTowards(currentSpeed, slowedSpeed, deceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, baseSpeed, acceleration * Time.deltaTime);
        }
    }

    Vector3 CalculatePatternOffset(float t)
    {
        Vector3 offset = Vector3.zero;

        if (waveAmplitude > 0f)
        {
            float wave = Mathf.Sin(t * waveFrequency) * waveAmplitude;
            offset += Vector3.up * wave;
        }

        if (zigzagStrength > 0f)
        {
            float zigzag = Mathf.Sign(Mathf.Sin(t * zigzagFrequency)) * zigzagStrength;
            offset += Vector3.up * zigzag;
        }

        if (pulseStrength > 0f)
        {
            float pulse = Mathf.Pow(Mathf.Sin(t * pulseFrequency), 8f) * pulseStrength;
            offset += Vector3.right * moveDir * pulse;
        }

        if (peakBoostStrength > 0f && waveAmplitude > 0f)
        {
            float wave = Mathf.Sin(t * waveFrequency);
            float peak = Mathf.Pow(Mathf.Clamp01(wave), peakBoostSharpness);
            offset += Vector3.right * moveDir * peak * peakBoostStrength;
        }

        return offset;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time - lastTurnTime < turnCooldown) return;

        lastTurnTime = Time.time;
        TurnAround();
    }

    void TurnAround()
    {
        moveDir *= -1;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * moveDir;
        transform.localScale = scale;
    }
}
