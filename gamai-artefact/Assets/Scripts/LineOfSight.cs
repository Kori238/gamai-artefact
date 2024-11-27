using UnityEngine;

public class LineOfSight
{
    private readonly NodeGrid grid;

    public LineOfSight(NodeGrid grid)
    {
        this.grid = grid;
    }

    public bool HasLineOfSight(Vector3Int origin, Vector3Int destination)
    {
        Vector3 direction = (destination - origin);
        int distance = Mathf.RoundToInt(direction.magnitude);

        Vector3 stepIncrement = direction.normalized * 1f; // 1f or a smaller step for precision

        Vector3 currentPos = origin + stepIncrement; // Move one step ahead to not check origin cell

        for (int i = 0; i <= distance; i++)
        {
            Vector3 checkPos = Vector3Int.RoundToInt(currentPos);
            var currentZ = Mathf.Clamp((int)checkPos.z, 0, grid.Dimensions.z - 1); // Ensure we're within grid bounds
            Vector3Int gridPos = new Vector3Int(Mathf.RoundToInt(checkPos.x), Mathf.RoundToInt(checkPos.y), currentZ);

            // If this position is out of the grid's bounds, skip it but do not block LoS yet
            if (gridPos.x >= grid.Dimensions.x / 2 || gridPos.y >= grid.Dimensions.y / 2 ||
                gridPos.x < -grid.Dimensions.x / 2 || gridPos.y < -grid.Dimensions.y / 2)
            {
                currentPos += stepIncrement;
                continue;
            }

            // Check for tiles that might block line of sight, ensure z within bounds
            if (!IsWalkableOrEmpty(grid.GetNodeFromCell(gridPos.x, gridPos.y, currentZ).OccupiedBy, gridPos))
            {
                return false;
            }

            currentPos += stepIncrement;
        }

        return true;
    }

    // New method to check if a position is walkable or empty for Line of Sight
    private bool IsWalkableOrEmpty(NodeOccupiers occupier, Vector3Int position)
    {
        if (occupier != NodeOccupiers.None) return false; // Not empty

        var tile = grid.GetTile(position);

        // Assuming CustomTile has a property 'walkable', tweak this as necessary based on your CustomTile implementation
        return tile == null || (tile && tile.walkable);
    }
}