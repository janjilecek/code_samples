using System.Collections;
using System.Collections.Generic;
using System.Security;
using UnityEditor;
using UnityEngine;

public class MazeGen : MonoBehaviour
{
    public GameObject wall;
    public GameObject floor;

    [Header("Dimensions controller")] 
    public int rows;
    public int cols;

    [Header("Wall Traps")] public GameObject[] wallTraps;

    [Header("Floor Traps")] public GameObject[] floorTraps;

    [Header("Custom Walls")] public GameObject[] customWalls;

    [Header("Archetypes")] public GameObject shadow;

    public GameObject tunnelHolder;

    private MazeCell[,] _grid;
    private List<GameObject> _floors;
    private List<GameObject> _localTraps;

    public GameObject oT;
    public GameObject oX;
    public GameObject oI;
    public GameObject oD;
    public GameObject oL;
    public GameObject oDoor;


    private God _god;


    private int _currentRow = 0;
    private int _currentCol = 0;
    private bool _scanComplete = false;


    public float mnm = 87f;

    // Start is called before the first frame update
    void Start()
    {
        _god = GameObject.FindGameObjectWithTag("God").GetComponent<God>();

        _floors = new List<GameObject>();
        _localTraps = new List<GameObject>();
        RegenerateGrid();

        //_god.setNextRubedoRoom();

        GameObject[] _traps = GameObject.FindGameObjectsWithTag("TrapHolder");
        int rand;
        foreach (GameObject trp in _traps)
        {
            rand = Random.Range(0, 15);
            if (rand < 8)
            {
                trp.SetActive(false);
            }
        }
    }


    void RegenerateGrid()
    {
        //Random.InitState(2008303430);

        // destroy all children
        foreach (Transform _transform in transform)
        {
            Destroy(_transform.gameObject);
        }

        _floors = new List<GameObject>();
        _localTraps = new List<GameObject>();
        _currentCol = 0;
        _currentRow = 0;
        _scanComplete = false;
        CreateGrid();
        HuntAndKill();
        GenerateTraps();
        //DeleteVisibleTraps();

        handleJunctions();
        //_god.respawnAnima();
        //_god.spawnAudiologs();
        setVolumeAll();
    }

    void setVolumeAll() // sets volume of all traps
    {
        GameObject[] gom = GameObject.FindGameObjectsWithTag("Trap");

        foreach (GameObject gob in gom)
        {
            AudioSource aso = gob.GetComponent<AudioSource>();

            if (aso)
            {
                AudioClip cl = aso.clip;
                //Debug.Log(cl.name);
                if (cl.name == "pila2" || cl.name == "pila" || cl.name == "2untitled")
                {
                    aso.volume = 0.03f;
                }
            }
        }
    }


    private void handleJunctions()
    {
        int countStraight = 0;
        int numOfExits = 2;
        for (int rowIndex = 0; rowIndex < rows; rowIndex++)
        {
            for (int colIndex = 0; colIndex < cols; colIndex++)
            {
                int T = -1;
                int L = -1;
                int X = -1;
                int I = -1;
                int D = -1;

                I = isStraight(_grid[rowIndex, colIndex]);
                X = isCross(_grid[rowIndex, colIndex]);
                L = isCellCorner(_grid[rowIndex, colIndex]);
                D = isDeadend(_grid[rowIndex, colIndex]);
                T = isTJunction(_grid[rowIndex, colIndex]);

                Debug.Log($"--- {rowIndex},{colIndex} : T: {T}, I: {I}, L: {L}, X: {X}, D: {D}");
                //Debug.Log(grid[rowIndex, colIndex].hasUpWall + ", " + grid[rowIndex, colIndex].hasRightWall + ", " + grid[rowIndex, colIndex].hasDownWall + ", " + grid[rowIndex, colIndex].hasLeftWall);


                HandleMazeCell(I, oI, rowIndex, colIndex);
                HandleMazeCell(X, oX, rowIndex, colIndex);
                HandleMazeCell(L, oL, rowIndex, colIndex);
                HandleMazeCell(D, oD, rowIndex, colIndex);
                HandleMazeCell(T, oT, rowIndex, colIndex);
            }
        }
    }


