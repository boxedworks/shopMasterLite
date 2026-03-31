

using System.Collections.Generic;
using Assets.Scripts.Game.SimpleScript.Entities.Item;

namespace Assets.Scripts.Game.SimpleScript
{

  // Common interface for entities and items
  public abstract class CommonEntity
  {

    public abstract List<ScriptItemData> _Storage { get; }
    public bool _HasStorage { get { return (_Storage?.Count ?? 0) > 0; } }

    public abstract Dictionary<string, string> _Attributes { get; }

    //
    public bool HasEntityVariable(string variableName)
    {
      return _Attributes.ContainsKey(variableName);
    }
    public string GetEntityVariable(string variableName)
    {
      return _Attributes[variableName];
    }
    public void SetEntityVariable(string variableName, string value)
    {
      if (!HasEntityVariable(variableName))
      {
        _Attributes.Add(variableName, value);
        return;
      }
      _Attributes[variableName] = value;
    }
  }

}