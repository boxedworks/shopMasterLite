
using System.Collections.Generic;
using Assets.Scripts.Game.SimpleScript.Entities.Entity;
using UnityEngine;

namespace Assets.Scripts.Game.SimpleScript.Entities.Item
{

  public class ScriptItemVisualIndicatorController
  {

    List<ScriptItemVisualIndicator> _activeIndicators;

    public ScriptItemVisualIndicatorController()
    {
      _activeIndicators = new();
    }

    public void Update()
    {
      for (var i = _activeIndicators.Count - 1; i >= 0; i--)
      {
        var indicator = _activeIndicators[i];
        var elapsedTime = Time.time - indicator._CreationTime;
        var duration = 0.5f; // Duration for the indicator to move from spawn position to entity position
        if (elapsedTime > duration)
        {
          // Destroy visual indicator object
          if (indicator._IndicatorObject != null)
            Object.Destroy(indicator._IndicatorObject);

          // Remove from active indicators list
          _activeIndicators.RemoveAt(i);
        }
        else
        {
          // Update visual indicator position or other properties if needed
          if (indicator._IndicatorObject != null)
          {
            var indicatorTransform = indicator._IndicatorObject.transform;
            var lookAt = Quaternion.LookRotation(GameResources._MainCamera.transform.position - indicatorTransform.position);
            indicatorTransform.rotation = lookAt;
            indicatorTransform.localRotation = Quaternion.Euler(indicatorTransform.localRotation.eulerAngles.x, GameResources._MainCamera.transform.localRotation.eulerAngles.y + 180f, indicatorTransform.localRotation.eulerAngles.z);

            indicator._IndicatorObject.transform.position = Vector3.Lerp(indicator._SpawnPosition, indicator._Entity._TilePositionVector3, elapsedTime / duration);

            var position = Vector3.Lerp(indicator._SpawnPosition, indicator._Entity._TilePositionVector3, elapsedTime / duration);
            var jumpHeight = 0.5f;
            position.y += Mathf.Sin(elapsedTime / duration * Mathf.PI) * jumpHeight;
            indicator._IndicatorObject.transform.position = position;
          }
        }
      }
    }

    public void CreateIndicator(ScriptEntity forEntity, ScriptItemTypeData itemType, Vector3 spawnPosition)
    {
      // Create visual indicator object and set properties based on item type
      var itemSprite = GameResources.LoadItemSprite($"{itemType.Name.ToLower()}");
      var indicatorObject = new GameObject($"ItemIndicator_{itemType.Name}");
      indicatorObject.transform.position = spawnPosition;
      var spriteRenderer = indicatorObject.AddComponent<SpriteRenderer>();
      spriteRenderer.sprite = itemSprite;

      var indicator = new ScriptItemVisualIndicator
      {
        _Entity = forEntity,
        _ItemType = itemType,
        _CreationTime = Time.time,
        _SpawnPosition = spawnPosition,
        _IndicatorObject = indicatorObject
      };

      _activeIndicators.Add(indicator);
    }

  }

}