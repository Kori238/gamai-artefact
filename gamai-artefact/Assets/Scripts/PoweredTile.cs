using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class PoweredTile : Wire
{
    [SerializeField] private CustomTile occupiedTile;
    [SerializeField] private bool enabledByDefault = false;
    // Start is called before the first frame update
    void Start()
    {
        _world = GameObject.Find("Grid").GetComponent<GridSetup>();
        var cell = _world.Tilemaps[_startingLayer].WorldToCell(transform.position - new Vector3(0, _startingLayer * 0.5f, 0));
        transform.position = _world.Tilemaps[_startingLayer].GetCellCenterWorld(cell) - new Vector3(0, 0.01f, 0);
        _position = new Vector3Int(cell.x, cell.y, _startingLayer);
        sortingGroup.sortingOrder = _startingLayer;
        occupiedTile = _world.Tilemaps[_startingLayer].GetTile<CustomTile>(cell);
        PowerDown();
    }

    public override void PowerUp()
    {
        base.PowerUp();
        _world.Tilemaps[_startingLayer].SetTile(new Vector3Int(_position.x, _position.y, 0),
            enabledByDefault ? null : occupiedTile);
    }

    public override void PowerDown()
    {
        base.PowerDown();
        _world.Tilemaps[_startingLayer].SetTile(new Vector3Int(_position.x, _position.y, 0),
            enabledByDefault ? occupiedTile : null);
    }
}
