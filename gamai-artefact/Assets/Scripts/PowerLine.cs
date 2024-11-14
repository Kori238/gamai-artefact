using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PowerLine : MonoBehaviour
{
    [SerializeField] private List<Wire> wires;
    [SerializeField] public WireColours wireColor;

    void Awake()
    {
        wires = new List<Wire>(transform.GetChild(0).GetComponentsInChildren<Wire>());
    }

    public enum WireColours
    {
        Red,
        Purple,
        Blue,
        Orange,
        Empty
    }
    public async void Enable()
    {
        foreach (var wire in wires)
        {
            wire.PowerUp();
            await Task.Delay(50);
        }
    }

    public async void Disable()
    {
        foreach (var wire in wires)
        {
            wire.PowerDown();
            await Task.Delay(50);
        }
    }
}


