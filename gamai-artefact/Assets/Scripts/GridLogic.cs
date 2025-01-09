using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.VisualScripting;using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using static UnityEditor.Experimental.GraphView.GraphView;

[Serializable]
public class Node
{
    public int FCost, GCost, HCost;
    public readonly Vector3Int Position;
    public Node PreviousNode;
    public NodeOccupiers OccupiedBy = NodeOccupiers.None;
    public Podium Occupant;

    public Node(Vector3Int position)
    {
        Position = position;
    }

    public void UpdateFCost()
    {
        FCost = GCost + HCost;
    }
}

public enum NodeOccupiers
{
    None,
    Podium
}

public class NodeGrid
{
    public readonly Vector3Int Dimensions;
    public List<Tilemap> Tilemaps;
    public readonly Node[,,] Nodes;
    public Dictionary<string, CustomTile> TileDictionary = new();

    public NodeGrid(int width, int height, List<Tilemap> tilemaps)
    {
        CreateTileDictionary();
        var layers = tilemaps.Count;
        Dimensions = new(width, height, layers);
        Tilemaps = tilemaps;
        Nodes = new Node[width, height, layers];
        for (var z = 0; z < layers; z++)
        {
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var position = new Vector3Int(x - width / 2, y - height / 2, z);
                    Nodes[x, y, z] = new Node(position);
                }
            }
        }
    }
    public NodeGrid(WrappedGrid wrappedGrid)
    {
        CreateTileDictionary();
        GridSetup gridObject = GameObject.FindObjectOfType<GridSetup>();
        Tilemaps = gridObject.Tilemaps;
        foreach (KeyValuePair<Vector3Int, string> entry in wrappedGrid.Tilemaps)
        {
            Vector3Int pos = entry.Key;
            CustomTile tile = TileDictionary.GetValueOrDefault(entry.Value);
            Tilemaps[pos.z].SetTile(new Vector3Int(pos.x, pos.y, 0), tile);
        }
        Dimensions = new(gridObject.gridDimensions.x, gridObject.gridDimensions.y, Tilemaps.Count);
        Nodes = new Node[gridObject.gridDimensions.x, gridObject.gridDimensions.y, Tilemaps.Count];
        for (var z = 0; z < Tilemaps.Count; z++)
        {
            for (var x = 0; x < gridObject.gridDimensions.x; x++)
            {
                for (var y = 0; y < gridObject.gridDimensions.y; y++)
                {
                    var position = new Vector3Int(x - gridObject.gridDimensions.x / 2, y - gridObject.gridDimensions.y / 2, z);
                    Nodes[x, y, z] = new Node(position);
                }
            }
        }
    }

    private void CreateTileDictionary()
    {
        foreach (var asset in AssetDatabase.FindAssets("t:CustomTile"))
        {
            var tile = AssetDatabase.LoadAssetAtPath<CustomTile>(AssetDatabase.GUIDToAssetPath(asset));
            try
            {
                TileDictionary.Add(tile.name, tile);
            }
            catch (ArgumentException e)
            {
                Debug.LogError($"Tile could not be added to dictionary as it's name is duplicate {e.Message}, {e.StackTrace}");
            }
        }
    }

    public Dictionary<Vector3Int, string> WrapTilemaps()
    {
        Dictionary<Vector3Int, string> wrappedTilemaps = new();
        for (int z = 0; z < Tilemaps.Count; z++)
        {
            for (int x = 0; x < Dimensions.x; x++)
            {
                for (int y = 0; y < Dimensions.y; y++)
                {
                    Tilemap tilemap = Tilemaps[z];
                    Vector3Int position = new Vector3Int(x, y, z);
                    if (tilemap.HasTile(position))
                    {
                        CustomTile tile = tilemap.GetTile<CustomTile>(position);
                        wrappedTilemaps.Add(position, tile.name);
                    }
                }
            }
        }
        return wrappedTilemaps;
    }

    public void UnwrapTilemaps(Dictionary<Vector3Int, string> wrappedTilemaps)
    {
        foreach (KeyValuePair<Vector3Int, string> entry in wrappedTilemaps)
        {
            Vector3Int pos = entry.Key;
            CustomTile tile = TileDictionary.GetValueOrDefault(entry.Value);
            Tilemaps[pos.z].SetTile(new Vector3Int(pos.x, pos.y, 0), tile);
        }
    }

    public Node GetNodeFromCell(int x, int y, int z)
    {
        return Nodes[x + Dimensions.x / 2, y + Dimensions.y / 2, z];
    }

    public CustomTile GetTile(Vector3Int position)
    {
        return Tilemaps[position.z].GetTile<CustomTile>(new Vector3Int(position.x, position.y, 0));
    }

    public bool HasTile(Vector3Int position)
    {
        return Tilemaps[position.z].HasTile(new Vector3Int(position.x, position.y, 0));
    }

    public Vector3 GetCenter(Vector3Int position)
    {
        return Tilemaps[position.z].GetCellCenterWorld(new Vector3Int(position.x, position.y, 0));
    }

    public bool CheckTileValid(Vector3Int position)
    {
        return HasTile(position) && !HasTile(new Vector3Int(position.x, position.y, position.z + 1));
    }

    public void SetTile(Vector3Int position, CustomTile tile)
    {
        Tilemaps[position.z].SetTile(new Vector3Int(position.x, position.y, 0), tile);
    }

    public void FillArea(Vector3Int pos1, Vector3Int pos2, CustomTile tile)
    {
        //tile = Tilemaps[0].GetTile<CustomTile>(new Vector3Int(0, 0, 0));
        for (int x = pos1.x; x < pos2.x; x++)
        {
            for (int y = pos1.y; y < pos2.y; y++)
            {
                SetTile(new Vector3Int(x, y, pos1.z), tile);
                Debug.Log($"{x}, {y}, {tile}");
                
            }
        }
        Debug.Log(Tilemaps[0].GetTile<CustomTile>(new Vector3Int(0, 0, 0)));
    }
}


