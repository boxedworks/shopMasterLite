using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

using Assets.Scripts.Game.SimpleScript.Entities.Item;
using Assets.Scripts.Game.UI;
using Assets.Scripts.Game.SimpleScript.Scripting;

namespace Assets.Scripts.Game.SimpleScript.Entities.Entity
{

  public class ScriptEntityHelper
  {
    public static ScriptEntityHelper s_Singleton { get; private set; }

    List<ScriptEntityTypeData> _entityTypeData
    {
      get
      {
        return _entityDataWrapper._EntityTypeData;
      }
    }

    ScriptEntityTypeDataWrapper _entityDataWrapper;

    //
    FunctionRepository _functionRepository;
    public static FunctionRepository s_FunctionRepository { get { return s_Singleton._functionRepository; } }

    //
    public ScriptEntityHelper()
    {
      s_Singleton = this;

      new ScriptEntityMaterialController();

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
      var rawText = File.ReadAllText("entityTypeData.json");
      var jsonData = JsonConvert.DeserializeObject<ScriptEntityTypeDataWrapper>(rawText);
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
        var jsonData = JsonConvert.DeserializeObject<ScriptEntityDataWrapper>(rawText);
        var entityDataWrapper = jsonData;
        foreach (var entityData in entityDataWrapper._EntityData)
        {
          var entity = new ScriptEntity(entityData);

          // Load items
          if (entity._HasStorage)
            foreach (var itemData in entity._Storage)
            {
              if (itemData != null)
                new ScriptItem(itemData, entity);
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
      var entityDataWrapper = new ScriptEntityDataWrapper
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
    public static ScriptEntityTypeData GetEntityTypeData(int id)
    {
      return s_Singleton._entityTypeData[id];
    }
    public static ScriptEntityTypeData GetEntityTypeData(ScriptEntity entity)
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

    #region Variable Handling
    // Function for validating variable types

    public static bool IsValidVariableEntity(string variable)
    {
      return variable.StartsWith("$Entity[") && variable.EndsWith("]");
    }

    // Function for getting entity from variable
    public static ScriptEntity GetEntityByStatement(string statement_)
    {
      statement_ = statement_.Trim();
      if (IsValidVariableEntity(statement_))
        return ScriptEntity.GetEntity(int.Parse(statement_.Split("$Entity[")[1][..^1]));
      return null;
    }
    public static ScriptEntity GetEntityById(int id)
    {
      return ScriptEntity.GetEntity(id);
    }
    public static ScriptEntity GetEntityByIdOrStatement(string idOrStatement)
    {
      if (IsValidVariableEntity(idOrStatement))
        return GetEntityByStatement(idOrStatement);
      else if (int.TryParse(idOrStatement, out var id))
        return GetEntityById(id);
      return null;
    }

    public static string GetEntityStatement(ScriptEntity entity)
    {
      return $"$Entity[{entity._EntityData.Id}]";
    }

    // Function for validating item variable
    public static bool IsValidVariableItem(string variable)
    {
      return variable.StartsWith("$Item[") && variable.EndsWith("]");
    }

    // Function for getting entity from variable
    public static ScriptItem GetItemByStatement(string statement_)
    {
      statement_ = statement_.Trim();
      if (IsValidVariableItem(statement_))
        return ScriptItemController.GetItem(int.Parse(statement_.Split("$Item[")[1][..^1]));
      return null;
    }
    public static ScriptItem GetItemById(int id)
    {
      return ScriptItemController.GetItem(id);
    }
    public static ScriptItem GetItemByIdOrStatement(string idOrStatement)
    {
      if (IsValidVariableItem(idOrStatement))
        return GetItemByStatement(idOrStatement);
      else if (int.TryParse(idOrStatement, out var id))
        return GetItemById(id);
      return null;
    }

    public static string GetItemStatement(ScriptItem item)
    {
      return GetItemStatement(item._ItemData.Id);
    }
    public static string GetItemStatement(int itemId)
    {
      return $"$Item[{itemId}]";
    }

    public static bool IsValidTargetVariable(string variable)
    {
      return IsValidVariableEntity(variable) || IsValidVariableItem(variable);
    }
    public static ScriptTarget GetTargetByStatement(string statement)
    {
      var entity = GetEntityByStatement(statement);
      if (entity != null) return new ScriptTarget(entity);
      var item = GetItemByStatement(statement);
      if (item != null) return new ScriptTarget(item);
      return null;
    }

    public static string GetTargetStatement(ScriptTarget target)
    {
      return target._TargetType switch
      {
        ScriptTarget.TargetType.SCRIPT_ENTITY => GetEntityStatement(target._ScriptEntity),
        ScriptTarget.TargetType.ITEM => GetItemStatement(target._Item),
        _ => null
      };
    }

    public static bool IsStringVariable(string variable)
    {
      return variable.StartsWith("\"") && variable.EndsWith("\"");
    }
    public static string GetStringVariable(string param)
    {
      return param[1..^1];
    }
    #endregion

    public static Vector3 DirectionToVector3(int direction)
    {
      switch (direction)
      {
        case 0: return new Vector3(0, 0, 1);
        case 1: return new Vector3(0, 0, -1);
        case 2: return new Vector3(-1, 0, 0);
        case 3: return new Vector3(1, 0, 0);
      }
      return Vector3.zero;
    }

  }
}