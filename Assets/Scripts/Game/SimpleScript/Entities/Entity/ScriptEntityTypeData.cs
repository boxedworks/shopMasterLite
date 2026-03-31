using System;

namespace Assets.Scripts.Game.SimpleScript.Entities.Entity
{

  //
  [Serializable]
  public struct ScriptEntityTypeData
  {

    public int Id;

    // Name of type; ex: Wood
    public string Name;

    // Flavor text for user
    public string Description;

    // Whether other entities can share the tile; ex: ground items
    public bool Solid;

    // 'Radius' of the object; default 1, adding 1 will expand the outer ring of tiles
    public int Size;

    // Public functions are how entities can interact with other entities
    [NonSerialized]
    public int[] PublicFunctionIds;

    // Private functions are how entities can interact with other entities
    [NonSerialized]
    public int[] PrivateFunctionIds;
  }

}