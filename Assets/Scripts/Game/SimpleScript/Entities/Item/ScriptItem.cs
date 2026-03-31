using System.Collections.Generic;
using Assets.Scripts.Game.SimpleScript.Entities.Entity;
using Assets.Scripts.Game.SimpleScript.Scripting;
using UnityEngine;

namespace Assets.Scripts.Game.SimpleScript.Entities.Item
{

  public class ScriptItem : CommonEntity
  {

    //
    public ScriptItemData _ItemData;
    public ScriptItemTypeData _ItemTypeData { get { return ScriptItemController.GetItemTypeData(_ItemData.TypeId); } }
    public override Dictionary<string, string> _Attributes { get { return _ItemData.Attributes; } set { _ItemData.Attributes = value; } }
    public override List<ScriptItemData> _Storage { get { return _ItemData.ItemStorage?.Items; } }

    //
    string _spriteName;
    public string _SpriteName { get { return _spriteName ?? _ItemTypeData.Name.ToLower(); } }

    //
    public ScriptItem(int typeId, ScriptEntity spawnEntity)
    {
      _ItemData = new ScriptItemData()
      {
        Id = ScriptItemData.s_Id++,
        TypeId = typeId
      };

      Init(spawnEntity);
    }

    // Load item off of itemdata
    public ScriptItem(ScriptItemData itemData, ScriptEntity spawnEntity)
    {
      _ItemData = itemData;
      if (itemData.Id >= ScriptItemData.s_Id)
        ScriptItemData.s_Id = itemData.Id + 1;

      Init(spawnEntity);
    }

    void Init(ScriptEntity spawnEntity)
    {
      ScriptItemController.AddItem(this);

      // Check spawn script
      Debug.Log($"Checking spawn script for item type {_ItemTypeData.Name}");
      if (ScriptItemController.HasFunction(this, "spawn"))
      {
        var itemScript = spawnEntity.LoadAndAttachScript(new ScriptBaseController.ScriptLoadData()
        {
          ScriptType = ScriptBaseController.ScriptType.ITEM,
          PathTo = $"{_ItemTypeData.Name.ToLower()}.spawn"
        });
        itemScript.AttachItem(this);

        // Tick spawn script immediately
        itemScript.Tick();
      }
    }

    //
    public void Destroy()
    {
      ScriptItemController.RemoveItem(_ItemData.Id);
    }

    //
    public bool SetSprite(string spritePath)
    {
      _spriteName = spritePath;
      return true;
    }

    //
    public override string ToString()
    {
      return $"Item[{_ItemTypeData.Name}]";
    }

  }
}