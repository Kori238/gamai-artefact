using System;using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = System.Random;
[ExecuteAlways]

public class GridSetup : MonoBehaviour
{
    public NodeGrid Grid;
    public List<Tilemap> Tilemaps;
    public AStar Pathfinding;
    public CustomTile tile;
    public List<List<Area>> areas = new();
    [SerializeField] public Vector2Int gridDimensions = new Vector2Int(51, 51);

    
    public void Awake()
    {
        Tilemaps = new List<Tilemap>(GetComponentsInChildren<Tilemap>());
        Grid = new NodeGrid(gridDimensions.x, gridDimensions.y, Tilemaps);
        Pathfinding = new AStar(Grid);
    }

    public void Generate()
    {
        //Logically split grid into 16 quadrants
        Vector2Int quadrantLength = gridDimensions / 4;
        Debug.Log(quadrantLength);
        for (int y = 1; y < 5; y++)
        {
            List<Area> area_row = new List<Area>();
            for (int x = 1; x < 5; x++)
            {
                Vector2Int topRight = new Vector2Int(quadrantLength.x * x, quadrantLength.y * y);
                Vector2Int bottomLeft = new Vector2Int(quadrantLength.x * (x - 1), quadrantLength.y * (y - 1));
                area_row.Add(new Area(topRight, bottomLeft));
                Debug.Log($"{bottomLeft}, {topRight}");
            }
            areas.Add(area_row);
        }

        /*foreach (List<Area> row in areas)
        {
            foreach (Area a in row)
            {
                Debug.Log($"{a.bottomLeft}, {a.topRight}");
                Grid.FillArea(new Vector3Int(a.bottomLeft.x + 1, a.bottomLeft.y + 1, 0),
                    new Vector3Int(a.topRight.x - 1, a.topRight.y - 1, 0), tile);
            }
        }*/
        Random rand = new Random();
        for (int i = 0; i < areas.Count / 4; i++)
        {
        }
    }

    
}

public enum RoomTypes
{
    Default,
    IShape,
    LongIShape,
    Large,
    Overwritten
}
public class Area
{
    public Vector2Int topRight;
    public Vector2Int bottomLeft;
    public RoomTypes roomType = RoomTypes.Default;
    public Area(Vector2Int topRight, Vector2Int bottomLeft)
    {
        this.topRight = topRight;
        this.bottomLeft = bottomLeft;
    }
}