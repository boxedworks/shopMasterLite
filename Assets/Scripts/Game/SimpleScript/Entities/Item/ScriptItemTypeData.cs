

namespace Assets.Scripts.Game.SimpleScript.Entities.Item
{
  //
  [System.Serializable]
  public struct ScriptItemTypeData
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

}