    void HandleMazeCell(int typeNum, GameObject typeHolder, int rowIndex, int colIndex)
    {
        float localScaler = 0.65f;
        if (typeNum != 999 && typeNum != -1)
        {
            GameObject part = Instantiate(
                typeHolder,
                _grid[rowIndex, colIndex].floor.transform.position,
                Quaternion.identity);

            part.transform.localScale = new Vector3(localScaler, localScaler, localScaler);
            part.transform.Rotate(0f, typeNum, 0f, Space.World);
            part.transform.parent = tunnelHolder.transform;
        }
    }


    int isCellCorner(MazeCell cell)
    {
        int plus = 180;
        if (cell.hasDownWall && cell.hasRightWall && !cell.hasLeftWall && !cell.hasUpWall)
        {
            return 0;
        }

        if (!cell.hasDownWall && cell.hasRightWall && !cell.hasLeftWall && cell.hasUpWall)
        {
            return 90 + plus;
        }

        if (!cell.hasDownWall && !cell.hasRightWall && cell.hasLeftWall && cell.hasUpWall)
        {
            return 180;
        }

        if (cell.hasDownWall && !cell.hasRightWall && cell.hasLeftWall && !cell.hasUpWall)
        {
            return 270 + plus;
        }

        return 999;
    }

    int isTJunction(MazeCell cell)
    {
        if (!cell.hasDownWall && !cell.hasRightWall && !cell.hasLeftWall && cell.hasUpWall)
        {
            return 0;
        }

        if (!cell.hasDownWall && cell.hasRightWall && !cell.hasLeftWall && !cell.hasUpWall)
        {
            return 90;
        }

        if (cell.hasDownWall && !cell.hasRightWall && !cell.hasLeftWall && !cell.hasUpWall)
        {
            return 180;
        }

        if (!cell.hasDownWall && !cell.hasRightWall && cell.hasLeftWall && !cell.hasUpWall)
        {
            return 270;
        }

        return 999;
    }

    int isStraight(MazeCell cell)
    {
        if (!cell.hasDownWall && cell.hasRightWall && cell.hasLeftWall && !cell.hasUpWall)
        {
            return 90;
        }

        if (cell.hasDownWall && !cell.hasRightWall && !cell.hasLeftWall && cell.hasUpWall)
        {
            return 0;
        }

        return 999;
    }

    int isCross(MazeCell cell)
    {
        if (!cell.hasDownWall && !cell.hasRightWall && !cell.hasLeftWall && !cell.hasUpWall)
        {
            return 0;
        }

        return 999;
    }

    int isDeadend(MazeCell cell)
    {
        int plus = 0;
        if (!cell.hasDownWall && cell.hasRightWall && cell.hasLeftWall && cell.hasUpWall)
        {
            return 0 + plus;
        }

        if (cell.hasDownWall && cell.hasRightWall && !cell.hasLeftWall && cell.hasUpWall)
        {
            return 90 + plus;
        }

        if (cell.hasDownWall && cell.hasRightWall && cell.hasLeftWall && !cell.hasUpWall)
        {
            return 180 + plus;
        }

        if (cell.hasDownWall && !cell.hasRightWall && cell.hasLeftWall && cell.hasUpWall)
        {
            return 270 + plus;
        }

        return 999;
    }


