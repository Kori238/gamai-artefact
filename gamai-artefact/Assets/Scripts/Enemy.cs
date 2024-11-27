using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
    [SerializeField] private Movement playerMovement; // Serialize for ease of setting up, but not directly linked in inspector due to singleton-like setup
    private NodeGrid _grid;
    private Path _path;
    private int _pathIndex = 0;
    [SerializeField] private int movementSpeed = 10;

    private void Start()
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

        // Assuming GridSetup is on the same GameObject as Tilemaps
        _grid = playerMovement._world.Grid;
        playerMovement.PlayerMoved += OnPlayerMoved;
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
        UpdatePath();
    }

    private async Task FollowPlayerPath()
    {
        if (_path != null && _path.Nodes.Count > 1)
        {
            var nextNode = _path.Nodes[_pathIndex > _path.Nodes.Count - 1 ? _path.Nodes.Count - 1 : _pathIndex];
            await MoveToCell(nextNode);
            _pathIndex++;

            // Reset path index if we've reached the end
            if (_pathIndex >= _path.Nodes.Count)
            {
                _pathIndex = 0;
                _path = null;
            }
        }
        else
        {
            _pathIndex = 0;
        }
    }

    private void UpdatePath()
    {
        Vector3Int currentPos = WorldToCellPosition(transform.position);
        Node currentNode = _grid.GetNodeFromCell(currentPos.x, currentPos.y, currentPos.z);

        // Assuming the player's position or path's start/end node gives us position
        var playerNode = _grid.GetNodeFromCell(playerMovement._position.x, playerMovement._position.y, playerMovement._position.z);

        // Find path to player
        _path = playerMovement._world.Pathfinding.FindPath(currentPos.x, currentPos.y, currentPos.z,
            playerNode.Position.x, playerNode.Position.y, playerNode.Position.z);

        _pathIndex = 1; // Start from the second index since it's where we want to move next

        if (_path != null && _path.Nodes.Count > 1)
            FollowPlayerPath();
    }

    private async Task MoveToCell(Node node)
    {
        Vector3 target = _grid.GetCenter(node.Position);
        Vector2 direction = target - transform.position;

        while (Vector2.Distance(transform.position, target) > 0.05f)
        {
            direction = target - transform.position;
            transform.position += (Vector3)(movementSpeed * Time.deltaTime * direction.normalized);
            await Task.Yield();
        }

        // Here you could add animation logic later if needed
    }

    private Vector3Int WorldToCellPosition(Vector3 worldPos)
    {
        int z = 0; // Assuming start at layer 0, adjust if needed based on your layer logic
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