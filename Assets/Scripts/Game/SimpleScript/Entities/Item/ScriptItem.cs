using System.Collections.Generic;

using Assets.Scripts.Game.SimpleScript.Entities.Entity;

namespace Assets.Scripts.Game.SimpleScript.Entities.Item
{

  public class ScriptItem : CommonEntity
  {

    //
    public ScriptItemData _ItemData;
    public ScriptItemTypeData _ItemTypeData { get { return ScriptItemController.GetItemTypeData(_ItemData.TypeId); } }
    public override Dictionary<string, string> _Attributes { get { return _ItemData.Attributes; } }
    public override List<ScriptItemData> _Storage { get { return _ItemData.ItemStorage?.Items; } }

    //
    public ScriptItem(int typeId)
    {
      _ItemData = new ScriptItemData()
      {
        Id = ScriptItemData.s_Id++,
        TypeId = typeId
      };

      ScriptItemController.AddItem(this);
    }

    // Load item off of itemdata
    public ScriptItem(ScriptItemData itemData)
    {
      _ItemData = itemData;
      if (itemData.Id >= ScriptItemData.s_Id)
        ScriptItemData.s_Id = itemData.Id + 1;
      ScriptItemController.AddItem(this);
    }

    //
    public void Destroy()
    {
      ScriptItemController.RemoveItem(_ItemData.Id);
    }

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
      _ItemData.Attributes ??= new();
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

  }
}