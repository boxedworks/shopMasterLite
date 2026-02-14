using UnityEngine;

using SimpleScript;
using CustomUI;

public class GameController : MonoBehaviour
{
  UIElements _uiElements { get { return UIElements.s_Singleton; } }

  public void Start()
  {
    // Initialize Script Manager
    ScriptManager.Initialize();
    ItemManager.Initialize();
    new PlayerController();
    new UIElements();

    new ScriptEntity(4, new Vector3(0, 0, 0), -1);
    var playerEntity = new ScriptEntity(0, new Vector3(0, 0, 1), -1);
    playerEntity.LoadAndAttachScript(new ScriptManager.ScriptLoadData()
    {
      PathTo = "test"
    });
    playerEntity._EntityData.ItemStorage = new();
    for (int i = 0; i < 4; i++)
      playerEntity._EntityData.ItemStorage.Add(null);

    // Initizalize other systems
    new Terminal();

    ScriptEntity.ScriptEntityHelper.SaveTypeData();

    _lastTick = Time.time;
  }

  //
  float _lastTick,
    _tickRate = 0.1f;
  void Update()
  {

    // Tick scripts
    if (Time.time - _lastTick > _tickRate)
    {
      _lastTick += _tickRate;

      ScriptManager.TickScripts();
      ScriptEntity.TickScriptEntities();
    }

    // Update player
    PlayerController.s_Singleton.Update();
    _uiElements.Update();
  }

}
