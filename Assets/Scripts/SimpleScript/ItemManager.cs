using System.Collections.Generic;
using UnityEngine;

namespace SimpleScript
{

  public class ItemManager
  {

    //
    static ItemManager s_singleton;

    //
    [System.Serializable]
    struct EntityTypeDataWrapper
    {
      public List<Item.ItemTypeData> _ItemTypeData;
    }
    EntityTypeDataWrapper _itemDataWrapper;

    //
    public static void Initialize()
    {
      s_singleton = new ItemManager();

      // Load entity type data
      LoadTypeData();
    }

    // Give entity an item
    public static Item GiveItem(ScriptEntity entity, int itemId)
    {
      var storage = entity._EntityData.ItemStorage;
      for (var i = 0; i < storage.Count; i++)
      {
        if (storage[i] != null)
          continue;

        var newItem = new Item(itemId);
        storage[i] = newItem;
        entity.UpdateUIs();

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
    public static void LoadTypeData()
    {
      // Load in item type data
      var rawText = System.IO.File.ReadAllText("itemTypeData.json");
      var jsonData = JsonUtility.FromJson<EntityTypeDataWrapper>(rawText);
      s_singleton._itemDataWrapper = jsonData;
    }

    // Save objects to json
    public static void SaveTypeData()
    {
      var jsonData = JsonUtility.ToJson(s_singleton._itemDataWrapper, true);
      System.IO.File.WriteAllText("itemTypeData.json", jsonData);
    }

  }
}