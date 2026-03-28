using System.Collections.Generic;
using System.Linq;
using CustomUI;
using UnityEngine;

using Newtonsoft.Json;
using System.IO;

namespace Assets.Scripts.Game.SimpleScript
{

  public class ScriptEntityHelper
  {
    public static ScriptEntityHelper s_Singleton { get; private set; }

    List<EntityTypeData> _entityTypeData
    {
      get
      {
        return _entityDataWrapper._EntityTypeData;
      }
    }
    [System.Serializable]
    struct EntityTypeDataWrapper
    {
      public List<EntityTypeData> _EntityTypeData;
    }
    EntityTypeDataWrapper _entityDataWrapper;

    [System.Serializable]
    struct EntityDataWrapper
    {
      public List<ScriptEntity.EntityData> _EntityData;
    }

    //
    FunctionRepository _functionRepository;
    public static FunctionRepository s_FunctionRepository { get { return s_Singleton._functionRepository; } }

    //
    public ScriptEntityHelper()
    {
      s_Singleton = this;

      new EntityMaterialManager();

      ScriptEntity.s_ScriptEntities = new();
      ScriptEntity.s_ScriptEntitiesMapped = new();

      s_Singleton._functionRepository = new();

      LoadTypeData();
    }

    // Load type data from json
    public static void LoadTypeData()
    {
      s_Singleton._functionRepository.Load(true);

      // Load in entity type data
      var rawText = System.IO.File.ReadAllText("entityTypeData.json");
      var jsonData = JsonConvert.DeserializeObject<EntityTypeDataWrapper>(rawText);
      s_Singleton._entityDataWrapper = jsonData;

      // Match functions per entity type
      for (var i = 0; i < s_Singleton._entityTypeData.Count; i++)
      {
        var entityTypeData = s_Singleton._entityTypeData[i];
        var functionIds = new List<int>();
        var entityTypeKey = entityTypeData.Name.ToLower();
        var functionsByType = s_FunctionRepository.GetFunctionsByType(entityTypeKey);
        foreach (var func in functionsByType)
        {
          var id = s_Singleton._functionRepository.GetFunctionId($"{entityTypeKey}.{func}");
          functionIds.Add(id);
        }
        if (functionIds.Count > 0)
        {
          var publicFunctionIds = functionIds.ToArray();
          entityTypeData.PublicFunctionIds = publicFunctionIds;
          s_Singleton._entityTypeData[i] = entityTypeData;
        }
      }
    }

    // Save type data to json
    public static void SaveTypeData()
    {
      var jsonData = JsonConvert.SerializeObject(s_Singleton._entityDataWrapper, Formatting.Indented, new JsonSerializerSettings
      {
        NullValueHandling = NullValueHandling.Ignore
      });
      File.WriteAllText("entityTypeData.json", jsonData);
    }

    // Load game from save data, if no save data, create new game
    public static void LoadEntityData()
    {
      var entityDataFilePath = "entityData.json";

      // Check for load data, if none, create new game
      var hasSaveData = File.Exists(entityDataFilePath);
      if (hasSaveData)
      {
        var rawText = File.ReadAllText(entityDataFilePath);
        var jsonData = JsonConvert.DeserializeObject<EntityDataWrapper>(rawText);
        var entityDataWrapper = jsonData;
        foreach (var entityData in entityDataWrapper._EntityData)
        {
          var entity = new ScriptEntity(entityData);

          // Load items

          if (entity._HasStorage)
            foreach (var itemData in entity._Storage)
            {
              if (itemData != null)
                new Item(itemData);
            }
        }
      }

      // New save data
      else
      {

        // Create starting area
        new ScriptEntity(1, new Vector3(0, 0, -1), -1);
        new ScriptEntity(2, new Vector3(0, 0, -3), -1);
        new ScriptEntity(2, new Vector3(2, 0, -2), -1);

        new ScriptEntity(3, new Vector3(3, 0, 0), -1);
        new ScriptEntity(3, new Vector3(3, 0, -1), -1);
        new ScriptEntity(3, new Vector3(3, 0, 1), -1);
        new ScriptEntity(3, new Vector3(3, 0, -2), -1);
        new ScriptEntity(3, new Vector3(3, 0, 2), -1);
        new ScriptEntity(3, new Vector3(3, 0, -3), -1);
        new ScriptEntity(3, new Vector3(3, 0, 3), -1);

        new ScriptEntity(3, new Vector3(-3, 0, 0), -1);
        new ScriptEntity(3, new Vector3(-3, 0, -1), -1);
        new ScriptEntity(3, new Vector3(-3, 0, 1), -1);
        new ScriptEntity(3, new Vector3(-3, 0, -2), -1);
        new ScriptEntity(3, new Vector3(-3, 0, 2), -1);
        new ScriptEntity(3, new Vector3(-3, 0, -3), -1);
        new ScriptEntity(3, new Vector3(-3, 0, 3), -1);

        new ScriptEntity(3, new Vector3(2, 0, -3), -1);
        new ScriptEntity(3, new Vector3(-2, 0, 3), -1);
        new ScriptEntity(3, new Vector3(1, 0, -3), -1);
        new ScriptEntity(3, new Vector3(-1, 0, 3), -1);
        new ScriptEntity(3, new Vector3(0, 0, 3), -1);
        new ScriptEntity(3, new Vector3(-2, 0, -3), -1);
        new ScriptEntity(3, new Vector3(2, 0, 3), -1);
        new ScriptEntity(3, new Vector3(1, 0, 3), -1);
        new ScriptEntity(3, new Vector3(-1, 0, -3), -1);

        //Terminal.HandleCommand("generate map");
      }
    }

