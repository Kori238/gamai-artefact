using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class CustomTile : Tile
{
    public bool layerTraversal = false;
    public bool walkable = true;
    public bool goal = false;
}