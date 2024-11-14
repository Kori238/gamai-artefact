using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridSetup : MonoBehaviour
{
    public NodeGrid Grid;
    public List<Tilemap> Tilemaps;
    public AStar Pathfinding;

    public void Awake()
    {
        Tilemaps = new List<Tilemap>(GetComponentsInChildren<Tilemap>());
        Grid = new NodeGrid(51, 51, Tilemaps);
        Pathfinding = new AStar(Grid);
    }
}
