using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orb : MonoBehaviour
{
    [SerializeField] private Podium podium;

    [SerializeField] public PowerLine.WireColours colour;
    void Start()
    {
        if (podium != null)
        {
            podium.SetItem(this);
            podium.UpdatePowerLines(colour, true);
        }
    }
}