    void CreateGrid()
    {
        float size = wall.transform.localScale.x;
        _grid = new MazeCell[rows, cols];


        float a = 1.25f; // spacing depends on prefab dimensions
        float b = 1.75f;

        for (int rowIndex = 0; rowIndex < rows; rowIndex++)
        {
            for (int colIndex = 0; colIndex < cols; colIndex++)
            {
                int randIndex = Random.Range(0, customWalls.Length);

                wall = customWalls[randIndex];

                GameObject main_floor = Instantiate(floor,
                    new Vector3(this.transform.position.x + colIndex * size, 0,
                        transform.position.z + -rowIndex * size), Quaternion.identity);
                main_floor.name = "Floor_" + rowIndex + "_" + colIndex;
                _floors.Add(main_floor);


                GameObject up_wall = Instantiate(wall,
                    new Vector3(this.transform.position.x + colIndex * size, b,
                        transform.position.z + -rowIndex * size + a), Quaternion.identity);
                up_wall.name = "Up_Wall_" + rowIndex + "_" + colIndex;

                GameObject down_wall = Instantiate(wall,
                    new Vector3(this.transform.position.x + colIndex * size, b,
                        transform.position.z + -rowIndex * size - a), Quaternion.identity);
                down_wall.name = "Down_Wall_" + rowIndex + "_" + colIndex;

                GameObject left_wall = Instantiate(wall,
                    new Vector3(this.transform.position.x + colIndex * size - a, b,
                        transform.position.z + -rowIndex * size), Quaternion.Euler(0, 90, 0));
                left_wall.name = "Left_Wall_" + rowIndex + "_" + colIndex;

                GameObject right_wall = Instantiate(wall,
                    new Vector3(this.transform.position.x + colIndex * size + a, b,
                        transform.position.z + -rowIndex * size), Quaternion.Euler(0, -90, 0));
                right_wall.name = "Right_Wall_" + rowIndex + "_" + colIndex;

                main_floor.transform.parent = transform;
                up_wall.transform.parent = transform;
                down_wall.transform.parent = transform;
                right_wall.transform.parent = transform;
                left_wall.transform.parent = transform;

                _grid[rowIndex, colIndex] = new MazeCell(up_wall, down_wall, right_wall, left_wall, main_floor);


                // Destroy first and last
                if (rowIndex == 0 && colIndex == 0)
                {
                    //Destroy(left_wall);
                    //grid[rowIndex, colIndex].hasLeftWall = false;
                }

                if (rowIndex == rows - 1 && colIndex == cols - 1)
                {
                    //Destroy(right_wall);
                    //grid[rowIndex, colIndex].hasRightWall = false;
                }
            }
        }
    }


    bool unvisitedNeighborsExist()
    {
        // up
        if (isCellUnvisited(_currentRow - 1, _currentCol))
        {
            return true;
        }

        // down
        if (isCellUnvisited(_currentRow + 1, _currentCol))
        {
            return true;
        }

        // left
        if (isCellUnvisited(_currentRow, _currentCol - 1))
        {
            return true;
        }

        // right
        if (isCellUnvisited(_currentRow, _currentCol + 1))
        {
            return true;
        }

        return false;
    }

    bool isCellUnvisited(int row, int col)
    {
        return (row >= 0 && row < rows && col < cols && col >= 0 && !_grid[row, col].visited);
    }

    void HuntAndKill()
    {
        // mark the first cell
        _grid[_currentRow, _currentCol].visited = true;

        while (!_scanComplete)
        {
            Walk();
            Hunt();
        }
    }


