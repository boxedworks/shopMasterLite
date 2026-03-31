
using Assets.Scripts.Game.SimpleScript.Entities.Entity;
using UnityEngine;

namespace Assets.Scripts.Game.SimpleScript.Entities.Item
{
  struct ScriptItemVisualIndicator
  {
    public ScriptEntity _Entity;
    public ScriptItemTypeData _ItemType;
    public GameObject _IndicatorObject;
    public float _CreationTime;
    public Vector3 _SpawnPosition;
  }
}