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


}