    void Walk()
    {
        while (unvisitedNeighborsExist())
        {
            int direction = Random.Range(0, 4);
            switch (direction)
            {
                case 0: // up
                    if (_currentRow > 0 && !_grid[_currentRow - 1, _currentCol].visited)
                    {
                        if (_grid[_currentRow, _currentCol].upWall)
                        {
                            Destroy(_grid[_currentRow, _currentCol].upWall);
                            _grid[_currentRow, _currentCol].hasUpWall = false;
                        }

                        _currentRow--;
                        _grid[_currentRow, _currentCol].visited = true;

                        if (_grid[_currentRow, _currentCol].downWall)
                        {
                            Destroy(_grid[_currentRow, _currentCol].downWall);
                            _grid[_currentRow, _currentCol].hasDownWall = false;
                        }
                    }

                    break;
                case 1: // down
                    if (_currentRow < rows - 1 && !_grid[_currentRow + 1, _currentCol].visited)
                    {
                        if (_grid[_currentRow, _currentCol].downWall)
                        {
                            Destroy(_grid[_currentRow, _currentCol].downWall);
                            _grid[_currentRow, _currentCol].hasDownWall = false;
                        }

                        _currentRow++;
                        _grid[_currentRow, _currentCol].visited = true;
                        if (_grid[_currentRow, _currentCol].upWall)
                        {
                            Destroy(_grid[_currentRow, _currentCol].upWall);
                            _grid[_currentRow, _currentCol].hasUpWall = false;
                        }
                    }

                    break;

                case 2: // left
                    if (_currentCol > 0 && !_grid[_currentRow, _currentCol - 1].visited)
                    {
                        if (_grid[_currentRow, _currentCol].leftWall)
                        {
                            Destroy(_grid[_currentRow, _currentCol].leftWall);
                            _grid[_currentRow, _currentCol].hasLeftWall = false;
                        }

                        _currentCol--;
                        _grid[_currentRow, _currentCol].visited = true;
                        if (_grid[_currentRow, _currentCol].rightWall)
                        {
                            Destroy(_grid[_currentRow, _currentCol].rightWall);
                            _grid[_currentRow, _currentCol].hasRightWall = false;
                        }
                    }

                    break;

                case 3: // right
                    if (_currentCol < cols - 1 && !_grid[_currentRow, _currentCol + 1].visited)
                    {
                        if (_grid[_currentRow, _currentCol].rightWall)
                        {
                            Destroy(_grid[_currentRow, _currentCol].rightWall);
                            _grid[_currentRow, _currentCol].hasRightWall = false;
                        }

                        _currentCol++;
                        _grid[_currentRow, _currentCol].visited = true;
                        if (_grid[_currentRow, _currentCol].leftWall)
                        {
                            Destroy(_grid[_currentRow, _currentCol].leftWall);
                            _grid[_currentRow, _currentCol].hasLeftWall = false;
                        }
                    }

                    break;
            }
        }
    }

    void Hunt()
    {
        _scanComplete = true;
        for (int rowIndex = 0; rowIndex < rows; rowIndex++)
        {
            for (int colIndex = 0; colIndex < cols; colIndex++)
            {
                if (!_grid[rowIndex, colIndex].visited && AreThereVisitedNeighbors(rowIndex, colIndex))
                {
                    _scanComplete = false;
                    _currentRow = rowIndex;
                    _currentCol = colIndex;
                    _grid[_currentRow, _currentCol].visited = true;
                    DestroyAdjacentWall();
                    return;
                }
            }
        }
    }

    public bool AreThereVisitedNeighbors(int row, int column)
    {
        // check up.
        if (row > 0 && _grid[row - 1, column].visited)
        {
            return true;
        }

        // check down.
        if (row < rows - 1 && _grid[row + 1, column].visited)
        {
            return true;
        }

        // check left.
        if (column > 0 && _grid[row, column - 1].visited)
        {
            return true;
        }

        // check right.
        if (column < cols - 1 && _grid[row, column + 1].visited)
        {
            return true;
        }

        return false;
    }

