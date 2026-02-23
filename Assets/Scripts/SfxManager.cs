using UnityEngine;

public class SfxManager
{

  //
  public static SfxManager s_Singleton;
  public SfxManager()
  {
    s_Singleton = this;
  }

  //
  public static void PlaySfx(string sfxName, Vector3 position)
  {
    var sfxPrefab = Resources.Load<GameObject>($"Sfx/{sfxName}");
    if (sfxPrefab == null)
    {
      Debug.LogError($"Sfx prefab not found: {sfxName}");
      return;
    }

    Object.Instantiate(sfxPrefab, position, Quaternion.identity);
  }
}