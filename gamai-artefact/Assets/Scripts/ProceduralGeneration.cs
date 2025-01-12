using System;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Xml.Schema;
using TMPro;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;
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
    public CustomTile pathStairTileY;
    public CustomTile pathStairTileX;
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
        await SpawnRoom(centerRoom, new Vector3Int(100, 100, 3), false);
        startRoomOrigin = new Vector3Int(106, 106, 3);
        for (int i = 0; i < roomCount; i++)
        {
            int percentChoice = rand.Next(100);
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
                int xPos = rand.Next(-1, 2) * (int)Math.Pow(rand.Next(8), 2) + 100;
                int yPos = rand.Next(-1, 2) * (int)Math.Pow(rand.Next(8), 2) + 100;
                int zPos = rand.Next(-3,1) + (int)((xPos + yPos) / 40);
                zPos = Mathf.Min(5, zPos);
                zPos = Mathf.Max(0, zPos);
                Debug.Log($"{zPos}, {xPos + yPos}");
                Vector3Int offset = new Vector3Int(xPos, yPos, zPos);
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
            //await ConnectRooms(startRoomOrigin, roomOrigins[index]);
            await ConnectRooms(roomOrigins[index], startRoomOrigin);
            roomOriginsDistances.Remove(roomOriginsDistances[index]);
            roomOrigins.Remove(roomOrigins[index]);
        }
        generationCamera.enabled = false;
        playerCamera.enabled = true;
    }

    public async Task ConnectRooms(Vector3Int originA, Vector3Int originB)
    {
        List<Vector3Int> tilesToFill = new();
        List<Vector3Int> tilesToClear = new();
        List<Vector3Int> tilesToFillStairsY = new();
        List<Vector3Int> tilesToFillStairsX = new();
        Path path = world.Pathfinding.FindPath(originA.x, originA.y, originA.z, originB.x, originB.y, originB.z, true);
        if (path == null)
        {
            Debug.Log($"Path could not be found between A: {originA}, B: {originB}");
            return;
        }

        Node previousNode = path.Nodes[0];
        Node nextNode = path.Nodes[1];

        for (int i = 0; i < path.Nodes.Count; i++)
        {
            Node n = path.Nodes[i];
            if (i < path.Nodes.Count - 1) nextNode = path.Nodes[i + 1];
            if (i > 0) previousNode = path.Nodes[i - 1];
            if (world.Grid.HasTile(n.Position) && world.Grid.HasTile(nextNode.Position) && world.Grid.HasTile(previousNode.Position)) continue;
            Vector3Int nodePositionDelta = n.Position - previousNode.Position;
            bool fillingStairs = false;
            if (previousNode.Position.z < n.Position.z || nextNode.Position.z < n.Position.z)
            {
                if (nodePositionDelta.x != 0 && nodePositionDelta.y != 0) tilesToFill.Add(new Vector3Int(n.Position.x, n.Position.y, n.Position.z));
                else
                {
                    if (nodePositionDelta.x != 0) tilesToFillStairsY.Add(new Vector3Int(n.Position.x, n.Position.y, n.Position.z));
                    else tilesToFillStairsX.Add(new Vector3Int(n.Position.x, n.Position.y, n.Position.z));
                    tilesToFill.Add(new Vector3Int(n.Position.x, n.Position.y, n.Position.z - 1));
                }
                fillingStairs = true;
            }
            else
                tilesToFill.Add(new Vector3Int(n.Position.x, n.Position.y, n.Position.z));
            tilesToClear.Add(new Vector3Int(n.Position.x, n.Position.y, n.Position.z + 1));
            tilesToClear.Add(new Vector3Int(n.Position.x, n.Position.y, n.Position.z + 2));

            if (nodePositionDelta.x != 0 && nodePositionDelta.y != 0) // Path is moving diagonally
            {
                if (fillingStairs)
                {
                    tilesToFillStairsX.Add(
                        new Vector3Int(n.Position.x + nodePositionDelta.x, n.Position.y, n.Position.z));
                    tilesToFillStairsY.Add(
                        new Vector3Int(n.Position.x, n.Position.y + nodePositionDelta.y, n.Position.z));
                    tilesToFill.Add(
                        new Vector3Int(n.Position.x + nodePositionDelta.x, n.Position.y, n.Position.z-1));
                    tilesToFill.Add(
                        new Vector3Int(n.Position.x, n.Position.y + nodePositionDelta.y, n.Position.z-1));
                }
                else
                {
                    tilesToFill.Add(new Vector3Int(n.Position.x + nodePositionDelta.x, n.Position.y, n.Position.z));
                    tilesToFill.Add(new Vector3Int(n.Position.x, n.Position.y + nodePositionDelta.y, n.Position.z));
                    tilesToFill.Add(new Vector3Int(n.Position.x - nodePositionDelta.x, n.Position.y, n.Position.z));
                    tilesToFill.Add(new Vector3Int(n.Position.x, n.Position.y - nodePositionDelta.y, n.Position.z));
                }

                tilesToClear.Add(new Vector3Int(n.Position.x + nodePositionDelta.x, n.Position.y, n.Position.z + 1));
                tilesToClear.Add(new Vector3Int(n.Position.x, n.Position.y + nodePositionDelta.y, n.Position.z + 1));
                tilesToClear.Add(new Vector3Int(n.Position.x + nodePositionDelta.x, n.Position.y, n.Position.z + 2));
                tilesToClear.Add(new Vector3Int(n.Position.x, n.Position.y + nodePositionDelta.y, n.Position.z + 2));
                tilesToClear.Add(new Vector3Int(n.Position.x - nodePositionDelta.x, n.Position.y, n.Position.z + 1));
                tilesToClear.Add(new Vector3Int(n.Position.x, n.Position.y - nodePositionDelta.y, n.Position.z + 1));
                tilesToClear.Add(new Vector3Int(n.Position.x - nodePositionDelta.x, n.Position.y, n.Position.z + 2));
                tilesToClear.Add(new Vector3Int(n.Position.x, n.Position.y - nodePositionDelta.y, n.Position.z + 2));
            }

            else if (nodePositionDelta.x != 0) // Path is moving in the x-axis
            {
                if (fillingStairs)
                {
                    tilesToFillStairsY.Add(new Vector3Int(n.Position.x, n.Position.y - nodePositionDelta.x, n.Position.z));
                    tilesToFillStairsY.Add(new Vector3Int(n.Position.x, n.Position.y + nodePositionDelta.x, n.Position.z));
                    tilesToFill.Add(new Vector3Int(n.Position.x, n.Position.y - nodePositionDelta.x, n.Position.z - 1));
                    tilesToFill.Add(new Vector3Int(n.Position.x, n.Position.y + nodePositionDelta.x, n.Position.z - 1));
                }
                else
                {
                    tilesToFill.Add(new Vector3Int(n.Position.x, n.Position.y - nodePositionDelta.x, n.Position.z));
                    tilesToFill.Add(new Vector3Int(n.Position.x, n.Position.y + nodePositionDelta.x, n.Position.z));
                }
                tilesToClear.Add(new Vector3Int(n.Position.x, n.Position.y - nodePositionDelta.x, n.Position.z + 1));
                tilesToClear.Add(new Vector3Int(n.Position.x, n.Position.y + nodePositionDelta.x, n.Position.z + 1));
                tilesToClear.Add(new Vector3Int(n.Position.x, n.Position.y - nodePositionDelta.x, n.Position.z + 2));
                tilesToClear.Add(new Vector3Int(n.Position.x, n.Position.y + nodePositionDelta.x, n.Position.z + 2));
            }

            else if (nodePositionDelta.y != 0) // Path is moving in the y-axis
            {
                if (fillingStairs)
                {
                    tilesToFillStairsX.Add(new Vector3Int(n.Position.x - nodePositionDelta.y, n.Position.y, n.Position.z));
                    tilesToFillStairsX.Add(new Vector3Int(n.Position.x + nodePositionDelta.y, n.Position.y, n.Position.z));
                    tilesToFill.Add(new Vector3Int(n.Position.x - nodePositionDelta.y, n.Position.y, n.Position.z - 1));
                    tilesToFill.Add(new Vector3Int(n.Position.x + nodePositionDelta.y, n.Position.y, n.Position.z - 1));
                }
                else
                {
                    tilesToFill.Add(new Vector3Int(n.Position.x - nodePositionDelta.y, n.Position.y, n.Position.z));
                    tilesToFill.Add(new Vector3Int(n.Position.x + nodePositionDelta.y, n.Position.y, n.Position.z));
                }
                tilesToClear.Add(new Vector3Int(n.Position.x - nodePositionDelta.y, n.Position.y, n.Position.z + 1));
                tilesToClear.Add(new Vector3Int(n.Position.x + nodePositionDelta.y, n.Position.y, n.Position.z + 1));
                tilesToClear.Add(new Vector3Int(n.Position.x - nodePositionDelta.y, n.Position.y, n.Position.z + 2));
                tilesToClear.Add(new Vector3Int(n.Position.x + nodePositionDelta.y, n.Position.y, n.Position.z + 2));
            }
        }

        foreach (Vector3Int position in tilesToClear)
        {
            await Task.Delay(delayTime * 3);
            world.Grid.SetTile(position, null);
        }
        foreach (Vector3Int position in tilesToFill)
        {
            await Task.Delay(delayTime * 10);
            world.Grid.SetTile(position, pathTile);
        }
        foreach (Vector3Int position in tilesToFillStairsY)
        {
            await Task.Delay(delayTime * 10);
            world.Grid.SetTile(position, pathStairTileY);
        }
        foreach (Vector3Int position in tilesToFillStairsX)
        {
            await Task.Delay(delayTime * 10);
            world.Grid.SetTile(position, pathStairTileX);
        }
    }

    private async Task<bool> SpawnRoom(Room room, Vector3Int positionOffset, bool addOrigin = true)
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

        if (addOrigin)
        {
            roomOrigins.Add(positionOffset + room.roomOrigin);
            roomOriginsDistances.Add((positionOffset + room.roomOrigin - startRoomOrigin).magnitude);
        }

        return true;
    }
}
