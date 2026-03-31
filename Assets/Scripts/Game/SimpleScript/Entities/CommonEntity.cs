

using System.Collections.Generic;
using Assets.Scripts.Game.SimpleScript.Entities.Item;

namespace Assets.Scripts.Game.SimpleScript
{

  // Common interface for entities and items
  public abstract class CommonEntity
  {

    public abstract List<ScriptItemData> _Storage { get; }
    public bool _HasStorage { get { return (_Storage?.Count ?? 0) > 0; } }

    public abstract Dictionary<string, string> _Attributes { get; set; }

    //
    public bool HasAttribute(string key)
    {
      return _Attributes?.ContainsKey(key) ?? false;
    }
    public string GetAttribute(string key)
    {
      return _Attributes?[key] ?? null;
    }
    public void SetAttribute(string key, string value = null)
    {
      _Attributes ??= new();
      if (_Attributes.ContainsKey(key))
        _Attributes[key] = value;
      else
        _Attributes.Add(key, value);
    }
    public void RemoveAttribute(string key)
    {
      if (_Attributes == null) return;
      if (!_Attributes.ContainsKey(key)) return;
      _Attributes.Remove(key);
    }

    //
    public abstract override string ToString();
  }

}