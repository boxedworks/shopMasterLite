using System.Collections.Generic;
using Mono.Cecil;
using UnityEngine;

namespace Assets.Scripts.Game.SimpleScript
{

  public class EntityMaterialManager
  {

    static EntityMaterialManager s_Singleton { get; set; }

    Dictionary<string, Material> _materialsByName;
    Dictionary<string, int> _entityCountBySpritePath;
    Material _baseMaterial { get { return GameController.s_Singleton._baseEntityMaterial; } }

    public EntityMaterialManager()
    {
      s_Singleton = this;

      _materialsByName = new();
      _entityCountBySpritePath = new();
    }

    // Check if material for this entity type already exists, if not, create it and add to memory
    public static Material GetMaterialBySpritePath(string spritePath)
    {
      var materialsByName = s_Singleton._materialsByName;
      var materialCountBySpritePath = s_Singleton._entityCountBySpritePath;
      var hasMaterial = materialsByName.TryGetValue(spritePath, out var material);
      if (!hasMaterial)
      {
        material = new Material(s_Singleton._baseMaterial)
        {
          name = spritePath
        };
        var sprite = GameResources.LoadSprite(spritePath);
        material.SetTexture("_BaseMap", sprite.texture);

        materialsByName.Add(spritePath, material);

        materialCountBySpritePath.Add(spritePath, 1);

        Debug.Log("Created new material for " + spritePath);
      }
      else
      {

        materialCountBySpritePath[spritePath]++;

        //Debug.Log("Material already exists for " + spritePath);
      }

      return material;
    }

    // If no more entitys of this type, remove material from memory
    public static void OnEntityRemoved(string spritePath)
    {
      if (string.IsNullOrEmpty(spritePath))
      {
        return;
      }

      var materialsByName = s_Singleton._materialsByName;
      var materialCountBySpritePath = s_Singleton._entityCountBySpritePath;
      var hasMaterial = materialsByName.TryGetValue(spritePath, out var material);
      if (hasMaterial)
      {
        materialCountBySpritePath[spritePath]--;
        if (materialCountBySpritePath[spritePath] <= 0)
        {
          Object.Destroy(material);
          materialsByName.Remove(spritePath);
          materialCountBySpritePath.Remove(spritePath);

          Debug.Log("Removing material for " + spritePath);
        }
        else
        {
          Debug.Log("Decrementing material count for " + spritePath + " to " + materialCountBySpritePath[spritePath]);
        }
      }
    }

  }

}