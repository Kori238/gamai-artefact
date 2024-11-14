using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class Podium : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private GridSetup _world;
    [SerializeField] private Vector3Int _position;
    [SerializeField] private int _startingLayer;
    [SerializeField] private Orb _heldItem = null;
    [SerializeField] private SortingGroup sortingGroup;
    [SerializeField] private List<PowerLine> powerLines;
    public Node occupiedNode;

    void Start()
    {
        var cell = _world.Tilemaps[_startingLayer].WorldToCell(transform.position - new Vector3(0, _startingLayer * 0.5f, 0));
        transform.position = (Vector2)_world.Tilemaps[_startingLayer].GetCellCenterWorld(cell);
        _position = new Vector3Int(cell.x, cell.y, _startingLayer);
        sortingGroup.sortingOrder = _startingLayer + 1;
        occupiedNode = _world.Grid.GetNodeFromCell(cell.x, cell.y, _startingLayer);
        occupiedNode.OccupiedBy = NodeOccupiers.Podium;
        occupiedNode.Occupant = this;
        
    }

    public void UpdatePowerLines(PowerLine.WireColours colour, bool enable)
    {
        foreach (var powerLine in powerLines.Where(powerLine => powerLine.wireColor == colour))
        {
            Debug.Log(powerLine);
            if (enable) powerLine.Enable(); 
            else powerLine.Disable();
        }
    }


    public void SetItem(Orb orb)
    {
        _heldItem = orb;
        orb.transform.SetParent(this.transform);
        orb.transform.position = transform.position + new Vector3(0, 0.875f, 0);
    }

    public void Interact(Movement player)
    {
        if (player._heldItem == null && _heldItem != null)
        {
            player._heldItem = _heldItem;
            _heldItem.transform.SetParent(player.transform);
            _heldItem.transform.position = player.transform.position + new Vector3(0, 1, 0);
            _heldItem = null;
            UpdatePowerLines(player._heldItem.colour, false);
        }
        else if (player._heldItem != null && _heldItem == null)
        {
            SetItem(player._heldItem);
            player._heldItem = null;
            UpdatePowerLines(_heldItem.colour, true);
        }
    }
}
