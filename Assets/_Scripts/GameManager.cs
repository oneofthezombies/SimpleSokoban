using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

//[SerializeField]
public enum CoordState
{
    kNonReachable,
    kMovable,
    kPushable,
    kGoal,
    kPlayer
}

public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;

    public static GameManager instance
    {
        get { return _instance; }
    }

    public CameraController CameraController = null;
    public GroupController TerrainGroup = null;
    public GroupController GoalGroup = null;
    public GroupController PushableGroup = null;
    public GroupController PlayerGroup = null;

    public GameObject TerrainPrefab = null;
    public GameObject GoalPrefab = null;
    public GameObject PushablePrefab = null;
    public GameObject PlayerPrefab = null;

    public Material[] Materials = null;

    public Text StageText = null;
    public GameObject InGameUI = null;
    public GameObject OutroText = null;

    private const float startMoveDamping = 2.5f;
    private const float terrainStartHeight = -200f;
    private const float othersStartHeight = -10f;   // for goal, pushable, player

    private float terrainOffsetY = 0f;
    private float goalOffsetY = 0f;
    private float pushableOffsetY = 0f;
    private float playerOffsetY = 0f;

    private CoordState[,] _coordStates = null;
    private int coordOffset = 0;
    private int numGoals = 0;
    private int numClearedGoals = 0;
    private bool isStaging = false;
    private bool isHiding = false;
    //private bool isBreakingTerrain = false;
    private List<Transform> visibles = null;

    private int stage = 1;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        terrainOffsetY = -GetHalfHeight(TerrainPrefab);
        goalOffsetY = GetHalfHeight(GoalPrefab);
        pushableOffsetY = GetHalfHeight(PushablePrefab);
        playerOffsetY = 0.3f;

        visibles = new List<Transform>();

        SetStage1();
        //SetStage2();
        //SetStage3();
        //SetStage4();
        //Outro();
    }

    private void Update()
    {
        if (isStaging && numGoals != 0 && numClearedGoals == numGoals && IsArrived())
        {
            isStaging = false;

            HideToTerrain();
        }

        if (isHiding && IsArrived())
        {
            isHiding = false;

            BreakTerrain();

            StartCoroutine(Fall());
        }
    }

    private void SetStage1()
    {
        isStaging = true;
        visibles.Clear();

        _coordStates = new CoordState[6, 6];
        for (int i = 0; i < _coordStates.GetLength(0); ++i)
            for (int j = 0; j < _coordStates.GetLength(1); ++j)
                _coordStates[i, j] = CoordState.kNonReachable;
        coordOffset = 3;

        // left
        AddTerrain(-3f, 0f, Materials[0]);
        AddTerrain(-2f, 0f, Materials[1]);
        AddTerrain(-1f, 0f, Materials[0]);

        // forward
        AddTerrain(0f, 2f, Materials[1]);
        AddTerrain(0f, 1f, Materials[0]);
        AddTerrain(0f, 0f, Materials[1]);

        // right
        AddTerrain(2f, -1f, Materials[0]);
        AddTerrain(1f, -1f, Materials[1]);
        AddTerrain(0f, -1f, Materials[0]);

        // back
        AddTerrain(-1f, -3f, Materials[1]);
        AddTerrain(-1f, -2f, Materials[0]);
        AddTerrain(-1f, -1f, Materials[1]);

        MoveStartPosition(TerrainGroup, 100.0f);

        AddGoal(-3f, 0f);
        AddGoal(0f, 2f);
        AddGoal(2f, -1f);
        AddGoal(-1f, -3f);

        AddPushable(-1f, 0f);
        AddPushable(0f, 1f);
        AddPushable(1f, -1f);
        AddPushable(-1f, -2f);

        AddPlayer(0f, 0f);

        CameraController.rotation = Quaternion.Euler(45f, 255f, 0f);
        CameraController.rotationDestination = Quaternion.Euler(45f, 345f, 0f);
        CameraController.rotationDamping = 3.0f;
        CameraController.isLookAt = false;
        CameraController.visibles = visibles;

        StageText.text = "First Stage";
    }

    private float GetHalfHeight(GameObject go)
    {
        return go.transform.localScale.y / 2f;
    }

    private BlockMover AddBlock(GameObject prefab, GroupController group, Vector3 start, Vector3 dest, float moveDamp, Material material)
    {
        BlockMover bc = Instantiate(prefab, group.transform)
            .GetComponent<BlockMover>();

        bc.MoveTo(start, dest, moveDamp);
        bc.material = material;
        return bc;
    }

    private void AddTerrain(float x, float z, Material material)
    {
        AddBlock(TerrainPrefab, TerrainGroup, new Vector3(x, terrainStartHeight, z), new Vector3(x, terrainOffsetY, z), startMoveDamping, material);
        SetCoordState(x, z, CoordState.kMovable);
    }

    private void AddGoal(float x, float z)
    {
        BlockMover bc = AddBlock(GoalPrefab, GoalGroup, new Vector3(x, othersStartHeight, z), new Vector3(x, goalOffsetY, z), startMoveDamping, Materials[2]);

        ++numGoals;

        visibles.Add(bc.transform);
    }

    private void AddPushable(float x, float z)
    {
        BlockMover bc = AddBlock(PushablePrefab, PushableGroup, new Vector3(x, othersStartHeight, z), new Vector3(x, pushableOffsetY, z), startMoveDamping, Materials[4]);
        SetCoordState(x, z, CoordState.kPushable);

        visibles.Add(bc.transform);
    }

    private void AddPlayer(float x, float z)
    {
        BlockMover bc = AddBlock(PlayerPrefab, PlayerGroup, new Vector3(x, othersStartHeight, z), new Vector3(x, playerOffsetY, z), startMoveDamping, Materials[3]);
        SetCoordState(x, z, CoordState.kPlayer);

        visibles.Add(bc.transform);
    }

    private void MoveStartPosition(GroupController group, float stride)
    {
        Transform[] transforms = group.GetElements()
            .OrderBy(tf => tf.position.x + tf.position.z)
            .ToArray();

        int[] ranges = Enumerable.Range(0, transforms.Length).ToArray();

        for (int i = 0; i < transforms.Length; ++i)
        {
            Vector3 pos = transforms[i].position;
            transforms[i].position = new Vector3(pos.x, pos.y - ranges[i] * stride, pos.z);
        }
    }

    private void SetCoordState(float x, float z, CoordState coordState)
    {
        int coordX = GetNearInt(x) + coordOffset;
        int coordY = GetNearInt(z) + coordOffset;
        if (coordY > -1 &&
            coordX > -1 &&
            coordY < _coordStates.GetLength(0) &&
            coordX < _coordStates.GetLength(1))
        {
            CoordState prevCoordState = _coordStates[coordY, coordX];
            _coordStates[coordY, coordX] = coordState;

            if (coordState == CoordState.kGoal && prevCoordState != CoordState.kGoal)
            {
                ++numClearedGoals;
            }
            else if (coordState != CoordState.kGoal && prevCoordState == CoordState.kGoal)
            {
                --numClearedGoals;
            }
        }
    }

    public void SetCoordState(Vector3 position, CoordState coordState)
    {
        SetCoordState(position.x, position.z, coordState);
    }

    public CoordState GetCoordState(Vector3 pos)
    {
        int coordX = GetNearInt(pos.x) + coordOffset;
        int coordY = GetNearInt(pos.z) + coordOffset;
        if (coordY < 0 ||
            coordX < 0 ||
            coordY >= _coordStates.GetLength(0) || 
            coordX >= _coordStates.GetLength(1))
        {
            return CoordState.kNonReachable;
        }

        return _coordStates[coordY, coordX];
    }

    private BlockMover GetBlockMover(GroupController group, Vector3 position)
    {
        Vector2 v2Position = new Vector2(position.x, position.z);

        foreach (Transform tf in group.GetElements())
        {
            if ((new Vector2(tf.position.x, tf.position.z) - v2Position).sqrMagnitude < 0.001f)
            {
                return tf.GetComponent<BlockMover>();
            }
        }
        return null;
    }

    public BlockMover GetPushable(Vector3 position)
    {
        return GetBlockMover(PushableGroup, position);
    }

    public BlockMover GetGoal(Vector3 position)
    {
        return GetBlockMover(GoalGroup, position);
    }

    private int GetNearInt(float value)
    {
        int iValue = (int)value;
        if (iValue - value < 0.001f)
        {
            return iValue;
        }
        else
        {
            return iValue + 1;
        }
    }

    private void HideToTerrain()
    {
        isHiding = true;

        const float hideFactor = 2.3f;
        MoveDown(PlayerGroup, playerOffsetY * hideFactor, startMoveDamping, true);
        MoveDown(PushableGroup, pushableOffsetY * hideFactor, startMoveDamping, true);
        MoveDown(GoalGroup, goalOffsetY * hideFactor, startMoveDamping, true);
    }

    private void MoveDown(GroupController group, float depthY, float moveDaming, bool isSmooth)
    {
        Transform[] transforms = group.GetElements();
        foreach (var tf in transforms)
        {
            Vector3 pos = tf.position;
            BlockMover blockMover = tf.GetComponent<BlockMover>();
            blockMover.MoveTo(new Vector3(pos.x, pos.y - depthY, pos.z), moveDaming);
            blockMover.isSmooth = isSmooth;
        }
    }

    private bool IsArrived(GroupController group)
    {
        Transform[] transforms = group.GetElements();
        foreach (var tf in transforms)
        {
            BlockMover blockMover = tf.GetComponent<BlockMover>();
            if (!blockMover.isArrived)
                return false;
        }

        return true;
    }

    private bool IsArrived()
    {
        return IsArrived(PlayerGroup) 
            && IsArrived(PushableGroup) 
            && IsArrived(GoalGroup);
    }

    private void BreakTerrain()
    {
        //isBreakingTerrain = true;
        CameraController.isFloating = false;

        const float hideFactor = 30f;
        const float moveDaming = 6f;
        MoveDown(PlayerGroup, hideFactor, moveDaming, true);
        MoveDown(PushableGroup, hideFactor, moveDaming, true);
        MoveDown(GoalGroup, hideFactor, moveDaming, true);

        Transform[] transforms = TerrainGroup.GetElements();
        foreach (var tf in transforms)
        {
            Vector3 pos = tf.position;
            BlockMover blockMover = tf.GetComponent<BlockMover>();
            blockMover.MoveTo(new Vector3(pos.x, pos.y - Random.Range(1f, 4f), pos.z), moveDaming);
        }
    }

    IEnumerator Fall()
    {
        yield return new WaitForSeconds(0.2f);

        const float hideFactor = 30f;
        const float moveDamping = 6f;
        MoveDown(PlayerGroup, hideFactor, moveDamping, false);
        MoveDown(PushableGroup, hideFactor, moveDamping, false);
        MoveDown(GoalGroup, hideFactor, moveDamping, false);
        MoveDown(TerrainGroup, hideFactor, moveDamping, false);

        yield return new WaitForSeconds(2f);

        ++stage;

        StartCoroutine(SetStage());
    }

    private void SetStage2()
    {
        isStaging = true;
        visibles.Clear();

        _coordStates = new CoordState[7, 7];
        for (int i = 0; i < _coordStates.GetLength(0); ++i)
            for (int j = 0; j < _coordStates.GetLength(1); ++j)
                _coordStates[i, j] = CoordState.kNonReachable;
        coordOffset = 0;

        AddTerrain(0f, 0f, Materials[0]);
        AddTerrain(1f, 0f, Materials[1]);
        AddTerrain(2f, 0f, Materials[0]);
        AddTerrain(0f, 1f, Materials[1]);
        AddTerrain(1f, 1f, Materials[0]);
        AddTerrain(2f, 1f, Materials[1]);
        AddTerrain(0f, 2f, Materials[0]);
        AddTerrain(1f, 2f, Materials[1]);
        AddTerrain(2f, 2f, Materials[0]);
        AddTerrain(2f, 3f, Materials[1]);
        AddTerrain(2f, 4f, Materials[0]);
        AddTerrain(1f, 5f, Materials[0]);
        AddTerrain(2f, 5f, Materials[1]);
        AddTerrain(3f, 5f, Materials[0]);
        AddTerrain(1f, 6f, Materials[1]);
        AddTerrain(2f, 6f, Materials[0]);
        AddTerrain(3f, 6f, Materials[1]);
        AddTerrain(3f, 4f, Materials[1]);
        AddTerrain(4f, 4f, Materials[0]);
        AddTerrain(5f, 4f, Materials[1]);
        AddTerrain(5f, 5f, Materials[0]);
        AddTerrain(6f, 2f, Materials[0]);
        AddTerrain(6f, 3f, Materials[1]);
        AddTerrain(6f, 4f, Materials[0]);
        AddTerrain(6f, 5f, Materials[1]);

        MoveStartPosition(TerrainGroup, 2.0f);

        AddGoal(6f, 2f);
        AddGoal(6f, 3f);
        AddGoal(6f, 4f);

        AddPushable(1f, 1f);
        AddPushable(2f, 1f);
        AddPushable(1f, 2f);

        AddPlayer(0f, 0f);

        CameraController.rotation = Quaternion.Euler(50f, 255f, 0f);
        CameraController.rotationDestination = Quaternion.Euler(50f, 345f, 0f);
        CameraController.rotationDamping = 3.0f;
        CameraController.isLookAt = false;
        CameraController.visibles = visibles;

        StageText.text = "Second Stage";
    }

    private void SetStage3()
    {
        isStaging = true;
        visibles.Clear();

        _coordStates = new CoordState[6, 4];
        for (int i = 0; i < _coordStates.GetLength(0); ++i)
            for (int j = 0; j < _coordStates.GetLength(1); ++j)
                _coordStates[i, j] = CoordState.kNonReachable;
        coordOffset = 0;

        AddTerrain(1f, 0f, Materials[1]);
        AddTerrain(2f, 0f, Materials[0]);
        AddTerrain(0f, 1f, Materials[0]);
        AddTerrain(1f, 1f, Materials[1]);
        AddTerrain(2f, 1f, Materials[0]);
        AddTerrain(1f, 2f, Materials[1]);
        AddTerrain(2f, 2f, Materials[0]);
        AddTerrain(1f, 3f, Materials[1]);
        AddTerrain(2f, 3f, Materials[0]);
        AddTerrain(3f, 3f, Materials[1]);
        AddTerrain(0f, 4f, Materials[0]);
        AddTerrain(1f, 4f, Materials[1]);
        AddTerrain(2f, 4f, Materials[0]);
        AddTerrain(3f, 4f, Materials[1]);
        AddTerrain(0f, 5f, Materials[0]);
        AddTerrain(1f, 5f, Materials[1]);
        AddTerrain(2f, 5f, Materials[0]);
        AddTerrain(3f, 5f, Materials[1]);

        MoveStartPosition(TerrainGroup, 2.0f);

        AddGoal(0f, 4f);
        AddGoal(0f, 5f);
        AddGoal(1f, 5f);
        AddGoal(2f, 5f);
        AddGoal(3f, 5f);

        AddPushable(1f, 1f);
        AddPushable(1f, 2f);
        AddPushable(1f, 4f);
        AddPushable(2f, 3f);
        AddPushable(2f, 5f);

        AddPlayer(0f, 1f);

        CameraController.rotation = Quaternion.Euler(55f, 255f, 0f);
        CameraController.rotationDestination = Quaternion.Euler(55f, 345f, 0f);
        CameraController.rotationDamping = 3.0f;
        CameraController.isLookAt = false;
        CameraController.visibles = visibles;

        BlockMover blockMover = GetPushable(new Vector3(2f, 0f, 5f));
        blockMover.material = Materials[2];
        SetCoordState(2f, 5f, CoordState.kGoal);

        StageText.text = "Third Stage";
    }

    private void SetStage4()
    {
        isStaging = true;
        visibles.Clear();

        _coordStates = new CoordState[5, 8];
        for (int i = 0; i < _coordStates.GetLength(0); ++i)
            for (int j = 0; j < _coordStates.GetLength(1); ++j)
                _coordStates[i, j] = CoordState.kNonReachable;
        coordOffset = 0;

        AddTerrain(1f, 0f, Materials[0]);
        AddTerrain(2f, 0f, Materials[1]);
        AddTerrain(3f, 0f, Materials[0]);
        AddTerrain(4f, 0f, Materials[1]);
        AddTerrain(5f, 0f, Materials[0]);
        AddTerrain(1f, 1f, Materials[1]);
        AddTerrain(5f, 1f, Materials[1]);
        AddTerrain(6f, 1f, Materials[0]);
        AddTerrain(7f, 1f, Materials[1]);
        AddTerrain(0f, 2f, Materials[1]);
        AddTerrain(1f, 2f, Materials[0]);
        AddTerrain(2f, 2f, Materials[1]);
        AddTerrain(3f, 2f, Materials[0]);
        AddTerrain(4f, 2f, Materials[1]);
        AddTerrain(5f, 2f, Materials[0]);
        AddTerrain(6f, 2f, Materials[1]);
        AddTerrain(7f, 2f, Materials[0]);
        AddTerrain(0f, 3f, Materials[0]);
        AddTerrain(1f, 3f, Materials[1]);
        AddTerrain(2f, 3f, Materials[0]);
        AddTerrain(4f, 3f, Materials[0]);
        AddTerrain(5f, 3f, Materials[1]);
        AddTerrain(6f, 3f, Materials[0]);
        AddTerrain(1f, 4f, Materials[0]);
        AddTerrain(2f, 4f, Materials[1]);
        AddTerrain(4f, 4f, Materials[1]);
        AddTerrain(5f, 4f, Materials[0]);
        AddTerrain(6f, 4f, Materials[1]);

        MoveStartPosition(TerrainGroup, 2.0f);

        AddGoal(1f, 3f);
        AddGoal(1f, 4f);
        AddGoal(2f, 3f);
        AddGoal(2f, 4f);

        AddPushable(1f, 1f);
        AddPushable(3f, 2f);
        AddPushable(5f, 3f);
        AddPushable(6f, 2f);

        AddPlayer(1f, 2f);

        CameraController.rotation = Quaternion.Euler(53f, 255f, 20f);
        CameraController.rotationDestination = Quaternion.Euler(53f, 345f, 20f);
        CameraController.rotationDamping = 3.0f;
        CameraController.isLookAt = false;
        CameraController.visibles = visibles;

        StageText.text = "Last Stage";
    }

    public void OnClickRetryButton()
    {
        StartCoroutine(SetStage());
    }

    IEnumerator SetStage()
    {
        numGoals = 0;
        numClearedGoals = 0;
        TerrainGroup.ClearElements();
        GoalGroup.ClearElements();
        PushableGroup.ClearElements();
        PlayerGroup.ClearElements();
        CameraController.visibles.Clear();

        yield return new WaitForSeconds(0.1f);

        if (stage == 1)
        {
            SetStage1();
        }
        else if (stage == 2)
        {
            SetStage2();
        }
        else if (stage == 3)
        {
            SetStage3();
        }
        else if (stage == 4)
        {
            SetStage4();
        }
        else
        {
            Outro();
        }
    }

    private void Outro()
    {
        InGameUI.SetActive(false);
        OutroText.SetActive(true);
    }
}
