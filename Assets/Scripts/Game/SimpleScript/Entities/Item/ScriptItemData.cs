
using System;
using System.Collections.Generic;

namespace Assets.Scripts.Game.SimpleScript.Entities.Item
{
  // Saveable data for items
  [Serializable]
  public class ScriptItemData
  {

    // Unique Id of item
    public int Id;
    public static int s_Id;

    // Identifier for what type of item this is (ex: 0 = wood, 1 = stone, etc.)
    public int TypeId;

    // Item storage
    public ScriptItemStorage ItemStorage;

    // Holds variables that can be referenced in scripts
    public Dictionary<string, string> Attributes;
  }
}