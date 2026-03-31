using System.Collections.Generic;

using Assets.Scripts.Game.SimpleScript.Entities.Entity;
using Assets.Scripts.Game.SimpleScript.Entities.Item;

namespace Assets.Scripts.Game.SimpleScript.Scripting
{

  public class ScriptTarget : CommonEntity
  {

    // Holds target types
    ScriptEntity _scriptEntity;
    public ScriptEntity _ScriptEntity { get { return _scriptEntity; } }
    ScriptItem _item;
    public ScriptItem _Item { get { return _item; } }

    // Target type
    public enum TargetType
    {
      NONE,

      SCRIPT_ENTITY,
      ITEM
    }
    TargetType _targetType;
    public TargetType _TargetType { get { return _targetType; } }

    public bool _IsItem { get { return _targetType == TargetType.ITEM; } }
    public bool _IsScriptEntity { get { return _targetType == TargetType.SCRIPT_ENTITY; } }

    // Get name
    public string _Type
    {
      get
      {
        return _targetType switch
        {
          TargetType.SCRIPT_ENTITY => _scriptEntity._EntityTypeData.Name,
          _ => _item._ItemTypeData.Name
        };
      }
    }

    // Get position
    public (int x, int y, int z) _TilePosition
    {
      get
      {
        return _targetType switch
        {
          TargetType.SCRIPT_ENTITY => _scriptEntity._TilePosition,
          _ => (0, 0, 0)
        };
      }
    }

    public override List<ScriptItemData> _Storage { get { return _IsScriptEntity ? _scriptEntity._EntityData.ItemStorage?.Items : _item._ItemData.ItemStorage?.Items; } }
    public override Dictionary<string, string> _Attributes { get { return _IsScriptEntity ? _scriptEntity._EntityData.Attributes : _item._ItemData.Attributes; } }

    //
    public ScriptTarget(ScriptEntity entity)
    {
      _scriptEntity = entity;
      _targetType = TargetType.SCRIPT_ENTITY;
    }
    public ScriptTarget(ScriptItem item)
    {
      _item = item;
      _targetType = TargetType.ITEM;
    }

    //
    public override string ToString()
    {
      return _IsScriptEntity ? _scriptEntity.ToString() : _item.ToString();
    }

    //
    public static ScriptTarget TryGetScriptTarget(string targetString)
    {

      if (ScriptEntityHelper.IsValidVariableEntity(targetString))
        return new ScriptTarget(ScriptEntityHelper.GetEntityByStatement(targetString));
      else if (ScriptEntityHelper.IsValidVariableItem(targetString))
        return new ScriptTarget(ScriptEntityHelper.GetItemByStatement(targetString));
      return null;
    }
  }

}