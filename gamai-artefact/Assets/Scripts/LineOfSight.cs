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
        origin.z += 1;
        destination.z += 1;
        Vector3 direction = (destination - origin);
        int distance = Mathf.RoundToInt(direction.magnitude);

        Vector3 stepIncrement = direction.normalized;
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

            if (IsObstruction(gridPos))
            {
                return false;
            }

            currentPos += stepIncrement;
        }

        return true;
    }

    private bool IsObstruction(Vector3Int position)
    {
        var tile = grid.GetTile(position);
        return tile != null;
    }
}