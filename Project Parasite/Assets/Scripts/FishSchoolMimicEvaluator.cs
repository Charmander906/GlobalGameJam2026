using System.Collections.Generic;
using UnityEngine;

public class FishSchoolMimicEvaluator : MonoBehaviour
{
    [Header("Evaluation Settings")]
    public float maxDistanceScore = 5f;
    public float maxSpeedDifference = 5f;
    public float maxRotationDifference = 90f;

    [Header("Outlier Filtering")]
    public float positionOutlierThreshold = 2f;
    public float speedOutlierThreshold = 2f;
    public float rotationOutlierThreshold = 2f;

    [Header("Weights")]
    [Range(0f,1f)] public float positionWeight = 0.5f;
    [Range(0f,1f)] public float speedWeight = 0.3f;
    [Range(0f,1f)] public float rotationWeight = 0.2f;

    [Header("Blackening Influence")]
    [Range(0f,1f)] public float blackeningInfluence = 1f;

    //[HideInInspector]
    public float mimicEfficiency = 0f;
    private PlayerController playerController;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
            Debug.LogWarning("oops dun fuked up: FishSchoolMimicEvaluator missing PlayerController");
    }

    void Update()
    {
        if (playerController.isSwimming)
        {
            mimicEfficiency = 0f;
            //Debug.Log($"Fish mimic score: {mimicEfficiency:F2}");
            return;
        }

        GameObject controlledFish = playerController.control;
        FishController fc = controlledFish.GetComponent<FishController>();
        if (fc == null || fc.fishSchool == null) return;

        List<FishController> schoolFish = new List<FishController>();
        foreach (Transform child in fc.fishSchool.transform)
        {
            if (child.gameObject.CompareTag("possess") && child.gameObject != controlledFish)
            {
                FishController f = child.GetComponent<FishController>();
                if (f != null) schoolFish.Add(f);
            }
        }
        if (schoolFish.Count == 0) return;

        List<float> distances = new List<float>();
        foreach (var f in schoolFish)
            distances.Add(Vector3.Distance(f.transform.position, controlledFish.transform.position));

        float posMean = Mean(distances);
        float posStd = StdDev(distances, posMean);

        List<FishController> filteredPosFish = new List<FishController>();
        foreach (var f in schoolFish)
        {
            float d = Vector3.Distance(f.transform.position, controlledFish.transform.position);
            if (Mathf.Abs(d - posMean) <= positionOutlierThreshold * posStd)
                filteredPosFish.Add(f);
        }

        Vector3 schoolCenter = Vector3.zero;
        foreach (var f in filteredPosFish)
            schoolCenter += f.transform.position;
        if (filteredPosFish.Count > 0)
            schoolCenter /= filteredPosFish.Count;
        else
            schoolCenter = controlledFish.transform.position;

        float distanceToCenter = Vector3.Distance(controlledFish.transform.position, schoolCenter);
        float positionScore = Mathf.Clamp01(1f - (distanceToCenter / maxDistanceScore));

        List<float> speeds = new List<float>();
        foreach (var f in schoolFish)
            speeds.Add(f.rb.linearVelocity.magnitude);

        float speedMean = Mean(speeds);
        float speedStd = StdDev(speeds, speedMean);

        List<FishController> filteredSpeedFish = new List<FishController>();
        foreach (var f in schoolFish)
        {
            float s = f.rb.linearVelocity.magnitude;
            if (Mathf.Abs(s - speedMean) <= speedOutlierThreshold * speedStd)
                filteredSpeedFish.Add(f);
        }

        float avgSpeed = 0f;
        foreach (var f in filteredSpeedFish)
            avgSpeed += f.rb.linearVelocity.magnitude;
        avgSpeed /= Mathf.Max(1, filteredSpeedFish.Count);

        float controlledSpeed = fc.rb.linearVelocity.magnitude;
        float speedDifference = Mathf.Abs(controlledSpeed - avgSpeed);
        float speedScore = Mathf.Clamp01(1f - (speedDifference / maxSpeedDifference));

        Vector2 controlledDir = fc.rb.linearVelocity.normalized;
        if (controlledDir.sqrMagnitude < 0.01f) controlledDir = Vector2.right;

        List<float> angles = new List<float>();
        foreach (var f in schoolFish)
        {
            Vector2 otherDir = f.rb.linearVelocity.normalized;
            if (otherDir.sqrMagnitude < 0.01f) otherDir = Vector2.right;
            angles.Add(Vector2.Angle(controlledDir, otherDir));
        }

        float angleMean = Mean(angles);
        float angleStd = StdDev(angles, angleMean);

        List<FishController> filteredRotFish = new List<FishController>();
        foreach (var f in schoolFish)
        {
            Vector2 otherDir = f.rb.linearVelocity.normalized;
            if (otherDir.sqrMagnitude < 0.01f) otherDir = Vector2.right;
            float angleDiff = Vector2.Angle(controlledDir, otherDir);
            if (Mathf.Abs(angleDiff - angleMean) <= rotationOutlierThreshold * angleStd)
                filteredRotFish.Add(f);
        }

        float rotationSum = 0f;
        foreach (var f in filteredRotFish)
        {
            Vector2 otherDir = f.rb.linearVelocity.normalized;
            if (otherDir.sqrMagnitude < 0.01f) otherDir = Vector2.right;
            rotationSum += Vector2.Angle(controlledDir, otherDir);
        }
        float avgRotationDiff = rotationSum / Mathf.Max(1, filteredRotFish.Count);
        float rotationScore = Mathf.Clamp01(1f - (avgRotationDiff / maxRotationDifference));

        float mimicScore = Mathf.Clamp01(
            positionScore * positionWeight +
            speedScore * speedWeight +
            rotationScore * rotationWeight
        );

        float blackEffect = Mathf.Clamp01(fc.blackProgress * blackeningInfluence);
        mimicScore *= (1f - blackEffect);

        mimicEfficiency = mimicScore;

        //Debug.Log($"Fish mimic score: {mimicScore:F2} (Pos: {positionScore:F2}, Speed: {speedScore:F2}, Rot: {rotationScore:F2}, BlackEffect: {blackEffect:F2})");
    }

    private float Mean(List<float> values)
    {
        if (values.Count == 0) return 0f;
        float sum = 0f;
        foreach (var v in values) sum += v;
        return sum / values.Count;
    }

    private float StdDev(List<float> values, float mean)
    {
        if (values.Count == 0) return 0f;
        float sum = 0f;
        foreach (var v in values) sum += (v - mean) * (v - mean);
        return Mathf.Sqrt(sum / values.Count);
    }
}
