using UnityEngine;
using SimpleScript;

public class GameController : MonoBehaviour
{


  public void Start()
  {
    // Initialize Script Manager
    ScriptManager.Initialize(new ScriptManager());

    new ScriptEntity(4, new Vector3(0, 0, 5), -1);
    var secondEntity = new ScriptEntity(0, new Vector3(0, 0, 6), -1);
    secondEntity.LoadAndAttachScript(new ScriptManager.ScriptLoadData()
    {
      PathTo = "test"
    });

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
    if (Time.time - _lastTick > _tickRate)
    {
      _lastTick += _tickRate;

      ScriptManager.TickScripts();
      ScriptEntity.TickScriptEntities();
    }
  }

}
