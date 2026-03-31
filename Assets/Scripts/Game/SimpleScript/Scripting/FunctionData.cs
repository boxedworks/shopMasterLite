using System;

namespace Assets.Scripts.Game.SimpleScript.Scripting
{
  // Holds all function data
  [Serializable]
  public class FunctionData
  {
    public int Id;

    public string Name;
    public int ParameterCount;

    public string Description;
  }

}