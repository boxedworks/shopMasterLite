
using System;

namespace Assets.Scripts.Game.SimpleScript.Scripting
{
  public class SystemFunction
  {
    public string _Name;

    // Script base, accesor, parameters
    public Func<ScriptBase, string, string[], SystemFunctionReturnData> _Function;

    public SystemFunctionReturnData Execute(ScriptBase script, string accessor, string[] parameters)
    {
      return _Function(script, accessor, parameters);
    }
  }
}