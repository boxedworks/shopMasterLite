namespace SimpleScript
{

  public class ScriptTarget
  {

    // Holds target types
    ScriptEntity _scriptEntity;
    public ScriptEntity _ScriptEntity { get { return _scriptEntity; } }
    Item _item;
    public Item _Item { get { return _item; } }

    // Target type
    public enum TargetType
    {
      NONE,

      SCRIPT_ENTITY,
      ITEM
    }
    TargetType _targetType;
    public TargetType _TargetType { get { return _targetType; } }

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

    //
    public ScriptTarget(ScriptEntity entity)
    {
      _scriptEntity = entity;
      _targetType = TargetType.SCRIPT_ENTITY;
    }
    public ScriptTarget(Item item)
    {
      _item = item;
      _targetType = TargetType.ITEM;
    }
  }

}