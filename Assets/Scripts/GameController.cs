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


    var playerEntity = new ScriptEntity(0, new Vector3(0, 0, 1), -1);
    playerEntity.LoadAndAttachScript(new ScriptManager.ScriptLoadData()
    {
      PathTo = "test",
      ScriptType = ScriptManager.ScriptType.PLAYER
    });
    playerEntity._EntityData.ItemStorage = new();
    for (int i = 0; i < 4; i++)
      playerEntity._Storage.Add(null);

    new ScriptEntity(4, new Vector3(0, 0, 0), -1);


    // Initizalize other systems
    new Terminal();

    ScriptEntity.ScriptEntityHelper.SaveTypeData();

    _lastTick = Time.time;
  }

  //
  float _lastTick,
    _tickRate = 2f;//0.1f;
  public static int s_CurrentTick;
  void Update()
  {

    // Tick scripts
    if (Time.time - _lastTick > _tickRate)
    {
      _lastTick += _tickRate;
      s_CurrentTick++;

      Debug.Log($"<color=yellow>Tick: {s_CurrentTick}</color>");
      ScriptManager.TickScripts();
      ScriptEntity.TickScriptEntities();
    }

    // Update player
    PlayerController.s_Singleton.Update();
    _uiElements.Update();
  }

}
