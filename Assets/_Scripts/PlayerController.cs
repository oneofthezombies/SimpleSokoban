using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private const float moveDamping = 15f;

    public Material Orange = null;

    private Transform _transform = null;
    private BlockMover blockMover = null;
    private CameraController cameraController = null;
    private BlockMover lastPushable = null;

    private void Start()
    {
        _transform = transform;
        blockMover = GetComponent<BlockMover>();
        cameraController = Camera.main.GetComponent<CameraController>();
    }

    private void Update()
    {
        bool isMovable = true;
        if (lastPushable != null)
        {
            if (!lastPushable.isArrived)
            {
                isMovable = false;
            }
        }

        if (blockMover.isArrived && isMovable)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            if (IsPressed(horizontal + vertical))
            {
                Vector3 movement = GetMovement(horizontal, vertical);
                Vector3 target = _transform.position + movement;

                bool isMove = false;
                GameManager gameManager = GameManager.instance;
                CoordState coordState = gameManager.GetCoordState(target);
                if (coordState == CoordState.kPushable ||
                    coordState == CoordState.kGoal)
                {
                    Vector3 targetForPushable = target + movement;
                    if (gameManager.GetCoordState(targetForPushable) == CoordState.kMovable)
                    {
                        lastPushable = gameManager.GetPushable(target);
                        lastPushable.MoveTo(targetForPushable, moveDamping);

                        BlockMover goal = gameManager.GetGoal(targetForPushable);
                        if (goal)
                        {
                            lastPushable.material = Orange;
                            gameManager.SetCoordState(targetForPushable, CoordState.kGoal);
                        }
                        else
                        {
                            gameManager.SetCoordState(targetForPushable, CoordState.kPushable);
                        }

                        isMove = true;
                    }
                }
                else if (coordState == CoordState.kMovable)
                {
                    isMove = true;
                }

                if (isMove)
                {
                    blockMover.MoveTo(target, moveDamping * 0.4f);
                    blockMover.isSmooth = false;
                    gameManager.SetCoordState(target, CoordState.kPlayer);
                    gameManager.SetCoordState(_transform.position, CoordState.kMovable);

                    const float degreesXFactor = 0.5f;
                    const float degreesZFactor = 0.5f;
                    Quaternion destination = Quaternion.Euler(new Vector3(-movement.z * degreesXFactor, 0f, movement.x * degreesZFactor));
                    cameraController.rotationDestination = cameraController.rotation * destination;
                    cameraController.rotationDamping = 3.0f;
                    cameraController.isLookAt = false;
                }
            }
        }
    }

    private bool IsPressed(float value)
    {
        return value != 0f;
    }

    private Vector3 GetMovement(float horizontal, float vertical)
    {
        if (IsPressed(horizontal))
        {
            return Vector3.right * horizontal;
        }
        else if (IsPressed(vertical))
        {
            return Vector3.forward * vertical;
        }
        else
        {
            return Vector3.zero;
        }
    }
}
