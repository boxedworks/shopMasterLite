
namespace Assets.Scripts.Game.SimpleScript.Scripting.Logic
{
  class Variable
  {
    public string _Value;
    int _logicDepth;

    public Variable(int logicDepth)
    {
      _logicDepth = logicDepth;
    }

    public bool IsInScope(int currentLogicDepth)
    {
      return currentLogicDepth >= _logicDepth;
    }
  }
}