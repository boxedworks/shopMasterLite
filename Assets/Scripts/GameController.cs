using UnityEngine;

using SimpleScript;
using CustomUI;

public class GameController : MonoBehaviour
{
  UIElements _uiElements { get { return UIElements.s_Singleton; } }

  public void Start()
  {
    new GameResources();
    ScriptManager.Initialize();
    ItemManager.Initialize();
    new PlayerController();
    new UIElements();
    new SfxController();

    // Initizalize other systems
    new Terminal();

    ScriptEntityHelper.SaveTypeData();

    _lastTick = Time.time;
    s_CurrentTick = 1;

    //
    LoadGame();
  }

  //
  void LoadGame()
  {

    // Check for load data, if none, create new game
    var hasSaveData = false;
    if (hasSaveData)
    {

    }

    // New save data
    else
    {

      // Create starting area


      //
      new ScriptEntity(1, new Vector3(0, 0, -1), -1);
      new ScriptEntity(2, new Vector3(0, 0, -3), -1);

      new ScriptEntity(1, new Vector3(3, 0, 0), -1);
      new ScriptEntity(1, new Vector3(3, 0, -1), -1);
      new ScriptEntity(1, new Vector3(3, 0, 1), -1);
      new ScriptEntity(1, new Vector3(3, 0, -2), -1);
      new ScriptEntity(1, new Vector3(3, 0, 2), -1);
      new ScriptEntity(1, new Vector3(3, 0, -3), -1);
      new ScriptEntity(1, new Vector3(3, 0, 3), -1);

      new ScriptEntity(1, new Vector3(-3, 0, 0), -1);
      new ScriptEntity(1, new Vector3(-3, 0, -1), -1);
      new ScriptEntity(1, new Vector3(-3, 0, 1), -1);
      new ScriptEntity(1, new Vector3(-3, 0, -2), -1);
      new ScriptEntity(1, new Vector3(-3, 0, 2), -1);
      new ScriptEntity(1, new Vector3(-3, 0, -3), -1);
      new ScriptEntity(1, new Vector3(-3, 0, 3), -1);

      new ScriptEntity(1, new Vector3(2, 0, -3), -1);
      new ScriptEntity(1, new Vector3(-2, 0, 3), -1);
      new ScriptEntity(1, new Vector3(1, 0, -3), -1);
      new ScriptEntity(1, new Vector3(-1, 0, 3), -1);
      new ScriptEntity(1, new Vector3(0, 0, 3), -1);
      new ScriptEntity(1, new Vector3(-2, 0, -3), -1);
      new ScriptEntity(1, new Vector3(2, 0, 3), -1);
      new ScriptEntity(1, new Vector3(1, 0, 3), -1);
      new ScriptEntity(1, new Vector3(-1, 0, -3), -1);

    }


  }

  //
  float _lastTick;
  public static float s_TickRate = 0.1f;
  public static int s_CurrentTick;
  void Update()
  {

    // Tick scripts
    if (Time.time - _lastTick > s_TickRate)
    {
      _lastTick += s_TickRate;
      s_CurrentTick++;

      //Debug.Log($"<color=yellow>Tick: {s_CurrentTick}</color>");
      ScriptManager.TickScripts();
      ScriptEntity.TickScriptEntities();
    }

    // Update entities
    ScriptEntity.UpdateScriptEntities();

    // Update player
    PlayerController.s_Singleton.Update();

    _uiElements.Update();
    SfxController.Update();
  }

}
