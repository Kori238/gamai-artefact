using System;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
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

    public int delayTime = 10;
    public List<Vector3Int> roomOrigins;
    public List<float> roomOriginsDistances;
    public Vector3Int startRoomOrigin;
    public CustomTile pathTile;
    public Camera generationCamera;
    public Camera playerCamera;

    public int roomCount = 5;
    void Start()
    {
        Generate();
    }

    private async Task Generate()
    {
        playerCamera.enabled = false;
        generationCamera.enabled = true;
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
        await SpawnRoom(centerRoom, new Vector3Int(100, 100, 0));
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
                Vector3Int offset = new Vector3Int(rand.Next(-1, 2) * (int)Math.Pow(rand.Next(8), 2) + 100, rand.Next(-1, 2) * (int)Math.Pow(rand.Next(8), 2) + 100, 0);
                spawned = await SpawnRoom(roomToSpawn, offset);
            }
        }

        foreach (Tilemap tilemap in world.Tilemaps) // get rid of extra space tiles (used to prevent rooms from spawning in a certain space of each other in a customizable way)
        {
            for (int x = 0; x < 200; x++)
            {
                for (int y = 0; y < 200; y++)
                {
                    CustomTile tile = tilemap.GetTile<CustomTile>(new Vector3Int(x, y, 0));
                    if (tile != null && tile.name == "OccupiedSpace")
                    {
                        tilemap.SetTile(new Vector3Int(x, y, 0), null);
                        await Task.Delay(delayTime);
                    }
                }
            }
        }

        int max = roomOrigins.Count; //this would dynamically update so cannot be put inside the for loop
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
            await ConnectRooms(startRoomOrigin, roomOrigins[index]);
            roomOriginsDistances.Remove(roomOriginsDistances[index]);
            roomOrigins.Remove(roomOrigins[index]);
        }
        generationCamera.enabled = false;
        playerCamera.enabled = true;
    }

    public async Task ConnectRooms(Vector3Int originA, Vector3Int originB)
    {
        List<List<Vector3Int>> pathAreaToFill = new();
        List<List<Vector3Int>> pathAreaToClear = new();
        Debug.Log("test");
        Path path = world.Pathfinding.FindPath(originA.x, originA.y, originA.z, originB.x, originB.y, originB.z, true);
        if (path == null) return;
        bool previousNodeHasTile = false;
        // calculate path in both directions for more consistent fill
        List<Node> bothDirectionNodes = new List<Node>();
        foreach (Node n in path.Nodes)
        {
            bothDirectionNodes.Add(n);
        }

        path.Nodes.Reverse();
        foreach (Node n in path.Nodes)
        {
            bothDirectionNodes.Add(n);
        }

        foreach (Node n in bothDirectionNodes)
        {
            bool thisNodeHasTile = world.Grid.HasTile(n.Position);
            if (thisNodeHasTile && previousNodeHasTile)
            {
                previousNodeHasTile = thisNodeHasTile;
                continue;
            }

            previousNodeHasTile = thisNodeHasTile;
            pathAreaToFill.Add(new List<Vector3Int>
            {
                new Vector3Int(n.Position.x - 1, n.Position.y - 1, n.Position.z),
                new Vector3Int(n.Position.x + 1, n.Position.y + 1, n.Position.z),
            });
            pathAreaToClear.Add(new List<Vector3Int>
            {
                new Vector3Int(n.Position.x - 1, n.Position.y - 1, n.Position.z + 1),
                new Vector3Int(n.Position.x + 1, n.Position.y + 1, n.Position.z + 1),
            });
            pathAreaToClear.Add(new List<Vector3Int>
            {
                new Vector3Int(n.Position.x - 1, n.Position.y - 1, n.Position.z + 2),
                new Vector3Int(n.Position.x + 1, n.Position.y + 1, n.Position.z + 2),
            });
            //world.Grid.FillArea(new Vector3Int(n.Position.x-1, n.Position.y-1, n.Position.z), new Vector3Int(n.Position.x + 1, n.Position.y + 1, n.Position.z), pathTile);
        }

        foreach (List<Vector3Int> area in pathAreaToClear)
        {
            await Task.Delay(delayTime * 5);
            world.Grid.FillArea(area[0], area[1], null);
        }

        foreach (List<Vector3Int> area in pathAreaToFill)
        {
            await Task.Delay(delayTime * 5);
            world.Grid.FillArea(area[0], area[1], pathTile);
        }
    }

    private async Task<bool> SpawnRoom(Room room, Vector3Int positionOffset)
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
            await Task.Delay(delayTime);
        }
        roomOrigins.Add(positionOffset + room.roomOrigin);
        roomOriginsDistances.Add((positionOffset + room.roomOrigin - startRoomOrigin).magnitude);
        return true;
    }
}
