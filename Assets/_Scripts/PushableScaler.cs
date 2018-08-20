using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushableScaler : MonoBehaviour
{
    private const float twoPI = Mathf.PI * 2f;

    private const float originScale = 0.6f;
    private const float scaleFactor = 0.05f;
    private const float radiansFactor = 0.08f;

    private float radiansXFactor = 0f;
    private float radiansYFactor = Mathf.PI / 3f;
    private float radiansZFactor = Mathf.PI * 2f / 3f;

    private Transform _transform = null;
    private float radians = 0f;

    private void Awake()
    {
        _transform = transform;
    }

    private void Update()
    {
        radians += radiansFactor;
        if (radians >= twoPI)
            radians -= twoPI;

        float scaleXFactor = Mathf.Sin(radians + radiansXFactor);
        float scaleYFactor = Mathf.Sin(radians + radiansYFactor);
        float scaleZFactor = Mathf.Sin(radians + radiansZFactor);

        _transform.localScale = Vector3.one * originScale + new Vector3(scaleXFactor, scaleYFactor, scaleZFactor) * scaleFactor;
    }
}
