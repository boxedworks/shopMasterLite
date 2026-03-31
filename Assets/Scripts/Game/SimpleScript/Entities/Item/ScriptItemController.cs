using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

using Assets.Scripts.Game.SimpleScript.Entities.Entity;
using Assets.Scripts.Game.SimpleScript.Scripting;

namespace Assets.Scripts.Game.SimpleScript.Entities.Item
{

  public class ScriptItemController
  {

    //
    static ScriptItemController s_singleton;

    //
    FunctionRepository _functionRepository;
    public static FunctionRepository s_FunctionRepository { get { return s_singleton._functionRepository; } }

    //
    [System.Serializable]
    struct ScriptItemTypeDataWrapper
    {
      public List<ScriptItemTypeData> _ItemTypeData;
    }
    ScriptItemTypeDataWrapper _dataTypeWrapper;
    static List<ScriptItemTypeData> s_itemTypeData
    {
      get
      {
        return s_singleton._dataTypeWrapper._ItemTypeData;
      }
    }

    //
    Dictionary<int, ScriptItem> _items;

    //
    public static void Initialize()
    {
      new ScriptItemController();
    }
    public ScriptItemController()
    {
      s_singleton = this;

      _functionRepository = new();

      // Load entity type data
      LoadTypeData();

      //
      _items = new();
    }

    //
    public static ScriptItemVisualIndicatorController s_ItemVisualIndicatorManager { get { return s_singleton._itemVisualIndicatorManager; } }
    ScriptItemVisualIndicatorController _itemVisualIndicatorManager = new();
    public static void Update()
    {
      s_singleton._itemVisualIndicatorManager.Update();
    }

    // Get item by id
    public static ScriptItem GetItem(int id)
    {
      var items = s_singleton._items;
      return items.ContainsKey(id) ? items[id] : null;
    }

    //
    public static void AddItem(ScriptItem item)
    {
      var items = s_singleton._items;
      items[item._ItemData.Id] = item;
    }
    public static void RemoveItem(int id)
    {
      var items = s_singleton._items;
      if (items.ContainsKey(id))
        items.Remove(id);
    }

    // Give entity an item
    public static ScriptItem GiveItem(ScriptEntity entity, int itemTypeId)
    {
      var storage = entity._Storage;
      if (storage == null)
      {
        Debug.LogError("Entity has no item storage!");
        return null;
      }
      for (var i = 0; i < storage.Count; i++)
      {
        if (storage[i] != null)
          continue;

        var newItem = new ScriptItem(itemTypeId);
        storage[i] = newItem._ItemData;
        entity.OnItemGiven();

        return newItem;
      }

      Debug.LogWarning("No empty storage slot found for item!");
      return null;
    }

    //
    public static ScriptItemTypeData GetItemTypeData(int itemTypeId)
    {
      return s_singleton._dataTypeWrapper._ItemTypeData[itemTypeId];
    }

    // Load objects from json
    public void LoadTypeData()
    {
      _functionRepository.Load(false);

      // Load in item type data
      var rawText = System.IO.File.ReadAllText("itemTypeData.json");
      var jsonData = JsonConvert.DeserializeObject<ScriptItemTypeDataWrapper>(rawText);
      _dataTypeWrapper = jsonData;

      // Match functions per item type
      for (var i = 0; i < s_itemTypeData.Count; i++)
      {
        var itemTypeData = s_itemTypeData[i];
        var functionsByType = ScriptEntityHelper.s_FunctionRepository.GetFunctionsByType(itemTypeData.Name.ToLower());
        var functionIds = new List<int>();
        foreach (var func in functionsByType)
        {
          var id = _functionRepository.GetFunctionId(func);
          functionIds.Add(id);
        }
        var publicFunctionIds = functionIds.ToArray();
        itemTypeData.PublicFunctionIds = publicFunctionIds;
        s_itemTypeData[i] = itemTypeData;
      }
    }

    // Save objects to json
    public void SaveTypeData()
    {
      var jsonData = JsonConvert.SerializeObject(_dataTypeWrapper, Formatting.Indented, new JsonSerializerSettings
      {
        NullValueHandling = NullValueHandling.Ignore
      });
      System.IO.File.WriteAllText("itemTypeData.json", jsonData);
    }

    //
    public static bool HasFunction(ScriptItem item, string functionName)
    {
      var itemKey = item._ItemTypeData.Name.ToLower();
      var functionId = s_FunctionRepository.GetFunctionId($"{itemKey}.{functionName}");
      var typeData = item._ItemTypeData;
      var functions = typeData.PublicFunctionIds;
      return functions.Contains(functionId);
    }

    //
    public static int GetFunctionParameterCount(ScriptItem item, string functionName)
    {
      var itemKey = item._ItemTypeData.Name.ToLower();
      var functionTypeData = s_FunctionRepository.GetFunctionData($"{itemKey}.{functionName}");
      return functionTypeData.ParameterCount;
    }

  }
}