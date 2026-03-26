using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Assets.Scripts.Game.SimpleScript
{

  public class ItemManager
  {

    //
    static ItemManager s_singleton;

    //
    FunctionRepository _functionRepository;
    public static FunctionRepository s_FunctionRepository { get { return s_singleton._functionRepository; } }

    //
    [System.Serializable]
    struct ItemTypeDataWrapper
    {
      public List<Item.ItemTypeData> _ItemTypeData;
    }
    ItemTypeDataWrapper _itemDataWrapper;
    static List<Item.ItemTypeData> s_itemTypeData
    {
      get
      {
        return s_singleton._itemDataWrapper._ItemTypeData;
      }
    }

    //
    Dictionary<int, Item> _items;

    //
    public static void Initialize()
    {
      new ItemManager();
    }
    public ItemManager()
    {
      s_singleton = this;

      _functionRepository = new();

      // Load entity type data
      LoadTypeData();

      //
      _items = new();
    }

    // Get item by id
    public static Item GetItem(int id)
    {
      var items = s_singleton._items;
      return items.ContainsKey(id) ? items[id] : null;
    }

    //
    public static void AddItem(Item item)
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
    public static Item GiveItem(ScriptEntity entity, int itemTypeId)
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

        var newItem = new Item(itemTypeId);
        storage[i] = newItem._ItemData;
        entity.OnItemGiven();

        return newItem;
      }

      Debug.LogWarning("No empty storage slot found for item!");
      return null;
    }

    //
    public static Item.ItemTypeData GetItemTypeData(int itemTypeId)
    {
      return s_singleton._itemDataWrapper._ItemTypeData[itemTypeId];
    }

    // Load objects from json
    public void LoadTypeData()
    {
      _functionRepository.Load(false);

      // Load in item type data
      var rawText = System.IO.File.ReadAllText("itemTypeData.json");
      var jsonData = JsonConvert.DeserializeObject<ItemTypeDataWrapper>(rawText);
      _itemDataWrapper = jsonData;

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
      var jsonData = JsonConvert.SerializeObject(_itemDataWrapper, Formatting.Indented, new JsonSerializerSettings
      {
        NullValueHandling = NullValueHandling.Ignore
      });
      System.IO.File.WriteAllText("itemTypeData.json", jsonData);
    }

    //
    public static bool HasFunction(Item item, string functionName)
    {
      var itemKey = item._ItemTypeData.Name.ToLower();
      var functionId = s_FunctionRepository.GetFunctionId($"{itemKey}.{functionName}");
      var typeData = item._ItemTypeData;
      var functions = typeData.PublicFunctionIds;
      return functions.Contains(functionId);
    }

    //
    public static int GetFunctionParameterCount(Item item, string functionName)
    {
      var itemKey = item._ItemTypeData.Name.ToLower();
      var functionTypeData = s_FunctionRepository.GetFunctionData($"{itemKey}.{functionName}");
      return functionTypeData.ParameterCount;
    }

  }
}