public class WrappedGrid
{
    public readonly Vector3Int Dimensions;
    public Dictionary<Vector3Int, string> Tilemaps;

    public WrappedGrid(NodeGrid grid)
    {
        Tilemaps = grid.WrapTilemaps();
        Dimensions = grid.Dimensions;
    }

    [JsonConstructor]
    public WrappedGrid(Vector3Int dimensions, Dictionary<Vector3Int, string> tilemaps)
    {
        Tilemaps = tilemaps;
        Dimensions = dimensions;
    }
}

public class Path
{
    public int FCost, Cost;
    public List<Node> Nodes;

    public Path()
    {
        FCost = 0;
        Cost = 0;
        Nodes = new List<Node>();
    }
}


public class AStar
{
    private const int DIAGONAL_COST = 14;
    private const int STRAIGHT_COST = 10;
    private const int LAYER_COST = 24;

    private readonly NodeGrid _grid;
    private List<Node> _searchedNodes;
    private List<Node> _unsearchedNodes;

    public AStar(NodeGrid grid)
    {
        _grid = grid;
    }

    public NodeGrid GetGrid()
    {
        return _grid; 
    }

    public Path FindPath(int x0, int y0, int z0, int x1, int y1, int z1)
    {
        var grid = GetGrid();
        var startNode = grid.GetNodeFromCell(x0, y0, z0);
        var endNode = grid.GetNodeFromCell(x1, y1, z1);

        _unsearchedNodes = new List<Node> { startNode };
        _searchedNodes = new List<Node>();
        for (var z = 0; z < grid.Dimensions.z; z++)
        {
            for (var x = -(grid.Dimensions.x / 2); x < grid.Dimensions.x / 2; x++)
            {
                for (var y = -(grid.Dimensions.y / 2); y < grid.Dimensions.y / 2; y++)
                {
                    var node = grid.GetNodeFromCell(x, y, z);
                    node.GCost = int.MaxValue;
                    node.UpdateFCost();
                    node.PreviousNode = null;
                }
            }
        }
        var i = 0;
        while (_unsearchedNodes.Count > 0 && i < 1000)
        {
            i++;
            var currentNode = FindLowestFCostNode(_unsearchedNodes);

            if (currentNode == endNode)
            {
                if (endNode.OccupiedBy == NodeOccupiers.Podium)
                {
                    return CalculatePath(endNode.PreviousNode);
                }

                return CalculatePath(endNode);
            }
            _unsearchedNodes.Remove(currentNode);
            _searchedNodes.Add(currentNode);
            var currentTile = _grid.GetTile(currentNode.Position);
            var hasTile = _grid.HasTile(currentNode.Position);

            if (!hasTile) continue;

            List<Node> adjacents = new();
            FindAdjacents(currentNode.Position, ref adjacents);
            if(currentNode.Position.z >= 1 && currentTile.layerTraversal) //if the current tile is stairs and not the bottom layer then add adjacents below this tile 
                FindAdjacents(new Vector3Int(currentNode.Position.x, currentNode.Position.y, currentNode.Position.z-1), ref adjacents);
            if(currentNode.Position.z < _grid.Dimensions.z) //if this is not the top layer, check for stairways to the layer above
                FindAdjacents(new Vector3Int(currentNode.Position.x, currentNode.Position.y, currentNode.Position.z+1), ref adjacents, true);

            foreach (var adjacentNode in adjacents)
            {
                var adjacentTile =_grid.GetTile(adjacentNode.Position);
                if (_searchedNodes.Contains(adjacentNode) ||
                    !_grid.CheckTileValid(adjacentNode.Position) ||
                    (adjacentNode.OccupiedBy != NodeOccupiers.None && adjacentNode != endNode)) continue;
                if (!adjacentTile.walkable) 
                { 
                    _searchedNodes.Add(adjacentNode);
                    continue;
                }
                var tentativeGCost = currentNode.GCost + CalculateDistanceCost(currentNode, adjacentNode);
                if (tentativeGCost < adjacentNode.GCost)
                {
                    adjacentNode.PreviousNode = currentNode; 
                    adjacentNode.GCost = tentativeGCost; 
                    adjacentNode.HCost = CalculateDistanceCost(adjacentNode, endNode); 
                    adjacentNode.UpdateFCost();
                    
                    if (!_unsearchedNodes.Contains(adjacentNode)) 
                    { 
                        _unsearchedNodes.Add(adjacentNode); 
                    }
                }
            }
        }
        return null;
    }

