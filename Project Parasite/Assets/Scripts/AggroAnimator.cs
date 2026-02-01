using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AggroAnimator : MonoBehaviour
{
    [Header("Frames")]
    public List<Sprite> questionFrames;
    public Sprite exclamationFrame;

    [Header("Merfolk Reference")]
    public MerfolkController merfolk;

    private SpriteRenderer sr;
    private int lastFrameIndex = -1;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();

        if (merfolk == null)
            merfolk = GetComponentInParent<MerfolkController>();
    }

    void Update()
    {
        if (sr == null || merfolk == null || questionFrames.Count == 0) return;

        if (merfolk.playerSpotted)
        {
            if (sr.sprite != exclamationFrame)
                sr.sprite = exclamationFrame;
        }
        else
        {
            int frameIndex = Mathf.Clamp(Mathf.FloorToInt(merfolk.suspicion * (questionFrames.Count - 1)), 0, questionFrames.Count - 1);

            if (frameIndex != lastFrameIndex)
            {
                sr.sprite = questionFrames[frameIndex];
                lastFrameIndex = frameIndex;
            }
        }
    }

    public void ResetIndicator()
    {
        lastFrameIndex = -1;
        if (questionFrames.Count > 0)
            sr.sprite = questionFrames[0];
    }
}
