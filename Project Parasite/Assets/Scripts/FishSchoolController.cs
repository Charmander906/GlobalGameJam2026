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
    public float zigzagStrength = 1f;
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
    private float targetSpeed;
    private float sineTime;
    private float lastTurnTime;
    private Vector3 patternOffset;

    void Start()
    {
        currentSpeed = baseSpeed;
        targetSpeed = baseSpeed;

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
            Collider2D otherCollider = obj.GetComponent<Collider2D>();
            if (otherCollider != null)
                Physics2D.IgnoreCollision(thisCollider, otherCollider);
        }

        GameObject[] intangibleObjects = GameObject.FindGameObjectsWithTag("intangible");
        foreach (GameObject obj in intangibleObjects)
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

        HandleFollowerSpeed();

        Vector3 forwardMove = Vector3.right * moveDir * currentSpeed * Time.deltaTime;

        Vector3 newOffset = CalculatePatternOffset(sineTime);
        Vector3 offsetDelta = newOffset - patternOffset;
        patternOffset = newOffset;

        transform.position += forwardMove + offsetDelta;
    }

    void HandleFollowerSpeed()
    {
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, (currentSpeed < targetSpeed ? acceleration : deceleration) * Time.deltaTime);

        if (followers.Count == 0)
        {
            targetSpeed = baseSpeed;
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
            targetSpeed = 0f;
        else if (worstDistance > followSlowDistance)
        {
            float t = Mathf.InverseLerp(followSlowDistance, followStopDistance, worstDistance);
            targetSpeed = Mathf.Lerp(baseSpeed, 0f, t);
        }
        else
            targetSpeed = baseSpeed;
    }

    Vector3 CalculatePatternOffset(float t)
    {
        Vector3 offset = Vector3.zero;

        if (waveAmplitude > 0f)
        {
            float wave = Mathf.Sin(t * waveFrequency * 2f * Mathf.PI) * waveAmplitude;
            offset += Vector3.up * wave;
        }

        if (zigzagStrength > 0f)
        {
            float triangle = 2f * Mathf.Abs((t * zigzagFrequency) % 1f - 0.5f) - 1f;
            offset += Vector3.up * triangle * zigzagStrength;
        }

        if (pulseStrength > 0f)
        {
            float pulse = Mathf.Pow(Mathf.Sin(t * pulseFrequency * 2f * Mathf.PI), 8f) * pulseStrength;
            offset += Vector3.right * moveDir * pulse;
        }

        if (peakBoostStrength > 0f && waveAmplitude > 0f)
        {
            float wave = Mathf.Sin(t * waveFrequency * 2f * Mathf.PI);
            float peak = Mathf.Pow(Mathf.Clamp01(wave), peakBoostSharpness);
            offset += Vector3.right * moveDir * peak * peakBoostStrength;
        }

        return offset;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time - lastTurnTime < turnCooldown) return;

        lastTurnTime = Time.time;

        moveDir *= -1;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * moveDir;
        transform.localScale = scale;

        targetSpeed = 0f;
    }
}
