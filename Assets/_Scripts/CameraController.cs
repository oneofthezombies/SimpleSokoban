using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private const float twoPI = Mathf.PI * 2f;
    private const float radiansFactor = 0.05f;
    private const float radiansXFactor = Mathf.PI;
    private const float radiansZFactor = Mathf.PI / 2f;
    private const float rotationFactor = 0.05f;
    private const float smoothMoveTime = 0.2f;
    private const float screenEdgeBuffer = 2f;
    private const float minSize = 10f;

    private Transform _transform = null;
    private float radians = 0f;
    private Vector3 desiredPosition = Vector3.zero;
    private Vector3 moveVelocity = Vector3.zero;

    private List<Transform> _visibles = null;
    public List<Transform> visibles
    {
        get { return _visibles; }
        set { _visibles = value; }
    }

    private float _rotationDamping = 0f;
    public float rotationDamping
    {
        get { return _rotationDamping; }
        set { _rotationDamping = value; }
    }

    private Quaternion _rotationDestination = Quaternion.identity;
    public Quaternion rotationDestination
    {
        get { return _rotationDestination; }
        set { _rotationDestination = value; }
    }

    private bool _isLookAt = false;
    public bool isLookAt
    {
        get { return _isLookAt; }
        set { _isLookAt = value; }
    }

    private GameObject _player = null;
    public GameObject player
    {
        get { return _player; }
        set { _player = value; }
    }

    public Quaternion rotation
    {
        get { return _transform.rotation; }
        set { _transform.rotation = value; }
    }

    private bool _isFloating = true;
    public bool isFloating
    {
        get { return _isFloating; }
        set { _isFloating = value; }
    }

    private void Awake()
    {
        _transform = transform;
    }

    private void Update()
    {
        if (!_isLookAt)
        {
            if (IsLookAt())
            {
                _isLookAt = true;
                _isFloating = true;
            }
        }

        if (!_isLookAt)
        { 
            SmoothlyRotate();
        }

        if (_isFloating)
        {
            Float();

            if (_visibles != null &&
                _visibles.Count > 0)
            {
                Zoom();
            }
        }
    }

    private void SmoothlyRotate()
    {
        transform.rotation = Quaternion.Slerp(_transform.rotation, _rotationDestination, _rotationDamping * Time.deltaTime);
    }

    private bool IsLookAt()
    {
        return _transform.rotation == _rotationDestination;
    }

    private void Float()
    {
        radians += radiansFactor;
        if (radians >= twoPI)
            radians -= twoPI;

        float rotationXFactor = Mathf.Sin(radians + radiansXFactor);
        float rotationZFactor = Mathf.Sin(radians + radiansZFactor);

        transform.rotation *= Quaternion.Euler(new Vector3(rotationXFactor, 0f, rotationZFactor) * rotationFactor);
    }

    private void Zoom()
    {
        Vector3 averagePos = _visibles
            .Select(tf => tf.position)
            .Aggregate(Vector3.zero, (acc, e) => acc + e)
            / _visibles.Count;

        desiredPosition = averagePos;
        desiredPosition.y += 10f;

        Vector3 desiredLocalPos = _transform.InverseTransformPoint(desiredPosition);

        float size = 0f;

        for (int i = 0; i < _visibles.Count; i++)
        {
            if (!_visibles[i].gameObject.activeSelf)
                continue;

            Vector3 targetLocalPos = transform.InverseTransformPoint(_visibles[i].position);

            Vector3 desiredPosToTarget = targetLocalPos - desiredLocalPos;

            size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.y));

            size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.x) / Camera.main.aspect);
        }

        size += screenEdgeBuffer;

        size = Mathf.Max(size, minSize);

        const float sizeFactor = 1.3f;
        desiredPosition.y = size * sizeFactor;
        _transform.localPosition = Vector3.SmoothDamp(_transform.localPosition, desiredPosition, ref moveVelocity, smoothMoveTime);
    }
}
