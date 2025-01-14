using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using Random = System.Random;

public class Enemy : MonoBehaviour
{
    [SerializeField] private Movement playerMovement;
    private NodeGrid _grid;
    private Path _path;
    private int _pathIndex = 0;
    public Vector3Int position;
    [SerializeField] private int movementSpeed = 10;
    public int _startingLayer = 0;
    [SerializeField] private int viewRange = 12;
    [SerializeField] private SortingGroup visualLayer;

    private LineOfSight _lineOfSight;
    private Vector3Int _lastKnownPlayerPosition;
    private bool _hasLastKnownPosition = false;
    private Task MoveNextTask;
    [SerializeField] private SpriteRenderer sprite;

    private LineOfSight los;

    void Start()
    {
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<Movement>();
            if (playerMovement == null)
            {
                Debug.LogError("No Player Movement component found in the scene!");
                return;
            }
        }

        _grid = playerMovement._world.Grid;
        playerMovement.PlayerMoved += OnPlayerMoved;
        var cell = playerMovement._world.Tilemaps[_startingLayer].WorldToCell(transform.position);
        transform.position = playerMovement._world.Tilemaps[_startingLayer].GetCellCenterWorld(cell);
        position = new Vector3Int(cell.x, cell.y, _startingLayer);
        visualLayer.sortingOrder = _startingLayer + 1;

        // Initialize Line of Sight
        los = new LineOfSight(_grid);
    }

    private void OnDestroy()
    {
        if (playerMovement != null)
        {
            playerMovement.PlayerMoved -= OnPlayerMoved;
        }
    }

    private void OnPlayerMoved()
    {
        Debug.Log("player moved ");
        playerMovement.enemiesMoving++;
        UpdatePathToPlayer();
        TraversePath();
    }

    private void RandomPath()
    {
        Node currentNode = _grid.GetNodeFromCell(position.x, position.y, position.z);
        CustomTile currentTile = _grid.GetTile(currentNode.Position);
        List<Node> adjacents = new();
        playerMovement._world.Pathfinding.FindAdjacents(currentNode.Position, ref adjacents, false);

        // Check adjacent layers if applicable
        if (currentNode.Position.z >= 1 && currentTile != null && currentTile.layerTraversal)
            playerMovement._world.Pathfinding.FindAdjacents(new Vector3Int(currentNode.Position.x, currentNode.Position.y, currentNode.Position.z - 1), ref adjacents, false);

        if (currentNode.Position.z < _grid.Dimensions.z - 1)
            playerMovement._world.Pathfinding.FindAdjacents(new Vector3Int(currentNode.Position.x, currentNode.Position.y, currentNode.Position.z + 1), ref adjacents, true);

        Random rand = new Random();
        int attempts = 0;
        bool foundPath = false;
        while (attempts < 3 && !foundPath)
        {
            Node n = adjacents[rand.Next(0,adjacents.Count)];
            Path path = playerMovement._world.Pathfinding.FindPath(position.x, position.y, position.z, n.Position.x,
                n.Position.y, n.Position.z);
            if (path != null)
            {
                foundPath = true;
                _path = path;
                _pathIndex = 1;
                sprite.color = Color.red;
            }
            attempts++;
        }
    }

    private async Task MoveToCell(Node node)
    {
        playerMovement.enemiesMoving++;
        position = node.Position;
        if (position.z + 1 > visualLayer.sortingOrder) visualLayer.sortingOrder = position.z + 1;
        Vector3 target = _grid.GetCenter(node.Position);
        Vector3 direction = target - transform.position;
        float distanceThisFrame = Time.deltaTime * movementSpeed;

        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            float moved = Mathf.Min(distanceThisFrame, Vector3.Distance(transform.position, target));
            transform.position += (direction.normalized * moved);
            await Task.Yield();
        }

        playerMovement.enemiesMoving--;
    }

    private async Task TraversePath()
    {
        if (_path == null)
        {
            RandomPath();
            if (_path == null)
            {
                Debug.LogError("Enemy could not find random path, is he in a void area??");
                return;
            }
        }
        MoveNextTask = MoveNext();
        await MoveNextTask;
        playerMovement.enemiesMoving--;
    }

    public async Task MoveNext()
    {
        await Task.Delay(100);
        if (!(_pathIndex >= _path.Nodes.Count))
        {
            var node = _path.Nodes[_pathIndex];
            if (!_grid.HasTile(node.Position))
            {
                _pathIndex = 999;
            }
            else await MoveToCell(node);
        }
        if (_pathIndex >= _path.Nodes.Count - 1)
        {
            _path = null;
            _pathIndex = 1;
            return;
        }
        _pathIndex++;
    }

    private void UpdatePathToPlayer()
    {
        if (Vector3.Distance(playerMovement.transform.position, transform.position) > viewRange ||
            !los.HasLineOfSight(position, playerMovement._position))
        {
            sprite.color = Color.yellow;
            return;
        }
        Debug.Log(Vector3.Distance(playerMovement.transform.position, transform.position));
        Debug.Log(los.HasLineOfSight(position, playerMovement._position));
        var playerPos = playerMovement._position;
        _path = playerMovement._world.Pathfinding.FindPath(position.x, position.y, position.z,
            playerPos.x, playerPos.y, playerPos.z);
        _pathIndex = 1;
        sprite.color = Color.green;
    }

private Vector3Int WorldToCellPosition(Vector3 worldPos)
    {
        Vector3Int cellPos = Vector3Int.zero;
        for (int layer = _grid.Dimensions.z - 1; layer >= 0; layer--)
        {
            Tilemap map = playerMovement._world.Tilemaps[layer];
            cellPos = map.WorldToCell(worldPos);
        }
        return new Vector3Int(cellPos.x, cellPos.y, cellPos.z);
    }
}