    // Save game data
    public static void SaveGame()
    {
      var saveTimeStart = Time.time;

      // Save entities
      var entityDataFilePath = "entityData.json";
      var entityDataWrapper = new EntityDataWrapper
      {
        _EntityData = new()
      };
      foreach (var entityPair in ScriptEntity.s_ScriptEntities)
      {
        entityDataWrapper._EntityData.Add(entityPair.Value._EntityData);
      }

      var jsonData = JsonConvert.SerializeObject(entityDataWrapper, Formatting.Indented, new JsonSerializerSettings
      {
        NullValueHandling = NullValueHandling.Ignore
      });
      File.WriteAllText(entityDataFilePath, jsonData);

      Terminal.s_Singleton.LogMessage($"Game saved in {Time.time - saveTimeStart:0.00} seconds");
    }

    //
    public static EntityTypeData GetEntityTypeData(int id)
    {
      return s_Singleton._entityTypeData[id];
    }
    public static EntityTypeData GetEntityTypeData(ScriptEntity entity)
    {
      return GetEntityTypeData(entity._EntityData.TypeId);
    }

    //
    public static bool HasFunction(ScriptEntity entity, string functionName)
    {
      var entityKey = entity._EntityTypeData.Name.ToLower();
      var functionId = s_Singleton._functionRepository.GetFunctionId($"{entityKey}.{functionName}");
      var typeData = entity._EntityTypeData;
      var functions = typeData.PublicFunctionIds;
      return functions?.Contains(functionId) ?? false;
    }

    //
    public static int GetFunctionParameterCount(ScriptEntity entity, string functionName)
    {
      var entityKey = entity._EntityTypeData.Name.ToLower();
      var functionTypeData = s_Singleton._functionRepository.GetFunctionData($"{entityKey}.{functionName}");
      return functionTypeData.ParameterCount;
    }

    //
    public static void DestroyAllEntities()
    {
      var entities = ScriptEntity.s_ScriptEntities.Values.ToList();
      foreach (var entity in entities)
      {
        entity.Destroy();
      }
    }

    //
    public struct NoiseSettings
    {
      public float XOffset;
      public float ZOffset;
      public float NoiseScale;
    }
    public static void GenerateMap(NoiseSettings noiseSettings)
    {
      DestroyAllEntities();

      var mapSizeX = 7;
      var mapSizeZ = 7;
      GameObject.Find("Floor").transform.localScale = new Vector3(mapSizeX, 1, mapSizeZ);
      for (var x = 0; x < mapSizeX; x++)
      {
        for (var z = 0; z < mapSizeZ; z++)
        {
          var x_ = -mapSizeX / 2 + x;
          var z_ = -mapSizeZ / 2 + z;

          var noise = Mathf.PerlinNoise(
            (x + noiseSettings.XOffset) * noiseSettings.NoiseScale,
            (z + noiseSettings.ZOffset) * noiseSettings.NoiseScale);
          noise = Mathf.Pow(noise, 2f);

          var entityType = -1;
          if (noise < 0.01f)
          {
            entityType = 2;
          }
          else if (noise < 0.5f)
          {
          }
          else if (noise < 0.52f)
          {
            entityType = 2;
          }
          else
          {
            entityType = 3;
          }

          Debug.Log($"Noise for {x_}, {z_}: {noise} .. entity type: {entityType}");

          if (entityType != -1)
            new ScriptEntity(entityType, new Vector3(x_, 0, z_), -1);
        }
      }
    }
  }
}