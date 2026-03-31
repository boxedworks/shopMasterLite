
// Saveable data for the entity
using System;
using System.Collections.Generic;
using Assets.Scripts.Game.SimpleScript.Entities.Item;

namespace Assets.Scripts.Game.SimpleScript.Entities.Entity
{

  [Serializable]
  public struct ScriptEntityData
  {

    // Unique Id of entity
    public int Id;

    // Identifier for what type of entity this is (ex: 0 = wood, 1 = stone, etc.)
    public int TypeId;

    // Owner of entity; -1 = server
    public int OwnerId;

    // X, Y, and Z position of the entity
    public int X, Y, Z;

    // Entity direction
    public int Direction;

    // Item storage
    public ScriptItemStorage ItemStorage;

    // Logger
    public List<string> Log;

    // Holds variables that can be referenced in scripts
    public Dictionary<string, string> Attributes;
  }

}