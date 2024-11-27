using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
    [SerializeField] private Movement playerMovement;
    private NodeGrid _grid;
    private Path _path;
    private int _pathIndex = 0;
    [SerializeField] private int movementSpeed = 10;

    private LineOfSight _lineOfSight;
    private Vector3Int _lastKnownPlayerPosition;
    private bool _hasLastKnownPosition = false;

    private Vector3Int lastKnownPlayerPosition;
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

        // Initialize Line of Sight
        los = new LineOfSight(_grid);

        // Set an initial last known position to the player's start
        lastKnownPlayerPosition = playerMovement._position;
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
        UpdateAndMove();
    }

    private async void UpdatePathToLastKnownPosition()
    {
        if (lastKnownPlayerPosition == Vector3Int.zero) return; // Safety check

        Vector3Int currentPos = WorldToCellPosition(transform.position);
        Node currentNode = _grid.GetNodeFromCell(currentPos.x, currentPos.y, currentPos.z);

        _path = playerMovement._world.Pathfinding.FindPath(currentPos.x, currentPos.y, currentPos.z, lastKnownPlayerPosition.x, lastKnownPlayerPosition.y, lastKnownPlayerPosition.z);
        _pathIndex = 1;

        if (_path != null && _path.Nodes.Count > 1)
        {
            await FollowPlayerPath();
            if (Vector3Int.Equals(lastKnownPlayerPosition, _path.Nodes.Last().Position))
            {
                MoveRandomly();
            }
        }
        else
        {
            MoveRandomly();
        }
    }

    private void MoveRandomly()
    {
        List<Vector3Int> directions = new List<Vector3Int>
        {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
            // Add more directions if you want diagonal movement or layer changes
        };

        Vector3Int currentPos = WorldToCellPosition(transform.position);
        var currentZ = currentPos.z; // Keep current layer for random move

        foreach (var direction in directions)
        {
            Vector3Int targetPos = currentPos + direction;
            // Ensure we stay on the current layer
            Node node = _grid.GetNodeFromCell(targetPos.x, targetPos.y, currentZ);

            if (node != null && node.OccupiedBy == NodeOccupiers.None) // Move only if empty
            {
                StartCoroutine(MoveToCell(node));
                return;
            }
        }
    }

    private async Task FollowPlayerPath()
    {
        // ... Existing logic but with LOS check ...
        // Before moving, update last known position if player in sight
        if (los.HasLineOfSight(WorldToCellPosition(transform.position), playerMovement._position))
        {
            lastKnownPlayerPosition = playerMovement._position;
        }

        // Current movement code here
    }

    // ... existing WorldToCellPosition function ...

    private IEnumerator MoveToCell(Node node)
    {
        playerMovement.enemiesMoving++;
        Vector3 target = _grid.GetCenter(node.Position);
        Vector3 direction = target - transform.position;
        float distanceThisFrame = Time.deltaTime * movementSpeed;

        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            float moved = Mathf.Min(distanceThisFrame, Vector3.Distance(transform.position, target));
            transform.position += (direction.normalized * moved);
            yield return null;
        }

        playerMovement.enemiesMoving--;
    }

    private void UpdateAndMove()
    {
        if (los.HasLineOfSight(WorldToCellPosition(transform.position), playerMovement._position))
        {
            lastKnownPlayerPosition = playerMovement._position;
            UpdatePathToPlayer();
        }
        else
        {
            UpdatePathToLastKnownPosition();
        }
    }

    private void UpdatePathToPlayer()
    {
        var playerPos = playerMovement._position;
        Vector3Int currentPos = WorldToCellPosition(transform.position);
        _path = playerMovement._world.Pathfinding.FindPath(currentPos.x, currentPos.y, currentPos.z,
            playerPos.x, playerPos.y, playerPos.z);
        _pathIndex = 1; // Start from the second index

        if (_path != null && _path.Nodes.Count > 1)
            FollowPlayerPath().ConfigureAwait(false);
    }

private Vector3Int WorldToCellPosition(Vector3 worldPos)
    {
        int z = playerMovement._position.z; // Assuming same layer as player for simplicity
        Vector3Int cellPos = Vector3Int.zero;
        for (int layer = _grid.Dimensions.z - 1; layer >= 0; layer--)
        {
            Tilemap map = playerMovement._world.Tilemaps[layer];
            cellPos = map.WorldToCell(worldPos);
            if (map.HasTile(cellPos))
            {
                z = layer;
                break;
            }
        }
        return new Vector3Int(cellPos.x, cellPos.y, z);
    }
}