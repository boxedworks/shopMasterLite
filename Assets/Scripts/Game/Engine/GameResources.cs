using UnityEngine;

public class GameResources
{

  static GameResources s_singleton;

  Camera _mainCamera;
  public static Camera _MainCamera { get { return s_singleton._mainCamera; } }

  //
  public GameResources()
  {
    s_singleton = this;

    _mainCamera = Camera.main;
  }

  //
  public static Sprite LoadSprite(string path)
  {
    return Resources.Load<Sprite>($"Images/{path}");
  }
  public static Sprite LoadItemSprite(string path)
  {
    return LoadSprite($"items/{path}");
  }


}