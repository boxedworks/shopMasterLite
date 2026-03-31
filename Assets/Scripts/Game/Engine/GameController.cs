using UnityEngine;
using Assets.Scripts.Game.SimpleScript.Scripting;
using Assets.Scripts.Game.SimpleScript.Entities.Item;
using Assets.Scripts.Game.SimpleScript.Entities.Entity;
using Assets.Scripts.Game.UI;

public class GameController : MonoBehaviour
{
  public static GameController s_Singleton { get; private set; }

  UIElements _uiElements { get { return UIElements.s_Singleton; } }

  public Material _baseEntityMaterial;

  public void Start()
  {
    s_Singleton = this;

    new GameResources();
    ScriptBaseController.Initialize();
    ScriptItemController.Initialize();
    new PlayerController();
    new UIElements();
    new SfxController();

    // Initizalize other systems
    new Terminal();

    _lastTick = Time.time;
    s_CurrentTick = 1;

    //
    ScriptEntityHelper.LoadEntityData();

    // Other settings

    //
    Debug.Log("Game loaded");
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
      ScriptBaseController.TickScripts();
      ScriptEntity.TickScriptEntities();
    }

    // Update entities
    ScriptEntity.UpdateScriptEntities();
    ScriptItemController.Update();

    // Update player
    PlayerController.s_Singleton.Update();

    _uiElements.Update();
    SfxController.Update();
  }

}
