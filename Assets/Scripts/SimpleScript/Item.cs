using System.Collections.Generic;

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

      // Identifier for what type of item this is (ex: 0 = wood, 1 = stone, etc.)
      public int TypeId;

      // Item storage
      public Dictionary<int, int> ItemStorage;

      // Holds item's variables that can be referenced in scripts
      public List<string> ItemAttributes;
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

    //
    public Item(int typeId)
    {
      _ItemData = new ItemData()
      {
        TypeId = typeId
      };
    }

  }
}