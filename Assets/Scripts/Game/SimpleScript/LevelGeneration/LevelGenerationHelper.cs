using Assets.Scripts.Game.SimpleScript.Entities.Entity;
using UnityEngine;

namespace Assets.Scripts.Game.SimpleScript.LevelGeneration
{


  public static class LevelGenerationHelper
  {

    //
    public static void GenerateMap(NoiseSettings noiseSettings)
    {
      ScriptEntityHelper.DestroyAllEntities();

      var mapSizeX = 7;
      var mapSizeZ = 7;
      GameObject.Find("Floor").transform.localScale = new Vector3(mapSizeX, 1, mapSizeZ);
      for (var x = 0; x < mapSizeX; x++)
      {
        for (var z = 0; z < mapSizeZ; z++)
        {
          var x_ = -mapSizeX / 2 + x;
          var z_ = -mapSizeZ / 2 + z;

          var noise = Mathf.PerlinNoise(
            (x + noiseSettings.XOffset) * noiseSettings.NoiseScale,
            (z + noiseSettings.ZOffset) * noiseSettings.NoiseScale);
          noise = Mathf.Pow(noise, 2f);

          var entityType = -1;
          if (noise < 0.01f)
          {
            entityType = 2;
          }
          else if (noise < 0.5f)
          {
          }
          else if (noise < 0.52f)
          {
            entityType = 2;
          }
          else
          {
            entityType = 3;
          }

          Debug.Log($"Noise for {x_}, {z_}: {noise} .. entity type: {entityType}");

          if (entityType != -1)
            new ScriptEntity(entityType, new Vector3(x_, 0, z_), -1);
        }
      }
    }
  }

}