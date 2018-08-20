using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockMover : MonoBehaviour
{
    private const float smoothTime = 0.15f;

    private Transform _transform = null;
    private MeshRenderer meshRenderer = null;
    private float moveDamping = 0.0f;
    private Vector3 destination = Vector3.zero;
    private Vector3 velocity = Vector3.zero;

    private bool _isSmooth = true;
    public bool isSmooth
    {
        set { _isSmooth = value; }
    }

    private bool _isArrived = false;
    public bool isArrived
    {
        get { return _isArrived; }
    }

    public Material material
    {
        set { meshRenderer.material = value; }
    }

    private void Awake()
    {
        _transform = transform;
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        if (!_isArrived)
        {
            if (IsArrived())
            {
                _isArrived = true;
                _transform.position = destination;
            }
        }

        if (!_isArrived)
        {
            if (_isSmooth)
            {
                SmoothlyMove();
            }
            else
            {
                Move();
            }
        }
    }

    private void SmoothlyMove()
    {
        //_transform.position = Vector3.Lerp(_transform.position, destination, moveDamping * Time.deltaTime);
        _transform.position = Vector3.SmoothDamp(_transform.position, destination, ref velocity, smoothTime);
    }

    private bool IsArrived()
    {
        return Mathf.Abs((destination - _transform.position).sqrMagnitude) < 0.0005f;
    }

    public void MoveTo(Vector3 dest, float moveDamp)
    {
        destination = dest;
        moveDamping = moveDamp;
        _isArrived = false;
    }

    public void MoveTo(Vector3 start, Vector3 dest, float moveDamp)
    {
        _transform.position = start;
        MoveTo(dest, moveDamp);
    }

    private void Move()
    {
        Vector3 prevPosition = _transform.position;
        Vector3 toDest = destination - prevPosition;
        _transform.position += toDest.normalized * moveDamping * Time.deltaTime;
        Vector3 overDest = _transform.position - prevPosition;
        if (overDest.sqrMagnitude > toDest.sqrMagnitude)
        {
            _transform.position = destination;
        }
    }
}
