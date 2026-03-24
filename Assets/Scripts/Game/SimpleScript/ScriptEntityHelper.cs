using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Game.SimpleScript
{

  public static class ScriptEntityHelper
  {

    static List<EntityTypeData> s_entityTypeData
    {
      get
      {
        return s_entityDataWrapper._EntityTypeData;
      }
    }
    [System.Serializable]
    struct EntityTypeDataWrapper
    {
      public List<EntityTypeData> _EntityTypeData;
    }
    static EntityTypeDataWrapper s_entityDataWrapper;


    //
    public static FunctionRepository s_FunctionRepository;

    //
    public static void Init()
    {
      ScriptEntity.s_ScriptEntities = new();
      ScriptEntity.s_ScriptEntitiesMapped = new();

      s_FunctionRepository = new();

      LoadTypeData();
    }

    // Load objects from json
    public static void LoadTypeData()
    {
      var functionsByType = s_FunctionRepository.Load(true);

      // Load in entity type data
      var rawText = System.IO.File.ReadAllText("entityTypeData.json");
      var jsonData = JsonUtility.FromJson<EntityTypeDataWrapper>(rawText);
      s_entityDataWrapper = jsonData;

      // Match functions per entity type
      for (var i = 0; i < s_entityTypeData.Count; i++)
      {
        var entityTypeData = s_entityTypeData[i];
        var functionIds = new List<int>();
        var entityTypeKey = entityTypeData.Name.ToLower();
        if (functionsByType.ContainsKey(entityTypeKey))
          foreach (var func in functionsByType[entityTypeKey])
          {
            var id = s_FunctionRepository.GetFunctionId($"{entityTypeKey}.{func}");
            functionIds.Add(id);
          }
        if (functionIds.Count > 0)
        {
          var publicFunctionIds = functionIds.ToArray();
          entityTypeData.PublicFunctionIds = publicFunctionIds;
          s_entityTypeData[i] = entityTypeData;
        }
      }
    }

    // Save objects to json
    public static void SaveTypeData()
    {
      var jsonData = JsonUtility.ToJson(s_entityDataWrapper, true);
      System.IO.File.WriteAllText("entityTypeData.json", jsonData);
    }

    //
    public static EntityTypeData GetEntityTypeData(int id)
    {
      return s_entityTypeData[id];
    }
    public static EntityTypeData GetEntityTypeData(ScriptEntity entity)
    {
      return GetEntityTypeData(entity._EntityData.TypeId);
    }

    //
    public static bool HasFunction(ScriptEntity entity, string functionName)
    {
      var entityKey = entity._EntityTypeData.Name.ToLower();
      var functionId = s_FunctionRepository.GetFunctionId($"{entityKey}.{functionName}");
      var typeData = entity._EntityTypeData;
      var functions = typeData.PublicFunctionIds;
      return functions?.Contains(functionId) ?? false;
    }

    //
    public static int GetFunctionParameterCount(ScriptEntity entity, string functionName)
    {
      var entityKey = entity._EntityTypeData.Name.ToLower();
      var functionTypeData = s_FunctionRepository.GetFunctionData($"{entityKey}.{functionName}");
      return functionTypeData.ParameterCount;
    }
  }
}