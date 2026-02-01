using UnityEngine;

public class SimpleSpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float framesPerSecond = 12f;

    private SpriteRenderer sr;
    private int currentFrame = 0;
    private float timer = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (frames.Length == 0)
        {
            Debug.LogWarning("oops dun fuked up: No frames assigned to SimpleSpriteAnimator.");
        }
    }

    void Update()
    {
        if (frames.Length == 0) return;

        timer += Time.deltaTime;

        if (timer >= 1f / framesPerSecond)
        {
            timer -= 1f / framesPerSecond;
            currentFrame = (currentFrame + 1) % frames.Length;
            sr.sprite = frames[currentFrame];
        }
    }
}
