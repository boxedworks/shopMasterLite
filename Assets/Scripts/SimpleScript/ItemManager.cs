using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimpleScript
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
    struct EntityTypeDataWrapper
    {
      public List<Item.ItemTypeData> _ItemTypeData;
    }
    EntityTypeDataWrapper _itemDataWrapper;
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
    }

    // Get item by id
    public static Item GetItem(int id)
    {
      var items = s_singleton._items;
      return items.ContainsKey(id) ? items[id] : null;
    }

    // Give entity an item
    public static Item GiveItem(ScriptEntity entity, int itemId)
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

        var newItem = new Item(itemId);
        storage[i] = newItem;
        entity.OnItemGiven();

        return newItem;
      }

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
      var functionsByType = _functionRepository.Load(false);

      // Load in item type data
      var rawText = System.IO.File.ReadAllText("itemTypeData.json");
      var jsonData = JsonUtility.FromJson<EntityTypeDataWrapper>(rawText);
      _itemDataWrapper = jsonData;

      // Match functions per item type
      for (var i = 0; i < s_itemTypeData.Count; i++)
      {
        var itemTypeData = s_itemTypeData[i];
        var functionIds = new List<int>();
        if (functionsByType.ContainsKey(itemTypeData.Name.ToLower()))
          foreach (var func in functionsByType[itemTypeData.Name.ToLower()])
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
      var jsonData = JsonUtility.ToJson(_itemDataWrapper, true);
      System.IO.File.WriteAllText("itemTypeData.json", jsonData);
    }

    //
    public static bool HasFunction(Item item, string functionName)
    {
      var functionId = s_FunctionRepository.GetFunctionId(functionName);
      var typeData = item._ItemTypeData;
      var functions = typeData.PublicFunctionIds;
      return functions.Contains(functionId);
    }

  }
}