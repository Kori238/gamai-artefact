using System;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml.Schema;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = System.Random;
using static UnityEngine.UI.Image;
using Vector3 = UnityEngine.Vector3;

public class ProceduralGeneration : MonoBehaviour
{
    // Start is called before the first frame update
    private GridSetup world;
    private Dictionary<RoomTypes, List<Room>> allRooms = new();
    [SerializeField] private RoomTypes roomTypeReference; //To see the order of rooms for defining roomChances;
    [Tooltip("Cumulative percentage, maps to above enum, refer to above field to see room order. Must total 100")]
    public List<int> roomChances;

    public List<Vector3Int> roomOrigins;
    public List<float> roomOriginsDistances;
    public Vector3Int startRoomOrigin;
    public CustomTile pathTile;

    public int roomCount = 5;
    void Start()
    {
        // Initialize room lists, create possible asset paths where rooms are stored for the find assets function
        List<string> roomsPath = new();
        foreach (var type in Enum.GetValues(typeof(RoomTypes))) 
        {
            Debug.Log(type.ToString());
            roomsPath.Add("Assets/Rooms/" + type);
            Enum.TryParse(type.ToString(), out RoomTypes enumType);
            allRooms.Add(enumType, new List<Room>());
        }
        // Find all assets that are a text asset in the rooms directories and add these rooms to the allRooms dict
        foreach (var asset in AssetDatabase.FindAssets("t:TextAsset", roomsPath.ToArray()))
        {
            string path = AssetDatabase.GUIDToAssetPath(asset);
            Room room = JsonConvert.DeserializeObject<Room>(File.ReadAllText(path));
            allRooms.GetValueOrDefault(room.roomType).Add(room);
        }

        world = GameObject.FindObjectOfType<GridSetup>();
        foreach (Tilemap tilemap in world.Tilemaps)
        {
            tilemap.ClearAllTiles();
        }

        Random rand = new Random();
        RoomTypes roomToSpawnType = new();
        Room centerRoom = allRooms.GetValueOrDefault(RoomTypes.Small)[0];
        SpawnRoom(centerRoom, new Vector3Int(100, 100, 0));
        startRoomOrigin = new Vector3Int(106, 106, 0);
        for (int i = 0; i < roomCount; i++)
        {
            int percentChoice = rand.Next(100);
            Debug.Log(percentChoice);
            for (int j = 0; j < roomChances.Count; j++)
            {
                if (percentChoice > roomChances[j])
                {
                    continue;
                }

                roomToSpawnType = (RoomTypes)j;
                break;
            }

            List<Room> roomsList = allRooms.GetValueOrDefault(roomToSpawnType);
            Room roomToSpawn = roomsList[rand.Next(roomsList.Count)];
            bool spawned = false;
            int attempts = 0;
            while (!spawned && attempts < 50)
            {
                attempts++;
                int sign = rand.Next(-1, 2);
                Vector3Int offset = new Vector3Int(rand.Next(-1, 2)*(int)Math.Pow(rand.Next(8),2) + 100, rand.Next(-1, 2)*(int)Math.Pow(rand.Next(8),2) + 100, 0);
                spawned = SpawnRoom(roomToSpawn, offset);
            }
        }

        foreach (Tilemap tilemap in world.Tilemaps)
        {
            for (int x = 0; x < 200; x++)
            {
                for (int y = 0; y < 200; y++)
                {
                    CustomTile tile = tilemap.GetTile<CustomTile>(new Vector3Int(x, y, 0));
                    if (tile != null && tile.name == "OccupiedSpace") tilemap.SetTile(new Vector3Int(x, y, 0), null);
                }
            }
        }


        int max = roomOrigins.Count;
        for (int i = 0; i < max; i++)
        {
            float lowestValue = float.MaxValue;
            int index = 0;
            foreach (float value in roomOriginsDistances)
            {
                if (value < lowestValue)
                {
                    lowestValue = value;
                    index = roomOriginsDistances.IndexOf(value);
                }
            }
            ConnectRooms(roomOrigins[index], startRoomOrigin);
            roomOriginsDistances.Remove(roomOriginsDistances[index]);
            roomOrigins.Remove(roomOrigins[index]);
        }
    }

    public void ConnectRooms(Vector3Int originA, Vector3Int originB)
    {
        List<List<Vector3Int>> pathAreaToFill = new();
        Debug.Log("test");
        Path path = world.Pathfinding.FindPath(originA.x, originA.y, originA.z, originB.x, originB.y, originB.z, true);
        if (path == null) return;
        foreach (Node n in path.Nodes)
        {
            if (world.Grid.HasTile(n.Position)) continue;
            pathAreaToFill.Add(new List<Vector3Int>{new Vector3Int(n.Position.x - 1, n.Position.y - 1, n.Position.z), new Vector3Int(n.Position.x + 1, n.Position.y + 1, n.Position.z), });
            //world.Grid.FillArea(new Vector3Int(n.Position.x-1, n.Position.y-1, n.Position.z), new Vector3Int(n.Position.x + 1, n.Position.y + 1, n.Position.z), pathTile);
        }
        foreach(List<Vector3Int> area in pathAreaToFill)
        {
            world.Grid.FillArea(area[0], area[1], pathTile);
        }
    }

    private bool SpawnRoom(Room room, Vector3Int positionOffset)
    {
        TileDictionary tileDictionary = new TileDictionary();
        bool valid = true;
        foreach (KeyValuePair<Vector3Int, string> entry in room.wrappedTilemaps.wrappedTilemaps)
        {
            Vector3Int pos = entry.Key + positionOffset;
            CustomTile tile = world.Tilemaps[pos.z].GetTile<CustomTile>(new Vector3Int(pos.x, pos.y, 0));
            if (tile != null)
            {
                valid = false;
                break;
            }
        }
        if (!valid) return false;
        foreach (KeyValuePair<Vector3Int, string> entry in room.wrappedTilemaps.wrappedTilemaps)
        {
            Vector3Int pos = entry.Key + positionOffset;
            CustomTile tile = tileDictionary.dict.GetValueOrDefault(entry.Value);
            world.Tilemaps[pos.z].SetTile(new Vector3Int(pos.x, pos.y, 0), tile);
        }
        roomOrigins.Add(positionOffset + room.roomOrigin);
        roomOriginsDistances.Add((positionOffset + room.roomOrigin - startRoomOrigin).magnitude);
        return true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
