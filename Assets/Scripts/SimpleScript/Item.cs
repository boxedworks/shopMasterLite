using System.Collections.Generic;
using System.Linq;

namespace SimpleScript
{

  public class Item
  {

    // Saveable data for items
    [System.Serializable]
    public struct ItemData
    {

      // Unique Id of item
      public int Id;
      public static int s_Id;

      // Identifier for what type of item this is (ex: 0 = wood, 1 = stone, etc.)
      public int TypeId;

      // Item storage
      public Dictionary<int, int> ItemStorage;

      // Holds item's variables that can be referenced in scripts
      public Dictionary<string, object> ItemAttributes;
    }

    //
    [System.Serializable]
    public struct ItemTypeData
    {

      public int Id;

      // Name of type; ex: Wood
      public string Name;

      // Flavor text
      public string Description;

      // Public functions are how entities can interact with items
      [System.NonSerialized]
      public int[] PublicFunctionIds;
    }

    //
    public ItemData _ItemData;
    public ItemTypeData _ItemTypeData { get { return ItemManager.GetItemTypeData(_ItemData.TypeId); } }
    Dictionary<string, object> _attributes { get { return _ItemData.ItemAttributes; } }

    //
    public Item(int typeId)
    {
      _ItemData = new ItemData()
      {
        Id = ItemData.s_Id++,
        TypeId = typeId
      };
    }

    //
    public bool HasAttribute(string key)
    {
      return _attributes?.ContainsKey(key) ?? false;
    }
    public object GetAttribute(string key)
    {
      return _attributes?[key] ?? null;
    }
    public void SetAttribute(string key, object value = null)
    {
      _ItemData.ItemAttributes ??= new();
      if (_attributes.ContainsKey(key))
        _attributes[key] = value;
      else
        _attributes.Add(key, value);
    }
    public void RemoveAttribute(string key)
    {
      if (_attributes == null) return;
      if (!_attributes.ContainsKey(key)) return;
      _attributes.Remove(key);
    }

  }
}