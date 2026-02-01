using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    public bool isSwimming = true;
    public GameObject control;

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
    public float possessRadius = 1f;

    [Header("Camera Settings")]
    public Camera cam;

    [Header("Particles")]
    public ParticleSystem possessionParticles;
    public float burstDuration = 0.3f;
    public float hoverDuration = 0.2f;
    public float homingGravity = 50f;
    public float maxHomingSpeed = 25f;

    private Rigidbody2D rb;
    private Vector2 targetVelocity;
    private float currentTilt;

    private enum ParticlePhase { None, Burst, Hover, Homing }
    private class PossessionParticle
    {
        public int index;
        public Vector3 velocity;
        public float phaseTimer;
        public ParticlePhase phase;
    }
    private List<PossessionParticle> activeParticles = new List<PossessionParticle>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        if (cam == null) cam = Camera.main;

        if (possessionParticles == null)
            Debug.LogWarning("No particle system assigned on PlayerController.");
    }

    void Update()
    {
        HandleMovementInput();
        HandlePossessInput();
        HandleParticles();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, moveDrag * Time.fixedDeltaTime);

        if (targetVelocity.magnitude < 0.05f)
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, stopDrag * Time.fixedDeltaTime);

        RotateSpriteTowardsMouse();
    }

    private void HandleMovementInput()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        Vector2 toMouse = mouseWorldPos - transform.position;
        float distance = toMouse.magnitude;

        if (isSwimming)
        {
            if (distance <= stopRadius)
                targetVelocity = Vector2.zero;
            else
            {
                float speedFactor = Mathf.Clamp01((distance - stopRadius) / (slowRadius - stopRadius));
                targetVelocity = toMouse.normalized * speedFactor * moveSpeed;
                targetVelocity = Vector2.ClampMagnitude(targetVelocity, maxSpeed);
            }
        }
        else
        {
            targetVelocity = Vector2.zero;
            if (control != null)
                rb.position = control.GetComponent<Rigidbody2D>().position;
        }
    }

    private void RotateSpriteTowardsMouse()
    {
        if (sprite == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        Vector3 dir = mouseWorldPos - sprite.transform.position;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        currentTilt = Mathf.LerpAngle(currentTilt, targetAngle, turnSpeed * Time.deltaTime);
        sprite.transform.rotation = Quaternion.Euler(0f, 0f, currentTilt);
    }

    private void HandlePossessInput()
    {
        if (isSwimming && control != null && control.GetComponent<FishController>().blackProgress >= 1.0f) possessionParticles.Emit(50);

        if (!Mouse.current.rightButton.wasPressedThisFrame) return;

        if (isSwimming)
        {
            GameObject newControl = CheckPossess(possessRadius);
            if (newControl != null)
            {
                control = newControl;
                isSwimming = false;
                control.GetComponent<FishController>().isControlled = true;
                sprite.GetComponent<SpriteRenderer>().enabled = false;
                GetComponent<BoxCollider2D>().enabled = false;

                if (possessionParticles != null)
                {
                    var emission = possessionParticles.emission;
                    emission.enabled = false;

                    if (possessionParticles.particleCount == 0)
                    possessionParticles.Emit(50);

                    ParticleSystem.Particle[] particles = new ParticleSystem.Particle[possessionParticles.main.maxParticles];
                    int count = possessionParticles.GetParticles(particles);

                    activeParticles.Clear();

                    for (int i = 0; i < count; i++)
                    {
                        PossessionParticle p = new PossessionParticle();
                        p.index = i;

                        Vector2 vel2D = Random.insideUnitCircle.normalized * Random.Range(4f, 8f);
                        p.velocity = new Vector3(vel2D.x, vel2D.y, 0f);

                        p.phase = ParticlePhase.Burst;
                        p.phaseTimer = 0f;

                        activeParticles.Add(p);
                    }
                }
            }
        }
        else
        {
            isSwimming = true;
            if (sprite != null) sprite.GetComponent<SpriteRenderer>().enabled = true;
            GetComponent<BoxCollider2D>().enabled = true;

            if (control != null)
            {
                control.GetComponent<FishController>().isControlled = false;
                rb.linearVelocity = Vector2.zero;
            }

            if (possessionParticles != null)
            {
                var emission = possessionParticles.emission;
                emission.enabled = true;
                activeParticles.Clear();
            }

            control = null;
        }
    }

    private void HandleParticles()
    {
        if (activeParticles.Count == 0 || possessionParticles == null) return;

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[possessionParticles.main.maxParticles];
        int count = possessionParticles.GetParticles(particles);

        activeParticles.RemoveAll(p => p.index >= count);

        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            PossessionParticle p = activeParticles[i];

            switch (p.phase)
            {
                case ParticlePhase.Burst:
                    particles[p.index].position += p.velocity * Time.deltaTime;
                    p.velocity *= Mathf.Pow(0.9f, Time.deltaTime * 60f);
                    p.phaseTimer += Time.deltaTime;
                    if (p.phaseTimer >= burstDuration)
                    {
                        p.phase = ParticlePhase.Hover;
                        p.phaseTimer = 0f;
                        p.velocity = Vector3.zero;
                    }
                    break;

                case ParticlePhase.Hover:
                    p.phaseTimer += Time.deltaTime;
                    if (p.phaseTimer >= hoverDuration)
                    {
                        p.phase = ParticlePhase.Homing;
                        p.phaseTimer = 0f;
                    }
                    break;

                case ParticlePhase.Homing:
                    Vector3 dir = -particles[p.index].position;
                    p.velocity += dir.normalized * homingGravity * Time.deltaTime;
                    if (p.velocity.magnitude > maxHomingSpeed)
                        p.velocity = p.velocity.normalized * maxHomingSpeed;

                    particles[p.index].position += p.velocity * Time.deltaTime;

                    if (dir.magnitude < 0.05f)
                    {
                        particles[p.index].remainingLifetime = 0f;
                        activeParticles.RemoveAt(i);
                        continue;
                    }
                    break;
            }
        }

        possessionParticles.SetParticles(particles, count);
    }

    private GameObject CheckPossess(float radius = 1f)
    {
        GameObject closest = null;

        Collider2D[] results = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (Collider2D collider in results)
        {
            if (collider.gameObject.CompareTag("possess"))
            {
                if (closest == null)
                {
                    closest = collider.gameObject;
                }
                else
                {
                    float currentDist = (closest.transform.position - transform.position).sqrMagnitude;
                    float newDist = (collider.transform.position - transform.position).sqrMagnitude;
                    if (newDist < currentDist)
                        closest = collider.gameObject;
                }
            }
        }

        return closest;
    }
}