    void DestroyAdjacentWall()
    {
        bool destroyed = false;

        while (!destroyed)
        {
            // pick a random adjacent cell that is visited and within boundaries,
            // and destroy the wall/s between the current cell and adjacent cell.
            int direction = Random.Range(0, 4);

            // check up.
            if (direction == 0)
            {
                if (_currentRow > 0 && _grid[_currentRow - 1, _currentCol].visited)
                {
                    if (_grid[_currentRow, _currentCol].upWall)
                    {
                        Destroy(_grid[_currentRow, _currentCol].upWall);
                        _grid[_currentRow, _currentCol].hasUpWall = false;
                    }

                    if (_grid[_currentRow - 1, _currentCol].downWall)
                    {
                        Destroy(_grid[_currentRow - 1, _currentCol].downWall);
                        _grid[_currentRow - 1, _currentCol].hasDownWall = false;
                    }

                    destroyed = true;
                }
            }
            // check down.
            else if (direction == 1)
            {
                if (_currentRow < rows - 1 && _grid[_currentRow + 1, _currentCol].visited)
                {
                    if (_grid[_currentRow, _currentCol].downWall)
                    {
                        Destroy(_grid[_currentRow, _currentCol].downWall);
                        _grid[_currentRow, _currentCol].hasDownWall = false;
                    }

                    if (_grid[_currentRow + 1, _currentCol].upWall)
                    {
                        Destroy(_grid[_currentRow + 1, _currentCol].upWall);
                        _grid[_currentRow + 1, _currentCol].hasUpWall = false;
                    }

                    destroyed = true;
                }
            }
            // check left.
            else if (direction == 2)
            {
                if (_currentCol > 0 && _grid[_currentRow, _currentCol - 1].visited)
                {
                    if (_grid[_currentRow, _currentCol].leftWall)
                    {
                        Destroy(_grid[_currentRow, _currentCol].leftWall);
                        _grid[_currentRow, _currentCol].hasLeftWall = false;
                    }

                    if (_grid[_currentRow, _currentCol - 1].rightWall)
                    {
                        Destroy(_grid[_currentRow, _currentCol - 1].rightWall);
                        _grid[_currentRow, _currentCol - 1].hasRightWall = false;
                    }

                    destroyed = true;
                }
            }
            // check right.
            else if (direction == 3)
            {
                if (_currentCol < cols - 1 && _grid[_currentRow, _currentCol + 1].visited)
                {
                    if (_grid[_currentRow, _currentCol].rightWall)
                    {
                        Destroy(_grid[_currentRow, _currentCol].rightWall);
                        _grid[_currentRow, _currentCol].hasRightWall = false;
                    }

                    if (_grid[_currentRow, _currentCol + 1].leftWall)
                    {
                        Destroy(_grid[_currentRow, _currentCol + 1].leftWall);
                        _grid[_currentRow, _currentCol + 1].hasLeftWall = false;
                    }

                    destroyed = true;
                }
            }
        }
    }

    void GenerateTraps()
    {
        int counter = 0;
        transform.position = transform.position + new Vector3(0f, mnm, 0f);
        foreach (MazeCell cell in _grid)
        {
            foreach (Transform child in cell.floor.transform)
            {
                if (child.CompareTag("TrapHolder"))
                {
                    if (counter == Mathf.RoundToInt(_grid.Length / 2))
                    {
                        Debug.Log("Spawning Shadow at " + child.transform.position);
                        Instantiate(shadow, child.transform.position, Quaternion.identity);
                        cell.hasEnemy = true;
                        break; // don't generate traps for this cell that holds Shadow
                    }

                    int genOrNot = Random.Range(0, 500);

                    if (genOrNot < 100 && counter > _grid.Length / 8) // generuje nahodne a negeneruj na zacataku
                    {
                        // negeneruj pasti v rozich
                        if (!((cell.hasUpWall && cell.hasLeftWall) ||
                              (cell.hasUpWall && cell.hasRightWall) ||
                              (cell.hasDownWall && cell.hasRightWall) ||
                              (cell.hasDownWall && cell.hasLeftWall)))
                        {
                            int randIndex = Random.Range(0, floorTraps.Length);
                            GameObject trap = Instantiate(floorTraps[randIndex], child.transform.position,
                                Quaternion.identity);
                            trap.transform.parent = transform;
                            cell.trap = trap;
                            _localTraps.Add(trap);
                        }
                    }
                }
            }

            counter++;
        }
    }

    void DeleteVisibleTraps()
    {
        for (int index = 0; index < _localTraps.Count; index++)
        {
            for (int j = 0; j < _localTraps.Count; j++)
            {
                if (index == j) continue;
                if (!Physics.Linecast(_localTraps[index].transform.position, _localTraps[j].transform.position))
                {
                    Debug.Log("Destroying trap" + index + "," + j);
                    Destroy(_localTraps[index].gameObject);
                }
            }
        }
    }
}