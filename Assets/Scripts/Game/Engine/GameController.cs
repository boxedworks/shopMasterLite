using UnityEngine;

using Assets.Scripts.Game.SimpleScript;
using CustomUI;

public class GameController : MonoBehaviour
{
  public static GameController s_Singleton { get; private set; }

  UIElements _uiElements { get { return UIElements.s_Singleton; } }

  public Material _baseEntityMaterial;

  public void Start()
  {
    s_Singleton = this;

    new GameResources();
    ScriptManager.Initialize();
    ItemManager.Initialize();
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
    ItemManager.Update();

    // Update player
    PlayerController.s_Singleton.Update();

    _uiElements.Update();
    SfxController.Update();
  }

}
