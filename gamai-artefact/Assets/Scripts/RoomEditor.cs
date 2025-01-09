using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteInEditMode]
public class RoomEditor : MonoBehaviour
{
    public string currentRoomName = "default";
    public string newRoomName = "";
    public bool attemptLoad = false;
    public bool attemptSave = false;
    public string roomDirPath = "C:/Users/kori/gamai-artefact/gamai-artefact/Assets/Rooms/";
    public GridSetup world;

    public void Update()
    {
        if (attemptLoad && currentRoomName != newRoomName) 
        {
            currentRoomName = newRoomName;
            Debug.Log($"Attempting to load room with name: {newRoomName}");
            attemptLoad = false;
            LoadRoom();
        }

        if (attemptSave)
        {
            Debug.Log($"Attempting to save room with name: {newRoomName}");
            attemptSave = false;
            SaveRoom();
        }
        
    }

    public void LoadRoom()
    {
        foreach (Tilemap tilemap in world.Tilemaps)
        {
            tilemap.ClearAllTiles();
        }
        string path = roomDirPath + newRoomName + ".json";
        if (!File.Exists(path))
        {
            Debug.LogError($"Cannot load file at {path} as it does not exist");
        }

        try
        {
            Room room= JsonConvert.DeserializeObject<Room>(File.ReadAllText(path));
            room.wrappedTilemaps.Unwrap(world);
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not load data at {path} due to {e.Message} {e.StackTrace}");
            throw;
        }
    }

    public bool SaveRoom()
    {
        Debug.Log(world.Tilemaps.Count);
        var settings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        string path = roomDirPath + newRoomName + ".json";
        try
        {
            if (File.Exists(path))
            {
                Debug.Log($"Data at {path} exists. Deleting old file");
                File.Delete(path);
            }
            else
            {
                Debug.Log($"Saving data at {path} for the first time");
            }

            using var stream = File.Create(path);
            stream.Close();
            Room data = new Room(new WrappedTilemaps(world));
            File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented, settings));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Saving to {path} failed due to {e.Message} {e.StackTrace}");
            return false;
        }
        
    }
}

public class Room
{
    public WrappedTilemaps wrappedTilemaps;

    public Room(WrappedTilemaps wrappedTilemaps)
    {
        this.wrappedTilemaps = wrappedTilemaps;
    }
}

[Serializable]
public class WrappedTilemaps
{
    public Dictionary<Vector3Int, string> wrappedTilemaps;

    public WrappedTilemaps(GridSetup world)
    {
        wrappedTilemaps = new();
        for (int z = 0; z < world.Tilemaps.Count; z++)
        {
            for (int x = 0; x < world.gridDimensions.x; x++)
            {
                for (int y = 0; y < world.gridDimensions.y; y++)
                {
                    Tilemap tilemap = world.Tilemaps[z];
                    Vector3Int position = new Vector3Int(x, y, z);
                    CustomTile tile = tilemap.GetTile<CustomTile>(new Vector3Int(position.x, position.y, 0));
                    if (tile != null && tile.name != "Barrier")
                    {
                        wrappedTilemaps.Add(position, tile.name);
                    }
                }
            }
        }
        Debug.Log(wrappedTilemaps.Count);
    }

    [JsonConstructor]
    public WrappedTilemaps(Dictionary<string, string> wrappedTilemaps)
    {
        this.wrappedTilemaps = new Dictionary<Vector3Int, string>();
        foreach (var kvp in wrappedTilemaps)
        {
            Vector3Int key = ParseVector3Int(kvp.Key);
            this.wrappedTilemaps[key] = kvp.Value;
        }
    }

    private Vector3Int ParseVector3Int(string str)
    {
        str = str.Trim('(', ')');
        var parts = str.Split(',');
        if (parts.Length == 3 &&
            int.TryParse(parts[0], out int x) &&
            int.TryParse(parts[1], out int y) &&
            int.TryParse(parts[2], out int z))
        {
            return new Vector3Int(x, y, z);
        }
        throw new JsonSerializationException("Invalid Vector3Int format");
    }

    public void Unwrap(GridSetup world)
    {
        TileDictionary tileDictionary = new TileDictionary();
        foreach (KeyValuePair<Vector3Int, string> entry in wrappedTilemaps)
        {
            Vector3Int pos = entry.Key;
            CustomTile tile = tileDictionary.dict.GetValueOrDefault(entry.Value);
            world.Tilemaps[pos.z].SetTile(new Vector3Int(pos.x, pos.y, 0), tile);
            // Create border
        }
        FillArea(new Vector3Int(-1, -1, 0), new Vector3Int(0, 50, 0), tileDictionary.dict.GetValueOrDefault("Barrier"), world);
        FillArea(new Vector3Int(-1, 49, 0), new Vector3Int(50, 50, 0), tileDictionary.dict.GetValueOrDefault("Barrier"), world);
        FillArea(new Vector3Int(-1, -1, 0), new Vector3Int(50, 0, 0), tileDictionary.dict.GetValueOrDefault("Barrier"), world);
        FillArea(new Vector3Int(49, -1, 0), new Vector3Int(50, 50, 0), tileDictionary.dict.GetValueOrDefault("Barrier"), world);
    }

    public void FillArea(Vector3Int pos1, Vector3Int pos2, CustomTile tile, GridSetup world)
    {
        //tile = Tilemaps[0].GetTile<CustomTile>(new Vector3Int(0, 0, 0));
        Debug.Log("test");
        for (int x = pos1.x; x < pos2.x; x++)
        {
            for (int y = pos1.y; y < pos2.y; y++)
            {
                world.Tilemaps[pos1.z].SetTile(new Vector3Int(x, y), tile);
            }
        }
    }
}

public class TileDictionary
{
    public Dictionary<string, CustomTile> dict;
    public TileDictionary()
    {
        dict = new Dictionary<string, CustomTile>();
        foreach (var asset in AssetDatabase.FindAssets("t:CustomTile"))
        {
            var tile = AssetDatabase.LoadAssetAtPath<CustomTile>(AssetDatabase.GUIDToAssetPath(asset));
            try
            {
                dict.Add(tile.name, tile);
            }
            catch (ArgumentException e)
            {
                Debug.LogError($"Tile could not be added to dictionary as it's name is duplicate {e.Message}, {e.StackTrace}");
            }
        }
    }
}