    private static Path CalculatePath(Node endNode)
    {
        var path = new Path { FCost = endNode.FCost };
        path.Nodes.Add(endNode);
        var currentNode = endNode;
        while (currentNode.PreviousNode != null)
        {
            path.Nodes.Add(currentNode.PreviousNode);
            currentNode = currentNode.PreviousNode;
        }
        path.Nodes.Reverse();
        return path;
    }

    private static Node FindLowestFCostNode(List<Node> nodeList)
    {
        var lowestFCostNode = nodeList[0];
        foreach (var node in nodeList)
        {
            if (node.FCost < lowestFCostNode.FCost)
            {
                lowestFCostNode = node;
            }
        }
        return lowestFCostNode;
    }

    private void FindAdjacents(Vector3Int position, ref List<Node> adjacents, bool checkStairs = false)
    {
        var directions = new List<Vector2Int> {
            new Vector2Int(position.x - 1, position.y), new Vector2Int(position.x + 1, position.y), //cardinals:
            new Vector2Int(position.x, position.y - 1), new Vector2Int(position.x, position.y + 1),

            new Vector2Int(position.x - 1, position.y - 1), new Vector2Int(position.x - 1, position.y + 1), //diagonals:
            new Vector2Int(position.x + 1, position.y - 1), new Vector2Int(position.x + 1, position.y + 1),
        };

        foreach (var direction in directions)
        {
            if (!(
                direction.x > _grid.Dimensions.x/2 || direction.x < -_grid.Dimensions.x/2 || //boundaries check
                direction.y > _grid.Dimensions.y/2 || direction.y < -_grid.Dimensions.y/2) &&
                _grid.CheckTileValid(new Vector3Int(direction.x, direction.y, position.z)) &&
                (!checkStairs || _grid.GetTile(new Vector3Int(direction.x, direction.y, position.z)).layerTraversal)
                )
                adjacents.Add(_grid.GetNodeFromCell(direction.x, direction.y, position.z));
        }
    }

    public int CalculateDistanceCost(Node a, Node b)
    {
        var xDistance = Mathf.Abs(a.Position.x - b.Position.x);
        var yDistance = Mathf.Abs(a.Position.y - b.Position.y);
        var zDistance = Mathf.Abs(a.Position.z - b.Position.z);
        var remaining = Mathf.Abs(xDistance - yDistance);

        return DIAGONAL_COST * Mathf.Min(xDistance, yDistance) + STRAIGHT_COST * remaining + LAYER_COST * zDistance;
    }
}