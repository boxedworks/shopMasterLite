
namespace Assets.Scripts.Game.SimpleScript.Scripting.Logic
{
  class LoopData
  {
    public enum LoopType
    {
      FOR,
      WHILE
    }
    public LoopType _Type;
    public int _LineIndexStart, _LogicDepth, _LoopCounter;
    public bool _Break;
  }
}