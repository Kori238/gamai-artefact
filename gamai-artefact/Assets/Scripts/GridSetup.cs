using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
[ExecuteAlways]

public class GridSetup : MonoBehaviour
{
    public NodeGrid Grid;
    public List<Tilemap> Tilemaps;
    public AStar Pathfinding;
    public CustomTile tile;
    [SerializeField] public Vector2Int gridDimensions = new Vector2Int(51, 51);

    
    public void Awake()
    {
        Tilemaps = new List<Tilemap>(GetComponentsInChildren<Tilemap>());
        Grid = new NodeGrid(gridDimensions.x, gridDimensions.y, Tilemaps);
        Pathfinding = new AStar(Grid);
    }
}